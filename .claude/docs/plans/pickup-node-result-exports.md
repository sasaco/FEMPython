## Implementation Plan: PICKUP Node Result Exports

### Purpose
Let FrameWebforCS users save separate displacement and support-reaction PICKUP files after a completed 2D or 3D calculation. Each row reports the selected maximum and minimum COMBINE IDs and the full correlated vectors for one focus component and node.

### Scope
- New files: `FrameWebforCS/calculation/PickupNodeExportFormatter.cs`, `FrameWebforCS/tests/PickupNodeExportFormatterTests.cs`.
- Modified files: `FrameWebforCS/components/menu/MenuComponent.cs`, `FrameWebforCS/tests/CalculationDerivedPresenterTests.cs`.
- Dependencies: `CalculationResultStore.Current.Derived.Pickups`, `CalculationDerivedCase.Displacements` and `.Reactions`, `ResultSet.Topology.Nodes`, and `ResultSet.Units`. Keep `PickupExportFormatter` and its section-force `.pik`/`.csv` contract unchanged. Do not change Angular, FEM result contracts, or unrelated concurrent desktop files.
- File contract: two distinct UTF-8 without BOM CSV files for both dimensions, default names `FrameWebforCS.pickup-displacement.csv` and `FrameWebforCS.pickup-reaction.csv`. Header order is `pickup_id,focus_component,node_id,max_combine_id,min_combine_id`, followed by six `max_*` and six `min_*` component columns. Component order is `dx,dy,dz,rx,ry,rz` or `fx,fy,fz,mx,my,mz`; each header gives its unit. Values use raw finite result numbers and invariant round-trip formatting, without display rounding. Translation uses `ResultSet.Units.Length`, rotation `rad`, force `ResultSet.Units.Force`, and moment `Force*Length`. 2D emits focus modes `dx,dy,rz` or `fx,fy,mz` while retaining all six correlated values. 3D emits all six focus modes. Rows follow PICKUP order, focus order, then topology node order; reactions include only nodes with reaction results. Max/min remain signed and use the presenter's first-wins tie selection.

### Implementation Steps

Complete the contract and validation before adding save actions; then run the relevant gates.

#### Step 1: Fix the node CSV contract and boundary checks
- [x] Add a quantity-specific formatter over one committed `CalculationResultPresentation`, selecting the appropriate PICKUP mode map and constructing the unit-bearing 17-column header.
- [x] Reject absent derived data, empty PICKUP sets, missing focus modes, missing max/min node pairs, inconsistent reaction-node sets across focus modes, duplicate or unknown nodes, non-null station IDs, missing source COMBINE IDs/components, and nonfinite values. Escape CSV text and neutralize spreadsheet formulas.
**Verification**: Focused formatter tests cover 2D/3D focus sets, both quantities, multiple topology nodes, six correlated values, source IDs, units, ordering, malformed pairs, and nonfinite or unsafe cells.

#### Step 2: Add desktop save actions
- [x] Add separate discoverable file-menu actions for displacement and reaction while retaining the section-force action and its format.
- [x] Enable the new actions only when the current committed derived result contains PICKUP definitions. Recheck current identity and derived readiness after the save dialog, format before writing, write a same-directory temporary file, then replace the destination so a failed format or write does not leave a partial result.
- [x] Save UTF-8 without BOM and show formatting or I/O errors in the UI.
**Verification**: Build FrameWebforCS; test replacement and BOM behavior; inspect the menu lifecycle and save code for stale-result gating, distinct names, atomic replacement, and preservation of section-force behavior.

#### Step 3: Run focused and integration checks
- [x] Run `PickupNodeExportFormatterTests`, `PickupExportFormatterTests`, and `CalculationDerivedPresenterTests` sequentially with a FrameWebforCS build.
- [x] Run the wider desktop test suite once, compare any failures with the four documented preexisting failures, and run `git diff --check`.
**Verification**: New focused tests and existing section-force tests pass; build has no errors; the diff contains only planned paths plus already-owned concurrent changes.

### Risks & Considerations
- Section-force `.pik` uses member stations and fixed-width 2D records; node results require their own schema.
- Reaction node coverage is the support-node subset. An empty result is rejected rather than saved as a plausible header-only result.
- The display converts translations to mm and rounds values; file output preserves raw result units and precision, with units in every numeric column header.
- The store invalidates Derived while rebuilding; menu state and the final save operation must reject stale snapshots.
- `HANDOFF.md`, `FrameWebforCS/FrameWebforCS.csproj`, `FrameWebforCS/calculation/PythonCalculationRuntime.cs`, and `FrameWebforCS/Headless/` contain separate work and must be preserved.

### Open Questions
- No implementation blockers remain. No external node-result schema was specified; the implemented CSV contract is documented above. A future consumer-specific format would require its actual schema.

### Completion Evidence
- End-to-end implementation was authorized by the user's initial request to plan, implement, and verify; no additional implementation approval was requested.
- Focused node/section-force exporter and presenter tests: 30/30 passed. Product build: passed, 0 errors (1,383 existing warnings).
- Final wider desktop suite: 385/388 passed. The three failures remain the documented result-page activation and camera-state baseline; no new exporter regression was found.
- Native independent plan review: PASS. Initial code review findings were fixed and final code review: PASS.
- Required nested CLI consultations were attempted through `codex_consult.py`. Step decomposition produced recommendations but timed out; final validation timed out without a response. The nested CLI gate therefore has no PASS claim. Evidence: `.agents/logs/codex/20261005T134802Z-plan-pickup-node-result-exports-steps.md` and `.agents/logs/codex/20261005T145558Z-plan-pickup-node-result-exports-validate.err.log`.
- Output contract is recorded in `.agents/docs/DESIGN.md`; current implementation and baseline failures are recorded in `.agents/STATE.md`.
- Plan shape/artifact gates and `git diff --check` passed. Agent infrastructure tests/contracts passed; the aggregate checker reports only scope-isolation failure for separate concurrent FramePrintPDF changes (`.agents/logs/check-20261005T150539276Z-25644.log`).
