# Quality Review: 3D Bridge Deck Loads

## Scope

Independent review of `.agents/logs/review-diff-bridge-deck-loads-3d.patch` against HEAD, followed by the live fixes. Emphasis: input persistence and case projection, THREE geometry and selection, backend shell namespaces and actual solver audit integration. Reviewed the PDF work only for integration and the separately assigned print-budget fix.

Reviewed files include InputBridgeLoadService, InputBridgeLoadComponent, InputDataService, InputLoadService, CalculationRequest, ThreeService, ThreeBridgeLoadsService, BridgeLoadGeometry, BridgeLoadValidation, backend spatial serialization/validation/assembly, result projection/contracts, and focused bridge tests. No nested consultation or concurrent .NET command was used.

## Findings

### [High, fixed] Root normalized shell references changed namespace on import

`InputBridgeLoadService.MigrateSolverBridge` previously copied root `spatial_loads.panels[].elements` unchanged into the desktop model, which later emits case-local spatial loads. Root elements use unified backend IDs, while case-local elements use public shell IDs. With members 1–4 and shell 1, a valid root reference 5 became a request for public shell 5, causing a missing-shell failure or targeting the wrong shell.

Evidence: `FrameWeb/src/fem/spatial_loads/serialization.py:107` versus `:111`; the existing backend test `FrameWeb/tests/io/test_bridge_load_cases.py:57` explicitly verifies the namespace distinction. Coordinated with the input owner. Root-only import now translates via `RootShellIds` / `TranslateRootShellReferences` in `FrameWebforCS/components/input/InputBridgeLoadService.cs:341`. The case-local path remains unchanged. Regression: `BridgeLoadInputTests.RootNormalizedShellReferencesMigrateFromUnifiedToPublicIds`.

### [High, fixed] Independent loading mesh holes were checked against structural nodes

The original editor snapshot builder required every hole ID to belong to the structural panel nodes. Backend holes belong to loading nodes when an independent loading mesh exists (`FrameWeb/src/fem/spatial_loads/definitions.py:184`). Thus a valid panel with structural nodes 1–4 and hole nodes 105–108 could not be imported or edited.

The input owner changed the hole universe to loading IDs when present (`FrameWebforCS/components/input/InputBridgeLoadService.cs:436`), with a save/reload/projection regression `IndependentLoadingMeshHoleIdsRemainInTheirOwnNamespace`.

### [High, fixed] Normal load arrows used loading-mesh winding

THREE originally derived a normal load direction from the first loading triangle. Backend uses the structural interpolation cell orientation (`FrameWeb/src/fem/spatial_loads/assembler.py:159`). A reversed independent loading mesh is valid, but originally reversed the arrows and therefore the original-load PDF relative to the solver force.

The lead now selects structural triangles for load normals in `FrameWebforCS/three/ThreeBridgeLoadsService.cs`, with regression `BridgeLoadThreeTests.NormalDirectionUsesStructuralWindingWithReversedLoadingMesh`.

### [High, fixed] Display clipping could conceal invalid geometry

The original renderer clipped area strips into triangles without rejecting strips outside the external boundary or off the panel plane. It sampled line coverage at only 17 positions, which could miss a narrow hole. Such an input could appear valid and print before solving, although the backend rejects it (`FrameWeb/src/fem/spatial_loads/validation.py:142`).

The backend owner added the pure `FrameWebforCS/three/BridgeLoadValidation.cs` helper. THREE invokes it before creating load glyphs, checks continuous segment coverage, planarity, winding/partition and hole boundaries, and area coverage, and rolls back partial load graphics on failure. The lead also passes panel tolerance into endpoint correspondence. This validates display geometry only; equivalent forces still come exclusively from solver audit output. The owner reported the combined geometry/THREE regression gate passed 17/17 after this change.

## Other Integration Checks

- Bridge-only cases are retained by the shared case registry and emitted even without node/member load rows. Referenced definitions are isolated per selected case.
- 2D hides bridge commands and rendering, rejects calculation/bridge print requests, and retains saved bridge input for returning to 3D.
- Incomplete input rows remain in drafts; calculation and bridge printing reject drafts explicitly.
- Actual solver audit is optional and static-only. Python projection maps node IDs through canonical topology; strict Python/C#/TypeScript readers validate it. C# input projection never converts the audit back into input forces.
- Audit display and audit printing require the input revision that produced the current result. Original-load printing remains available before calculation.
- Print capture uses the bridge screenshot mode, separate input-case filtering, plan/iso choices, and camera/selection/visibility restoration.

## PDF Visual Verification

Rendered `.tmp/bridge-deck-smoke/bridge-load-complete.pdf` using PDFium because Poppler is unavailable in this Windows environment. Inspected all six pages: Japanese definition/coordinate/intensity tables, actual force totals -40 and -30, actual nodal forces, and four plan/iso screenshots. Page headers and numbering are intact; text extraction has no legacy `Error:` marker. PNGs are `.tmp/bridge-deck-smoke/bridge-load-complete-1.png` through `-6.png`.

The result contract intentionally reports unknown units as `unspecified`; the new audit report now displays this as `単位未指定` in Japanese and collapses unknown composite units. It does not guess physical units or change values.

The security review separately requested bounded PDF text wrapping. The PDF owner added limits of 4,096 characters/cell, 256/header, two million total characters, and 200,000 total wrapped lines, plus cancellation checks inside the measuring loop. A 10 MB single-cell regression verifies rejection before wrapping. The post-review BridgeLoadPrintTests gate passed 11/11, including the budget rejection and Japanese unknown-unit labels (`.tmp/bridge-deck-smoke/print-review-fixes.log`).

Final verification: the lead regenerated the native six-page PDF after all review fixes. This reviewer rerendered it and visually checked page 2 audit labels and pages 3–6 diagrams. Japanese `単位未指定` labels are readable; no `unspecified` text or `Error:` marker remains. Plan/iso captions, image bounds, load labels, and page numbering are intact. The lead reported desktop regressions 108/108 passed and solution build completed with zero errors.

## Limitations

No unresolved high-priority finding remains in this reviewed scope. This is a targeted correctness review, not a proof of complete geometric equivalence. Full application and Python suites were not independently rerun; the lead holds the canonical gate evidence.
