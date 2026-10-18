# Codex CLI Execution and Verification

Root `AGENTS.md` is the shared contract. This file governs explicit nested
Codex CLI consultations; the primary Codex agent normally uses its native
tools directly.

## Response Contract

Ask for only the sections the task needs, normally:

```markdown
## Summary
## Analysis
## Plan or Changes Made
## Validation
## Remaining Risks
```

Long raw logs stay under `.agents/logs/`; return their paths and the
decision-relevant facts.

## Wrapper-only Invocation

Do not construct a bare `codex exec` shell command. The shared wrapper closes
stdin, validates sandbox/config overrides, enforces a timeout, and stores stdout
and stderr.

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file <path> --label <slug> --sandbox read-only
```

- Default: `--sandbox read-only`.
- Approved repository implementation: `--sandbox workspace-write`.
- Outside-workspace access: `--sandbox danger-full-access` only when explicitly
  required by the task.
- Keep `approval_policy = "never"`; authority is expressed by the sandbox and
  the owned-path contract.

## Completion Verification

The caller independently runs acceptance checks and reviews the diff. The
canonical agent-infrastructure gate is:

```powershell
& .agents/check.ps1
```

When path-level delegation evidence is needed:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/verify_delegation.py --base HEAD --expect-files <path> --forbid-outside <scope> --label <slug>
```

Reject completion for unauthorized deletions, out-of-scope changes, skipped or
weakened tests, swallowed exceptions, hardcoded substitutes, or placeholders.
The verifier is heuristic and never replaces review judgment.

On a failed verification, report evidence and retry at most once with a tighter
prompt. If the repeated attempt fails, stop and ask the user for direction.
