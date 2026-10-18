# Delegation Patterns

## Direct Codex Work

Use the primary Codex agent when the task is cohesive, context is already
loaded, and one agent can implement and verify it without ownership conflicts.

## Native Collaboration

Use native collaborators for independent workstreams with disjoint owned paths.
Launch work concurrently only when no result is required to frame another
worker's task. The lead integrates and verifies all results.

## Nested Codex Consultation

Use a nested read-only pass for independent planning, architecture evaluation,
debugging hypotheses, or review:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py `
  --prompt-file .agents/logs/codex/prompt-architecture.md `
  --label architecture `
  --sandbox read-only
```

Grant `workspace-write` only for an explicitly approved implementation with a
clear owned-path contract. Do not assume external plugins or named workers.

## Escalation

After one failed or incomplete attempt, add the concrete failure evidence and
narrow the question. Do not repeat the same prompt. After a second failure,
stop and report the blocker or ask the user for the missing decision.
