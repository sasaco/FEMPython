from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import tomllib
from types import ModuleType
from typing import Any

import pytest


ROOT = Path(__file__).parents[2]
AGENTS = ROOT / ".agents"
SHARED = AGENTS / "skills/_shared"
CHECK = AGENTS / "check.ps1"
SIMPLIFY_GATE = AGENTS / "skills/simplify/simplify_gate.py"
POWERSHELL = shutil.which("pwsh") or shutil.which("powershell")
DELETED_GATES = (
    ".agents/check.sh",
    ".agents/skills/_shared/verify.sh",
)
ACTIVE_SUFFIXES = frozenset({".md", ".py", ".toml"})
ACTIVE_EXCLUDED_PARTS = frozenset(
    {"logs", "checkpoints", "research", "reviews", "plans"}
)
FORBIDDEN_ACTIVE_TEXT = {
    "mojibake": tuple(
        "".join(chr(codepoint) for codepoint in codepoints)
        for codepoints in (
            (0x9B29, 0x5305),
            (0x9A55, 0xFF6F),
            (0x90E2, 0x6662),
            (0x7E5D, 0xFF7B),
            (0x90B5, 0xFF7A),
            (0x906F, 0xFF76),
        )
    ),
    "deleted-agent": tuple(
        "-".join(parts)
        for parts in (
            ("codex", "debugger"),
            ("fable", "advisor"),
            ("general", "purpose", "opus"),
            ("general", "purpose", "sonnet"),
        )
    ),
    "missing-template": ("".join(("TEMPLATE_", "DESIGN_LOG.md")),),
    "mutating-install": (
        "".join(("npm install ", "-g @openai/codex@latest")),
    ),
    "noncanonical-python": ("".join(("python", "3 ")),),
}


def live_markdown_files() -> list[Path]:
    excluded = {"logs", "checkpoints", "research", "reviews", "plans"}
    files = [
        path
        for path in AGENTS.rglob("*.md")
        if not excluded.intersection(path.relative_to(AGENTS).parts)
    ]
    files.extend(
        path
        for path in (ROOT / "AGENTS.md", ROOT / "README.md")
        if path.is_file()
    )
    return files


def active_agent_files() -> list[Path]:
    return sorted(
        path
        for path in AGENTS.rglob("*")
        if path.is_file()
        and path.suffix.casefold() in ACTIVE_SUFFIXES
        and not ACTIVE_EXCLUDED_PARTS.intersection(path.relative_to(AGENTS).parts)
    )


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def git(root: Path, *arguments: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(root), *arguments],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=True,
    )
    return completed.stdout.strip()


def gate_config(include_product_gates: bool = True) -> str:
    if not include_product_gates:
        return "schema_version = 1\n"
    return """schema_version = 1
[[gates]]
id = "python-tests"
component = "python"
classification = "product"
optional = false
command = ["not-a-real-python-gate", "python"]
[[gates]]
id = "angular-build"
component = "angular"
classification = "product"
optional = false
command = ["not-a-real-angular-gate", "angular"]
[[gates]]
id = "dotnet-build"
component = "dotnet"
classification = "product"
optional = false
command = ["not-a-real-dotnet-gate", "dotnet"]
"""


def init_gate_fixture(root: Path, include_product_gates: bool = True) -> str:
    git(root, "init")
    git(root, "config", "user.email", "agents@example.invalid")
    git(root, "config", "user.name", "Agent Test")
    write(root / ".agents/repository.toml", gate_config(include_product_gates))
    write(root / "README.md", "fixture\n")
    write(root / "FrameWeb/staged.py", "VALUE = 1\n")
    write(root / "tools/unstaged.txt", "before\n")
    git(root, "add", ".")
    git(root, "commit", "-m", "baseline")
    return git(root, "rev-parse", "HEAD")


