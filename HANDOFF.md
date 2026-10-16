# Handoff: Rail Transverse Numeric Differences and Print Layout

Date: 2026-10-16
Repository HEAD during verification: 018d0d0ee9453464b0b541a5344ce5a6c0acdfc6
Previous handoff (preserved byte for byte): .agents/docs/research/handoff-transverse-analysis-print-corrections-20261015.md

## Goal

Continue the owner's rail transverse report parity work. Keep numerical discrepancies separate from layout differences. The owner has named FrameGConverter as the intended place to correct print layout. Verify component ownership before editing: for the supplied saved JSON, the observed production route goes directly through FrameWebforCS print projection and FramePrintPDF PDF composition; FrameGConverter is not called. Correct the layout at the responsible source, or establish and test a concrete upstream conversion effect before changing FrameGConverter. Do not mask numerical differences with layout changes.

## Current Progress

- The approved fresh run used the unchanged K: saved JSON, SHA-256 c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553. It solved all 10 base cases once with zero warnings. Reference and generated reports have 30/60/2 A4 portrait pages. Full report: .agents/docs/research/rail-transverse-print-parity-report.md. Run manifest: .tmp/rail-transverse-print-parity/20261016T041428Z/manifest.json.
- This is the original JSON, not the earlier 22-edit corrected private COPY used by final-9. The prior COPY had 67 topology nodes; the original run has 70. Do not transfer final-9 row parity conclusions to this original-input run.
- Case counts below compare **printed numeric vectors in unambiguously paired PDF rows**. They do not prove an independent FEM engine defect. A value differing only by 0.0001 is one last displayed digit, with the underlying unrounded reference unavailable.

| ID | Basic force differing / paired rows | Base displacement differing / paired nodes | COMBINE displacement differing / paired nodes |
|---|---:|---:|---:|
| 1 | 0 / 72 | 4 / 12 | 2 / 12 |
| 2 | 0 / 72 | 3 / 12 | 0 / 12 |
| 3 | 0 / 72 | 0 / 12 | 0 / 12 |
| 4 | 0 / 72 | 0 / 12 | 4 / 12 |
| 5 | 0 / 72 | 4 / 12 | 12 / 12 |
| 6 | 0 / 72 | 2 / 12 | 3 / 12 |
| 7 | 0 / 72 | 0 / 12 | 1 / 12 |
| 8 | 0 / 72 | 2 / 12 | 4 / 12 |
| 9 | 0 / 72 | 0 / 12 | 2 / 12 |
| 10 | **60 / 72** | **12 / 12** | **12 / 12** |

- Basic force CASE 1-9 paired rows match numerically. For each of those cases, eight ambiguous same-position groups (16 rows per side) have equal numeric multisets, but their side-by-side physical pairing is unproved. Every base CASE has 7 reference-only and 10 generated-only force rows, all at different stations of member 10. Thus CASE 1-9 are not complete report parity.
- The base-displacement differences in CASE 1-9 and COMBINE differences in MIX 1-4 and 6-9 are all 0.0001 in the final printed digit. Base CASE 10 and COMBINE MIX 5/10 differ by up to 1.5281 mm; all 12 nodes differ in each. MIX 5 and 10 both include CASE 10 in the printed formula. Propagation from CASE 10 is plausible but not independently proved.
- Basic CASE 10 example: member 4, distance 0.000, printed M is reference +8.45 versus generated -28.38 (page 28 on both). Base displacement CASE 10/node 9 X is +18.6157 versus +20.1438 mm. These are substantive printed differences.
- PICKUP MIX 1-5 are envelope/selection reports, not base analysis CASE 1-10. Each has 432 paired rows; their numeric differing-row counts are respectively 20, 20, 4, 27, 360. For MIX 1-4, every numeric-different row also has a different selected source ID, and the printed selected formula does not contain CASE 10. For MIX 5, all 360 numeric-different rows have CASE 10 in the printed formula; only 6 of these rows have a different source ID. Investigate source selection separately from the base CASE 10 values.
- Source evidence: .tmp/rail-transverse-print-parity/20261016T041428Z/reference-refresh/comparison-original/row-differences.json and summary.json. The 50 basic CASE 10 keys in numeric-by-case.json merge coincident printed occurrences; the correct row count is 60/72.
- All 92 reference and 92 generated pages were rendered. All pages were reviewed in contact sheets and representative pages at full rendered size. The reference has a narrower/right-shifted table, different type, spacing, captions and page-break positions. Generated displacement expressions wrap and shift the table. Visual evidence: .tmp/rail-transverse-print-parity/20261016T041428Z/visual/. Original-size review was representative, not an exhaustive subtle-glyph audit of 184 pages.
- Focused tests passed: headless real-process smoke 1/1; transverse projection 41/41; PDF composition 27/27. Independent internal component audit passed 145800 checks. These are internal consistency checks, not external parity.
- Layout ownership evidence: FrameWebforCS/components/print/PrintDialogForm.cs:332-334 builds PrintProjection and calls PDF_Manager.DirectPdfGenerator.Generate. FramePrintPDF/PDF_Manager/Printing/PrintResult/ReferenceResultLayout.cs and ResultFsec.cs, ResultFsecCombine.cs, ResultDisg.cs, ResultDisgCombine.cs, plus Printing/Table.cs and Printing/Comon/printManager.cs define the report layout. FrameGConverter/Convert_Manager/ConvertManager.cs:16-121 reads LZH/FRD data and serializes JSON; it does not render the supplied saved-JSON PDFs. Its Function1.cs has a legacy FramePrintPDF namespace/string placeholder but is not in this verified route.

