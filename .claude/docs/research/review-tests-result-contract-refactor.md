# Test Review: AnalysisResultSet-Only Result Contract Refactor

## Verdict

**PASS — final focused follow-up 2026-09-19.** No Critical or High test finding
remains. The work-amplification boundary and canonical public Python lifecycle
are covered, and the new executable Angular integration spec closes the former
High display-pipeline gap by driving the real App, result orchestration, three
result services, persistence, and pager through six deterministic fake Worker
completion boundaries. Remaining breadth and environment limitations are
listed separately as Medium/Low risks and do not block this verdict.

Coverage was **not measured**. `gather_diff.py` reported `coverage: null`, and
the repository has no configured pytest coverage dependency or canonical
coverage command. No percentage is estimated.

## Focused Follow-up Review — 2026-09-19

| Severity | Open | Blocking |
|---|---:|---:|
| Critical | 0 | 0 |
| High | 0 | 0 |
| Medium | 6 | 0 |
| Low | 2 | 0 |

### TEST-1 — Resolved: bounded nonlinear and case fan-out before model creation

- `MAX_NONLINEAR_STEPS_PER_CASE`, `MAX_ITERATIONS_PER_STEP`,
  `MAX_PROJECTED_STATES_PER_REQUEST`, and
  `MAX_NONLINEAR_ITERATIONS_PER_REQUEST` now bound the previously arbitrary
  nonlinear schedule and case fan-out.
- `_validate_request_work_budget()` runs before `_read_json_model()` and
  `FemModel()` construction. Tests use sentinels to prove rejection occurs at
  that boundary for top-level step overrides, per-step iteration limits,
  explicit `load_factors`, displacement-control targets, request-wide state
  totals, and request-wide nonlinear iteration totals.
- Exact-limit tests establish inclusive boundaries. This removes the compact
  request amplification described by the original High finding. A
  topology/DOF-aware state-row quota remains desirable and is recorded below as
  a non-blocking Medium hardening/test gap.

### TEST-2 — Resolved: executable integrated Angular success and rejection paths

- `analysis-result-set.spec.ts` and the review-negative matrix cover the strict
  validator and canonical index. `result-worker-pipeline.spec.ts` covers typed
  completion plus `message`, `error`, and `messageerror` failure propagation.
  `result-data.service.spec.ts` proves the top-level calculated flags are set
  only after three injected promises complete.
- `analysis-result-presentation.spec.ts` covers moving-load page grouping,
  max/min envelopes, signed member-force extrema, safe reserved-key storage,
  and the new component-wise absolute reaction-row helper used for the 3D
  moving-load reaction view. Its reaction assertion also proves child positions
  are used instead of the parent aggregate placeholder and that inputs are not
  mutated.
- `result-pipeline.integration.spec.ts` calls the real private
  `AppComponent.post_compress()` response path, real `ResultDataService`, real
  `ResultDisgService`/`ResultReacService`/`ResultFsecService`, real
  `InputDataService.getResult()`, and real `PagerComponent`. Only transport and
  external collaborators are substituted.
- The success case uses six deterministic fake Worker completion boundaries and
  proves persistence occurs only after `isCalculated`, before modal close. It
  asserts moving-load plus following-static page order/labels, formatted table
  rows, LL flags/print refresh, all three 3D handoffs, and the child-position
  absolute reaction vector sent to the 3D reaction service.
- The rejection case sends an invalid decoded response through the same App
  callback and proves zero result-service calls, zero Worker posts, zero
  persistence, unchanged previous input state, null result index, false
  calculated state, visible alert, and modal close.
- The frontend implementer captured **2/2 focused integration specs** and
  **41/41 provider specs** passing in ChromeHeadless with a temporary test-only
  tsconfig and stale-asset shim. Both temporary files were removed. Independent
  review confirms the checked-in spec and application TypeScript programs
  compile, while the default Karma target still has the pre-existing harness
  blockers documented below.

### Canonical public Python boundary — Resolved

