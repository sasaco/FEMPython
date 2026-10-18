# Quality Review: Agents Repository Alignment

## Verdict

**APPROVED (post-fix)** — all three High, three Medium, and two Low findings
from the initial review have been resolved. No Critical/High findings or lower
severity residuals remain in the re-reviewed repository-alignment scope. The
historical findings below are retained as the change record; current evidence
is in `## Resolution Verification`.

## Review Scope

Reviewed the approved plan, the gathered patch, and the current agent-infrastructure
files under `AGENTS.md`, `.codex/`, and `.agents/`. The concurrent
`feature-result-contract-refactor-*` research and result-set/result-contract
Codex/design logs were explicitly excluded. Product code was not reviewed or
modified.

## Findings

### [High] The canonical PowerShell gate does not implement the output or exit-code contract consumed by active skills

- **Evidence:** `.agents/check.ps1:21-43` emits per-command prose with
  `Write-Host`; `.agents/check.ps1:210-215` emits only a prose summary and maps
  every gate failure to exit `1`. In contrast,
  `.agents/skills/team-execute/SKILL.md:371-380` requires a JSON object containing
  `overall`, `tools`, and `log_file`, with exit `2` for a failed or empty gate,
  exit `1` for bad arguments, and exit `3` for write failure. The same contract
  is repeated by `codex-system`, `feature`, `tdd`, `troubleshoot`, and
  `update-lib-docs`.
- **Observed behavior:** `& .agents/check.ps1 -AgentOnly` printed
  `Results: 8 passed, 1 failed, 1 skipped` and returned exit `1`; it emitted no
  top-level JSON payload. The failure in that run was the cache-absence gate,
  not a bad argument.
- **Impact:** workflow code and instructions cannot reliably distinguish bad
  invocation, gate failure, no-gate execution, or log-write failure. In
  particular, Phase 2 says to stop on exit `2`, but this implementation never
  returns `2`, and reports cannot quote the required `tools` object.
- **Recommended fix:** make `check.ps1` emit one structured JSON result with
  `ok`, `overall`, `tools`, `log_file`, and `artifacts`; restore the documented
  `0/1/2/3` meanings and either implement `-AllowNoGates` or remove that option
  from every active consumer. Add an execution-level test that parses stdout and
  asserts failure/no-gate exit codes, not a source-string presence test.

### [High] The active `simplify` gate still executes the deleted Bash verifier

- **Evidence:** `.agents/skills/simplify/simplify_gate.py:61` binds
  `VERIFY_SH` to `.agents/skills/_shared/verify.sh`, and
  `.agents/skills/simplify/simplify_gate.py:90-126` runs it through
  `command = ["bash", ...]` and parses its JSON. This change deletes
  `.agents/skills/_shared/verify.sh` and declares PowerShell canonical.
- **Impact:** the retained `simplify` skill fails before recording its baseline
  on the supported Windows environment. The current residue tests miss it
  because `.agents/tests/test_shared_script_contract.py:80-87` scans Markdown
  references only, while the executable stale reference is in Python.
- **Recommended fix:** route `simplify_gate.py` through the canonical
  `.agents/check.ps1` contract (preferably the structured contract described
  above), update its messages and timeout/error handling, and add a regression
  test that scans executable active surfaces for deleted gate paths and Bash
  invocations.

### [High] The migration tool is not idempotent against the migrated live DESIGN document

- **Evidence:** `.agents/skills/init/migrate_repository_state.py:57` defines an
  English literal `CANONICAL_DESIGN_MARKER`; the only no-op guard at
  `.agents/skills/init/migrate_repository_state.py:349-353` depends on that
  literal. The migrated live document instead begins its project description in
  Japanese at `.agents/docs/DESIGN.md:12`, so it does not contain the marker.
- **Observed behavior:** an in-memory, no-write probe of the current files
  returned `state_noop=True` but `design_noop=False`; the candidate DESIGN grew
  from 10,327 to 16,193 characters. This violates the approved plan's explicit
  re-run/no-op requirements at
  `.agents/docs/plans/agents-repository-alignment.md:67` and `:96`.
- **Impact:** rerunning the documented migration against the current repository
  would rewrite and expand a valid design document, risking duplication or loss
  of concurrently preserved decisions.
- **Recommended fix:** use a stable machine-readable migration marker or a
  structural migrated-state check rather than natural-language prose. Add a
  regression that loads the actual post-migration STATE/DESIGN pair (including
  preserved concurrent decisions), runs both text migrations again, and asserts
  byte-for-byte equality.

