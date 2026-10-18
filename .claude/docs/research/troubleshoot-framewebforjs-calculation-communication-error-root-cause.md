# Root Cause Analysis: FrameWebforJS Calculation Communication Error

## Definitive Root Cause

The observed error is a compressed-request wire-contract regression between the calculation producer and consumer.

- Producer defect: `FrameWebforJS/src/app/app.component.ts:233-245` passes the `Uint8Array` returned by `pako.gzip()` directly to `btoa()`.
- JavaScript converts that typed array to bracketless decimal CSV, for example `31,139,8,...`, before Base64 encoding it. The request is therefore `base64(ASCII("31,139,8,..."))`.
- Consumer contract: `FrameWeb/main.py:153-160` Base64-decodes the body and runs `json.loads(b)`, which requires a JSON integer array such as `[31,139,8,...]` before reconstructing the gzip bytes.
- Historical trigger: commit `29df328eb29d1a17b91f706eb1b7f6dd16a0fa78` replaced unsafe `eval(b)` with `json.loads(b)`. The security hardening was correct, but the old evaluator had accidentally accepted the producer's bracketless CSV as a Python tuple. The unchanged producer and narrowed consumer became incompatible.

This is not a network outage. The backend receives the POST and returns an application-generated HTTP 400. It is also not caused by Ct-girder fields: the failure occurs before gzip decompression, model parsing, or structural analysis.

## Exact Execution and State Transforms

| Step | Location | State |
|---|---|---|
| 1 | `FrameWebforJS/src/app/app.component.html:20` | Calculation click calls `calcrate()`. |
| 2 | `FrameWebforJS/src/app/app.component.ts:196-226` | Login/local-anonymous gate passes; `InputData.getInputJson(0)` constructs the calculation object; `uid` and `production` are added; `post_compress()` is called. |
| 3 | `FrameWebforJS/src/app/app.component.ts:233` | Object becomes a JSON string. |
| 4 | `FrameWebforJS/src/app/app.component.ts:236` | `pako.gzip(json)` returns `Uint8Array([31, 139, 8, ...])`. |
| 5 | `FrameWebforJS/src/app/app.component.ts:238` | `btoa(compressed)` first stringifies the typed array as `"31,139,8,..."`, then Base64-encodes those ASCII characters. |
| 6 | `FrameWebforJS/src/app/app.component.ts:240-247` | Angular sends that Base64 text with `Content-Encoding: gzip,base64`. |
| 7 | `FrameWeb/main.py:91-102` | Because the encoding is not exactly `json`, `FEMPython()` sends `request.data` to `Compressor.decompress()`. |
| 8 | `FrameWeb/main.py:153-156` | Base64 decode recreates `b'31,139,8,...'`; `json.loads()` parses `31` as one complete JSON number. Character index 2 is the following comma, which is illegal extra top-level data. Since columns are one-based, this is `line 1 column 3 (char 2)`. |
| 9 | `FrameWeb/main.py:130-132`; `FrameWeb/src/fem/diagnostics.py:64-80` | `JSONDecodeError` is a `ValueError`, classified as `invalid_input`, and returned as HTTP 400. |
| 10 | `FrameWebforJS/src/app/app.component.ts:319-323` | Angular routes non-2xx to the HTTP error callback and shows `message.transmission-error`. |

The following code is never reached for the observed error: `gzip.decompress()`, the inner model `json.loads()`, `_read_json_model()`, `FemModel.read_json_model()`, and `FemModel.run()`.

## Accepted-Envelope Control

`.agents/logs/accepted-envelope-framewebforjs-calculation-communication-error.cjs` creates one Ct-derived gzip stream and sends two bodies that differ only in the outer byte-list representation.

Observed run (`node .agents/logs/accepted-envelope-framewebforjs-calculation-communication-error.cjs`, exit 0):