- Public `FemModel.run()` is tested as the sole validated `AnalysisResultSet`
  root, including independent nonlinear snapshots, generated stations,
  support filtering, displacement/member accessors, and classification of an
  invalid internal analysis type as a contract error.
- The result lifecycle round trip covers `run()`, `get_results()`,
  `save_results()`, and `load_results()`. Separate negative assertions reject a
  solver-native snapshot when loading, getting, or saving public state.
- The focused Python command listed below passed all 53 selected tests.

### Remaining Medium risks

1. **Transport-size boundaries (original TEST-3):** no encoded, compressed, or
   expanded request-size limits or gzip-bomb boundary tests exist because the
   inherited limits are still absent.
2. **Schema/parity matrix (original TEST-5):** the checked-in Draft 2020-12
   schema is still inspected rather than executed by a schema validator. The
   new semantic mutation matrix is TypeScript-only; Python and TypeScript do not
   consume one shared negative matrix that proves parity for every invariant.
3. **Legacy analysis precedence (original TEST-6):** the ordered mixed test is
   still nonlinear plus static only. There is no static/nonlinear/modal request
   or observable case/top-level precedence matrix for `n_load_steps`,
   `max_iterations`, `load_factors`, and `n_modes`.
4. **Shell/solid breadth (original TEST-7):** canonical projection remains
   limited to a quadrilateral shell and `tetra10`; TypeScript shared fixtures
   still contain no non-empty shell/solid results. Other declared formulations
   and frontend location/order selection remain unproved.
5. **Topology-aware total work:** the new preflight bounds states and solver
   iterations but not the product of projected states and topology/result-row
   cardinality. A model-size/DOF-aware quota and limit-boundary test remain
   useful defense in depth.
6. **Angular variant/Worker breadth:** the integration fixture exercises the
   full real service pipeline for moving-load and ordinary static snapshots,
   but not nonlinear-step or modal snapshots. The six actual Worker entry-point
   scripts are compiled and their shared completion primitive has direct specs,
   but the integration test intentionally substitutes deterministic fake Worker
   replies because the legacy Karma webpack target does not transform their
   `import.meta.url` URLs.

### Remaining Low risks

- Reserved-ID regression explicitly covers `__proto__` and `constructor`, but
  not `prototype`, and it exercises `createSafeRecord()` rather than all three
  derived-result service adapters.
- Exact 256-case acceptance, custom/cross-case units, exact nonlinear
  diagnostics partitioning remain without direct tests.

### Follow-up verification and environment-only blockers

- `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_legacy_cases_api.py tests/io/test_model_roundtrip.py tests/io/test_model_lifecycle.py tests/io/test_result_contracts.py tests/integration/test_result_projection.py -q`
  -> **53 passed** in 0.75s.
- `npm exec -- tsc --noEmit -p tsconfig.spec.json` from `FrameWebforJS/`
  -> **passed**. All current TypeScript specs type-check.
- `npm exec -- tsc --noEmit -p tsconfig.app.json` from `FrameWebforJS/`
  -> **passed**. The application program type-checks. Separate frontend
  implementer evidence records successful standalone checks for all six Worker
  sources.
- Captured executable Karma evidence with temporary test-only harness inputs:
  focused integration **2/2 passed** and all provider specs **41/41 passed**.
- Independent rerun with the checked-in default Karma configuration -> **0
  specs executed**. Bundle setup reports that `src/polyfills.ts` and
  `src/test.ts` exist but are absent from the test TypeScript program, and the
  configured `node_modules/@fortawesome/some-free/js/all.min.js` asset is
  missing. This reproduces the known harness blocker after the temporary
  inputs were removed; it is not a failed product assertion.
- `npm --prefix FrameWebforJS run build` -> **failed before product
  compilation** because `src/environments/environment.prod.ts` is absent.
- These default Karma/build failures are existing harness/environment blockers;
  no frontend assertion failed. The temporary harness run nevertheless provides
  executable ChromeHeadless evidence for the checked-in integration spec.

## Original Review Record (Superseded Where the Follow-up Says Otherwise)

## Review Scope

