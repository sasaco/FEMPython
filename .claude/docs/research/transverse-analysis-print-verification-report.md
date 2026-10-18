# Transverse Analysis and Print Verification

## Result

**The current FrameWebforCS recalculation and printed reports do not reproduce any of the three supplied reference PDFs.** Differences include numerical values, force signs, selected PICKUP sources, output stations, displacement display scaling, and report structure. This verification changed no product code or external input/reference files; repairs are outside the approved scope.

Run: `20261015T182603`. Approved plan: [transverse-analysis-print-verification.md](../plans/transverse-analysis-print-verification.md). HEAD: `08aac8fd4b64b8513047adc575d87893a38fe818`. The supplied JSON and all three PDFs matched the plan's SHA-256 baselines. Their original locations/hashes, versions and environment are recorded in the [run manifest](../../../.tmp/transverse-analysis-print-verification/20261015T182603/manifest.json).

| Requested report | Reference pages | Generated pages | Result |
|---|---:|---:|---|
| Basic section forces (`基本.pdf`) | 15 | 30 | Numerical, station and presentation differences |
| PICKUP section forces (`PICK UP.pdf`) | 45 | 60 | Numerical, selected-source, station and presentation differences |
| Base + COMBINE displacement (`変位量.pdf`) | 4 | 34 | Numerical/display differences and repeated COMBINE tables |

All 64 reference pages and 124 generated pages were extracted and rasterized. Every decimal/placeholder token in the target tables was accounted for. Ambiguous coincident force stations are retained as groups and are not assigned an artificial one-to-one match; the unequivocal differences below already disprove overall agreement.

## Recalculation and evidence

The private Windows x64 .NET 10 harness calls the same implementation as the current application:

`CalculationRequestBuilder.FromSavedJson` → `PythonCalculationRuntime.CalculateAsync` → `build_analysis_result_set` → strict `AnalysisResultSetJson.Deserialize` → `CalculationDerivedPresenter.Build` → `CalculationResultPresentation` → `PrintSnapshot` → `PrintProjection.Build` → `DirectPdfGenerator.Generate`.

The sample was solved once, with all three reports generated from that same fresh result. A separate small known axial fixture first verified displacement `dx = 0.0004` and exclusion of deliberately stale saved results. There were 10 saved/projected/analyzed cases, no skipped cases, and no solver warnings. Canonical topology contains the original 12 nodes plus 46 generated nodes, and all 11 original members. Displacement printing selects exactly the original 12 nodes.

The harness fixes `ja-JP`, Japanese report language, title `線路直角方向`, and A4 landscape. The canonical unit metadata is `consistent_user_defined` with all named units `unspecified`; no compensating unit or sign conversion was applied by the harness. Runtime versions include .NET SDK 10.0.402, .NET runtime 10.0.13, Python 3.13.11, NumPy 2.4.1 and SciPy 1.17.0.

Primary evidence:

- [Harness source and reproducible call chain](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/Program.cs), [run summary, commands, source/artifact hashes](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/run-summary.json).
- [Actual solver request](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/request.json), [canonical result](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/canonical.json), [derived result](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/derived.json).
- Generated PDFs: [section forces](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/section-force.pdf), [PICKUP section forces](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/pickup-section-force.pdf), [base + COMBINE displacement](../../../.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/displacement-and-combine.pdf).
- The output directory also retains `print-*.json` and `settings-*.json` for all three reports, frozen saved input, smoke output and stage diagnostics. Reference copies are retained under `preflight/inputs/`.

## Confirmed differences

### Numerical examples

Values below are the actual printed values. They are not normalized to remove sign or scale differences.

