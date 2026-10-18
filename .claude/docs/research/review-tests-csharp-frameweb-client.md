# Test/Coverage Review: C# FrameWebforJS Screen Composition

## Verdict

**CHANGES REQUESTED — Critical: 0, High: 4, Medium: 4, Low: 0.**

The focused UI suite is green, but it does not establish the required screen-composition parity. The most important false-negative paths are the unverified secondary input-table reachability and the complete absence of production `ResultRouteSurfaceControl` transition coverage. Visual evidence is also limited to three Japanese 1200x800/100% states and is not coupled to the committed-reference comparison.

Coverage percentage is **not measured**. Test counts and manifest inventory counts are not statement, branch, or UI-state coverage.

## Review Scope

- Reviewed the complete patch at `.agents/logs/review-diff-csharp-frameweb-client.patch` and the current parity manifest, schema, captures, capture probe, WinForms UI tests, screen-composition production code, Angular routing/source evidence, implementation plan, and parity tester log.
- Assessed false positives/negatives and executable coverage of all 14 input routes, all 9 result routes, Start/Preset/Print and operation overlays, responsive sizes, 150% DPI, and ja/en/zh runtime language switching.
- Re-ran the focused parity, input-matrix, and vertical-integration tests. The green result is recorded below but is not treated as proof of the unexercised states.

## High Findings

### [High] H1 — The manifest completeness gate validates a hand-maintained duplicate, not an independently extracted Angular inventory

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ScreenManifestTests.cs:19-35` is the only source parser and extracts route/component pairs from `app-routing.module.ts`; it does not extract fields, controls, groups, defaults, read-only state, visibility conditions, actions, or transitions.
- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ScreenManifestTests.cs:69-82` checks only that broad source paths exist and that three top-level schema properties are required.
- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ScreenManifestContract.cs:12-103` defines the expected field, shell-control, overlay-control, and print-state inventories as constants inside the test project. `ScreenManifestContract.cs:154-197` then validates the manifest against those constants.
- The approved plan explicitly requires independent source extraction and says that a manifest entry cannot prove its own completeness (`.agents/docs/plans/csharp-frameweb-client.md:253-259`).

**Why this is a false positive**

An omitted or incorrectly modeled Angular control can be absent from both the manifest and the test constants while every mutation and equality test remains green. The current mutation tests prove that the local validator notices edits to its own data; they do not prove the local inventory matches Angular.

**Required tests**

Build an independent extractor over the Angular route templates, menu/optional-header templates, Sheet descriptors, print components, and actions. Assert exact bidirectional key/value equality for fields, controls, grouping, order, defaults, read-only/visibility conditions, actions, and transitions. Add source-side omission/extra/conditional-branch mutations.

### [High] H2 — The 14-input-route matrix bypasses the user path and therefore misses unreachable secondary tables

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/Step5EditorMatrixTests.cs:16-47` proves only the catalog's 14 route-to-table declarations.
- `Step5EditorMatrixTests.cs:58-76` constructs each surface directly and calls the public `surface.ShowTable(table)` method for every table. It never locates or operates a visible selector that a user could use.
- `Step5EditorMatrixTests.cs:82-94` checks exact field order for only Elements, Nodes, and Supports, not all 14 route screens and conditional branches.
- The shell interaction suite navigates only Elements and Nodes by calling `RouteController` directly (`FramePrintPDF/PDF_Manager.UiTests/UiParity/ShellInteractionTests.cs:15-40,49-66`).
- Production has only `_gridHost` in the route body (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/InputRouteSurfaceControl.cs:47-67`), automatically shows only `definition.Tables[0]` (`InputRouteSurfaceControl.cs:100-120`), and exposes table changes only through `ShowTable` (`InputRouteSurfaceControl.cs:129-145`).

**Why this matters**

The green matrix masks the real UI reachability contract: secondary tables such as Member Loads and Prescribed Displacements can be exercised by the test even when no user-visible control reaches them. The same weakness applies to secondary Elements, Supports, Joints, and Member Springs tables.

**Required tests**

Drive the real MainForm navigation and contextual controls for all 14 routes. For every declared table, assert a visible control can select it, the intended grid becomes parented and editable, all source-ordered fields/defaults/read-only/2D-3D branches are correct, and one representative edit commits through the real UI path.

### [High] H3 — None of the 9 production result surfaces is exercised through creation, transition, paging, or disposal

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ScreenRouteStateTests.cs:11-39` asserts counts and catalog order only. Its sole result navigation is one controller-state transition to `ResultBasicDisplacements` (`ScreenRouteStateTests.cs:43-65`).
- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ShellInteractionTests.cs:71-102` checks only that three navigation buttons become enabled; it does not open a result route.
- A repository-wide test-source search found no test reference to `ResultRouteSurfaceControl`.
- The untested transition is materially stateful: the new result surface attaches the shared grid during construction (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Surfaces/ResultRouteSurfaceControl.cs:67-95`), `RoutePanelHostControl` creates the replacement before `ReplaceSurface` (`FramePrintPDF/PDF_Manager/Shell/ScreenComposition/Core/RoutePanelHostControl.cs:34-37`), and replacement then disposes the old surface (`RoutePanelHostControl.cs:78-95`), whose disposal parks that shared grid (`ResultRouteSurfaceControl.cs:147-156`).

