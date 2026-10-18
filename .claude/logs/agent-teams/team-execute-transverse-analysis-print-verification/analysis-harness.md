# Work Log: Analysis Harness

## Summary
Executed approved verification-plan Steps 2 and 3 through current saved-document request, embedded Python runtime, strict canonical result parsing, derived presenter, print projection, and direct PDF generation. One fresh sample solve generated all three requested reports; numerical and presentation agreement is assessed independently by the comparison and audit workers.

## Tasks Completed
- [x] Created an isolated net10.0-windows x64 UseWindowsForms friend-assembly harness with a real desktop project reference.
- [x] Proved the known axial fixture gives dx=0.0004 within 1e-12, ignores both stale saved-result roots, and generates a real PDF.
- [x] Verified frozen sample SHA-256 c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553 before one fresh sample solve.
- [x] Persisted saved input, actual request, strict canonical wire result, explicit full derived results and input definitions, case-projection inventory, three print JSON datasets and settings, three PDF byte streams, run summary and artifact hashes.
- [x] Used original 12 node IDs only in displacement print selection; retained unfiltered canonical and derived results with all 58 generated/original nodes and 11 members.
- [x] Confirmed all 10 saved cases are analyzed, no cases skipped, and static solver diagnostics contain no warnings.
- [x] Ran existing Headless stale-result real-process regression: 1 passed, 0 failed, 0 skipped.
- [x] Ran focused PrintProjection tests: 21 passed, 0 failed, 0 skipped.
- [x] Ran PDF numeric-format, displacement-node-ID layout, and library tests: 34 passed, 0 failed, 0 skipped.
- [x] Work-log contract passed; .agents/check.ps1 -AgentOnly passed all infrastructure checks, with product matrix intentionally skipped. Gate log: .agents/logs/check-20261015T093304476Z-15248.log.

## Files Modified
- `.tmp/transverse-analysis-print-verification/20261015T182603/harness/Verification.csproj`: private executable with AssemblyName FrameWebforCS.Tests and production project reference.
- `.tmp/transverse-analysis-print-verification/20261015T182603/harness/Program.cs`: actual current production call chain, known-result smoke, single sample solve, artifact serialization and real PDF output.
- `.tmp/transverse-analysis-print-verification/20261015T182603/harness/finalize_manifest.py`: records Python/numpy/scipy/.NET/HEAD versions, exact commands, targeted test counts, settings and final artifact hashes without changing analysis output.
- `.tmp/transverse-analysis-print-verification/20261015T182603/harness/output/**`: private client evidence; see run-summary.json.
- `.agents/logs/agent-teams/team-execute-transverse-analysis-print-verification/analysis-harness.md`: this resolved work log.
- No product files, external originals, dependencies or lockfiles changed.

## Key Decisions
- Kept smoke and sample in one embedded runtime lifetime; awaited runtime idle between solves and disposed runtime after all reports.
- Serialized internal derived case properties explicitly; canonical.json is the unchanged production snake_case wire, while derived.json and derived-input.json use camelCase with full vectors/sourceCaseId/provenance.
- Fixed culture to ja-JP, title to 線路直角方向, A4 Horizontal, and used original node IDs for both basic and combined displacement. No numeric interpretation, scaling or sign adjustment was added by the harness.
- Canonical units are consistent_user_defined with all base units unspecified. This limits physical unit attribution and is reported to the independent auditor and lead.
- Current displacement PDF has 34 pages: 4 basic plus 30 combined. The print JSON proves each family/mode has exactly 12 original nodes; extra pages arise from six COMBINE modes and product table composition, not generated-node leakage.
- PrintLayout applies only to vector diagrams (PrintProjection.cs:653-669). It cannot reproduce reference paired table columns: ResultFsec.cs:499-506 uses Table.OneUpInfo. No alternate layout regeneration or additional sample solve was performed.
- Retained all observed sample/reference differences for the comparison team; passing infrastructure/product smoke tests does not establish reference agreement.

## Communication with Teammates
- -> lead: production success, complete artifacts, all-case inventory, unspecified-unit warning, targeted test results, and source evidence explaining layout/page differences.
- -> pdf_comparison: exact paths for three PDFs and three projection JSON files, readiness, settings, and explanation of 34 displacement pages.
- -> derived_audit: exact camelCase derived schema, original snake_case canonical schema, artifact readiness, case inventory and unspecified-unit warning.
- <- lead: frozen sample location and exclusive .NET build ownership; investigated layout equivalence and filtering as requested.
- <- pdf_comparison: observed numerical/sign/scale differences; retained all raw stages for independent attribution.

## Issues Encountered
- Initial private project reference had one excess parent component and failed harness compilation; corrected only Verification.csproj before successful production build. Subsequent build passed with existing dependency/compiler warnings (2664 warnings, no errors).
- Several exploratory source path/glob queries used nonexistent guessed names or unsupported PowerShell wildcard shapes; corrected through rg --files/source discovery, without edits to product code.
- Existing MSB3277 WindowsBase/WebView2 and other compiler warnings are retained in private build/test logs; targeted tests all pass.
- PROGRESS.md is missing as already recorded in the approved plan; context-loader reported missing ["PROGRESS.md"], unreadable [], warnings [], design.placeholder=false and progress.entries=0.

