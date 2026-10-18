# Test Review: FrameWebforJS results-display fix

## Verdict

**NEEDS FOLLOW-UP.** Backend contract coverage is green and materially protects the new representation, but two **High** test/correctness gaps block claiming the user-visible Ct display defect fully complete. No Critical finding was identified.

- Critical: 0
- High: 2
- Medium: 1
- Low: 0
- Completion blocked by High/Critical findings: **Yes**

## Findings

### [High] Empty per-case result maps still pass frontend validation

- **Files:** `FrameWebforJS/src/app/providers/result-data.service.ts:49-79`, `FrameWebforJS/src/app/providers/result-data.service.spec.ts:8-16`
- **Evidence:** `validateLegacyCasesResult` requires `disg`, `reac`, and `fsec` to be objects but does not require them to contain any entries. The positive spec deliberately accepts empty required maps. `loadResultData` then dispatches all three result services, and the calculation path subsequently sets `ResultData.isCalculated = true` (`app.component.ts:295-300`).
- **Impact:** A structurally case-shaped but empty backend response can still reproduce the failure class the fix is meant to eliminate: workers receive no usable rows while the request is treated as calculated.
- **Required regression:** Reject an expected Ct/basic case when any of `disg`, `reac`, or `fsec` is empty, or document and test a narrower model-aware exception. Add explicit spies proving `setDisgJson`, `setReacJson`, and `setFsecJson` are not called and `isCalculated` remains false.

### [High] No executable Ct 11-case browser/runtime acceptance exists

- **Files:** `FrameWeb/tests/io/test_legacy_cases_api.py:145-217`, `FrameWebforJS/src/app/providers/result-data.service.spec.ts:1-101`, `FrameWebforJS/src/app/app.component.ts:232-305`
- **Evidence:** Backend tests use a hand-checkable two-case plain-JSON fixture. The 110 compressed-transport tests and the legacy selector tests pass separately, but no test exercises their actual FrameWebforJS combination: normalized Ct input, `Content-Encoding: gzip,base64`, `Accept: application/vnd.frameweb.legacy-cases-v1+json`, and ordered cases `1..11`. No browser test opens case 1 and case 11 displacement/reaction/section-force views or observes worker completion/console errors. The focused Jasmine command exits before running any spec because the existing Karma setup omits `src/test.ts` and `src/polyfills.ts` and references a missing Font Awesome script.
- **Impact:** The backend and frontend halves compile independently, but the reported user scenario and worker/UI consumption remain unproved. TypeScript `--noEmit` is not execution evidence.
- **Required regression:** Run the actual normalized Ct request end to end and assert ordered IDs `1..11`, non-empty `disg/reac/fsec` for every case, distinct representative cases, case 1/11 visible output, DEFINE/COMBINE/PICKUP base-case availability, and zero console/worker errors. Until that passes, user-visible completion is blocked.

### [Medium] Projection tests cover only axial signs and omit important legacy branches

- **Files:** `FrameWeb/tests/io/test_legacy_cases_api.py:96-131`, `FrameWeb/src/fem/legacy_results.py:134-157,160-201,256-278`
- **Evidence:** The two-case fixture strongly covers rate-once behavior, node labels, numeric ordering, axial `fxi/fxj`, `P1/P2`, `L`, `size`, atomicity, and input immutability. All shear/torsion/bending expected values are zero, so sign regressions in `fyi/fzi/mxi/myi/mzi/fyj/fzj/mxj/myj/mzj` would pass. The 2D auxiliary-restraint/forced-zero path and rigid-boundary/load-only-subdivision grouping are also untested. The atomic test does not assert the returned `case_id` diagnostic.
- **Recommended regression:** Add one small bending/torsion fixture with nonzero values at both ends, one 2D auxiliary-support fixture, and a multi-case rigid/load-point split fixture. Assert the later failing case ID in the diagnostic payload.

## Adequate Existing Coverage

