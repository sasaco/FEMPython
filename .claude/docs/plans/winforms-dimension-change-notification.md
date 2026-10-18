## Implementation Plan: WinForms Dimension Change Notification

### Purpose
Selecting 2D or 3D in the legacy WinForms menu updates `InputDataService.dimension` and immediately reconfigures the displayed route's dimension-dependent items. Switching in either direction preserves entered model values, including fields hidden in 2D, and leaves calculated result values unchanged; cached routes adopt the current display dimension when opened later.

### Scope
- New files: `FrameWebforCS/components/IDimensionAwareComponent.cs` (or an equivalent small shared view contract); focused WinForms tests under `FrameWebforCS.Tests/` for menu/service routing and dimension-sensitive sheets.
- Modified files: `FrameWebforCS/components/menu/MenuComponent.cs` and, only if needed for event hookup, `MenuComponent.Designer.cs`; `FrameWebforCS/providers/InputDataService.cs`; `FrameWebforCS/AppRoutingModule.cs`; `FrameWebforCS/components/input/InputFixNodeComponent.cs`, `InputFixMemberComponent.cs`, `InputJointComponent.cs`, `InputElementsComponent.cs`, `InputNodesComponent.cs`, `InputMembersComponent.cs`; dimension-dependent result views `FrameWebforCS/components/result/ResultDisgComponent.cs`, `ResultReacComponent.cs`, `ResultFsecComponent.cs`, `ResultCombineDisgComponent.cs`, `ResultCombineFsecComponent.cs`, `ResultCombineReacComponent.cs`, `ResultPickupDisgComponent.cs` (and inherited pickup views if needed); `FrameWebforCS/three/SceneService.cs` only if the existing camera-mode change must be invoked from the menu path.
- Dependencies: Existing WinForms, FarPoint Spread, singleton service, and xUnit/STA test patterns. No new package is planned. The JSON file format and result-coordinator edits already in `InputDataService.cs` stay intact.
- Excluded: A new global notification for direct property assignment or JSON dimension loading, model coordinate conversion or deletion of out-of-plane values, result-data recalculation or mutation, changes to the Python/Angular clients, and unrelated result aggregation work.

### Implementation Steps

Implement the transition and route dispatch first, then update each view's existing sheet schema in place.

#### Step 1: Lock the switching contract and establish regression tests
- [ ] Inventory every `dimension` branch in `FrameWebforCS` and record the exact visible columns, `DataField` bindings, headers, spans, locks, widths, and camera behavior for 2D and 3D; confirm the listed views are complete before edits.
- [ ] Add isolated STA tests for 3D→2D→3D on a visible input sheet (including all six fix-node sheets), hidden 3D field preservation, current sheet preservation, and later activation of a cached view. Add a populated result-view case asserting that display items change while underlying result values stay equal. Capture initial menu state and same-dimension selection as separate assertions.
- [ ] Establish the baseline build/test result and record the staged and unstaged `InputDataService.cs` diff and concurrently edited result paths so implementation does not overwrite other work.
**Verification**: Tests demonstrate the current missing live-switch behavior while existing JSON dimension tests and the baseline build result are documented.

#### Step 2: Add one menu-origin dimension transition
- [ ] Establish a router/view preflight that commits and validates the active Spread edit before any state change. A failed preflight cancels the entire menu switch: retain the old dimension, menu checks/text, camera, and result presentation; emit no notification. Test the preflight with a stub view before wiring real sheets in Step 4.
- [ ] Add a validated 2-or-3 menu transition in `InputDataService` that changes `dimension` and emits one notification only after successful preflight and only when the menu selects a different dimension; leave JSON assignment silent. Keep the existing serialization value and JSON validation unchanged.
- [ ] Connect both menu items to that transition, initialize and update their mutually exclusive checked state and parent 2D/3D text from the service, and prevent the parent item from independently toggling. Invoke the existing scene camera-mode switch only after the transition succeeds if a live scene is registered.
- [ ] After a file open, synchronize the menu's displayed choice from the loaded service value without emitting the menu-change notification.
**Verification**: Tests cover 2D/3D/same-value menu choices, notification count and ordering, a failed active-edit preflight with zero state change, saved `dimension`, exclusive checks/text, unchanged JSON notification behavior, and camera-mode invocation where a scene exists.

