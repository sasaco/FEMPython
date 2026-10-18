Objective: Decompose into an ordered, independently testable implementation plan for direct C# desktop printing.

Context:
- Purpose: Add printing to the active FrameWebforCS application. The older FrameWebforJS sends an HTTP POST to FramePrintPDF; the C# application must call FramePrintPDF directly, without an HTTP print transport.
- User-confirmed scope: Full old print functionality, all reports and output selections, PDF save and preview. All print settings and preview interaction must be within FrameWebforCS, not another application's window.
- Exclusions: no changes to the FEM solver, calculation transport, legacy Angular client, or FrameGConverter except reference-only investigation. Avoid unrelated refactors and preserving an HTTP fallback.
- Constraints: Windows PowerShell, active FrameWebforCS targets net10.0-windows. Current PDF_Manager targets netcoreapp3.1 and exposes PrintInput(string), createPDF(filename), getPdfSource(). This checkout lacks the typed PDF_Manager.Printing project despite stale DESIGN.md and prior plan references. Do not assume that absent project exists. Existing user/concurrent work must be preserved. Plan only, no implementation now.
- Current state with file:line evidence:
  * FrameWebforJS/src/app/components/print/print.component.html:358-368 invokes onPrintPDFNew; print.component.ts:780-839,849-936 constructs legacy print dictionaries; :1001-1011 JSON/string/gzip/base64; :967-983 HTTP POST; :1030-1048 previews Blob in Electron/browser.
  * FramePrintPDF/FramePrintAzure/Function2.cs:34-54 reverses encoding and invokes new PrintInput(jsonString).getPdfSource().
  * FramePrintPDF/PDF_Manager/PDF_Manager.csproj:4,29-40 targets netcoreapp3.1 with Newtonsoft.Json 13.0.1, PdfSharpCore 1.3.9, embedded fonts. PrintInput.cs:11-64 has public constructor/createPDF/getPdfSource; PrintData.cs:24-36,63-147,151-237 parses legacy root dictionaries. PdfDocument.cs:186-209 saves PDF. FramePrintAzure.csproj:3,18 already references PDF_Manager from net8. FrameWebforCS/FrameWebforCS.csproj:4-6,35-38 targets net10.0-windows and lacks PDF_Manager reference.
  * FrameWebforCS/components/menu/MenuComponent.Designer.cs:117-121 has inert print menu; MenuComponent.cs:25-31 wires calculation only.
  * FrameWebforCS/providers/InputDataService.cs:364-391 returns current input JSON, :396-408 nests results under result. FrameWebforCS/providers/CalculationResultPresentation.cs:57-61,86-112 stores canonical AnalysisResultSet/Derived and clears it on changes. FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisg.cs:68-132 expects root legacy disg/disgName data, so passing GetSaveJson() directly loses results. MenuComponent.cs:120-143 commits calculation asynchronously.
  * FrameWebforCS/AppComponent.cs:14-25 owns ThreeComponent; FrameWebforCS/three/ThreeComponent.cs:64-77 renders GL on UI thread and has no capture API.
  * Existing old options: print.service.ts:95-114 and print.component.html:22-34,39-320 cover inputs/results/derived diagrams; custom/print-custom-three/print-custom-three.component.html:89-160 covers layout/orientation/scales; further custom result filters under custom/.
  * No automated FramePrintPDF test csproj currently. PDF_Test_CLI/Program.cs:11-61 is interactive/manual. Existing CS CalculationResultPersistenceTests.cs:28-77 covers result persistence but no print tests.
  * Risks: PrintInput.cs:620-622 rethrows losing stack; PdfDocument.cs:51-70,225-274 uses global font resolver and embedded fonts; old packages and font redistribution merit audit; absent diagram capture and different result schemas need explicit mapping.

Constraints for plan:
- Order by dependency and put high-risk/high-uncertainty feasibility work first.
- Give each step a verification that detects realistic failure; include parity fixtures and current CS data, missing/invalid results, stale async state, cancellation/save failure, preview/export consistency, long reports and diagrams.
- Use concrete source/test paths. Distinguish source present here from stale design. No silent assumption that net10 can compile/run a netcoreapp3.1 print library or that its exact old options work from current CS data.
- Keep steps small; avoid implementation over-detail.

Output format:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions
