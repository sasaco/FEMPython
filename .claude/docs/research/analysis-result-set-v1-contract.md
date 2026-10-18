# AnalysisResultSet v1 Contract

## Boundary

- This contract defines calculation success output only. Existing calculation input schemas remain outside this refactor.
- Root: `AnalysisResultSet`
- HTTP content type: `application/json; charset=utf-8`
- Version: string literal `"1.0"`
- `additionalProperties: false` at every contract-owned object boundary.
- Every number is finite; `NaN` and infinities are invalid.
- Every public ID is a non-empty, case-sensitive string, unique in its declaring array, and referenced by exact equality.
- Array order is normative; object property order is never meaningful.

## Root

```text
AnalysisResultSet
  kind: "analysis_result_set"
  schema_version: "1.0"
  units: UnitSystem
  coordinate_system: CoordinateSystem
  cases: ResultCase[1..256]
  topology: ResultTopology
  results: AnalysisResult[1..]
```

`UnitSystem` contains `system`, `length`, `force`, `mass`, and `time`. It is copied from the existing normalized `model_metadata.units`; omitted input declarations therefore become `system: "consistent_user_defined"` with `"unspecified"` base-unit values. Rotation is always in radians, stress is expressed in `force/length^2`, and frequency is expressed in `1/time`. The solver does not infer or convert units.

`CoordinateSystem` is exactly `{name: "global_cartesian", handedness: "right", axes: ["x", "y", "z"]}`. Node displacements and support reactions use global axes. Member section forces use the member-local right-handed frame defined by `node_i -> node_j` and the model orientation rule. Shell/solid items state their element frame.

`ResultCase` contains:

```text
case_id: string
name: string
symbol: string
analysis_type: "static" | "material_nonlinear" | "modal"
support_node_ids: string[]
```

`support_node_ids` is case-specific, contains only public nodes with user-defined restraints/supports, and excludes automatic auxiliary 2D constraints. It is ordered by public topology node order.

`cases` preserves the application's validated case order. Existing inputs are enumerated without introducing a second input schema:

- Legacy `node` input: every entry of the non-empty `load` map becomes one case in insertion order. `case_id` is the string form of its map key. `name` and `symbol` use the case's non-empty values, falling back independently to `case_id`.
- For each legacy case, effective `analysis_type` uses the existing precedence: non-null top-level `analysis_type`, then that case's non-null `analysis_type`, then model inference (`material_nonlinear` when nonlinear bars or nonlinear support springs exist, otherwise `static`). A top-level value therefore forces all cases to the same type; without it, mixed case types are legal.
- For each legacy case, effective analysis parameters begin with `n_load_steps: 10`, `max_iterations: 50`, `tolerance: 1e-6`, `n_modes: 10`, `load_factors: null`, and `displacement_control: null`; values present on that case replace those defaults, then present keys in top-level `analysis_params` replace the corresponding case values. Existing validation and analysis-specific parameter use remain unchanged.
- Modern `nodes` input: the current schema represents one analysis and therefore produces exactly one case with `case_id`, `name`, and `symbol` equal to `"1"`. It keeps the existing top-level `analysis_type`, `analysis_params`, and inference behavior. Modern multi-case input is not added by this refactor.
- `ResultCase.analysis_type` records the validated effective type actually passed to the solver.

Duplicate normalized case IDs, more than 256 legacy cases, or a missing/empty legacy `load` map are input errors. The result-contract refactor does not introduce an input compatibility adapter.

## Request-Wide Canonical Topology

Public topology is constructed before the first case solve.

1. Collect member station candidates from member ends, notice points, rigid-zone boundaries, and every case's member load/action boundaries.
2. Sort and coalesce candidates using one documented length-relative tolerance.
3. Assign deterministic member-local station IDs `S0..Sn` in ascending distance from `node_i`.
4. Build the union segmentation for every case. A station introduced by another case remains present; it is not hidden.
5. Assign deterministic public generated-node IDs derived from `(member_id, station_id)`; internal numeric solver IDs never cross the contract.
6. Map every isolated case mesh to this topology. Missing or extra public entities abort the complete response as an internal contract failure.

