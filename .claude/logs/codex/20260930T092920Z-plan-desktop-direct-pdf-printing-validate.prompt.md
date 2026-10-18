Objective: Validate .agents/docs/plans/desktop-direct-pdf-printing.md for completeness, correctness, and risk. Read it and relevant local code. Do not edit files.

Purpose/scope: Full FrameWebforJS printing parity in active FrameWebforCS, including all report/diagram choices, PDF preview and save inside FrameWebforCS. The C# app calls FramePrintPDF directly; no print POST.

Current state: Actual FramePrintPDF/PDF_Manager/PDF_Manager.csproj targets netcoreapp3.1 and exposes PrintInput(string), createPDF, getPdfSource; no typed PDF_Manager.Printing project is present. FrameWebforCS.csproj targets net10.0-windows and has no printing reference. MenuComponent has an inert print menu. InputDataService.GetSaveJson nests results, whereas PDF_Manager.PrintData/ResultDisg expects root legacy dictionaries. CalculationResultStore owns live AnalysisResultSet; ThreeComponent has no capture API. FrameWebforJS print.component.ts builds legacy dictionaries, sends gzip/Base64 JSON POST, and previews returned PDF. Existing FramePrintPDF automated tests are absent.

Check missing edge cases/error handling; dependency-correct step order; verification that would detect a failure; integration risks/conventions; and whether Open Questions hides a blocker. Treat old DESIGN.md descriptions of a typed printing project as stale relative to this checkout.

Output format:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
