from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
from types import ModuleType

import pytest


ROOT = Path(__file__).parents[2]


def load_module(name: str, relative: str) -> ModuleType:
    path = ROOT / relative
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


repository_config = load_module(
    "agents_repository_config", ".agents/skills/_shared/repository_config.py"
)
catchup = load_module(
    "agents_collect_repo_state", ".agents/skills/catchup/collect_repo_state.py"
)
inventory = load_module(
    "agents_lib_inventory", ".agents/skills/update-lib-docs/lib_inventory.py"
)
checkpoint = load_module(
    "agents_checkpoint", ".agents/skills/checkpointing/checkpoint.py"
)


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def config_with_manifest(manifest: str, working_directory: str = ".") -> str:
    return f'''[[components]]
id = "python"
kind = "python"
manifest = "{manifest}"
working_directory = "{working_directory}"
'''


@pytest.mark.parametrize("value", ("../outside.json",))
def test_shared_validator_rejects_traversal(tmp_path: Path, value: str) -> None:
    with pytest.raises(repository_config.RepositoryPathError, match="stay inside"):
        repository_config.resolve_declared_path(tmp_path, value, "manifest")


def test_shared_validator_rejects_absolute_path(tmp_path: Path) -> None:
    outside = (tmp_path.parent / "outside.json").resolve()
    with pytest.raises(repository_config.RepositoryPathError, match="stay inside"):
        repository_config.resolve_declared_path(
            tmp_path, str(outside), "manifest"
        )


def test_catchup_and_inventory_reject_config_traversal_before_read(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    outside = tmp_path / "outside/pyproject.toml"
    write(outside, "[project]\nname='outside'\n")
    write(
        root / ".agents/repository.toml",
        config_with_manifest("../outside/pyproject.toml", "../outside"),
    )

    components, _gates, catchup_errors = catchup.repository_components(root)
    candidates, inventory_errors = inventory._manifest_candidates(root)

    assert components == []
    assert candidates == []
    assert any("must stay inside the repository" in error for error in catchup_errors)
    assert any(
        "must stay inside the repository" in item["error"]
        for item in inventory_errors
    )


def test_all_consumers_reject_manifest_symlink_escape_when_supported(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    outside = tmp_path / "outside/pyproject.toml"
    write(outside, "[project]\nname='outside'\n")
    link = root / "linked/pyproject.toml"
    link.parent.mkdir(parents=True)
    try:
        link.symlink_to(outside)
    except (OSError, NotImplementedError) as exc:
        pytest.skip(f"symlink creation is unavailable: {exc}")
    write(
        root / ".agents/repository.toml",
        config_with_manifest("linked/pyproject.toml"),
    )

    detector = load_module(
        "agents_detect_stack_security",
        ".agents/skills/init/detect_stack.py",
    )
    write(root / "AGENTS.md", "# Agents\n")
    write(root / ".agents/STATE.md", "# Agent State\n")
    report, failed = detector.build_report(root)
    components, _gates, catchup_errors = catchup.repository_components(root)
    candidates, inventory_errors = inventory._manifest_candidates(root)

    assert failed == ["repository_config"]
    assert "must resolve inside the repository" in report["configuration"]["errors"][0]
    assert components == []
    assert any("must resolve inside the repository" in error for error in catchup_errors)
    assert candidates == []
    assert any(
        "must resolve inside the repository" in item["error"]
        for item in inventory_errors
    )


def test_inventory_rejects_derived_lockfile_symlink_escape_when_supported(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    write(root / "backend/pyproject.toml", "[project]\nname='fixture'\n")
    write(
        root / ".agents/repository.toml",
        config_with_manifest("backend/pyproject.toml", "backend"),
    )
    outside_lock = tmp_path / "outside/uv.lock"
    write(outside_lock, '[[package]]\nname = "private"\nversion = "1.0"\n')
    link = root / "backend/uv.lock"
    try:
        link.symlink_to(outside_lock)
    except (OSError, NotImplementedError) as exc:
        pytest.skip(f"symlink creation is unavailable: {exc}")

    result = inventory.collect_dependencies(root)

    assert any(
        "derived lockfile" in item["error"]
        and "must resolve inside the repository" in item["error"]
        for item in result["manifest_errors"]
    )
    assert "private" not in result["locked"]


def test_inventory_reports_valid_sources_relative_to_repository(
    tmp_path: Path,
) -> None:
    root = tmp_path / "repo"
    write(
        root / "backend/pyproject.toml",
        "[project]\nname='fixture'\ndependencies=['httpx>=0.27']\n",
    )
    write(
        root / ".agents/repository.toml",
        config_with_manifest("backend/pyproject.toml", "backend"),
    )

    result = inventory.collect_dependencies(root)

    assert result["manifest_errors"] == []
    assert result["sources"][0]["file"] == "backend/pyproject.toml"
    assert result["resolution"]["httpx"]["declared_in"].startswith(
        "backend/pyproject.toml "
    )


def test_external_agent_history_is_opt_in_and_in_repo_logs_remain_default(
    tmp_path: Path,
) -> None:
    catchup_args = catchup._build_parser().parse_args([])
    checkpoint_args = checkpoint._build_parser().parse_args([])
    assert catchup_args.external_agent_history is None
    assert checkpoint_args.external_agent_history is None
    assert not hasattr(catchup_args, "claude_home")
    assert not hasattr(checkpoint_args, "claude_home")

    log = tmp_path / ".agents/logs/agent-teams/team/reviewer.md"
    write(log, "# Work Log\n")
    catchup_data = catchup.collect_agent_teams(tmp_path, None)
    collected = checkpoint.Collected()
    checkpoint_logs = checkpoint.collect_work_logs(tmp_path, collected)

    assert catchup_data["sessions"] == []
    assert catchup_data["work_logs"][0]["file"].endswith("reviewer.md")
    assert checkpoint_logs["team"][0]["file"].endswith("reviewer.md")


def test_explicit_external_agent_history_is_runtime_neutral(tmp_path: Path) -> None:
    root = tmp_path / "repo"
    history = tmp_path / "external-history"
    write(history / "teams/team/config.json", '{"members": ["reviewer"]}\n')
    write(history / "tasks/team/1.json", '{"status": "completed"}\n')

    catchup_data = catchup.collect_agent_teams(root, history)
    collected = checkpoint.Collected()
    checkpoint_data = checkpoint.collect_agent_teams_data(history, collected)

    assert catchup_data["sessions"][0]["tasks_completed"] == 1
    assert checkpoint_data[0]["tasks"][0]["status"] == "completed"
