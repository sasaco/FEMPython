# Quality Review: AnalysisResultSet-only Result Contract Refactor

## Verdict

**PASS after follow-up.** The live HTTP and public Python success paths now use
the sole `AnalysisResultSet` root, the moving-load regressions have been
repaired, and no Critical or High finding remains. Remaining Medium and Low
risks are listed separately in the follow-up section.

This review is source- and patch-based. It used the supplied final-gate evidence:
Python 3270 passed, .NET build passed, repository checks passed, and Angular
test/build remained blocked only by the declared pre-existing missing files and
assets. No product code was modified by this review.

## Findings

### High — The public Python calculation API still exposes the legacy result contract

- **Location:** `FrameWeb/main.py:34`, `FrameWeb/src/fem/__init__.py:7`,
  `FrameWeb/src/fem/__init__.py:59`, `FrameWeb/src/fem/model.py:406`,
  `FrameWeb/src/fem/model.py:499`, `FrameWeb/src/fem/model.py:512`,
  `FrameWeb/src/fem/solver_results.py:434`,
  `FrameWeb/src/fem/solver_results.py:445`,
  `FrameWeb/src/fem/solver_results.py:456`.
- **Current behavior/evidence:** `FemModel` remains explicitly exported for
  embedding clients. Its public `run()` returns the solver-native dictionary,
  including nested nonlinear `step_results`, and invokes
  `legacy_nonlinear_result`. This conflicts with the plan's explicit direction
  at `.agents/docs/plans/result-contract-refactor.md:194` not to create a second
  public result contract.
- **Impact:** HTTP callers see only `AnalysisResultSet`, but Python callers still
  consume a flat/nested legacy schema. The repository therefore has two public
  success contracts that can drift independently, and `step_results` remains a
  publicly reachable nested nonlinear representation.
- **Suggested improvement:** make the public calculation API return
  `AnalysisResultSet`. Move the existing single-case solver return value behind
  an explicitly internal snapshot API, migrate direct tests to that API, and
  remove `legacy_nonlinear_result` and public `step_results` exposure.

### High — Moving-load result aggregation, flags, printing, and animation are broken

- **Location:**
  `FrameWebforJS/src/app/components/input/input-load/input-load.service.ts:606`,
  `FrameWebforJS/src/app/components/input/input-load/input-load.service.ts:629`,
  `FrameWebforJS/src/app/components/input/input-load/input-load.service.ts:647`,
  `FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:87`,
  `FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:94`,
  `FrameWebforJS/src/app/components/result/result-reac/result-reac.service.ts:85`,
  `FrameWebforJS/src/app/components/result/result-reac/result-reac.service.ts:92`,
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec.service.ts:83`,
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec.service.ts:91`,
  `FrameWebforJS/src/app/components/three/geometry/three-displacement.service.ts:216`,
  `FrameWebforJS/src/app/components/three/geometry/three-displacement.service.ts:323`,
  and `FrameWebforJS/src/app/components/print/custom/print-custom.service.ts:12`.
- **Current behavior/evidence:** the unchanged input path still expands an `LL`
  case into decimal child case IDs such as `1.1` and `1.2`. The three rewritten
  result services now store snapshots by one-based page number and hard-code
  every `LL_flg` entry to `false`; the former child-case envelope/aggregation
  workers were removed. The still-active displacement animation searches the
  result object for keys prefixed with `id + "."`, but page-number keys no longer
  preserve those case IDs. `PrintCustomService.LL()` also has no remaining
  caller, leaving `LL_exist` unset.
- **Impact:** moving-load tables no longer use their LL presentation/envelope,
  print controls remain disabled, and the 3D displacement animation cannot
  traverse the generated load positions. When an LL case creates extra result
  pages, page indices also stop matching `InputLoadService.getLoadName(index)`,
  so later case symbols can be associated with the wrong snapshot. This is a
  silent structural-result presentation regression.
- **Suggested improvement:** preserve both ordered page identity and canonical
  `case_id`. Rebuild the LL grouping explicitly from `AnalysisResultSet.cases`
  and canonical static snapshots, restore extrema/envelope generation without
  mutating base results, pass case IDs rather than page numbers to animation,
  restore the print flag update, and add an end-to-end fixture with one LL case,
  multiple decimal child cases, and a following ordinary case.

### Medium — Member-force extrema are returned as zeros for every result

- **Location:**
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:19`,
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:20`,
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:45`,
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:57`,
  and
  `FrameWebforJS/src/app/components/three/geometry/three-section-force/three-section-force.service.ts:230`.
