# Agent State Contract

`AGENTS.md` is the concise Codex bootstrap. Repository-specific and
cross-session state belongs in `.agents/STATE.md`, not in the bootstrap.

## State Ownership

| Section | Owner / writers | Content |
|---|---|---|
| `## Main Agent` | explicit runtime-change workflow | Active main runtime |
| `## Repository Identity` | `init` skill | Thin identity plus a pointer to `.agents/docs/DESIGN.md` |
| `## Progress Tracker` | `checkpointing` skill | Idempotent link to `PROGRESS.md` |
| Working blocks | workflow skills and manual notes | Current feature and bug-fix context |

Installers and updaters must preserve `.agents/STATE.md`. New bootstraps must
not contain legacy boundary markers or runtime-specific pseudo-links.

## Mechanical Checks

`refresh_guard.py` provides distinct `check`, `plan`, `compose`, `apply`, and
`verify` modes. `apply` is dry-run unless `--apply` is present and requires the
writer's hash guard for concurrent-modification safety. A compaction candidate
may remove only redundant `## Current *` blocks; other sections are preserved.

Run helpers from the repository root:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/checkpointing/refresh_guard.py --mode check
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/validate_doc.py --contract state-doc --file .agents/STATE.md
```

The state document must contain exactly one `# Agent State`, `## Main Agent`,
and `## Progress Tracker` structure according to the validator contract.
