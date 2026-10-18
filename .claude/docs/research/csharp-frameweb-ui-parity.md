# C# FrameWeb UI Parity Investigation

## Conclusion

The current C# desktop has reusable typed analysis, document, rendering,
result-presentation, and printing foundations, but its visible information
architecture is not a FrameWebforJS reproduction. The remediation must replace
the user-visible four-pane docking composition with a route/state-driven shell
that follows the Angular screen composition. Re-arranging the existing dock
panes is not sufficient.

No backup or compatibility layer is required. The existing `ProjectDocument`,
`AnalysisResultSet v1`, private loopback process boundary, edit session,
rendering lifecycle, result presentation, and typed print/export services stay
as internal foundations; the visible shell is rebuilt around them.

## Evidence and Limits

- `npm --prefix FrameWebforJS run start:local` compiled successfully and the
  root URL returned HTTP 200 with an Angular `app-root` on 2026-09-21.
- The enabled Computer Use runtime exposed neither a browser nor native-app
  surface, so no new live Angular/C# screenshots were captured. The existing
  C# screenshot recorded in `HANDOFF.md` was inspected and confirms the
  left-navigation / central document / right editor / bottom diagnostics
  docking layout.
- Source-derived structure is sufficient to plan the remediation, but fixed
  viewport/DPI Angular and C# reference captures remain the first implementation
  gate. This report does not claim live visual sign-off.

## Source-Derived Reference

### FrameWebforJS

- The route inventory is 14 input routes, 9 result routes, and named-outlet
  Start, Preset, and Print overlays
  (`FrameWebforJS/src/app/app-routing.module.ts:34-72`).
- The common hierarchy is top menu, contextual optional header, ordered left
  navigation, full workspace viewport, a movable/resizable input or result
  panel, and named-outlet overlays
  (`FrameWebforJS/src/app/app.component.html:1-18,958-980`).
- The left navigation order is Elements, Nodes, Supports, Members, Panel,
  Joints, Notice Points, Member Springs, Loads, DEFINE, followed by the three
  result categories; results are disabled before calculation
  (`FrameWebforJS/src/app/app.component.html:40-745`).
- The contextual header owns 2D/3D switching, Members/Rigid Zone,
  Load Name/Load Strength, DEFINE/COMBINE/PICKUP,
  Basic/COMBINE/PICKUP, paging, and display controls
  (`FrameWebforJS/src/app/components/optional-header/optional-header.component.html:1-66`,
  `optional-header.component.ts:105-137,176-269`).
- Most input/result routes render their route-specific column order and groups
  through the shared Sheet surface. Shared keyboard and fill behavior lives in
  `FrameWebforJS/src/app/components/input/sheet/sheet.component.ts:27-129`.
- Start is a full overlay with New, Open, and Preset tiles
  (`start-menu.component.html:1-34`, `start-menu.component.scss:1-112`).
- Print is one overlay: selection at left, live preview at right, and
  Cancel/PDF actions at the bottom
  (`print.component.html:1-75,200-370`, `print.component.scss:1-107`).
- The anonymous menu visibly includes Help and Contact/chat controls in
  addition to language and file/action controls
  (`menu.component.html:92-120`); Help opens the documented support site and
  Contact activates the chat surface (`menu.component.ts:705-706,764-766`).
  Login and authenticated MyPage/logout are the sole approved first-release
  exception because the product decision explicitly excludes login.

### Current C# Desktop

- `MainForm` opens Navigation left, Editor right, Diagnostics bottom, and the
  document in the center using a DockPanelSuite shell
  (`FramePrintPDF/PDF_Manager/Shell/MainForm.cs:102-138`).
- Navigation is a small `TreeView`, not the Angular icon order
  (`Shell/Contents/NavigationContent.cs:10-25,44-56`).
- `EditorContent` eagerly places all 21 input surfaces into one `TabControl`
  (`Shell/Contents/EditorContent.cs:25-81,91-115,234-299`).
- `ProjectDocumentContent` puts many selectors on one ToolStrip and always
  splits the viewport/result grid horizontally
  (`Shell/Contents/ProjectDocumentContent.cs:137-219`).
- The bottom diagnostics pane has no Angular counterpart
  (`Shell/Contents/DiagnosticsContent.cs:7-44`).
- Printing uses separate setup and preview dialogs rather than the Angular
  overlay composition (`Shell/Printing/PrintUiModels.cs:100-197,302-436`).
- Existing tests currently lock in the incompatible shell: four dock areas in
  `MainFormIntegrationTests.cs:13-32` and one 21-tab editor in
  `Step5EditorMatrixTests.cs:15-44,92-114`.

## Screen Parity Matrix

