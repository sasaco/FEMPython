# FrameWeb3 Agent Contract

Codex is the primary agent for this repository. Keep repository-specific state
in `.agents/STATE.md` and durable design decisions in
`.agents/docs/DESIGN.md`.

## Language Protocol

- Communicate with the user in Japanese unless they request another language.
- Use English for code identifiers, commit-ready technical prose, and prompts
  sent to collaborating agents unless the existing artifact uses Japanese.
- Preserve the language and terminology of existing product documentation.

## Repository Components

| Component | Location | Toolchain | Purpose |
|---|---|---|---|
| FEM backend | `FrameWeb/` | Python 3.11+ / uv | Structural-analysis engine and HTTP entry points |
| Web client | `FrameWebforJS/` | Node 18 / npm / Angular 15 | Browser and Electron client |
| Desktop and printing | `FrameWeb.sln`, `FrameWebforCS/`, `FramePrintPDF/` | .NET / dotnet | WinForms client and PDF/print services |
| Legacy converter | `FrameGConverter/` | .NET | Separate converter solution; change only when explicitly in scope |
| Agent infrastructure | `.agents/`, `.codex/` | PowerShell and repository Python environment | Rules, skills, state, checks, and logs |

## Working Directory and Commands

Run commands from the repository root in PowerShell. Do not rely on Bash,
`python3`, virtual-environment activation, or a globally installed Python.

```powershell
# Python
uv --directory FrameWeb run --locked --extra dev python -m pytest tests -q

# Angular
npm --prefix FrameWebforJS run test -- --watch=false --browsers=ChromeHeadless
npm --prefix FrameWebforJS run build

# .NET desktop
dotnet build FrameWeb.sln
dotnet run --project FrameWebforCS/FrameWebforCS.csproj

# Agent-infrastructure checks
& .agents/check.ps1
```

Use `FrameWeb/uv.lock` and `FrameWebforJS/package-lock.json` for reproducible
installs. Keep compatible dependency ranges in `pyproject.toml` and
`package.json`; update lockfiles with the component's package manager rather
than hand-editing them. Run only the gates relevant to the files changed, plus
the agent-infrastructure gate when `.agents/`, `.codex/`, or this file changes.

## Ownership and Concurrency

- Inspect `git status` and the relevant diff before editing. Existing changes
  belong to the user or another active worker unless ownership says otherwise.
- In multi-agent work, give each worker exclusive paths and do not modify paths
  owned by another worker. Coordinate shared-file changes through the lead.
- Keep product edits inside the component requested by the user. Do not fold
  opportunistic cleanup into an unrelated task.
- Never discard or rewrite concurrent work to make a check pass.

## Agent Infrastructure Entrypoints

- Start every task with `.agents/skills/context-loader/SKILL.md`.
- Read `.agents/INDEX.md` for the active infrastructure map and
  `.agents/rules/` for detailed policy.
- Use `.agents/check.ps1` as the canonical Windows validation entrypoint.
- Use `.agents/skills/_shared/codex_consult.py` only when an explicit nested
  Codex consultation is useful; read-only is the default and write authority
  must be passed explicitly.
- Use `.agents/change_main.md` only when the user explicitly requests a main
  runtime change.
