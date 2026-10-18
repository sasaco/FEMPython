# Work Log: Tester

## Summary

Added PDF-derived numeric oracle tests and real STA Spread cell-display tests for input and result views. Updated two legacy result display expectations while retaining underlying numeric and provenance assertions.

## Tasks Completed

- [x] Pinned the PDF printer's F2/F3/F4/F6/E2 behavior, culture-specific separators, 2D/3D material boundary, blank/NaN handling, string-backed load parse behavior, and actual 2D/3D reaction tables in 19 oracle cases.
- [x] Exercised actual Spread `Cell.Text` for node, member, rigid, spring, load, notice, COMBINE, element, canonical 2D/3D result, and derived COMBINE/PICKUP reaction fields, including midpoint, negative-zero, current culture, dynamic columns, and unchanged-edit value preservation.
- [x] Verified COMBINE coefficient columns grow from five to six then shrink to five without leaving the Name column numeric, and its edit/save behavior and C5 precision remain intact.
- [x] Verified de-DE bound-float comma-decimal edit/save, string-backed load save/reload, and the existing calculation-request normalization; missing input coordinates remain blank while missing legacy result components retain zero display.
- [x] Updated the PICKUP midpoint display test from JavaScript-style `7.63` to PDF F2 `7.62`; kept aggregate, source-case, and budget tests.
- [x] Updated five stale canonical G10 display assertions to PDF F4/F2/F3 and added raw-value checks.
- [x] Verified focused desktop tests (23/23), full PDF tests (54/54), and the agent infrastructure gate.

## Files Modified

- `FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs`
- `FrameWebforCS/tests/SpreadPrintNumericFormatTests.cs`
- `FrameWebforCS/tests/ResultCombineFsecCtParityTests.cs`
- `FrameWebforCS/tests/CalculationResultContractTests.cs`

## Key Decisions

- PDF table text, after saved JSON float serialization and printer `double` parse, is the display oracle. The tests assert the bound float/string values independently.
- The printer's culture-dependent reparse of string-backed L1/L2 is recorded as a PDF value-parsing exception; desktop calculation strings stay unchanged.
- The calculation request currently rounds a saved L2 string of `2.34567` to numeric `2.346`; the test separately proves the original string survives save/reload under de-DE.
- Existing modal/moving format paths and unrelated result/camera behavior are outside the approved numeric parity scope.

## Communication with Teammates

- -> Input implementer: Reported 2D element float F6 and member Cg F3 round-trip mismatches; confirmed the shared formatter, string load edits, and expanded input tests pass.
- -> Input implementer: Confirmed the COMBINE 5-to-6-to-5 Name-column fix through actual Spread display/edit/save.
- -> Result implementer: Reported PDF midpoint behavior and canonical negative-zero loss; confirmed 2D/3D canonical tests pass after text-cell fix.
- -> Lead: Reported full-gate outcomes, stale contract expectations, and three remaining unrelated desktop suite failures.

## Issues Encountered

- Initial oracle/test fixture errors (raw JSON interpolation and notice-point object shape) were corrected before final verification.
- Automated keyboard paste remains unverified. Spread's editor is `GeneralEditor` rather than `TextBoxBase`, and Ctrl+V through `SendKeys` did not reach it in the STA testhost; the non-deterministic clipboard attempt was removed. Direct editor input under de-DE and bound/save values pass.
- Full desktop suite initially passed 394/399. Two failures were stale print-backed display assertions and were fixed. The final suite passed 396/399; the remaining three also failed in isolation: `ResultPageActivationTests.CombinedPagePublishesWhenInitiallyShownAfterRouteSelection` (empty route-event collection), plus two `DimensionStateTests` camera orientation assertions. The pre-implementation baseline in `.agents/logs/codex/20261006T012935Z-plan-desktop-spread-print-display-parity-verdict.err.log` records these exact three failures (385 passed, 3 failed, 388 total) before numeric-format implementation.
- Commands and outcomes: `dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj -c Release --no-restore -v q -clp:ErrorsOnly` -> 54/54 pass; `dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SpreadPrintNumericFormatTests|FullyQualifiedName~ResultCombineFsecCtParityTests|FullyQualifiedName~UnrecognizedUnitsKeepRawValuesAndShowUnspecifiedUnit|FullyQualifiedName~SectionForceTableUsesTopologyStationPositions' -v q -clp:ErrorsOnly` -> 23/23 pass; `dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj -c Release --no-restore -v q -clp:ErrorsOnly` -> 400/403 pass, exactly the three baseline failures; `& .agents/check.ps1 -AgentOnly -AllowProductPath 'FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs'` -> pass; `git diff --check` -> pass.
