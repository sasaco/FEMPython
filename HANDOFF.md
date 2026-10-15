# Handoff: Fix Transverse Analysis and Print Parity

Date: 2026-10-15
Repository: `C:\Users\sasai\Documents\structural-mcp\FEMPython`
Verification HEAD: `08aac8fd4b64b8513047adc575d87893a38fe818`

The user approved implementation on 2026-10-15. Input conversion, print display/provenance and report composition corrections are now implemented and tested. **Complete reference parity is still incomplete.** Continue from the maintained [correction plan](.agents/docs/plans/transverse-analysis-print-corrections.md) and [before/after report](.agents/docs/research/transverse-analysis-print-corrections-report.md); do not restart the completed input/display repairs. The previous PICKUP-export handoff is preserved byte-for-byte in [.agents/docs/research/handoff-pickup-node-result-exports-20261005.md](.agents/docs/research/handoff-pickup-node-result-exports-20261005.md).

## Latest implementation status

- User selected UPDATED K: references30/60/2portrait pages and confirmed identical CASE10 pile pressure distributions. OriginalJSON remains unchanged; corrected22-edit COPY SHA256`60dcb754088a58f2008696e76b982c787a61592fdd8fba426d47d40fc5751ad3` is at `.tmp/transverse-analysis-print-corrections/20261015T193610/residual-investigation/corrected-input-row-labels-layout.json`.
- ✅ Code-proven input/display/provenance/composer corrections, integer-mm stage rounding and bounded continuous foundation TABLE occurrences implemented with observed native TDD. Lead237 native tests passed,0fail/skip; coverage unmeasured. Internal-marker collision and mixed diagram duplicate rendering fixed; independent bounded reviews completed.
- ✅ Final shared stage `.tmp/transverse-analysis-print-corrections/20261015T193610/stages/final-9/`: all10cases once/no warnings,67nodes/11members, source stable; PDFs30/60/2pages. ALL4040rows associated:950basic/2850PICKUP/240displacement, missing/extra/ambiguous ALL0. Independent140400component audit and exact occurrence proof pass.
- **Exact reference parity remains incomplete:** numericvectors904/950basic,2470/2850PICKUP,173/240displacement; numericalcomponents56/553/122 differ. Selected PICKUP source IDs differ215rows; source-only member names absent. No arbitrary numerical/tie/name corrections were made.
- Existing baseline2Dvector-diagram case-key and station-node mapping defects are documented separately. New occurrence filter is native-tested at actualparser/render-listboundary; general diagramPDF correctness is NOT claimed.
- Final report/manifest link commands, hashes, row/token ledgers, tests, visual and cleanup evidence. Final8 is preserved; final9 removes reserved-marker collision without changing actual sample outputs. Earlier7extra/ambiguous counts are historical, now resolved. Both reference versions and all prior evidence remain preserved.

## Goal

Correct the current FrameWebforCS saved-document analysis and printing pipeline so the supplied transverse model reproduces the requested numerical results, case/member/node/station associations, PICKUP source selections, and report presentation of the three reference PDFs. Fix the causes in the application, not the verifier or reference data. Run a fresh end-to-end comparison after corrections and publish reproducible evidence.

## Constraints

### Mandatory working method from the user

1. **Create the correction plan first in `.agents/docs/plans/`, before changing product code.** Use a new plan for corrections; preserve the completed verification plan as historical evidence. A suitable English title is `Transverse Analysis Print Corrections`. Resolve the path with the shared workspace helper and use its returned `paths.plan_doc` verbatim:

   ```powershell
   uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/workspace.py --skill plan --title "Transverse Analysis Print Corrections" --create
   ```

   If that document already exists, read and revise it rather than overwriting it blindly. Include Goal, Constraints, Acceptance criteria, dependency-ordered tasks, test cases, open questions and verification commands.

2. **Update the same plan as soon as progress occurs.** Record status, new findings, design principles and their background/rationale, rejected approaches, Tips, commands/log paths, remaining risks and information another worker needs. Mark verified completed items with **✅**. Keep unfinished or blocked items visibly unfinished. A written implementation or a passing build alone is not proof that the relevant acceptance criterion passed.

3. **Implement with `.agents/skills/tdd/SKILL.md`.** Read that skill and follow Red → Green → Refactor, one small failure-driven change at a time. Record the failing assertion, minimal correction, passing run and retained regression evidence for each cycle. Do not implement first and add a test afterward. Follow its ownership/delegation and lead-verification requirements for nontrivial cycles.

