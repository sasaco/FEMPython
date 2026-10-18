from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import subprocess
import sys
from types import ModuleType

import pytest


ROOT = Path(__file__).parents[2]
MODULE_PATH = ROOT / ".agents/skills/init/detect_stack.py"


def load_module() -> ModuleType:
    spec = importlib.util.spec_from_file_location("agents_detect_stack", MODULE_PATH)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


detect_stack = load_module()


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def bootstrap(root: Path) -> None:
    write(root / "AGENTS.md", "# Agent instructions\n")
    write(root / ".agents/STATE.md", "# Agent State\n\n## Main Agent\n\nCodex\n")


def configured_fixture(root: Path) -> None:
    bootstrap(root)
    write(root / "FrameWeb/pyproject.toml", "[project]\nname='fixture'\n")
    write(
        root / "FrameWebforJS/package.json",
        json.dumps({"dependencies": {"@angular/core": "15.2.8"}}),
    )
    write(root / "FrameWeb.sln", "Microsoft Visual Studio Solution File\n")
    write(root / "tools/App/App.csproj", "<Project />\n")
    write(root / "scripts/smoke-local.py", "print('smoke')\n")
    write(
        root / ".agents/repository.toml",
        """schema_version = 1
[[components]]
id = "python"
kind = "python"
manifest = "FrameWeb/pyproject.toml"
working_directory = "FrameWeb"
[components.commands]
test_prefix = ["uv", "--directory", "FrameWeb", "run", "--locked", "--extra", "dev", "python", "-m", "pytest"]
test = ["uv", "--directory", "FrameWeb", "run", "--locked", "--extra", "dev", "python", "-m", "pytest", "tests", "-q"]
[[components]]
id = "angular"
kind = "angular"
manifest = "FrameWebforJS/package.json"
working_directory = "FrameWebforJS"
[[components]]
id = "dotnet"
kind = "dotnet"
manifest = "FrameWeb.sln"
working_directory = "."
projects = ["tools/App/App.csproj"]
[[components]]
id = "local-smoke"
kind = "smoke"
manifest = "scripts/smoke-local.py"
working_directory = "."
[[gates]]
id = "python-tests"
component = "python"
classification = "product"
optional = false
command = ["uv", "--directory", "FrameWeb", "run", "--locked", "--extra", "dev", "python", "-m", "pytest", "tests", "-q"]
""",
    )


