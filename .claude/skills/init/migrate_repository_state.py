#!/usr/bin/env python3
"""Migrate copied repository state documents to FrameWeb3 safely.

The migration is intentionally narrow.  It removes only the known copied
STATE blocks and known foreign DESIGN rows, preserves unrelated working-state
blocks, validates both candidates before writing either target, and guards the
two-file transaction with independent SHA-256 values, an inter-process lock,
commit-time byte checks, atomic replaces, and recoverable backups.
"""

from __future__ import annotations

import argparse
import contextlib
import hashlib
import json
import os
import re
import sys
import tempfile
import time
from collections.abc import Callable
from contextlib import contextmanager
from dataclasses import dataclass
from pathlib import Path
from typing import BinaryIO, Iterator, NoReturn


PROJECT_ROOT = Path(__file__).resolve().parents[3]
DEFAULT_STATE = PROJECT_ROOT / ".agents" / "STATE.md"
DEFAULT_DESIGN = PROJECT_ROOT / ".agents" / "docs" / "DESIGN.md"

H2_RE = re.compile(r"(?m)^##[ \t]+([^\n]+)$")
FOREIGN_RE = re.compile(
    r"tick.?replay|duckdb|minute-context|daily-context|stocks_daily|sma25|sma200|src/tickreplay",
    re.IGNORECASE,
)

FOREIGN_STATE_HEADINGS = {
    "Current Feature: Minute chart interaction and historical loading",
    "Current Feature: Order-book continuous scrolling and price formatting",
    "Current Bug Fix: local-authoritative-cache-download",
    "Current Feature: Daily chart and moving averages",
    "Current Bug Fix: daily-mode-pauses-replay",
    "Current Bug Fix: daily-mode-stops-tick-chart-and-tape",
    "Current Feature: Daily chart history paging",
    "Current Bug Fix: stock-daily-285a-case-mismatch",
}

STATE_IDENTITY = """FrameWeb3 is a Windows-oriented monorepo for web-based structural frame analysis.

- `FrameWeb/`: Python FEM engine and Flask/functions-framework HTTP API, managed with `uv`.
- `FrameWebforJS/`: Angular 15 browser/Electron client, managed with npm.
- `tools/FrameWeb.Startup/`: .NET 8 local launcher and print HTTP host.
- `FramePrintPDF/`: .NET printing and PDF projects; `FrameGConverter/` is a separate conversion utility.
- `FrameWeb.sln`: Visual Studio solution joining the local launcher, frontend, and printing projects.

Windows PowerShell is the canonical development shell. Repository-wide requirements and durable decisions live in [docs/DESIGN.md](docs/DESIGN.md)."""

MIGRATION_MARKER = "<!-- frameweb3:repository-state-migration:v1 -->"
CANONICAL_STATE_MARKER = "FrameWeb3 is a Windows-oriented monorepo"
CANONICAL_DESIGN_ANCHORS = (
    "FrameWeb3",
    "`FrameWeb/`",
    "`FrameWebforJS/`",
    "`tools/FrameWeb.Startup/`",
    "`FramePrintPDF/`",
)
DESIGN_PLACEHOLDER_RE = re.compile(
    r"not initialized|run [`/]?init|todo: describe|placeholder",
    re.IGNORECASE,
)

DESIGN_PREAMBLE = f"""# Design Document — 要件定義書 (Requirements & Macro Design)

{MIGRATION_MARKER}

> **Role:** Macro-level requirements and design — *what* this project builds and *why*.
> Kept current by `/init`, `/design-tracker`, and `/checkpointing`.
>
> **Document map:** Shared rules → [rules/](../rules/) ·
> Shared bootstrap → [AGENTS.md](../../AGENTS.md) · State → [STATE.md](../STATE.md) ·
> Micro work progress (latest 5 checkpoints) → [PROGRESS.md](../../PROGRESS.md)
"""

