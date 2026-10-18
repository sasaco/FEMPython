#!/usr/bin/env python3
"""Run the project's tests for one target and check the observed outcome
against the outcome the caller expected.

This exists for the TDD red/green invariant. "Run the test and confirm it
fails" is not one observation but four: the test can fail (red, the intended
state), pass (nothing new is being asserted), fail to import or collect (a
syntax error or a bad fixture — red for the wrong reason), or collect nothing
at all (a mistyped path, or a test that was never written). ``pytest`` reports
all four as a non-zero exit, so a human reading "not passing" accepts every one
of them and the Green step then writes code against a test that never ran.

This script does **not** re-implement ``.agents/check.ps1``: that runs the whole
configured gate set and answers "is the project healthy". This answers the
narrower question "did this one run come out the way the caller said it would".
Which tests to write stays a judgment call and is never scripted; only the
invariant is.

Observed states (payload ``observed``), mapped from the pytest exit code:

    passed             exit 0 — every selected test passed
    failed             exit 1 — at least one selected test failed
    collection_error   exit 2 — collection aborted (bad import, syntax error)
    no_tests_collected exit 5 — nothing was selected

A pytest usage error (exit 4: unknown node id, unreadable path, unsupported
option such as ``--cov`` without ``pytest-cov`` installed) is a caller mistake,
not an observation: ``observed`` is ``null`` and the exit code is 1. A pytest
internal error (exit 3) or a missing/timed-out runner is an external failure:
``observed`` is ``null`` and the exit code is 3. ``observed`` is never guessed.

Usage:
    uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --target FrameWeb/tests/validation/test_unit_invariance.py --expect fail
    uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --target FrameWeb/tests/validation/test_unit_invariance.py --expect pass
    uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --target FrameWeb/tests/validation/test_unit_invariance.py --target FrameWeb/tests/validation/test_stored_references.py --expect pass --label green-2
    uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --target FrameWeb/tests --expect pass --cov FrameWeb --min-coverage 90

Exit codes:
    0  ok — observed matches --expect (and coverage meets --min-coverage)
    1  bad arguments — unparseable flags, a target that does not exist, or a
       pytest usage error (exit 4)
    2  contract violation — observed does not match --expect, or coverage is
       below --min-coverage
    3  external failure — no pytest runner available, the runner timed out, a
       pytest internal error, or the log file could not be written
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tomllib
from pathlib import Path
from typing import NoReturn

PROJECT_ROOT = Path(__file__).parent.parent.parent.parent

EXIT_OK = 0
EXIT_BAD_ARGS = 1
EXIT_EXPECTATION_VIOLATED = 2
EXIT_EXTERNAL_FAILURE = 3

# pytest's documented exit codes, split into observations and non-observations.
OBSERVED_BY_EXIT_CODE = {
    0: "passed",
    1: "failed",
    2: "collection_error",
    5: "no_tests_collected",
}
PYTEST_USAGE_ERROR = 4
PYTEST_INTERNAL_ERROR = 3

EXPECTED_OBSERVATION = {"pass": "passed", "fail": "failed"}

LABEL_RE = re.compile(r"^[a-z0-9][a-z0-9-]{0,63}$")
FAILED_TEST_RE = re.compile(r"^(?:FAILED|ERROR)\s+(\S+)")
COVERAGE_TOTAL_RE = re.compile(r"^TOTAL\s+.*?(\d+(?:\.\d+)?)%", re.MULTILINE)
SUMMARY_TOKENS = ("passed", "failed", "error", "no tests ran", "skipped")


def _emit(obj: dict) -> None:
    """Print a single JSON object to stdout."""
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    print(json.dumps(obj, ensure_ascii=False))


class JsonArgumentParser(argparse.ArgumentParser):
    """ArgumentParser that reports usage errors through this tool's own
    JSON-on-stdout / exit-1 contract instead of argparse's default stderr
    text + exit(2), so an unknown flag never masquerades as this tool's
    exit code 2 (expectation violated) or 3 (external failure)."""

    def error(self, message: str) -> NoReturn:
        _emit({"ok": False, "error": message, "artifacts": []})
        sys.exit(EXIT_BAD_ARGS)


def _as_text(raw: str | bytes | None) -> str:
    """Decode captured output that may arrive as bytes, str, or None."""
    if raw is None:
        return ""
    if isinstance(raw, bytes):
        return raw.decode("utf-8", errors="replace")
    return raw


def _rel(path: Path, project_root: Path) -> str:
    try:
        return path.resolve().relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def target_path(target: str, project_root: Path) -> Path:
    """Filesystem part of a pytest target, which may carry ``::node`` ids."""
    file_part = target.split("::", 1)[0]
    candidate = Path(file_part)
    return candidate if candidate.is_absolute() else project_root / candidate


def confined_target_path(target: str, project_root: Path) -> Path:
    """Resolve a pytest target and reject lexical or symlink escapes."""
    root = project_root.resolve()
    resolved = target_path(target, project_root).resolve()
    try:
        resolved.relative_to(root)
    except ValueError as exc:
        raise ValueError(f"target escapes project root: {target}") from exc
    return resolved


def configured_test_prefixes(
    project_root: Path,
) -> tuple[list[dict[str, object]], str | None, bool]:
    """Return Python test prefixes declared in ``repository.toml``.

    The boolean distinguishes an unconfigured repository (where the legacy
    bounded fallback remains useful) from a configured repository that declares
    no usable test prefix. The latter is an explicit ``no_gates`` state and must
    not silently fall back to an unrelated global pytest.
    """
    path = project_root / ".agents" / "repository.toml"
    if not path.is_file():
        return [], None, False
    try:
        data = tomllib.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, tomllib.TOMLDecodeError) as exc:
        return [], f"cannot parse .agents/repository.toml: {exc}", True
    components = data.get("components")
    if not isinstance(components, list):
        return [], "repository.toml has no [[components]] declarations", True

    prefixes: list[dict[str, object]] = []
    for component in components:
        if not isinstance(component, dict) or component.get("kind") != "python":
            continue
        commands = component.get("commands")
        prefix = commands.get("test_prefix") if isinstance(commands, dict) else None
        if not isinstance(prefix, list) or not prefix:
            continue
        if not all(isinstance(item, str) and item for item in prefix):
            return [], "a configured test_prefix contains a non-string value", True
        component_id = component.get("id")
        working_directory = component.get("working_directory", ".")
        if not isinstance(component_id, str) or not component_id:
            return [], "a Python component has no non-empty id", True
        if not isinstance(working_directory, str) or not working_directory:
            return [], f"component {component_id} has no working_directory", True
        prefixes.append(
            {
                "id": component_id,
                "working_directory": Path(working_directory).as_posix(),
                "prefix": list(prefix),
            }
        )
    if not prefixes:
        return [], "no configured Python test gate (commands.test_prefix)", True
    return prefixes, None, True


def _target_under(target: str, directory: str) -> bool:
    """Return whether the filesystem portion of *target* is under *directory*."""
    if directory in {"", "."}:
        return True
    target_parts = Path(target.split("::", 1)[0]).parts
    directory_parts = Path(directory).parts
    return target_parts[: len(directory_parts)] == directory_parts


def select_configured_prefix(
    targets: list[str],
    project_root: Path,
    requested_component: str | None,
) -> tuple[list[str] | None, str | None, str | None, bool]:
    """Select one configured Python test prefix for *targets*.

    Returns ``(prefix, component_id, error, config_present)``. Agent-framework
    tests outside a product working directory use the sole Python environment,
    which is how this repository runs ``.agents/tests`` without a root venv.
    """
    prefixes, error, config_present = configured_test_prefixes(project_root)
    if error:
        return None, None, error, config_present
    if not config_present:
        return None, None, None, False
    if requested_component is not None:
        matches = [item for item in prefixes if item["id"] == requested_component]
        if not matches:
            return (
                None,
                None,
                f"component {requested_component!r} has no configured Python test gate",
                True,
            )
        selected = matches[0]
    else:
        matches = [
            item
            for item in prefixes
            if all(
                _target_under(target, str(item["working_directory"]))
                for target in targets
            )
        ]
        if len(matches) == 1:
            selected = matches[0]
        elif len(prefixes) == 1:
            selected = prefixes[0]
        else:
            return None, None, "test targets do not select one configured component", True
    return list(selected["prefix"]), str(selected["id"]), None, True


def targets_for_runner(
    targets: list[str], project_root: Path, runner_note: str
) -> list[str]:
    """Render repo-root targets in a configured component's coordinates.

    The public CLI always accepts repository-relative targets. A configured
    prefix may establish a component working directory (for example, ``uv
    --directory FrameWeb``), so the target arguments appended to that prefix
    must be relative to the component rather than the repository root.
    """
    if not runner_note.startswith("repository.toml:"):
        return list(targets)

    component_id = runner_note.partition(":")[2]
    prefixes, error, _ = configured_test_prefixes(project_root)
    if error:
        return list(targets)
    selected = next((item for item in prefixes if item["id"] == component_id), None)
    if selected is None:
        return list(targets)

    component_root = (
        project_root / str(selected["working_directory"])
    ).resolve()
    rendered: list[str] = []
    for target in targets:
        file_part, separator, node_id = target.partition("::")
        absolute_target = confined_target_path(file_part, project_root)
        relative_target = Path(
            os.path.relpath(absolute_target, start=component_root)
        ).as_posix()
        rendered.append(
            relative_target + (f"::{node_id}" if separator else "")
        )
    return rendered


def resolve_runner(
    runner: str,
    project_root: Path,
    targets: list[str] | None = None,
    component: str | None = None,
) -> tuple[list[str] | None, str]:
    """Return the argv prefix that runs pytest, or ``(None, reason)``.

    Prefer the configured component command, then use a bounded legacy fallback.
    This only decides *how* to invoke pytest, never which gates a project has.
    """
    has_pyproject = (project_root / "pyproject.toml").is_file()
    if runner == "auto":
        prefix, component_id, error, config_present = select_configured_prefix(
            targets or [], project_root, component
        )
        if error:
            return None, error
        if config_present and prefix is not None:
            if shutil.which(prefix[0]) is None:
                return None, f"configured test runner is not on PATH: {prefix[0]}"
            return prefix, f"repository.toml:{component_id}"
    if runner in {"auto", "uv"}:
        if shutil.which("uv") and (has_pyproject or runner == "uv"):
            return ["uv", "run", "pytest"], "uv"
        if runner == "uv":
            return None, "uv is not on PATH"
    if runner in {"auto", "pytest"}:
        pytest_bin = shutil.which("pytest")
        if pytest_bin:
            return [pytest_bin], "pytest"
        if runner == "pytest":
            return None, "pytest is not on PATH"
    # `python -m pytest` exits 1 when pytest is not importable, which would be
    # indistinguishable from a failing test — so probe before trusting it.
    try:
        probe = subprocess.run(
            [sys.executable, "-c", "import pytest"],
            capture_output=True,
            text=True,
            timeout=60,
        )
    except (OSError, subprocess.SubprocessError) as exc:
        return None, f"cannot probe for pytest: {exc}"
    if probe.returncode == 0:
        return [sys.executable, "-m", "pytest"], "python -m pytest"
    return None, "no pytest runner available (tried uv, pytest, python -m pytest)"


def build_command(prefix: list[str], targets: list[str], cov: list[str]) -> list[str]:
    command = [*prefix, "-p", "no:cacheprovider", *targets]
    for module in cov:
        command.append(f"--cov={module}")
    if cov:
        command.append("--cov-report=term-missing")
    return command


def parse_summary(output: str) -> str | None:
    """Last pytest summary line, e.g. ``1 failed in 0.01s``."""
    for line in reversed(output.splitlines()):
        stripped = line.strip().strip("=").strip()
        if stripped and any(token in stripped for token in SUMMARY_TOKENS):
            return stripped
    return None


def parse_failed_tests(output: str) -> list[str]:
    return [
        match.group(1)
        for line in output.splitlines()
        if (match := FAILED_TEST_RE.match(line.strip()))
    ]


def parse_coverage(output: str) -> float | None:
    matches = COVERAGE_TOTAL_RE.findall(output)
    return float(matches[-1]) if matches else None


def _build_parser() -> JsonArgumentParser:
    parser = JsonArgumentParser(
        description="Run tests for one target and check the outcome against "
        "the expected outcome",
    )
    parser.add_argument(
        "--expect",
        required=True,
        choices=sorted(EXPECTED_OBSERVATION),
        help="Outcome the caller expects: 'fail' for the TDD Red step, "
        "'pass' for Green and Refactor",
    )
    parser.add_argument(
        "--target",
        action="append",
        default=[],
        metavar="PATH",
        help="Test file, directory, or 'file::node' id (repeatable, "
        "at least one required)",
    )
    parser.add_argument(
        "--cov",
        action="append",
        default=[],
        metavar="MODULE",
        help="Measure coverage of MODULE (repeatable; requires the pytest-cov plugin)",
    )
    parser.add_argument(
        "--min-coverage",
        type=float,
        metavar="N",
        help="Fail with exit 2 when total coverage is below N percent (requires --cov)",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=600.0,
        metavar="SECONDS",
        help="Kill the test run after SECONDS (default: 600)",
    )
    parser.add_argument(
        "--runner",
        choices=("auto", "uv", "pytest", "python"),
        default="auto",
        help="How to invoke pytest (default: auto — uv, then pytest, then "
        "python -m pytest)",
    )
    parser.add_argument(
        "--component",
        default=None,
        help="Configured Python component id (normally inferred from --target)",
    )
    parser.add_argument(
        "--label",
        default="run-tests",
        help="Log file name stem under .agents/logs/ (default: run-tests)",
    )
    parser.add_argument(
        "--project-root",
        type=Path,
        default=PROJECT_ROOT,
        help="Repository root the tests run in (defaults to 4 levels above "
        "this script)",
    )
    return parser


def validate_args(args: argparse.Namespace) -> str | None:
    """Return an error message if the arguments are unusable, else None."""
    if not args.target:
        return "at least one --target is required"
    if not LABEL_RE.match(args.label):
        return f"invalid --label '{args.label}': expected {LABEL_RE.pattern}"
    if args.timeout <= 0:
        return "--timeout must be greater than 0"
    if args.min_coverage is not None and not args.cov:
        return "--min-coverage requires at least one --cov MODULE"
    if args.min_coverage is not None and not 0 <= args.min_coverage <= 100:
        return "--min-coverage must be between 0 and 100"
    if not args.project_root.is_dir():
        return f"--project-root is not a directory: {args.project_root}"
    for target in args.target:
        try:
            resolved = confined_target_path(target, args.project_root)
        except (OSError, ValueError) as exc:
            return str(exc)
        if not resolved.exists():
            return f"target does not exist: {target}"
    return None


def main() -> int:  # noqa: C901 — single-function CLI entry point
    args = _build_parser().parse_args()

    error = validate_args(args)
    if error:
        _emit({"ok": False, "error": error, "artifacts": []})
        return EXIT_BAD_ARGS

    prefix, runner_note = resolve_runner(
        args.runner, args.project_root, args.target, args.component
    )
    if prefix is None:
        no_gate = (
            "configured" in runner_note
            or "repository.toml" in runner_note
            or "component" in runner_note
        )
        _emit(
            {
                "ok": False,
                "expected": EXPECTED_OBSERVATION[args.expect],
                "observed": None,
                "overall": "no_gates" if no_gate else "external_failure",
                "error": runner_note,
                "artifacts": [],
            }
        )
        return EXIT_EXPECTATION_VIOLATED if no_gate else EXIT_EXTERNAL_FAILURE

    try:
        command_targets = targets_for_runner(
            args.target, args.project_root, runner_note
        )
    except (OSError, ValueError) as exc:
        _emit({"ok": False, "error": str(exc), "artifacts": []})
        return EXIT_BAD_ARGS
    command = build_command(prefix, command_targets, args.cov)
    payload: dict[str, object] = {
        "expected": EXPECTED_OBSERVATION[args.expect],
        "observed": None,
        "runner": runner_note,
        "component": (
            runner_note.partition(":")[2]
            if runner_note.startswith("repository.toml:")
            else None
        ),
        "command": command,
        "exit_code": None,
        "summary": None,
        "failed_tests": [],
        "coverage_percent": None,
        "min_coverage": args.min_coverage,
        "log_file": None,
        "artifacts": [],
        "error": None,
    }

    try:
        completed = subprocess.run(
            command,
            cwd=str(args.project_root),
            capture_output=True,
            text=True,
            timeout=args.timeout,
        )
        output = completed.stdout + completed.stderr
        exit_code: int | None = completed.returncode
        timed_out = False
    except subprocess.TimeoutExpired as exc:
        # Even with text=True, TimeoutExpired carries the partial output as
        # bytes (CPython does not decode it), so each half is decoded here
        # rather than concatenated blindly.
        output = _as_text(exc.stdout) + _as_text(exc.stderr)
        exit_code = None
        timed_out = True
    except OSError as exc:
        _emit({**payload, "ok": False, "error": f"cannot run {command[0]}: {exc}"})
        return EXIT_EXTERNAL_FAILURE

    log_path = args.project_root / ".agents" / "logs" / f"{args.label}.log"
    try:
        log_path.parent.mkdir(parents=True, exist_ok=True)
        log_path.write_text(output, encoding="utf-8")
    except OSError as exc:
        _emit({**payload, "ok": False, "error": f"cannot write log: {exc}"})
        return EXIT_EXTERNAL_FAILURE

    payload["log_file"] = _rel(log_path, args.project_root)
    payload["artifacts"] = [payload["log_file"]]
    payload["exit_code"] = exit_code
    payload["summary"] = parse_summary(output)
    payload["failed_tests"] = parse_failed_tests(output)
    payload["coverage_percent"] = parse_coverage(output)

    if timed_out:
        payload["error"] = f"test run timed out after {args.timeout}s"
        _emit({**payload, "ok": False})
        return EXIT_EXTERNAL_FAILURE

    if exit_code == PYTEST_USAGE_ERROR:
        payload["error"] = (
            "pytest usage error (exit 4): the target, node id, or an option is "
            f"not usable — see {payload['log_file']}"
        )
        _emit({**payload, "ok": False})
        return EXIT_BAD_ARGS

    if exit_code not in OBSERVED_BY_EXIT_CODE:
        detail = (
            "pytest internal error"
            if exit_code == PYTEST_INTERNAL_ERROR
            else "unexpected pytest exit code"
        )
        payload["error"] = f"{detail} ({exit_code}) — see {payload['log_file']}"
        _emit({**payload, "ok": False})
        return EXIT_EXTERNAL_FAILURE

    observed = OBSERVED_BY_EXIT_CODE[exit_code]
    payload["observed"] = observed

    if observed != payload["expected"]:
        payload["error"] = (
            f"expected the run to be '{payload['expected']}' but observed "
            f"'{observed}' — see {payload['log_file']}"
        )
        _emit({**payload, "ok": False})
        return EXIT_EXPECTATION_VIOLATED

    if args.cov and payload["coverage_percent"] is None:
        payload["error"] = (
            "coverage was requested but no TOTAL line was produced (is "
            f"pytest-cov installed?) — see {payload['log_file']}"
        )
        _emit({**payload, "ok": False})
        return EXIT_EXTERNAL_FAILURE

    coverage = payload["coverage_percent"]
    if (
        args.min_coverage is not None
        and isinstance(coverage, float)
        and coverage < args.min_coverage
    ):
        payload["error"] = (
            f"coverage {coverage}% is below the required {args.min_coverage}%"
        )
        _emit({**payload, "ok": False})
        return EXIT_EXPECTATION_VIOLATED

    _emit({**payload, "ok": True})
    return EXIT_OK


if __name__ == "__main__":
    sys.exit(main())
