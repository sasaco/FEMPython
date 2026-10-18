## Implementation Plan: Transverse Analysis Print Corrections

### Purpose

Correct the source-proven saved-document analysis and printing defects described in root HANDOFF.md. Recalculate the supplied transverse model through the actual shared application pipeline, compare all rows/pages with the three reference PDFs, and preserve evidence of both corrections and unresolved historical-model differences.

Current steering: the user explicitly selected the UPDATED K: PDFs after the closeout
hash check. Final-target validation completed against the stable snapshots in
`reference-refresh/snapshot/` (portrait30/60/2pages). Starting-reference15/45/4 results
below are retained as historical evidence; they do not close the updated-target criteria.

Authorization: the user approved the specialist review and requested implementation on 2026-10-15. This plan incorporates that approval and its eight actionable review findings; another approval round is not required for the authorized corrections.

### Scope

- Input conversion: `FrameWebforCS/calculation/CalculationRequest.cs`, focused request tests, and the independent `components/input/InputLoadService.cs` / `three/LoadMemberExpansion.cs` display path when the same lexical defect is reproduced.
- Presentation: `FrameWebforCS/providers/printing/PrintProjection.cs` and narrowly scoped helpers/tests; investigate `calculation/CalculationDerivedPresenter.cs`, result tables, diagrams and `.pik`/node CSV consumers before changing shared behavior.
- Composition: `FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisg*.cs`, `ResultFsec*.cs`, necessary table helpers and focused `PDF_Manager.Tests` regressions. Preserve numeric-string formatting outside provenance cells.
- Verification tooling: one scoped pytest-to-dotnet bridge under `FrameWebforCS/tests/TransverseParity/`; independent audit/comparison/harness copies in a NEW private `.tmp/transverse-analysis-print-corrections/<run>/` directory. Existing verification run is immutable.
- Lead-owned docs: this plan, root HANDOFF.md, the relevant STATE block, final correction report and durable design decisions where established.
- Exclusions: incidental Angular/converter changes, canonical schema redesign, global sign/unit guesses, changing client originals/reference values, uploading artifacts, broad cleanup, dependency upgrades.
- Baseline: HEAD `08aac8fd4b64b8513047adc575d87893a38fe818`; existing dirty STATE/HANDOFF and untracked verification documents belong to earlier work and must be preserved.
- Historical evidence: `.tmp/transverse-analysis-print-verification/20261015T182603/`, including frozen input/reference copies and manifest. Input SHA-256 `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553`.

### Constraints

- Follow root AGENTS.md, PowerShell, locked repository toolchains and `.agents/skills/tdd/SKILL.md`. Each corrected behavior requires observed assertion-level Red then Green, retained logs and lead re-verification.
- Preserve raw canonical results and export semantics. Document unit/axis/sign contracts with source/fixture evidence before applying transformations. A sign-reversing display transform may exchange raw maximum/minimum; preserve correlated vectors and source IDs.
- Distinguish facts, hypotheses and unknowns. Passing arithmetic checks is not reference parity or proof of FEM correctness.
- Keep external originals, frozen baseline and old verification run unchanged. Every new run retains commands, source/input hashes, raw/intermediate outputs, PDFs, all-row differences and visual evidence.
- The reference historical input is unknown and predates the source JSON. Never force unexplained differences into the code. Any unresolved acceptance criterion remains unfinished with evidence.
- Changes to shared paths require regression coverage of actual affected callers, including ordinary versus true envelopes, 2D/3D, modal normalization, moving loads, filters, numeric-string input tables and exports.
- Independent writers own disjoint paths. Serialize dotnet build/test operations that share outputs. Lead owns integration and final acceptance.

### Acceptance criteria

