# Test Review: 3D Bridge Deck Loads

## Scope

Reviewed the approved 3D bridge input, THREE display, solver boundary/audit and PDF integration against `.agents/logs/review-diff-bridge-deck-loads-3d.patch` and the live fixes. Emphasis: case isolation, shell/node namespaces, independent loading meshes, holes, non-XY geometry, physical force direction, and invalid geometry before printing. Coverage: not measured; this is targeted behavior and cross-system review.

## Findings and Resolution

### [High, resolved] Invalid area geometry could be displayed and printed

The renderer clipped strips without requiring the original area to lie on the panel plane or inside its outer boundary. A rectangle at z=1 above a z=0 deck was accepted visually, while the backend rejected its points as off-plane. The sampled line check could also miss a narrow opening or exterior notch.

Added the pure `FrameWebforCS/three/BridgeLoadValidation.cs` helper under exclusive ownership. The lead integrated it into `ThreeBridgeLoadsService` before creating load glyphs and into the existing render-error path used by printing. It checks plane membership, triangular partitions and directed boundaries, declared holes, continuous segment coverage, strip convexity and overlap, and complete area coverage. Area loads may span a declared hole and are clipped there, matching the backend; a line may follow its boundary but cannot cross its interior. Hole polygons are triangulated before checking area coverage. The helper returns the validated strips and computes no equivalent forces.

Added eight focused geometry tests covering off-plane and inclined geometry, required explicit planes, exterior areas, narrow holes, wrong/missing hole declarations, loads entirely inside a hole, concave exterior notches, independent reversed mesh winding, self-crossing paths, nonconvex strips, and a valid 1e-5-size model. Combined helper and THREE tests passed 17/17.

### [High, resolved] Normal arrows followed the independent loading mesh winding

Backend assembly derives a normal load from the structural interpolation cell. An independent loading mesh with opposite winding is valid. The old display used that loading mesh and reversed the arrows relative to the actual solver contribution. A direct Python probe confirmed resultant Z=+50 with reversed independent triangles.

The lead now obtains structural triangles for the direction. `BridgeLoadThreeTests.NormalDirectionUsesStructuralWindingWithReversedLoadingMesh` covers the case; it passed in the combined 17-test run. The original-load PDF uses the same corrected scene.

### [High, resolved] Independent loading holes were checked against structural node IDs

The input builder initially required hole IDs to exist in structural nodes even when the panel had separate loading nodes. Backend holes use the loading-node namespace in this case. Coordinated with the input owner, who fixed the node universe and added `BridgeLoadInputTests.IndependentLoadingMeshHoleIdsRemainInTheirOwnNamespace` for editing, persistence and case projection.

### [High, resolved by input owner after quality review] Root shell references changed meaning on desktop import

Root normalized spatial definitions reference unified backend element IDs, while the new case-local boundary accepts public shell IDs. The input owner added root-only translation and `RootNormalizedShellReferencesMigrateFromUnifiedToPublicIds`. Existing Python regressions explicitly distinguish these namespaces. Case-local imports retain public IDs.

### [Medium, resolved] Absolute tolerance floors rejected small valid geometry

The initial strip correspondence and clipping helpers used a unit-size floor. This could mark endpoints ambiguous or overfill clipped polygons for small models. The lead added an explicit correspondence tolerance and removed the clipping unit floor. The validator supplies the backend dimensional tolerance, `max(absolute, relative * panel_extent)`, multiplied by path extent for endpoint correspondence. The 1e-5-size regression passes. Polygon degeneracy and side predicates use local lengths, and area validity requires positive covered area.

## Cross-System Checks

- Selected-case spatial definitions remain isolated even with ordinary loads and different signed bridge intensities. Bridge-only cases are retained.
- The selected legacy case remaps public shell IDs at the Python boundary. Existing root normalized semantics remain unchanged.
- Explicit 2D requests reject bridge loads. Desktop editing/rendering/printing is restricted to 3D while retained saved data supports returning to 3D.
- Static audit vectors and nodal forces come from the actual solver assembly snapshot; public topology remapping and strict Python/C#/TypeScript readers reject malformed/nonfinite values and unknown node IDs. Nonlinear and modal contracts have no audit.
- Display and PDF audit require the input revision that produced the result. Equivalent forces are never reconstructed by the renderer or fed back into input.
- The PDF owner verified actual audit values and bounded text wrapping; the lead verified native save/reload, solver, GL capture and final PDF generation.

## Test Execution Results

Direct backend-owner checks:

| Gate | Result |
| --- | --- |
| Focused Python IO/spatial/general-plane/result contracts/solver integration | 139 passed |
| Python spatial physical acceptance and result projection | 50 passed |
| Strengthened bridge case regressions, repeated subset | 22 passed |
| C# audit and existing result contracts | 34 passed |
| Geometry helper plus THREE regressions after fixes | 17 passed |
| Actual TypeScript validator through native Node transform | 19 checks passed: 7 valid fixtures and 12 malformed/nonstatic cases |

The distinct focused Python total is 189; the repeated 22 are not added to it. Coverage percentage was not measured. The lead subsequently reported final combined desktop tests 108 passed, native smoke passed, regenerated PDF, and solution build with zero errors. PDF aggregate and final infrastructure gates are recorded by their owners.

## Full-Suite and Environment Limitations

An initial unscoped `.agents/check.ps1` launched the full Python suite. It was stopped at the lead's request after more than 15 minutes and approximately 92% progress by terminating only the verified child Python process. It did not complete and is not claimed as passing. Log: `.agents/logs/check-20261006T152149520Z-45708.log`. The checker continued and its solution build passed; the initial scope check and Angular gates failed.

Before interruption pytest printed four errors and two failures. Mapping progress against the 3,354 collected test IDs identified:

1. `tests/validation/test_mesh_convergence.py::test_closed_form_references_and_expected_asymptotic_rates`
2. `tests/validation/test_mesh_convergence.py::test_distortion_and_aspect_sweeps_remain_within_measured_scope`
3. `tests/validation/test_mesh_convergence.py::test_mitc4_thickness_sweep_has_no_shear_locking_in_strip_problem`
4. `tests/validation/test_mesh_convergence.py::test_hexa8_nearly_incompressible_stress_and_energy_are_an_explicit_limit`
5. `tests/validation/test_performance.py::test_smoke_profile_covers_phases_and_workload_dimensions`
6. `tests/validation/test_performance.py::test_regression_gate_rejects_environment_workload_and_limit_changes`

Narrow reproductions established the cause without restarting the broad suite: `beam_benchmark(2)` fails at `tools/validation/mesh_convergence.py:67` with `KeyError: 'node_displacements'`; the performance smoke test fails in 0.44 seconds at `tools/validation/performance.py:386` with `KeyError: 'metadata'`. These benchmark files and `fem/model.py` have no task diff, and HEAD already returns the public `AnalysisResultSet` from `FemModel.run`. These are existing benchmark consumers of the old top-level result format. They remain outside this bridge-load change and were reported to the lead.

Angular's canonical build/test gates could not run because `ng`/node_modules and `FrameWebforJS/package-lock.json` are absent. No dependency changes or lockfile creation were made. Native execution of the actual edited validator passed, but does not replace an Angular compile.

No unresolved feature-specific high finding remains in this reviewed scope. The incomplete broad run, existing benchmark migration failures, and unavailable Angular gates remain explicit validation limitations.
