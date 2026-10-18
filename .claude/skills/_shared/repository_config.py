"""Shared path validation for ``.agents/repository.toml`` consumers.

Repository declarations are data, not authority to read outside the checkout.
Every consumer validates the lexical path and its resolved target immediately
before use so absolute paths, ``..`` traversal, and symlink escapes have one
consistent failure mode.
"""

from __future__ import annotations

from pathlib import Path
from typing import Literal


PathKind = Literal["file", "directory", "any"]


class RepositoryPathError(ValueError):
    """A declared or derived path escaped the repository contract."""


def _resolved_inside(root: Path, candidate: Path, field: str) -> Path:
    root_resolved = root.resolve()
    try:
        resolved = candidate.resolve(strict=False)
    except (OSError, RuntimeError) as exc:
        raise RepositoryPathError(f"{field} cannot be resolved: {exc}") from exc
    try:
        resolved.relative_to(root_resolved)
    except ValueError as exc:
        raise RepositoryPathError(
            f"{field} must resolve inside the repository: {candidate}"
        ) from exc
    return resolved


def _require_kind(path: Path, field: str, kind: PathKind) -> None:
    if kind == "file" and not path.is_file():
        raise RepositoryPathError(f"{field} is not an existing file: {path}")
    if kind == "directory" and not path.is_dir():
        raise RepositoryPathError(f"{field} is not an existing directory: {path}")


def resolve_declared_path(
    root: Path,
    value: object,
    field: str,
    *,
    kind: PathKind = "any",
) -> tuple[str, Path]:
    """Validate one repository-relative declaration and return its safe target."""
    if not isinstance(value, str) or not value.strip():
        raise RepositoryPathError(f"{field} must be a non-empty relative path")
    candidate = Path(value)
    if candidate.is_absolute() or ".." in candidate.parts:
        raise RepositoryPathError(f"{field} must stay inside the repository: {value}")
    resolved = _resolved_inside(root, root.resolve() / candidate, field)
    _require_kind(resolved, field, kind)
    return candidate.as_posix(), resolved


def resolve_derived_path(
    root: Path,
    candidate: Path,
    field: str,
    *,
    kind: PathKind = "any",
) -> Path:
    """Confine a path derived from an already validated repository path."""
    resolved = _resolved_inside(root, candidate, field)
    _require_kind(resolved, field, kind)
    return resolved
