"""Scoped Windows .NET runner; no repository-wide runner changes or dependencies."""
from __future__ import annotations

import contextlib
import ctypes
from ctypes import wintypes
import json
import msvcrt
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / ".tmp/transverse-analysis-print-corrections"


class InvalidObservation(RuntimeError):
    """No executed native assertion can be inferred."""


@contextlib.contextmanager
def build_lock(timeout=120):
    """One process-safe lock for all correction build/test commands."""
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    with (EVIDENCE / "dotnet.lock").open("a+b") as stream:
        stream.seek(0)
        stream.write(b"0")
        stream.flush()
        deadline = time.monotonic() + timeout
        while True:
            stream.seek(0)
            try:
                msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
                break
            except OSError:
                if time.monotonic() >= deadline:
                    raise InvalidObservation("dotnet lock acquisition timed out")
                time.sleep(0.1)
        try:
            yield
        finally:
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)


class _Job:
    """Kill all owned descendants on close, including a surviving testhost."""

    def __init__(self):
        class Basic(ctypes.Structure):
            _fields_ = [("process_time", ctypes.c_int64), ("job_time", ctypes.c_int64),
                        ("flags", wintypes.DWORD), ("min_ws", ctypes.c_size_t),
                        ("max_ws", ctypes.c_size_t), ("active", wintypes.DWORD),
                        ("affinity", ctypes.c_size_t), ("priority", wintypes.DWORD),
                        ("scheduling", wintypes.DWORD)]

        class Io(ctypes.Structure):
            _fields_ = [(name, ctypes.c_uint64) for name in
                        ("read_ops", "write_ops", "other_ops", "read_bytes", "write_bytes", "other_bytes")]

        class Extended(ctypes.Structure):
            _fields_ = [("basic", Basic), ("io", Io), ("process_memory", ctypes.c_size_t),
                        ("job_memory", ctypes.c_size_t), ("peak_process", ctypes.c_size_t),
                        ("peak_job", ctypes.c_size_t)]

        self.api = ctypes.WinDLL("kernel32", use_last_error=True)
        self.api.CreateJobObjectW.argtypes = [ctypes.c_void_p, wintypes.LPCWSTR]
        self.api.CreateJobObjectW.restype = wintypes.HANDLE
        self.api.SetInformationJobObject.argtypes = [wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD]
        self.api.AssignProcessToJobObject.argtypes = [wintypes.HANDLE, wintypes.HANDLE]
        self.api.CloseHandle.argtypes = [wintypes.HANDLE]
        self.api.TerminateJobObject.argtypes = [wintypes.HANDLE, wintypes.UINT]
        self.api.QueryInformationJobObject.argtypes = [wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p,
                                                       wintypes.DWORD, ctypes.c_void_p]
        self.handle = self.api.CreateJobObjectW(None, None)
        info = Extended()
        info.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if not self.handle or not self.api.SetInformationJobObject(self.handle, 9, ctypes.byref(info), ctypes.sizeof(info)):
            self.close()
            raise ctypes.WinError(ctypes.get_last_error())

    def assign(self, process):
        if not self.api.AssignProcessToJobObject(self.handle, int(process._handle)):
            raise ctypes.WinError(ctypes.get_last_error())

    def close(self):
        if getattr(self, "handle", None):
            self.api.CloseHandle(self.handle)
            self.handle = None

    def terminate_and_wait(self):
        class Accounting(ctypes.Structure):
            _fields_ = [(name, ctypes.c_int64) for name in ("user", "kernel", "period_user", "period_kernel")] + [
                (name, wintypes.DWORD) for name in ("faults", "total", "active", "terminated")]
        if not self.api.TerminateJobObject(self.handle, 1):
            raise ctypes.WinError(ctypes.get_last_error())
        deadline = time.monotonic() + 5
        while True:
            info = Accounting()
            if not self.api.QueryInformationJobObject(self.handle, 1, ctypes.byref(info), ctypes.sizeof(info), None):
                raise ctypes.WinError(ctypes.get_last_error())
            if info.active == 0:
                return {"active_after_termination": 0, "total_owned_processes": info.total}
            if time.monotonic() > deadline:
                raise InvalidObservation(f"owned process cleanup did not finish: active={info.active}")
            time.sleep(0.05)


def run_owned(command, log_path, timeout):
    """Keep partial output, terminate descendants and reap the owned root."""
    job = _Job()
    process = None
    cleanup = {}
    try:
        with Path(log_path).open("w", encoding="utf-8") as output:
            # The launcher cannot create native descendants until assigned to
            # the job. This closes the launch/assignment orphan-process race.
            launcher = [sys.executable, "-c",
                        "import sys, subprocess; "
                        "token=sys.stdin.buffer.read(1); "
                        "sys.exit(subprocess.call(sys.argv[1:], stdin=subprocess.DEVNULL) if token else 125)",
                        *command]
            process = subprocess.Popen(launcher, cwd=ROOT, stdin=subprocess.PIPE, stdout=output, stderr=subprocess.STDOUT,
                                       creationflags=subprocess.CREATE_NO_WINDOW)
            job.assign(process)
            process.stdin.write(b"1")
            process.stdin.close()
            try:
                return process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                raise InvalidObservation(f"native process timed out after {timeout}s; log: {log_path}")
    finally:
        primary_error = sys.exc_info()[1]
        cleanup_error = None
        try:
            cleanup = job.terminate_and_wait()
        except Exception as exc:
            cleanup_error = exc
        finally:
            job.close()
            if process is not None:
                try:
                    if process.poll() is None:
                        process.kill()
                    process.wait(timeout=10)
                    cleanup.update(root_pid=process.pid, root_reaped=True)
                except Exception as exc:
                    cleanup_error = cleanup_error or exc
            if cleanup_error:
                cleanup["error"] = str(cleanup_error)
            try:
                Path(log_path).with_name("cleanup.json").write_text(json.dumps(cleanup, indent=2), encoding="utf-8")
            except OSError as exc:
                cleanup_error = cleanup_error or exc
        if cleanup_error:
            if primary_error is None:
                raise InvalidObservation(f"owned process cleanup failed: {cleanup_error}")
            primary_error.add_note(f"Cleanup also failed: {cleanup_error}; see cleanup.json")