- **Current behavior/evidence:** the worker initializes `valueRange` to zero,
  updates only `maxValue`, and returns the untouched zero ranges. The 3D section
  force service consumes `valueRange` for maximum/minimum display.
- **Impact:** force and moment minima, maxima, and corresponding member IDs are
  displayed as zero even when the canonical member forces are nonzero.
- **Suggested improvement:** compute signed extrema for `fx/fy/fz/mx/my/mz`
  over every non-dummy end row, retain the winning member ID, initialize with
  infinities, and normalize only an empty result to zeros. Add worker tests with
  positive and negative values across multiple members.

### Medium — The UI reports calculation success before result workers finish

- **Location:** `FrameWebforJS/src/app/providers/result-data.service.ts:105`,
  `FrameWebforJS/src/app/providers/result-data.service.ts:133`,
  `FrameWebforJS/src/app/app.component.ts:281`,
  `FrameWebforJS/src/app/app.component.ts:282`,
  `FrameWebforJS/src/app/app.component.ts:307`,
  `FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:78`,
  `FrameWebforJS/src/app/components/result/result-reac/result-reac.service.ts:76`,
  and
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec.service.ts:74`.
- **Current behavior/evidence:** `loadResultData()` starts three asynchronous
  worker pipelines and returns `void`; the caller immediately sets aggregate
  `isCalculated = true` and displays the completion alert. Worker failures are
  logged to the console and are not propagated to the caller.
- **Impact:** the result UI and printing can be enabled with incomplete or empty
  derived views, while the user has already been told the calculation finished.
  This recreates the previously identified silent `HTTP 200 / isCalculated`
  failure mode at the worker boundary.
- **Suggested improvement:** return typed promises from the three result
  services, handle `message`, `error`, and `messageerror`, await all three with
  `Promise.all`, and set aggregate success/show the completion alert only after
  all workers succeed.

### Medium — The TypeScript trust-boundary validator is weaker than Python and the schema

- **Location:** `FrameWeb/src/fem/result_contracts.py:284`,
  `FrameWeb/src/fem/result_contracts.py:365`,
  `FrameWeb/src/fem/result_contracts.py:400`,
  `FrameWeb/src/fem/result_contracts.py:603`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:292`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:503`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:694`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:705`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:725`, and
  `FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json:59`.
- **Current behavior/evidence:** TypeScript accepts whitespace-only IDs because
  `stringAt(..., true)` checks only `length === 0`; it accepts empty `name` and
  `symbol`; it does not require nonlinear iteration indices to match array
  positions or norms to be nonnegative; and it does not enforce ascending modal
  eigenvalues or contiguous degeneracy groups. Python rejects each of these.
  TypeScript also permits an empty solid `result_locations` array at
  `analysis-result-set.ts:476`, while Python and the schema require at least one.
- **Impact:** saved or externally supplied payloads that the backend contract
  rejects can cross the frontend's sole trust boundary and reach workers whose
  assumptions are stricter than the validation performed.
- **Suggested improvement:** mirror all Python semantic checks in TypeScript and
  add shared negative fixtures for whitespace/empty labels, empty solid
  locations, invalid iteration diagnostics, descending eigenvalues, and invalid
  degeneracy groups. Prefer a generated/shared rule source to three independent
  handwritten implementations.

### Medium — An invalid response is persisted before it is validated

- **Location:** `FrameWebforJS/src/app/app.component.ts:278`,
  `FrameWebforJS/src/app/app.component.ts:281`,
  `FrameWebforJS/src/app/providers/input-data.service.ts:49`, and
  `FrameWebforJS/src/app/providers/input-data.service.ts:281`.
- **Current behavior/evidence:** the raw JSON response is assigned to
  `InputData.result` before `ResultData.loadResultData()` invokes the strict
  validator. If validation throws, the catch path clears only the calculated
  flag; it does not restore or remove the invalid `InputData.result`. Saved input
  JSON later serializes that stored value.
- **Impact:** a visibly rejected response remains in shared application state and
  can be saved into a project file, while any prior valid stored result is lost.
- **Suggested improvement:** validate first and persist only
  `validatedIndex.value` after success, or make `loadResultData()` return the
  validated frozen root. Add a test proving invalid input leaves the previous or
  null persisted result unchanged.

### Medium — Mixed result sets disable valid static-only derived operations

