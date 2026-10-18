# Work Log: print-viewport

## Summary

Implemented ordered diagram PNG capture from the active `FrameWebforCS` renderer. The capture uses an off-screen GL framebuffer while a modal fully covers the viewport, then draws labels, scale, extrema, and legend from viewport presentation state. Each request changes the actual scene mode/case and restores the prior mode, result, load case, scales, selection, and scoped derived samples in `finally`.

## Tasks Completed

- [x] `ThreeComponent.CapturePrintDiagrams(IReadOnlyList<PrintDiagramRequest>)` returns ordered `PrintDiagramImage(Request, PngBytes)` values and allows empty requests without an OpenGL context.
- [x] `PrintViewportCapture` renders the existing `SceneService` into a bounded `GLRenderTarget`, reads RGBA, flips to PNG row order, and releases framebuffer resources after restoring the prior renderer target.
- [x] `ViewportTextLabels` can draw into the export bitmap. The bitmap also includes the scale control, result extrema, and gradient legend without reading desktop/window pixels.
- [x] Mode/case-specific capture supports load, displacement, reaction, section-force, COMBINE, and PICKUP routes. Derived section-force samples are installed only during capture, then previous samples and revision ordering are restored.
- [x] Bounded 120 diagrams, 4096 dimensions, 8 million pixels per image, 64 million pixels per batch, and 16 MiB PNG output per diagram; unavailable/empty result and disposed context fail clearly.
- [x] Added readback/bounds tests, state and derived-scope tests, and a Windows real-GL modal smoke executable.

## Files Modified

- `FrameWebforCS/three/ThreeComponent.cs`, `ThreeService.cs`, `ThreeResultsService.cs`, `ThreeMembersService.cs`, `ViewportTextLabels.cs`, new `PrintViewportCapture.cs`
- `FrameWebforCS/tests/PrintViewportCaptureTests.cs`, `PrintViewportStateTests.cs`, `PrintViewportSmoke/PrintViewportSmoke.csproj`, `PrintViewportSmoke/PrintViewportFocusedTests.csproj`, `PrintViewportSmoke/Program.cs`

## Communication with Teammates

- Shared the capture signature and PNG record with the lead, projection agent, and modal UI agent.
- Coordinated scoped derived section-force samples with projection; flagged that legacy `print_load` emits one capture per selected load case, prompting projection to remove duplicate axis/load screenshots.
- Obtained lead ownership for the minimal `ThreeService`, `ThreeResultsService`, and `ThreeMembersService` state API changes.

## Issues Encountered

- Aggregate `FrameWebforCS.Tests` still cannot compile due 12 preexisting `LoadDisplayConversionTests.cs` CS0103 errors. An isolated owned test project runs the viewport tests: **9/9 passed**.
- Product compilation briefly failed during concurrent UI edits; after the UI owner completed them, full-reference smoke and focused tests passed.
- `GLRenderTarget` has `Dispose()` but does not implement `IDisposable`; the capture disposes it in `finally` after restoring the renderer target.
- `& .agents/check.ps1 -AgentOnly` passed agent pytest, contracts, repository detector, script references, and diff checks; overall failed only scope isolation because the checker has no approved product paths and flags the print-library teammate's `FramePrintPDF` changes. Log: `.agents/logs/check-20260930T104340884Z-26392.log`.

## Validation

- `dotnet run --project FrameWebforCS/tests/PrintViewportSmoke/PrintViewportSmoke.csproj --no-restore -v:q -clp:ErrorsOnly`: **PASS** on target Windows with a real OpenGL context. The owner-bound modal fully covers the viewport; PNG has geometry/annotation ink and no modal magenta. Repeated capture, resize, partial failure restoration, disposed-context rejection, and empty input-only requests pass.
- `dotnet test FrameWebforCS/tests/PrintViewportSmoke/PrintViewportFocusedTests.csproj --no-restore -v:q -clp:ErrorsOnly`: **9/9 passed**.
- `dotnet build FrameWebforCS/FrameWebforCS.csproj --no-restore -v:q -clp:ErrorsOnly`: **0 errors** (1382 existing warnings when recorded).
- `git diff --check` on owned tracked files: no whitespace errors.

## Remaining Risks

- A rendered PDF page comparison for every legacy diagram mode and physical printer output is an integration gate owned by the lead. The smoke proves the scene capture and modal occlusion path, not PDF layout parity.