- [x] ✅ Saved notice arrays (including all three sample records and member 5 at 0.566) survive request, canonical topology, aggregation and real PDF output. Cover absent, empty and malformed data without silently dropping input.
- [x] ✅ Lexical `"-0"` and negative offsets produce correct physical intervals for supported marks, consecutive/nonconsecutive rows, member changes and ordinary zero. Compare solver and displayed load intervals on the same fixture.
- [x] ✅ Established displacement/rotation/force display contract applies at the correct boundary, keeps canonical/export values unchanged, and selects signed extrema with full correlated vectors/source IDs. Cover physical ordinary/COMBINE/PICKUP/moving paths and preserve dimensionless modal shapes.
- [x] ✅ Provenance stays text with correct weighted constituent expressions. Existing numeric-string input-table/culture behavior remains intact.
- [ ] Required case/member/end/notice/rigid/station/extrema labels and row multiplicity are represented. Ordinary COMBINE vectors print once while true PICKUP/moving extrema and requested filters remain available.
- [x] ✅ Updated-reference table composition is implemented and parsed/rasterized. The user-selected replacement PDFs are A4 portrait595x842pt with30/60/2pages; original15/45/4 acceptance was verified against the previous target only. Record every residual difference explicitly.
- [x] ✅ Full fresh shared pipeline comparison against the user-selected UPDATED references accounts for all numeric rows/pages, missing/extra/duplicate rows and unresolved coincident-side associations. No partial match is called complete parity.
- [x] ✅ Native TDD evidence proves the selected xUnit assertion actually executed. Zero tests, skipped-only runs, restore/build/collection errors, timeouts and bridge failures are not accepted as valid product Red or Green.
- [x] ✅ Relevant component tests/builds and agent checks pass, or exact preexisting/environment failures are reported separately. Coverage is measured or explicitly unmeasured, without an invented repository threshold.
- [x] ✅ Final maintained plan/report/manifest/artifacts and final STATE/HANDOFF status are delivered for success as well as incomplete work. Owned children are terminated/reaped on failure/timeout; protected evidence is retained.

- [ ] Complete updated-reference numerical, station-side, source, name and whole-row parity. The final comparison is complete, but this acceptance remains unresolved.

### Implementation Steps

Execute the following steps in dependency order; independent investigations can run alongside the test-runner setup without product writes.

#### Step 1: Establish the correction workspace and native TDD runner

- [x] ✅ Load context, inspect dirty scope and read the approved handoff/review. PROGRESS.md is absent, not fabricated.
- [x] ✅ Create this correction plan before product edits using the shared workspace-returned path.
- [x] ✅ Create a new private run with baseline manifest and evidence links; verify old source/input evidence needed for each repair against current checkout.
- [x] ✅ Reuse or add one small pytest-to-dotnet bridge. Check intended native assertion failure/pass, unmatched selection, skipped-only selection and build failure. Capture named tests/TRX and distinguish infrastructure failures from product assertions.
- [x] ✅ Bound owned dotnet/testhost and verification process trees, retain partial logs and serialize builds. Prefer local scoped tooling over changing shared agent infrastructure.
**Verification**: run_tests.py classifies native Red/Green correctly and rejects invalid native observations; bridge self-check logs and original-run integrity evidence retained.

#### Step 2: Repair saved input conversion and reconcile load display

- [x] ✅ Red/Green notice array retention, absent/empty/malformed handling and a solver topology regression proving actual requested stations.
- [x] ✅ Red/Green lexical negative-zero continuation with explicit projected intervals, blank/row-gap resets, member changes and supported marks; preserve existing negative-offset behavior.
- [x] ✅ Reproduce and repair sibling load-display semantics where they diverge, without changing saved input representations. Compare display/request intervals.
- [x] ✅ Recalculate sample through the private shared-production harness; inspect CASE 10 request intervals/results and notice station coverage; record measured before/after effects.
**Verification**: focused request/display/native solver regressions, source diff review, fresh CASE 10 and station ledgers; no numerical causality claimed before rerun.

#### Step 3: Establish display conventions and implement bounded transformations

- [x] ✅ Trace force/rotation/unit semantics from solver, UI/legacy evidence and independent simple fixtures; document signs at I/J ends, axes and units.
- [x] ✅ Red/Green physical displacement/rotation conversion for base and derived output; retain modal normalization, moving branches and 2D/3D contracts.
- [x] ✅ Red/Green display-convention signed maximum/minimum, source/tie and correlated-vector behavior; preserve raw derived/export semantics and test relevant `.pik`/CSV consumers.
- [x] ✅ Preserve independent raw arithmetic audit; adapt a NEW copy of projection audit to the established contract and test missing/double conversion negative controls.
**Verification**: known physical fixtures and raw immutability/export assertions, affected presentation/print tests, fresh stage artifacts; unresolved historical numerical differences remain visible.

#### Step 4: Repair provenance, station labels and actual report composition

- [x] ✅ Red/Green provenance text at semantic text cells; avoid weakening shared numeric-string formatting.
- [ ] Red/Green case/member/end/notice/rigid labels and required extrema rows using real PDF parsing. Determine M-MAX/placeholder meaning from reference evidence rather than inventing an algorithm.
- [x] ✅ Red/Green ordinary COMBINE deduplication with nondefault component filters; retain PICKUP and delegated moving-load envelope modes.
- [x] ✅ Red/Green reference-facing paired-case / max-min layout, pagination, long labels/IDs and 2D/3D unaffected behavior in the actual composer.
**Verification**: parsed actual PDF values/labels/row multiplicity and rasterized affected pages; existing numeric-format/node-ID/input-table regressions pass.