- **Location:** `FrameWebforJS/src/app/providers/result-data.service.ts:115`,
  `FrameWebforJS/src/app/providers/result-data.service.ts:120`,
  `FrameWebforJS/src/app/providers/result-data.service.ts:123`,
  `FrameWebforJS/src/app/providers/result-data.service.ts:133`, and
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:848`.
- **Current behavior/evidence:** the presence of any nonlinear step or modal mode
  makes `hasNonStaticResult` true for the whole set. If any derived definition
  exists, `requireStaticResults(index)` rejects the entire response even when
  every referenced case is static. Without definitions, the code still clears
  all derived lists and disables derivation for the static siblings.
- **Impact:** a canonical set containing static, nonlinear, and modal cases—the
  target structure described for this refactor—cannot apply
  DEFINE/COMBINE/PICKUP to its static cases. The plan requires operation-level
  rejection of nonlinear/modal operands, not global rejection merely because
  such siblings exist (`result-contract-refactor.md:214`).
- **Suggested improvement:** build a static-result index by case ID; validate
  each DEFINE/COMBINE/PICKUP operand against that index and reject only an
  operation that references a non-static case/state. Continue to show every base
  state independently.

### Medium — The shared JSON Schema is not actually executed by the fixture tests

- **Location:** `FrameWeb/tests/io/test_result_contracts.py:18`,
  `FrameWeb/tests/io/test_result_contracts.py:31`,
  `FrameWebforJS/src/app/providers/analysis-result-set.spec.ts:1`, and
  `FrameWebforJS/src/app/providers/analysis-result-set.spec.ts:34`.
- **Current behavior/evidence:** the Python test checks a handful of schema
  fields and then validates fixtures only with the Python semantic validator.
  The Angular test imports the fixture JSON but neither imports nor compiles the
  schema. The plan requires fixtures to be validated against the shared schema
  in both runtimes (`result-contract-refactor.md:234`).
- **Impact:** the schema can become invalid or drift from both handwritten
  validators while every fixture test remains green; it is not functioning as a
  checked source of truth.
- **Suggested improvement:** validate the schema itself as Draft 2020-12 and run
  all positive/negative fixtures through it in Python. Compile the same schema
  with an Angular-compatible validator or generate the TypeScript validator from
  it, and keep the semantic-only checks explicit.

### Medium — A legacy all-zero support row becomes a result-processing failure

- **Location:** `FrameWeb/src/fem/file_io.py:466`,
  `FrameWeb/src/fem/file_io.py:490`,
  `FrameWeb/src/fem/result_topology.py:250`,
  `FrameWeb/src/fem/result_topology.py:254`,
  `FrameWeb/src/fem/solver.py:659`,
  `FrameWeb/src/fem/solver.py:664`,
  `FrameWeb/src/fem/result_projection.py:78`, and
  `FrameWeb/src/fem/result_projection.py:308`.
- **Current behavior/evidence:** the legacy parser adds a restraint object even
  when every DOF value is zero. `public_support_node_ids()` treats every
  restraint-map key as a public support, but the solver emits a reaction row only
  when at least one fixed or spring DOF exists. Projection then requires a row
  for the declared support and fails when it is missing.
- **Impact:** a legacy payload accepted by input parsing can fail atomically in
  result projection instead of producing no support or a clear input error.
- **Suggested improvement:** reject or ignore all-zero support rows during
  parsing, and derive `support_node_ids` only from nodes with at least one actual
  restraint or spring. Add a direct HTTP regression test.

### Low — Validators do not enforce the modal frequency/eigenvalue relationship

- **Location:** `FrameWeb/src/fem/result_projection.py:269`,
  `FrameWeb/src/fem/result_contracts.py:418`,
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:708`, and
  `FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json:179`.
- **Current behavior/evidence:** the producer correctly computes
  `frequency = sqrt(eigenvalue) / (2*pi)`, but both semantic validators accept any
  two positive finite values and the schema expresses no relationship.
- **Impact:** a saved or third-party canonical payload can carry physically
  contradictory modal values while passing both runtime boundaries.
- **Suggested improvement:** add a named relative tolerance check in Python and
  TypeScript and a shared negative fixture. Keep this as a semantic rule outside
  JSON Schema if necessary.

### Low — A solver invariant failure is classified as invalid client input

- **Location:** `FrameWeb/src/fem/analysis_result_sets.py:76`,
  `FrameWeb/src/fem/analysis_result_sets.py:78`, and
  `FrameWeb/src/fem/analysis_result_sets.py:207`.
- **Current behavior/evidence:** an unsupported `analysis_type` returned by the
  solver raises `ValueError`; the outer case wrapper converts arbitrary
  `ValueError` into `InputValidationError` and HTTP 400.
