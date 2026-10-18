# Security Review: Agents Repository Alignment

## Verdict

**Approved.** Final post-fix verification found no remaining Critical, High,
Medium, or Low security issue. The migration transaction, repository-config
path boundaries, pytest-target confinement, and scope-isolation gate now
implement the requested controls, including non-ASCII product paths when the
repository or user configuration enables Git path quoting.

This review covers only the implementation of
`.agents/docs/plans/agents-repository-alignment.md`. Concurrent
`feature-result-contract-refactor-*` research and result-set/result-contract
Codex/design logs are explicitly excluded.

## Original Findings (Pre-Fix)

### [High] The migration hash guard is not checked at commit time

- **Evidence**: `.agents/skills/init/migrate_repository_state.py:456-459`
  reads both documents and validates their expected hashes once. Candidate
  generation and temporary-file staging then occur before
  `.agents/skills/init/migrate_repository_state.py:425-427` replaces each live
  target without re-reading it or holding an exclusive lock.
- **Impact**: another agent or editor can update `STATE.md` or `DESIGN.md` after
  the initial hash check but before `os.replace`. The migration then silently
  overwrites that uncommitted update. This is a realistic failure mode in this
  repository because these documents are shared multi-agent state and a
  concurrent `DESIGN.md` change already occurred during this implementation.
- **Recommended fix**: stage and validate both candidates, then obtain an
  inter-process transaction lock and re-read both live targets immediately
  before the first replace. Abort if either byte sequence differs from the
  originals. Keep the lock through both replaces and any rollback. Add a test
  that mutates one target after staging and proves that neither document is
  replaced.

### [Medium] A failed rollback destroys the recovery copies

- **Evidence**: `.agents/skills/init/migrate_repository_state.py:429-438`
  detects and reports rollback failures, but the unconditional `finally` at
  `.agents/skills/init/migrate_repository_state.py:440-443` then unlinks every
  `.backup` file, including a backup that could not be restored.
- **Impact**: after a second-file replace failure plus a first-file rollback
  failure, the tool can leave a partially migrated pair and remove the only
  recovery copy it created. An unlink error can also mask the original
  transaction failure.
- **Recommended fix**: delete a backup only after that target was either never
  replaced or was successfully restored. On rollback failure, retain the
  backup and include its exact path in the error payload. Make cleanup
  best-effort without replacing the primary exception, and test the
  rollback-failure branch.

### [Medium] Repository configuration paths are not consistently confined

- **Evidence**: `detect_stack.py` rejects lexical absolute/`..` paths at
  `.agents/skills/init/detect_stack.py:95-101`, but its existence and parsing
  checks follow symlinks at `.agents/skills/init/detect_stack.py:146-179`.
  More importantly, `.agents/skills/catchup/collect_repo_state.py:608-614`
  accepts manifest strings without the detector's validation and later reads
  them and component README paths at
  `.agents/skills/catchup/collect_repo_state.py:644-700`.
  `.agents/skills/update-lib-docs/lib_inventory.py:652-665` has the same
  unvalidated join and subsequently reads adjacent lockfiles at
  `.agents/skills/update-lib-docs/lib_inventory.py:759-777`.
- **Impact**: a modified `repository.toml` can use an absolute path, `..`, or a
  repository-local symlink to make catch-up/library tooling read manifests,
  scripts, dependency specifications, lockfiles, or README command blocks from
  outside the repository. Those values can then enter generated reports and
  model context, potentially exposing local private package URLs or other
  machine-specific data.
- **Recommended fix**: centralize repository-config parsing. For every declared
  manifest, project, working directory, and derived lockfile, resolve the path
  and require `resolved.relative_to(project_root.resolve())` to succeed; reject
  symlink escapes. Have all consumers use the same validated representation
  rather than re-parsing the TOML independently. Add absolute, `..`, and
  symlink-escape tests for all consumers.

### [Medium] `run_tests.py` can execute pytest targets outside the repository

- **Evidence**: `.agents/skills/_shared/run_tests.py:120-125` accepts absolute
  paths and joins relative paths without confinement. Validation at
  `.agents/skills/_shared/run_tests.py:425-429` checks only existence, and the
  resulting argument is passed to pytest at
  `.agents/skills/_shared/run_tests.py:462-491`.
- **Impact**: `--target ..\outside\test_payload.py`, an absolute target, or a
  repository symlink to an external Python test is imported and executed by
  pytest. This exceeds the helper's repository-scoped contract if a target is
  derived from untrusted instructions or malformed automation.
- **Recommended fix**: resolve the filesystem portion before `::node-id` and
  require it to remain under `project_root.resolve()`, including after symlink
  resolution. Continue allowing `../.agents/...` only in the command rendered
  relative to the `FrameWeb` component after the already-confined repository
  target has been validated. Add traversal, absolute-outside, and symlink
  regression tests.

### [Low] The canonical gate omits the approved product-scope isolation check

- **Evidence**: the approved plan requires a baseline-aware product-path scope
  gate at `.agents/docs/plans/agents-repository-alignment.md:100`. The actual
  gate sequence in `.agents/check.ps1:142-182` runs tests, detector/document
  contracts, reference/foreign/cache checks, and `git diff --check`, but no
  changed-path isolation check. The contract test's required list at
  `.agents/tests/test_shared_script_contract.py:111-126` likewise does not
  assert one.
- **Impact**: a future agent-infrastructure cleanup can modify or delete product
  files and still pass `-AgentOnly`; default product tests are not a substitute
  for authorization/scope enforcement.
- **Recommended fix**: implement the approved baseline-aware union of committed,
  staged, unstaged, and untracked paths, fail on newly introduced product paths,
  and add a fixture test covering both an allowed pre-existing product change
  and a newly introduced out-of-scope product change.

## Resolution Verification

### Resolved: migration commit-time locking and recheck

- `.agents/skills/init/migrate_repository_state.py:516-550` acquires one stable
  OS-backed inter-process lock, and `migrate_repository` holds it from the
  initial reads through the complete transaction at
  `.agents/skills/init/migrate_repository_state.py:665-717`.
- After both candidates and backups are staged, the implementation re-reads
  both live targets and compares their exact bytes immediately before the first
  replace at `.agents/skills/init/migrate_repository_state.py:557-574,598-607`.
- `.agents/tests/test_migrate_repository_state.py:229-253` mutates STATE after
  staging and proves the commit aborts without replacing either target.
- **Status**: the original High finding is resolved.

### Resolved: rollback backup retention

- Failed restores add the exact backup to `retained`, expose it through
  `MigrationError.recovery_paths`, and skip its cleanup at
  `.agents/skills/init/migrate_repository_state.py:610-649`. Cleanup failures
  are attached as notes instead of replacing the primary transaction error.
- `.agents/tests/test_migrate_repository_state.py:256-298` forces a second-file
  write failure, first-file rollback failure, and cleanup failure, then verifies
  that the recovery backup remains byte-exact and its path is reported.
- **Status**: the original Medium rollback finding is resolved.

### Resolved: repository-config and derived-path confinement

- The shared validator rejects absolute and parent-traversal declarations, then
  resolves the physical target and requires it to remain below the resolved
  repository root at `.agents/skills/_shared/repository_config.py:22-70`.
- Detector reads declared Python and Node manifests only after revalidation at
  `.agents/skills/init/detect_stack.py:518-575`; catchup validates manifests,
  working directories, projects, and documentation paths at
  `.agents/skills/catchup/collect_repo_state.py:625-652,696-755`; inventory
  validates the same declarations and adjacent lockfiles at
  `.agents/skills/update-lib-docs/lib_inventory.py:669-695,817-824`.
- Absolute, `..`, symlink-escape, and adjacent-lockfile escape regressions are
  covered by `.agents/tests/test_detect_stack.py:285-354` and
  `.agents/tests/test_repository_config_security.py:52-152`.
- **Status**: the original Medium configuration-path finding is resolved.

### Resolved: pytest target confinement

- `.agents/skills/_shared/run_tests.py:120-135,438-445` strips the pytest node
  selector, resolves the filesystem target, requires it to remain below the
  repository root, and performs this validation before runner selection or
  subprocess execution. Configured component rebasing reuses the confined
  physical path at `.agents/skills/_shared/run_tests.py:240-274`.
- `.agents/tests/test_run_tests.py:99-153` covers safe `::node` rebasing plus
  traversal, absolute-outside, and repository-symlink escapes.
- **Status**: the original Medium pytest-target finding is resolved.

### Resolved: product-scope isolation

- `.agents/check.ps1:449-503,601` now unions committed changes since an optional
  baseline, unstaged changes, staged changes, and untracked files, then fails
  unapproved product paths.
- `.agents/check.ps1:405-446` routes every changed-path Git query through
  `Invoke-GitPathList`, which supplies per-command
  `-c core.quotePath=false` at line 411. The gate therefore receives literal
  non-ASCII paths even when repository or user Git configuration sets
  `core.quotePath=true`, without mutating that configuration.
- `.agents/tests/test_shared_script_contract.py:249-297` forces
  `core.quotePath=true`, creates an untracked Japanese-named file under
  `FrameWebforJS`, proves the gate rejects it, and proves only the explicit JSON
  allowlist changes that result to pass. The same fixture continues to exercise
  committed, staged, unstaged, and untracked path sources.
- **Status**: both the original missing-gate finding and the later Low
  non-ASCII-path residual are resolved.

### Final verdict

**Approved.** Critical: 0; High: 0; Medium: 0; Low: 0. No findings remain.

## Positive Controls Confirmed

- `.codex/config.toml:6-11` keeps `approval_policy = "never"` and defaults to
  `sandbox_mode = "read-only"`; write-capable nested Codex work remains an
  explicit sandbox override.
- `.agents/check.ps1:21-42` executes argv arrays with PowerShell's call operator,
  not interpolated command strings. Repository gate commands are trusted code
  by design, and no shell metacharacter expansion is introduced.
- `.agents/repository.toml` contains no credentials and uses explicit argv
  arrays. No reviewed active code automatically installs global tools; the two
  remaining global-install strings are informational hints in the explicitly
  invoked legacy external-CLI compatibility wrapper.
- Intended Claude pseudo-links, runtime agent definitions, Bash gates, and
  Antigravity workflow files are absent. The product component diff is empty;
  deletions are limited to the approved agent-infrastructure paths.

## Verification

- With `PYTHONDONTWRITEBYTECODE=1`, the focused migration, detector,
  repository-path, runner, and PowerShell-contract suite passed: **57 passed**.
- The final focused PowerShell contract rerun passed: **14 passed**, including
  the forced-`core.quotePath=true` Japanese-path rejection/allowlist fixture.
- `& .agents/check.ps1 -AgentOnly` emitted one parseable JSON payload and exited
  `0` with `ok=true`, `overall="pass"`, `scope-isolation.status="pass"`, all
  required agent tools passing, product gates explicitly skipped, and no
  warnings. The gate inspected 129 changed paths and reported no product path.
- Active-surface scan found no automatic `npm install -g`, `pip install`,
  download/execute, or hard-coded secret behavior. The only install strings are
  non-executing hints in `_shared/cli_consult.py`.