DESIGN_SECTION_BODIES: dict[str, str] = {
    "背景・目的 (Background & Purpose)": """FrameWeb3 provides web-based structural frame analysis for engineers who need to edit models, run finite-element calculations, inspect results, and produce print/PDF output from one repository. It combines a Python analysis service, an Angular client, and .NET local-startup and printing hosts while retaining explicit boundaries between their public contracts.

The repository is developed primarily on Windows. PowerShell, the checked-in dependency locks, and the component-specific commands are the canonical reproducible workflow.""",
    "スコープ (Scope)": """### In Scope

- Python frame finite-element analysis and its HTTP/functions-framework boundary in `FrameWeb/`.
- Angular browser/Electron model editing, calculation requests, and result presentation in `FrameWebforJS/`.
- Local orchestration and the print HTTP host in `tools/FrameWeb.Startup/`.
- Azure/local printing and PDF generation in `FramePrintPDF/`.
- The standalone `FrameGConverter/` conversion utility and the shared Visual Studio solution/build entry points.

### Out of Scope

- Production identity-provider configuration, cloud secrets, and deployment credentials.
- Silent changes to published analysis or printing response formats.
- Treating generated dependencies, build output, or vendored frontend assets as repository components.""",
    "機能要件 (Functional Requirements)": """| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-FRAMEWEB-1 | Accept supported structural-model input, run frame analysis, and return displacement, reaction, and element-result data. | High | The default modern response remains the documented flat representation. |
| FR-FRAMEWEB-2 | Let the Angular client edit models, request calculation, and display calculation results and errors. | High | Client and server must negotiate any compatibility representation explicitly. |
| FR-FRAMEWEB-3 | Start the analysis API, Angular development server, and local print host through the .NET startup project. | High | Visual Studio F5 and `dotnet run --project tools/FrameWeb.Startup` are supported local entry points. |
| FR-FRAMEWEB-4 | Produce print/PDF output through the existing .NET printing handlers without conflating their transport contract with the calculation API. | High | Azure deployment continues to use the dedicated printing project. |""",
    "非機能要件 (Non-Functional Requirements)": """| Category | Requirement | Metric / Target |
|----------|-------------|-----------------|
| Compatibility | Preserve the default analysis response and version any legacy-client representation explicitly. | Existing unversioned contract tests remain green; incompatible schemas fail visibly in the client. |
| Reproducibility | Use the committed Python, npm, and .NET project metadata and locks from their component directories. | Canonical commands run without relying on a root-level Python or npm project. |
| Security | Decode untrusted compressed calculation input without evaluating code and keep local secrets out of tracked files. | No `eval`-style decoder; local environment files remain untracked. |
| Maintainability | Keep analysis, frontend, startup, printing, and conversion responsibilities independently testable. | Component-specific gates report their working directory and failing command. |
| Platform | Keep the supported local workflow executable from Windows PowerShell. | Bootstrap, setup, and verification paths avoid requiring WSL or Bash. |""",
    "アーキテクチャ (Architecture)": """The repository is a component-oriented monorepo:

1. `FrameWebforJS/` owns browser/Electron interaction, model editing, calculation requests, and result rendering.
2. `FrameWeb/` owns input validation, finite-element model construction and solving, and the calculation HTTP response.
3. `tools/FrameWeb.Startup/` prepares the local toolchain, launches the Angular and Python children, exposes readiness, and hosts local printing.
4. `FramePrintPDF/` owns the existing C# print/PDF handlers and deployment project.
5. `FrameGConverter/` remains a separate conversion utility rather than part of the local web startup lifecycle.

The calculation and printing transports are separate contracts. Representation adapters belong at an explicit service boundary; frontend workers must reject missing required result fields instead of converting them into an empty success.""",
    "技術選定 (Tech Stack & Rationale)": """| Area | Technology | Rationale | Alternatives Considered |
|------|------------|-----------|-------------------------|
| Analysis service | Python 3.11+; NumPy, SciPy, Flask, functions-framework; `uv` | Matches the finite-element implementation and supplies a locked component environment. | A root-level Python environment or rewriting the solver in the host process. |
| Web client | Angular 15, TypeScript 4.9, npm/Node 18 | Existing browser/Electron application and UI ecosystem. | Replacing the client framework during repository alignment. |
| Local orchestration | .NET 8 `FrameWeb.Startup` | Provides one Visual Studio/CLI entry point, readiness, child-process management, and local printing. | Requiring developers to start every service manually. |
| Printing | Existing .NET projects in `FramePrintPDF/` | Preserves the established C# print and Azure deployment boundary. | Reusing the calculation transport or moving printing into Python. |
| Developer shell | Windows PowerShell | Matches the supported setup scripts, Visual Studio workflow, and current environment. | Requiring WSL/Bash for repository administration. |""",
    "制約 (Constraints)": """- Run Python repository tooling through `uv run --project FrameWeb --locked --extra dev python ...`; bare `python` is not assumed to be on `PATH`.
- The local setup targets Python 3.12 and Node 18/npm 9 while respecting the broader versions declared by each component.
- Do not change the default calculation result contract merely to satisfy the Angular legacy-case consumer; compatibility is explicit and versioned.
- Do not share calculation encoders with the C# printing APIs unless both wire contracts are proven equivalent.
- Generated directories (`.venv`, `node_modules`, `dist`, `bin`, `obj`, caches) and vendored frontend assets are not source components.
- Local environment/authentication files may contain machine-specific values and must not be overwritten or committed by bootstrap automation.""",
    "Key Decisions": """| Decision | Rationale | Alternatives Considered | Date |
|----------|-----------|------------------------|------|
| Use Codex as the main repository agent and Windows PowerShell as the canonical administration path. | This matches the active runtime and the repository's supported local development environment. | Preserve copied Claude-first and Bash-first bootstrap assumptions. | 2026-09-18 |
| Keep Python, Angular, startup, printing, and conversion as explicit component boundaries in one monorepo. | Each component has a different toolchain and public contract; explicit boundaries make setup and validation reproducible. | Treat the root as one Python project or collapse services into the startup host. | 2026-09-18 |
| Preserve the default flat analysis response and require explicit negotiation for compatibility representations. | Existing modern clients and documentation depend on the default, while the Angular legacy consumer needs an ordered case map. | Replace the default response or infer schema from transport encoding. | 2026-09-18 |""",
    "TODO / Open Questions": """- Complete end-to-end browser verification of the versioned legacy-case result path, including the first and last load cases.
- Establish an old-backend oracle before claiming general shell-result compatibility.
- Keep production authentication and deployment configuration separate from the anonymous local-development calculation path.""",
}