**Why this matters**

The tests cannot detect shared-grid detachment during result-to-result navigation, selector mirroring errors, category/substate mismatches, duplicate pager behavior, or stale page/provenance state. This is a critical user workflow across all 9 result viewers.

**Required tests**

Publish representative static, nonlinear, modal, moving, Combine, and Pickup results into a shown MainForm. Navigate every one of the 9 result routes in sequence and assert surface type/category/context, grid parent, populated columns/rows, case/state/parent/child/extrema selectors, optional-header synchronization, first/last-page boundaries, selection sync, and correct grid ownership after every replacement and final disposal.

### [High] H4 — Live visual evidence is both incomplete and disconnected from the Angular comparison

**Evidence**

- The live probe hard-codes Japanese, 1200x800, 96 DPI and captures only Start, empty shell, and Elements (`FramePrintPDF/PDF_Manager.UiTests/LiveCaptureProbe/Program.cs:33-85`).
- The process integration test only requires three generated PNGs with distinct hashes (`FramePrintPDF/PDF_Manager.UiTests/Step4VerticalIntegrationTests.cs:198-209`); it does not compare those fresh images to Angular references.
- The strict comparator instead reads three committed WinForms PNGs and metadata (`FramePrintPDF/PDF_Manager.UiTests/UiParity/ReferenceCaptureTests.cs:73-93,97-174`). A product change can therefore make the live probe output differ while the stale committed-reference comparison still passes.
- Angular metadata requires 1024x768/100% and 1440x900/150% references (`ReferenceCaptureTests.cs:18-24`), while the WinForms metadata test explicitly requires every desktop capture to be only 1200x800/100% (`ReferenceCaptureTests.cs:82-92`).

**Why this matters**

There is no current-product visual gate for the other 13 input routes, any of the 9 result routes, Preset/Print/operation overlays, 1024x768 responsive layout, or 1440x900 at 150% DPI. Fresh WinForms regressions can coexist with green committed-image tests.

**Required tests**

Generate fresh WinForms captures inside the comparison run (or bind them by verified build/source identity), then compare them to Angular references for the complete manifest-driven route/overlay matrix at 1200x800/100%, 1024x768/100%, and 1440x900/150%. Assert logical bounds, clipping, z-order, enabled/visible/text state, and non-GL pixel thresholds.

## Medium Findings

