# Security Review: FrameWebforJS legacy-cases-v1 result compatibility

## Recommendation

**PASS after re-review** — the prior **High** availability finding is resolved.
The compatibility route now rejects more than 256 load cases before entering
the per-case loop, `select_case()`, model construction, or solving. No open
Critical or High finding remains.

The inherited Medium compressed-size hardening item and Low `Accept` quality
item remain non-blocking follow-ups. Exact media-type selection, failure
atomicity, unsupported shell/solid rejection, and public error sanitization are
otherwise sound.

## Re-review Verdict

- **Prior High SEC-1: RESOLVED.** `MAX_LEGACY_CASES = 256` is checked by
  `_validate_legacy_beam_input()` before `solve_legacy_cases()` creates the
  result accumulator or enters its case loop (`FrameWeb/src/fem/legacy_results.py:16,35-46,64-72`).
- The HTTP boundary returns 400 `invalid_input` for 257 cases. The regression
  test replaces `FemModel` with a sentinel and proves no model is constructed
  (`FrameWeb/tests/io/test_legacy_cases_api.py:239-261`). Static control flow
  also proves `select_case()` and `model.run()` are unreachable after that
  validation failure.
- Boundary tests admit 255 and 256 cases at the validator and reject 257; the
  endpoint documentation states the same limit and pre-analysis rejection
  (`FrameWeb/tests/io/test_legacy_cases_api.py:229-261`,
  `FrameWeb/docs/wiki/endpoints.md:75`).
- Independent reviewer rerun: focused legacy-case suite **11 passed**. Lead
  evidence reports **142 backend tests passed** and ruff passed for changed
  files.
- A maximum of 256 solves is still a meaningful operational workload, but it is
  bounded and documented. Capacity measurement, gateway rate limiting, and
  execution timeouts remain defense in depth; they do not leave the original
  unbounded-cardinality High open.

## Review Scope

- `FrameWeb/main.py`
- `FrameWeb/src/fem/legacy_results.py`
- `FrameWeb/tests/io/test_legacy_cases_api.py`
- `FrameWeb/docs/wiki/endpoints.md`
- `FrameWebforJS/src/app/app.component.ts`
- `FrameWebforJS/src/app/providers/result-data.service.ts`
- `FrameWebforJS/src/app/providers/result-data.service.spec.ts`

Interactions inspected only where necessary: `select_case()` behavior and the
shared diagnostic boundary. Unrelated dirty-tree files and pre-existing
transport changes were not reviewed as product changes.

Focus: `Accept` negotiation, compressed-input trust boundaries, request and
case cardinality, information disclosure, frontend response validation,
shell/solid exclusion, and all-or-nothing behavior.

## Severity Summary

| Severity | Open | Blocking |
|---|---:|---:|
| Critical | 0 | 0 |
| High | 0 | 0 |
| Medium | 1 | 0 |
| Low | 1 | 0 |

## Resolved Findings

### [Resolved High][SEC-1] Unbounded load-case fan-out enabled request-level compute and memory exhaustion

- **Evidence**: `FrameWeb/src/fem/legacy_results.py:16,35-46,64-72`,
  `FrameWeb/src/fem/legacy_beam.py:6-14`,
  `FrameWeb/tests/io/test_legacy_cases_api.py:229-261`,
  `FrameWeb/main.py:78-90`, `FrameWeb/docs/wiki/endpoints.md:27,75`
- **Original issue**: validation required at least one load case but set no
  upper bound. `solve_legacy_cases()` iterates accepted entries and performs a
  fresh `FemModel.read_json_model()` plus `run()` for each one. The called
  `select_case()` deep-copies the complete request on every iteration, so
  unbounded case cardinality also implied unbounded copying and retained result
  growth before the final atomic response.
- **Original exposure**: the application advertises wildcard CORS and explicitly
  documents that it has no in-application authentication or rate limiting. The
  `Accept` header was enough to select that unbounded high-amplification path.
- **Why it mattered**: the default flat path solves one selected case, whereas
  the initial compatibility route turned attacker-controlled case cardinality
  into an unbounded number of full solves.
