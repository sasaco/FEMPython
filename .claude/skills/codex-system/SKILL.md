---
name: codex-system
description: |
  Codex CLI handles planning, design, and complex code implementation.
  Use for: architecture design, implementation planning, complex algorithms,
  debugging (root cause analysis), trade-off evaluation, code review.
  External research is NOT Codex's job — use a web-enabled research collaborator instead.
  Explicit triggers: "plan", "design", "architecture", "think deeper",
  "analyze", "debug", "complex", "optimize".
metadata:
  short-description: Codex CLI — planning, design, and complex implementation
---

# Codex System — Planning, Design & Complex Implementation

**Codex CLI handles planning, design, and complex code implementation.**

> **Preflight (SSOT):** Do not update Codex, Node, or any global CLI as task preflight. Use the repository's pinned toolchains and review upgrades as explicit maintenance work.
> **Delegation policy (when to delegate)**: `.agents/rules/codex-delegation.md`

## Two Roles of Codex

### 1. Planning & Design

- Architecture design, module composition
- Implementation plan creation (step breakdown, dependency ordering)
- Trade-off evaluation, technology selection
- Code review (quality and correctness analysis)

### 2. Complex Implementation

- Complex algorithms, optimization
- Debugging with unknown root causes
- Advanced refactoring
- Multi-step implementation tasks

## When to Delegate