`ResultTopology` contains:

```text
nodes:
  {node_id, coordinates: {x, y, z}, source_node_id: string|null, generated: boolean}[]
members:
  {member_id, node_i, node_j, local_frame: CoordinateFrame,
   stations: {station_id, position}[]}[]
shell_elements:
  {element_id, element_type: "triangle3" | "quadrilateral4", node_ids,
   local_frame: CoordinateFrame,
   result_locations: [{location_id: "element_average", kind: "element_average"}]}[]
solid_elements:
  {element_id,
   element_type: "tetra4" | "wedge6" | "hexa8" | "tetra10" | "wedge15" | "hexa20",
   node_ids, coordinate_frame: "global",
   result_locations: {location_id, natural_coordinates: {xi, eta, zeta}}[]}[]
```

`position` is non-negative distance from member `node_i` in the declared length unit.

`CoordinateFrame` is `{origin: {x,y,z}, x_axis: {x,y,z}, y_axis: {x,y,z}, z_axis: {x,y,z}}`. Axes are finite, unit length, mutually orthogonal, and right-handed. A member's origin is `node_i`; `x_axis` points from `node_i` to `node_j`; the existing member orientation/`cg` rule deterministically fixes `y_axis` and `z_axis`. A shell's origin is its first node and its axes are the exact basis used by shell postprocessing.

Shell topology has exactly one canonical sampling location, `element_average`; canonical resultants and top/bottom stresses are integration-measure-weighted element averages expressed in the element's serialized `local_frame`. `triangle3` has exactly 3 node IDs and `quadrilateral4` exactly 4.

Solid `result_locations` reproduce the element formulation's integration-point order. IDs are `GP0..GPn` in that order. Node cardinality is encoded by the element-type suffix. Stress/strain components use the global coordinate system. Every canonical row maps one-to-one to these locations; a formulation/result row-count mismatch aborts the response.

## AnalysisResult Union

Results are case-major and then ascending state index. The unique coordinate is `(case_id, state.kind, state.index)`.

### StaticAnalysisResult

```text
case_id: string
state: {kind: "static", index: 0}
node_displacements: NodeDisplacement[]
support_reactions: SupportReaction[]
member_section_forces: MemberSectionForces[]
shell_results: ShellResult[]
solid_results: SolidResult[]
diagnostics: {warnings: string[]}
```

### NonlinearStepAnalysisResult

```text
case_id: string
state: {
  kind: "load_step"
  index: zero-based integer
  load_factor: finite number
  is_final: boolean
}
node_displacements: NodeDisplacement[]
support_reactions: SupportReaction[]
member_section_forces: MemberSectionForces[]
shell_results: ShellResult[]
solid_results: SolidResult[]
diagnostics: {
  warnings: string[]
  iterations: {index, residual_norm, correction_norm, converged}[]
}
```

Each nonlinear case has consecutive indices starting at 0 and exactly one `is_final: true` on its last accepted step. There is no enclosing final result and no nested `step_results`. Cumulative convergence records are partitioned by step and appear exactly once.

### ModalAnalysisResult

```text
case_id: string
state: {
  kind: "mode"
  index: zero-based integer
  eigenvalue: positive finite number
  frequency: positive finite number, in inverse declared time units
  degeneracy_group: zero-based integer
}
node_mode_shapes: NodeModeShape[]
diagnostics: {
  warnings: string[]
  normalization: "mass"
  eigenvalue_tolerance: non-negative finite number
  degeneracy_relative_tolerance: 1e-8
}
```

Modal results prohibit displacement, reaction, member/shell/solid force fields, `load_factor`, and `is_final`.

## Physical Items

All six-component values use names rather than positional arrays.

