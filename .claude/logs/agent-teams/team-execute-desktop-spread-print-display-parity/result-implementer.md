# Work Log: Result Implementer

## Summary
Aligned the existing WinForms result numeric text with the corresponding PDF fixed precision, rounding, and current-culture decimal separator. Preserved raw result values, presentation scaling, aggregation, provenance, and the modal/moving result formats with no PDF counterpart.

## Tasks Completed
- [x] Canonical base displacement, reaction, and section-force tables use F4, F2, and F3/F2 respectively. Static force-result numeric columns use one text cell type per column so Spread retains fixed trailing zeros and negative zero.
- [x] Canonical COMBINE/PICKUP derived views use the same numeric text formats without changing source selection or value conversion.
- [x] Audited all nine legacy result components. Updated base and COMBINE display formatters and the three PICKUP projection formatters; PICKUP Reac/Fsec components inherit the formatted projection route.
- [x] Verified result formatting with PDF-backed 2D/3D, ja-JP/de-DE, midpoint, and negative-zero tests; checked Release build and agent infrastructure gate.

## Files Modified
- `FrameWebforCS/calculation/CalculationResultTableWriter.cs`: Format canonical static force results by field and preserve assigned text in Spread; leave G10 modal and moving envelope paths intact.
- `FrameWebforCS/components/result/ResultPrintNumberFormat.cs`: Centralize current-culture F4/F2/F3 numeric presentation.
- `FrameWebforCS/components/result/CalculationDerivedViewRenderer.cs`: Format canonical COMBINE/PICKUP result text without JavaScript pre-rounding.
- `FrameWebforCS/components/result/ResultDisgComponent.cs`, `ResultReacComponent.cs`, `ResultFsecComponent.cs`: Align legacy base text, retaining existing displacement scaling and section station construction.
- `FrameWebforCS/components/result/ResultCombineDisgComponent.cs`, `ResultCombineReacComponent.cs`, `ResultCombineFsecComponent.cs`: Align legacy COMBINE numeric text and station display.
- `FrameWebforCS/components/result/ResultPickupDisgAggregator.cs`, `ResultPickupReacAggregator.cs`, `ResultPickupFsecAggregator.cs`: Align display-only PICKUP projection strings; retain nonfinite FSEC fallback.

## Key Decisions
- Format the existing post-aggregation numeric values directly with .NET F4/F2/F3 and `CurrentCulture`, matching `printManager.toString` on the same numeric value. Do not alter aggregators, stored values, or screen-side displacement scaling.
- Configure text cell types only on canonical static numeric columns. Default Spread cell formatting normalizes assigned `-0.0000` to `0`, so a string assignment alone does not preserve the PDF display.
- Keep `ResultCombineFsecAggregator.ToStationMap` invariant F3 because it is an identity key for aggregation, not display text. Keep modal and moving envelope G10 for lack of a PDF counterpart.

## Communication with Teammates
- -> Lead: Reported legacy PICKUP formatting location, preserved modal/moving paths, Release build and gate results, Spread negative-zero failure and fix, and remaining station identity formatting.
- -> Tester: Shared expected PDF midpoint/current-culture contract and requested canonical/legacy focused cases. Tester confirmed 2D/3D, culture, and negative-zero result cases pass.
- <- Lead: Directed preservation of nonfinite/null behavior and the relevant `-AgentOnly` gate.
- <- Tester: Confirmed PDF oracle `7.625 -> 7.62`, `-7.625 -> -7.62`, and de-DE `7,62`.

## Issues Encountered
- The first result-only run passed 19/23 tests; four new canonical negative-zero cases failed because Spread displayed assigned `-0.0000` as `0`. Per-column `TextCellType` resolved it; the same result filter then passed 23/23.
- An initial unrestricted `.agents/check.ps1` run was stopped before output when the lead clarified that only the agent gate was relevant. The subsequent `-AgentOnly` invocation passed.
- `dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CanonicalBaseTablesUsePdfDigitsAndCultureWhileRetainingDisplayScaling|FullyQualifiedName~ResultCombineFsecCtParityTests|FullyQualifiedName~ResultCtBaseViewTests|FullyQualifiedName~ResultCtDisgParityTests|FullyQualifiedName~ResultCtReacPrecisionTests|FullyQualifiedName~CalculationResultUiTests|FullyQualifiedName~ResultPickupTests' -clp:ErrorsOnly`: passed 23/23, exit 0.
- `dotnet build FrameWebforCS/FrameWebforCS.csproj -c Release --no-restore -clp:ErrorsOnly`: passed, 0 errors, exit 0.
- `& .agents/check.ps1 -AgentOnly -AllowProductPath 'FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs'`: overall pass, exit 0.
- `git diff --check -- FrameWebforCS/calculation/CalculationResultTableWriter.cs FrameWebforCS/components/result`: exit 0.
