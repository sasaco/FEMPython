# Security Review: Transverse Analysis and Print Verification

## Summary

**PASS — no actionable security finding in the scoped verification work.** Critical: 0; High: 0; Medium: 0; Low: 0. This verdict covers local evidence handling and the verification harness, not structural correctness, reference parity, release suitability, or the unchanged product dependency graph.

## Review Scope

- Approved plan and `transverse-analysis-print-verification-report.md`; the nonempty review patch at `.agents/logs/review-diff-transverse-analysis-print-verification.patch`.
- Private run `.tmp/transverse-analysis-print-verification/20261015T182603/`: `preflight/freeze.py`, `preflight/closeout.py`, `harness/Program.cs`, `harness/Verification.csproj`, `harness/finalize_manifest.py`, `comparison/compare_pdfs.py`, `audit/checker.py`, `visual/render_pages.py`, and source manifest.
- Filesystem preservation, process/network boundaries, unsafe evaluation, secrets and output privacy. The existing pythonnet runtime was checked at `FrameWebforCS/calculation/PythonCalculationRuntime.cs:142`–`:153` and `:185`–`:235` to confirm the relevant in-process JSON/import/call path.
- No implementation edits, package changes, full builds, external uploads, or dependency advisory research were performed by this reviewer.

## Findings

None requiring remediation within the approved verification scope.

Evidence supporting the verdict:

- `preflight/freeze.py:53`–`:62` checks each original SHA-256 against the approved baseline, refuses an existing frozen destination, copies into the private run, and checks the copy. Original files are only read. `preflight/closeout.py:20`–`:25` checks both original and frozen bytes again.
- Independently recomputed SHA-256 values for all four external originals and all four frozen copies during review: **8/8 match** the approved manifest baselines. The manifest already records `closedUtc = 2026-10-15T09:40:13.426008+00:00` and empty `productDiffAfter`. An independent product `git diff --name-only HEAD` was also empty.
- `harness/Program.cs:68`–`:71` reads and verifies the frozen sample, then writes a copy to the selected local output directory. Fixed report names and paths are supplied by the trusted task harness; sample field values are not executed or used as output paths. Request/result parsing uses JSON APIs. No dynamic code evaluation or shell interpolation was introduced.
- `preflight/freeze.py:30`–`:31`, `preflight/closeout.py:26` and `:31`, and `harness/finalize_manifest.py` use fixed argument arrays for local `git`/`dotnet` queries, without a shell. The harness calls existing in-process pythonnet calculation and PDF generation. Reviewed scripts add no network client, upload call or HTTP print host.
- Comparer, arithmetic audit and renderer writes stay in their respective private run directories; no destructive delete or move exists in the reviewed scripts. The comparer exports the authorized sample's values and associations as evidence without a new public endpoint.
- No credentials, tokens, environment-file dumps or connection strings were found in the scoped plan, report or verification sources. Environment evidence contains versions, executable/assembly paths, culture and selected font names. Detailed requests, results, PDF text, renderings and exception diagnostics remain local evidence; they are not copied wholesale into the tracked report.
- `git check-ignore` confirms the canonical result is ignored by `.gitignore:14` (`.tmp/`). The same rule covers the private source copies and generated report evidence.

## Validation

- Read-only source and diff inspection; targeted searches for process/network, filesystem, evaluation and secret-bearing operations.
- Independent live original/frozen SHA-256 verification: 8 passed, 0 failed.
- Independent product diff: empty. Reviewed the lead's supplied agent gate evidence at `.agents/logs/check-20261015T093745732Z-16128.log`; did not rerun unrelated product tests.

## Remaining Limits

Private evidence includes the complete supplied model, reference PDFs, extracted text, generated results and local paths. Gitignore prevents ordinary Git inclusion; it does not encrypt these files, set a separate ACL, back them up or guarantee retention. No automatic retention/deletion policy was introduced. Evidence links in the tracked report therefore depend on preserving this local private run. This is an existing, disclosed evidence-retention limitation, not a new actionable security defect in this authorized local verification.

The review establishes no global absence of network traffic from the operating system or dependency tooling, and does not certify unchanged legacy printing packages. The scoped workflow introduces no sample-upload mechanism.