- Default/no selector and `Accept: application/json` preserve the flat response.
- Exact v1 selector returns the vendor media type and ordered case map.
- Unknown legacy vendor version returns 406.
- Modern and shell inputs are rejected for v1.
- Two cases use distinct element/support references and distinct exact results.
- Non-unit `rate`, metre/radian displacement values, reaction rename/zero-fill, axial force signs, `P1/P2` order/length, beam `shell_fsec`, and `size` are pinned.
- Repeated helper calls are deterministic and do not mutate the caller input.
- A later invalid case cannot return a partial 200 map.

## Independent Test Execution

1. `uv run --project FrameWeb --locked --extra dev pytest FrameWeb/tests/io/test_http.py FrameWeb/tests/io/test_legacy_cases_api.py FrameWeb/tests/io/test_compressed_transport.py -q`
   - Exit 0; **139 passed in 11.55s**.
2. `npx tsc -p tsconfig.app.json --noEmit`
   - Exit 0.
3. `npx tsc -p tsconfig.spec.json --noEmit`
   - Exit 0.
4. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/providers/result-data.service.spec.ts`
   - Exit 1 before test execution: `src/test.ts` and `src/polyfills.ts` missing from TypeScript compilation; missing `node_modules/@fortawesome/some-free/js/all.min.js`; **0 frontend specs executed**.

## Coverage

Coverage was **not measured**. No fresh scoped coverage artifact was available, and the frontend test runner failed before executing assertions. No percentage is estimated.

## Re-review after fixes (2026-09-18)

### Verdict

**PASS for the two prior High blockers.** Both are resolved. No remaining Critical or High finding from this test re-review blocks completion.

- Critical: 0
- High: 0
- Completion blocked by High/Critical findings: **No**
- The earlier Medium projection-branch gap remains outside this targeted re-review and is non-blocking.

### Prior High: empty per-case results — RESOLVED

- `validateLegacyCasesResult` now rejects every case whose `disg`, `reac`, and `fsec` maps are all empty, before any result worker dispatch.
- The focused spec covers all-empty rejection and deliberately accepts each legitimate partial-empty shape. This is the narrower model-compatible exception requested by the initial review: an individual result category may validly be empty, but an entirely empty case may not be reported as calculated output.
- The existing fail-fast load test still proves `isCalculated` is cleared before validation and execution stops on invalid input.
- Both frontend application and spec TypeScript projects compile successfully after the change.

### Prior High: Ct 11-case browser/runtime proof — RESOLVED

- `.agents/logs/e2e-framewebforjs-ct-evidence.json` records an actual Angular/Flask browser run of `サンプル（Ct桁）.json` with input, response, displacement, reaction, and section-force case IDs all exactly `1..11` in order.
- All three worker completion flags are true, the completion alert was observed, and no browser error was recorded.
- The displacement result table was visible for case 1 and after selecting case 11. The case 11 sample row records node 2 as `dx=0.0008 mm`, `dy=0.0166 mm`.
- `.agents/logs/e2e-framewebforjs-ct.cjs` is a reusable CDP evaluation driver rather than a self-contained automated assertion suite. That limits replay convenience, but the detailed run artifact directly covers the previously unproved user scenario and is sufficient to clear the High blocker.

### Re-review validation

1. `uv run --project FrameWeb --locked --extra dev pytest FrameWeb/tests/io/test_http.py FrameWeb/tests/io/test_legacy_cases_api.py FrameWeb/tests/io/test_compressed_transport.py -q`
   - Exit 0; **142 passed in 10.01s**.
2. `npx tsc -p tsconfig.app.json --noEmit`
   - Exit 0.
3. `npx tsc -p tsconfig.spec.json --noEmit`
   - Exit 0.
4. Parsed the Ct evidence and independently asserted exact ordered `1..11` IDs for all five recorded ID sets, calculated/worker/display flags, and zero browser errors.
   - Exit 0; evidence assertions passed.
5. `node --check .agents/logs/e2e-framewebforjs-ct.cjs`
   - Exit 0.
