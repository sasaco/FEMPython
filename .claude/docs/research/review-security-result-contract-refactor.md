# Security Review: AnalysisResultSet-Only Result Contract Refactor

## Final Verdict — Follow-up 2026-09-19

**PASS.** No Critical or High security finding remains after the focused fix
review. SEC-1, SEC-3, SEC-4, and SEC-5 are resolved. The only open finding is
SEC-2, an inherited Medium risk at the pre-existing compressed-request boundary;
it is explicitly separated below and does not block this result-contract
refactor under the requested gate criterion.

| Severity | Open | Blocking |
|---|---:|---:|
| Critical | 0 | 0 |
| High | 0 | 0 |
| Medium | 1 | 0 |
| Low | 0 | 0 |

## Focused Follow-up Review — 2026-09-19

### SEC-1 — Resolved: bounded, inclusive, request-wide work preflight

- **Files/lines**: `FrameWeb/src/fem/result_contracts.py:10-16`;
  `FrameWeb/src/fem/analysis_result_sets.py:39-48,269-416`;
  `FrameWeb/tests/io/test_legacy_cases_api.py:160-333`
- The synchronous API now caps cases at 256, nonlinear steps per case at
  1,000, iterations per step at 1,000, request-wide projected states at 10,000,
  and request-wide projected nonlinear iterations at 500,000.
- `build_analysis_result_set()` enumerates all requested cases and completes
  `_validate_request_work_budget()` before `_read_json_model()`, `FemModel()` or
  any solver call. The preflight derives explicit schedule length from
  `load_factors` or displacement-control `targets`, counts modal `n_modes`, uses
  the same case-then-top-level override precedence as model ingestion, rejects
  booleans/non-positive/non-integer controls, and aggregates every case before
  any model is created.
- The comparisons reject only `requested > limit`, so each exact boundary is
  inclusive. Regression tests cover exact step/iteration/request totals,
  limit+1 cases, top-level overrides, explicit schedules, and multi-case
  crossing at the later case while sentinel assertions prove no model or model
  JSON conversion started.
- **Security conclusion**: the compact-request arbitrary amplification described
  by the original High finding is removed. Operational deadlines, rate limits,
  and topology/DOF-aware quotas remain worthwhile defense in depth, but the
  formerly unbounded control fields are now bounded before execution.

### SEC-3 — Resolved: JavaScript-reserved case IDs remain data keys

- **Files/lines**: `FrameWebforJS/src/app/providers/analysis-result-presentation.ts:26-28`;
  `FrameWebforJS/src/app/providers/analysis-result-set.ts:647-771,798-833`;
  `FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:161-169`;
  `FrameWebforJS/src/app/components/result/result-reac/result-reac.service.ts:137-142`;
  `FrameWebforJS/src/app/components/result/result-fsec/result-fsec.service.ts:140-146`;
  `FrameWebforJS/src/app/providers/analysis-result-presentation.spec.ts:99-116`
- Canonical indexes use `Map<string, ...>`, while the three legacy derived-result
  adapters and related keyed accumulators use `Object.create(null)` through
  `createSafeRecord()`. The public contract therefore continues to accept
  `__proto__`, `constructor`, and `prototype` without treating them as prototype
  members or dropping them from own-key enumeration.
- The checked-in regression covers the two prototype-sensitive names; an
  independent Node probe additionally verified all three names as own keys both
  before and after structured cloning, matching the worker-transfer boundary.

### SEC-4 — Resolved: strict validation precedes persistence on every changed path

- **Files/lines**: `FrameWebforJS/src/app/app.component.ts:269-277`;
  `FrameWebforJS/src/app/components/menu/menu.service.ts:71-80`;
  `FrameWebforJS/src/app/components/menu/menu.component.ts:417-427,688-700,729-739`;
  `FrameWebforJS/src/app/components/preset/preset.component.ts:74-84`;
  `FrameWebforJS/src/app/providers/result-data.service.ts:105-145`
- Every changed calculation/file/preset path now awaits
  `ResultData.loadResultData(untrustedValue)` first and calls
  `InputData.getResult(accepted.value)` only after strict validation and all
  three worker pipelines complete. A validation or worker-start failure throws
  before the received value can enter saved input state.

### SEC-5 — Resolved: full request/result console logging removed

- **File/lines**: `FrameWebforJS/src/app/app.component.ts:230-277`
- The former `console.log(json)` and `console.log(jsonData)` calls are absent.
  Repository searches found no other console call that logs the full calculation
  request or AnalysisResultSet. Remaining calls in this flow are a localized
  success status and the existing HTTP error object, not the successful model or
  result payload.

### Open inherited risk — SEC-2 (Medium): unbounded gzip expansion

- **File/lines**: `FrameWeb/main.py:91-102,145-165`
- This is unchanged from the pre-refactor transport: the endpoint fully
  Base64-decodes and calls unbounded `gzip.decompress()` before JSON/case/work
  validation. No request-body, compressed-byte, or expanded-byte ceiling is
  present. It remains a separate deployment hardening item and can still cause
  memory exhaustion before the new solver-work preflight runs.

### Follow-up Verification