def run_check(root: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    if POWERSHELL is None:
        pytest.skip("PowerShell is unavailable")
    return subprocess.run(
        [
            POWERSHELL,
            "-NoProfile",
            "-NonInteractive",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            str(CHECK),
            "-ProjectRoot",
            str(root),
            *arguments,
        ],
        capture_output=True,
        encoding="utf-8-sig",
        errors="replace",
        check=False,
    )


def load_simplify_gate() -> ModuleType:
    spec = importlib.util.spec_from_file_location("agents_simplify_gate", SIMPLIFY_GATE)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_bash_gates_and_claude_hooks_are_removed() -> None:
    assert not (AGENTS / "check.sh").exists()
    assert not (SHARED / "verify.sh").exists()
    hooks = AGENTS / "hooks"
    assert not hooks.exists() or not any(path.is_file() for path in hooks.rglob("*"))


def test_live_docs_do_not_reference_deleted_gates() -> None:
    offenders: list[str] = []
    for path in live_markdown_files():
        text = path.read_text(encoding="utf-8")
        for deleted in DELETED_GATES:
            if deleted in text:
                offenders.append(f"{path.relative_to(ROOT).as_posix()}: {deleted}")
    assert offenders == []


def test_active_agent_surfaces_have_no_copied_runtime_residue() -> None:
    offenders: list[str] = []
    for path in active_agent_files():
        text = path.read_text(encoding="utf-8")
        for line_number, line in enumerate(text.splitlines(), start=1):
            for category, needles in FORBIDDEN_ACTIVE_TEXT.items():
                for needle in needles:
                    if needle in line:
                        relative = path.relative_to(ROOT).as_posix()
                        offenders.append(f"{relative}:{line_number}: {category}")
    assert offenders == []


def test_shared_readme_names_only_existing_shared_files() -> None:
    text = (SHARED / "README.md").read_text(encoding="utf-8")
    contents = text.split("## Contents", 1)[1].split("## Shared Script Contract", 1)[0]
    names = set(re.findall(r"`([A-Za-z0-9_-]+\.(?:py|md))`", contents))
    missing = sorted(name for name in names if not (SHARED / name).is_file())
    assert missing == []


def test_powershell_gate_covers_required_agent_checks() -> None:
    text = (AGENTS / "check.ps1").read_text(encoding="utf-8")
    for required in (
        "AgentOnly",
        "PYTHONDONTWRITEBYTECODE",
        ".agents/tests",
        "detect_stack.py",
        "state-doc",
        "design-doc",
        "plan-doc",
        "Test-LiveScriptReferences",
        "Test-ForeignReferences",
        "Test-NoAgentCaches",
        "Invoke-ScopeIsolation",
        "ListGates",
        "ScopeAllowlist",
        "git', 'diff', '--check",
    ):
        assert required in text
    assert "bash" not in text.casefold()


def test_list_gates_emits_json_without_executing_declared_commands(
    tmp_path: Path,
) -> None:
    init_gate_fixture(tmp_path)

    completed = run_check(tmp_path, "-ListGates")
    payload = json.loads(completed.stdout)

    assert completed.returncode == 0
    assert set(("ok", "overall", "tools", "log_file", "warnings", "artifacts")) <= set(payload)
    assert payload["ok"] is True
    assert payload["overall"] == "pass"
    for gate in ("python-tests", "angular-build", "dotnet-build"):
        assert payload["tools"][f"product: {gate}"]["status"] == "listed"
    assert "not recognized" not in completed.stderr
    log_path = tmp_path / payload["log_file"]
    assert log_path.is_file()
    assert payload["artifacts"] == [payload["log_file"]]


def test_scope_isolation_covers_committed_staged_and_untracked_paths(
    tmp_path: Path,
) -> None:
    baseline = init_gate_fixture(tmp_path)
    git(tmp_path, "config", "core.quotePath", "true")
    write(tmp_path / "FramePrintPDF/committed.txt", "committed\n")
    git(tmp_path, "add", "FramePrintPDF/committed.txt")
    git(tmp_path, "commit", "-m", "product commit")
    write(tmp_path / "FrameWeb/staged.py", "VALUE = 2\n")
    git(tmp_path, "add", "FrameWeb/staged.py")
    write(tmp_path / "tools/unstaged.txt", "after\n")
    write(tmp_path / "FrameWebforJS/untracked.js", "export const value = 3;\n")
    write(
        tmp_path / "FrameWebforJS/日本語-未追跡.js",
        "export const japaneseName = true;\n",
    )

    rejected = run_check(tmp_path, "-ListGates", "-BaselineRef", baseline)
    rejected_payload = json.loads(rejected.stdout)
    disallowed = set(
        rejected_payload["tools"]["scope-isolation"]["disallowed_product_paths"]
    )

    assert rejected.returncode == 2
    assert rejected_payload["overall"] == "fail"
    assert disallowed == {
        "FramePrintPDF/committed.txt",
        "FrameWeb/staged.py",
        "FrameWebforJS/untracked.js",
        "FrameWebforJS/日本語-未追跡.js",
        "tools/unstaged.txt",
    }

    allowlist = sorted(disallowed)
    write(tmp_path / ".agents/scope-allowlist.json", json.dumps(allowlist))
    accepted = run_check(
        tmp_path,
        "-ListGates",
        "-BaselineRef",
        baseline,
        "-ScopeAllowlist",
        ".agents/scope-allowlist.json",
    )
    accepted_payload = json.loads(accepted.stdout)

    assert accepted.returncode == 0
    assert accepted_payload["ok"] is True
    assert accepted_payload["tools"]["scope-isolation"]["status"] == "pass"
    assert accepted_payload["tools"]["scope-isolation"]["allowed_product_paths"] == allowlist


def test_list_gates_reports_no_product_gates_with_exit_two(tmp_path: Path) -> None:
    init_gate_fixture(tmp_path, include_product_gates=False)

    completed = run_check(tmp_path, "-ListGates")
    payload = json.loads(completed.stdout)

    assert completed.returncode == 2
    assert payload["ok"] is False
    assert payload["overall"] == "no_gates"


def test_powershell_gate_reports_bad_argument_combination_with_exit_one(
    tmp_path: Path,
) -> None:
    init_gate_fixture(tmp_path)

    completed = run_check(
        tmp_path, "-ListGates", "-AgentOnly", "-IncludeOptionalProduct"
    )
    payload = json.loads(completed.stdout)

    assert completed.returncode == 1
    assert payload["ok"] is False
    assert payload["overall"] == "bad_args"


def test_powershell_gate_reports_log_write_failure_with_exit_three(
    tmp_path: Path,
) -> None:
    init_gate_fixture(tmp_path)
    write(tmp_path / "blocked", "not a directory\n")

    completed = run_check(tmp_path, "-ListGates", "-LogFile", "blocked/check.log")
    payload = json.loads(completed.stdout)

    assert completed.returncode == 3
    assert payload["ok"] is False
    assert payload["overall"] == "external_failure"
    assert payload["log_file"] is None
    assert payload["artifacts"] == []


def test_active_executable_surfaces_do_not_invoke_deleted_bash_gate() -> None:
    deleted_gate = "".join(("verify", ".sh"))
    offenders = []
    for path in (AGENTS / "skills").rglob("*.py"):
        if ACTIVE_EXCLUDED_PARTS.intersection(path.relative_to(AGENTS).parts):
            continue
        if deleted_gate in path.read_text(encoding="utf-8"):
            offenders.append(path.relative_to(ROOT).as_posix())
    assert offenders == []

    simplify = (AGENTS / "skills/simplify/simplify_gate.py").read_text(
        encoding="utf-8"
    )
    bash_argv = "".join(("[\"", "bash", "\""))
    assert bash_argv not in simplify
    assert "check.ps1" in simplify


def test_simplify_gate_invokes_and_parses_canonical_powershell_json(
    tmp_path: Path, monkeypatch: Any
) -> None:
    simplify_gate = load_simplify_gate()
    captured: list[str] = []
    expected = {
        "ok": True,
        "overall": "pass",
        "tools": {},
        "log_file": ".agents/logs/check.log",
        "warnings": [],
        "artifacts": [".agents/logs/check.log"],
    }

    monkeypatch.setattr(simplify_gate.shutil, "which", lambda name: "powershell")

    def fake_run(command: list[str], **_: Any) -> subprocess.CompletedProcess[str]:
        captured.extend(command)
        return subprocess.CompletedProcess(
            command, 0, stdout=json.dumps(expected), stderr=""
        )

    monkeypatch.setattr(simplify_gate.subprocess, "run", fake_run)

    payload = simplify_gate.run_verify(
        tmp_path, allow_no_gates=False, allowed_product_paths=["FrameWeb/a", "tools/b"]
    )

    assert payload == expected
    assert captured[0] == "powershell"
    assert str(tmp_path / ".agents/check.ps1") in captured
    assert captured[captured.index("-AllowProductPath") + 1] == "FrameWeb/a;tools/b"


def test_repository_config_declares_existing_component_paths() -> None:
    data = tomllib.loads((AGENTS / "repository.toml").read_text(encoding="utf-8"))
    components = data["components"]
    for component in components:
        assert (ROOT / component["manifest"]).is_file()
        assert (ROOT / component["working_directory"]).is_dir()
        for project in component.get("projects", []):
            assert (ROOT / project).is_file()
    assert {gate["classification"] for gate in data["gates"]} == {"product"}
    assert all(gate["component"] in {component["id"] for component in components} for gate in data["gates"])


def test_python_product_gate_runs_from_the_declared_component_directory() -> None:
    data = tomllib.loads((AGENTS / "repository.toml").read_text(encoding="utf-8"))
    python_component = next(
        item for item in data["components"] if item["id"] == "python"
    )
    expected_prefix = [
        "uv",
        "--directory",
        "FrameWeb",
        "run",
        "--locked",
        "--extra",
        "dev",
        "python",
        "-m",
        "pytest",
    ]
    expected_gate = [*expected_prefix, "tests", "-q"]
    assert python_component["working_directory"] == "FrameWeb"
    assert python_component["commands"]["test_prefix"] == expected_prefix
    assert python_component["commands"]["test"] == expected_gate
    python_gate = next(gate for gate in data["gates"] if gate["id"] == "python-tests")
    assert python_gate["command"] == expected_gate