def parse_trx(path, expected, returncode):
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(path).getroot()
    counters = root.find("t:ResultSummary/t:Counters", ns)
    rows = root.findall("t:Results/t:UnitTestResult", ns)
    if counters is None or not rows:
        raise InvalidObservation("TRX contains no result rows/counters")
    counts = {key: int(counters.get(key, "0")) for key in ("total", "executed", "passed", "failed")}
    methods = {row.get("id"): row.find("t:TestMethod", ns)
               for row in root.findall("t:TestDefinitions/t:UnitTest", ns)}
    if counts["total"] != len(rows) or counts["executed"] != len(rows):
        raise InvalidObservation(f"unexecuted/skipped or inconsistent results: {counts}")
    if counts["passed"] + counts["failed"] != len(rows):
        raise InvalidObservation(f"non-pass/fail results: {counts}")
    for name in expected:
        if not any(name in row.get("testName", "") for row in rows):
            raise InvalidObservation(f"required test did not execute: {name}")
    failures = []
    for row in rows:
        method = methods.get(row.get("testId"))
        if method is None or "executor://xunit/" not in method.get("adapterTypeName", ""):
            raise InvalidObservation(f"result was not executed by xUnit: {row.get('testName')}")
        outcome = row.get("outcome")
        if outcome == "Passed":
            continue
        error = row.find("t:Output/t:ErrorInfo", ns)
        detail = "" if error is None else " ".join(error.itertext())
        message = "" if error is None else error.findtext("t:Message", "", ns)
        is_assertion = "Xunit.Sdk." in detail or re.match(r"^Assert\.[A-Za-z]+\(.*\) Failure", message)
        if outcome != "Failed" or not is_assertion:
            raise InvalidObservation(f"non-xUnit assertion failure: {row.get('testName')}: {detail}")
        failures.append({"name": row.get("testName"), "assertion": detail})
    if any(not any(name in failure["name"] for name in expected) for failure in failures):
        raise InvalidObservation("an unrelated assertion failed; required test names must identify the Red")
    if returncode != (1 if failures else 0):
        raise InvalidObservation(f"dotnet exit {returncode} conflicts with native results")
    return {"failed": failures, "counts": counts,
            "executed": [row.get("testName") for row in rows],
            "description": json.dumps(failures, ensure_ascii=False)}


def observe():
    project = os.environ.get("TRANSVERSE_NATIVE_PROJECT", "")
    selection = os.environ.get("TRANSVERSE_NATIVE_FILTER", "")
    expected = [name for name in os.environ.get("TRANSVERSE_NATIVE_EXPECT", "").split(";") if name]
    label = os.environ.get("TRANSVERSE_NATIVE_LABEL", "native")
    if not project or not selection or not expected:
        raise InvalidObservation("PROJECT, FILTER and EXPECT environment values are required")
    project = (ROOT / project).resolve()
    if not project.is_file() or project.suffix != ".csproj" or not project.is_relative_to(ROOT):
        raise InvalidObservation(f"project must be a repository .csproj: {project}")
    if not re.fullmatch(r"[A-Za-z0-9_.-]+", label):
        raise InvalidObservation("LABEL must contain only letters, digits, dot, underscore or hyphen")
    timeout = int(os.environ.get("TRANSVERSE_NATIVE_TIMEOUT", "180"))
    if not 1 <= timeout <= 400:
        raise InvalidObservation("TIMEOUT must be 1..400 seconds; outer timeout must exceed 550s")
    output = EVIDENCE / "native" / f"{label}-{time.strftime('%Y%m%dT%H%M%S')}-{uuid.uuid4().hex[:8]}"
    output.mkdir(parents=True)
    command = ["dotnet", "test", str(project), "-c", "Release", "-m:1", "/nodeReuse:false",
               "--filter", selection, "--logger", "trx;LogFileName=native.trx",
               "--results-directory", str(output)]
    manifest = {"command": command, "expected": expected, "output": str(output), "timeout": timeout}
    try:
        with build_lock():
            returncode = run_owned(command, output / "dotnet.log", timeout)
        trx = output / "native.trx"
        if not trx.is_file():
            raise InvalidObservation(f"no TRX (restore/build/discovery failed); exit {returncode}")
        result = parse_trx(trx, expected, returncode)
        manifest.update(result)
        return result
    except Exception as exc:
        manifest["invalid"] = str(exc)
        raise InvalidObservation(f"{exc}; evidence: {output}") from exc
    finally:
        (output / "observation.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Native evidence: {output}")
