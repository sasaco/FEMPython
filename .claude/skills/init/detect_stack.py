#!/usr/bin/env python3
"""Detect declared or bounded-fallback repository components for ``/init``.

``.agents/repository.toml`` is preferred when present. Every declared manifest,
working directory, .NET project, command, and gate is validated before it is
reported. Repositories without the config use a depth-bounded scan with
generated, dependency, vendor, and submodule paths pruned.

Usage:
    python detect_stack.py [--project-root DIR]

Exit codes:
    0  stack configuration and the Codex bootstrap are valid
    1  bad arguments
    2  repository declarations or the Codex bootstrap are invalid
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tomllib
from pathlib import Path
from typing import Any, NoReturn

PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
CONFIG_PATH = Path(".agents/repository.toml")
SHARED_DIR = Path(__file__).resolve().parents[1] / "_shared"
if str(SHARED_DIR) not in sys.path:
    sys.path.insert(0, str(SHARED_DIR))

from repository_config import (  # noqa: E402
    RepositoryPathError,
    resolve_declared_path,
    resolve_derived_path,
)

EXIT_OK = 0
EXIT_BAD_ARGS = 1
EXIT_CONTRACT = 2

DEFAULT_MAX_DEPTH = 4
DEFAULT_EXCLUDED_DIRECTORIES = frozenset(
    {
        ".git",
        ".venv",
        "venv",
        "node_modules",
        "dist",
        "bin",
        "obj",
        "__pycache__",
        ".pytest_cache",
        ".ruff_cache",
        ".mypy_cache",
        ".tox",
        ".nox",
        ".cache",
        "vendor",
    }
)
DEFAULT_EXCLUDED_PATHS = (
    "FrameWebforJS/src/assets/js/rxfire",
    "FrameWebforJS/src/assets/js/paramquery",
    "tools/local-tools",
)
REQUIREMENT_NAME_RE = re.compile(r"^([A-Za-z0-9._-]+)")
WORKFLOW_SUFFIXES = frozenset({".yml", ".yaml"})

KIND_DEFAULTS: dict[str, tuple[str | None, str | None]] = {
    "python": ("python", "uv"),
    "angular": ("typescript", "npm"),
    "node": ("javascript", "npm"),
    "dotnet": ("csharp", "dotnet"),
    "smoke": (None, None),
}


def _emit(obj: dict[str, Any]) -> None:
    print(json.dumps(obj, ensure_ascii=False, indent=2))


class JsonArgumentParser(argparse.ArgumentParser):
    """Report argparse failures through the JSON/exit-1 contract."""

    def error(self, message: str) -> NoReturn:
        _emit({"ok": False, "error": message, "artifacts": []})
        raise SystemExit(EXIT_BAD_ARGS)


def _read_text(path: Path) -> tuple[str | None, str | None]:
    try:
        return path.read_text(encoding="utf-8"), None
    except OSError as exc:
        return None, f"unreadable: {exc}"
    except UnicodeDecodeError as exc:
        return None, f"not valid UTF-8: {exc}"


def _safe_relative(
    root: Path, value: object, field: str
) -> tuple[str | None, str | None]:
    try:
        relative, _ = resolve_declared_path(root, value, field)
    except RepositoryPathError as exc:
        return None, str(exc)
    return relative, None


def _command(value: object, field: str) -> tuple[list[str] | None, str | None]:
    if not isinstance(value, list) or not value:
        return None, f"{field} must be a non-empty string array"
    if not all(isinstance(item, str) and item for item in value):
        return None, f"{field} must contain only non-empty strings"
    return list(value), None


def _load_config(root: Path) -> tuple[dict[str, Any] | None, list[str]]:
    path = root / CONFIG_PATH
    if not path.is_file():
        return None, []
    text, error = _read_text(path)
    if text is None:
        return None, [f"{CONFIG_PATH.as_posix()}: {error}"]
    try:
        parsed = tomllib.loads(text)
    except tomllib.TOMLDecodeError as exc:
        return None, [f"{CONFIG_PATH.as_posix()}: invalid TOML: {exc}"]
    if not isinstance(parsed, dict):
        return None, [f"{CONFIG_PATH.as_posix()}: top level must be a table"]
    return parsed, []


def _normalize_component(root: Path, raw: object, index: int) -> dict[str, Any]:
    errors: list[str] = []
    if not isinstance(raw, dict):
        return {
            "id": f"component-{index}",
            "valid": False,
            "errors": ["component must be a table"],
        }

    component_id = raw.get("id")
    kind = raw.get("kind")
    if not isinstance(component_id, str) or not component_id.strip():
        errors.append("id must be a non-empty string")
        component_id = f"component-{index}"
    if kind not in KIND_DEFAULTS:
        errors.append(f"unsupported kind: {kind!r}")
        kind = str(kind or "unknown")

    manifest, error = _safe_relative(
        root, raw.get("manifest"), f"components[{index}].manifest"
    )
    if error:
        errors.append(error)
    elif manifest is not None and not (root / manifest).is_file():
        errors.append(f"declared manifest does not exist: {manifest}")

    working_directory, error = _safe_relative(
        root,
        raw.get("working_directory", "."),
        f"components[{index}].working_directory",
    )
    if error:
        errors.append(error)
    elif working_directory is not None and not (root / working_directory).is_dir():
        errors.append(
            f"declared working directory does not exist: {working_directory}"
        )

    projects: list[str] = []
    raw_projects = raw.get("projects", [])
    if not isinstance(raw_projects, list):
        errors.append("projects must be an array")
    else:
        for project_index, value in enumerate(raw_projects):
            project, project_error = _safe_relative(
                root, value, f"components[{index}].projects[{project_index}]"
            )
            if project_error:
                errors.append(project_error)
            elif project is not None:
                projects.append(project)
                if not (root / project).is_file():
                    errors.append(f"declared project does not exist: {project}")

    commands: dict[str, list[str]] = {}
    raw_commands = raw.get("commands", {})
    if not isinstance(raw_commands, dict):
        errors.append("commands must be a table")
    else:
        for name, value in raw_commands.items():
            command, command_error = _command(
                value, f"components[{index}].commands.{name}"
            )
            if command_error:
                errors.append(command_error)
            elif command is not None:
                commands[str(name)] = command

    default_language, default_manager = KIND_DEFAULTS.get(kind, (None, None))
    return {
        "id": component_id,
        "kind": kind,
        "language": raw.get("language", default_language),
        "package_manager": raw.get("package_manager", default_manager),
        "manifest": manifest,
        "working_directory": working_directory,
        "projects": projects,
        "commands": commands,
        "valid": not errors,
        "errors": errors,
    }


def _normalize_gates(
    raw_gates: object, component_ids: set[str]
) -> tuple[list[dict[str, Any]], list[str]]:
    if raw_gates is None:
        return [], []
    if not isinstance(raw_gates, list):
        return [], ["gates must be an array of tables"]
    gates: list[dict[str, Any]] = []
    errors: list[str] = []
    seen: set[str] = set()
    for index, raw in enumerate(raw_gates):
        if not isinstance(raw, dict):
            errors.append(f"gates[{index}] must be a table")
            continue
        gate_id = raw.get("id")
        component = raw.get("component")
        classification = raw.get("classification")
        optional = raw.get("optional")
        command, command_error = _command(
            raw.get("command"), f"gates[{index}].command"
        )
        gate_errors: list[str] = []
        if not isinstance(gate_id, str) or not gate_id:
            gate_errors.append("id must be a non-empty string")
            gate_id = f"gate-{index}"
        elif gate_id in seen:
            gate_errors.append(f"duplicate gate id: {gate_id}")
        seen.add(gate_id)
        if component not in component_ids:
            gate_errors.append(f"unknown component: {component!r}")
        if classification not in {"agent", "product"}:
            gate_errors.append("classification must be 'agent' or 'product'")
        if not isinstance(optional, bool):
            gate_errors.append("optional must be a boolean")
        if command_error:
            gate_errors.append(command_error)
        gate = {
            "id": gate_id,
            "component": component,
            "classification": classification,
            "optional": optional,
            "command": command or [],
            "valid": not gate_errors,
            "errors": gate_errors,
        }
        gates.append(gate)
        errors.extend(f"{gate_id}: {item}" for item in gate_errors)
    return gates, errors


def _declared_components(
    root: Path, config: dict[str, Any]
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], list[str]]:
    raw_components = config.get("components")
    if not isinstance(raw_components, list) or not raw_components:
        return [], [], [
            "repository.toml must declare at least one [[components]] entry"
        ]
    components = [
        _normalize_component(root, raw, index)
        for index, raw in enumerate(raw_components)
    ]
    ids = [str(component["id"]) for component in components]
    errors = [
        f"{component['id']}: {error}"
        for component in components
        for error in component["errors"]
    ]
    duplicates = sorted({item for item in ids if ids.count(item) > 1})
    errors.extend(f"duplicate component id: {item}" for item in duplicates)
    gates, gate_errors = _normalize_gates(config.get("gates"), set(ids))
    return components, gates, [*errors, *gate_errors]


def _config_discovery(
    config: dict[str, Any] | None,
) -> tuple[int, set[str], tuple[str, ...]]:
    discovery = config.get("discovery", {}) if isinstance(config, dict) else {}
    if not isinstance(discovery, dict):
        discovery = {}
    max_depth = discovery.get("max_depth", DEFAULT_MAX_DEPTH)
    if not isinstance(max_depth, int) or max_depth < 1:
        max_depth = DEFAULT_MAX_DEPTH
    raw_dirs = discovery.get("exclude_directories", [])
    directories = set(DEFAULT_EXCLUDED_DIRECTORIES)
    if isinstance(raw_dirs, list):
        directories.update(
            str(item) for item in raw_dirs if isinstance(item, str)
        )
    raw_paths = discovery.get("exclude_paths", [])
    paths = list(DEFAULT_EXCLUDED_PATHS)
    if isinstance(raw_paths, list):
        paths.extend(
            str(item).replace("\\", "/").rstrip("/")
            for item in raw_paths
            if isinstance(item, str)
        )
    return max_depth, directories, tuple(dict.fromkeys(paths))


def _submodule_paths(root: Path) -> tuple[str, ...]:
    text, _ = _read_text(root / ".gitmodules")
    if not text:
        return ()
    paths: list[str] = []
    for line in text.splitlines():
        key, separator, value = line.partition("=")
        if separator and key.strip() == "path" and value.strip():
            paths.append(value.strip().replace("\\", "/").rstrip("/"))
    return tuple(paths)


def _excluded(rel: str, excluded_paths: tuple[str, ...]) -> bool:
    normalized = rel.replace("\\", "/").strip("/").casefold()
    return any(
        normalized == item.casefold()
        or normalized.startswith(f"{item.casefold()}/")
        for item in excluded_paths
        if item
    )


def _bounded_files(
    root: Path,
    max_depth: int,
    excluded_directories: set[str],
    excluded_paths: tuple[str, ...],
) -> list[Path]:
    found: list[Path] = []
    excluded_names = {item.casefold() for item in excluded_directories}
    for current, directories, files in os.walk(root):
        current_path = Path(current)
        rel_dir = current_path.relative_to(root)
        depth = 0 if rel_dir == Path(".") else len(rel_dir.parts)
        kept: list[str] = []
        for directory in directories:
            child = current_path / directory
            child_rel = child.relative_to(root).as_posix()
            if (
                directory.casefold() in excluded_names
                or _excluded(child_rel, excluded_paths)
            ):
                continue
            if (child / ".git").is_file():
                continue
            kept.append(directory)
        directories[:] = kept if depth < max_depth else []
        if _excluded(rel_dir.as_posix(), excluded_paths):
            directories[:] = []
            continue
        for filename in files:
            path = current_path / filename
            rel = path.relative_to(root).as_posix()
            if _excluded(rel, excluded_paths):
                continue
            try:
                safe_path = resolve_derived_path(
                    root, path, f"fallback manifest candidate {rel}", kind="file"
                )
            except RepositoryPathError:
                continue
            found.append(safe_path)
    return found


def _package_kind(path: Path) -> str:
    text, _ = _read_text(path)
    if text is None:
        return "node"
    try:
        data = json.loads(text)
    except ValueError:
        return "node"
    if not isinstance(data, dict):
        return "node"
    dependencies: dict[str, object] = {}
    for key in ("dependencies", "devDependencies"):
        block = data.get(key)
        if isinstance(block, dict):
            dependencies.update(block)
    return "angular" if "@angular/core" in dependencies else "node"


def _fallback_components(
    root: Path, config: dict[str, Any] | None
) -> list[dict[str, Any]]:
    max_depth, excluded_directories, excluded_paths = _config_discovery(config)
    excluded_paths = (*excluded_paths, *_submodule_paths(root))
    files = _bounded_files(root, max_depth, excluded_directories, excluded_paths)
    pyprojects = sorted(path for path in files if path.name == "pyproject.toml")
    packages = sorted(path for path in files if path.name == "package.json")
    solutions = sorted(
        path for path in files if path.suffix.casefold() == ".sln"
    )
    csprojects = sorted(
        path for path in files if path.suffix.casefold() == ".csproj"
    )
    components: list[dict[str, Any]] = []

    def add(
        component_id: str,
        kind: str,
        manifest_path: Path,
        projects: list[Path] | None = None,
    ) -> None:
        language, manager = KIND_DEFAULTS[kind]
        rel = manifest_path.relative_to(root).as_posix()
        working = manifest_path.parent.relative_to(root).as_posix() or "."
        components.append(
            {
                "id": component_id,
                "kind": kind,
                "language": language,
                "package_manager": manager,
                "manifest": rel,
                "working_directory": working,
                "projects": [
                    path.relative_to(root).as_posix()
                    for path in (projects or [])
                ],
                "commands": {},
                "valid": True,
                "errors": [],
            }
        )

    for index, path in enumerate(pyprojects, 1):
        add(f"python-{index}", "python", path)
    for index, path in enumerate(packages, 1):
        kind = _package_kind(path)
        add(f"{kind}-{index}", kind, path)
    if solutions:
        for index, path in enumerate(solutions, 1):
            add(f"dotnet-{index}", "dotnet", path, csprojects)
    else:
        for index, path in enumerate(csprojects, 1):
            add(f"dotnet-{index}", "dotnet", path, [path])
    smoke = root / "scripts" / "smoke-local.py"
    if smoke in files:
        add("local-smoke", "smoke", smoke)
    return components


class Evidence:
    def __init__(self) -> None:
        self.tools: list[dict[str, str]] = []
        self.scripts: list[dict[str, str]] = []
        self.dependencies: list[dict[str, str]] = []
        self.parsed_manifests: list[str] = []
        self.unparsed_manifests: list[str] = []
        self.warnings: list[dict[str, str]] = []
        self.errors: list[dict[str, str]] = []

    def warn(self, manifest: str, error: str) -> None:
        self.warnings.append({"manifest": manifest, "error": error})

    def fail(self, manifest: str, error: str) -> None:
        self.errors.append({"manifest": manifest, "error": error})

    def as_dict(self, ci: list[dict[str, str]]) -> dict[str, Any]:
        return {
            "parsed_manifests": self.parsed_manifests,
            "unparsed_manifests": self.unparsed_manifests,
            "tools": self.tools,
            "scripts": self.scripts,
            "dependencies": self.dependencies,
            "errors": self.errors,
            "ci": ci,
        }


def _add_python_dependencies(
    evidence: Evidence, manifest: str, key: str, values: object
) -> None:
    if not isinstance(values, list):
        evidence.warn(manifest, f"{key} is not an array")
        return
    for value in values:
        if not isinstance(value, str):
            continue
        match = REQUIREMENT_NAME_RE.match(value.strip())
        if match:
            evidence.dependencies.append(
                {
                    "name": match.group(1),
                    "spec": value,
                    "manifest": manifest,
                    "key": key,
                }
            )


def _collect_pyproject(
    root: Path, component: dict[str, Any], evidence: Evidence
) -> None:
    manifest = str(component["manifest"])
    try:
        _, path = resolve_declared_path(
            root, manifest, f"component {component['id']} manifest", kind="file"
        )
    except RepositoryPathError as exc:
        evidence.fail(manifest, str(exc))
        return
    text, error = _read_text(path)
    if text is None:
        evidence.fail(manifest, error or "unreadable")
        return
    try:
        data = tomllib.loads(text)
    except tomllib.TOMLDecodeError as exc:
        evidence.fail(manifest, f"invalid TOML: {exc}")
        return
    evidence.parsed_manifests.append(manifest)
    tools = data.get("tool")
    if isinstance(tools, dict):
        for name in tools:
            evidence.tools.append(
                {"tool": name, "manifest": manifest, "key": f"tool.{name}"}
            )
    project = data.get("project")
    if isinstance(project, dict):
        _add_python_dependencies(
            evidence,
            manifest,
            "project.dependencies",
            project.get("dependencies", []),
        )
        optional = project.get("optional-dependencies")
        if isinstance(optional, dict):
            for group, values in optional.items():
                _add_python_dependencies(
                    evidence,
                    manifest,
                    f"project.optional-dependencies.{group}",
                    values,
                )


def _collect_package_json(
    root: Path, component: dict[str, Any], evidence: Evidence
) -> None:
    manifest = str(component["manifest"])
    try:
        _, path = resolve_declared_path(
            root, manifest, f"component {component['id']} manifest", kind="file"
        )
    except RepositoryPathError as exc:
        evidence.fail(manifest, str(exc))
        return
    text, error = _read_text(path)
    if text is None:
        evidence.fail(manifest, error or "unreadable")
        return
    try:
        data = json.loads(text)
    except ValueError as exc:
        evidence.fail(manifest, f"invalid JSON: {exc}")
        return
    if not isinstance(data, dict):
        evidence.fail(manifest, "top level is not an object")
        return
    evidence.parsed_manifests.append(manifest)
    scripts = data.get("scripts")
    if isinstance(scripts, dict):
        for name, command in scripts.items():
            if isinstance(command, str):
                evidence.scripts.append(
                    {
                        "name": name,
                        "command": command,
                        "manifest": manifest,
                        "key": f"scripts.{name}",
                    }
                )
    for key in ("dependencies", "devDependencies"):
        dependencies = data.get(key)
        if isinstance(dependencies, dict):
            for name, spec in sorted(dependencies.items()):
                evidence.dependencies.append(
                    {
                        "name": str(name),
                        "spec": spec if isinstance(spec, str) else "",
                        "manifest": manifest,
                        "key": f"{key}.{name}",
                    }
                )


def collect_evidence(
    root: Path, components: list[dict[str, Any]]
) -> Evidence:
    evidence = Evidence()
    for component in components:
        if not component.get("valid"):
            continue
        kind = component.get("kind")
        if kind == "python":
            _collect_pyproject(root, component, evidence)
        elif kind in {"angular", "node"}:
            _collect_package_json(root, component, evidence)
        else:
            manifest = component.get("manifest")
            if isinstance(manifest, str):
                evidence.unparsed_manifests.append(manifest)
    return evidence


def _tracked_files(root: Path, pathspec: str) -> list[str]:
    try:
        completed = subprocess.run(
            ["git", "ls-files", "-z", "--", pathspec],
            cwd=root,
            capture_output=True,
            timeout=30,
        )
    except (OSError, subprocess.SubprocessError):
        return []
    if completed.returncode != 0:
        return []
    return [
        item.decode("utf-8", errors="replace")
        for item in completed.stdout.split(b"\0")
        if item
    ]


def detect_ci(root: Path) -> list[dict[str, str]]:
    found: list[dict[str, str]] = []
    workflows = [
        path
        for path in _tracked_files(root, ".github/workflows")
        if Path(path).suffix.casefold() in WORKFLOW_SUFFIXES
        and (root / path).is_file()
    ]
    for path in workflows:
        found.append({"system": "github-actions", "path": path})
    for system, pathspec in (
        ("gitlab-ci", ".gitlab-ci.yml"),
        ("circleci", ".circleci"),
    ):
        for path in _tracked_files(root, pathspec):
            if (root / path).is_file():
                found.append({"system": system, "path": path})
    return found


def detect_agent_bootstrap(root: Path) -> tuple[dict[str, bool], list[str]]:
    agents_text, _ = _read_text(root / "AGENTS.md")
    state_text, _ = _read_text(root / ".agents" / "STATE.md")
    status = {
        "agents_md": bool(agents_text and agents_text.strip()),
        "state_md": bool(
            state_text and "# Agent State" in state_text and state_text.strip()
        ),
    }
    return status, [name for name, valid in status.items() if not valid]


def _ordered_unique(values: list[str | None]) -> list[str]:
    return list(
        dict.fromkeys(
            value for value in values if isinstance(value, str) and value
        )
    )


def build_report(root: Path) -> tuple[dict[str, Any], list[str]]:
    config_path = root / CONFIG_PATH
    config, load_errors = _load_config(root)
    gates: list[dict[str, Any]] = []
    configuration_errors = list(load_errors)
    if config_path.is_file():
        if config is None:
            components: list[dict[str, Any]] = []
        else:
            components, gates, declaration_errors = _declared_components(
                root, config
            )
            configuration_errors.extend(declaration_errors)
        source = CONFIG_PATH.as_posix()
    else:
        components = _fallback_components(root, None)
        source = "bounded-fallback"

    evidence = collect_evidence(root, components)
    ci = detect_ci(root)
    bootstrap, bootstrap_failed = detect_agent_bootstrap(root)
    failed = list(bootstrap_failed)
    if configuration_errors:
        failed.append("repository_config")
    if evidence.errors:
        failed.append("manifest_evidence")
    valid_components = [
        component for component in components if component.get("valid")
    ]
    report: dict[str, Any] = {
        "ok": not failed,
        "source": source,
        "configuration": {
            "present": config_path.is_file(),
            "path": CONFIG_PATH.as_posix() if config_path.is_file() else None,
            "valid": not configuration_errors,
            "errors": configuration_errors,
        },
        "stack_families": _ordered_unique(
            [
                ".net"
                if component.get("kind") == "dotnet"
                else component.get("kind")
                for component in valid_components
                if component.get("kind") != "smoke"
            ]
        ),
        "languages": _ordered_unique(
            [component.get("language") for component in valid_components]
        ),
        "package_managers": _ordered_unique(
            [
                component.get("package_manager")
                for component in valid_components
            ]
        ),
        "components": components,
        "gates": gates,
        "manifests": {
            str(component.get("manifest")): bool(component.get("valid"))
            for component in components
            if component.get("manifest")
        },
        "evidence": evidence.as_dict(ci),
        "agent_bootstrap": bootstrap,
        "warnings": evidence.warnings,
        "artifacts": [],
    }
    if failed:
        report["error"] = "invalid contract marker(s): " + ", ".join(failed)
    return report, failed


def main() -> int:
    parser = JsonArgumentParser(
        description="Detect repository components and Codex bootstrap"
    )
    parser.add_argument("--project-root", type=Path, default=PROJECT_ROOT)
    args = parser.parse_args()
    if not args.project_root.is_dir():
        _emit(
            {
                "ok": False,
                "error": f"--project-root is not a directory: {args.project_root}",
                "artifacts": [],
            }
        )
        return EXIT_BAD_ARGS
    report, failed = build_report(args.project_root.resolve())
    _emit(report)
    return EXIT_CONTRACT if failed else EXIT_OK


if __name__ == "__main__":
    raise SystemExit(main())