| Location / quantity | Reference | Current PDF | Reference / current page |
|---|---:|---:|---|
| Basic CASE 1, member 3, J end, DIST 0.881, M | −22.82 | +22.82 | 1 / 1 |
| Basic CASE 10, member 10, DIST 0.000, N | +13.52 | +1538.53 | 14 / 28 |
| PICKUP 1, MOMENT MAX, member 1, DIST 0.000, M | +10.41 | −7.40 | 1 / 8 |
| Same PICKUP row, S | +21.45 | +15.30 | 1 / 8 |
| Same PICKUP row, selected COMBINE | 6 (`1+4+5+6`) | 1 (constituent formula absent) | 1 / 8 |
| PICKUP 5, MOMENT MAX, member 6, DIST 0.059, S | −315.89 | −1850.49 | 37 / 57 |
| Basic CASE 1, node 1, Y displacement, header mm | −1.5543 | −0.0016 | 1 / 1 |
| Basic CASE 10, node 1, X displacement, header mm | +19.0393 | +0.3250 | 2 / 4 |
| COMBINE/MIX 5, node 2, X displacement, header mm | +38.0541 | +0.3440 | 3 / 17 |

Full details include reference/current values, semantic keys, page/column/bounding boxes, missing/extra rows and ambiguous groups: [row differences CSV](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/row-differences.csv), [full JSON](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/row-differences.json), [comparison notes](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/audit-notes.md).

### Saved input is not preserved completely in the solver request

Two concrete conversion defects were observed in the supplied model. These findings concern the current saved-document-to-request conversion; no repaired analysis was run.

1. **Notice points are dropped.** `FrameWebforCS/calculation/CalculationRequest.cs:40`–`:42` copies optional sections only when they are `JsonObject`s. The supplied `notice_points` is an array with three records, so the actual request has no `notice_points`. The member 5 midpoint at 0.566 is absent from canonical stations, which contain only 0 and 1.132 for that member. Some other requested locations survive accidentally because they coincide with existing load boundaries. Evidence: [feature/station ledger](../../../.tmp/transverse-analysis-print-verification/20261015T182603/audit/feature-stations.json).

2. **Negative-zero continuation loses its meaning in CASE 10 member loads.** `CalculationRequest.cs:282`–`:286` converts L1 to a number and tests `rawStart < 0`; the string `"-0"` therefore follows the absolute-zero branch. In contrast, `FrameWebforJS/src/app/components/input/input-load/input-load.service.ts:950`–`:957` recognizes the original string's minus sign as continuation. In the actual request, 23 nonzero patches on each pile all start at 0, including 22 rows originally using `"-0"`. For example, a later `L1="-0", L2="-2.2"` row becomes a physical interval [0, 2.2] instead of continuing from the previous interval; another `"-0", "-0.7"` row becomes [0, 0.7]. The pile length is 23.95, but all these projected intervals overlap within the first 2.2. Evidence: [CASE 10 original/request interval ledger](../../../.tmp/transverse-analysis-print-verification/20261015T182603/audit/case10-load-extents.json).

The second finding is a concrete cause candidate for CASE 10's large discrepancy, which propagates into COMBINE 5/10 and PICKUP 5. Quantifying the repaired result requires a separate authorized correction and rerun. It is not established merely by comparing two outputs.

### Signs, display units and source labels

- The current print projection carries canonical force components directly (`PrintProjection.cs:413`–`:425`, `:618`–`:635`). Reference M and current canonical/display M use different signs in clear examples such as CASE 1/member 3. PICKUP selection is consistent with the current signed components, but therefore need not select the same maximum-source combination as the reference. A blanket absolute-value comparison would hide this discrepancy and was not used.
- Basic and COMBINE displacement projection passes raw displacement/rotation components unchanged (`PrintProjection.cs:391`–`:397`). The PDF parser/formatter also keeps them unchanged while labeling the columns mm and mmrad (`ResultDisg.cs:98`–`:103`, `:312`–`:314`, `:438`–`:442`; corresponding COMBINE parsing/formatting in `ResultDisgCombine.cs`). For CASE 1/node 1, canonical Y is −0.0015542503589541191 and the printed result is −0.0016, while the reference is −1.5543. The magnitude suggests a missing legacy display conversion, but unit metadata is unspecified, and conversion alone cannot reconcile CASE 10 or rotation signs. No unit multiplier was introduced to force agreement.
- The current basic case caption is a canonical page label such as `1/Static/0 1`, rather than the reference case name such as `固定死荷重`. I-TAN/J-TAN/rigid labels, member endpoint IDs and member names are replaced/omitted in favor of station IDs `S0`, `S1`, etc.
- The PICKUP PDF prints only the selected COMBINE ID, without the reference's constituent-case expression. For COMBINE 1 displacement, the reference expression is `1+2+3+4+5`, the projected `case` source string is `1,2,3,4,5`, and the PDF prints `12345.0000`. The comma-separated source IDs are being formatted as a number; the captured projection also retains the separate weighted `comb` expression.

