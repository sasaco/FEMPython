# Work Log: input

## Summary
Implemented the approved 3D bridge-load input workflow, including typed detached snapshots, a three-tab FarPoint editor, lossless unfinished edits, atomic persistence, case-specific solver projection, and legacy import migration.

## Tasks Completed
- [x] Added shared transfer surfaces, ordered XYZ paths, case-specific static line/area loads, explicit planes/directions, holes, and preserved independent loading meshes.
- [x] Added 3D-only sidebar/routing, mesh/label/equivalent-load toggles, plan/iso requests, bidirectional stable-ID selection, structured triangle editing, and an advanced-column toggle.
- [x] Added bridge_loads save/reload, staged validation before document replacement, draft retention, calculation revisions, bridge-only case availability, and COMBINE effective-case retention.
- [x] Projected only each case's referenced definitions to load[case].spatial_loads, preserving double precision and removing desktop names.
- [x] Migrated legacy inf_panel/line/load_inf and case-local normalized spatial input; definition-ID conflicts across cases are remapped without mixing cases. Ambiguous common normalized input is rejected with an actionable message.
- [x] Preserved bridge input on dimension changes and rejected incompatible 2D calculations and moving LL cases explicitly.
- [x] Added focused input, migration, rollback, precision, case isolation, UI, and 3D-gating tests.

## Files Modified
- FrameWebforCS/components/input/InputBridgeLoadService.cs: typed bridge data, editable rows, snapshots, validation, migration, persistence, projection, and viewport API.
- FrameWebforCS/components/input/InputBridgeLoadComponent.cs: three-tab grid editor, triangle dialog, display controls, selection, and errors.
- FrameWebforCS/components/input/InputLoadService.cs: bridge-only cases participate in saved/effective case registries.
- FrameWebforCS/providers/InputDataService.cs: atomic bridge import, save/calculation snapshot, and revision notifications.
- FrameWebforCS/components/menu/SidebarComponent.cs: bridge route visible in 3D.
- FrameWebforCS/AppRoutingModule.cs: 3D route guard, cached-editor refresh, and safe dimension exit.
- FrameWebforCS/calculation/CalculationRequest.cs: per-case referenced spatial definitions and bridge-only request retention.
- FrameWebforCS/tests/BridgeLoadInputTests.cs: roundtrip, precision, case projection, rollback, drafts, dimensions, imports, and shell-reference regression tests.
- FrameWebforCS/tests/BridgeLoadUiTests.cs: three tabs, selection, units, and 3D visibility tests.

## Key Decisions
- Default direction is global +Z; negative intensities act downwards. Line strength uses kN/m and area strength uses kN/m².
- Transfer topology stays separate from structural shell stiffness. Existing shell IDs remain legacy IDs until the Python boundary remaps them.
- Incomplete rows are retained verbatim in optional drafts metadata; calculation and bridge printing reject unfinished data instead of silently dropping rows.
- Canonical root spatial_loads imports require exactly one case. Case-local normalized definitions may share IDs only when their definitions match; differing definitions receive fresh IDs.
- Plane/tolerance/independent-mesh columns are advanced controls. Basic panel editing uses explicit triangles or shell references, with a structured triangle dialog.

## Validation
- dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj --no-restore --filter "FullyQualifiedName~BridgeLoadInputTests|FullyQualifiedName~BridgeLoadUiTests|FullyQualifiedName~BridgeLoadThreeTests|FullyQualifiedName~InputLoadLayoutPersistenceTests|FullyQualifiedName~CalculationRequestTests" -clp:ErrorsOnly --logger "console;verbosity=minimal": exit 0, 59 passed, 0 failed.
- Lead independently ran all BridgeLoad, CalculationResultContract, and LoadCase tests after the migration additions: 66 passed. Lead native smoke verified save/reload, pythonnet, solver audit, GL, and 2D behavior; editor screenshot layout was confirmed.
- .agents/check.ps1 -AgentOnly with explicit approved literal component paths: exit 0, overall: pass. Log: .agents/logs/check-20261006T152714751Z-45100.log. Product gates were skipped because AgentOnly was requested; product test evidence is separate above.
- Scoped git diff --check: exit 0.

## Communication with Teammates
- Sent exact service records/events/selection/view API to the lead before THREE integration.
- Confirmed persistence shape, signed intensity conventions, and drafts handling with PDF worker.
- Coordinated legacy shell IDs and normalized case-local directions with backend worker.
- Used the shared .NET build lock and handed it to PDF after initial 59 tests passed; lead performs final integration tests.

## Issues Encountered
- Initial shared bridge test fixture omitted the desktop-required fix_node row field; corrected to row=1. All 59 initial focused tests then passed.
- Concurrent THREE/print compilation dependencies were reported to their owners and fixed without editing owned paths.
- Infrastructure scope allowlisting uses literal directory prefixes, not glob patterns. Corrected the invocation and achieved overall: pass.
