# AnalysisResultSet-Only Refactor Plan

## Purpose

Make `AnalysisResultSet` the sole public calculation response for every analysis. Remove representation negotiation, display-specific backend contracts, nested nonlinear result duplication, and all pre-release compatibility code so that one schema serves static, nonlinear, modal, single-case, and multi-case workflows.

## Scope

- Include: one result root schema, one HTTP success path, ordered case/state snapshots, canonical topology and result fields, backend domain postprocessing, frontend direct consumption, worker simplification, tests, docs, and deletion of legacy/display result contracts.
- Exclude: calculation-input redesign, backward compatibility with flat `AnalysisResult`, `FrameResultSet`, `legacy-cases-v1`, or saved result files; linear combination of nonlinear/modal snapshots; changes to FEM equations, eigensolver algorithms, or nonlinear convergence algorithms. Deterministic canonicalization of modal output is in scope.

The normative field-level output contract is [analysis-result-set-v1-contract.md](../research/analysis-result-set-v1-contract.md). Existing validated input schemas remain outside this refactor.

## Target Contract

```json
{
  "kind": "analysis_result_set",
  "schema_version": "1.0",
  "units": {
    "system": "consistent_user_defined",
    "length": "unspecified",
    "force": "unspecified",
    "mass": "unspecified",
    "time": "unspecified"
  },
  "coordinate_system": {
    "name": "global_cartesian",
    "handedness": "right",
    "axes": ["x", "y", "z"]
  },
  "cases": [
    {
      "case_id": "1",
      "name": "固定死荷重",
      "symbol": "D1",
      "analysis_type": "static",
      "support_node_ids": []
    }
  ],
  "topology": {
    "nodes": [],
    "members": [],
    "shell_elements": [],
    "solid_elements": []
  },
  "results": [
    {
      "case_id": "1",
      "state": {
        "kind": "static",
        "index": 0
      },
      "node_displacements": [],
      "support_reactions": [],
      "member_section_forces": [],
      "shell_results": [],
      "solid_results": [],
      "diagnostics": {}
    }
  ]
}
```

Contract rules:

- `POST /` always returns this root shape with `application/json`.
- `cases` and `results` are arrays. Object-key enumeration order is never part of the contract.
- `results` order is case-major, then state-index order.
- A result is uniquely addressed by `(case_id, state.kind, state.index)`; no redundant `result_id` or `sequence` field exists.
- `topology` is emitted once. Every isolated case solve must produce the same public topology; mismatch is a server error rather than silently changing entity identity by case.
- Supports are not shared topology: every case declares its ordered `support_node_ids`, so cases may select different existing support definitions while retaining one geometric topology.
- JSON contains finite numbers only and responses are atomic.

State is a discriminated union:

```text
static:
  {kind: "static", index: 0}

nonlinear accepted step:
  {kind: "load_step", index: 0..n, load_factor, is_final}

modal mode:
  {kind: "mode", index: 0..n, eigenvalue, frequency}
```

For nonlinear analysis there is no duplicate top-level final result and no nested `step_results`. Each accepted step is one complete `AnalysisResult`; only the last step for that case has `is_final: true`. Iteration/convergence records for a step live in that result's `diagnostics`.

The complete result union is frozen as follows:

- `StaticAnalysisResult`: `state.kind === "static"`; requires `node_displacements`, `support_reactions`, `member_section_forces`, `shell_results`, `solid_results`, and `diagnostics`; prohibits `node_mode_shapes`.
- `NonlinearStepAnalysisResult`: `state.kind === "load_step"`; requires the same physical result fields plus `load_factor`, `is_final`, and step-owned convergence diagnostics; prohibits `node_mode_shapes`.
- `ModalAnalysisResult`: `state.kind === "mode"`; requires `node_mode_shapes`, `eigenvalue`, unit-relative `frequency`, and `diagnostics`; prohibits support reactions, member section forces, shell/solid force/stress results, and nonlinear fields.
- Physical result arrays are present even when empty because the topology contains no entity of that kind; a missing required field is invalid.
- Field presence and collection cardinality follow the normative contract: an empty physical result array is valid only when its corresponding topology entity array is empty.

## Canonical Result Model

