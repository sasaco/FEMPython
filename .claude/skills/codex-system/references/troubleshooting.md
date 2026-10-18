# Troubleshooting with Codex

## Investigation Order

1. Capture the exact error, command, environment, and expected behavior.
2. Reproduce with the smallest safe command and preserve its output.
3. Separate facts from hypotheses and rank hypotheses by evidence.
4. Identify the smallest experiment that can falsify the leading hypothesis.
5. Propose a fix only after the root cause is supported.

## Read-only Consultation

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py `
  --prompt-file .agents/logs/codex/prompt-troubleshoot.md `
  --label troubleshoot `
  --sandbox read-only
```

The prompt includes logs, relevant paths, attempted reproductions, and explicit
questions. Do not perform global CLI upgrades as a diagnostic step.

## Fix Gate

After proving the cause, implement under explicit owned paths, rerun the failing
reproduction, run adjacent regression tests, and inspect the diff for hidden
error suppression or test weakening. Record environment-only blockers
separately from product defects.