- Full patch: `.agents/logs/review-diff-result-contract-refactor.patch`
- Approved plan and normative `AnalysisResultSet v1` contract
- Backend orchestration, projection, schema/fixtures, HTTP/compression/error
  tests, nonlinear/modal paths, shell/solid paths, and regression migrations
- Frontend validator/indexer, request/response flow, result services/workers,
  pager, static-only derived-result guard, immutability behavior, and tests
- Lead gate: `.agents/logs/check-20260918T141444756Z-59412.log`
- Security review: `.agents/docs/research/review-security-result-contract-refactor.md`

The shared fixture design is sound: both Python and TypeScript import the same
six positive and seven negative JSON files. Python tests are deterministic and
independent, use scoped `monkeypatch`, and exercise the Flask boundary without
external network calls. No order-dependent test state or weakened assertion was
found in the reviewed backend suite.

## Findings

### [High][TEST-1] No pre-solve total-work limit or boundary test protects nonlinear fan-out

- **File/function**: `FrameWeb/src/fem/analysis_result_sets.py::_enumerate_cases`;
  `FrameWeb/src/fem/model.py::FemModel._run`;
  `FrameWeb/tests/io/test_legacy_cases_api.py::test_case_count_guard_rejects_before_model_creation`
- **Missing scenario**: the only resource boundary test rejects case 257. It
  does not bound `n_load_steps`, `max_iterations`, explicit `load_factors`,
  topology/entity count, projected result rows, or the product of those values
  across 256 cases. A compact request can therefore reach `np.arange` and
  solver/model construction with arbitrarily large requested work, matching
  security finding SEC-1.
- **Proposed test**: after product limits are frozen, parameterize every limit
  at `limit - 1`, `limit`, and `limit + 1`; spy on `FemModel` and the solver to
  prove an over-budget request is rejected with stable HTTP 400 before the
  first model/allocation. Add a 256-case aggregate-budget case whose individual
  cases are legal but whose total state/topology budget is illegal, plus an
  explicit-`load_factors` length boundary.

### [High][TEST-2] The canonical Angular success pipeline has no executable runtime test

- **File/function**: `FrameWebforJS/src/app/providers/result-data.service.ts::loadResultData`;
  `ResultDisgService.setDisgJson`, `ResultReacService.setReacJson`,
  `ResultFsecService.setFsecJson`; the six result workers;
  `PagerComponent.refreshPages`; `AppComponent.post_compress`
- **Missing scenario**: `analysis-result-set.spec.ts` tests the pure validator,
  while `result-data.service.spec.ts` has only one invalid-payload test created
  via `Object.create`. No test executes a valid result through worker dispatch,
  formatted rows, 3D-service handoff, page labels/order, first/last static
  cases, every nonlinear step, a modal mode, worker failure, or the HTTP decode
  and visible-error path. Karma did not start because `src/polyfills.ts`,
  `src/test.ts`, and the FontAwesome asset are missing; the production build is
  also blocked by missing `src/environments/environment.prod.ts`.
- **Proposed test**: add fake-Worker service tests that feed the shared static,
  nonlinear, and modal fixtures and assert ordered rows, selection keys,
  `isCalculated`, derived-operation calls, and worker error propagation. Add
  pager bounds/labels tests. Once the declared environment files/assets are
  restored, run nonzero Karma specs and a browser E2E that posts the Ct model
  and verifies first/last cases, every accepted step, reactions, member-force
  tables/diagrams, and visible rejection of nonlinear/modal combinations.

### [Medium][TEST-3] Compressed request size limits have no tests because no limits exist

- **File/function**: `FrameWeb/main.py::Compressor.decompress`;
  `FrameWeb/tests/io/test_compressed_transport.py`
- **Missing scenario**: the transport suite thoroughly covers malformed
  Base64/CSV/gzip/JSON, but not encoded-body size, compressed-byte size,
  expanded JSON size, concatenated gzip members, or a high-compression-ratio
  payload. This leaves security finding SEC-2 unguarded.