- `node_displacements`: public node IDs plus named displacement/rotation components.
- `support_reactions`: public support node IDs plus named force/moment components.
- `member_section_forces`: original public member IDs with ordered stations/segments, physical positions, and consistently named local force/moment components.
- `shell_results` and `solid_results`: typed element results keyed by public element ID and exact topology-declared sampling locations.
- Member and shell local frames are serialized as right-handed origin/basis vectors. Shell results use one integration-weighted `element_average`; solid `GP0..GPn` locations preserve formulation integration-point order and natural coordinates.
- `diagnostics`: solver metadata relevant to only this snapshot, including nonlinear iteration history.
- Raw solver vectors and UI abbreviations (`disg`, `reac`, `fsec`, `shell_fsec`, `size`) are not public contract fields.

The useful code currently in `legacy_results.py` is renamed and promoted to domain postprocessing:

- generated-node/public-node identity becomes topology construction;
- support filtering becomes canonical reaction extraction;
- split internal elements are aggregated into `member_section_forces` by original member/station;
- end-force sign conventions are defined once as the canonical local member convention;
- `size` is derived from `topology.nodes.length` and is not serialized.

## Rate Removal Decision

- The current `rate` property is a legacy display/post-solve multiplier, not part of the physical analysis definition.
- Delete `rate` from active Angular models, editors, serializers, built-in presets, result workers, and tests.
- Do not replace it with `load_scale` in this result-contract refactor.
- Applied load values remain explicit in the existing input schemas. DEFINE/COMBINE/PICKUP coefficients remain separate derived-result concepts and never mutate base `AnalysisResult` values.
- No compatibility reader or migration path is retained because the application is unreleased.

## Implementation Steps

Execute the following steps in dependency order; parallel work begins only after Step 0 freezes the shared contract.

### 0. Freeze the single-contract design

- Confirm `DESIGN.md` contains only the sole-root requirement/decision and no active compatibility, display-result, default-flat-response, or rate-after-solve rule.
- Freeze `AnalysisResultSet v1` from the normative contract document, including IDs/references, topology items, components/units/coordinate frames, diagnostics, extra-property prohibition, empty/count rules, and the three complete discriminated result variants.
- Freeze the schema above, state union, case-major order, topology identity invariant, and canonical force signs.
- Create `analysis-result-set-v1.schema.json` in the shared contract fixture directory before backend/frontend work starts.
- Generate shared positive and negative fixtures listed in the normative contract, including legitimate topology-driven empty arrays.
- Freeze modal ordering, positive-eigenvalue policy, degeneracy grouping, mass normalization, deterministic degenerate-subspace basis, and sign rule.

Completion evidence: design validation passes and backend/frontend owners use the same checked-in fixtures.

### 1. Characterize domain results, not old wire formats

- Add tests around current solver snapshots, member splitting, generated nodes, support reactions, recovered member forces, shell/solid outputs, and modal modes.
- Establish numerical oracles before restructuring serialization.
- Do not add golden tests for `FrameResultSet` or `legacy-cases-v1`; those contracts are intentionally deleted.

Completion evidence: focused solver/domain tests prove the numerical values to preserve.

### 2. Introduce canonical contract and topology modules

- Add `FrameWeb/src/fem/result_contracts.py` for TypedDict/dataclass definitions and strict finite-value validation.
- Add `FrameWeb/src/fem/result_topology.py` for public nodes, generated-node metadata, member segments/stations, and shell/solid entity identity.
- Add `FrameWeb/src/fem/result_projection.py` for canonical support reactions, member section forces, shell results, and solid results.
- Keep solver-native arrays internal.

Completion evidence: unit tests build and validate canonical topology/results without HTTP.

### 3. Normalize solver output into snapshots

- Refactor `solver_results.py` so static, accepted nonlinear steps, and modal modes can each become one snapshot.
- Remove nonlinear final-state duplication and nested `step_results` from the public path.
- Partition cumulative `convergence_history` into the owning step's `diagnostics`.
- Refactor `model.py` postprocessing so every nonlinear snapshot receives the same canonical element/shell recovery as its final step.

Completion evidence: no public snapshot contains another result; final nonlinear state equals the last accepted snapshot numerically.

### 4. Build ordered multi-case execution

