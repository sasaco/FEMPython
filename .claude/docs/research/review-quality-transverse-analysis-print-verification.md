# Quality Review: Transverse Analysis Print Verification

## Result

PASS for verification quality after the lead corrected the source-label explanation and request-conversion line citation. No unresolved High, Medium, or Low quality finding. This accepts the evidence-backed mismatch verdict, not production correctness or structural design acceptance.

## Review Scope

- Approved plan and durable verification report; private run `20261015T182603` harness, comparer, coverage/summary/difference inventories, independent arithmetic/mapping checker and notes.
- Read-only inspection of the production request conversion, legacy Angular continuation rule, print projection, COMBINE displacement parser and numeric formatter.
- Representative raster review: reference/current basic force page 1, current COMBINE displacement page 5 and reference displacement page 3. The lead's separate all-page review is recorded in `visual/review.json`; this reviewer does not claim an independent inspection of all 188 pages.

## Findings

None unresolved. Two factual documentation corrections were communicated and are present in the reviewed report:

- The notice-point object-only condition is `FrameWebforCS/calculation/CalculationRequest.cs:40`–`:42`, including the actual `JsonObject` condition at line 41. The report now cites that range.
- The report's source-label explanation now distinguishes reference `1+2+3+4+5`, projected `case="1,2,3,4,5"`, and rendered `12345.0000`; a separate weighted `comb` is retained. `ResultDisgCombine.cs:238`–`:239` reads these separate strings, `:593` formats `caseStr` with four decimals, and `printManager.cs:67`–`:77` parses/formats numeric-looking text. The earlier wording incorrectly implied that a plus expression was itself numerically parsed.

## Evidence Checked

- Harness `Program.cs:30`–`:59`, `:73`–`:93` invokes existing production request/solve/strict parsing/derived/projection/PDF methods; one sample solve feeds all reports. Its separate known axial fixture checks `dx=0.0004` and strips stale saved result sections. No calculation replacement or sign/unit compensation is present.
- Actual extracted rows and page images support CASE 1/member 3/J-end/DIST 0.881 M `-22.82` versus `22.82`; CASE 10/member 10/DIST 0 N `13.52` versus `1538.53`; PICKUP 1/M MAX/member 1/DIST 0 COMBINE `6` versus `1` and M `10.41` versus `-7.40`; CASE 1/node 1 Y `-1.5543` versus `-0.0016`.
- Both inventories account for all decimal/placeholder tokens with zero unresolved entries. Reported row partitions reconcile: basic reference `300+291+360=951`, current `300+200+180=680`; PICKUP reference `900+870+1080=2850`, current `900+600+540=2040`; displacement reference `240`, current `240+600=840`.
- `compare_pdfs.py:90`–`:91`, `:145`–`:147` preserves report/case/focus/extremum/entity/distance identities and withholds forced matching for duplicate reference force stations. Ambiguous rows are excluded from the 300/900 unambiguous force pairs and their numerical-match totals. Matching is conservative; it does not prove or presume a physical side correspondence for those groups.
- `compare_pdfs.py:154`–`:161` separates Decimal-equal formatting from numeric and source/label changes. No tolerance, sign flip or scale factor approves PDF differences. Six mutation assertions cover the stated comparer failure modes. All reference/current page geometries in the captured inventories are 842 by 595 pt.
- `checker.py:86`–`:176`, `:179`–`:191` independently uses saved/sample-checked coefficients and candidate sets, `math.fsum`, stable signed extrema, correlated vectors and exact source/provenance comparison. The report correctly scopes its 124,200 comparisons and zero errors to current aggregation consistency, with maximum raw delta `2.2737367544323206e-13` and explicit `1e-10` arithmetic threshold; it does not treat these as reference agreement.
- Notice arrays excluded by the object-only request condition and CASE 10 textual negative zero losing continuation are supported by actual request/export ledgers and current source. The negative-zero comparison with Angular is explicit (`input-load.service.ts:957`–`:964`); no corrected solve was performed, so the report appropriately calls attribution of the large historical CASE 10 discrepancy a candidate.

## Considerations

- Canonical units remain unspecified. The report keeps legacy m/rad scaling conditional and does not claim that multiplying by 1000 resolves all differences.
- Matching every extracted decimal token is an extraction coverage claim, not proof of historic input identity or every physical station mapping. The report explicitly retains both limits.
- Detailed artifacts are local and gitignored. The lead added their retention caveat; evidence links require preserving the run directory.
- The all-page contact-sheet review supports obvious rendering-fault observations, not exhaustive typography certification. The report describes its rendering check as limited.

## Codex Consultations

None. Native independent evidence inspection resolved the bounded questions; no unanswered complex reasoning required a nested CLI consultation, and no CLI PASS is claimed.
