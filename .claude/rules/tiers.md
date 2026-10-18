# Agent Tier Definitions

Tiers describe responsibility and authority, not a vendor-specific model name.

## Tier 1: `default`

- Codex is the main runtime and owns user interaction, routing, edits,
  integration, and verification.
- Project defaults come from `.codex/config.toml`: read-only sandbox and
  `approval_policy = "never"`.
- A build/change request grants repository-scoped write authority to the active
  implementation work; read-only requests do not.

## Tier 2: `collaborator`

- A native collaborator receives one bounded objective and exclusive paths.
- Read-only is the default. Write access is explicit in the task and limited to
  the owned paths.
- The collaborator returns decision-relevant findings, changed files, executed
  checks, and remaining risks. The lead independently verifies them.

## Tier 3: `independent-reviewer`

- Used for security, quality, test coverage, design challenge, or repeated
  failure when an independent view materially reduces risk.
- Reviewers are read-only unless the lead issues a separate fix task.
- Findings include severity, evidence, and a concrete remediation or an
  explicit no-finding result.

No tier implies a particular external plugin, CLI, or model. Use only
capabilities that are actually available in the current runtime.