4. **Unsupported speculation and assertions are prohibited.** Separate observed facts, source-proven transformations, hypotheses and unknowns. Every cause or completion claim needs file:line evidence, a reproducible test, or exact before/after artifacts. Say what is unproven. Do not claim all results match from HTTP/process success, test-suite success, a PDF signature, a few matching rows or arithmetic consistency alone.

### Scope and invariants

- Start with `AGENTS.md`, `.agents/skills/context-loader/SKILL.md`, `.agents/INDEX.md`, the rule read plan, STATE and DESIGN. Communicate with the user in Japanese. Use repository-root PowerShell and the locked component environments.
- Inspect current git status and relevant diffs. This verification's documentation is currently uncommitted; preserve it and all concurrent work. The verification made no product code changes.
- Primary correction areas are `FrameWebforCS/` and `FramePrintPDF/`. Investigate `FrameWeb/` when evidence locates an engine/input-contract issue there. Angular is useful evidence for the original input semantics; do not change Angular or `FrameGConverter/` as incidental cleanup.
- Preserve the canonical `AnalysisResultSet` contract and raw-unit/result boundary. Do not globally negate moments or multiply results by 1000 merely to match one PDF. Establish the intended unit, axis and sign conventions at each boundary, including rotations, before changing them. Keep existing section-force/CSV exports correct.
- Do not alter the supplied K: JSON/PDFs, frozen originals, reference values or this completed verification run. Do not hardcode this sample's IDs/results, synthesize missing cases as zeros, skip problematic rows, weaken tests, add arbitrary tolerances, or rename a mismatch as accepted.
- Put each new run in a new private directory with its own input/source hashes and commands. Detailed client artifacts stay local; no upload is authorized. `.tmp/` is gitignored, not an archive: preserve the original run directory.
- The source JSON is newer than the reference PDFs. Their historical input/software identity is unknown. Fix code-proven defects, then investigate any residual differences; do not invent historical equivalence or force output to match an unknown older model.

## Acceptance criteria

- [x] ✅ Notice-point arrays survive saved JSON → actual solver request → canonical topology → force aggregation → printing. The sample's three records are present, including member 5 at DIST 0.566, with appropriate labels/results.
- [x] ✅ Relative member-load positions preserve the lexical meaning of `"-0"` and negative offsets. CASE 10 load intervals continue as intended instead of all starting at the I end. Cover ordinary zero, negative zero, consecutive/nonconsecutive rows, changing members, and the supported load marks with evidenced expectations.
- [x] ✅ A documented and tested force/rotation/unit display contract maps canonical results to the reference-facing display without mutating canonical results. Both ordinary and COMBINE displacement display correct values and units. PICKUP selects signed extrema in the intended convention and retains the full correlated vector and correct source.
- [ ] Source expressions remain text: reference COMBINE/CASE relationships are legible and correct, not rendered as numeric digit sequences such as `12345.0000`. Case/member/end/station labels, notice/rigid boundaries and required extrema rows are represented correctly.
- [ ] Basic and PICKUP force reports, and base + COMBINE displacement reports, reproduce the requested case ordering, components, row multiplicity, provenance and page composition. COMBINE displacement does not repeat each ordinary combination vector six times. The reference geometry is A4 landscape, 842 × 595 pt; observed reference page counts are 15/45/4. Any remaining layout/page-count difference must be recorded, not silently waived.
- [x] ✅ Re-run the actual shared application pipeline, not Python alone or saved cached results. Compare all reference/current pages and all numerical rows, account for missing/extra/duplicate rows, resolve or explicitly retain ambiguous station-side mappings, and inspect rasterized output. Comparison is complete; complete reference parity remains open.
- [x] ✅ Regression tests include observed Red and Green runs for each corrected behavior; affected component tests/builds and the agent-infrastructure gate pass. AgentOnly uses the approved exact product-path scope and skips its product gates; separate native gates are recorded in the report. Coverage is unmeasured.
- [x] ✅ Deliver the maintained correction plan, changes/tests, new manifest, raw/projection results, generated PDFs, row diffs, visual evidence, and a before/after verification report. STATE and HANDOFF contain the explicit incomplete parity status and continuation evidence.

## Current Progress

### Completed verification — facts