def test_declared_components_are_reported_independently(tmp_path: Path) -> None:
    configured_fixture(tmp_path)

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == []
    assert report["source"] == ".agents/repository.toml"
    assert report["stack_families"] == ["python", "angular", ".net"]
    assert report["package_managers"] == ["uv", "npm", "dotnet"]
    assert {item["id"] for item in report["components"]} == {
        "python",
        "angular",
        "dotnet",
        "local-smoke",
    }
    assert report["gates"][0]["classification"] == "product"
    python_component = next(
        item for item in report["components"] if item["id"] == "python"
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
    assert python_component["working_directory"] == "FrameWeb"
    assert python_component["commands"]["test_prefix"] == expected_prefix
    assert python_component["commands"]["test"] == [*expected_prefix, "tests", "-q"]
    assert report["gates"][0]["command"] == [*expected_prefix, "tests", "-q"]


def test_present_malformed_config_does_not_fall_back(tmp_path: Path) -> None:
    bootstrap(tmp_path)
    write(tmp_path / "nested/pyproject.toml", "[project]\nname='ignored'\n")
    write(tmp_path / ".agents/repository.toml", "[[components]\n")

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == ["repository_config"]
    assert report["source"] == ".agents/repository.toml"
    assert report["components"] == []
    assert "invalid TOML" in report["configuration"]["errors"][0]


def test_missing_declared_manifest_is_an_error_without_fallback(tmp_path: Path) -> None:
    bootstrap(tmp_path)
    write(tmp_path / "other/pyproject.toml", "[project]\nname='not-declared'\n")
    write(
        tmp_path / ".agents/repository.toml",
        """[[components]]
id = "python"
kind = "python"
manifest = "missing/pyproject.toml"
working_directory = "."
""",
    )

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == ["repository_config"]
    assert report["components"][0]["valid"] is False
    assert "declared manifest does not exist" in report["configuration"]["errors"][0]


def test_bounded_fallback_prunes_generated_vendor_and_explicit_paths(
    tmp_path: Path,
) -> None:
    bootstrap(tmp_path)
    write(tmp_path / "backend/pyproject.toml", "[project]\nname='backend'\n")
    write(
        tmp_path / "frontend/package.json",
        json.dumps({"dependencies": {"@angular/core": "15"}}),
    )
    write(tmp_path / "FrameWeb.sln", "solution\n")
    for excluded in (
        ".git/pyproject.toml",
        ".venv/package.json",
        "venv/pyproject.toml",
        "node_modules/pkg/package.json",
        "dist/package.json",
        "bin/package.json",
        "obj/pkg/pyproject.toml",
        "__pycache__/package.json",
        ".pytest_cache/package.json",
        ".ruff_cache/package.json",
        ".mypy_cache/package.json",
        ".tox/package.json",
        ".nox/package.json",
        ".cache/package.json",
        "vendor/pkg/pyproject.toml",
        "FrameWebforJS/src/assets/js/rxfire/package.json",
        "FrameWebforJS/src/assets/js/paramquery/package.json",
        "tools/local-tools/package.json",
        "too/deep/for/the/scan/package.json",
    ):
        write(tmp_path / excluded, "{}\n")
    write(tmp_path / "at/depth/four/ok/pyproject.toml", "[project]\nname='deep'\n")
    write(tmp_path / ".gitmodules", "[submodule \"fixture\"]\npath = deps/submodule\n")
    write(tmp_path / "deps/submodule/package.json", "{}\n")
    write(tmp_path / "embedded/.git", "gitdir: elsewhere\n")
    write(tmp_path / "embedded/package.json", "{}\n")

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == []
    manifests = {item["manifest"] for item in report["components"]}
    assert "backend/pyproject.toml" in manifests
    assert "frontend/package.json" in manifests
    assert "FrameWeb.sln" in manifests
    assert "at/depth/four/ok/pyproject.toml" in manifests
    for excluded_fragment in (
        ".git/",
        ".venv/",
        "venv/",
        "node_modules/",
        "dist/",
        "bin/",
        "obj/",
        "__pycache__/",
        ".pytest_cache/",
        ".ruff_cache/",
        ".mypy_cache/",
        ".tox/",
        ".nox/",
        ".cache/",
        "vendor/",
        "rxfire/",
        "paramquery/",
        "local-tools/",
        "too/deep/for/the/scan/",
        "deps/submodule/",
        "embedded/",
    ):
        assert all(excluded_fragment not in item for item in manifests)


@pytest.mark.parametrize(
    ("manifest_text", "expected_error"),
    (("[project\n", "invalid TOML"),),
)
def test_malformed_declared_pyproject_is_a_contract_failure(
    tmp_path: Path, manifest_text: str, expected_error: str
) -> None:
    configured_fixture(tmp_path)
    write(tmp_path / "FrameWeb/pyproject.toml", manifest_text)

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == ["manifest_evidence"]
    assert report["ok"] is False
    assert expected_error in report["evidence"]["errors"][0]["error"]
    assert report["warnings"] == []


def test_malformed_declared_manifest_makes_cli_nonzero(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
) -> None:
    configured_fixture(tmp_path)
    write(tmp_path / "FrameWeb/pyproject.toml", "[project\n")
    monkeypatch.setattr(
        sys,
        "argv",
        ["detect_stack.py", "--project-root", str(tmp_path)],
    )

    exit_code = detect_stack.main()
    output = json.loads(capsys.readouterr().out)

    assert exit_code == detect_stack.EXIT_CONTRACT
    assert output["ok"] is False
    assert "invalid TOML" in output["evidence"]["errors"][0]["error"]


@pytest.mark.parametrize(
    ("manifest_text", "expected_error"),
    (("{", "invalid JSON"), ("[]", "top level is not an object")),
)
def test_malformed_declared_package_json_is_a_contract_failure(
    tmp_path: Path, manifest_text: str, expected_error: str
) -> None:
    configured_fixture(tmp_path)
    write(tmp_path / "FrameWebforJS/package.json", manifest_text)

    report, failed = detect_stack.build_report(tmp_path)

    assert failed == ["manifest_evidence"]
    assert report["ok"] is False
    assert expected_error in report["evidence"]["errors"][0]["error"]
    assert report["warnings"] == []


@pytest.mark.parametrize("value", ("../outside/pyproject.toml",))
def test_declared_manifest_traversal_is_rejected(tmp_path: Path, value: str) -> None:
    root = tmp_path / "repo"
    bootstrap(root)
    write(tmp_path / "outside/pyproject.toml", "[project]\nname='outside'\n")
    write(
        root / ".agents/repository.toml",
        f'''[[components]]
id = "python"
kind = "python"
manifest = "{value}"
working_directory = "."
''',
    )

    report, failed = detect_stack.build_report(root)

    assert failed == ["repository_config"]
    assert "must stay inside the repository" in report["configuration"]["errors"][0]


def test_declared_absolute_working_directory_and_project_are_rejected(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    outside = tmp_path / "outside"
    bootstrap(root)
    write(root / "FrameWeb.sln", "solution\n")
    write(outside / "App.csproj", "<Project />\n")
    write(
        root / ".agents/repository.toml",
        f'''[[components]]
id = "dotnet"
kind = "dotnet"
manifest = "FrameWeb.sln"
working_directory = "{outside.as_posix()}"
projects = ["../outside/App.csproj"]
''',
    )

    report, failed = detect_stack.build_report(root)

    assert failed == ["repository_config"]
    errors = "\n".join(report["configuration"]["errors"])
    assert "working_directory must stay inside the repository" in errors
    assert "projects[0] must stay inside the repository" in errors


def test_declared_manifest_symlink_escape_is_rejected_when_supported(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    outside = tmp_path / "outside/pyproject.toml"
    bootstrap(root)
    write(outside, "[project]\nname='outside'\n")
    link = root / "linked/pyproject.toml"
    link.parent.mkdir(parents=True)
    try:
        link.symlink_to(outside)
    except (OSError, NotImplementedError) as exc:
        pytest.skip(f"symlink creation is unavailable: {exc}")
    write(
        root / ".agents/repository.toml",
        '''[[components]]
id = "python"
kind = "python"
manifest = "linked/pyproject.toml"
working_directory = "."
''',
    )

    report, failed = detect_stack.build_report(root)

    assert failed == ["repository_config"]
    assert "must resolve inside the repository" in report["configuration"]["errors"][0]


def test_github_actions_requires_a_tracked_workflow_file(tmp_path: Path) -> None:
    bootstrap(tmp_path)
    (tmp_path / ".github/workflows").mkdir(parents=True)
    subprocess.run(["git", "init"], cwd=tmp_path, check=True, capture_output=True)

    assert detect_stack.detect_ci(tmp_path) == []

    workflow = tmp_path / ".github/workflows/check.yml"
    write(workflow, "name: check\n")
    assert detect_stack.detect_ci(tmp_path) == []

    subprocess.run(
        ["git", "add", ".github/workflows/check.yml"],
        cwd=tmp_path,
        check=True,
        capture_output=True,
    )
    assert detect_stack.detect_ci(tmp_path) == [
        {"system": "github-actions", "path": ".github/workflows/check.yml"}
    ]


def test_bootstrap_requires_only_nonempty_codex_files(tmp_path: Path) -> None:
    bootstrap(tmp_path)

    status, failed = detect_stack.detect_agent_bootstrap(tmp_path)

    assert status == {"agents_md": True, "state_md": True}
    assert failed == []