### [Medium] M1 — Runtime localization tests do not cover the full visible shell or no-clipping behavior

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/MainFormIntegrationTests.cs:100-122` switches en -> ja -> zh but asserts only the window title/culture and File menu text.
- `FramePrintPDF/PDF_Manager.UiTests/Step5EditorMatrixTests.cs:132-165` tests one active InputJoints grid, accepts merely nonblank Chinese headers, and does not assert exact translated controls or geometry.
- Desktop visual metadata is Japanese-only (`FramePrintPDF/PDF_Manager.UiTests/UiParity/ReferenceCaptureTests.cs:73-88`).

**Missing cases**

For ja/en/zh, keep representative input, result, Start/Preset/Print, wait/confirm/alert, optional-header, and navigation surfaces open while switching language. Assert every visible text against resources plus no clipping, overlap, loss of selection/page state, duplicate subscription, or route reordering. Add three-language captures at the responsive/DPI reference sizes.

### [Medium] M2 — Overlay tests assert construction/type replacement, not the required control actions and state transitions

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ScreenRouteStateTests.cs:96-128` loops overlay enum values and asserts only that the factory returns an `IFrameWebSurface`.
- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ShellInteractionTests.cs:44-67` asserts only that Preset and Print surface types replace one another while the route is preserved.
- Existing print acceptance exercises meaningful preview/export behavior, but no equivalent tests click Start New/Open/Preset, Preset choice/open/cancel, wait cancel, confirm cancel/OK, or alert OK through the parity surfaces. Focus order, default button, accessibility name, and enabled/visible defaults are also unasserted.

**Missing cases**

Add manifest-driven STA interaction tests for every overlay control and success/cancel/failure branch, including focus/default action, route preservation, repeated replacement/disposal, and overlay z-order above both route card and viewport.

### [Medium] M3 — The pixel comparator can vacuously pass when its edge mask excludes every compared pixel

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ReferenceCaptureTests.cs:201-219` builds the edge mask from both expected and actual images.
- `ReferenceCaptureTests.cs:224-257` divides by `comparedPixels` without asserting it is positive or that a minimum region percentage remains unmasked.
- `ReferenceCaptureTests.cs:260-293` dilates all detected edges. A sufficiently high-frequency divergent actual image can mask an entire region; `0 / 0` yields `NaN`, and the only failure comparison at `ReferenceCaptureTests.cs:128-133` does not reject `NaN`.

**Missing cases**

Require finite metrics, `comparedPixels > 0`, and a minimum unmasked fraction per region. Add adversarial mutations for full checkerboard/noise, large geometry shifts, solid-color replacement, and one-pixel-over-threshold changes and prove each fails.

### [Medium] M4 — Human visual approval is a declared flag, not a verified acceptance artifact

**Evidence**

- `FramePrintPDF/PDF_Manager.UiTests/UiParity/ReferenceCaptureTests.cs:47-56` only asserts that metadata says `humanApprovalRequired=true` and self-generated C# goldens are disallowed.
- The parity tester explicitly records that human approval remains outstanding and that only shell/Start/Elements have paired evidence (`.agents/logs/agent-teams/team-execute-csharp-frameweb-client/parity-tester.md:61-63`).
- The plan requires complete-matrix user visual approval before sign-off (`.agents/docs/plans/csharp-frameweb-client.md:330-335`).

**Classification**

This is a verification gap, not a newly proven product defect. Automated gates must not report screen parity complete until a dated, immutable approval record binds the exact Angular/WinForms evidence set and source/build identity.

## Explicit Zero-Finding Severities

- **Critical: 0.** No test change directly creates data loss or a security vulnerability.
- **Low: 0.** All actionable gaps are at least Medium because they affect required parity evidence or permit false-positive acceptance.

## Positive Coverage Observed

- Route/controller defaults, result navigation gating, overlay enum exhaustiveness, base shell hierarchy/z-order, and one Elements route-card geometry path have focused tests.
- The 21 typed editor tables retain substantial lower-level editing, clipboard, selection, validation, and lifecycle tests.
- The typed print pipeline has meaningful preview/export, cancellation, atomic-file, page-navigation, language-selection, and text-fidelity coverage.
- The live capture probe executes on a process-main STA and validates its three supported states without leaving the test process hung.

These strengths do not cover the missing end-user reachability, result-surface lifecycle, complete matrix, language layout, or responsive/DPI acceptance above.

## Test Execution Results

- Command: `dotnet test FramePrintPDF/PDF_Manager.UiTests/PDF_Manager.UiTests.csproj -c Release --no-build --filter "FullyQualifiedName~UiParity|FullyQualifiedName~Step5EditorMatrixTests|FullyQualifiedName~Step4VerticalIntegrationTests"`
- Result: **PASS — 40 passed, 0 failed, 0 skipped** in approximately 4 seconds.
- Coverage: **not measured**. No percentage is claimed.

## Severity Summary

| Severity | Count | Disposition |
|---|---:|---|
| Critical | 0 | None. |
| High | 4 | Independent inventory, input reachability, result lifecycle, and live visual matrix must be fixed before parity sign-off. |
| Medium | 4 | Localization breadth, overlay actions, comparator fail-closed behavior, and approval evidence remain incomplete. |
| Low | 0 | None. |
