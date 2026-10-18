# Code Review Task

## Objective

Find correctness, security, compatibility, performance, and test-coverage
problems in the scoped change. Review is read-only.

## Prompt Template

```text
Objective: Review the changes in <scope> against <base>.
Constraints:
- Do not modify files.
- Prioritize behavior and contracts over style.
- Check changed and untracked files.
Relevant files: <paths>
Acceptance checks: <commands already run or to reproduce>
Output:
## Verdict
## Findings
- [severity] path:line — issue, impact, and remediation
## Missing Tests
## Residual Risks
```

## Invocation

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py `
  --prompt-file .agents/logs/codex/prompt-code-review.md `
  --label code-review `
  --sandbox read-only
```

The lead independently inspects the diff and runs relevant checks. A review
with no findings states what was inspected and which risks remain unverified.