- ✅ Approved verification plan executed: [.agents/docs/plans/transverse-analysis-print-verification.md](.agents/docs/plans/transverse-analysis-print-verification.md).
- ✅ Full report: [.agents/docs/research/transverse-analysis-print-verification-report.md](.agents/docs/research/transverse-analysis-print-verification-report.md). Read it and the security/quality/test review reports linked from it before planning repairs.
- ✅ Original run: `.tmp/transverse-analysis-print-verification/20261015T182603/`. Its `manifest.json` records input hashes, environment and final artifact inventory. All four external originals and four frozen copies matched their hashes after verification.
- ✅ Actual current application pipeline solved all 10 sample cases once, with no skipped cases or solver warnings. A separate known axial fixture checked `dx=0.0004` and stale saved-result exclusion.
- ✅ Reference/current PDFs: 15/30 basic-force pages, 45/60 PICKUP pages, 4/34 displacement pages. All 188 pages were extracted and rasterized; zero unaccounted decimal/placeholder tokens.
- ✅ Independent aggregation checks: 124,200 components and 20,700 correlated rows/source/provenance checks passed against the current canonical base. Maximum arithmetic delta was 2.2737367544323206e-13. This establishes aggregation consistency, not base-solver or reference correctness.
- ✅ Existing focused product tests: 56 passed (Headless 1, print projection 21, PDF 34); infrastructure 57 passed. Three independent evidence reviews passed. Product code coverage was not measured.
- This section records the original verification before corrections. Its defect list below is historical evidence; the latest status and correction report supersede its implementation status.

### Inputs

Input:
`K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json`

References:
- Basic section forces: `K:\レールウェイコンサルタント\26_ピット\計算書\03_analysis\05_線路直角方向の計算\基本.pdf`
- PICKUP section forces: `K:\レールウェイコンサルタント\26_ピット\計算書\03_analysis\05_線路直角方向の計算\PICK UP.pdf`
- Base + COMBINE displacement: `K:\レールウェイコンサルタント\26_ピット\計算書\03_analysis\05_線路直角方向の計算\変位量.pdf`

Input SHA-256: `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553`. Reference hashes are in the verification plan and run manifest. Frozen copies are in the original run's `preflight/inputs/` directory.

### Original problems, evidence, and first regression targets (baseline)

1. **Dropped notice-point array — source/data proven.** `FrameWebforCS/calculation/CalculationRequest.cs:40–42` copies optional sections only when they are JsonObject. The supplied notice_points is a three-record JsonArray, and the actual request omits it. Member 5 has only stations 0 and 1.132; 0.566 is missing. Use `audit/feature-stations.json`, saved input/request and `CalculationRequestTests.cs`. Test array/absent/empty/malformed handling and actual resulting station coverage, not just key presence.

2. **Negative-zero continuation — source/data proven.** `CalculationRequest.cs:282–286` converts L1 to numeric and tests `<0`; `"-0"` loses continuation semantics. Angular's `FrameWebforJS/src/app/components/input/input-load/input-load.service.ts:950–964` uses `sL1.includes("-")`. The actual CASE 10 request has 23 nonzero patches per pile all starting at zero, including 22 negative-zero continuation rows. Length is 23.95 but these intervals overlap within the first 2.2. Inspect `audit/case10-load-extents.json`. This is a concrete cause candidate for the large CASE 10 mismatch; no corrected solve was run to quantify causality.

3. **Displacement display conversion and rotation signs — observed; full contract still needs proof.** `PrintProjection.cs:391–397` passes raw values to `ResultDisg.cs`, which labels mm/mmrad and formats without scaling; COMBINE does likewise. CASE 1/node 1 Y is raw -0.0015542503589541191, printed -0.0016, reference -1.5543. Canonical metadata says units unspecified. Legacy display/docs support an m/rad interpretation, but do not declare all raw inputs SI without evidence. Multiplying rotation by 1000 would still have the opposite sign in the example. See `audit/mapping-unit-audit.md` and existing display formatters.

4. **Force signs and PICKUP selection — observed; locate the correct boundary before fixing.** CASE 1/member 3/J end DIST 0.881: reference M=-22.82 versus current +22.82, while S=-51.80 matches. PICKUP 1/M MAX/member 1/I end: reference COMBINE 6, M=10.41, S=21.45; current COMBINE 1, M=-7.40, S=15.30. Canonical I/J sign rules are explicit in `FrameWeb/src/fem/result_projection.py:192–199`; projection currently passes components unchanged. Test signed maxima/minima and correlated sources, not absolute-value approximations.

5. **CASE 10 large numerical difference — observed; rerun after input fixes.** CASE 10/member 10/DIST 0 N: reference 13.52 versus current 1538.53. CASE 10/node 1 Dx: reference printed 19.0393 versus current 0.3250; a universal ×1000 does not reconcile it. Its contribution reaches COMBINE 5/10 and PICKUP 5. Keep this regression distinct from generic display scaling.