#### Step 3: Route the notification to the current view
- [ ] Extend the Step 2 preflight contract with dimension refresh and make `AppRoutingModule` the single subscriber/dispatcher for the menu-origin notification. Dispatch on the WinForms UI thread, guard closed/disposed forms and controls, and avoid duplicate subscriptions.
- [ ] Apply the current dimension before showing a cached view and before the existing same-target early return, so inactive and repeated routes do not display a stale schema. Preserve `CurrentComponent`, route title, and `setActiveSheet(option)` behavior.
**Verification**: Router tests show exactly one refresh of the visible view per actual menu change, correct cached-view refresh on later navigation, no refresh for unchanged dimension, and no exception or lingering subscription after disposal.

#### Step 4: Reconfigure input sheets without losing model values
- [ ] Refactor each dimension-dependent input view in Scope so the 2D/3D schema can be reapplied to existing Spread sheets: column count, `DataField`, headers/spans, widths, protected/locked state, and host width. Keep the existing service-backed row collections and active sheet instead of rebuilding domain rows.
- [ ] Implement the Step 2 preflight in each editable input view. Clear stale schema metadata when moving in both directions; preserve hidden 3D fields in the underlying data and restore them on return to 3D.
**Verification**: STA tests exercise each affected input view in both directions, including repeated cached activation, a non-default sheet, a valid pending edit, and an invalid pending edit. Assert the full schema (`DataField`, spans, locks, widths), unchanged service values and active selection on rejected switches, restored hidden values, and no duplicate sheets/bindings.

#### Step 5: Reconfigure dimension-dependent result views
- [ ] Apply the same view contract to dimension-dependent basic, combined, and pickup result components listed in Scope, coordinating with the owners of currently edited result files before touching them. Reapply only presentation schema (visible modes/columns, bindings, headers, spans, widths) for the selected menu dimension while retaining every loaded/calculated result value and the existing case selection where valid. Preserve the selected result mode by key when still available, otherwise select a valid fallback. Map values by mode/field key rather than old positional cells; fields unavailable in a result are displayed blank, never invented.
- [ ] Separate the selected **display dimension** from a result snapshot's source dimension in combined/pickup presentation (`ResultCombineDisgComponent.cs:195-211,225-246`; `ResultPickupDisgComponent.cs:193-208`). When an async output reaches the UI thread, build its display from the latest menu dimension; a late completion must not restore an old layout. Do not change aggregation or coordinator result data.
**Verification**: With populated 2D and 3D result fixtures, switch each visible result view in both directions and navigate back to cached result views; assert changed columns/modes, unchanged underlying result values and case IDs, blank unavailable fields, and no late old-layout publication. Existing combination/pickup tests continue to pass.

#### Step 6: Integrate and validate
- [ ] Run the focused `FrameWebforCS.Tests` suite and build `FrameWebforCS/FrameWebforCS.csproj`; perform one manual WinForms route/menu check for a displayed fix-node view, an inactive cached input view, and a result view.
- [ ] Inspect `git status` and diffs against the Step 1 baseline, including the existing result-coordinator changes in `InputDataService.cs`, and confirm no unrelated product edits or lost user changes.
**Verification**: Relevant tests and build pass, 2D→3D restores hidden values and menu state, and the final diff is limited to the agreed component.

### Risks & Considerations
- Spread can retain obsolete `DataField`, header spans, locked columns, or edited-cell state when column counts shrink and expand; tests must inspect actual bindings and values, not only column counts.
- A cached view constructed at another dimension remains stale unless route activation reapplies its schema. The router's same-target early return is another required refresh path (`AppRoutingModule.cs:58-62,88-115`).
- JSON loading validates and assigns dimension after sequential service loads (`InputDataService.cs:153-199`); the menu-origin event must not fire during that partial load. A file-open action must still synchronize the menu display.
- Both menu children currently use independent `CheckOnClick` (`MenuComponent.Designer.cs:123-143`), allowing contradictory choices until the menu handler enforces one selection.
- A failed active-cell commit must be detected before `InputDataService.dimension` changes; event-time rejection cannot safely roll back already updated menu, camera, or result views.
- Result components and the unstaged `InputDataService.cs` load-coordinator block are under concurrent modification; implement against a fresh diff and coordinate ownership before editing those paths.
- Combined and pickup outputs carry their source dimension, and current code uses it to choose the visible schema (`ResultCombineDisgComponent.cs:195-211,225-246`; `ResultPickupDisgComponent.cs:193-208`). Refresh logic must use the menu dimension for presentation while keeping source values intact and rendering unavailable fields blank.
- The repository's durable desktop architecture targets .NET 8, while this request names the legacy `FrameWebforCS` WinForms project; this plan confines product edits to the explicitly requested project.

### Open Questions
- None. The user specified that input values remain, only menu operations issue the dimension-change notification, and result data remains unchanged while visible result items change. JSON loading keeps its current non-notifying service behavior; menu display synchronization after open is in scope.