- **Impact:** an internal solver/result integration regression is attributed to
  the caller, weakening monitoring and diagnostics.
- **Suggested improvement:** raise `ResultContractError` or a dedicated internal
  invariant exception here, and restrict input-error conversion to parsing and
  selection boundaries.

### Low — Dead legacy display/rate implementations remain in the product tree

- **Location:** `FrameWeb/src/app/result.py:113`,
  `FrameWeb/src/app/result.py:170`, `FrameWeb/src/app/result.py:284`,
  `FrameWeb/src/app/inputData.py:379`, and
  `FrameWeb/src/app/components/load.py:42`.
- **Current behavior/evidence:** the active HTTP/Angular calculation path no
  longer uses `rate`, but the old display-result builder still applies it to
  legacy `disg/reac/fsec` output, and old input/load classes retain the field.
  Repository import/call searches did not identify these modules in the active
  calculation path.
- **Impact:** the deletion decision is incomplete, the obsolete schema remains
  discoverable, and future reuse can reintroduce the removed multiplier and
  display contract.
- **Suggested improvement:** confirm the modules are unused with an import and
  packaging inventory, then delete them or move them into an explicitly isolated
  legacy package that is excluded from the public runtime.

## Confirmed Strengths

- `FrameWeb/main.py` returns the canonical root for plain and compressed POST
  success responses without `Accept` negotiation or legacy alternatives.
- Backend orchestration preserves input case order, isolates a fresh model per
  case, compares units and topology across cases, and validates the complete
  result set before returning it.
- The public topology uses input identities, deterministic generated station
  identities, explicit local frames, and exact member/shell/solid result
  coverage.
- Nonlinear accepted steps are projected as sibling results with consecutive
  indices and exactly one final marker; modal vectors are mass-normalized,
  ordered, sign-canonicalized, and stabilized within degenerate eigenspaces.
- Case-specific support IDs are separate from shared topology, and automatic 2D
  auxiliary restraints are excluded.
- The canonical frontend root is recursively frozen before indexing, and workers
  receive derived data instead of mutating the base response.

## Codex Consultation

The required read-only consultation used
`.agents/logs/codex/prompt-quality-review.md`. The first invocation exposed a
Windows multiline-argument issue and produced no useful review. The prompt was
rewritten as one line and rerun through the required wrapper. That run exceeded
the wrapper's 600-second limit, but the wrapper saved and verified a substantive
13,805-character response at
`.agents/logs/codex/20260918T144214Z-quality-review.md` before reporting the
timeout.

The consultation's public-API, member-force extrema, asynchronous completion,
validator parity, schema-test, all-zero-support, modal-relation, error-typing,
and dead-rate observations were independently checked against current files and
included above. Its assessment that static-only derived behavior fully passed
was not accepted: the current whole-set gate rejects valid static operands in a
mixed result set. The moving-load and pre-validation persistence regressions
were found independently and added to this report.

## Remaining Risks

- Generated-node remapping in `FrameWeb/src/fem/result_topology.py:232` uses
  coordinate-only matching. Distinct solver nodes at identical coordinates may
  therefore be ambiguous; this is a plausible edge case but was not reproduced
  during this read-only review.
- The index object freezes its container and canonical value, but the runtime
  `Map` instances at `analysis-result-set.ts:791` remain mutable if a caller
  bypasses `ReadonlyMap` typing with a cast. No active mutation was found.
- Worker messages remain effectively untyped, and several large handwritten
  validator functions duplicate constants and rules across Python, TypeScript,
  and JSON Schema. This is the main long-term parity-drift risk.

## Follow-up Review — 2026-09-19

### High-finding verification

#### Addressed — public Python calculation contract is canonical-only

`FemModel.run()` now stores and returns a validated `AnalysisResultSet`.
`model.py:428` keeps solver-native state in the private `_solver_snapshot`, while
`get_results()` validates and returns only public canonical state.
`save_results()` and `load_results()` persist and validate that same root, and
the convenience accessors select canonical result rows instead of reading flat
solver keys. `test_result_projection.py:149` covers return/storage/accessor
behavior, and `test_model_roundtrip.py:40` proves solver-native result files and
manually injected native state are rejected by the public lifecycle.

#### Addressed — moving-load grouping, presentation, printing, and animation

