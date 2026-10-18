# Rail Transverse Analysis and Print Parity Verification

Date: 2026-10-16  
Repository HEAD: `018d0d0ee9453464b0b541a5344ce5a6c0acdfc6`  
Plan: [rail-transverse-print-parity.md](../plans/rail-transverse-print-parity.md)  
Private evidence: `.tmp/rail-transverse-print-parity/20261016T041428Z/`  
**Verdict: the current calculation and all three printed PDFs do not match the supplied references.**

## Scope and identity

The exact original `K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json` was used. All three current reference PDFs under `K:\レールウェイコンサルタント\26_ピット\計算書\03_analysis\05_線路直角方向の計算\` were copied and compared. Their SHA-256 values matched the approved updated reference snapshots before and after the run:

| File | Bytes | SHA-256 |
|---|---:|---|
| Original input JSON | 44,097 | `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553` |
| `基本.pdf` | 51,184 | `3f543da4a08ac47c69034e99d5364316a61a9c6fc17b6c41646ba6ddbf707d4f` |
| `PICK UP.pdf` | 136,889 | `0dd0a1c861c0148865b6b09cfaa25ee9e9e66fc3a711e26c8a513aa0c4c75ec9` |
| `変位量.pdf` | 14,766 | `67871f0298b11ae2060ac368ac180573d25ef557bdb169593220b80eb52bfbb0` |

This is **not** the corrected private input copy used by [the prior final-9 comparison](transverse-analysis-print-corrections-report.md). That copy changed 11 CASE 10 row IDs and 11 layout references. Its 67-node, 950/2,850/240-row comparison cannot be reported as a run of the unchanged K: JSON.

## Fresh production run

The copied private [harness](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/harness/Program.cs) called the production `CalculationRequestBuilder.FromSavedJson` → `PythonCalculationRuntime.CalculateAsync` → strict `AnalysisResultSetJson.Deserialize` → `CalculationDerivedPresenter.Build` → `PrintProjection.Build` → `DirectPdfGenerator.Generate` route. It requested A4 portrait, `ja`, title `線路直角方向`, and respectively `[SectionForce]`, `[PickupSectionForce]`, and `[Displacement, CombinedDisplacement]`; the last report selected the 12 original nodes. The smoke fixture excluded stale saved results and produced its expected `dx=0.0004`.

The original input solved all **10 cases once**, with zero solver warnings. It produced **70 topology nodes** and 11 original members. The three generated reports were 30, 60 and 2 A4 portrait pages. The [run summary](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/stages/original/output/run-summary.json), [full manifest](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/manifest.json), source/input/artifact hashes and retained command logs make this run reproducible. The external originals and prior `.tmp` evidence were not overwritten.

## Complete printed-row comparison

The copied updated-reference parser and portrait generated parser accounted for every decimal token. Parser mutation controls, label-association controls, crosschecks and before/after source hashes passed. [Coverage](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/reference-refresh/comparison-original/coverage.json), [summary](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/reference-refresh/comparison-original/summary.json), and [every row difference with PDF page/column/box](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/reference-refresh/comparison-original/row-differences.json) are retained.

| Report | Pages ref./generated | Rows ref./generated | Paired | Same numeric vector among paired | Missing / extra rows | Ambiguous rows on each side | Exact complete rows |
|---|---:|---:|---:|---:|---:|---:|---:|
| Basic section force | 30 / 30 | 950 / 980 | 720 | 660 / 720 | 70 / 100 | 160 | 0 |
| PICKUP section force | 60 / 60 | 2,850 / 2,940 | 2,160 | 1,729 / 2,160 | 210 / 300 | 480 | 0 |
| Displacement + COMBINE | 2 / 2 | 240 / 240 | 240 | 173 / 240 | 0 / 0 | 0 | 94 |

Ambiguous coincident-position rows are counted separately; no numerical value or source label was used to force a pairing. All 3,800/3,920 basic, 11,400/15,267 PICKUP and 720/732 displacement numeric tokens on the reference/generated sides were accounted for. The PICKUP paired rows include **209 selected source-ID differences**. The force reports also omit authoritative member/section names, so even equal numerical rows do not become complete row matches. Printed negative zero, provenance expressions and case labels add formatting/association differences.

Examples of actual printed differences, not normalized or tolerance-adjusted:

| Position | Reference | Generated | PDF page ref./generated |
|---|---:|---:|---:|
| Basic CASE 10, member 4, DIST 0.000, M | +8.45 | −28.38 | 28 / 28 |
| PICKUP 5, MOMENT MAX, member 4, DIST 0.000, M | +31.87 | −4.96 | 49 / 49 |
| Base CASE 10, node 9, X displacement (mm) | +18.6157 | +20.1438 | 1 / 1 |

The [independent arithmetic audit](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/audit/original/summary.json) passed **145,800 component checks**, with zero input/mapping/projection/derived/foundation-occurrence errors and maximum raw arithmetic delta `5.684341886080802e-14`. Its mutation controls detect numerical, source, coefficient and provenance changes. This establishes internal consistency of this fresh result; it does **not** establish reference equivalence or independent FEM correctness. The original input has 70 nodes versus 67 in the previous corrected-copy run, consistent with the different CASE 10 row identifiers; the supplied PDFs do not prove which historical input or software version produced them.

## Visual comparison

All **92 reference and 92 generated pages** were rendered at 120 dpi. An independent reviewer viewed all **48 contact sheets**, covering every page, and compared both sides at original rendered size on basic pages 1, 2 and 30; PICKUP pages 1, 2, 49 and 60; and displacement pages 1 and 2. The lead also inspected basic/PICKUP page 1 and both displacement pages. The [render manifests](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/) report A4 portrait page rectangles and **zero words outside the page bounds**. Contact-only review of the other pages does not exclude small internal glyph overlaps or defects.

The three references and generated PDFs are **not visually equivalent** even though page counts agree:

- **Basic:** The generated table begins much farther left, spans more of the page and uses different typography, column widths and row spacing. It adds a top-right page number and unit line. [Reference page 1](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-reference-section-force/page-001.png) and [generated page 1](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-generated-section-force/page-001.png) show the common layout difference; page 30 retains the same CASE/member context but different content and positioning.
- **PICKUP:** Caption, table width, COM:CASE placement and page breaks differ. [Reference page 49](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-reference-pickup-section-force/page-049.png) reaches member 10 point 26, whereas [generated page 49](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-generated-pickup-section-force/page-049.png) reaches point 32. Both side-by-side page 60 views also disagree in printed forces.
- **Displacement:** Both reports pair CASE 1–10 and MIX 1–10 on two pages, but the generated columns spread across the page and long weighted expressions wrap, shifting the table starts. [Reference page 1](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-reference-displacement-and-combine/page-001.png) and [generated page 1](../../../.tmp/rail-transverse-print-parity/20261016T041428Z/visual/original-generated-displacement-and-combine/page-001.png) show the different title/column typography and CASE 10 values.

No obvious clipped text, overlapping text or missing glyphs appeared on the full-size pages inspected. This is a failure of visual parity, not a claim that every generated page has a rendering defect. The requested reports contain tables; no separate diagram panels were established as part of these three references.

## Gates, classification and next work

Focused native gates passed: Headless real-process smoke **1/1**, transverse projection tests **41/41**, and PDF composition tests **27/27**, with zero selected failures/skips. Exact logs are under the run's `gates/` directory. These tests do not replace the external row and page comparison.

The differences are confirmed at the printed output. The extra original-input force stations, CASE 10 numerical shifts, PICKUP source choices, absent names and report layout should be investigated separately using the retained request, canonical, derived and print payloads. Arithmetic consistency alone does not locate the external mismatch in FEM calculation, input history, derived selection or print rendering. Do not change raw results, strict extrema, historical references, labels or tolerances to make this sample appear equal without independent evidence. No product source or external K: file was changed in this verification.