```text
NodeDisplacement
  node_id
  components: {dx, dy, dz, rx, ry, rz}

NodeModeShape
  node_id
  components: {dx, dy, dz, rx, ry, rz}

SupportReaction
  node_id
  components: {fx, fy, fz, mx, my, mz}

MemberSectionForces
  member_id
  segments:
    {segment_id, station_i, station_j, length,
     i_end: {fx, fy, fz, mx, my, mz},
     j_end: {fx, fy, fz, mx, my, mz}}[]
```

Segment endpoints remain separate so point-load discontinuities are representable. For a member with stations `S0..Sn`, results contain exactly `n` segments in order; segment `i` has `segment_id: "S{i}-S{i+1}"`, `station_i: "S{i}"`, `station_j: "S{i+1}"`, and the matching station-distance difference as `length`. Member signs use the serialized member `local_frame` and one canonical end-force convention fixed by a two-node beam oracle.

```text
ShellResult
  element_id
  locations:
    {location_id: "element_average",
     membrane_force: {nx, ny, nxy},
     bending_moment: {mx, my, mxy},
     transverse_shear: {qx, qy},
     top_stress: {sx, sy, txy},
     bottom_stress: {sx, sy, txy}}[]

SolidResult
  element_id
  locations:
    {location_id,
     stress: {sx, sy, sz, txy, tyz, tzx},
     strain: {ex, ey, ez, gxy, gyz, gzx}}[]
```

## Modal Determinism

Canonical modal postprocessing is in scope even though the eigensolver algorithm is unchanged.

1. Compute `eigenvalue_scale = max(abs(diag(K_free))) / max(abs(diag(M_free)))` and `zero_tolerance = max(machine_epsilon * free_dof_count * eigenvalue_scale, 1e-14 * eigenvalue_scale)`, matching the current solver. Accept only finite eigenvalues greater than `zero_tolerance`; negative and rigid-body/near-zero eigenvalues fail v1 modal analysis.
2. Sort by ascending eigenvalue.
3. Set `degeneracy_relative_tolerance = 1e-8`. Consecutive sorted eigenvalues `a,b` share a group when `abs(a-b) <= degeneracy_relative_tolerance * max(abs(a), abs(b), zero_tolerance)`; grouping is transitive over adjacent modes.
4. Mass-normalize to `phi^T M phi = 1`.
5. Canonicalize each degenerate eigenspace by projecting lexicographically ordered global DOF unit vectors into the subspace and orthonormalizing in the mass inner product.
6. Make the largest absolute component positive; break magnitude ties by `(node_id, dof)` lexical order.
7. Compute `frequency = sqrt(eigenvalue) / (2*pi)` in inverse declared time units; do not label it Hz unless the declared time unit is seconds.

## Empty and Coverage Rules

- Every static/load-step physical field is required.
- Node displacement/mode-shape, member, shell, and solid fields may be empty only when their corresponding topology array is empty. `support_reactions` may be empty only when the owning case's `support_node_ids` is empty.
- Node displacement/mode-shape IDs exactly cover `topology.nodes`.
- Reaction IDs exactly cover the owning `ResultCase.support_node_ids`; modal results contain no reactions even though their case still declares its supports.
- Member, shell, and solid result IDs exactly cover their matching topology arrays.
- Each member's segments exactly cover consecutive topology stations. Each shell/solid result's location IDs and order exactly equal its topology `result_locations`.
- Empty `results`, duplicate coordinates, missing case IDs, non-consecutive nonlinear indices, or missing/multiple nonlinear final markers are invalid.

## Step 0 Artifacts

Before product implementation starts, generate:

- `analysis-result-set-v1.schema.json`
- positive fixtures: one static case, multiple static cases, one nonlinear case, multiple nonlinear cases, modal case, and legitimate topology-driven empty arrays
- negative fixtures: duplicate IDs/coordinates, forbidden variant fields, topology/result ID mismatches, non-finite values, invalid final markers, and extra properties

Python and TypeScript tests consume the same schema and fixtures.