- `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_legacy_cases_api.py tests/io/test_result_contracts.py tests/io/test_http.py tests/io/test_compressed_transport.py -q`
  -> **164 passed**.
- Targeted TypeScript compile of the validator, presentation helpers, and review
  spec with `tsc --noEmit` -> **passed**.
- Independent Node safe-record/structured-clone probe for `__proto__`,
  `constructor`, and `prototype` -> **passed**.
- Direct Jasmine execution outside Angular stopped before spec execution because
  the installed Node 24 runtime requires a JSON import attribute for the shared
  fixture. This is a harness/runtime incompatibility, not a product assertion
  failure; full Angular Karma/build remain subject to the already-declared
  missing bootstrap/environment/assets blockers.

## Original Review Record (Superseded by the Follow-up Verdict)

## Review Scope

- Full review patch: `.agents/logs/review-diff-result-contract-refactor.patch`
- Current backend entry point, case orchestration, contract validation,
  topology/projection, solver snapshot normalization, and changed FEM
  post-processing files
- Current Angular result validator/indexer, request/response handling,
  result services/workers, pager, and rate-removal changes
- Shared JSON Schema, positive/negative fixtures, focused contract/API tests,
  endpoint documentation, and prior security findings relevant to the retained
  compression boundary

Focus areas were secrets, injection, unsafe parsing, response trust boundaries,
resource exhaustion, cross-case state isolation, atomicity, sensitive-data
exposure, and dependency changes.

## Original Severity Summary (Before Fixes)

| Severity | Open | Blocking |
|---|---:|---:|
| Critical | 0 | 0 |
| High | 1 | 1 |
| Medium | 2 | 0 |
| Low | 2 | 0 |

## Original Findings

### [Resolved High][SEC-1] Analysis work and result cardinality were unbounded below the 256-case cap

- **Files/lines**: `FrameWeb/src/fem/analysis_result_sets.py:42-48,45-46,100-101,127-137`; `FrameWeb/src/fem/model.py:491-497`; `FrameWeb/src/fem/solver.py:373-384`; `FrameWeb/docs/wiki/endpoints.md:143`
- **Issue**: `MAX_RESULT_CASES` bounds only the number of cases. The existing
  `n_load_steps` and `max_iterations` inputs are checked only for being positive
  integers, with no maximum. When `load_factors` is absent, the solver executes
  `np.arange(1, n_steps + 1, dtype=float)` before solving. The new application
  service then solves every accepted case and retains every projected step in
  `results` until the atomic response is serialized.
- **Evidence/impact**: a compact request containing, for example,
  `n_load_steps: 1000000000` can request an approximately 8 GB factor array
  before topology-sized snapshots or solver iterations are considered. The
  same top-level override can apply to every one of 256 cases. This creates a
  low-bandwidth memory/CPU denial-of-service path. The endpoint contains no
  authentication check, and the documentation confirms that it is synchronous
  and has no server-side cancellation endpoint; a client timeout does not stop
  the analysis.
- **Recommended fix**: freeze and enforce product limits for `n_load_steps`,
  `max_iterations`, explicit `load_factors`, model entity counts, and total
  request work. Validate the complete case/state/topology budget before the
  first `FemModel` construction or `np.arange` allocation. Add server-side
  execution deadlines/cancellation and deployment rate limiting. Add boundary
  tests proving an over-budget top-level override is rejected before any model
  or solver is constructed, including the 256-case multiplier.

### [Open Inherited Medium][SEC-2] Compressed request and expanded JSON sizes remain unbounded

- **File/line**: `FrameWeb/main.py:91-102,145-165`
- **Issue**: the request is fully Base64-decoded, materialized as a byte-list or
  decimal CSV, converted again to `bytes`, and passed to unbounded
  `gzip.decompress()` without application-level limits on the encoded body,
  compressed bytes, or expanded JSON bytes.
- **Evidence/impact**: a high-ratio or concatenated gzip payload can consume
  substantial memory before `_enumerate_cases()` reaches the 256-case check or
  any model validation runs. This is inherited from the pre-refactor transport
  and was previously recorded as Medium, but the sole public multi-case path
  continues to compose with it.
- **Recommended fix**: set a request-body limit before Base64 decoding and use
  bounded/streaming gzip expansion with a strict maximum expanded JSON size.
  Reject limit violations as stable 400 errors and test limit-1, limit, limit+1,
  and a high-compression-ratio payload. Retain infrastructure memory and timeout
  controls as defense in depth.

### [Resolved Medium][SEC-3] A `__proto__` case ID mutated derived-result map prototypes

- **Files/lines**: `FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:84-93`; `FrameWebforJS/src/app/components/result/result-reac/result-reac.service.ts:82-91`; `FrameWebforJS/src/app/components/result/result-fsec/result-fsec.service.ts:80-90`
- **Issue**: the v1 contract accepts any non-empty case ID, including
  `"__proto__"`. Each service reconstructs static results into `{}` with
  `staticByCaseId[entry.caseId] = entry.rows`. On a normal JavaScript object,
  assigning the `__proto__` key invokes the prototype setter instead of creating
  an own case property.