### [Medium] The mandatory product-scope isolation gate was not implemented

- **Evidence:** the approved plan requires a baseline-aware union of committed,
  staged, unstaged, and untracked paths to fail on newly changed product paths
  (`.agents/docs/plans/agents-repository-alignment.md:100`). The canonical gate's
  required agent checks at `.agents/check.ps1:179-182` cover references,
  foreign text, caches, and `git diff --check`, but contain no scope comparison.
  `.agents/tests/test_shared_script_contract.py:111-127` likewise checks only
  string presence and omits scope isolation.
- **Impact:** `.agents/check.ps1` can pass even when an alignment-only change
  accidentally adds or modifies files under product directories, defeating a
  stated acceptance condition.
- **Recommended fix:** add a baseline/scope input or recorded baseline artifact,
  compute the full changed-path union exactly as the plan specifies, and fail
  the agent gate when a newly introduced path falls under the product roots.
  Cover staged and untracked fixtures in tests.

### [Medium] Invalid declared manifests are reported only as warnings, so stack detection can still return `ok: true`

- **Evidence:** parse failures in declared Python and npm manifests are converted
  to `evidence.warnings` at `.agents/skills/init/detect_stack.py:490-501` and
  `:529-543`. `build_report` at `.agents/skills/init/detect_stack.py:670-678`
  sets failure only for bootstrap markers or repository-TOML declaration errors;
  evidence parse warnings never enter `failed`. The approved plan explicitly
  requires a broken-manifest regression at
  `.agents/docs/plans/agents-repository-alignment.md:38`, but
  `.agents/tests/test_detect_stack.py` covers malformed repository TOML and a
  missing manifest, not malformed declared `pyproject.toml`/`package.json`.
- **Impact:** `/init` can treat a repository with an unreadable product manifest
  as a valid detected stack and derive incomplete dependency/tool evidence.
- **Recommended fix:** distinguish manifest parse errors from advisory evidence
  warnings and propagate parse errors into the detector's contract failure;
  add invalid TOML, invalid JSON, and non-object JSON fixtures.

### [Medium] Core catchup/checkpointing paths still default to the removed Claude runtime surface

- **Evidence:** `.agents/INDEX.md:31` states that no Claude discovery surface is
  active, but `.agents/skills/catchup/collect_repo_state.py:44,741-746,883-886`
  and `.agents/skills/checkpointing/checkpoint.py:54-56,329-332,1051-1054`
  default to reading `~/.claude/teams` and `~/.claude/tasks`.
- **Impact:** Codex-first catchup/checkpointing can import stale or unrelated
  Claude-team data from the user's global home while omitting current Codex
  collaboration state. This also contradicts the approved Codex-focused
  runtime decision.
- **Recommended fix:** make in-repository work logs the default source. If an
  external runtime history remains useful, expose it only through an explicit,
  runtime-neutral option and document it as optional rather than defaulting to
  `~/.claude`.

### [Low] One active review template still contains mojibake

- **Evidence:** `.agents/skills/codex-system/references/code-review-task.md:21`
  contains `窶・` where the surrounding templates use an em dash.
- **Impact:** copied encoding damage remains in a prompt users and nested reviews
  may reproduce.
- **Recommended fix:** replace it with the intended UTF-8 punctuation and add
  this byte sequence to the active-surface mojibake regression set.

### [Low] The registry advertises an optional hooks directory whose files were all deleted

- **Evidence:** `.agents/INDEX.md:23` lists `.agents/hooks/` as an active registry
  entry, while this change deletes every tracked file under that directory.
  Git does not preserve empty directories, so the path will not exist in a clean
  checkout.
- **Impact:** the final infrastructure map does not exactly describe the retained
  set and sends maintainers to a nonexistent surface.
- **Recommended fix:** remove the registry row, or add and document a real
  retained hook entrypoint if hooks are intentionally part of the supported
  design.

## Positive Observations

- `AGENTS.md`, `.agents/repository.toml`, and the detector agree on the Python,
  Angular, .NET, and local-smoke boundaries and use repository-relative
  PowerShell-compatible commands.
- Claude pseudo-link files, runtime-specific agents, inactive Antigravity
  workflows, Bash gates, and copied TickReplay state/design content were removed
  without changing product source files.
- Active Markdown links resolved in the reviewed surfaces; the only missing
  link-shaped targets found were illustrative checkpoint paths inside a fenced
  format example.
