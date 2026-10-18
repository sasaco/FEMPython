# Quality Review: Desktop Spread/PDF Numeric Display Parity

## Scope

Read-only review of the task-owned input and result component changes, `CalculationResultTableWriter`, new format helpers, and focused desktop/PDF tests against the approved plan and `FramePrintPDF` table formatters. The unrelated Headless changes were excluded.

## Findings

- **Low — stale cell type on a shrinking COMBINE grid.** `FrameWebforCS/components/input/InputCombineComponent.cs:106-110` assigns `PrintNumberCellType("F2")` to every coefficient column, then moves the trailing Name column left as case count shrinks without resetting its cell type. A former coefficient column therefore remains `PrintNumberCellType` while holding names. Today the helper derives from `GeneralCellType`, delegates string formatting to its base, and does not override parsing, so I found no proven name display/edit failure. Set the trailing Name column to `GeneralCellType` on every rebuild and cover grow-then-shrink with a name edit to keep the column contract explicit.

## Checks and Observations

- The print mapping in the changed input code matches the inspected PDF calls: node/member/spring/notice/load/COMBINE F2/F3/E2, and `InputElement` uses strict `< 999` for 3D A/J/Iy/Iz while 2D remains fixed F4/F6.
- `PrintNumberCellType` formats bound `float` via its round-trip decimal, consistent with the saved JSON/PDF parse path, and preserves nonnumeric spring markers by delegating to `GeneralCellType`. The load-length cell type formats invariant numeric strings in the active culture and returns raw strings from parsing.
- Canonical result rows configure text cell types before writing formatted text; modal and moving envelope paths retain their existing G10 formatting. Legacy/derived result paths use F4/F2/F3 with current culture while retaining the existing value scaling and provenance paths.
- The focused tests exercise saved values, unchanged edit commits, spring outline `***`, a load-length edit, the 3D 999 boundary, positive/negative F2 and culture cases. I did not execute UI, build, or performance gates in this read-only review; the lead owns integration verification.
- The plan's normal/125% DPI clipping and 100,000-row timing/working-set checks require live UI and representative data. Source inspection alone cannot certify those criteria.

## Recommendation

Reset the COMBINE Name column cell type on each dynamic rebuild. No other actionable code defect was established in this review.
