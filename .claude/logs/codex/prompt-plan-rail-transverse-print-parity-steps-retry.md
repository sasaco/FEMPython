Write the requested ordered verification steps now. Do not ask what the target is: it is fully specified below. Do not edit files.

Target: Given original input K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json, run the CURRENT FrameWebforCS calculator and PDF printer and determine whether its three generated reports match these CURRENT K: references: 03_analysis\05_線路直角方向の計算\基本.pdf (section forces), PICK UP.pdf (PICKUP section forces), 変位量.pdf (base and COMBINE displacements). The user requires matching values, labels/order, and visual appearance.

Known baseline: Current reference PDFs are already hash-identical to frozen updated snapshots. Existing report .agents/docs/research/transverse-analysis-print-corrections-report.md:75-106 compared a corrected COPY of the input, with 22 CASE10 row-ID/layout-reference edits, to these PDFs and found numeric matches 904/950, 2470/2850, 173/240; 215 PICKUP source IDs differ and member names are absent. The original K: JSON has not been rerun against these references, so do not reuse corrected-copy numbers as its verdict. Existing production path is FrameWebforCS/Headless/HeadlessCalculation.cs:35-108; print UI shares PrintProjection and DirectPdfGenerator. Existing private harness/comparer/visual tools and final-9 evidence exist under .tmp/transverse-analysis-print-verification/20261015T182603 and .tmp/transverse-analysis-print-corrections/20261015T193610. Do not overwrite them. No product changes in this verification plan; discrepancies should be classified and reported for separate correction. Preserve K: originals.

Produce exactly four sections:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions

Each step must name an observable acceptance check. Cover frozen hashes, a fresh original-input production calculation/three-PDF run, complete row/provenance and printed-token comparison, all-page visual comparison including clipping/fonts/overlap/layout, and an evidence-backed report distinguishing differences from unknown historical input identity. Avoid byte-identical PDF as the sole visual criterion. State any genuinely unresolved question, but do not ask for information already in this prompt.