Delegation policy — when to consult, when NOT to, and trigger criteria — lives in `.agents/rules/codex-delegation.md` (SSOT). This skill covers *how* to consult. The other half of "how" is **how to verify what came back**: `.agents/rules/cli-execution.md` → *Guardrails (Completion Verification)* is mandatory for every write-access call, and [Verify Before Trusting](#verify-before-trusting) below is its executable form. A delegated CLI is never trusted on its self-report.

## How to Consult

> Invoke Codex through the wrapper — `.agents/skills/_shared/codex_consult.py` — instead of calling `codex exec` directly. `codex exec` itself waits for stdin EOF and hangs indefinitely when stdin is left open (e.g. background shells); the wrapper always runs it with stdin closed, so callers never need `< /dev/null`. It also passes the prompt as a single argv element (no shell, so nested quotes in the prompt body never break it), captures stdout/stderr to timestamped files under `.agents/logs/codex/`, and reports one JSON result instead of silently discarding stderr.

```
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py (--prompt-file PATH | --prompt-stdin) [--label L] [--sandbox {read-only,workspace-write,danger-full-access}] [--model M] [--timeout N] [--cwd DIR] [--project-root DIR] [--skip-git-repo-check] [--config KEY=VALUE]
```

- Write the prompt body (Objective / Constraints / Relevant files / Acceptance checks / Output format) to `.agents/logs/codex/prompt-{label}.md` and pass it via `--prompt-file`; use `--prompt-stdin` only for a short prompt. Keeping prompts beside wrapper responses makes a disappointing answer diagnosable.
- `--sandbox` defaults to `read-only`. Pass `--sandbox workspace-write` only for an approved repository implementation — see Sandbox Modes below.
- `--model` defaults to `$CODEX_MODEL`, else `gpt-5.6-sol`. `--label` is a `[a-z0-9-]+` slug used in the log filenames (default `consult`). `--timeout` defaults to 600 seconds. `--skip-git-repo-check` covers the non-Git working directory case — see `references/troubleshooting.md`.
- `--config KEY=VALUE` (repeatable) forwards a Codex config override, e.g. `--config model_reasoning_effort=low` for a cheap question. Keys naming a sandbox or approval setting are refused: `--sandbox` must stay the single visible statement of what Codex is allowed to touch.
- The wrapper prints exactly one JSON object: `{ok, exit_code, model, sandbox, write_access, timed_out, duration_sec, response_file, stderr_file, response_chars, response_head, error}`. `response_head` is only a ~400-char preview — read the file at `response_file` for the full response, and `stderr_file` (non-null whenever Codex wrote to stderr) when diagnosing a failure.
- Exit codes: `0` succeeded · `1` bad args or unreadable prompt file · `2` `codex` not on PATH · `3` codex exited non-zero or timed out.

### Subagent Pattern (Recommended)

```
Collaboration task brief:
- role: "high-capability analysis collaborator"
- run_in_background: true (optional)
- prompt: |
    Consult Codex about: {topic}

    Write the prompt body below to a file, then run the wrapper against it:

    Objective: {single-sentence objective}
    Constraints:
    - {constraint 1}
    Relevant files:
    - {file paths}
    Acceptance checks:
    - {commands}
    Output format:
    ## Analysis
    ## Recommendation
    ## Implementation Plan
    ## Risks
    ## Next Steps

    uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file {prompt_path} --label {short-slug} --sandbox read-only

    Parse the JSON result. ok: true means only that codex exec exited 0 — it is
    not a completion report. Read response_file for the full analysis, and judge
    it yourself: state which claims you verified and which you could not.
    Return CONCISE summary (key recommendation + rationale + what is unverified).
```

For a **write-access** subagent call, add the Verify Before Trusting steps to the delegated prompt as well, and require the subagent to return the `.agents/check.ps1` and `verify_delegation.py` verdicts. A subagent that summarises Codex's self-report without them has not verified anything, and its summary must not be treated as a completion.

### Direct Call (short questions, responses up to ~50 lines)

```powershell
echo "Objective: {brief question}" | uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-stdin --label quick-question --sandbox read-only
```

### Having Codex Implement Code

Create a stable, reviewable prompt file under `.agents/logs/codex/`, then grant
only repository-scoped write access:

```powershell
$promptFile = ".agents/logs/codex/prompt-implement.md"
New-Item -ItemType Directory -Force (Split-Path $promptFile) | Out-Null
@'
Objective: Implement {detailed implementation task}
Constraints:
- Follow existing project conventions
- Keep diffs minimal
Relevant files:
- {file paths}
Acceptance checks:
- {commands}
Output format:
## Changes Made
## Validation
## Remaining Risks
'@ | Set-Content -LiteralPath $promptFile -Encoding utf8NoBOM
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file $promptFile --label implement --sandbox workspace-write
```

The call is **not** finished here. Continue with the next section.

### Verify Before Trusting

Mandatory after every `workspace-write` / `danger-full-access` call (Codex or a peer CLI), per `.agents/rules/cli-execution.md` → *Guardrails*. `ok: true` from the wrapper means only that `codex exec` exited `0`; it says nothing about whether the change is correct, complete, or honest.

**1. Run the acceptance checks from your own prompt**, plus the project gates:

```powershell
& .agents/check.ps1
```

Exit `0` = `overall: "pass"`. Exit **`2`** = a gate failed, **or** no gate ran at all (`overall: "no_gates"`) — a delegated code change must never be accepted with zero checks executed. Exit `1` bad arguments, `3` the log file could not be written. Read `log_file` for the full output.

**2. Collect the Guardrail evidence from the diff**, naming the scope the prompt actually authorised:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/verify_delegation.py --base HEAD `
  --expect-files {file the task was supposed to change} `
  --forbid-outside {directory the task was scoped to}
```

It reports `deletions`, `placeholders`, `weakened_tests`, `out_of_scope_files`, `missing_expected_files`, `scope_empty`, and the captured diff at `diff_file`. Exit `0` = nothing actionable and no violated expectation — deletions alone land here, reported but not actionable on their own, and exit `0` is still not an accept. Exit **`2`** = an actionable finding (`placeholders`, `weakened_tests`) or a violated expectation (`out_of_scope_files`, `missing_expected_files`, `scope_empty`). Use `--base <pre-delegation ref>` when Codex committed its work; the default `HEAD` covers the usual uncommitted case.

**3. Read the diff and decide.** `verdict` is always `needs-review` and there is no verdict that means "accepted" — deliberately. The pattern list is heuristic (a legitimate test deletion exists, and a `TODO` in a docstring is not a stub), and only you know what the prompt authorised. Reject the completion when the diff shows any of:

- tests deleted, skipped (`@pytest.mark.skip`), or weakened (assertions removed or loosened) to make the suite pass;
- exceptions silently swallowed (`except: pass` or equivalent) to hide failures;
- hard-coded return values substituted for real logic — the one Guardrail item no script screens for, so it is listed under `not_automated` and only your read of the diff catches it;
- stub or placeholder completions where real logic was requested;
- files changed that the task never mentioned, or unapproved deletions.

**4. On failure, follow the re-delegate-once protocol** (`cli-execution.md` (c)): report the specific failures with evidence, re-delegate **once** with the original prompt plus the failure context appended, and if the second attempt also fails verification, **halt** and require explicit user approval before proceeding. Never patch over a failed delegation silently.

### Sandbox Modes

| Mode | Sandbox | Use Case |
|------|---------|----------|
| Analysis | `read-only` | Design review, debugging, trade-off analysis |
| Implementation | `danger-full-access` | Implementation, fixes, refactoring |

The wrapper and project configuration both default to `read-only`. Pass `--sandbox workspace-write` only after implementation is approved. Use `danger-full-access` only when the approved task truly requires writes outside the workspace, and make that expansion explicit.

## Task Templates

Write the full prompt to the named file before invoking the wrapper. These
PowerShell commands assume the files already contain Objective, Constraints,
Relevant files, Acceptance checks, and Output format sections.

### Implementation Planning

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file .agents/logs/codex/prompt-plan.md --label plan --sandbox read-only
```

### Design Review

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file .agents/logs/codex/prompt-design-review.md --label design-review --sandbox read-only
```

### Debug Analysis

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/codex_consult.py --prompt-file .agents/logs/codex/prompt-debug.md --label debug --sandbox read-only
```

## Language Protocol

See `.agents/rules/language.md` (SSOT): ask Codex in English, receive in English, report to the user per that rule.

## Why Codex?

- **Deep reasoning**: Complex analysis and problem-solving
- **Planning expertise**: Architecture and implementation strategies
- **Code mastery**: Complex algorithms, optimization, debugging

## References

Detailed templates and patterns in `references/`:

- [agent-prompts.md](references/agent-prompts.md) — Prompt templates for specialized review agents (Architect, etc.)
- [code-review-task.md](references/code-review-task.md) — Prompt template for delegating code review to Codex
- [delegation-patterns.md](references/delegation-patterns.md) — Delegation decision flowchart and detailed patterns
- [refactoring-task.md](references/refactoring-task.md) — Prompt template for delegating refactoring to Codex
- [troubleshooting.md](references/troubleshooting.md) — Codex CLI troubleshooting (installation, auth, common errors)

Also `.agents/docs/CODEX_HANDOFF_PLAYBOOK.md` — the handoff templates `.agents/rules/codex-delegation.md` points at, kept there because they are shared with non-Codex handoffs.
