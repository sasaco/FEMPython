# Phase 2 Code Quality Review: C# FrameWeb Screen Composition

## Verdict

**CHANGES REQUESTED** — Critical: 0, High: 3, Medium: 3, Low: 0.

The patch is not ready for screen-parity sign-off. Three reachable product defects break result-route ownership, hide required load-editing surfaces, and leave the reference optional-header pager disconnected from the actual result navigator. The remaining Medium findings cover an incorrect Contact transition/localization, retained hidden DockPanel composition debt, and a blocking verification gap. No product code was modified by this review.

## Scope and Method

The review inspected the complete 1,172,822-byte / 20,089-line patch at `.agents/logs/review-diff-csharp-frameweb-client.patch`, the current source behind `MainForm`, `ScreenComposition`, `EditorContent`, `ProjectDocumentContent`, `DataGridViewEditorController`, localization resources, the UI-parity manifest/tests, the Angular source reference, and both Angular/WinForms capture sets. Focus areas were correctness, WinForms ownership/disposal/z-order, shared-grid lifecycle, localization, clean architecture, obsolete UI removal, and exact FrameWebforJS composition.

The checked-in capture images were visually inspected. Product defects below are based on reachable source behavior; the final finding is explicitly a verification gap rather than a demonstrated runtime defect.

## Findings

### [High] H1 — A result-route transition removes the shared result grid from the newly active surface

- **Evidence:** `RoutePanelHostControl.ApplyState` evaluates `factory.CreateRouteSurface(state)` before entering `ReplaceSurface` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/RoutePanelHostControl.cs:34-37`). A new `ResultRouteSurfaceControl` immediately attaches the shared grid to its own `_gridHost` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/ResultRouteSurfaceControl.cs:67-95`). `ReplaceSurface` then disposes the old surface (`RoutePanelHostControl.cs:78-95`), and that old result surface unconditionally calls `_documentHost.ParkResultGrid()` (`ResultRouteSurfaceControl.cs:147-156`).
- **Impact:** Navigating from one calculated result route to another first moves `ResultGrid` into the new card, then the old card's disposal moves it back to the hidden parking host. The newly active route remains visible with an empty grid. This affects transitions among all nine result routes.
- **Recommended fix:** Dispose or detach the old surface before constructing/attaching the replacement, or make result-surface release conditional on the grid still belonging to that surface. Add an STA regression that navigates across at least basic displacement → reaction → section force and asserts `ResultGrid.Parent` remains inside the active surface after every transition.

### [High] H2 — The load-strength route makes required editing tables unreachable

- **Evidence:** The catalog assigns `InputLoads` three tables in the order `NodalLoads`, `MemberLoads`, `PrescribedDisplacements` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/FrameWebSurfaceCatalog.cs:56-61`). On route activation, `InputRouteSurfaceControl` calls only `ShowTable(definition.Tables[0])` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/InputRouteSurfaceControl.cs:100-120`). The surface contains only `_gridHost` and exposes no table selector or route-state field that can call `ShowTable` (`InputRouteSurfaceControl.cs:47-67,129-145`). The source-derived manifest nevertheless requires both member-load and nodal-load fields on this Angular route (`FramePrintPDF/PDF_Manager.UiTests/UiParity/framewebforjs-screen-manifest.v1.json:233-250`).
- **Impact:** Users can edit nodal loads but cannot reach member loads through the visible parity shell. Prescribed displacements are likewise unreachable. The same first-table-only design also strands the secondary set-management tables declared for other routes.
- **Recommended fix:** Model each required Angular field/group explicitly in the visible route surface, or add a typed route substate/control that selects every required backing table. Do not rely on the public `ShowTable` method being invoked by tests or hidden legacy UI.

### [High] H3 — The visible optional-header pager is disconnected from actual result paging

- **Evidence:** `OptionalHeaderControl` renders `state.Page`, enables its buttons from that value, and mutates only `ScreenRouteController.Page` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/OptionalHeaderControl.cs:158-182,348-371`). Production code never synchronizes that page state from `ProjectDocumentContent.ResultPageIndex/ResultPageCount`; the only production `SetPage`/`MovePage` callers are the optional-header buttons themselves (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/ScreenRouteController.cs:105-125`). Meanwhile, `ResultRouteSurfaceControl` creates a second pager inside the route card and drives `_documentHost` directly (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/ResultRouteSurfaceControl.cs:33-50,82-94,232-250,335-344`).
- **Impact:** The reference header remains at page 1/1 with disabled controls while result navigation happens in a second, non-reference location. Nonlinear, modal, moving, and derived pages therefore cannot satisfy the exact FrameWebforJS composition contract even when the underlying result navigator works.
- **Recommended fix:** Establish one page-state owner. Publish the document/result navigator's count and index into `ScreenRouteController`, route header page commands back to the typed navigator, and remove the duplicate card pager.

### [Medium] M1 — Contact does not perform the manifest-defined chat transition and emits English text in every language

- **Evidence:** The manifest defines Contact as `toggle contact chat` with transition `chat-open` (`FramePrintPDF/PDF_Manager.UiTests/UiParity/framewebforjs-screen-manifest.v1.json:54`). The header correctly raises `ShowContact` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/HeaderMenuControl.cs:338-345`), but `MainForm` handles it by assigning the hard-coded English message `Contact support is available from the FrameWeb support site.` and showing a generic Alert overlay (`FramePrintPDF/PDF_Manager/Shell/MainForm.cs:1087-1093`).
- **Impact:** The visible control has the wrong behavior, and Japanese/Chinese sessions receive English contact text. This is a concrete composition and localization defect.
- **Recommended fix:** Implement the manifest-defined chat/contact surface or obtain an explicit approved exception. Put every visible message in `Strings.resx` plus `Strings.en/ja/zh.resx` and test the transition and displayed text in all supported languages.

