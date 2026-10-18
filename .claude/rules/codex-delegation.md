# Codex Work Routing

Codex is both the primary runtime and the preferred reasoning engine for
planning, design, complex implementation, debugging, and code review.

## Direct Work vs Consultation

The main Codex agent handles the task directly when it already has the needed
context and authority. Use a native collaborator for an independent owned
workstream. Use `.agents/skills/_shared/codex_consult.py` only when an explicit
nested Codex CLI pass provides useful independent planning or review.

Typical consultation triggers:

- architecture or compatibility decisions;
- multi-step plans with meaningful ordering or rollback risk;
- unclear root cause or competing hypotheses;
- security, concurrency, data-integrity, or migration-sensitive changes;
- independent review of a large diff.

## Consultation Contract

Include objective, constraints, relevant paths, authority, acceptance checks,
and output format. Use repository-root PowerShell commands:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file <path> --label <slug> --sandbox read-only
```

Read-only is the default. For an approved implementation handoff, pass
`--sandbox workspace-write`. Use `danger-full-access` only when the task
requires access outside the workspace.

Detailed patterns are in `.agents/docs/CODEX_HANDOFF_PLAYBOOK.md` and
`.agents/skills/codex-system/SKILL.md`.

## Verification

The lead runs the original acceptance checks, inspects all changed/untracked
paths, and rejects out-of-scope edits, weakened tests, swallowed exceptions,
hardcoded substitutes, or unfinished placeholders. A successful consultation
process does not establish correctness by itself.