- **Proposed test**: introduce fixed body/compressed/expanded limits and cover
  `limit - 1`, `limit`, `limit + 1`, concatenated members, and a small gzip bomb.
  Assert HTTP 400 and bounded reads/allocation rather than merely asserting a
  post-decompression error.

### [Medium][TEST-4] Adversarial case IDs are not covered at the derived-result boundary

- **File/function**: `ResultDisgService.setDisgJson`,
  `ResultReacService.setReacJson`, `ResultFsecService.setFsecJson` in
  `FrameWebforJS/src/app/components/result/`; shared contract fixtures
- **Missing scenario**: the contract permits every non-empty string, but no
  fixture or service test uses `__proto__`, `constructor`, or `prototype`.
  Assignment into each service's plain `{}` `staticByCaseId` object mutates its
  prototype or collides with inherited keys, matching security finding SEC-3.
- **Proposed test**: add a shared positive fixture with those case IDs and a
  frontend service regression proving all three IDs remain ordered own entries
  through DEFINE/COMBINE/PICKUP input. Prefer assertions against a `Map` (or a
  null-prototype object) rather than narrowing the public ID contract.

### [Medium][TEST-5] The checked-in JSON Schema and strict-validator parity are not tested

- **File/function**: `FrameWeb/tests/io/test_result_contracts.py`;
  `FrameWeb/src/fem/result_contracts.py::_validate_results`;
  `FrameWebforJS/src/app/providers/analysis-result-set.ts::validateResults`
- **Missing scenario**: the test reads the schema and checks only its draft,
  root reference, and two constants; no Draft 2020-12 validator executes it
  against the fixtures. The seven negative fixtures do not cover result order,
  empty/missing cases/results, support order, frame orthogonality, station and
  shell/solid location coverage, analysis-type mismatch, nonlinear iteration
  indexing/non-negative norms, modal eigenvalue order, or degeneracy-group
  continuity. This already hides frontend/backend drift: Python requires each
  nonlinear iteration index to equal its array index and residual/correction
  norms to be non-negative, and requires modal eigenvalues/groups to be ordered;
  the TypeScript validator does not enforce those invariants.
- **Proposed test**: validate every positive/negative fixture with an actual
  Draft 2020-12 implementation, then add one shared mutation matrix consumed by
  both runtimes for every semantic invariant above. Assert both validators
  reject the same mutation before worker dispatch.

### [Medium][TEST-6] Legacy precedence tests omit modal cases and do not prove parameter override precedence

- **File/function**: `FrameWeb/tests/io/test_legacy_cases_api.py::test_top_level_analysis_settings_override_each_legacy_case`;
  `::test_case_analysis_settings_support_ordered_mixed_analysis_types`
- **Missing scenario**: the mixed test contains nonlinear plus static only.
  The top-level override test forces static, so its `n_load_steps` value is
  unused and does not prove default → case → top-level `analysis_params`
  precedence. There is no ordered legacy static/nonlinear/modal request, no
  case-level `n_modes`, and no top-level nonlinear/modal parameter override.
- **Proposed test**: build a three-case legacy model with static, nonlinear,
  and modal cases and assert case-major result variants/order. Add separate
  case-level values and present top-level overrides for `n_load_steps`,
  `max_iterations`, `load_factors`, and `n_modes`, then assert observed step/mode
  counts and effective state values.

### [Medium][TEST-7] Shell/solid contract coverage is backend-only and covers too few formulations

- **File/function**: `FrameWeb/tests/integration/test_result_projection.py::test_shell_projection_uses_serialized_local_frame_and_weighted_average`;
  `::test_solid_projection_covers_topology_gauss_points_in_formulation_order`;
  `FrameWebforJS/src/app/providers/analysis-result-set.spec.ts`
- **Missing scenario**: canonical projection tests one quadrilateral shell and
  one `tetra10`; no canonical test covers `triangle3`, `tetra4`, `wedge6`,
  `hexa8`, `wedge15`, or `hexa20`. Frontend selector tests use only empty
  shell/solid arrays, so non-empty topology/result validation and selection have
  never executed in TypeScript.