- STATE retains the real FrameWebforJS working blocks, DESIGN retains the
  concurrent result-contract decisions, and `PROGRESS.md` remains populated.

## Validation Evidence

- `detect_stack.py --project-root .`: exit `0`, correctly reports Python/Angular/.NET,
  four declared components, five gates, and no CI workflows.
- `& .agents/check.ps1 -AgentOnly`: `8 passed, 1 failed, 1 skipped`; failure was
  cache absence because `.pyc` files appeared during the parallel review. The
  command's prose-only output independently confirms the High contract finding.
- In-memory migration probe: `state_noop=True`, `design_noop=False`, DESIGN
  length `10327 -> 16193`; no repository file was written.
- Foreign project reference scan of STATE/DESIGN/PROGRESS: no matches.

## Resolution Verification

Re-reviewed the current implementation after the fixes, with
`PYTHONDONTWRITEBYTECODE=1`. The concurrent result-contract work remained
excluded.

| Original finding | Status | Post-fix evidence |
|---|---|---|
| [High] `check.ps1` output/exit contract | Resolved | `.agents/check.ps1:32-48,118-158,653-665` now emits one parseable JSON object with `ok`, `overall`, `tools`, `log_file`, `warnings`, and `artifacts`, and implements exits 0/1/2/3. A direct `-AgentOnly` run returned exit `0`, `ok=true`, `overall=pass`, 10 passing tools, and one intentionally skipped product-gate entry. `.agents/tests/test_shared_script_contract.py:229-333` executes the ListGates, exit 1, exit 2, and exit 3 contracts. |
| [High] `simplify` invokes deleted Bash gate | Resolved | `.agents/skills/simplify/simplify_gate.py:90-150` selects `pwsh`/`powershell`, invokes `.agents/check.ps1`, and parses its JSON. No active Python surface contains `verify.sh` or a Bash gate invocation; executable regression coverage is at `.agents/tests/test_shared_script_contract.py:336-385`. |
| [High] live DESIGN migration is non-idempotent | Resolved | `.agents/skills/init/migrate_repository_state.py:60-68,372-393` uses a stable marker plus a narrow structural fallback. A direct no-write probe on the latest live documents, including the concurrent DESIGN clarification, returned `state_noop=True` and `design_noop=True`. `.agents/tests/test_migrate_repository_state.py:317-329` locks byte-for-byte live-document idempotence. |
| [Medium] product-scope isolation absent | Resolved | `.agents/check.ps1:318-504,574-601` validates repository-relative allowlists and combines baseline-committed, unstaged, staged, and untracked paths. The direct AgentOnly result reported `scope-isolation: pass`; `.agents/tests/test_shared_script_contract.py:249-291` verifies rejection and explicit allowlisting across all path states. |
| [Medium] malformed declared manifests are warnings | Resolved | `.agents/skills/init/detect_stack.py:469-478,514-580,705-712` separates fatal evidence errors from advisory warnings and makes manifest evidence fatal. TOML/JSON and CLI regressions are covered at `.agents/tests/test_detect_stack.py:229-282`. |
| [Medium] implicit `~/.claude` history | Resolved | Catchup and checkpointing now expose runtime-neutral `--external-agent-history` with default `None` (`collect_repo_state.py:806-816,955-962`; `checkpoint.py:325-335,1054-1061`). In-repo work logs remain the default; `.agents/tests/test_repository_config_security.py:176-208` verifies both default and opt-in behavior. |
| [Low] mojibake in review prompt | Resolved | `.agents/skills/codex-system/references/code-review-task.md:21` now contains a valid UTF-8 em dash; the active-surface scan found no original mojibake sequence. |
| [Low] nonexistent hooks registry entry | Resolved | `.agents/INDEX.md:12-23` no longer registers `.agents/hooks/`; the retained registry matches tracked surfaces. |

Additional verification:

- Canonical AgentOnly gate: exit `0`, JSON parsed successfully,
  `overall=pass`, 10 pass / 1 skipped, no warnings.
- Agent-infrastructure tests executed by that gate: **57 passed**.
- Direct `-AgentOnly -ListGates`: exit `0`, `overall=pass`, scope isolation
  passed, nine agent gates listed without execution, product gates skipped.
- Direct invalid-project-root check: exit `1`, `overall=bad_args`, parseable JSON.
- Agent cache check after re-review: 0 `.pyc`, 0 `__pycache__` directories.
- `git diff --check`: passed as part of the canonical gate.

### Final Verdict

**APPROVED.** No unresolved Critical, High, Medium, or Low quality findings
remain from the original review.