REQUIRED_DESIGN_PREFIXES = (
    "背景・目的",
    "スコープ",
    "機能要件",
    "非機能要件",
    "アーキテクチャ",
    "技術選定",
    "制約",
    "Key Decisions",
    "TODO / Open Questions",
)


class MigrationError(RuntimeError):
    """A safe, expected migration refusal, optionally with recovery artifacts."""

    def __init__(
        self, message: str, *, recovery_paths: tuple[Path, ...] = ()
    ) -> None:
        super().__init__(message)
        self.recovery_paths = recovery_paths


class JsonArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> NoReturn:
        print(json.dumps({"ok": False, "error": message}, ensure_ascii=False))
        raise SystemExit(1)


@dataclass(frozen=True)
class MigrationResult:
    changed: bool
    state_changed: bool
    design_changed: bool
    state_hash_before: str
    design_hash_before: str
    state_hash_after: str
    design_hash_after: str
    artifacts: tuple[str, ...]

    def as_dict(self) -> dict[str, object]:
        return {
            "ok": True,
            "changed": self.changed,
            "state_changed": self.state_changed,
            "design_changed": self.design_changed,
            "state_hash_before": self.state_hash_before,
            "design_hash_before": self.design_hash_before,
            "state_hash_after": self.state_hash_after,
            "design_hash_after": self.design_hash_after,
            "artifacts": list(self.artifacts),
        }


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def _decode_utf8(data: bytes, label: str) -> str:
    try:
        return data.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise MigrationError(f"{label} is not strict UTF-8: {exc}") from exc


def _newline_style(text: str, label: str) -> tuple[str, bool]:
    without_crlf = text.replace("\r\n", "")
    if "\r" in without_crlf or ("\r\n" in text and "\n" in without_crlf):
        raise MigrationError(f"{label} uses mixed or unsupported newlines")
    return ("\r\n" if "\r\n" in text else "\n", text.endswith(("\n", "\r")))


def _normalise(text: str, label: str) -> tuple[str, str, bool]:
    newline, final_newline = _newline_style(text, label)
    return text.replace("\r\n", "\n"), newline, final_newline


def _restore_newlines(text: str, newline: str, final_newline: bool) -> str:
    text = text.rstrip("\n")
    if final_newline:
        text += "\n"
    return text if newline == "\n" else text.replace("\n", "\r\n")


def _h2_chunks(text: str) -> tuple[str, list[tuple[str, str]]]:
    matches = list(H2_RE.finditer(text))
    preamble = text[: matches[0].start()] if matches else text
    chunks: list[tuple[str, str]] = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        chunks.append((match.group(1).strip(), text[match.start() : end]))
    return preamble, chunks