- **Evidence/impact**: an independent Node probe produced `Object.keys(map) ==
  []` and an Array prototype after assigning an attacker-controlled
  `"__proto__"` key. The backend and strict frontend validator both accept that
  non-empty ID, so a crafted legacy model can silently remove the case from the
  object enumerated by DEFINE/COMBINE/PICKUP processing and corrupt or abort
  derived structural results. This is object-local prototype manipulation, not
  global `Object.prototype` modification, but it is still a calculation
  integrity and availability issue.
- **Recommended fix**: keep case-indexed data in `Map<string, ...>` throughout,
  or use `Object.create(null)` with explicit own-property access at the legacy
  derived-result boundary. Do not narrow the public case-ID contract merely to
  accommodate unsafe object dictionaries. Add a shared positive fixture and
  frontend regression using case IDs `__proto__`, `constructor`, and
  `prototype` and prove all cases remain own entries in order.

### [Resolved Low][SEC-4] An invalid server payload was persisted before strict validation succeeded

- **Files/lines**: `FrameWebforJS/src/app/app.component.ts:270-284`; `FrameWebforJS/src/app/providers/input-data.service.ts:48-50,281-283`; `FrameWebforJS/src/app/providers/result-data.service.ts:105-109`
- **Issue**: `InputData.getResult(jsonData)` stores the parsed response before
  `ResultData.loadResultData(jsonData)` invokes the strict validator. If
  validation throws, the catch path marks calculation unsuccessful but does not
  clear `InputData.result`.
- **Evidence/impact**: a malformed or compromised-backend response does not
  start result workers, but it remains mutable application state and is included
  in later saved input JSON whenever `getInputJson()` runs. This weakens the
  intended single trust boundary and can persist or redistribute an invalid
  payload after the UI has reported failure.
- **Recommended fix**: validate/index first, then store only
  `index.value` after validation succeeds. Clear both result stores on every
  validation/worker-start failure. Prefer one method that atomically validates,
  stores, and starts consumers so callers cannot invert the order. Add a test
  that an invalid payload leaves both `ResultData.resultSet` and
  `InputData.result` null and is absent from saved JSON.

### [Resolved Low][SEC-5] Full calculation inputs and results were written to the browser console

- **File/line**: `FrameWebforJS/src/app/app.component.ts:222-235,270-272`
- **Issue**: the frontend adds the authenticated Firebase UID to the calculation
  request, then logs the full serialized request. It also logs the complete
  structural result payload.
- **Evidence/impact**: the browser/Electron console can expose a user identifier,
  geometry, loads, supports, and solved forces/displacements to local support
  tooling, extensions, screen recordings, or retained diagnostic logs. The log
  statements predate this result refactor, but remain in a changed product file
  and now log the complete AnalysisResultSet.
- **Recommended fix**: remove the payload logs. If development diagnostics are
  required, guard concise metadata behind a non-production build flag and log
  counts/request IDs rather than identifiers or model/result contents.

## Security Properties Verified

- No hardcoded credential, private key, token, dynamic evaluation, raw HTML
  insertion, shell/process execution, SQL, or filesystem path sink was added.
- No dependency manifest or lockfile changed, so the refactor introduces no new
  package dependency to audit.
- `select_case()` deep-copies the source request and every iteration constructs
  a fresh `FemModel`; mutable solver/model state is not shared across cases.
- The service serializes only after all cases solve, project, and validate.
  Later-case failure therefore does not expose a partial result set.
- Backend validation rejects extra fields, duplicate IDs/coordinates,
  non-finite numbers, cross-reference/count mismatches, invalid nonlinear final
  markers, and unsupported state/analysis pairings.
- Unexpected internal exceptions cross `diagnostic_payload()` as a generic 500
  without stack traces or internal exception text. Caller-provided case IDs are
  JSON-encoded when reflected in input diagnostics.
- Frontend interpolation is used for case/state labels; no result-controlled
  `innerHTML` or sanitizer bypass was introduced.

## Verification Evidence

- Lead final gate: agent/document/scope/Python/.NET checks passed; Python
  **3270 passed**. Angular execution remains blocked only by the declared
  pre-existing missing files/assets.
- Independent focused rerun:
  `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_legacy_cases_api.py tests/io/test_result_contracts.py -q`
  -> **26 passed**.
- Independent JavaScript prototype probe confirmed that assigning
  `map["__proto__"]` to a normal `{}` changes its prototype and creates no own
  enumerable case key.
- Added-line scans found no secret-like value, dynamic execution, raw HTML,
  process execution, or SQL sink. Dependency manifests and lockfiles are
  unchanged.

## Residual Review Limits

- No live browser E2E or Karma execution was possible because of the declared
  missing Angular bootstrap/environment/assets. Source-level XSS and response
  trust-boundary review was completed instead.
- No load/peak-memory benchmark or dynamic gzip-bomb probe was run; the
  unbounded allocation paths are directly established by control flow.
- Authentication and deployment policy remain outside this refactor. If the
  calculation endpoint is exposed beyond the intended local boundary, the open
  inherited SEC-2 decompression risk is directly reachable; SEC-1 is resolved
  by the bounded preflight described above.