The implementation now groups a parent `LL` case with its decimal child cases
without consuming a following ordinary case, builds immutable table envelopes,
restores LL flags and print notification, and drives displacement animation by
canonical selection keys. The final 3D-reaction gap found during follow-up was
also fixed: `result-reac.service.ts:152` now passes
`buildMovingLoadAbsoluteRows(...)` to `ThreeReactService`, selecting the signed
greatest-absolute child value independently for each node/component. The pure
regression at `analysis-result-presentation.spec.ts:59` verifies this behavior
and verifies that source rows are not mutated.

### Addressed Medium findings

- Member-force extrema now use signed extrema and retain winning member IDs via
  `calculateMemberForceMetrics()`; its focused positive/negative regression is
  at `analysis-result-presentation.spec.ts:88`.
- Result loading now awaits all three typed worker pipelines with
  `Promise.all()` at `result-data.service.ts:127`; worker `error` and
  `messageerror` events reject the operation, and callers announce/persist
  success only after completion.
- The TypeScript boundary now trims required strings, rejects empty labels and
  solid locations, validates nonlinear iteration diagnostics, and enforces
  modal ordering/groups. It additionally checks the modal frequency relation.
- Calculation, preset, and project-load callers persist only the validated
  `accepted.value`; the primary calculation path does so at
  `app.component.ts:276-277`.
- Derived-operation validation now checks each referenced case against the
  static-case set instead of rejecting an otherwise valid mixed result set.
- `public_support_node_ids()` now excludes restraints whose six fixed flags are
  all false while retaining spring and nonlinear supports. The two regressions
  at `test_result_projection.py:189` and `test_result_projection.py:203` cover
  programmatic and legacy inputs.

### Remaining Medium risks

1. **The shared JSON Schema is still not executed.**
   `FrameWeb/tests/io/test_result_contracts.py:18-25` only inspects selected
   schema fields, while fixture validation at lines 31 and 51 calls the Python
   semantic validator. No Draft 2020-12 schema engine is declared or invoked,
   and the Angular tests likewise do not compile the shared schema. The schema
   can therefore drift while both handwritten validators remain green.
2. **Public Python documentation contradicts the new `run()` return value.**
   `FrameWeb/docs/wiki/results.md:5` still says `model.run()` returns a
   solver-native dictionary, while `python-api.md:54`, `getting-started.md:80`,
   `quick-reference.md:61`, and `file-formats.md:92` teach access through the
   old flat `node_displacements` shape. This will mislead callers as soon as the
   remaining public-accessor split is removed and already misdocuments the
   current `run()` return.

### Remaining Low risks

- TypeScript enforces `frequency = sqrt(eigenvalue) / (2*pi)` at
  `analysis-result-set.ts:719-724`, but Python
  `result_contracts.py:420-425` still accepts any positive finite frequency.
  The boundary implementations therefore remain semantically asymmetric.
- Dead display-specific `rate` handling remains in `FrameWeb/src/app/result.py`,
  `FrameWeb/src/app/inputData.py`, and `FrameWeb/src/app/components/load.py`.
  It is outside the active HTTP/Angular path but conflicts with the deletion
  decision and remains available for accidental reuse.
- Generated solver nodes are still remapped by coordinates alone in
  `result_topology.py:232-245`; coincident generated nodes remain ambiguous.
- `ReadonlyMap` protects the frontend indexes only at compile time; the runtime
  maps can still be mutated through a cast. No active mutation was found.

### Follow-up verification

- `uv --directory FrameWeb run --locked --extra dev python -m pytest
  tests/integration/test_result_projection.py tests/io/test_result_contracts.py
  tests/io/test_legacy_cases_api.py -q` — **44 passed**.
- `uv --directory FrameWeb run --locked --extra dev python -m pytest
  tests/integration/test_result_projection.py tests/io/test_model_lifecycle.py
  tests/io/test_model_roundtrip.py tests/io/test_diagnostics.py -q` — **31
  passed** after the public Python state split.
- `npx --prefix FrameWebforJS tsc --project
  FrameWebforJS/tsconfig.app.json --noEmit` — **passed**.
- `npx --prefix FrameWebforJS tsc --project
  FrameWebforJS/tsconfig.spec.json --noEmit` — **passed**.
- Focused Karma invocation for the three new provider specs — **blocked by the
  already-declared environment defects**: missing `src/polyfills.ts`, missing
  `src/test.ts`, and unresolved
  `node_modules/@fortawesome/some-free/js/all.min.js`.

## Final Verdict

**PASS.** No Critical or High finding remains. Public HTTP and Python result
paths now expose, store, and persist only validated `AnalysisResultSet` roots;
the moving-load table, print, animation, and 3D reaction behavior is restored.
The remaining Medium and Low items above should be tracked separately but do
not block this review gate.
