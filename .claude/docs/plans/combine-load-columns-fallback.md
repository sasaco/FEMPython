## Implementation Plan: COMBINE Load Column Fallback

### Purpose
Replace the fixed DEFINE headings in the C# COMBINE sheet. When no effective DEFINE case exists, show editable columns for the effective Load cases and preserve those coefficients through save and reload, following the legacy client.

### Scope
- New files: `FrameWebforCS.Tests/InputCombineColumnsTests.cs` for executable case selection, persistence, and event lifetime checks, provided the concurrently created test project is available for this work when implementation starts.
- Modified files: `FrameWebforCS/components/input/InputCombineComponent.cs` for column selection, headings, refresh, and event lifetime; `FrameWebforCS/components/input/InputLoadService.cs` for a lightweight maximum effective Load case ID. The test project file needs a change only if its existing references cannot run those tests.
- Dependencies: Existing `InputCombineService.DefineRows`, `InputLoadService.CasesChanged`, COMBINE `Cn` edit and JSON save paths. `FrameWebforCS/providers/InputDataService.cs` and `InputCombineService.cs` contain concurrent work; preserve it and stop calling the dummy `GetDifineCase()` from this view.
- Excluded: Changes to the legacy Angular client, structural calculation, result aggregation, and unrelated pending edits.

### Implementation Steps

Implement the case-count query first, then apply it to column layout and the existing edit/save path.

#### Step 1: Establish the legacy case predicates and ownership baseline
- [ ] Record the current staged and unstaged diff for affected files before editing; leave unrelated changes intact.
- [ ] Count a DEFINE row only when it has at least one coefficient; a name-only row does not prevent Load fallback. Use the highest qualifying row number, not the number of rows.
- [ ] Count a Load case only when it meets the old client's `getLoadJson()`/`getNodeLoadJson()`/`getMemberLoadJson()` criteria: a nonempty name or symbol, a base reference (`fix_node`, `fix_member`, `element`, `joint`), a node entry with a numeric node ID and at least one force or rotation value, or a member entry with a substantive member/load field. `LL_pitch` alone, a bare node number, and an empty case do not count.
- [ ] Add a lightweight maximum-case query in `InputLoadService` using the above predicate and numeric case IDs, without serializing all loads on each UI edit. Keep the existing save predicate separate unless changing it is independently required.
- [ ] Treat 50 as the required display range from the user's clarification. For a selected maximum above 50, display 50 coefficient columns with an explicit warning that higher columns are hidden; retain every coefficient in service state and saved JSON. Avoid repeated warnings for the same maximum while the control remains open.
**Verification**: Automated predicate cases cover sparse DEFINE rows 1 and 7, name-only DEFINE, effective Load cases 2 and 9, `LL_pitch` only, bare node ID, and valid node/member entries. Case 50 is displayed; case 51 produces a visible warning and retains its data.

#### Step 2: Replace the COMBINE sheet's fixed column setup
- [ ] In `InputCombineComponent`, choose `D` headings from the DEFINE maximum; if that maximum is zero, choose `C` headings from the effective Load maximum.
- [ ] Create `min(50, max(5, selected maximum))` coefficient columns, followed by the existing `名称` column. Keep column positions mapped to `C1...Cn` for coefficient editing and JSON; preserve widths and the frozen name column.
- [ ] Recompute columns when the component first opens, directly after DEFINE coefficient edits, when DEFINE rows are replaced, and when Load cases change. Do not rely on the concurrently edited `RowsChanged` event for this task. Refresh visible COMBINE rows after a column count or heading change so a moved name cell and newly visible coefficients render correctly; avoid rebuilding when the count and prefix are unchanged.
- [ ] Subscribe to and unsubscribe from Load change notifications with the component lifetime, marshal callbacks to the UI thread as needed, and use `_refreshing` to prevent programmatic redraws from becoming edits.
**Verification**: Automated UI checks show `C1...` plus `名称` with no DEFINE, `D1...` when DEFINE is valid, and five coefficients with neither source. Add, remove, and reload DEFINE/Load cases while reusing the view; confirm headings and COMBINE rows update, then dispose the view and verify its event handlers are detached.

#### Step 3: Verify edit, save, and reload behavior
- [ ] Ensure every generated coefficient column reaches `SetCombineCoefficient(row, column + 1, value)` and the trailing name column still reaches `SetCombineName`.
- [ ] Exercise a fallback column beyond the old fixed range with a sparse Load ID, then save, reload, and confirm its `Cn` value and name are retained. Check switching from Load headings to DEFINE headings and back does not discard stored coefficients.
- [ ] Avoid changing the existing COMBINE serialization unless verification finds a specific failure.
**Verification**: Automated test or isolated event-driven UI harness shows an edit in fallback column `C12` in saved `combine` JSON as `C12`, surviving reload and appearing in the correct cell. Name edits and clearing write through; a temporarily hidden `C12` coefficient is retained when DEFINE reduces the displayed range.

#### Step 4: Run scoped validation and review the diff
- [ ] Run the new `FrameWebforCS.Tests` target and `dotnet build FrameWebforCS/FrameWebforCS.csproj --no-restore` from the repository root. Keep any test project changes separate from another worker's active edits.
- [ ] Inspect `git diff --check` and the final staged/unstaged diff to confirm only authorized C# behavior and the planned document changed, with existing user work preserved.
**Verification**: Automated fallback/edit/save/reload and disposal checks pass, build succeeds, and the final diff contains no unrelated edits. Record any pre-existing build failure separately with its file and diagnostic.

### Risks & Considerations
- `InputLoadService.CaseIds` may include entries that the old client's count excludes. Its C# `HasData` predicate is broader than the Angular predicate, so use a dedicated count predicate without changing persistence behavior.
- A reused COMBINE control currently builds columns only once. Event subscriptions, UI thread marshaling, and refreshing after a column count change must be tested to avoid stale headings, stale name placement, or leaked handlers.
- Sparse case numbers determine the highest displayed column, not the count of populated cases. The Load parser allows IDs up to 100,000, while the user reports normal use at 50 or below. Above 50, preserve data and explicitly warn that only the first 50 columns are visible.
- `InputDataService.cs`, `InputCombineService.cs`, `SceneService.cs`, and the newly created `FrameWebforCS.Tests` project have concurrent changes. Preserve their staged and unstaged content; coordinate test-file ownership before implementation. The dummy provider method can remain unused in this scoped implementation.
- Programmatic `FarPoint` cell assignment does not fire the user edit event; verify writeback with a real edit or direct event invocation, not assignment alone.

### Open Questions
- None blocking. The user confirmed typical Load case numbers are 50 or below; this plan displays at most 50 columns and explicitly reports larger case numbers without deleting their data.