| Screen/state family | Angular reference | Current C# gap | Required acceptance |
|---|---|---|---|
| Empty shell | Menu + optional header + ordered left navigation + full viewport | Four visible dock regions | Same hierarchy, order, default visibility, and available workspace at fixed logical size |
| Anonymous top menu | File/actions, language, Help and Contact/chat are visible; login is a conditional anonymous control | Native menu does not reproduce the same visible controls/order | Reproduce all non-login controls, actions, focus/accessibility and failure states; record login/authenticated MyPage/logout as the explicit approved exception |
| Start | Full overlay with New/Open/Preset tiles | Menu commands only | Overlay hierarchy, close behavior, focus, and three actions match |
| Preset | Named-outlet full overlay | Preset is a service/menu concern | Four preset choices, titles, ordering, selection and close transition match |
| Elements | Route-specific Sheet on floating/resizable card | One generic editor tab | Field order/groups/read-only rules and card geometry match |
| Nodes | X/Y/Z in 3D and X/Y in 2D | Flat descriptor table | 2D/3D visibility, units, edit behavior and route transition match |
| Supports | Translation/rotation groups vary by dimension | Flat support columns | Group headers, order, dimension visibility and selection sync match |
| Members / Rigid Zone | Context header switches two related routes | Separate generic tabs | Header switch, fields, grouping, pager and shared selection match |
| Panel / Joints / Notice Points / Springs | Separate ordered routes, Panel conditional in 3D | Generic tabs, always discoverable | Navigation visibility/order and per-route columns match |
| Load Name / Load Strength | Two-state contextual header; moving loads add pitch | Generic load tabs | State switch, pager, moving-only controls and defaults match |
| DEFINE / COMBINE / PICKUP input | Three-state contextual header | Generic tabs | Header order, route transitions, fields and validation presentation match |
| Displacement | Left result category + Basic/Combine/Pickup substate | Toolbar selectors + persistent result split | Route family, substate switch, case/direction paging, fields and viewport split match |
| Reaction | Same shell pattern with reaction-specific rows | Same generic result grid | Angular grouping, ordering, enablement and extrema state match |
| Section force | Same shell pattern with force-specific rows | Same generic result grid | Angular categories, stations/components, paging and viewport integration match |
| Nonlinear / modal | Canonical results must fit the reference shell even where old Angular lacks a dedicated route | Generic selectors exist | Extend the same reference hierarchy without inventing a second shell; state labels and paging are explicit |
| Moving load | Parent/child and direction paging within result shell | Generic parent/child selectors | Order, active state, extrema and source provenance remain visible in the reference composition |
| Print | Single selection/preview/action overlay | Separate page setup and preview dialogs | One overlay, same grouping/defaults, live preview and Cancel/PDF transitions |
| Busy / confirm / error | Wait, confirm and alert overlays above the workspace | Diagnostics pane and dialogs | Modal hierarchy, focus, cancellation and safe error text match; no persistent diagnostics pane by default |
| ja/en/zh | Same route hierarchy with localized labels | Localization exists but on different controls | No clipping/overlap at reference sizes and DPI; route ordering is language-independent |

## Reuse and Replacement Boundary

Retain and adapt:

- `ProjectDocumentEditSession` and the validated edit transaction model.
- `DataGridViewEditorController` keyboard, clipboard, insert/delete, and
  selection behaviors behind route-specific screens.
- Stable entity IDs and bidirectional table/viewport selection.
- `OpenGlViewportLifecycle`, scene projection, invalidation budgets, and
  deterministic capture.
- `ResultPresentationService`, navigation, moving-load envelopes, and exports.
- Typed printing page plan, preview/export identity, budgets, and atomic save.
- Private loopback bearer, Job/listener ownership, cancellation, and
  validate-before-commit result handling.

Replace or remove from the default UI:

- `NavigationContent`, generic `EditorContent`, persistent
  `DiagnosticsContent`, and user-visible DockPanel layout restore.
- The permanent horizontal viewport/result split and monolithic result
  ToolStrip.
- Separate print setup/preview dialogs.
- Tests whose only purpose is to require the obsolete four-pane layout or
  single 21-tab editor.

Candidate new UI modules:

- `Shell/ScreenComposition/FrameWebShellControl.cs`
- `HeaderBarControl.cs`, `OptionalHeaderControl.cs`,
  `PrimaryNavigationControl.cs`, `WorkspaceControl.cs`
- `InputScreenHost.cs`, `ResultScreenHost.cs`
- `StartOverlayControl.cs`, `PresetOverlayControl.cs`,
  `PrintOverlayControl.cs`, `OperationOverlayControl.cs`
- `ScreenRouteState.cs` and `AngularScreenManifest.cs`

## Test Gaps and Required Gates

The current functional tests for editing, analysis, rendering, result
presentation, PDF generation, cancellation, and limits remain valuable. New
parity gates must add:

1. An independently extracted source inventory plus a versioned manifest
   covering every Angular route/state/outlet/header, nested print state,
   visible menu/action, field/control, conditional branch, order, grouping,
   defaults, read-only state, and transition. Exact bidirectional mapping to a
   C# item or an explicitly approved exception must reject missing, duplicate,
   and extra entries.
2. Structural UI tests for shell hierarchy, navigation order, enablement,
   route state, overlays, and focus.
3. Route-family tests for 2D/3D fields, sub-screen transitions, paging,
   clipboard/keyboard, selection sync, and empty/populated/error/cancel states.
4. Fixed logical-size and DPI `DrawToBitmap` or equivalent C# captures, plus
   Angular reference captures at the same sizes. C# self-generated images are
   not the sole golden.
5. Side-by-side human approval of the first representative input slice before
   propagating the pattern.
6. Final ja/en/zh, resize, scroll, drag/resize, and no-clipping evidence.
7. Existing solution build/test gates with obsolete layout assertions replaced,
   not weakened.

## Recommended Delivery Order

1. Capture and freeze the Angular manifest and visual references.
2. Build the common shell/navigation/overlay host.
3. Approve one representative input slice (Nodes, Elements, Supports).
4. Complete the remaining 14 input routes and contextual subnavigation.
5. Complete basic/derived/moving result routes and viewport integration.
6. Replace print dialogs with the reference overlay composition.
7. Close localization, resize/DPI, modal, error/cancel, and cleanup gates.
8. Only after parity sign-off, resume packaging, Startup cutover, and legacy
   deletion.