### Page structure and visual review

All 188 pages were rasterized at 120 dpi and reviewed in labeled contact sheets, including every discrepancy page. Reference reports use paired cases or paired maximum/minimum columns. Current force/PICKUP reports use a single vertical table; current base displacement splits one case's nodes across two columns, and COMBINE displacement prints six separate focus/extremum tables per case. All 120 COMBINE/node groups repeat the same vector six times. This explains 840 generated displacement rows versus 240 reference rows and much of the 34-page output.

Changing the harness's diagram layout cannot fix this table layout: `PrintProjection.cs:653`–`:669` applies `PrintLayout` only to diagrams, while table printers use `Table.OneUpInfo` (for example `ResultFsec.cs:499`–`:506`; COMBINE displacement mode loop `ResultDisgCombine.cs:620`–`:637`). The original 12-node displacement filter is working.

The reviewed pages show no blank pages, obvious clipped text, replacement-glyph boxes or page-boundary overflow. This limited rendering check does not establish parity: table order, grouping, headings, labels, pagination and values are visibly different. Rotation-aware word-box checks found zero words outside the page rectangle. Evidence: [reference render manifest](../../../.tmp/transverse-analysis-print-verification/20261015T182603/visual/reference-render-summary.json), [generated render manifest](../../../.tmp/transverse-analysis-print-verification/20261015T182603/visual/generated-render-summary.json), and all page PNGs/contact sheets in their linked report directories.

## Coverage and limits of matching

The comparer uses page-rotation-aware coordinates and semantic keys, not PDF text-stream order. It preserves repeated positions, labels, source IDs, printed decimal tokens and M-MAX placeholders. It neither flips signs nor converts units before comparison. Six private mutation checks verified detection of numeric/source changes, removed/added/duplicated rows and opposite sides at the same position.

| Report | Reference rows | Generated rows | Unambiguous pairs | Reference-only rows | Generated-only rows | Ambiguous groups (reference / current rows) |
|---|---:|---:|---:|---:|---:|---|
| Basic section force | 951 | 680 | 300 | 291 | 200 | 180 (360 / 180) |
| PICKUP section force | 2850 | 2040 | 900 | 870 | 600 | 540 (1080 / 540) |
| Displacement, including COMBINE | 240 | 840 | 240 | 0 | 600 | 0 |

These partitions account for every extracted row. Reference-only and generated-only mean absent from the opposite PDF at that report/case/member/position key; they are not all attributed to the same defect. Coincident reference rows cannot always be mapped to the current single station ID, so they remain explicitly ambiguous rather than being reported as numerical matches. For example, two reference pile rows at DIST 10.640 map to one current S14 location.

Among the unambiguous pairs, complete numeric vectors agree by decimal equality in 99/300 basic force rows, 152/900 PICKUP rows and 0/240 displacement rows. Labels/source associations are separate checks; this is not a percentage of the entire reference. In the 900 unambiguous PICKUP pairs, 433 selected COMBINE IDs differ and all 900 lack the printed reference constituent formula. Zero complete rows match all requested numeric/label/source fields. See [coverage](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/coverage.json), [summary](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/summary.json), [mutation checks](../../../.tmp/transverse-analysis-print-verification/20261015T182603/comparison/mutation-tests.json).

## Independent checks and validation

