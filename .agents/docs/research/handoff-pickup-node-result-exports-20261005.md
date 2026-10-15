# Handoff: PICKUP displacement and reaction file exports

Date: 2026-10-05

## Goal

In the next session, plan and implement file exports for PICKUP displacement and support-reaction aggregates in FrameWebforCS. The user wants files analogous to the existing PICKUP section-force export. Cover both 2D and 3D analysis. The files must report the selected maximum and minimum for each focus component and node, the COMBINE ID that produced each extreme, and the full correlated result vector from that same selection. This request concerns the desktop client; do not change FrameWebforJS or the FEM calculation contract merely to add exports.

The user has asked the next session to both plan and implement this feature. The exact extensions and column layouts for displacement and reaction files have not been specified. Resolve those contracts early in the plan. Do not assume that the section-force-specific 2D .pik format is suitable for node results or for an external reader.

## Current Progress

- HEAD at handoff creation: 175d776. The working tree was clean before this documentation edit.
- The existing file-menu action in FrameWebforCS/components/menu/MenuComponent.cs enables “PICKUPファイルを出力” after derived results are ready. It writes 2D section-force .pik or 3D section-force .csv through FrameWebforCS/calculation/PickupExportFormatter.cs. This action exports section forces only.
- FrameWebforCS/calculation/CalculationResultStore.cs is not a file in this checkout. The actual store is defined in FrameWebforCS/calculation/CalculationResultPresentation.cs. Read its Current/Derived lifecycle before wiring new actions.
- CalculationResultStore.Instance.Current.Derived.Pickups is the current canonical data source. Each CalculationDerivedCase has Displacements, Reactions, and SectionForces mode maps. Each CalculationDerivedRow has EntityId, StationId, Components, SourceCaseId, and Provenance. For displacement and reaction rows, EntityId is the node ID and StationId is null.
- FrameWebforCS/calculation/CalculationDerivedPresenter.cs builds all three quantities from the same static DEFINE → COMBINE → PICKUP pipeline. For each focus component and _max/_min mode it retains the selected COMBINE ID in SourceCaseId and all correlated components in Components. Strict comparisons retain the first candidate on a tie.
- 2D displacement focus modes are dx, dy, rz; 2D reaction focus modes are fx, fy, mz. 3D uses all six displacement modes dx, dy, dz, rx, ry, rz or reaction modes fx, fy, fz, mx, my, mz. The full row vector still has six components in either dimension.
- The display renderer at FrameWebforCS/components/result/CalculationDerivedViewRenderer.cs converts translational displacement values with CalculationResultPresentation.DisplayLength (m → mm for display) and rounds displacement to four decimals, reaction to two. The derived rows themselves hold raw result values. Specify export units and rounding deliberately.
- Existing relevant tests: FrameWebforCS/tests/PickupExportFormatterTests.cs and FrameWebforCS/tests/CalculationDerivedPresenterTests.cs. The former covers the current section-force format; the latter proves the selection/source behavior for derived results.

## What Worked

- Exporting from Current.Derived.Pickups preserves the same committed result generation as the visible PICKUP tables. It avoids rereading stale legacy result JSON.
- The section-force formatter shows the useful pattern: iterate pickups and focus modes, pair max/min rows by location, export SourceCaseId with the whole selected vector, use invariant numeric formatting, reject incomplete location pairs, and neutralize spreadsheet formulas in CSV text cells.
- The existing menu action tracks CalculationResultStore.Changed and disables export while derived results are absent or rebuilding. Reuse that lifecycle for the new actions.

## What Did Not Work / Constraints

- The existing PickupExportFormatter loops over topology members and stations and labels ITAN/JTAN. Displacement and reaction results are node-based; copying that traversal or those labels would produce the wrong files. Iterate matching node results in a deterministic node order instead.
- FrameWebforCS/components/result/ResultPickupDisgAggregator.cs and ResultPickupReacAggregator.cs belong to the legacy display path. Do not use their formatted strings as the export authority when the canonical CalculationResultStore result is available.
- A previous full FrameWebforCS.Tests run during section-force export work passed 364/368. Four failures were in untouched result-page activation, camera-state, and isolated Python-import tests. The Python-import test refers to a nonexistent FrameWebforCS.Tests/FrameWebforCS.Tests.csproj path in this checkout. Focused exporter and derived-presenter tests passed 10/10. Report these separately from new regressions; do not weaken tests to make the full suite green.
- The existing root HANDOFF.md described an older Ct preset comparison. The user explicitly allowed replacing it with this handoff. That older comparison is not the current task.

## Next Steps

1. Start with AGENTS.md, the context-loader read plan, .agents/INDEX.md, and git status/diff. Confirm the current source and tests still match this handoff.
2. Define two quantity-specific output contracts before editing: filename/extension and whether 2D uses fixed-width or CSV; headers and column order; whether a 2D row includes three displayed components or all six correlated components; node order; maximum/minimum sign and tie behavior; explicit displacement and reaction units; treatment of empty PICKUP definitions and missing node rows. Prefer separate, clearly named files for displacement and reaction. If an external consumer requires a format, verify its schema rather than inferring it from .pik.
3. Design a narrow exporter over CalculationResultPresentation and Derived.Pickups. Use Displacements or Reactions maps, pair each focus _max and _min by node ID, emit the selected COMBINE IDs and correlated vectors, and reject inconsistent or nonfinite data. Preserve the existing section-force format and menu behavior.
4. Add discoverable file-menu actions for displacement and reaction output, enabled only when the current derived result is ready. Choose extensions from the approved output contract, save UTF-8 without BOM, and surface formatting or I/O failures without writing a misleading partial result.
5. Add focused tests for 2D and 3D modes, several nodes, max/min from different COMBINE IDs, correlated non-focus values, first-wins ties, deterministic ordering, units and rounding, CSV formula/quote handling if CSV is chosen, missing row pairs, and no-ready-result behavior. Keep section-force regression tests.
6. Run the focused exporter and CalculationDerivedPresenter tests, a FrameWebforCS build, git diff --check, and relevant wider tests. Investigate any failures against the four known full-suite failures before attributing them to this feature. Report exactly which formats and units were implemented.

## Completion Criteria

From a completed 2D or 3D calculation, the user can save distinct displacement and reaction PICKUP files from the desktop UI. Every emitted row identifies its PICKUP, focus component, node, maximum and minimum source COMBINE IDs, and matching correlated values. The output contract is documented in the implementation/tests; malformed or stale results do not produce apparently valid files. Existing section-force .pik/.csv output continues to pass its tests.
