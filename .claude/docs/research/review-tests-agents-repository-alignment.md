# Test Review — `.agents` Repository Alignment

Date: 2026-09-18  
Reviewer: Test Reviewer  
Scope: approved repository-alignment plan and its detector, test runner, migration,
document, hygiene, and PowerShell gate changes. Concurrent result-contract/result-set
research, implementation, and logs are excluded.

## Verdict

**Approved after re-review.** No Critical or High findings remain. All three
original High findings and both original Medium findings have effective fixes and
green regressions. Two residual Low coverage gaps are recorded under Resolution
Verification; neither blocks approval. Coverage was not measured.

## Review Scope

- Approved plan: `.agents/docs/plans/agents-repository-alignment.md`.
- Patch under review: `.agents/logs/review-diff-agents-repository-alignment.patch`,
  filtered to repository-alignment changes only.
- Detector/config: `.agents/repository.toml`, `detect_stack.py`, and
  `test_detect_stack.py`.
- Gate/test runner: `.agents/check.ps1`, `run_tests.py`, `test_run_tests.py`, and
  `test_shared_script_contract.py`.
- State migration: `migrate_repository_state.py` and its focused tests.
- Documentation and hygiene gates: state/design/plan contracts, live-reference,
  foreign-reference, mojibake/residue, cache, deletion, and scope-isolation checks.
- Coverage: not measured. No fresh line/branch coverage artifact was available.

## Findings

- [High] `.agents/skills/init/migrate_repository_state.py:355` —
  `migrate_design_text` indexes only headings matching `REQUIRED_DESIGN_PREFIXES`
  and then renders only `DESIGN_SECTION_BODIES` (lines 362-369). Any valid,
  unrelated top-level DESIGN section is silently discarded. The preservation test
  at `.agents/tests/test_migrate_repository_state.py:135` exercises extra rows and
  notes only inside known sections, so it cannot detect this data loss. An in-memory
  probe with `## Custom Extension` / `KEEP-ME` reproduced both being removed.

- [High] `.agents/skills/init/migrate_repository_state.py:456` — Expected hashes
  are checked when the two files are first read, but neither target is re-read or
  rehashed immediately before `_atomic_replace_pair` at line 489. A concurrent
  edit made after the initial checks is overwritten, and the staged backup contains
  the earlier bytes. The existing mismatch/rollback tests at
  `.agents/tests/test_migrate_repository_state.py:156` and `:171` do not exercise
  this commit-time race. A replace-hook probe reproduced loss of concurrent STATE
  content.

- [High] `.agents/check.ps1:140` — The canonical gate runs tests, detector,
  document contracts, reference/foreign/cache predicates, and `git diff --check`
  through line 182, but never implements the baseline-versus-working-tree scope
  comparison required by plan line 100 and listed as mandatory at line 106.
  `.agents/tests/test_shared_script_contract.py:111` likewise asserts no scope-gate
  marker or behavior. Out-of-scope product changes can therefore pass this gate.

- [Medium] `.agents/check.ps1:1` — There is no dry-run/list-gates mode, while the
  product loop at lines 197-203 immediately invokes every non-optional command.
  The plan requires a dry run that enumerates Python, Angular, and .NET without
  starting unavailable tools. `.agents/tests/test_run_tests.py:47` covers prefix
  selection, rebasing/node IDs, no-gate reporting, and one failing subprocess, but
  has no canonical-gate dry run, successful CLI run, or missing-tool execution
  case. This leaves the approved command-enumeration and tool-missing behavior
  unverified.

- [Medium] `.agents/tests/test_detect_stack.py:154` — The fallback exclusion test
  creates `dist` and `vendor` manifests but asserts absence only for
  `node_modules`, `rxfire`, `paramquery`, and `local-tools` (lines 181-183).
  `.git`, `.venv`, `bin`, `obj`, cache directories, submodules, and the configured
  maximum-depth boundary are not exercised. The implementation contains these
  exclusions, but a regression in much of the approved exclusion set would not
  fail the suite.

- [Low] `.agents/check.ps1:79` — The live-reference predicate recognizes only
  `.agents/skills/*.(py|ps1|sh)` and `.agents/check.*` text, and excludes plans,
  research, reviews, checkpoints, and logs. The companion tests at
  `.agents/tests/test_shared_script_contract.py:80` validate deleted gates and the
  shared README only. General Markdown links, skill references, and produced work
  logs are not part of the canonical gate, despite the plan's all-link/reference
  requirement. State, DESIGN, and plan contracts themselves are correctly gated.

## Test Execution Results

- Agent suite: 28 passed, 0 failed in 1.85s.
- `.agents/check.ps1 -AgentOnly`: 8 passed, 1 failed, 1 skipped. The one failure
  was the cache-absence gate, which found three `.agents/**/__pycache__`
  directories and seven `.pyc` files. The gate detects the condition correctly;
  final integration must delete caches after all Python runs and rerun it.