#### Step 5: Full comparison, review and closeout

- [x] ✅ Fresh sample solve and all three PDFs from the corrected shared path; input/source hashes, complete output manifest and raw/projection artifacts retained.
- [x] ✅ Adapt NEW comparator templates when layout changes while preserving independent reference extraction and mutation controls. Reconcile all-row partitions, duplicates, selected sources, ambiguous station sides and all-page visual inspection.
- [x] ✅ Run relevant desktop/PDF tests and solution build; relevant Python gates if engine/topology changes; agent-infrastructure gate for docs/tooling changes. Run independent implementation reviews and resolve actionable regressions.
- [x] ✅ Write before/after report with exact residual differences, assumptions and coverage status. Mark only verified acceptance items ✅.
- [x] ✅ Record final completion/incomplete state and report/evidence links in STATE and HANDOFF unconditionally. Release owned processes; retain original and new evidence, remove only task-owned disposable files when no longer needed.
**Verification**: reproducible final manifest, complete row/page accounting, reviewed diff and explicit final acceptance checklist.

### Test cases and commands

Use actual project/filter selection for each regression and retain assertion names, counts, TRX and command logs. The scoped bridge is invoked through:

```powershell
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --component python --target FrameWebforCS/tests/TransverseParity/test_native.py --expect fail --label transverse-red-N
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --component python --target FrameWebforCS/tests/TransverseParity/test_native.py --expect pass --label transverse-green-N
dotnet build FrameWeb.sln
dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj -c Release --filter <affected-classes>
dotnet test FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj -c Release
dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj -c Release
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/integration/test_result_projection.py -q
& .agents/check.ps1 -AgentOnly
```

Native runs used as Red/Green evidence go through the bridge. Direct component gates supplement, rather than replace, assertion-level evidence.

### Risks & Considerations

- A new station can change output topology and derived work. Verify downstream station lookup, original/generated node filtering, coincident positions, export and print associations rather than only key presence.
- The independent load visualization pipeline can preserve the same negative-zero defect after request conversion is fixed.
- ResultDisgCombine is shared by PICKUP and moving-load delegation. Deduplicating an envelope as if ordinary would lose real extrema; filters must not select an unavailable representative mode.
- Unit conversions must exclude dimensionless modes and cover alternate physical/moving paths. Sign reversal requires extremum/source treatment before formatting, not just a printed-number negation.
- printManager.toString intentionally formats numeric strings for input tables. Provenance text repair must not change that behavior globally.
- The old projection checker intentionally asserts raw == printed values; those expectations become obsolete after valid display conversion. Keep its raw arithmetic independence and update only copied display expectations with independent evidence.
- Historical input identity and station-side correspondence may prevent complete reference parity. Report evidence and incomplete criteria instead of masking differences.
- Existing full desktop result-page/camera failures and Python legacy benchmark-consumer failures are known historical baselines; reproduce affected failures before attributing them to this change.

### Open Questions

- Resolved for this implementation: explicit print-only 2D clockwise Mz/Rz, physical mm/mrad, declared m/mm/cm and legacy unspecified fallback; 3D signs and dimensionless modal shapes retained. This is not proof of every historical convention.
- Measured: CASE 10 differences remain after input repairs; see the final report. Historical causes remain unproven.
- Reference coincident-position side mapping, M-MAX label/placeholder interpretation and historical input identity: preserve ambiguity until evidence resolves it.
- Paired-case/max-min composition is implemented and page counts now match. Exact member-level page breaks, missing/extra/ambiguous rows, M-MAX and reference-only names remain unresolved.

### Status, Findings, Rationale and Tips