| Property | Current producer | Accepted control |
|---|---|---|
| Same inner calculation JSON | Yes, 26,630 characters | Yes, 26,630 characters |
| Same gzip bytes | Yes, 4,080 bytes | Yes, 4,080 bytes |
| Base64-decoded prefix | `31,139,8,...` | `[31,139,8,...` |
| Outer JSON parse | Fails at position 2 / column 3 | Produces exactly 4,080 integers |
| Ungzip equality | Not reached | Exact match with the original JSON |
| HTTP result | 400, observed `Extra data` diagnostic | Advances beyond `Compressor.decompress`; later 400 `float() ... NoneType` |

The later accepted-control error is expected from the control's approximation: it deletes save-only preset sections but does not execute all Angular provider normalization performed by `getInputJson(0)`. It does not weaken the transport conclusion. It proves only the transport boundary, not complete Ct calculation success.

The lexical behavior is independently reproducible in Python:

- `eval(b'31,139,8,0')` returns tuple `(31, 139, 8, 0)`.
- `json.loads(b'31,139,8,0')` raises the exact `Extra data: line 1 column 3 (char 2)`.
- `json.loads(b'[31,139,8,0]')` returns the required list.

## Ct Content Boundary

Ct content is **eliminated as a contributor to the observed line/column-3 HTTP 400** because no decompressed model byte has been inspected when that exception is raised. The same producer transformation fails for every model sent through this calculation path to a post-`29df328` backend.

Ct content and Angular normalization remain relevant only after the envelope is repaired. The accepted-envelope control reached a later model-level error because it did not reproduce `getInputJson(0)` exactly. A real Angular-normalized Ct request is therefore required before claiming the complete calculation has been restored.

## Hypotheses Evaluated

| Hypothesis | Evidence for | Evidence against / discriminator | Verdict |
|---|---|---|---|
| 1. Compressed-request transport regression/type-coercion mismatch | Exact producer coercion, exact parser error, same-byte accepted-envelope control, and `29df328` history all agree. | None material. | **CONFIRMED** |
| 2. Frontend/backend deployment-version skew is required for the current incident | Pre-`29df328` backend accepts the legacy body, so version differences explain why older environments could work. | The checked-in current frontend and backend are intrinsically incompatible; mixed deployment versions are not required. No deployment inventory proves current skew. | **ELIMINATED as a required current cause; historical deployment state remains INCONCLUSIVE** |
| 3a. Ct-girder validation/model content causes the observed error | The named preset triggers the UI report. | Failure precedes gzip/model parsing; identical bytes with accepted framing cross the decoder boundary. | **ELIMINATED for the observed error** |
| 3b. Wrong URL or service unavailable | A generic UI message can resemble a connectivity problem. | `127.0.0.1:8080` receives the POST and returns structured application JSON; GET also returns 200. | **ELIMINATED** |
| 3c. CORS | Browser calls can fail at CORS boundaries. | Backend parsing is reached; the independent Node request reproduces the same response without browser CORS enforcement; route declares the required CORS headers. | **ELIMINATED** |
| 3d. Authentication or anonymous UID | The local calculation path permits anonymous use. | The backend performs decompression before any model processing and implements no authentication check here; empty UID reproduces the same parser error. | **ELIMINATED** |
| 4. Green compressed-route tests contradict the mismatch | Existing compressed tests pass. | They manually construct `base64(json.dumps(list(gzip_bytes)))`, the consumer-preferred bracketed envelope, and never run the JavaScript producer. | **ELIMINATED** |

The dedicated Codex hypothesis consultation timed out after its 300-second bound. Its response file was read as required but is not used as validation evidence. The verdicts above rest on direct source, history, runtime, and control evidence.

## Trigger Conditions

All of the following are sufficient to trigger the observed failure:

1. A calculation request uses `AppComponent.post_compress()` and therefore the bracketless `btoa(Uint8Array)` representation.
2. The target FrameWeb backend includes the post-`29df328` `json.loads()` decoder.
3. A non-empty gzip stream begins with the normal bytes `31,139,...`; the comma after `31` is at zero-based character 2.

The particular preset, login identity, CORS origin, structural element type, and analysis solver are not trigger variables.

## Compatibility and Blast Radius