- Add `FrameWeb/src/fem/analysis_result_sets.py` as the application service.
- Use the existing validated calculation inputs and enumerate every requested case; do not introduce a second input contract or an input compatibility adapter in this refactor.
- For legacy `node` input, enumerate every `load` map entry in insertion order; stringify its key as `case_id`; take non-empty `name`/`symbol` from the case and otherwise use `case_id`.
- Preserve existing legacy analysis precedence per case: non-null top-level `analysis_type`, then case `analysis_type`, then model inference; parameter defaults are replaced first by case values and then by present top-level `analysis_params` keys. Add mixed static/nonlinear/modal fixtures with and without top-level overrides.
- For modern `nodes` input, preserve its current single-analysis meaning, top-level analysis settings, and inference behavior; emit exactly one case whose `case_id`, `name`, and `symbol` are `"1"`; modern multi-case input is outside scope.
- Derive each case's `support_node_ids` from its selected, solved support definition, excluding automatic auxiliary 2D constraints. Different case support sets are valid and do not cause a topology mismatch.
- Build request-wide canonical topology before solving: union member ends, notice points, rigid boundaries, and every case's member-action boundaries; coalesce once; assign deterministic station/generated-node IDs; and map every isolated case mesh to it.
- Do not hide stations introduced by another case. A post-normalization topology mismatch is an internal error, not a normal difference between cases.
- Solve each requested case with isolated mutable solver/model state against that frozen public topology.
- Append only JSON-ready snapshots; release each solved model before the next case.
- Enforce the case count limit and topology identity; any case failure discards the entire response.

Completion evidence: single/multiple static and single/multiple nonlinear cases preserve requested case order and state order without retaining model collections.

### 5. Prepare the sole HTTP response without cutting over

- Add the pure `AnalysisResultSet` HTTP serialization/error path and test it directly while the currently connected backend/frontend pair remains usable.
- Keep compression only as transport encoding; compressed and plain canonical fixtures decode to the same schema.
- Preserve the existing diagnostic error boundary and prove every prospective success body validates as `AnalysisResultSet`.
- Do not switch `main.py`'s public success path or delete the old frontend/backend paths yet.

Completion evidence: backend contract/API tests pass against the new response builder, while no intermediate repository state requires the old frontend to consume the new response.

### 6. Make obsolete backend code deletion-ready

- Move all useful topology, support, member-force, shell, and solid postprocessing out of `legacy_results.py` into canonical domain modules.
- Replace legacy/display golden tests with canonical numerical oracles, but retain the connected old entry point until the atomic cutover.
- Update direct `FemModel.run()` API-facing tests to unwrap the set or call an explicitly internal snapshot API as appropriate; do not create a second public result contract.
- Produce a checked deletion inventory for representation negotiation, vendor media types, legacy projectors, and obsolete docs.

Completion evidence: the new backend path has no dependency on display fields, and the remaining old path is isolated and removable in one cutover change.

### 7. Add one strict frontend contract boundary

- Add `FrameWebforJS/src/app/providers/analysis-result-set.ts` with TypeScript discriminated unions and one strict validator/indexer.
- Build indexes keyed by case and state only after validation; preserve array order as the authority.
- Store immutable base results. Any combination/pickup calculation receives derived copies or immutable reads.
- Reject duplicate coordinates, missing final nonlinear state, topology/result ID mismatches, non-finite values, and unsupported state/analysis combinations.
- Require every physical field. Allow node/member/shell/solid fields to be empty exactly when their corresponding topology array is empty; allow reactions to be empty exactly when the owning case's `support_node_ids` is empty; otherwise enforce exact ID coverage.

Completion evidence: malformed success payloads never start result workers.

### 8. Refactor result processing around canonical fields

- Replace worker inputs based on `{caseId: {disg, reac, fsec}}` with canonical snapshots.
- Prefer small pure selectors over one adapter that recreates the deleted legacy shape.
- Make case/state selection explicit in displacement, reaction, member-force, table, and 3D display services.
- DEFINE/COMBINE/PICKUP accepts only `state.kind === "static"`; attempts to linearly combine nonlinear steps or modal modes fail visibly.
- Remove `Object.keys()` as a case/state ordering mechanism.

Completion evidence: UI displays first/last static cases and every nonlinear step directly from canonical results; integer-like case IDs retain input order.

### 9. Make the frontend cutover-ready and remove rate