def _require_unique_headings(text: str, required: tuple[str, ...], label: str) -> None:
    _, chunks = _h2_chunks(text)
    headings = [heading for heading, _ in chunks]
    missing = [prefix for prefix in required if not any(h.startswith(prefix) for h in headings)]
    duplicates = [
        prefix for prefix in required if sum(h.startswith(prefix) for h in headings) != 1
    ]
    if missing or duplicates:
        raise MigrationError(
            f"malformed {label}: missing={missing!r}, non_unique={duplicates!r}"
        )


def _validate_state(text: str) -> None:
    if not text.startswith("# Agent State"):
        raise MigrationError("malformed STATE: expected '# Agent State' title")
    _require_unique_headings(
        text, ("Main Agent", "Repository Identity", "Progress Tracker"), "STATE"
    )


def _validate_design(text: str) -> None:
    if not text.startswith("# Design Document"):
        raise MigrationError("malformed DESIGN: expected '# Design Document' title")
    _require_unique_headings(text, REQUIRED_DESIGN_PREFIXES, "DESIGN")


def _render_h2(heading: str, body: str) -> str:
    return f"## {heading}\n\n{body.rstrip()}\n\n"


def migrate_state_text(text: str) -> str:
    normal, newline, final_newline = _normalise(text, "STATE")
    _validate_state(normal)
    if CANONICAL_STATE_MARKER in normal and not FOREIGN_RE.search(normal):
        return text

    preamble, chunks = _h2_chunks(normal)
    output = [preamble]
    for heading, chunk in chunks:
        if heading in FOREIGN_STATE_HEADINGS:
            continue
        if heading == "Main Agent":
            output.append(_render_h2(heading, "Codex"))
        elif heading == "Repository Identity":
            output.append(_render_h2(heading, STATE_IDENTITY))
        else:
            output.append(chunk)

    candidate = "".join(output)
    _validate_state(candidate)
    remaining = FOREIGN_RE.search(candidate)
    if remaining:
        raise MigrationError(
            "STATE still contains an unrecognized foreign reference after known-block removal: "
            f"{remaining.group(0)!r}"
        )
    return _restore_newlines(candidate, newline, final_newline)


def _strip_comments(body: str) -> str:
    return re.sub(r"<!--.*?-->", "", body, flags=re.DOTALL)


def _is_scaffold_line(line: str) -> bool:
    stripped = line.strip()
    if not stripped or stripped in {"-", "### In Scope", "### Out of Scope", "### Agent Roles"}:
        return True
    if stripped.startswith("|") and stripped.endswith("|"):
        cells = [cell.strip() for cell in stripped.strip("|").split("|")]
        if all(not cell or set(cell) <= {"-", ":"} for cell in cells):
            return True
        lowered = [cell.lower() for cell in cells]
        header_words = {
            "id",
            "requirement",
            "priority",
            "notes",
            "category",
            "metric / target",
            "agent",
            "role",
            "responsibilities",
            "area",
            "technology",
            "rationale",
            "alternatives considered",
            "decision",
            "date",
        }
        if all(not cell or cell in header_words for cell in lowered):
            return True
        if sum(bool(cell) for cell in cells) <= 1:
            return True
    return False


def _preserved_design_lines(body: str) -> list[str]:
    clean = _strip_comments(body)
    return [
        line.rstrip()
        for line in clean.splitlines()
        if not _is_scaffold_line(line) and not FOREIGN_RE.search(line)
    ]


def _merge_design_body(heading: str, old_body: str) -> str:
    canonical = DESIGN_SECTION_BODIES[heading]
    extras = _preserved_design_lines(old_body)
    canonical_lines = {line.strip() for line in canonical.splitlines() if line.strip()}
    extras = [line for line in extras if line.strip() not in canonical_lines]
    if not extras:
        return canonical

    if heading in {"機能要件 (Functional Requirements)", "Key Decisions"}:
        table_rows = [line for line in extras if line.lstrip().startswith("|")]
        other = [line for line in extras if not line.lstrip().startswith("|")]
        merged = canonical
        if table_rows:
            merged += "\n" + "\n".join(table_rows)
        if other:
            merged += "\n\n### Preserved Existing Notes\n\n" + "\n".join(other)
        return merged

    if heading == "TODO / Open Questions":
        return canonical + "\n" + "\n".join(extras)

    return canonical + "\n\n### Preserved Existing Notes\n\n" + "\n".join(extras)