- Focused migration probes: 2 negative probes reproduced 2 failures (unknown H2
  preservation and commit-time hash race).
- Lead-reported product evidence: corrected five Python cases passed (5/5); .NET
  passed; full corrected Python suite was still running at handoff; Angular
  failures were classified as pre-existing product configuration failures.
- Coverage: not measured.

## Passing Coverage and Gates

- Detector positive config, malformed-config/no-fallback, missing manifest,
  bounded fallback basics, tracked GitHub workflow, and Codex bootstrap checks pass.
- The cwd-sensitive Python command and repo-root-to-component target rebasing retain
  pytest `::node` suffixes; the lead's five exact corrected product cases passed.
- Migration tests cover recognized foreign-content removal, preservation inside
  canonical sections, initial hash mismatch, second replace failure/rollback,
  idempotence, strict UTF-8, CRLF preservation, malformed documents, and dry-run
  source non-mutation.
- STATE, DESIGN, and plan contracts; detector JSON; foreign-reference scan;
  active-residue/mojibake checks; deletion checks; and `git diff --check` passed in
  the agent-only run. Cache absence did not pass, and scope isolation was not run.

## Recommended Additions

1. Preserve or explicitly reject unknown DESIGN H2 sections and add an exact
   preservation test.
2. Recheck both expected hashes immediately before commit and add a concurrent
   modification test between staging and replace.
3. Implement and behavior-test the approved scope-isolation gate.
4. Add a non-executing gate-list/dry-run mode plus successful-command and
   missing-tool cases.
5. Parameterize fallback exclusions, submodule pruning, and depth boundaries;
   add Markdown/skill-reference and work-log contract gates.

## Resolution Verification

Final verdict: **Approved**. No unresolved Critical or High finding remains.

- **Resolved — original High, unknown DESIGN H2 loss.**
  `.agents/skills/init/migrate_repository_state.py:397` now preserves unrelated,
  foreign-free H2 chunks as complete normalized-EOL chunks and refuses an unknown
  section containing recognized copied-project content. Regressions at
  `.agents/tests/test_migrate_repository_state.py:157` and `:175` verify exact
  preservation and safe refusal.

- **Resolved — original High, commit-time race.** The migration holds an
  OS-backed inter-process transaction lock across read/validate/stage/replace/
  rollback and `_unchanged_before_commit` re-reads and byte-compares both targets
  immediately before the first replace. The regression at
  `.agents/tests/test_migrate_repository_state.py:230` mutates STATE after staging
  and verifies the concurrent bytes remain while DESIGN is not replaced. Rollback
  recovery is additionally covered at line 257.

- **Resolved — original High, scope isolation.** `.agents/check.ps1:449` unions
  committed baseline changes, unstaged changes, staged changes, and untracked
  non-ignored paths, filters product roots, and reports allowed/disallowed paths in
  JSON. `.agents/tests/test_shared_script_contract.py:249` proves rejection across
  committed, staged, unstaged, and untracked product paths and proves explicit
  allowlisting permits the same set.

- **Resolved — original Medium, ListGates dry run.** `.agents/check.ps1:603`
  lists agent and product commands without invocation. The fixture at
  `.agents/tests/test_shared_script_contract.py:100` deliberately declares three
  nonexistent Python/Angular/.NET executables; the test at line 229 returns pass
  and `listed` for all three, proving unavailable tools are not started.

- **Resolved — original Medium, detector exclusion/depth coverage.**
  `.agents/tests/test_detect_stack.py:156` now covers every configured generated/
  cache/vendor exclusion, repository-specific vendor paths, `.gitmodules`
  submodules, embedded git worktrees, the inclusive depth-four boundary, and
  depth-five pruning with explicit positive and negative assertions.

- **Remaining Low — executed missing-tool/success runner branches.** The
  `ListGates` regression proves missing tools are not invoked during dry-run, but
  `test_run_tests.py` still has no successful end-to-end CLI case and no direct
  assertion for a non-dry-run command-not-found payload. Existing failure and
  no-gate cases cover adjacent behavior. This is a test-depth improvement, not a
  release blocker.

- **Remaining Low — broad reference and work-log gate coverage.**
  `.agents/check.ps1:506` still resolves only selected `.agents` script references,
  and the canonical gate does not validate all Markdown links/skill references or
  every produced work log. Deleted-gate references, active residue/mojibake, shared
  README entries, and current reviewer/implementer logs are separately validated,
  so this remains defense-in-depth rather than an unresolved functional defect.

Final evidence:

- Fresh reviewer run with `PYTHONDONTWRITEBYTECODE=1`: **57 passed in 8.71s**.
- Lead-provided final `.agents/check.ps1 -AgentOnly` JSON: pass.
- Lead-provided Python product suite: **3249 passed**.
- Lead-provided .NET gate: pass.
- Angular failures remain unchanged pre-existing product configuration failures;
  no repository-alignment regression was identified.
- Coverage: not measured.
