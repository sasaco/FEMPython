# Test and Evidence Review: Transverse Analysis Print Verification

## Summary

**PASS — the retained evidence supports the report's conclusion that the current three PDFs do not reproduce the supplied references.** This is acceptance of the verification evidence, not acceptance of the analysis or report output as correct. No blocking test/evidence findings were identified.

## Review Scope

- Approved plan and `.agents/docs/research/transverse-analysis-print-verification-report.md`, including the nonempty collected review diff.
- Run `.tmp/transverse-analysis-print-verification/20261015T182603/`: PDF parser/comparer, all extracted rows and coverage, differences, repetition ledger, independent arithmetic checker and audit summary, harness source, run summary, known-result smoke, and existing focused test logs.
- Coverage: **not measured** for product source. PDF extraction accounting and arithmetic comparison counts below are inventories, not code coverage percentages.
- Independent reviewer reran parser/comparer and arithmetic functions in memory with `python -B`; no sample reanalysis, product test suite rerun, or source/output/evidence rewrite was performed.

## Findings

None requiring correction to the verification deliverable.

### Evidence sufficient for the negative conclusion

1. **Every PDF page was reparsed.** `comparison/compare_pdfs.py:15` applies the page rotation matrix before grouping words. The reviewer reparsed the six PDF files and required equality with the retained `reference-rows.json` and `generated-rows.json` structures, including page/column/boxes and original decimal tokens. All 64 reference and 124 generated pages reproduce their retained extraction. Every page reports equal numeric/placeholder token and accounted-token counts, with no unresolved tokens: 14,499 reference tokens and 14,120 generated tokens.

2. **All extracted rows partition without loss.** Semantic grouping at `comparison/compare_pdfs.py:90` and `:138` includes report, case, PICKUP focus/extremum, original node/member and printed distance. Coincident reference force rows are explicitly ambiguous when current station labels cannot establish a side; they are never silently paired. The reviewer independently asserted both reference and generated row conservation:

   | Report | Reference / generated rows | Paired | Reference-only / generated-only | Ambiguous reference / generated rows |
   |---|---:|---:|---:|---:|
   | Basic force | 951 / 680 | 300 | 291 / 200 | 360 / 180, in 180 groups |
   | PICKUP force | 2850 / 2040 | 900 | 870 / 600 | 1080 / 540, in 540 groups |
   | Base + COMBINE displacement | 240 / 840 | 240 | 0 / 600 | 0 / 0 |

3. **Printed numeric equality is conservative.** `comparison/compare_pdfs.py:154` compares literal tokens and uses `Decimal` equality only to distinguish formatting from changed displayed values. Signs and units are not normalized. Reviewer recomputation confirms complete vector equality in exactly 99/300, 152/900 and 0/240 unambiguous pairs. Signed zero/token spelling differences remain formatting issues. These are counts for the paired subset, not a claim that unmatched/ambiguous rows agree. All complete-row matches including labels/source fields are zero.

4. **Provenance is checked separately from numeric vectors.** `comparison/compare_pdfs.py:160` checks source ID, constituent formula, endpoints, name and station label. Reviewer recount confirms 433 differing selected COMBINE IDs and 900 absent constituent formulas among the 900 paired PICKUP rows. The current parser deliberately records an absent PICKUP formula as `None`, so matching a numeric vector or source ID cannot hide this missing field. COMBINE displacement's malformed expression remains an association difference, and all 120 COMBINE/node groups have exactly six identical vectors; the 600 extra rows are retained.

5. **The aggregation oracle is independent of production aggregation.** `audit/checker.py:86` reconstructs single-case DEFINEs and weighted COMBINEs from canonical base vectors with `math.fsum`, then selects signed extrema and correlated six-component vectors. `:179` verifies its sample-specific coefficient/candidate constants against both saved input and captured derived input. It imports no product aggregation code. Reviewer rerun gives 124,200 component comparisons, zero arithmetic/source/provenance errors and maximum absolute difference `2.2737367544323206e-13`; raw-to-projection comparison at `:270` also reports no errors. The `1e-10` threshold is an internal arithmetic diagnostic, not a PDF acceptance tolerance. The audit correctly retains the separate missing member 5 notice station as a mapping error and reports overall `DIFFERENCES`, despite arithmetic `PASS`.

6. **Failure sensitivity was exercised.** The six extracted-row comparer mutations at `comparison/compare_pdfs.py:167` all pass: numeric, source ID, removed row, added row, duplicated row, and opposite sides of a coincident station. These exercise comparison, not synthetic PDF parsing. `audit/checker.py:314` additionally checks a known coefficient/vector/tie example and numeric/source/coefficient mutations; reviewer rerun reproduces 2/1/4541 detected discrepancies respectively. All 460 deduplicated segment endpoints were inspected numerically; their maximum vector difference is `8.881784197001252e-16`, so this run does not demonstrate loss of a significant force jump.

### Explicit limits retained in the verdict

- Reusing a parser and reproducing its outputs proves deterministic extraction; it does not independently prove every field's meaning. Page coordinates, source text, coverage accounting, product projections, reference template inspection and unequivocal numeric examples provide the additional evidence here. The six mutation tests do not claim parser corruption coverage.
- Coincident reference stations, M-MAX rows and unmatched stations remain explicit limitations. They prevent an unqualified complete-match claim and do not prevent the demonstrated mismatch verdict.
- The independent oracle validates aggregation against current canonical base results. It cannot validate the FEM base solution, restore dropped request features or establish historical input identity. The known axial fixture exercises one analytically known case only.
- Raw-to-reference rounding-interval reconciliation is not used as an acceptance oracle. The report states decimal equality and actual displayed differences; it does not certify that differing printed cells are equivalent after rounding, scaling or sign changes.
- The reviewer did not independently repeat the contact-sheet visual inspection or a full product suite. Existing visual evidence and the report's limited rendering claims remain distinguishable from the rerun numerical checks. Historical reference-producing input/software and raw physical units remain unknown, as reported.

## Test Execution Results

- Reviewer in-memory replay: six PDFs / 188 pages, full parsed-structure equality and row/token accounting; PASS, exit 0.
- Comparer mutations: 6 total, 6 passed, 0 failed. Independent aggregation mutations and known vector/tie assertions: PASS, exit 0.
- Recomputed aggregation: 124,200 components, 0 errors; saved coefficient/candidate contract and raw print-projection checks: PASS.
- Reviewed existing run evidence: known axial smoke `dx = 0.0004` with stale cache excluded; `harness/Program.cs:72`–`:82` executes the real solve and PDF path. Existing product tests: 56 total, 56 passed, 0 failed, 0 skipped (headless 1, print projection 21, PDF focused 34), confirmed in the three retained test log endings and `harness/output/run-summary.json`.
- Existing lead gate `.agents/logs/check-20261015T093745732Z-16128.log`: 57 infrastructure tests passed; `overall=pass`, 10 checks passed, 0 failed, 1 skipped. Product matrix was intentionally skipped.
- Reviewer gate `& .agents/check.ps1 -AgentOnly`: exit 0, `overall: "pass"`; `.agents/logs/check-20261015T094154418Z-13912.log`. All infrastructure checks passed; product gates are explicitly `skipped` for `AgentOnly`.
- Product code coverage: **not measured**. No percentage is inferred from the page, row, token or component counts.