- Every compressed calculation from the current FrameWebforJS producer to the current FrameWeb backend is affected, regardless of preset.
- Ordinary uncompressed JSON clients are unaffected.
- Existing canonical compressed clients and backend tests are unaffected.
- A canonical calculation producer works with both the old `eval` backend and the current `json.loads` backend.
- An old/current legacy calculation producer works only with the old backend unless a secure compatibility parser is added.
- `FrameWebforJS/src/app/components/print/print.component.ts:1006-1008` looks similar but targets a different contract. `FramePrintPDF/FramePrintAzure/Function1.cs:34-49` and `Function2.cs:34-49` explicitly Base64-decode, split bracketless CSV, and convert decimal values to bytes. A shared encoder change would break printing.

## Fix Alternatives

### Approach A: Calculation Producer Conformance (recommended durable code fix)

Emit the documented/canonical `base64(JSON byte array)` only in the calculation path. Semantically:

```typescript
const compressed = pako.gzip(json);
const byteArrayJson = `[${compressed.join(",")}]`;
const base64Encoded = btoa(byteArrayJson);
```

Use a calculation-specific pure helper and exercise that production helper from the cross-language contract test. `JSON.stringify(Array.from(compressed))` is semantically equivalent but should not be the default implementation: the largest checked preset, `サンプル（ラーメン高架橋）.json`, produces 2,025,295 gzip bytes, and `Array.from` would box roughly two million numbers. Direct inventory showed 7,238,353 CSV characters and 9,651,140 Base64 characters. `compressed.join(',')` yields the same canonical JSON text while adding only the two brackets to the representation already created today.

Trade-offs:

- Correctness: directly makes the producer conform to the documented consumer contract.
- Minimality: calculation-only change; no backend attack-surface expansion.
- Maintainability: one canonical calculation format.
- Performance: wire size is essentially the current CSV size plus two characters; `join` avoids the additional boxed-number array.
- Backward compatibility: works with both old `eval` and current `json.loads` backends; does not repair already cached legacy frontend bundles against the current backend.

### Approach B: Strict Backend Dual-Format Compatibility

Keep canonical JSON-array support and additionally accept only a tightly defined legacy decimal CSV grammar. Never restore `eval` or use `literal_eval`. Validate Base64, total input length, non-empty decimal tokens, integer syntax, values `0..255`, gzip integrity, decompressed-size bounds, UTF-8, and top-level JSON object.

Trade-offs:

- Correctness: immediately restores current and already deployed legacy calculation clients.
- Minimality: one backend location, but more validation and tests than Approach A.
- Maintainability: carries two formats and needs telemetry/removal criteria.
- Performance/security: extra parsing and a larger input surface; safe only with strict limits and bounded decompression.
- Backward compatibility: strongest mixed-version coverage.

### Approach C: Versioned Raw-Gzip Protocol

Introduce a new explicit contract using Base64 of raw gzip bytes (or standards-compliant HTTP gzip), while retaining the old version during migration.

Trade-offs:

- Correctness/performance: simplest and smallest representation after migration.
- Minimality: worst for this incident; requires coordinated frontend/backend deployment, version signaling, proxy/cloud validation, and migration tests.
- Maintainability: best long-term wire semantics if versioned, but inappropriate as an unversioned hot fix.
- Backward compatibility: incompatible without a negotiated transition.

## Recommendation

Use **Approach A** as the durable correction because it fixes the violating producer, preserves the current secure decoder, is compatible with old and current backends, and isolates the print contract.

If deployment inventory shows that cached or separately deployed legacy calculation bundles are a supported population, use a phased rollout: deploy **Approach B first as a temporary, measured compatibility layer**, then deploy Approach A, observe legacy usage, and remove Approach B under an explicit criterion. Without that compatibility requirement, do not add a second server grammar.

Do not restore `eval`, do not change the print producer, and do not combine this incident fix with an unversioned raw-gzip protocol migration.

## Separate Post-Transport Blocker

Repairing the envelope removes the observed HTTP 400 but does **not** prove the user can complete a Ct calculation.