- **Proposed test**: add shared non-empty shell and solid fixtures and assert
  frontend selection preserves element/location order and all components.
  Parameterize backend topology/projection over every declared element type,
  including row-count mismatch and local-frame/location-order rejection.

### [Low][TEST-8] Invalid frontend responses are not proven absent from persisted input state

- **File/function**: `FrameWebforJS/src/app/app.component.ts::post_compress`;
  `InputDataService.getResult`;
  `FrameWebforJS/src/app/providers/result-data.service.spec.ts`
- **Missing scenario**: the app stores `InputData.result` before strict
  validation. The existing invalid-data spec only checks
  `ResultDataService.isCalculated`; it cannot detect that invalid data remains
  in `InputData.result` and may be saved later, matching security finding
  SEC-4.
- **Proposed test**: exercise the app response callback with an invalid payload
  and assert both result stores are null, no worker is posted, saved JSON omits
  the payload, the user sees the contract error, and the wait dialog closes.

### [Low][TEST-9] Several result-set boundary invariants have only one-sided evidence

- **File/function**: `FrameWeb/tests/io/test_legacy_cases_api.py`;
  `FrameWeb/tests/integration/test_result_projection.py::test_every_nonlinear_step_is_a_complete_independent_snapshot`
- **Missing scenario**: there is no cheap acceptance test for exactly 256 cases
  (using a mocked solver), units copied from non-default normalized metadata,
  cross-case unit mismatch, or an exact one-to-one comparison between each
  step's diagnostics and its owning slice of cumulative convergence history.
  The current diagnostics assertion checks only total record count and final
  convergence, so duplicated/omitted records could preserve that count.
- **Proposed test**: mock the solve/projection boundary for 1/256-case
  acceptance and units mismatch, add an explicit custom-unit result-set test,
  and compare flattened snapshot diagnostics field-for-field with the original
  `(step, iteration)` history while also asserting no record appears twice.

## Confirmed Coverage

- Sole `AnalysisResultSet` success root is asserted for normal and alternate
  `Accept` headers.
- Legacy case insertion order, input immutability, no `rate` scaling, modern
  single case `"1"`, case-specific support sets, and auxiliary-restraint
  exclusion are covered.
- Later invalid case and injected topology mismatch return no partial result.
- Static and nonlinear displacement/reaction/member-force values have HTTP and
  independent numerical-oracle coverage.
- Nonlinear steps are siblings, consecutive, complete, and have one final
  marker; deterministic degenerate modal projection is covered.
- Quadrilateral shell weighted averages/local frame and `tetra10` integration-
  point order are covered in Python.
- Plain/compressed transport parity, malformed transport/input errors, generic
  internal failure, and reanalysis after nonlinear failure are covered.
- Shared positive/negative fixtures are imported by both Python and TypeScript;
  integer-like case order, explicit selectors, base-response freezing, and the
  static-only helper are specified in Jasmine tests, although those specs did
  not execute in the current Angular environment.

## Test Execution Results

- Lead full Python suite: **3270 passed**, 0 failed, in 24m05s.
- Reviewer focused command:
  `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_result_contracts.py tests/io/test_legacy_cases_api.py tests/integration/test_result_projection.py -q`
  -> **32 passed**, 0 failed, in 0.55s. These are a subset of the full suite and
  are not added to the 3270 total.
- Angular Karma: **not executed**; bundle setup failed before spec execution due
  to missing `src/polyfills.ts`, `src/test.ts`, and
  `node_modules/@fortawesome/some-free/js/all.min.js`.
- Angular production build: **not executed successfully**; blocked by missing
  `src/environments/environment.prod.ts`.
- Coverage: **not measured** (`coverage: null`; no configured coverage tool).

## Residual Review Limits

- No browser/Electron E2E was possible in the declared Angular environment.
- No load/peak-memory, cancellation, or gzip expansion benchmark was run; the
  corresponding missing limits are established directly from control flow and
  the security review.
- Full-suite evidence is taken from the lead's final gate log; this reviewer
  reran only the focused 32-test canonical subset.
