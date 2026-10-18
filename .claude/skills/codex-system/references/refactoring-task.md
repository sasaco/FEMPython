# Refactoring Task

## Constraints

- Preserve externally observable behavior and serialized contracts.
- Keep the change inside explicit owned paths.
- Add or strengthen characterization tests before risky structural changes.
- Do not combine dependency upgrades or broad formatting churn with the
  refactor.

## Prompt Template

```text
Objective: Refactor <target> to <quality outcome> without behavior change.
Scope:
- Own: <paths>
- Do not touch: <paths>
Invariants:
- <public/API/data/performance invariants>
Acceptance checks:
- <focused tests>
- <component build or integration gate>
Output:
## Changes Made
## Invariants Preserved
## Validation
## Remaining Risks
```

For a nested implementation, use explicit repository write authority:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py `
  --prompt-file .agents/logs/codex/prompt-refactor.md `
  --label refactor `
  --sandbox workspace-write
```

Finish with `& .agents/check.ps1` when agent infrastructure changed and with
the relevant Python, Angular, or .NET component gates.