- Current backend documentation explicitly says HTTP returns flat `node_displacements`, `reaction_forces`, and `element_stresses`, not `case1.disg/reac/fsec` (`FrameWeb/docs/wiki/endpoints.md:63-73`, `FrameWeb/docs/wiki/results.md:43`).
- `FrameWebforJS/src/app/providers/result-data.service.ts:87-95` passes the top-level response to workers.
- For example, `result-disg1.worker.ts:29-40` treats top-level keys as cases and silently skips any object without `disg`; reaction and section-force workers similarly expect `reac` and `fsec`.
- `app.component.ts:285-289` sets `ResultData.isCalculated = true` immediately after starting the asynchronous result loaders, so an HTTP 200 with the flat schema may produce empty results while still reporting completion.
- The backend analyzes one selected/first load case, while the Ct preset contains multiple load cases. Resolving this is a response/orchestration contract decision, not a simple field rename.

This response-contract incompatibility is separate from the proven transport root cause and should be investigated/planned as the next blocker. It must not be hidden by declaring the calculation fixed after decoder success or HTTP 200.

## Required Validation

1. Unit-test the production calculation encoder with ASCII, Japanese text, empty object, Ct data, and the largest supported preset.
2. Add a real JavaScript-producer-to-Python-`Compressor.decompress` contract test asserting exact object equality; do not duplicate the preferred envelope independently on both sides.
3. Test malformed Base64, empty input, non-array JSON, bool/float/string/nested/out-of-range values, trailing/double commas if compatibility mode exists, truncated/bad-CRC gzip, oversized compressed data, and excessive expansion.
4. Run the exact Angular-normalized Ct payload through transport and model construction.
5. Define and validate load-case cardinality and the result mapping between current backend fields and frontend `disg/reac/fsec` consumers.
6. Browser-test the named Ct preset through completion: HTTP 200, no error dialog, non-empty displacement/reaction/section-force data, correct case count, and working result navigation/rendering.
7. Verify printing independently after any calculation encoder refactor.
8. Measure time and peak memory with `サンプル（ラーメン高架橋）.json` (2,025,295 gzip bytes; 9,651,140 Base64 characters in the current decimal-array protocol).

## Codex Consultations

| Purpose / label | Result | Evidence status |
|---|---|---|
| Initial pattern recognition / `troubleshoot-initial` | Confirmed the envelope-regression pattern and Ct/content boundary. Response: `.agents/logs/codex/20260918T020208Z-troubleshoot-initial.md`. | Available supporting evidence |
| Execution flow / `troubleshoot-frameweb-flow` | Confirmed every state transform and the exact char-2/column-3 explanation. Response: `.agents/logs/codex/20260918T021808Z-troubleshoot-frameweb-flow.md`. | Available, exit 0 |
| Hypothesis evaluation / `troubleshoot-frameweb-hypothesis` | Returned a response consistent with direct evidence, but the wrapper timed out after the 300-second bound. Response: `.agents/logs/codex/20260918T021808Z-troubleshoot-frameweb-hypothesis.md`. | **Unavailable as validation evidence** |
| Fix design / `troubleshoot-frameweb-fix-design` | Recommended backend-first dual compatibility followed by canonical producer for mixed deployed clients; rejected `eval` and unversioned raw-gzip migration. Response: `.agents/logs/codex/20260918T021808Z-troubleshoot-frameweb-fix-design.md`. | Available, exit 0 |
| Fix correctness / `troubleshoot-frameweb-fix-verify` | **INCOMPLETE**: producer fix is correct for the HTTP 400 root cause, but full user outcome requires exact Angular Ct and response-contract/UI verification. Response: `.agents/logs/codex/20260918T022539Z-troubleshoot-frameweb-fix-verify.md`. | Available, exit 0 |

## Remaining Unknowns

- Whether production requirements include cached or independently deployed legacy calculation bundles; this decides whether temporary backend compatibility is necessary.
- The exact supported request/decompressed size limits for safe backend hardening.
- Whether the exact Angular-normalized Ct payload exposes any model-input error after transport repair.
- The intended multi-load-case response and mapping from current backend results to FrameWebforJS `disg/reac/fsec` consumers.
- Full browser peak memory and latency for the largest preset after the canonical encoder change.

No product source, product tests, dependencies, or running services were changed or restarted during this analysis.