def _is_migrated_design(text: str) -> bool:
    """Recognize a live FrameWeb3 design without depending on prose wording.

    Newly generated documents carry ``MIGRATION_MARKER``.  Documents migrated
    before that marker was introduced are recognized by their component anchors,
    required structure, and absence of copied-project or placeholder text.  This
    structural fallback is deliberately narrow so the current live document is a
    byte-for-byte no-op while an uninitialized template is still migrated.
    """

    if FOREIGN_RE.search(text) or DESIGN_PLACEHOLDER_RE.search(text):
        return False
    return MIGRATION_MARKER in text or all(
        anchor in text for anchor in CANONICAL_DESIGN_ANCHORS
    )


def migrate_design_text(text: str) -> str:
    normal, newline, final_newline = _normalise(text, "DESIGN")
    _validate_design(normal)
    if _is_migrated_design(normal):
        return text

    _, chunks = _h2_chunks(normal)
    output = [DESIGN_PREAMBLE.rstrip("\n") + "\n\n"]
    for old_heading, old_chunk in chunks:
        prefix = next(
            (p for p in REQUIRED_DESIGN_PREFIXES if old_heading.startswith(p)), None
        )
        if prefix is None:
            remaining = FOREIGN_RE.search(old_chunk)
            if remaining:
                raise MigrationError(
                    "unrelated DESIGN section contains a recognized foreign "
                    f"reference and cannot be preserved safely: {old_heading!r} "
                    f"({remaining.group(0)!r})"
                )
            # Preserve unrelated sections as one exact normalized-EOL chunk,
            # including their heading, comments, spacing, and body.
            output.append(old_chunk)
            continue

        canonical_heading = next(
            heading
            for heading in DESIGN_SECTION_BODIES
            if heading.startswith(prefix)
        )
        match = H2_RE.match(old_chunk)
        assert match is not None
        old_body = old_chunk[match.end() :]
        output.append(
            _render_h2(
                canonical_heading,
                _merge_design_body(canonical_heading, old_body),
            )
        )

    candidate = "".join(output)
    _validate_design(candidate)
    remaining = FOREIGN_RE.search(candidate)
    if remaining:
        raise MigrationError(
            "DESIGN still contains an unrecognized foreign reference after migration: "
            f"{remaining.group(0)!r}"
        )
    return _restore_newlines(candidate, newline, final_newline)


def _check_expected_hash(data: bytes, expected: str, label: str) -> str:
    if not re.fullmatch(r"[0-9a-fA-F]{64}", expected):
        raise MigrationError(f"invalid {label} expected hash: {expected!r}")
    actual = sha256_bytes(data)
    if actual.casefold() != expected.casefold():
        raise MigrationError(f"{label} hash mismatch: expected {expected.upper()}, got {actual}")
    return actual


def _write_same_directory_temp(target: Path, data: bytes, suffix: str) -> Path:
    target.parent.mkdir(parents=True, exist_ok=True)
    fd, raw_path = tempfile.mkstemp(
        prefix=f".{target.name}.", suffix=suffix, dir=str(target.parent)
    )
    path = Path(raw_path)
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        if target.exists():
            os.chmod(path, target.stat().st_mode)
        return path
    except BaseException:
        path.unlink(missing_ok=True)
        raise


def _lock_path(state_path: Path, design_path: Path) -> Path:
    identity = "\0".join(
        (
            os.path.normcase(str(state_path.resolve(strict=False))),
            os.path.normcase(str(design_path.resolve(strict=False))),
        )
    )
    digest = hashlib.sha256(identity.encode("utf-8", errors="strict")).hexdigest()[:24]
    return Path(tempfile.gettempdir()) / f"frameweb3-state-migration-{digest}.lock"