6. **Provenance text/labels/stations — observed.** For COMBINE 1, reference expression is `1+2+3+4+5`, projected case source string is `1,2,3,4,5`, and PDF prints `12345.0000`. Separate weighted comb text also exists in the projection. Inspect `ResultDisgCombine.cs:238–239,:593` and `Printing/Comon/printManager.cs:67–77`; do not parse source identifiers as numeric measurements. PICKUP prints source COMBINE ID but omits constituent expression. Current basic captions use `1/Static/0`, force labels use S0... and omit reference member/end names, rigid/notice labels and the M-MAX row.

7. **Report composition — observed.** Ordinary COMBINE displacement is printed once per dx/dy/rz max/min mode, yielding six identical vectors for every one of 120 combination/node groups and 600 extra rows. `ResultDisgCombine.cs:620–637` iterates the modes. Force tables use `Table.OneUpInfo` (`ResultFsec.cs:499–506`). `PrintLayout` is consumed only for diagrams (`PrintProjection.cs:653–669`), so toggling that enum cannot fix table layout. Test the true composer/parsed PDF, not merely projected JSON.

### Comparison limitations to keep visible

Reference/current row counts are 951/680 basic force, 2850/2040 PICKUP, and 240/840 displacement. Of these, 300 basic-force and 900 PICKUP rows have unambiguous one-to-one joins. There are 180 basic-force and 540 PICKUP coincident-position groups without a proven side correspondence. Preserve and resolve these groups; never count them as matches by selecting a convenient row. In the current fresh result the 460 deduplicated segment-end occurrences differ only by floating-point roundoff, so a significant lost force jump was not demonstrated in this run. Historical reference input identity remains unknown.

## What Worked

- Shared production path: saved JSON → CalculationRequestBuilder → PythonCalculationRuntime → strict AnalysisResultSet → CalculationDerivedPresenter → PrintProjection → DirectPdfGenerator. Python alone omits essential application processing.
- Private C# harness: `harness/Verification.csproj` and `harness/Program.cs` in the original run. It uses `net10.0-windows`, x64, UseWindowsForms, desktop ProjectReference and existing friend assembly name FrameWebforCS.Tests. Retain repository-relative executable placement for Python environment discovery; isolate outputs from real test assemblies.
- Inspect `harness/output/run-summary.json` for the exact build/run/test commands, versions and source/artifact hashes. Output includes saved-input.json, request.json, canonical.json, derived.json, settings-*.json, print-*.json and three PDFs. Reuse/adapt in a NEW run directory after changes; never overwrite old evidence. The old harness hardcodes the verified input hash and a 12-original-node assertion, so check its assumptions explicitly.
- `comparison/compare_pdfs.py` extracts by page-rotation-aware coordinates, preserves row multiplicity, compares semantic components despite M/S/N versus N/S/M ordering, and records unpaired/ambiguous rows. Six mutation checks exercise the row comparator. Adapt parser templates as corrected report layouts change; keep independent reference extraction and negative controls.
- `audit/checker.py` independently checks current coefficients, correlated vectors, sources, unit projection and mappings. It pins sample-specific coefficients and validates them against saved input. Use it as an independent diagnostic, not as proof the FEM base solution is correct.
- `visual/render_pages.py` rasterizes every page and makes contact sheets. Reference PDFs use rotation 90; transform text boxes with page.rotation_matrix before comparing visual coordinates. Full report acceptance also needs detailed checks of affected pages.

## What Didn't Work

- Passing the 56 existing product tests did not establish reference parity; actual three-report comparison failed. Add regressions for the newly observed behaviors.
- The Headless CLI does not expose CombinedDisplacement or all required print settings. Its five available PDF section names alone cannot reproduce this task; the private harness calls existing APIs directly.
- PrintLayout changes do not affect these table reports; they affect diagrams only. Do not spend a correction cycle on an unsupported layout assumption.
- Global sign flips, absolute values, rounding tolerances, dropping rows, or treating scaled values as equivalent would hide real differences. None is an approved shortcut.
- Legacy reference PDF bytes cannot prove unrounded historic values; report printed equality separately from fresh arithmetic consistency. No historical oracle input was recovered.
- PowerShell default encoding corrupts Japanese text piped to Python; use UTF-8 script files or explicitly set OutputEncoding and PYTHONIOENCODING. Use `uv run --project FrameWeb --locked --extra dev python`, not global Python.
- Nested Windows npm Codex shims truncated multiline argv prompts; a single-line prompt referencing a UTF-8 file worked. A broad CLI adequacy review timed out and left descendants holding output handles; a bounded document-only review succeeded. Do not treat CLI exit 0 or empty output as a review PASS.

