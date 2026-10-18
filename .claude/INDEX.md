# `.agents` Registry

## Ownership Boundary

| Directory | Owns |
|---|---|
| `.agents/` | Shared policy, skills, project docs, logs, checkpoints, and state |
| `.codex/` | Codex project configuration |

## Entries

| Item | Status | Canonical path | Notes |
|---|---|---|---|
| Root agent contract | normative | `AGENTS.md` | Language, component map, safe commands, ownership |
| Rules | normative | `.agents/rules/` | Coding, testing, security, routing, CLI, and state contracts |
| Skills | normative | `.agents/skills/` | Codex-discovered workflows and deterministic helpers |
| Mutable state | project-owned | `.agents/STATE.md` | Main runtime, repository identity, and current work |
| Project design | project-owned | `.agents/docs/DESIGN.md` | Requirements and durable decisions |
| Plans and reports | project-owned | `.agents/docs/` | Plans, research, reviews, and library notes |
| Progress | project-owned | `PROGRESS.md`, `.agents/checkpoints/` | Rolling summary and checkpoint details |
| Main-runtime runbook | normative | `.agents/change_main.md` | Used only for an explicit runtime change |
| Windows checker | tooling | `.agents/check.ps1` | Canonical agent-infrastructure gate |
| Logs | generated evidence | `.agents/logs/` | Keep task/review evidence; remove only by an explicit retention decision |

## Runtime Policy

- Codex is the primary runtime. `.codex/config.toml` defaults to read-only and
  `approval_policy = "never"`; implementation must request workspace-write
  explicitly.
- There is no active Claude or Antigravity discovery/configuration surface.
- PowerShell is canonical. Python helpers run from the repository root through
  `uv run --project FrameWeb --locked --extra dev python ...`.