From the fresh canonical base results, an independent Python checker recomputed this sample's DEFINEs, all 10 weighted COMBINEs, all 5 PICKUPs, per-focus signed extrema, correlated six-component vectors and selected-source/provenance data across displacement, reaction and force families. It performed 124,200 component comparisons with zero arithmetic/source errors; maximum absolute numerical difference was 2.2737367544323206e-13. This confirms current aggregation consistency with current base results, not agreement with the external reference or correctness of request conversion.

The checker also traced raw components into print projection, original/generated entity mappings and all cases. For the fresh result, 460 dropped coincident segment endpoints differ by at most 8.88e-16 (none above 1e-10); station deduplication is not demonstrated to discard a significant force jump in this particular run. Reference duplicate station rows nevertheless remain a report/mapping difference. [Audit summary](../../../.tmp/transverse-analysis-print-verification/20261015T182603/audit/summary.json), [checker](../../../.tmp/transverse-analysis-print-verification/20261015T182603/audit/checker.py), [unit and mapping investigation](../../../.tmp/transverse-analysis-print-verification/20261015T182603/audit/mapping-unit-audit.md).

Executed existing focused tests:

- Headless real-process/stale-cache/artifact regression: 1 passed.
- Print projection suite: 21 passed.
- PDF numeric-format/node-ID/library checks: 34 passed.
- Agent infrastructure: 57 tests passed and all other `AgentOnly` checks passed. Full product Python/Angular suites were not required or claimed.

Exact commands/logs are in the harness run summary and team work logs under `.agents/logs/agent-teams/team-execute-transverse-analysis-print-verification/`. Independent evidence reviews and final file-integrity checks are recorded in the closeout below.

## Interpretation and next correction scope

The requested hypothesis of identical results is disproved. The verification identified input conversion defects and display/provenance differences that should be addressed in a separate task, in dependency order: preserve notice-point arrays and negative-zero continuation, reestablish the legacy force/rotation/unit display contract, regenerate current results, then repair report stations/source expressions and table composition before repeating this full comparison.

The JSON was modified after the reference PDFs were created, and the historical input/software version that produced them is unknown. This limits attribution of every numerical difference to a particular code defect. It does not change the observed mismatch of the supplied current input/current code against the supplied references. No structural design acceptance is certified by this comparison.

Detailed artifacts are local, gitignored files under `.tmp/`; the report and hashes are durable repository documentation, but these links are not a protected archive. Preserve that run directory alongside the report if evidence must be retained beyond local workspace cleanup.

## Closeout

- Independent [security](review-security-transverse-analysis-print-verification.md), [quality](review-quality-transverse-analysis-print-verification.md), and [test/evidence](review-tests-transverse-analysis-print-verification.md) reviews: PASS, with no unresolved actionable finding in the verification deliverable. Quality review corrected one source-line citation and clarified comma-separated projected source IDs versus the reference's plus expression.
- The test reviewer independently reparsed all six PDFs / 188 pages and reproduced the stored rows, partitions, numeric-match counts and zero unresolved decimal/placeholder tokens. The independent arithmetic replay and private mutation controls also passed. This is review of verification evidence, not acceptance of the mismatching product output.
- All four external originals and four frozen copies were independently rehashed and still match the approved baselines. Product source diff is empty. The final run manifest inventories the private evidence with byte counts and SHA-256 hashes.
- Final lead `AgentOnly` gate: `overall=pass`, 57 infrastructure tests passed, no failed check, product matrix intentionally skipped. Evidence: `.agents/logs/check-20261015T094253356Z-34556.log` and the exact JSON `.agents/logs/check-transverse-analysis-print-verification-final.json`. Product focused tests separately passed 56/56.
- All six implementer/reviewer work logs pass the document contract. Ownership reconcile reports zero overlapping and zero unowned changed paths. It lists the three implementation owners as idle because `.tmp/` is gitignored; their executed outputs, hashes, test logs and validated work logs explicitly demonstrate their work. Product code coverage is not measured.
- STATE and the approved plan now link to this result. Detailed evidence remains in the local run directory; preserve it with the report for later correction and comparison.