## Completed implementation sequence (historical)

1. Load shared context and inspect current git status/diff, this handoff, verification report and private manifest. Confirm all cited files still exist and line numbers are current. Verify source hashes before assuming K: input is unchanged. PROGRESS.md was absent; do not fabricate prior progress.
2. Create the correction plan under `.agents/docs/plans/` FIRST. Record the user instructions above, observed failure cases, unknown unit/sign contracts, dependency order, ownership, test framework/tooling and final all-row/all-page acceptance. Begin its ongoing status/findings/rationale/Tips log immediately.
3. Read `.agents/skills/tdd/SKILL.md`. Inspect `.agents/skills/_shared/run_tests.py` before invoking it: it currently resolves pytest and classifies pytest exits; it is not demonstrated to accept arbitrary dotnet test invocations. Preserve the skill's checked Red/Green invariant. For C# tests, provide a minimal scoped pytest bridge to real dotnet/xUnit tests and inspect TRX/assertion outcomes if needed; compilation/restore/collection failures are invalid Red. Do not silently claim the existing wrapper validates native .NET failures or rewrite shared infrastructure opportunistically. Record the chosen runner in the plan.
4. Implement tested input-conversion repairs first: notice_points array retention and lexical negative-zero continuation, including boundary/negative/error cases. Pin physical projected load intervals in assertions; do not stop at JSON shape. Then regenerate the actual sample, inspect CASE 10, and update the plan with measured before/after results.
5. Establish and test the force/rotation/unit contract, then repair the correct transformation layer and verify signed PICKUP source/vector semantics. Keep raw canonical contracts and unrelated exports intact. Investigate remaining numerical differences with intermediate-stage evidence.
6. Repair provenance text, case/member/end/station labels, required extrema/notice rows, ordinary COMBINE repetition and table composition using real parsed/rasterized PDF regressions. Avoid merely making projected dictionaries look plausible.
7. Rerun the preserved comparison process in a new run directory, improve any template/side mapping with evidence, and evaluate EVERY acceptance item. Update the plan with ✅ only after successful relevant verification. Record unresolved mismatches and causal uncertainty honestly.
8. Run relevant component tests/builds, source/diff checks, agent gate, and independent review where needed. Deliver final before/after artifacts and the maintained plan. Continue until the requested repair is complete, or document the precise unresolved blocker rather than reporting partial parity as success.

## Next continuation steps

1. Read current correction report/plan and final manifest. Preserve uncommitted product work, original313artifacts+manifest and both reference sets; check hashes before edits.
2. Use corrected22-edit COPY, not old11-edit candidate. OriginalK:JSON stays untouched; no production auto-symmetry rule exists.
3. Start residual investigation at `reference-refresh/comparison-final-9/` and final all-paired residual/source ledgers. Row multiplicity/missing/extra/ambiguity are resolved. Do not repeat completed station repairs or force source ties/numeric joins.
4. Establish exact reference input/software identity or independent numerical evidence before changing residual values/strict extrema. Recover absent names only from an authoritative source.
5. Separate baseline vector-diagram case-key/node-mapping defects from the passing table/occurrence work. A future diagram repair needs actual canonical diagramPDF TDD, not only the renderer list helper.
6. Any further product correction requires observed Red/Green and a fresh uniquely named pipeline/comparison/audit. Preserve final-9, update plan/report/STATE/HANDOFF, and keep complete parity open until demonstrated.

### Existing verification commands to reuse selectively

```powershell
$env:PYTHONIOENCODING='utf-8'
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj -c Release --filter FullyQualifiedName~RealProcessRecalculatesAndEmitsRequestedArtifacts
dotnet test FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj -c Release
dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj -c Release --filter "FullyQualifiedName~PrintNumericFormatOracleTests|FullyQualifiedName~DisplacementNodeIdLayoutTests|FullyQualifiedName~PrintLibraryTests"
dotnet build FrameWeb.sln
# Relevant if backend topology/projection is changed or investigated:
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/integration/test_result_projection.py -q
# Infrastructure/document changes; run the relevant product gates separately:
& .agents/check.ps1 -AgentOnly
```

Additional source anchors: `FrameWebforCS/tests/CalculationRequestTests.cs`, `CalculationDerivedPresenterTests.cs`, `PrintProjectionTests.cs`; `FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs`, `DisplacementNodeIdLayoutTests.cs`. Resolve their current project files before choosing test commands. Do not assume a test project path from an older handoff exists.