- 2026-10-15: User approved the eleven-agent review and authorized implementation. No additional approval gate is introduced by creating this detailed continuation plan.
- Existing work is documentation-only; original verification reported 30/60/34 generated pages versus reference 15/45/4, with 56 focused product tests passing but parity failing. Product code coverage was unmeasured.
- Preserve `.tmp/transverse-analysis-print-verification/20261015T182603/` unchanged. Use UTF-8 scripts for Japanese paths; do not pipe non-UTF-8 PowerShell strings into Python.
- Ownership and fresh command/evidence paths will be appended here as workers are assigned and verified. This log must be updated when facts, decisions or acceptance status change.
- Initial worker ownership: native_tdd_bridge writes only `FrameWebforCS/tests/TransverseParity/**` and `.tmp/transverse-analysis-print-corrections/runner-selfcheck/**`; display_contract_investigation and composer_investigation are read-only. Lead owns this plan, STATE/HANDOFF and `.tmp/transverse-analysis-print-corrections/20261015T193610/**`.
- Native bridge interface: `TRANSVERSE_NATIVE_PROJECT`, `TRANSVERSE_NATIVE_FILTER`, `TRANSVERSE_NATIVE_EXPECT` (required test-name substrings separated by semicolons), `TRANSVERSE_NATIVE_LABEL`. Target `FrameWebforCS/tests/TransverseParity/test_native.py`; outer timeout 600s, inner deadline plus lock wait kept shorter. Interface is not yet verified.
- Plan shape and nonempty workspace gates passed; added introductory step text to avoid an empty-section warning from the shape parser.
- ✅ New run prepared at `.tmp/transverse-analysis-print-corrections/20261015T193610/`. `prepare_run.py` verified all four external/frozen source hashes, copied only frozen inputs and source scripts/harness, and recorded 1,078 initial product-source hashes in `manifest-start.json`. Existing run remains untouched.
- Bridge requires `--component python`: auto-resolution otherwise rejects a pytest target outside the FrameWeb component. Shared agent runner remains unchanged.
- ✅ Ownership preflight passed with `overlaps: []` (typed assignment `.agents/logs/ownership-transverse-analysis-print-corrections.json`). Set PYTHONIOENCODING=utf-8 for its non-ASCII warnings; `.tmp` globs are invisible to git inventory, so their absence from candidates is expected, not evidence of missing work.
- Independent display investigation: CASE1–9 diagnostic matching-station force rows N/S match unchanged and M matches -mz in 594/594. This establishes a supplied-reference-facing 2D convention, not a universal legacy or 3D rule. Right-handed canonical values remain unchanged. Legacy displacement +1000 is independently sourced; clockwise-positive 2D report rotation is treated as a separate explicit print convention and tested with analytical cantilever expectations. Historical residual differences remain unknown.
- Input investigation found Angular's relative-position behavior is mark-specific: lexical negative zero is meaningful in distributed expansion, while point marks1/11 have numeric normalization first. Preserve evidenced semantics, including member-range carry reset; do not apply lexical continuation blindly to every mark.
- Display worker recorded native `display-base-red` then `display-base-green` through the bridge; seven focused cases cover declared m/mm/cm, unspecified legacy fallback, 2D/3D signs, unknown-unit rejection and modal nonconversion. Lead re-verification is pending; derived/moving cycles remain unfinished.
- Composition investigation: derived COMBINE may itself contain real LL envelopes through DEFINE. An ordinary-case marker must follow contributing sources (or other proven semantic evidence), not merely the COMBINE key or selected-mode equality. Pair different cases horizontally; Table.Nup alone splits one table's rows and cannot implement the reference pairing.
- M-MAX evidence is a distinct extra CASE1/member5 row at 0.566 with shear placeholder, in addition to the notice row. A generic extremum algorithm is not yet established; no sampled-maximum fabrication is authorized.
- ✅ Native runner completed nine real native controls: pass/assertion failure valid, unmatched/skipped/build failure/wrong required name/runtime exception/unrelated assertion/timeout invalid. Timeout cleanup recorded zero active processes and reaped root. Lead independently re-ran pass/fail/unmatched with unique labels; all expected observations passed (`lead-runner-check.json`). Native coverage remains unmeasured.
- Input worker recorded assertion cycles `transverse-input-red-1b` → `transverse-input-green-1` (notice array/error cases) and `transverse-input-red-2` → `transverse-input-green-2` (request/display physical intervals and lexical save/reload). Lead full Green re-verification and solver-station regression pending. Initial red-1 was rejected by the bridge classifier and is not counted as Red; red-1b is the valid assertion evidence.
- Fresh input-stage shared pipeline launched with `run_pipeline.py --stage input`, owned process deadline 300s and shared build lock. Its raw results will quantify input corrections even though independent print display changes are also in flight.
- Composer interface: root `ordinaryDisplacementCombinations` array lists proven ordinary COMBINE IDs, absence retains envelope behavior. Force rows add `stationLabel` and `memberCaption`; station identity `n` remains separate. Composer-only writer does not modify the shared projection file.
- ✅ Fresh input-stage solve completed all10cases once, with known axial smoke dx0.0004 and topology72nodes/11members. Request retains3notices; member5 stations are0,0.566,1.132. First four member10 patches now span0→0.745→2.945→3.645→4.390 instead of overlapping at0. CASE10/node1 rawDx changed0.3250261313216033→0.020176485660886107; reference printed19.0393 remains different from new20.1765mm. CASE10/member10/I rawN changed1538.53(print baseline)→1.3917497244595407; reference13.52 remains unexplained. Evidence: stages/input/{stage-manifest.json,diagnostics.json,output/**}. PDF counts at this intermediate stage40/70/34 are not final composition acceptance.
- Display worker completed base/derived/moving Red→Green and focused43/43 including actual raw .pik/CSV byte preservation. Metadata/provenance/labels now delegated to the same projection owner with new tests; compositor gate is `referenceResultLayout=true` for2D result reports, absent for3D/input-only.
- Input main test project now builds and focused44/44 passed, so previously reported LoadDisplayConversion compile failure is not reproduced in this checkout. Temporary focused input project will be removed by its owner; no unrelated baseline repair is needed.
- Private final audit copy now applies the explicitly documented 2D print convention to expected projections and remaps reversed extrema modes; raw arithmetic recomputation remains unchanged. Original verifier is untouched. Independent missing/double-conversion controls still to be added/run.
- Verification-only PyMuPDF is supplied with `uv run --project FrameWeb --locked --extra dev --with pymupdf`; it is not installed into product manifests/lockfiles. Initial import failure under plain locked environment is tooling-only, not a product failure.
- ✅ Independent input-stage raw arithmetic and corrected projection audit passed with zero errors. Four private negative controls detect missing conversion, doubled conversion, omitted moment sign and wrong source separately (`audit/projection-controls.json`). Original arithmetic implementation remains independent of product code.
- Lead input Green attempt was correctly rejected as collection_error because an in-flight composer used C# features unsupported by PDF_Manager's C#8 target; no TRX meant no valid product test observation. Composer fixed the compile incompatibilities and confirmed actual parsed-PDF pairing Green. Retry is pending; invalid logs remain evidence.
- Input worker extended review to negative nonzero start after a member change; a targeted regression is in progress. Native solver regression isolates CPython in a no-build child with exact named child TRX, avoiding the one-interpreter-lifetime conflict with existing runtime tests.
- Input-stage loaded product DLL/PDB copies are preserved under `stages/input/assemblies/` with SHA256 inventory before later harness rebuilds. Future stage manifests capture all tracked/untracked product-source hashes before/after execution and reject claims of a stable source snapshot if they differ.
- Input final worker gate is46passed (new19cases plus affected existing regressions), including real station and runtime-interpreter coexistence. Added valid `transverse-input-red-3b` → `transverse-input-green-3` for negative nonzero offset after a member change. No sample same-member negative-zero behavior changed by this final guard.
- ✅ Temporary focused input project source removed. Lead verified the exact task-owned directory, deleted only3generated C# leaf files under its obj directory, then restored main test csproj's original Compile exclusion. Ignored disposable binaries may remain, but no extra source project or persistent test-project exclusion is left. Original evidence and TRX retained. An earlier recursive folder deletion was automatically rejected; this narrower cleanup succeeded.
- Independent input/native-runner review is now read-only against stable owned files. Lead affected-input re-verification is running with unique `transverse-lead-input-green-retry` label.
- ✅ Lead input re-verification passed through the native bridge after narrow cleanup. Independent input review then found2P2: zero endpoint aliases must be normalized before continuation-member checks; nested solver-test infrastructure failure must not be converted into valid assertionRed. Both are assigned back to input owner for targeted regression/fix before final acceptance.
- Metadata worker completed ordinary-marker/labels/provenance assertionRed→Green, then indexing refactor; fullfocused53/53passed0skips. Exact weightedbaseCASE expressions only replace display comb when raw source/vector association is proven; rawProvenance retained. Independent projection review and lead Green re-verification are running.
- User reconfirmed the existing K: JSON path as the authoritative input; no alternate historical input supplied. Sample member/section names remain absent in this JSON, so reference-only names cannot be reconstructed as facts.
- ✅ Input reviewer confirmed bothP2fixes resolved; finalworker56passed and lead `transverse-lead-input-verified` passed after using the correct required child-control test name. A previous lead invocation used nonexistent required name ChildObservation and was properly rejected despite successful native tests; it is not a passing gate.
- ✅ Lead focused projection gate passed (53native tests); independent projection reviewer found no P1/P2/P3 issue. Full raw export byte preservation, source/mode selection and labels/provenance regressions retained.
- Composer firstfullgate67passed; independent review found oversized-header no-progress pagination and unwrapped long member captions. Both assigned for focused fixes. Fresh intermediate final-stage pipeline has15basic/63PICKUP/4displacement pages. All executed product source hashes stayed stable; only the new composer TEST file changed during execution, so sourceChangedDuringRun is true in the broad inventory and is explicitly accounted for rather than hidden.
- Private comparison owner now adapts generated-parser templates in comparison/** while preserving independent reference extraction/negativecontrols. Weighted provenance source wrapping is being measured against actual column width to avoid unnecessary pagination without dropping expressions.
- ✅ DESIGN typed writer applied2decisions for print-only unit/sign handling and semantic ordinary/envelope metadata; skipped_duplicates0. These decisions preserve raw canonical/export contracts and do not certify historical/reference parity.
- ✅ Final source-stable shared pipeline stage `final-3` completed all10cases once, 72nodes/11members, no solver warnings, `sourceChangedDuringRun=false`. Corrected PDF pages15/45/4 match reference counts; final solution build passed0errors with1,479warnings (incremental build warning count is not a quality improvement metric).
- ✅ Independent input/native review's2P2 and composer's2P2 are fixed and rechecked. Lead all-page raster review found short-ID displacement heading overlap; valid `composer-short-id-heading-red-valid` → `composer-short-id-heading-full-green` resolved it. Latest full PDF gate71passed. Lead affected gates total181passed (56input/53projection/71PDF/1Headless), zero failures/skips; native coverage unmeasured.
- ✅ Final independent audit149,400components, errors0, maxAbs5.684341886080802e-14. All64finalPDFpages rasterized, outsidePageWords0. Final force/PICKUP rasters identical to all60previously inspected pages; all4changed displacement pages re-inspected. Four projection, six comparator and five actual-PDF parser controls pass.
- ✅ Final PDF-only comparison parsed all rows/tokens and retained unchanged reference hashes/parser/rows. Numericvectors479/519basic,1242/1560PICKUP,173/240displacement match among unambiguous pairs. Missing70/210,extra120/360; ambiguousreference/current362/181basic and1080/540PICKUP remain explicit. Parser completeness is true; exact reference parity is false.
- ✅ Ownership reconcile: overlaps[],unowned_changes[],idle_owners[]. AgentOnly overall=pass using an exact20-path approved-product scope allowlist; product gates are explicitly skipped by AgentOnly and separately evidenced by the181native tests/build. Initial scope-only failure is retained, not counted as a product failure or silently erased.
- Final status: implemented corrections verified; full reference parity remains incomplete. Required M-MAX/interior extrema, missing/extra/coincident station associations, residual CASE10/other values and reference-only absent names remain open. See [before/after report](../research/transverse-analysis-print-corrections-report.md), root HANDOFF and final private manifest for reproducible continuation. No arbitrary matches, fabricated names or sampled extrema were introduced.
- Closeout hash check detected all3externalK:PDFs changed content during execution (portrait30/60/2pages), while sourceJSON/all4frozeninputs/originalmanifest+313inventoriedartifacts remain unchanged. Preserve refreshed snapshots separately in `reference-refresh/**`; actor/intent unknown. Optional target-selection question sent; until another target is selected, all comparison metrics remain against the approved starting15/45/4-page frozen references. Updated references have not received a full product parity comparison. A PowerShell Console.Out payload bypassed assignment capture; final AgentOnly JSON was safely captured from a separate process stdout and overall=pass.
- User answered: use UPDATED K: PDFs for final verification. Native comparison owner now owns `reference-refresh/**` for complete independent updated-reference parsing/comparison; composer owner is read-only investigating `composer-refresh-investigation/**` before any further product edits. Existing final-3/original comparison artifacts remain unchanged. Updated PDFs still show CASE10/node1 Dx19.0393 and member10/I N13.52, so the known numerical residual did not disappear with target replacement.
- Updated-reference parser independently accounts for950/2850/240rows,3800/11400/720tokens on30/60/2portraitpages. UpdatedPICKUP has29changednumericcomponents versus startingoracle among unambiguouspairs; basiconeM-MAXrow is absent. Required extrema acceptance must follow this user-selected target, not fabricate the superseded row. All92referencepages visually reviewed; existing MIXcaption collisions are recorded and must not be copied into product output.
- Composer now implements an orientation-aware branch inside the private2Dreference layout: portraitbasic oneCASE/flow with wholememberkeep and a shortside-bounded tableviewport inferred from observed reference layout; portraitPICKUP sequentialMmax/Mmin/Smax/Smin/Nmax/Nminfullpageflows; displacement retains pairedCASE/MIXpanels. This is a deliberate presentation policy, not a claim of recovered legacy pagination code. Landscape behavior remains covered.
- Residual investigation executed actual Angular InputLoadService/helper/member-length code using localNodeTypeScriptstripping. OriginalCASE10all46effective loads agree with the corrected C#request except negligible lastpatchmargin1.309/1.310. All107unambiguousbasicnumericcomponentdifferences areCASE10; CASE1-9none. The supplied firstpile's array order and row labels differ (1,4,5..12,2,3,13..); row-sorted pressure placement differs from the secondpile despite equal resultants. Controlled copiedprofile yieldsN13.515099→13.52 andDx19.0388026mmvsreference19.0393. Source/canonicaldiagnostics remain in `residual-investigation/**`; no production auto-symmetry normalization is justified.
- User confirmed LEFT/RIGHT piles should have the SAME pressuredistribution. Prepare the smallest rowlabel-only corrected savedJSON COPY and compare its actual effective request with the diagnostic candidate; preserveKoriginal, numericpayloads and othercases. Lead privateharness now accepts an explicitly hash-pinned alternate privateinput and configuredportraitorientation. Plan fresh original-inputportraitstage plus confirmedcorrected-inputportraitstage, separatefullcomparisons. Current implementationclosure is pending these updated-target steps.

- ✅ User-confirmed correctedCOPYfinal successor has22scalar edits (11rowIDs+11layoutrefs), SHA60dcb754088a58f2008696e76b982c787a61592fdd8fba426d47d40fc5751ad3. Input-copy independentP2 oldlayoutrefs fixed/rechecked; actualservice save/clear/reload48visiblerows and46physicalrows matchactualAngular; oldBnegativecontrol rejected. Original/allotherdata preserved; no productautosymmetry rule.
- ✅ Portrait TDDcycles basicred→green-viewport-fixed, pickupred→green, disgred→green; boundarycoveragefull80passed. FinalvisualP2 numericcoefficients splitmidnumber received validcomposer-numeric-token-wrap-red→full-green,82tests. Finallead192nativepassed (56/53/82/1),0fail/skip; build0errors/1482warnings. Scope remains productcomponents alreadyapproved.
- ✅ Final source-stable sharedstagefinal-7 usesconfirmedcopy, all10cases once/no solverwarnings,69nodes/11members, PDFs30/60/2. Final6→7analysis/printJSON+allPDFrowvalues/source/pageassociations unchanged. All92pages rasterized;91byteidenticaltopreviouslyreviewed6, changedMIXpage2 root+independentreviewerchecked numericcoefficients whole andnoclipping. Mostpages contact-sheet-only; limitations retained.
- ✅ Updated-reference comparison-final-7 complete: rows950/790basic,2850/2370PICKUP,240/240displacement; numericvectors572/590,1514/1770,177/240; numericcomponents20/405/116differ. Missing0/0,extra20/60,ambiguousreference/current360/180 and1080/540. All3160/12300/732generatedtokens accounted, zero unresolved;5reference+4generatedPDFparsercontrols+6comparatorcontrols pass. Names/weights/format/source comparisons remain separate.
- ✅ Independentstagedaudit andleadreplay144000components/0errors, maxAbs5.684341886080802e-14; exact4800PICKUProws/28800components/source/provenance checked. fsumoracle tiesCOMBINE3/8 at~-3.1e-73 while sequentialstoredvalues differ5ULPs; initial3disagreements preserved. Independentupstreamarithmeticmustpass before exactstoredselection audit; wrongwinner/oneULP/source/provenance/upstream/projectioncontrolsdetect withoutweakenedtolerance. No productchange fororacleboundary.
- Finalcurrentstatus: implementation andconfirmedinputcopy verified; exactupdated-referenceparity INCOMPLETE. Remaining station-side multiplicity/20+60extra rows, printednumerical/source differences andabsentnames remainexplicit. UpdatedtargetcontainsnoexactM-MAXrow; supersededstartingreferenceMMAXisnotfabricated. Finalreport/HANDOFF/STATE/manifest carryauthoritativecurrentpaths andcleanup/protected-evidencefacts; earlierlogsabovearechronologicalhistory.
- Finalremaining-risk audit located ALL20/60extra force rows atmember10/11DIST22.641. This matches the actualAngularvsC#lastpatchmargin1.310/1.309 discrepancy; tiny displacement effect does NOT make its topology/row impact irrelevant. Inputowner now performs read-only exactnumber/rounding-contract investigation under `rounding-investigation/**` before anyproposedTDDfix. Independentcomparisonowner separately proves/refutes whether18coincidentgroups/case match savedsoil/springintervalboundaries and whether pairedreferencevectors/sources trulyagree (`station-side-investigation/**`). No hardcoded coordinate/tolerance/rowdropping/forcedjoins authorized. Finalcloseout deferred until these concrete remaining pathways are understood.
- RoundingdefectPROVEN withoutsampleIDs: generic10m mark2chain1.0333+1.034+1.0333 ends3.101inrequest vs3.100inactualAngular/load-display. MemberLengthbitsidentical; causeunroundedphysicalcarry BEFORElatequantization. Inputowner nowauthorizedTDD genericdistributedinteger-mmstagefix inCalculationRequest.cs/TransverseInputCorrectionTests.cs, preservingpointcarry/lexicalsigns/aliases andgeometry. No arbitrarygeometrymergeorrowdrop.
- FoundationrowmappingPROVEN: all180basic+540PICKUPreferencepairs haveidenticalprintedM/S/N andallPICKUPsource/formulas; exactly9explicitfix_memberintervaljoinsperpile across3activesheets, including10of54joinswithoutstiffnesschange. All180rawadjacentcanonicalleftJ/rightI sixcomponentvectors EXACTequal(maxdelta0). Displayowner nowauthorizedTDD print-onlyoccurrencehelper afterlabels/provenance: selected2DforceTABLES only, activejoinsetscompatible andallsourcesexact-sided-equalityproved; preservecanonicaln, clonealreadyprovenvector/sourceinto2displayoccurrences withsequentiallabels. Unknown/jump/mixedsheet/3D/nonstatic/LLproofabsent retainslegacyunique route; thisisnotuniversalone-sidedphysicsrepair. Diagram-only/rawexports unchanged; composeroccurrence-awarepairingalreadyexists. Typedownershippreflightoverlaps[] includesoptionalnewhelper/testpaths.
- Independentcomparatorowner preparesnon-numericoccurrencejoin wrapper: onlyequalmultiplicity>=2 groups withuniqueprintedstationlabelsandidenticallabelsets arepairedbylabel; othergroupsremainambiguous. No value-basedjoin, no matchingold2ref/1currentgroups, no originalparser/controlmodification. Addidentity/missingoccurrence/duplicate-label/numeric/sourcecontrols; fullnewstageafterbothproductownersrelease willverifyactualrows/labels/sources, notassumecounts.


### Final9 integration — verified corrections, remaining exact parity

- ✅ ActualAngular integer-mm stages proved/requestfixed beforecarry; native roundingRed10→Green69, stagesRed3→Green72. Independentnegative-subhalfL2Red1/4→Green76; reservedinternalmarkerinjectedtrue/nonboolean Red→Green78. Allpriorinput/display/request/runtimecases retained; no geometry/tolerance/rowfilter edits.
- ✅ ExactboundedfoundationTABLEproof+fallback implemented after independentreference/rawsideinventory. FourvalidRed/Greencycles, focusedprojection73PASS. Derivednull, allstatic6components, activegeometry, malformed/terminalintervals, notices/rigid, filters, LL/modal/3Dfallback covered. Privateindependentauditor verifiesproofandcardinality withouttrustingmetadata; adversarialcontrols pass.
- ✅ Mixedtable+diagram actualparser/render-listboundaryRed4→PDF85Green, onlyoccurrence>1filtered; unmarkedduplicates/discontinuities/bothextrema/tablelists preserved. Independentreviewconfirmsactualrouting. Existingbaselinecanonicaldiagramcase-key/nodemappingfailures documented, NOTclaimedfixed ordiagramPDFverified.
- ✅ Source-stablefinal-9 finalpipeline confirmedcopy67nodes/11members, all10casesonce/no warnings, PDFs30/60/2. ALL4040rowspartitioned:950/2850/240, missing/extra/ambiguous0. 180basic+540PICKUPcoincidentgroups resolvedonlycompleteuniquelabelidentity/equalmultiplicity; controlsrejectswaps/missing/duplicatelabels/numeric/sourcechanges. Sourcebefore/after/hashes preserved; final8/7 historicaloutputsunchanged.
- ✅ Independentlead140400componentauditPASS0errors,maxAbs1.1368683772161603e-13; exactPICKUPsource/provenance andfoundationoccurrences PASS. All3800/14793/732generatedtokensaccounted;5reference+4generated+6original+6labelcontrolsPASS. All92rasters verifiedagainstindependentlyreviewed8 (24contacts+7fullres;lead3fullres). Coverageunmeasured.
- ✅ Leadselectednative237passed78input/73projection/85PDF/1headless,0fail/skip. Finalsolutionbuild, AgentOnlyverbatimtools,ownershipandownedprocesscleanuparelinkedbyfinalreport/manifest. OriginalJSON,313artifacts+manifest,bothfrozenreferencesandearlierinputcandidates preserved; correctedCOPY22scalaronly.
- [ ] Exactparity remains INCOMPLETE:904/950,2470/2850,173/240numericvectors equal;56/553/122components differ,215selectedPICKUPsources differ,membernamesabsent. Whole-row0/0/94. Do not waive differences, fake coefficients/names or treat independentarithmeticconsistency asFEM/referenceidentity. Finalreport/HANDOFF/STATEcontain currentcontinuation; earlier20/60extra/180+540ambiguousgroups are nowresolved.