- Prove all live views/workers can start from canonical fixtures, then prepare the obsolete `LegacyCasesResult`, legacy header, and raw result-map paths for deletion in Step 10.
- Delete `rate` from Angular input models, editors, serializers, built-in presets, workers, and tests; do not add a replacement property.
- Leave the existing calculation input schemas otherwise unchanged.
- Because the app is unreleased, do not add compatibility normalizers or saved-result migrations.

Completion evidence: canonical frontend fixture tests pass; no active calculation or derived-result code depends on `rate`; the old transport path remains connected only until Step 10.

### 10. Perform one atomic cutover, delete obsolete paths, document, and verify

- In one integration change, switch `FrameWeb/main.py` to the unconditional `AnalysisResultSet` response and switch FrameWebforJS to its strict canonical validator/consumers.
- In that same change, delete result `Accept` negotiation, vendor media-type constants, `FrameWeb/src/fem/legacy_results.py`, legacy frontend headers/types/validators/raw-map handling, display-specific tests, and dead compatibility documentation.
- Do not merge or release an intermediate state in which only one side has switched contracts.
- Rewrite endpoint/result documentation around the sole schema.
- Verify the Step 0 JSON Schemas and shared positive/negative fixtures through both Python and TypeScript tests.
- Run backend tests, Angular tests with a nonzero spec count, production build, relevant .NET build, and browser E2E.
- Verify Ct first/last cases, nonlinear step navigation, member-force diagrams across split members, reaction display, DEFINE/COMBINE/PICKUP static behavior, and visible rejection of invalid combinations.

Completion evidence: all gates pass and no compatibility code remains.

## Parallel Work Packages

After Step 0 is complete:

- Backend result owner: `FrameWeb/src/fem/solver_results.py`, `FrameWeb/src/fem/model.py`, new `result_topology.py`, new `result_projection.py`, deletion of `legacy_results.py`, and their focused domain tests.
- Backend HTTP/orchestration owner: `FrameWeb/main.py`, `FrameWeb/src/fem/file_io.py`, `FrameWeb/src/fem/legacy_beam.py` removal/refactor as needed, new `result_contracts.py`, new `analysis_result_sets.py`, API tests, and canonical contract fixtures under `FrameWeb/tests/data/contracts/`.
- Frontend owner: all `FrameWebforJS/` product code, presets, and Angular tests; reads but does not edit the canonical fixtures owned by the backend HTTP/orchestration owner.
- Integration lead: `.agents/docs/DESIGN.md`, feature/plan docs, endpoint/result docs, cross-layer review, full gates, and browser E2E; does not edit product files owned above.

Ownership is file-exclusive. `solver_results.py` and `model.py` stay with one backend owner because nonlinear snapshot generation and postprocessing are tightly coupled. Contract fixtures are frozen in Step 0 and have one writer.

## Verification

Primary gates:

```powershell
uv --directory FrameWeb run --locked --extra dev python -m pytest tests -q
npm --prefix FrameWebforJS run test -- --watch=false --browsers=ChromeHeadless
npm --prefix FrameWebforJS run build
dotnet build FrameWeb.sln
& .agents/check.ps1
```

Additional contract checks:

- JSON schema validation for every HTTP success fixture.
- No duplicate `(case_id, state.kind, state.index)` coordinates.
- Exactly one `is_final: true` per nonlinear case and none on non-step states.
- Case-major/state-major order is identical in backend fixtures and frontend indexes.
- Canonical member-force values match pre-refactor numerical oracles.
- Full-text search confirms removed contracts and abbreviations are absent from active product code.

## Risks & Considerations

- This is intentionally a breaking internal/API change and must land backend and frontend together.
- Promoting member aggregation to canonical output changes its status from compatibility code to supported domain behavior; force signs and station definitions require explicit oracle tests.
- Shared topology requires deterministic preprocessing across all cases. If a case would create different public entities, fail rather than emit ambiguous IDs.
- Nonlinear response size grows with accepted steps; the response necessarily retains JSON-ready snapshots, but never solved models. Compression remains available.
- Modal results need a distinct state/result variant and must not be forced into static force fields.
- Existing calculation inputs stay in place, avoiding an unrelated request-schema migration in the same change.
- Existing saved results and external result consumers are deliberately unsupported before release.

## Open Questions

None block implementation. Streaming very large histories, nonlinear combination/envelope rules, and a future persisted-result file format are separate features.
