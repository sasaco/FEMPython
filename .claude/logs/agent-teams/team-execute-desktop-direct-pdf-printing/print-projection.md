# Work Log: print-projection

## Summary

Mapped reachable Angular print choices to current desktop input and canonical result sources, then implemented a guarded legacy-root JSON projection. Focused tests exercise real PDF generation for input, static results, and moving-load envelopes.

## Tasks Completed

- [x] Recorded IDs 0–9 and 11–15, root payload keys, filters, layouts, scale, diagram paths, and disabled historical IDs 10/16 with file and line evidence in the parity inventory.
- [x] Added `PrintSelection`, `PrintSnapshot`, `PrintDiagramRequest`, `PrintAvailability`, and `PrintProjection` in `FrameWebforCS/providers/printing/PrintProjection.cs`. Capture detaches UI input and stores document/input revisions and canonical/derived references; Build emits a complete root JSON string for `DirectPdfGenerator.Generate`.
- [x] Projected canonical static, nonlinear load-step, modal displacement, and grouped moving-load results to legacy `disg`, `reac`, `fsec`, `*Name` shapes. Invalid modal reaction/section selections fail clearly. Moving extrema retain signed max/min whole-vector values and source-case IDs.
- [x] Projected DEFINE/COMBINE/PICKUP data from immutable presentation data, including topology station positions. Prepared ordered 3D PNG requests, including six force components and derived force samples, for the viewport agent. 2D diagrams use `diagramInput`/`diagramResult` vector payloads.
- [x] Added isolated focused test project because the aggregate desktop test project has unrelated compile errors. Fifteen projection tests pass, including three direct PDF calls, simultaneous family selections, independent load/result diagram filters, and negative/stale tests.
- [x] Split component selection into displacement, reaction, section force, result diagram, and 2D load diagram filters. Legacy `Components` remains a fallback for existing callers; each new filter is detached at capture and validated against its own component names.
- [x] Addressed quality review: base 2D `PrintDiagram` includes selected displacement output in legacy order and conditionally attaches its data; mixed static/modal/reactionless result sets choose eligible pages per family. The same eligibility guard applies to 3D capture requests.
- [x] Added regressions for displacement-only and mixed force/displacement 2D diagrams, mixed default and explicit case filters, and A4/A3 PDF page geometry. The focused suite now passes 21/21.

## Files Modified

- `FrameWebforCS/providers/printing/PrintProjection.cs`
- `FrameWebforCS/tests/PrintProjectionTests.cs`
- `FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj`
- `.agents/docs/research/print-parity-desktop-direct-pdf-printing.md`

## Communication with Teammates

- Lead and print UI: sent stable option IDs, independent family filter API, availability API, and build/test results. Confirmed legacy static/COMBINE/PICKUP share family editors; UI can bind five independent filter groups.
- Print viewport: agreed neutral `PrintDiagramRequest`/`PrintSectionForcePoint` DTOs, scoped derived sample installation, and one 3D load image per case. Reported the empty-request capture guard issue for input-only printing.
- Print library: confirmed `DirectPdfGenerator.Generate(string, CancellationToken): byte[]` and legacy-root JSON contract.

## Issues Encountered

- Direct PDF test found `notice_points` must be a JSON array, matching `InputDataService.GetSaveJson()`; corrected the fallback and fixture. Input-only PDF generation then passed.
- The aggregate `FrameWebforCS.Tests` project does not compile because existing `LoadDisplayConversionTests.cs` references a missing type. The isolated `PrintProjection.Tests` project passes 15/15 via `dotnet test FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj --no-restore --filter FullyQualifiedName~PrintProjectionTests -v:q -p:WarningLevel=0`.
- `& .agents/check.ps1 -AgentOnly` passed agent pytest, detector, document contracts, and diff check, but overall failed scope-isolation because concurrent print-library files are absent from the checker allowlist. The lead owns final integration gate handling; log `.agents/logs/check-20260930T104532490Z-36380.log`.
- The final `& .agents/check.ps1 -AgentOnly` rerun after the filter changes had the same scope-isolation failure; all other agent infrastructure checks passed. Log: `.agents/logs/check-20260930T110221175Z-39844.log`.
- Product `dotnet build FrameWebforCS/FrameWebforCS.csproj --no-restore` and the isolated tests passed during integration. A rendered-page review, interactive GL capture, physical/virtual printer test, and shared solution gate remain lead-owned end-to-end checks.
- Final review-fix rerun: isolated projection tests passed 21/21; `dotnet build FrameWebforCS/FrameWebforCS.csproj --no-restore -clp:ErrorsOnly` succeeded with 0 errors (1382 existing warnings). PDF geometry assertions cover A4 portrait and A3 landscape; page text and visual legacy comparison remain integration review gates.
