# 3D Bridge Deck Loads

## Approval and Scope

The owner approved the preceding investigation and interface proposal on 2026-10-07, restricted to 3D. Implement fixed-position line and area loads end to end in the current FrameWebforCS application. Existing load cases are shared. Transfer panels add no structural stiffness. Vehicle motion and worst-position search are excluded.

## Implementation

1. Backend: accept normalized spatial definitions in the selected legacy case, remap shell references at that boundary, reject ambiguous dual representations and 2D bridge requests. Preserve existing normalized single-model and legacy input behavior. Expose optional validated static-result load audits with public node IDs for actual equivalent-load inspection.
2. Input: introduce a typed bridge surface/path/case-load service and three-tab FarPoint editor. Retain input through atomic import, save/reload, calculation snapshots, revisions, effective-case detection and combination selection. Use 3D coordinates and explicit planes/directions. Hide UI in 2D, reject analysis with incompatible retained data, and prevent silent data loss on dimension changes.
3. THREE: display transfer topology, ordered lines, line/area distributions and actual equivalent nodal loads; link grid and picking, apply case filtering and 3D gating, dispose resources and restore print state.
4. PDF: add definitions, intensities, case-selected original-load diagrams and optional solver audit tables; use the existing in-process generator and same viewport capture. Input-only printing works before analysis. Hide bridge options for 2D.
5. Validate focused Python, desktop and printing contracts, build affected projects, perform integration and independent reviews, and record evidence.

## Data Contract

Desktop persistence uses bridge_loads with shared panels and paths and case-indexed loads. The calculation projection emits each applicable load case's spatial_loads containing only its referenced normalized panels, paths and loads. Per-case panel elements identify legacy shell IDs and are remapped by Python. Input names remain desktop metadata. Stable integer load IDs identify selection and diagnostics; line intensity is force/length and area intensity is force/length squared. Default signed intensities follow global +Z.

## Acceptance

- No bridge commands or glyphs in 2D; incompatible requests are explicit errors and data is never silently deleted.
- A bridge-only case and mixed ordinary/bridge cases survive save and reload and reach the solver once each.
- Different case loads remain isolated, including inclined bridge surfaces and shell-ID namespace collisions.
- Triangles/holes, path order, signed intensities and units agree between input, rendering and PDF.
- Audit output originates from the solver assembly, is validated, and cannot be submitted as duplicate input loads.
- Case filtering and print capture preserve camera, visibility and selection.
- Focused regressions pass; existing unrelated failures are reported separately.

## Implementation Evidence

- Desktop input, geometry, contract and viewport regressions: 108 passed after review fixes.
- Print projection and bridge report regressions: 32 passed; PDF library 55 and print UI 8 passed.
- Python spatial loads, contracts and case regressions: 189 passed; lead independently reran the 22 bridge-case tests.
- Native smoke at `FrameWebforCS/tests/BridgeLoadSmoke` verifies save/reload, in-process Python calculation (case reactions 40/30/7), actual load audits, OpenGL plan/isometric capture, restored print state, PDF generation and 2D guards.
- `dotnet build FrameWeb.sln --no-restore`: successful with existing warnings. Six-page PDF and captured input/3D images are in `.tmp/bridge-deck-smoke/`.
- Security, quality and test reviews found and resolved independent-mesh hole IDs, root unified shell-ID migration, structural normal orientation, invalid area/line display clipping, and unbounded PDF cell wrapping.
- The Angular application gate cannot run because dependencies and its lockfile are absent; the actual TypeScript contract validator passed 19 direct runtime checks. The broad Python run was interrupted; six existing benchmark tests still expect pre-contract result keys, independently reproduced as `metadata` / `node_displacements` KeyErrors. No full-suite pass is claimed.
