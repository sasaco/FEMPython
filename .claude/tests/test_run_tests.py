from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import os
import subprocess
import sys
from types import ModuleType
from typing import Any

import pytest


ROOT = Path(__file__).parents[2]
MODULE_PATH = ROOT / ".agents/skills/_shared/run_tests.py"


def load_module() -> ModuleType:
    spec = importlib.util.spec_from_file_location("agents_run_tests", MODULE_PATH)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


run_tests = load_module()


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def write_config(root: Path, commands: str) -> None:
    write(
        root / ".agents/repository.toml",
        f"""[[components]]
id = "python"
kind = "python"
manifest = "FrameWeb/pyproject.toml"
working_directory = "FrameWeb"
[components.commands]
{commands}
""",
    )


def run_cli(project_root: Path, target: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--project-root",
            str(project_root),
            "--runner",
            "python",
            "--target",
            target,
            "--expect",
            "pass",
        ],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )


def test_auto_runner_selects_declared_python_environment(
    tmp_path: Path, monkeypatch: Any
) -> None:
    write_config(
        tmp_path,
        'test_prefix = ["uv", "--directory", "FrameWeb", "run", "--locked", "--extra", "dev", "python", "-m", "pytest"]',
    )
    monkeypatch.setattr(run_tests.shutil, "which", lambda name: f"C:/bin/{name}")

    prefix, note = run_tests.resolve_runner(
        "auto", tmp_path, [".agents/tests/test_example.py"]
    )

    assert prefix == [
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
    assert note == "repository.toml:python"


def test_configured_runner_rebases_repo_targets_to_component_directory(
    tmp_path: Path,
) -> None:
    write_config(
        tmp_path,
        'test_prefix = ["uv", "--directory", "FrameWeb", "run", "--locked", "--extra", "dev", "python", "-m", "pytest"]',
    )
    write(tmp_path / "FrameWeb/tests/test_product.py", "def test_ok():\n    assert True\n")
    write(tmp_path / ".agents/tests/test_agent.py", "def test_ok():\n    assert True\n")

    targets = run_tests.targets_for_runner(
        [
            "FrameWeb/tests/test_product.py::test_ok",
            ".agents/tests/test_agent.py",
        ],
        tmp_path,
        "repository.toml:python",
    )

    assert targets == [
        "tests/test_product.py::test_ok",
        "../.agents/tests/test_agent.py",
    ]


def test_cli_rejects_absolute_and_traversal_targets_outside_repository(
    tmp_path: Path,
) -> None:
    outside = tmp_path.parent / f"{tmp_path.name}-outside.py"
    write(outside, "def test_outside():\n    assert True\n")
    relative_escape = os.path.relpath(outside, start=tmp_path)

    for target in (str(outside.resolve()), relative_escape):
        completed = run_cli(tmp_path, target)
        payload = json.loads(completed.stdout)
        assert completed.returncode == run_tests.EXIT_BAD_ARGS
        assert payload["ok"] is False
        assert "escapes project root" in payload["error"]


def test_cli_rejects_repository_symlink_to_external_target(tmp_path: Path) -> None:
    outside = tmp_path.parent / f"{tmp_path.name}-external-target.py"
    write(outside, "def test_outside():\n    assert True\n")
    link = tmp_path / "tests/test_link.py"
    link.parent.mkdir(parents=True)
    try:
        link.symlink_to(outside)
    except OSError as exc:
        pytest.skip(f"symlink creation is unavailable: {exc}")

    completed = run_cli(tmp_path, "tests/test_link.py::test_outside")
    payload = json.loads(completed.stdout)
    assert completed.returncode == run_tests.EXIT_BAD_ARGS
    assert payload["ok"] is False
    assert "escapes project root" in payload["error"]


def test_configured_repository_with_no_test_gate_is_not_silent(
    tmp_path: Path,
) -> None:
    write_config(tmp_path, 'test = ["uv", "run", "pytest"]')

    prefix, component, error, configured = run_tests.select_configured_prefix(
        ["FrameWeb/tests"], tmp_path, None
    )

    assert prefix is None
    assert component is None
    assert configured is True
    assert error == "no configured Python test gate (commands.test_prefix)"


def test_explicit_unknown_component_is_rejected(tmp_path: Path) -> None:
    write_config(tmp_path, 'test_prefix = ["uv", "run", "pytest"]')

    prefix, component, error, configured = run_tests.select_configured_prefix(
        ["FrameWeb/tests"], tmp_path, "missing"
    )

    assert prefix is None
    assert component is None
    assert configured is True
    assert "missing" in str(error)


def test_cli_reports_a_real_test_failure_as_contract_violation(
    tmp_path: Path,
) -> None:
    failing = tmp_path / "tests/test_failure.py"
    write(failing, "def test_failure():\n    assert False\n")

    completed = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--project-root",
            str(tmp_path),
            "--runner",
            "python",
            "--target",
            "tests/test_failure.py",
            "--expect",
            "pass",
        ],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )

    payload = json.loads(completed.stdout)
    assert completed.returncode == run_tests.EXIT_EXPECTATION_VIOLATED
    assert payload["ok"] is False
    assert payload["observed"] == "failed"


def test_cli_reports_no_configured_gate(tmp_path: Path) -> None:
    write_config(tmp_path, 'test = ["uv", "run", "pytest"]')
    write(tmp_path / "FrameWeb/tests/test_sample.py", "def test_ok():\n    assert True\n")

    completed = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--project-root",
            str(tmp_path),
            "--target",
            "FrameWeb/tests/test_sample.py",
            "--expect",
            "pass",
        ],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )

    payload = json.loads(completed.stdout)
    assert completed.returncode == run_tests.EXIT_EXPECTATION_VIOLATED
    assert payload["overall"] == "no_gates"
    assert payload["observed"] is None