## What Worked

- Re-running the exact original JSON through CalculationRequestBuilder, PythonCalculationRuntime, strict AnalysisResultSet, CalculationDerivedPresenter, PrintProjection and DirectPdfGenerator gives traceable fresh artifacts.
- Frozen input/reference hashes, parser controls, all-token accounting, row-difference ledger and original/current page images make the finding reproducible. The manifest records commands and hashes.
- Counting paired rows by report and CASE, then separating missing/extra/ambiguous rows, gives a case-specific answer without forcing coincident stations to pair.
- The independent arithmetic audit confirms that current print/derived values are internally consistent with the current canonical result, while leaving reference correctness open.

## What Didn't Work

- Treating the earlier final-9 corrected-copy result as a run of the original JSON: its row IDs and layout references differ.
- Treating visual layout, missing names, source formulas or negative zero as an FEM numerical difference.
- Treating all numerical differences as CASE 10: PICKUP MIX 1-4 have distinct source-selection discrepancies that do not print CASE 10.
- Treating FrameGConverter as the PDF layout engine for this sample. Editing it alone cannot alter PDFs printed from the already saved JSON on the observed route.
- Passing focused tests or matching page counts alone did not establish reference parity. Historical reference input/software identity and unrounded values remain unknown.

## Next Steps

1. Read root AGENTS.md, context-loader output, this handoff, the current parity report, the manifest and current git status/diff. Preserve the two already untracked plan/report documents, the archived handoff and all private run evidence. Confirm the K: input and reference hashes before any new rerun.
2. For numerical investigation, start with base CASE 10 request/canonical/derived/print payloads and reference rows; examine the member-10 station differences separately. Test any suspected input-history or solver cause against independent data. Then inspect PICKUP MIX 1-4 source-ID decisions without attributing them to CASE 10. Keep one-digit displacement rounding discrepancies separate.
3. For layout, use the reference/current PNGs and PDF text boxes to define measurable page acceptance: A4 portrait, table x/y bounds, column widths, type size, captions, wrapping and page breaks. Compare representative basic pages 1/30, PICKUP 1/49/60, displacement 1/2 first, then review every affected page.
4. Respect the owner's FrameGConverter direction but verify a data conversion path before editing that component. If the work is physical PDF arrangement for this saved JSON, implement in the actual FramePrintPDF/FrameWebforCS path and explain the ownership finding. Do not alter conversion data to simulate margin/font changes.
5. Add focused parsed-PDF and visual regression checks for layout changes, then rerun the exact original JSON in a NEW private run. Preserve numeric row values/source IDs while changing presentation, and compare all reference/generated rows and pages. Use relevant dotnet gates and .agents/check.ps1 -AgentOnly for agent-document edits.
6. Record verified findings and remaining uncertainty in the maintained plan/report. Do not claim full parity until numeric, source, row and visual comparisons all pass. No product source was changed in this handoff task.
