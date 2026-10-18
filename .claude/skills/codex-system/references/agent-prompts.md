# Agent Prompt Patterns

Use these patterns for native collaborators or a nested Codex consultation.
Replace placeholders and remove sections that do not apply.

## Implementation

```text
Objective: <one outcome>
Scope:
- Own: <paths>
- Do not touch: <paths>
Constraints:
- <contract or compatibility requirement>
Authority: workspace write limited to owned paths
Acceptance checks:
- <exact command and expected result>
Output:
## Summary
## Files Changed
## Verification
## Remaining Risks
```

## Read-only Analysis or Review

```text
Objective: <decision the analysis enables>
Scope: read-only; inspect <paths>
Questions:
1. <question>
2. <question>
Evidence required: file/line references and command results
Output:
## Conclusion
## Evidence
## Findings
## Recommendation
```

Prompts must identify ownership, authority, and acceptance checks. Do not name a
runtime-specific worker unless that worker is actually available.
