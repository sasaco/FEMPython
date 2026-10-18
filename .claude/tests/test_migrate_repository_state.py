from __future__ import annotations

import importlib.util
import os
import sys
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / ".agents" / "skills" / "init" / "migrate_repository_state.py"
SPEC = importlib.util.spec_from_file_location("migrate_repository_state", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
migration = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = migration
SPEC.loader.exec_module(migration)


def state_fixture(newline: str = "\n") -> str:
    text = """# Agent State

## Main Agent

Claude Code

## Repository Identity

_Not initialized yet. Run `/init` to populate._

## Progress Tracker

Rolling progress summary: [PROGRESS.md](../PROGRESS.md)

---

## Current Feature: Minute chart interaction and historical loading

Copied TickReplay minute-context state.

---

## Current Bug Fix: framewebforjs-results-not-displayed
<!-- orchestra:block-id: framewebforjs-results-not-displayed -->

### Context

- 日本語の現行作業ブロックは一字も失わない。

### Decisions

- Preserve this exact FrameWeb3 decision.
"""
    return text.replace("\n", newline)


def design_fixture(newline: str = "\n") -> str:
    text = """# Design Document — fixture

## 背景・目的 (Background & Purpose)

Copied TickReplay system backed by DuckDB.

## スコープ (Scope)

- Existing repository-specific scope note must survive.

## 機能要件 (Functional Requirements)

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-TICKREPLAY-1 | Use minute-context. | High | copied |
| FR-FRAMEWEB-KEEP | 現行の要件を保持する。 | High | exact row |

## 非機能要件 (Non-Functional Requirements)

| Category | Requirement | Metric / Target |
|----------|-------------|-----------------|
| Performance | Rebuild SMA25 and SMA200. | copied |

## アーキテクチャ (Architecture)

- src/tickreplay/app.js owns replay state.

## 技術選定 (Tech Stack & Rationale)

| Area | Technology | Rationale | Alternatives Considered |
|------|------------|-----------|-------------------------|
| Replay | DuckDB | copied | none |

## 制約 (Constraints)

- Daily-context is bounded.

## Key Decisions

| Decision | Rationale | Alternatives Considered | Date |
|----------|-----------|------------------------|------|
| Use stocks_daily. | copied | none | 2026-08-30 |
| Preserve the explicit FrameWeb compatibility boundary. | It is current. | Replace the default response. | 2026-09-18 |

## TODO / Open Questions

- Keep this current open question.
"""
    return text.replace("\n", newline)


def write_pair(tmp_path: Path, newline: str = "\n") -> tuple[Path, Path, bytes, bytes]:
    state_path = tmp_path / "STATE.md"
    design_path = tmp_path / "DESIGN.md"
    state_bytes = state_fixture(newline).encode("utf-8")
    design_bytes = design_fixture(newline).encode("utf-8")
    state_path.write_bytes(state_bytes)
    design_path.write_bytes(design_bytes)
    return state_path, design_path, state_bytes, design_bytes


def apply_pair(
    state_path: Path,
    design_path: Path,
    state_bytes: bytes,
    design_bytes: bytes,
    **kwargs: object,
):
    return migration.migrate_repository(
        state_path=state_path,
        design_path=design_path,
        expected_state_hash=migration.sha256_bytes(state_bytes),
        expected_design_hash=migration.sha256_bytes(design_bytes),
        **kwargs,
    )


def test_removes_only_copied_content_and_preserves_current_sections(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)

    result = apply_pair(state_path, design_path, state_before, design_before)

    state_after = state_path.read_text(encoding="utf-8")
    design_after = design_path.read_text(encoding="utf-8")
    assert result.changed is True
    assert "TickReplay" not in state_after
    assert "minute-context" not in state_after
    assert "<!-- orchestra:block-id: framewebforjs-results-not-displayed -->\n\n### Context\n\n- 日本語の現行作業ブロックは一字も失わない。" in state_after
    assert "- Preserve this exact FrameWeb3 decision." in state_after
    assert "Claude Code" not in state_after
    assert "\nCodex\n" in state_after
    assert migration.FOREIGN_RE.search(design_after) is None
    assert "| FR-FRAMEWEB-KEEP | 現行の要件を保持する。 | High | exact row |" in design_after
    assert "| Preserve the explicit FrameWeb compatibility boundary. | It is current. | Replace the default response. | 2026-09-18 |" in design_after
    assert "Existing repository-specific scope note must survive." in design_after
    assert "Keep this current open question." in design_after
    assert migration.MIGRATION_MARKER in design_after


def test_preserves_unrelated_design_h2_chunk_exactly(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    boundary = "## 機能要件 (Functional Requirements)\n".encode()
    unrelated = (
        "## Extension Contract\n\n"
        "<!-- extension-owner: downstream -->\n"
        "Keep  two spaces, 日本語, and `literal/code`.  \n\n"
    ).encode("utf-8")
    design_before = design_before.replace(boundary, unrelated + boundary)
    design_path.write_bytes(design_before)

    apply_pair(state_path, design_path, state_before, design_before)

    design_after = design_path.read_bytes()
    assert unrelated in design_after
    assert design_after.count(unrelated) == 1


def test_refuses_foreign_content_in_unrelated_design_h2(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    boundary = "## 機能要件 (Functional Requirements)\n".encode()
    unsafe = "## Unknown Copied Section\n\nUse DuckDB here.\n\n".encode()
    design_before = design_before.replace(boundary, unsafe + boundary)
    design_path.write_bytes(design_before)

    with pytest.raises(migration.MigrationError, match="cannot be preserved safely"):
        apply_pair(state_path, design_path, state_before, design_before)

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before


def test_hash_mismatch_leaves_both_documents_unchanged(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)

    with pytest.raises(migration.MigrationError, match="STATE hash mismatch"):
        migration.migrate_repository(
            state_path=state_path,
            design_path=design_path,
            expected_state_hash="0" * 64,
            expected_design_hash=migration.sha256_bytes(design_before),
        )

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before


def test_second_replace_failure_rolls_back_first_document(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    failed = False

    def fail_design_once(source: Path, destination: Path) -> None:
        nonlocal failed
        if Path(destination) == design_path and not failed:
            failed = True
            raise OSError("simulated DESIGN replace failure")
        os.replace(source, destination)

    with pytest.raises(migration.MigrationError, match="both originals restored"):
        apply_pair(
            state_path,
            design_path,
            state_before,
            design_before,
            replace_fn=fail_design_once,
        )

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before
    assert list(tmp_path.glob(".*.candidate")) == []
    assert list(tmp_path.glob(".*.backup")) == []


def test_concurrent_edit_before_commit_aborts_without_replacing_either_target(
    tmp_path: Path,
) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    concurrent_state = state_before + b"\nCONCURRENT WRITER CONTENT\n"

    def mutate_state_after_staging() -> None:
        state_path.write_bytes(concurrent_state)

    with pytest.raises(
        migration.MigrationError,
        match="concurrent modification detected immediately before commit: STATE",
    ):
        apply_pair(
            state_path,
            design_path,
            state_before,
            design_before,
            before_commit_fn=mutate_state_after_staging,
        )

    assert state_path.read_bytes() == concurrent_state
    assert design_path.read_bytes() == design_before
    assert list(tmp_path.glob(".*.candidate")) == []
    assert list(tmp_path.glob(".*.backup")) == []


def test_rollback_failure_retains_exact_backup_and_exposes_recovery_path(
    tmp_path: Path,
) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)

    def fail_design_and_state_rollback(source: Path, destination: Path) -> None:
        source = Path(source)
        destination = Path(destination)
        if destination == design_path and source.name.endswith(".candidate"):
            raise OSError("simulated DESIGN replace failure")
        if destination == state_path and source.name.endswith(".backup"):
            raise OSError("simulated STATE rollback failure")
        os.replace(source, destination)

    def fail_candidate_cleanup(path: Path) -> None:
        if path.name.endswith(".candidate"):
            raise OSError("simulated cleanup failure")
        path.unlink(missing_ok=True)

    with pytest.raises(migration.MigrationError) as caught:
        apply_pair(
            state_path,
            design_path,
            state_before,
            design_before,
            replace_fn=fail_design_and_state_rollback,
            cleanup_fn=fail_candidate_cleanup,
        )

    error = caught.value
    assert "simulated DESIGN replace failure" in str(error)
    assert "rollback also failed" in str(error)
    assert "exact backup retained for manual recovery" in str(error)
    assert len(error.recovery_paths) == 1
    recovery_path = error.recovery_paths[0]
    assert str(recovery_path) in str(error)
    assert recovery_path.read_bytes() == state_before
    assert state_path.read_bytes() != state_before
    assert design_path.read_bytes() == design_before
    assert any(
        "simulated cleanup failure" in note
        for note in getattr(error, "__notes__", [])
    )


def test_apply_is_idempotent(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    first = apply_pair(state_path, design_path, state_before, design_before)
    state_once = state_path.read_bytes()
    design_once = design_path.read_bytes()

    second = apply_pair(state_path, design_path, state_once, design_once)

    assert first.changed is True
    assert second.changed is False
    assert second.artifacts == ()
    assert state_path.read_bytes() == state_once
    assert design_path.read_bytes() == design_once


def test_current_live_documents_are_byte_for_byte_idempotent() -> None:
    state_bytes = (ROOT / ".agents/STATE.md").read_bytes()
    design_bytes = (ROOT / ".agents/docs/DESIGN.md").read_bytes()

    state_candidate = migration.migrate_state_text(
        state_bytes.decode("utf-8", errors="strict")
    ).encode("utf-8")
    design_candidate = migration.migrate_design_text(
        design_bytes.decode("utf-8", errors="strict")
    ).encode("utf-8")

    assert state_candidate == state_bytes
    assert design_candidate == design_bytes


def test_utf8_and_crlf_are_preserved(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path, "\r\n")

    apply_pair(state_path, design_path, state_before, design_before)

    for path in (state_path, design_path):
        data = path.read_bytes()
        decoded = data.decode("utf-8", errors="strict")
        assert "日本語" in decoded or "要件定義書" in decoded
        assert b"\r\n" in data
        assert b"\n" not in data.replace(b"\r\n", b"")


@pytest.mark.parametrize("broken", ["state", "design"])
def test_malformed_document_refuses_before_any_write(tmp_path: Path, broken: str) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    if broken == "state":
        state_before = state_before.replace(b"## Progress Tracker", b"## Missing Tracker")
        state_path.write_bytes(state_before)
    else:
        design_before = design_before.replace(
            "## 制約 (Constraints)".encode(), "## Missing Constraints".encode()
        )
        design_path.write_bytes(design_before)

    with pytest.raises(migration.MigrationError, match="malformed"):
        apply_pair(state_path, design_path, state_before, design_before)

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before


def test_invalid_utf8_refuses_before_any_write(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    design_before += b"\xff"
    design_path.write_bytes(design_before)

    with pytest.raises(migration.MigrationError, match="strict UTF-8"):
        apply_pair(state_path, design_path, state_before, design_before)

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before


def test_dry_run_writes_separate_candidates_without_mutating_sources(tmp_path: Path) -> None:
    state_path, design_path, state_before, design_before = write_pair(tmp_path)
    output_dir = tmp_path / "candidates"

    result = apply_pair(
        state_path,
        design_path,
        state_before,
        design_before,
        dry_run=True,
        candidate_dir=output_dir,
    )

    assert state_path.read_bytes() == state_before
    assert design_path.read_bytes() == design_before
    assert {Path(path).name for path in result.artifacts} == {
        "STATE.md.candidate",
        "DESIGN.md.candidate",
    }
    assert migration.FOREIGN_RE.search(
        (output_dir / "DESIGN.md.candidate").read_text(encoding="utf-8")
    ) is None