### [Medium] M2 — The new shell still depends on hidden DockPanel UI objects as service containers

- **Evidence:** `MainForm` constructs hidden `EditorContent` and `ProjectDocumentContent` objects and passes them into the new surface factory/workspace (`FramePrintPDF/PDF_Manager/Shell/MainForm.cs:80-86`). Both remain `ShellDockContent` implementations tied to DockPanelSuite (`FramePrintPDF/PDF_Manager/Shell/Contents/EditorContent.cs:7,21`; `FramePrintPDF/PDF_Manager/Shell/Contents/ProjectDocumentContent.cs:12,16`). The viewport is physically detached from the latter instead of being owned by a UI-neutral coordinator (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/WorkspaceControl.cs:30-47`).
- **Impact:** The visible obsolete panes are gone, but their UI inheritance, disposal model, and hidden control ownership remain in the composition root. This makes the shared-grid bug above easier to introduce and leaves the promised obsolete-docking removal incomplete.
- **Recommended fix:** Extract editor sessions/table controllers and viewport/result coordination into non-visual owned components. Let the parity surfaces own only the controls they display, then remove `ShellDockContent`/DockPanelSuite inheritance from the active composition path.

### [Medium] M3 — Verification gap: the visual gate covers only three Japanese 1200×800 states

- **Evidence:** The WinForms metadata contains only `overlay.start`, `shell.empty`, and `route.input-elements`, all in Japanese at 1200×800 / 100% DPI (`FramePrintPDF/PDF_Manager.UiTests/UiParity/References/winforms-v1/winforms-captures.v1.json:7,16-45`). The test explicitly requires exactly those three states (`FramePrintPDF/PDF_Manager.UiTests/UiParity/ReferenceCaptureTests.cs:73-93`). Its automated comparison is limited to the common header/navigation plus one start dialog or one Elements card (`ReferenceCaptureTests.cs:182-198`); it does not compare the remaining 13 input routes, nine result routes, Preset/Print/operation overlays, English/Chinese, 1024×768, or 1440×900 at 150% DPI.
- **Impact:** This does not prove another product defect by itself, but it prevents the patch from claiming exact screen-composition parity or responsive/localized sign-off. The supplied Angular and WinForms images also visibly differ in the excluded viewport, which the current comparison intentionally does not adjudicate.
- **Recommended fix:** Produce and validate the complete manifest-driven matrix, including all routes/overlays, ja/en/zh, reference sizes/DPI, clipping/scroll/drag-resize, and recorded human approval. Keep viewport raster equivalence separate from composition, but assert viewport bounds, grid/axis presence, clipping, and surrounding z-order.

## Areas Checked With No Finding

- **Critical:** 0 findings. No data-loss, unsafe cross-process contract, or release-blocking security defect was identified in this quality pass.
- **Low:** 0 findings. All actionable items were at least Medium because they affect required composition, lifecycle, localization, or architectural ownership.
- `OverlayHostControl` brings the overlay above the shell and unsubscribes/disposes the replaced overlay surface in a coherent order (`OverlayHostControl.cs:30-51,70-104`).
- `WorkspaceControl` removes the detached viewport from its own child collection without disposing it, leaving final renderer disposal to `ProjectDocumentContent`; no second visible viewport owner was found (`WorkspaceControl.cs:38-48,61-72`).
- Input-route disposal parks its active grid before base control disposal, and the added `Paint`/`RowPostPaint` handlers are de-duplicated before reattachment (`InputRouteSurfaceControl.cs:173-191`).
- The obsolete visible `NavigationContent` and `DiagnosticsContent` source files are removed by the patch; the remaining concern is the hidden DockPanel-derived editor/document infrastructure described in M2.

## Validation and Consultation

- Reviewed `git status --short`, complete patch inventory/diffstat, current source with exact line evidence, Angular templates/styles/manifest, parity tests, and Angular/WinForms capture images.
- No product tests were rerun in this reviewer task; the test reviewer owns independent execution/coverage assessment.
- The required read-only nested Codex consultation was attempted twice through the repository wrapper. Both calls exited 0 but returned only generic requests for the objective, despite a complete fixed prompt. Their outputs at `.agents/logs/codex/20260921T073610Z-quality-review.md` and `.agents/logs/codex/20260921T073655Z-quality-review-retry.md` were rejected as non-substantive evidence. No further retry was made under the one-retry rule.

## Explicit Counts

- Critical: 0
- High: 3
- Medium: 3
- Low: 0