- **Resolution**: the implementation now caps the route at 256 cases inside the
  mandatory validator, before the loop and before any `deepcopy` or solve.
  Tests cover 255, 256, and 257 and prove the rejected request constructs no
  `FemModel`; documentation records the contract. The limit is not tied to Ct's
  count of 11 and therefore leaves room for dotted moving-load cases.

## Open Findings

### [Medium][SEC-2] Compressed request and decompressed JSON sizes remain unbounded

- **Evidence**: `FrameWeb/main.py:107-114`, `FrameWeb/main.py:192-213`
- **Issue**: compressed requests are Base64-decoded and passed to unbounded
  `gzip.decompress()` without application-level limits on encoded bytes,
  compressed bytes, or expanded JSON bytes. A high-ratio or concatenated gzip
  payload can allocate substantial memory before legacy input validation or the
  new case limit can run.
- **Scope assessment**: this boundary predates the legacy-cases-v1 route and is
  not introduced by its selector. It nevertheless composes with the new public
  multi-solve path and remains relevant to deployment of the reviewed working
  tree. The earlier transport review also recorded this gap.
- **Recommended remediation**: enforce a request-body limit before Base64
  decoding and a strict expanded-byte limit with bounded/streaming gzip
  decompression. Reject over-limit data as a stable client error and test exact
  boundaries plus a high-compression-ratio payload. Keep infrastructure body,
  memory, and timeout controls even after adding application limits.

### [Low][SEC-3] Accept parsing ignores an explicit `q=0` refusal

- **Evidence**: `FrameWeb/main.py:160-176`
- **Issue**: parameters are stripped before selection, so
  `Accept: application/vnd.frameweb.legacy-cases-v1+json; q=0` selects the
  legacy representation even though quality zero means the representation is
  unacceptable. This does not let a caller select a representation it could
  not already request, so the direct security impact is low, but it weakens the
  exact-negotiation contract.
- **Recommended remediation**: parse media ranges and quality values, ignore
  entries with `q=0`, and add tests for exact v1 at positive quality, v1 at
  zero quality, wildcard/default behavior, and mixed supported/unsupported
  vendor versions.

## Security Properties Verified

- The compatibility representation is selected only by the explicit v1 vendor
  media type; unknown `application/vnd.frameweb.legacy-cases-*` versions fail
  closed with 406 instead of falling back to the flat response.
- Shell and solid input with non-empty data is rejected before solving, and the
  v1 result always emits an empty `shell_fsec` rather than claiming shell
  compatibility.
- Every case receives a fresh `FemModel`; no mutable solver state is shared
  across cases.
- The server serializes only after all cases finish. A later failure therefore
  cannot expose a partial case map, and the focused atomicity test checks this
  behavior.
- Known input errors retain structured diagnostics. Unexpected exceptions cross
  the shared diagnostic boundary without stack traces or internal exception
  text. Case IDs echoed in validation errors originate in the caller's own
  request and are JSON-encoded.
- Frontend validation rejects a flat backend response, empty top-level result,
  missing/extra/reordered requested case IDs, non-object cases, and missing or
  non-object `disg`/`reac`/`fsec` fields before worker dispatch. It also resets
  `isCalculated` before validation.
- The reviewed changes add no secret, credential, dynamic evaluation, shell
  execution, filesystem path use, SQL, or HTML injection sink.

## Non-blocking Validation Note

The frontend validator intentionally validates the compatibility envelope, not
every numeric leaf, and permits individual `disg`, `reac`, or `fsec` maps to be
empty. That is appropriate only if valid models can legitimately produce an
empty category. User-visible completion should still include the planned Ct
browser check that confirms non-empty display data for representative cases;
HTTP 200 and schema acceptance alone are insufficient.

## Verification Evidence

- Supplied focused backend evidence: **139 passed** across legacy-case, existing
  HTTP, and compressed-transport tests.
- Supplied frontend evidence: application and spec TypeScript no-emit checks
  passed.
- Re-review focused legacy-case suite: **11 passed**.
- Re-review lead evidence: **142 backend tests passed**; ruff passed on changed
  files.
- Manual security trace covered media selection, compressed decode, every
  per-case allocation/solve, error return paths, and pre-worker frontend
  validation.
- No product or test file was modified by this review.
