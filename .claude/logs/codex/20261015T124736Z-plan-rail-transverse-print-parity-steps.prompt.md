Objective: Decompose a read-only verification into an ordered, independently testable plan. Do not implement or change product code.

Purpose: Verify whether recalculating the exact original K: transverse JSON through the current FrameWebforCS pipeline and printing three reports reproduces the current K: Basic section-force, PICK UP section-force, and Displacement + COMBINE PDFs. The user explicitly includes PDF visual appearance in the comparison.

Scope: Freeze four external input/reference files; rerun actual production calculation and print methods in a new private evidence directory; compare every numerical row, station/node/member association, source/provenance label, report structure and every page visually; report pass/fail/incomplete with traceable evidence. No product fixes or external file changes in this plan. If differences are found, classify their stage and propose separate correction work, not silent normalization.

Constraints and current state:
- Repository-root PowerShell, locked repository toolchains; no global Python. Git working tree currently clean; HEAD 018d0d0ee9453464b0b541a5344ce5a6c0acdfc6.
- Exact original JSON exists at K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json, 44097 bytes, SHA256 c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553.
- Current reference PDFs exist under K:\レールウェイコンサルタント\26_ピット\計算書\03_analysis\05_線路直角方向の計算\: 基本.pdf SHA256 3f543da4a08ac47c69034e99d5364316a61a9c6fc17b6c41646ba6ddbf707d4f; PICK UP.pdf 0dd0a1c861c0148865b6b09cfaa25ee9e9e66fc3a711e26c8a513aa0c4c75ec9; 変位量.pdf 67871f0298b11ae2060ac368ac180573d25ef557bdb169593220b80eb52fbb0. These equal prior frozen updated snapshots.
- .agents/docs/research/transverse-analysis-print-corrections-report.md:9-30 says final-9 used a corrected private COPY of the input (11 CASE10 row IDs and 11 layout references changed), not the original. :75-106 reports corrected-copy/reference numeric vector matches 904/950 basic, 2470/2850 pickup, 173/240 displacement, with 0 missing/extra/ambiguous rows, 215 differing PICKUP source IDs and missing member names. This is evidence against claiming exact parity but cannot stand in for a fresh original-JSON run.
- HANDOFF.md:110 warns the original private harness hardcodes the prior input hash and 12-original-node assertion. HANDOFF.md:136-147 points to existing comparator/manifest and unresolved gaps.
- Production route: FrameWebforCS/Headless/HeadlessCalculation.cs:35,49,61-62,92-108 builds request, calculates with Python, derives, projects, exports PDF; FrameWebforCS/components/print/PrintDialogForm.cs:330-334 uses same projection/generator for UI. FrameWebforCS/providers/printing/PrintProjection.cs:13-18 defines report modes.
- Existing reusable private harness: .tmp/transverse-analysis-print-verification/20261015T182603/harness/{Verification.csproj,Program.cs}. Existing comparison/audit/visual tools: .tmp/transverse-analysis-print-corrections/20261015T193610/{comparison/compare_pdfs.py,comparison/generated_parser.py,audit/checker.py,visual/render_pages.py}; prior final-9 evidence and manifest are under stages/final-9 and reference-refresh/comparison-final-9. Reuse in a NEW run, never overwrite history.
- Focused tests: FrameWebforCS/tests/TransverseInputCorrectionTests.cs, TransverseDisplayProjectionTests.cs, TransversePrintMetadataTests.cs, TransverseFoundationPrintTests.cs, FramePrintPDF/PDF_Manager.Tests/TransverseReportCompositionTests.cs and FrameWebforCS/Headless/tests/RunnerTests.cs. Focused regressions do not prove external parity.
- Invariant: preserve original K: files, all frozen runs and canonical raw results; do not force numerical tolerances, extrema ties, names, or reference data. Historical input/software identity for PDFs remains unknown. Current 2D vector diagram correctness is separately unverified per correction report:165-170.

Output format:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions

Make each step small and independently verifiable. Put high-risk/high-uncertainty work first. Distinguish a verification result from correction work. Include explicit visual pass criteria without demanding identical PDF bytes or reproducing documented caption collisions unless the user specified those details.
