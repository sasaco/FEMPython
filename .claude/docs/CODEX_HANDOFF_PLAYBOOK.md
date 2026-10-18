# Codex Handoff Playbook

This playbook standardizes bounded handoffs between the Codex lead, native
collaborating agents, and an optional nested Codex CLI consultation.

## When to Hand Off

Use a collaborator for an independent, clearly owned workstream. Use a nested
Codex consultation when an explicit second planning, architecture, debugging,
or review pass materially improves confidence. Keep trivial edits and named
verification commands with the lead.

## Prompt Contract

Every handoff includes:

1. Objective: one-sentence outcome.
2. Scope: owned paths and explicit exclusions.
3. Inputs: relevant files and constraints.
4. Authority: read-only or explicitly granted write scope.
5. Acceptance checks: exact commands and expected results.
6. Output shape: concise decision-relevant sections and durable artifact paths.

Native collaboration tools are preferred when they already provide the needed
worker or reviewer. For a nested CLI consultation, use the shared wrapper; it
closes stdin, applies a timeout, and captures stdout/stderr.

## Read-only Consultation

```powershell
$promptFile = '.agents/logs/codex/prompt-plan.md'
@'
Objective: Create an implementation plan for {feature}.
Constraints:
- Preserve existing behavior unless a change is explicitly justified.
- Do not modify files.
Relevant files:
- {file1}
- {file2}
Acceptance checks:
- {read-only checks}
Output format:
## Analysis
## Recommendation
## Implementation Plan
## Risks
'@ | Set-Content -LiteralPath $promptFile -Encoding utf8

uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file $promptFile --label plan --sandbox read-only
```

## Explicit Implementation Handoff

```powershell
$promptFile = '.agents/logs/codex/prompt-implement.md'
@'
Objective: Implement {feature or fix}.
Scope:
- Own: {paths}
- Do not touch: {paths}
Acceptance checks:
- {tests or build commands}
Output format:
## Changes Made
## Validation
## Remaining Risks
'@ | Set-Content -LiteralPath $promptFile -Encoding utf8

uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file $promptFile --label implement --sandbox workspace-write
```

Use `danger-full-access` only when the approved task requires access outside the
workspace; it is not the routine implementation mode.

## Result Handling and Verification

- Keep the recommendation, implementation summary, evidence, and risks that
  require a decision. Store long material under `.agents/docs/` or
  `.agents/logs/` and return its path.
- Independently run the acceptance checks and inspect the diff.
- Reject out-of-scope edits, weakened tests, swallowed failures, and placeholder
  implementations. A successful delegated process is evidence, not approval.
- If a result is not actionable, retry once with the missing evidence and a
  narrower question; after a repeated failure, stop and report the blocker.