def _try_lock(stream: BinaryIO) -> bool:
    if os.name == "nt":
        import msvcrt

        stream.seek(0, os.SEEK_END)
        if stream.tell() == 0:
            stream.write(b"\0")
            stream.flush()
        stream.seek(0)
        try:
            msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError:
            return False
        return True

    import fcntl

    try:
        fcntl.flock(stream.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
    except (BlockingIOError, OSError):
        return False
    return True


def _unlock(stream: BinaryIO) -> None:
    if os.name == "nt":
        import msvcrt

        stream.seek(0)
        msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
        return

    import fcntl

    fcntl.flock(stream.fileno(), fcntl.LOCK_UN)


@contextmanager
def _transaction_lock(
    state_path: Path,
    design_path: Path,
    *,
    timeout: float = 10.0,
    poll_interval: float = 0.05,
) -> Iterator[Path]:
    """Hold one OS-backed inter-process lock for the complete transaction.

    The stable lock file lives in the system temporary directory rather than the
    repository.  The OS releases its advisory lock if the process exits; the
    harmless file may remain and be reused by the next invocation.
    """

    path = _lock_path(state_path, design_path)
    path.parent.mkdir(parents=True, exist_ok=True)
    stream = path.open("a+b")
    deadline = time.monotonic() + timeout
    acquired = False
    try:
        while not acquired:
            acquired = _try_lock(stream)
            if acquired:
                break
            if time.monotonic() >= deadline:
                raise MigrationError(f"migration transaction lock is busy: {path}")
            time.sleep(poll_interval)
        yield path
    finally:
        if acquired:
            with contextlib.suppress(OSError):
                _unlock(stream)
        with contextlib.suppress(OSError):
            stream.close()


def _unlink(path: Path) -> None:
    path.unlink(missing_ok=True)


def _unchanged_before_commit(
    originals: tuple[tuple[str, Path, bytes], ...]
) -> None:
    changed: list[str] = []
    for label, path, expected in originals:
        try:
            current = path.read_bytes()
        except OSError as exc:
            raise MigrationError(
                f"cannot re-read {label} immediately before commit: {exc}"
            ) from exc
        if current != expected:
            changed.append(label)
    if changed:
        raise MigrationError(
            "concurrent modification detected immediately before commit: "
            + ", ".join(changed)
        )


def _atomic_replace_pair(
    changes: list[tuple[Path, bytes, bytes]],
    originals: tuple[tuple[str, Path, bytes], ...],
    replace_fn: Callable[[Path, Path], None] = os.replace,
    before_commit_fn: Callable[[], None] | None = None,
    cleanup_fn: Callable[[Path], None] = _unlink,
) -> None:
    if not changes:
        return

    staged: list[tuple[Path, Path, Path]] = []
    replaced: list[tuple[Path, Path]] = []
    retained: set[Path] = set()
    primary: BaseException | None = None
    final_error: MigrationError | None = None
    try:
        for target, original, candidate in changes:
            candidate_path = _write_same_directory_temp(target, candidate, ".candidate")
            backup_path = _write_same_directory_temp(target, original, ".backup")
            staged.append((target, candidate_path, backup_path))

        if before_commit_fn is not None:
            before_commit_fn()
        # This is deliberately the final operation before the first replace.
        # The transaction lock prevents another cooperating migration process;
        # the byte comparison also detects non-cooperating writers.
        _unchanged_before_commit(originals)

        for target, candidate_path, backup_path in staged:
            replace_fn(candidate_path, target)
            replaced.append((target, backup_path))
    except BaseException as exc:
        primary = exc
        rollback_errors: list[str] = []
        for target, backup_path in reversed(replaced):
            try:
                replace_fn(backup_path, target)
            except BaseException as rollback_exc:  # pragma: no cover - catastrophic FS failure
                rollback_errors.append(f"{target}: {rollback_exc}")
                retained.add(backup_path)
        if rollback_errors:
            recovery_paths = tuple(sorted(retained, key=lambda item: str(item)))
            final_error = MigrationError(
                f"write failed ({exc}); rollback also failed: "
                f"{'; '.join(rollback_errors)}; exact backup retained for "
                f"manual recovery: {', '.join(str(path) for path in recovery_paths)}",
                recovery_paths=recovery_paths,
            )
        elif replaced:
            final_error = MigrationError(
                f"write failed; both originals restored: {exc}"
            )
        elif isinstance(exc, MigrationError):
            final_error = exc
        else:
            final_error = MigrationError(f"write failed before replacement: {exc}")
    finally:
        cleanup_errors: list[str] = []
        for _target, candidate_path, backup_path in staged:
            for path in (candidate_path, backup_path):
                if path in retained:
                    continue
                try:
                    cleanup_fn(path)
                except BaseException as cleanup_exc:
                    cleanup_errors.append(f"{path}: {cleanup_exc}")
        if final_error is not None and cleanup_errors:
            final_error.add_note(
                "best-effort cleanup could not remove: " + "; ".join(cleanup_errors)
            )

    if final_error is not None:
        raise final_error from primary


def migrate_repository(
    *,
    state_path: Path,
    design_path: Path,
    expected_state_hash: str,
    expected_design_hash: str,
    dry_run: bool = False,
    candidate_dir: Path | None = None,
    replace_fn: Callable[[Path, Path], None] = os.replace,
    before_commit_fn: Callable[[], None] | None = None,
    cleanup_fn: Callable[[Path], None] = _unlink,
    lock_timeout: float = 10.0,
) -> MigrationResult:
    with _transaction_lock(state_path, design_path, timeout=lock_timeout):
        state_original = state_path.read_bytes()
        design_original = design_path.read_bytes()
        state_before = _check_expected_hash(
            state_original, expected_state_hash, "STATE"
        )
        design_before = _check_expected_hash(
            design_original, expected_design_hash, "DESIGN"
        )

        state_text = _decode_utf8(state_original, "STATE")
        design_text = _decode_utf8(design_original, "DESIGN")
        state_candidate = migrate_state_text(state_text).encode("utf-8")
        design_candidate = migrate_design_text(design_text).encode("utf-8")

        # Validate encoded candidates once more before creating any target temp file.
        _validate_state(
            _decode_utf8(state_candidate, "STATE candidate").replace("\r\n", "\n")
        )
        _validate_design(
            _decode_utf8(design_candidate, "DESIGN candidate").replace("\r\n", "\n")
        )

        state_changed = state_candidate != state_original
        design_changed = design_candidate != design_original
        artifacts: list[str] = []

        if dry_run:
            if candidate_dir is None:
                raise MigrationError("--dry-run requires --candidate-dir")
            candidate_dir.mkdir(parents=True, exist_ok=True)
            state_output = candidate_dir / f"{state_path.name}.candidate"
            design_output = candidate_dir / f"{design_path.name}.candidate"
            state_output.write_bytes(state_candidate)
            design_output.write_bytes(design_candidate)
            artifacts.extend((str(state_output), str(design_output)))
        else:
            changes: list[tuple[Path, bytes, bytes]] = []
            if state_changed:
                changes.append((state_path, state_original, state_candidate))
            if design_changed:
                changes.append((design_path, design_original, design_candidate))
            _atomic_replace_pair(
                changes,
                (
                    ("STATE", state_path, state_original),
                    ("DESIGN", design_path, design_original),
                ),
                replace_fn=replace_fn,
                before_commit_fn=before_commit_fn,
                cleanup_fn=cleanup_fn,
            )
            artifacts.extend(str(path) for path, _original, _candidate in changes)

    return MigrationResult(
        changed=state_changed or design_changed,
        state_changed=state_changed,
        design_changed=design_changed,
        state_hash_before=state_before,
        design_hash_before=design_before,
        state_hash_after=sha256_bytes(state_candidate),
        design_hash_after=sha256_bytes(design_candidate),
        artifacts=tuple(artifacts),
    )


def _build_parser() -> JsonArgumentParser:
    parser = JsonArgumentParser(description=__doc__)
    parser.add_argument("--state", type=Path, default=DEFAULT_STATE)
    parser.add_argument("--design", type=Path, default=DEFAULT_DESIGN)
    parser.add_argument("--expect-state-hash", required=True)
    parser.add_argument("--expect-design-hash", required=True)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--candidate-dir", type=Path)
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    try:
        result = migrate_repository(
            state_path=args.state,
            design_path=args.design,
            expected_state_hash=args.expect_state_hash,
            expected_design_hash=args.expect_design_hash,
            dry_run=args.dry_run,
            candidate_dir=args.candidate_dir,
        )
    except (MigrationError, OSError) as exc:
        recovery_paths = (
            [str(path) for path in exc.recovery_paths]
            if isinstance(exc, MigrationError)
            else []
        )
        print(
            json.dumps(
                {
                    "ok": False,
                    "error": str(exc),
                    "recovery_paths": recovery_paths,
                    "artifacts": recovery_paths,
                },
                ensure_ascii=False,
            )
        )
        return 1
    print(json.dumps(result.as_dict(), ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
