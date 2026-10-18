# Changing the Main Agent

Read this runbook only when the user explicitly asks to change the main
runtime. Codex is the repository default. The active selection is recorded in
`.agents/STATE.md` under `## Main Agent`.

## Meaning of Main Agent

The main agent owns user interaction, task decomposition, routing, authority
boundaries, result integration, and the final response. Another runtime is
available only when its integration is explicitly installed and maintained.

## Invariants

- `.agents/` remains the shared source of truth and root `AGENTS.md` remains the
  concise discovery contract.
- Rules, skills, state, and docs are not copied into runtime-native directories.
- Machine-readable settings stay in each runtime's official configuration path.
- A runtime change must not silently broaden permissions.
- Codex keeps `approval_policy = "never"` and a read-only default unless the
  user explicitly approves a different repository policy.

## Change Procedure

1. Confirm the requested runtime and whether the change is session-only or a
   new repository default. Treat an unqualified request as session-only.
2. Verify official discovery/configuration requirements. Do not invent a
   compatibility surface for an uninstalled runtime.
3. Confirm the target can read `AGENTS.md`, `.agents/STATE.md`, and the relevant
   `.agents/rules/` and skills.
4. For a repository-default change, update `## Main Agent` in
   `.agents/STATE.md`, the default statement in `AGENTS.md`, and the minimum
   native configuration required by the target. A session-only change modifies
   no tracked file.
5. Translate permissions and sandbox semantics using least privilege. Record
   unavoidable differences in `.agents/docs/DESIGN.md`.
6. Run the validation below and inspect the final diff.

## Validation

- Start the target runtime in a disposable session and confirm that it loads
  the root contract and shared context.
- Invoke one shared skill without copying its files into a runtime directory.
- Run `& .agents/check.ps1` and the relevant component tests.
- Inspect native permission settings, discovery paths, and changed files.

## Rollback

Restore the previous `## Main Agent` value and native configuration from
version control or a backup, then rerun the same validation.
