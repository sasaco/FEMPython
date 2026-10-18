# Impact Assessment: FrameWebforJS calculation transport regression

## Introducing Change

- **Introducing backend commit:** `29df328eb29d1a17b91f706eb1b7f6dd16a0fa78` (`2026-09-08 20:32 +0900`, `fix(fem): validate nonlinear input and solver integration`). In `main.py`, it replaced `eval(b)` with `json.loads(b)` and added the comment `legacy JSON byte-array transport, never execute input`.
- **Prior behavior:** after Base64 decode, `eval(b"31,139,...")` produced a tuple and `eval(b"[31,139,...]")` produced a list, so the old backend accepted both the browser's bracketless decimal CSV and the backend-preferred JSON array. A read-only compatibility probe confirmed both forms. The old behavior was also a remote-code-execution risk and must not be restored.
- **New behavior:** `json.loads(b"31,139,...")` fails at the first comma with `JSONDecodeError: Extra data: line 1 column 3`, while `json.loads(b"[31,139,...]")` succeeds. The security hardening was intentional and correct, but it narrowed an undocumented de facto contract without changing the existing JavaScript producer.
- **Producer history limit:** the available monorepo history first records `FrameWebforJS/src/app/app.component.ts` in import commit `be59bd4` (`2026-09-16`), already containing `btoa(pako.gzip(...))`. Earlier frontend provenance is unavailable in this repository. The incompatible backend change nevertheless predates the import and is an ancestor of `HEAD`; the combined monorepo was definitely broken from `be59bd4` onward.
- **Related commits:** `d111a02` (`2026-09-09`) documented Base64 of a JSON byte array as the current compressed request format; `f275b4e` (`2026-09-10`) added compressed HTTP parity tests but constructed that canonical array in Python, masking the browser producer. `971f98f` (`2026-09-16`) added the local launcher/smoke test but exercised ordinary JSON for calculation and bracketless CSV only for print. `90ea69c` (`2026-09-17`) enabled local anonymous calculation and thereby exposed the already-existing failure to more local users; it did not change compression.

## Blast Radius

### Producer/consumer inventory

| Classification | Path | Contract and impact |
|---|---|---|
| Definitely affected | `FrameWebforJS/src/app/app.component.ts:196-247` | Every authenticated or allowed-local-anonymous calculation calls one private compressed producer. It emits Base64 of bracketless CSV, so every model/preset fails against the current backend before model parsing. Ct data is not the trigger. Browser and Electron builds share this code. |
| Definitely affected | `FrameWeb/main.py:91-115,140-160` | Every compressed calculation request whose Base64-decoded text is bracketless CSV fails. Any `Content-Encoding` value other than exact `json` selects this decoder, including the frontend's `gzip,base64`. |
| Potentially affected | Deployed/cached FrameWebforJS bundles and external legacy calculation clients | Any client retaining the old CSV wire form fails after its backend is upgraded to `29df328` or later. Actual production/staging version skew is not recorded in this repository. |
| Unaffected, but regression-sensitive | `FrameWebforJS/src/app/components/print/print.component.ts:967-1011` and `FramePrintPDF/FramePrintAzure/Function1.cs:34-48`, `Function2.cs:34-48` | Print is a different API. Its C# consumers intentionally Base64-decode bracketless CSV and split on commas. It works with the existing producer and would break if a shared helper changed it to bracketed JSON. |
| Unaffected | Ordinary JSON callers | Requests with no `Content-Encoding` or with `Content-Encoding: json` use `request.get_json()` and never enter `Compressor.decompress`. |
| Unaffected | Canonical compressed clients | The four Python test producers and the Wiki example send Base64 of a bracketed JSON byte array and remain accepted. |
| Unaffected | GET, OPTIONS, and successful-response decompression | These paths do not parse the compressed request envelope. The response format is raw gzip bytes followed by Base64 and is a separate asymmetric contract. |
| Unaffected | `scripts/smoke-local.py:92-99` | Its bracketless producer targets the C# print API, not calculation; it is evidence that print must remain CSV. |

### Secondary compatibility blocker

Fixing the request envelope is necessary but may not make the UI calculation usable. The current backend returns flat `node_displacements`, `reaction_forces`, and `element_stresses`, while `ResultData.loadResultData()` and its workers treat top-level entries as load cases containing `disg`, `reac`, and `fsec`. A live canonical-envelope probe bypassed the `Extra data` error but, because it only approximated Angular's normalization, then reached a separate model-input `NoneType` error. Therefore neither successful decode nor HTTP 200 is an adequate acceptance criterion; the actual Ct UI flow must populate results successfully.

## Coverage Gap

- `FrameWeb/tests/integration/test_input_routes.py:86-89`, `tests/integration/test_spatial_general_plane.py:95-101`, `tests/io/test_axial_force_input.py:173-181`, and `tests/regression/test_slip_support_history.py:30-39` all generate `base64(json.dumps(list(gzip(...))))` inside Python. They prove the consumer-preferred envelope, not compatibility with the real JavaScript producer.
- `FrameWebforJS` contains no `*.spec.ts` files. Its Protractor sample only checks the application title; no calculation, compressed transport, error branch, or result ingestion is covered.
- `scripts/smoke-local.py` checks the engine through ordinary JSON and checks PDF through the print API. It never sends the browser's compressed calculation request, explaining why the combined launcher was reported healthy.
- No test pins the calculation and print formats as intentionally different. No mixed-version compatibility test exists. No negative security tests constrain a possible legacy parser.

Exact regression checks needed:

1. A pure calculation-encoder unit test that Base64-decodes the produced body, parses it as a JSON list of integer bytes, ungzips it, and obtains the original JSON. Assert the existing header and leave the print helper untouched.
2. An executable **actual JavaScript producer -> Python `Compressor.decompress`/Flask** contract test. It must call the production calculation encoder, not recreate the desired body in Python.
3. A print contract regression showing that the print producer still Base64-decodes to bracketless decimal CSV and that the C# print endpoint still returns a PDF.
4. A real UI/browser test using `InputData.getInputJson(0)` for `サンプル（Ct桁）.json`, asserting no transmission dialog, successful calculation, and populated `disg`/`reac`/`fsec` result consumption. This will distinguish the transport fix from the separate input/result-schema problems.
5. A large-preset browser memory/performance regression. `サンプル（ラーメン高架橋）.json` compresses to 2,025,295 bytes and produces about 9.65 MB of Base64. Avoid and test against an implementation that first boxes roughly two million bytes through `Array.from`.
6. If a server fallback is selected, negative tests for invalid Base64, empty tokens, leading/trailing commas, signs, whitespace policy, floats, booleans, hex, out-of-range bytes, excessive element/body/decompressed sizes, invalid/truncated gzip, and code-like input. Canonical JSON-array requests and strict legacy CSV must both remain deterministic.

## Regression and Compatibility Risks

### Compatibility matrix

| Calculation client | Pre-`29df328` backend (`eval`) | Current strict backend (`json.loads`) | Optional strict dual decoder |
|---|---|---|---|
| Older/current frontend, bracketless CSV | Accepted, but backend is unsafe | **Fails with current incident** | Accepted only by validated legacy branch |
| Fixed frontend, canonical JSON array | Accepted | Accepted | Accepted |
| Canonical Python/Wiki client | Accepted | Accepted | Accepted |

Compatibility with the pre-`29df328` backend is not an endorsement of deploying it: the unauthenticated `eval` parser remains unsafe.

### Candidate fix risk

1. **Calculation-only canonical producer: low transport risk and recommended.** It is backward-compatible with old and current calculation backends and leaves print intact. Prefer a calculation-specific tested helper such as ``btoa(`[${compressed.join(',')}]`)``; it emits the canonical JSON array without `Array.from`'s high transient boxed-array cost. Residual risk is the independent backend input/result schema mismatch, so full UI validation is mandatory.
2. **Temporary strict server compatibility branch: medium risk, rollout-only.** It can support cached/deployed legacy clients without code execution, but creates two accepted wire formats, expands malformed-input/resource-exhaustion surface, and needs telemetry plus a removal date. Keep `json.loads` as the canonical path and accept legacy CSV only through a narrow grammar and byte/size/gzip limits. `eval` and `literal_eval` are prohibited. A strict splitter/parser does not reintroduce executable parsing; a loose fallback or Python expression parser would.
3. **Shared frontend canonical helper for calculation and print: high risk / reject.** The print C# service parses comma tokens directly; the opening `[` would make `Convert.ToByte` fail.
4. **Direct Base64(raw gzip) protocol rewrite: high migration risk / defer.** It requires coordinated client/server deployment or explicit version negotiation and breaks both current strict and old calculation decoders during skew. It is disproportionate to the incident.

### Environment/deployment considerations

- The frontend target is ES2022 and current browser/Electron runtimes support `Uint8Array.join`. The new wire text is only two characters longer than the existing CSV before Base64; the main performance risk comes from unnecessary intermediate allocations, not network size.
- Do not combine this fix with `Content-Encoding` cleanup. The current nonstandard `gzip,base64` frontend value and documented `gzip` value both select the same backend branch; changing header semantics at the same time would widen proxy/deployment risk.
- If frontend and backend cannot be deployed atomically and old cached bundles matter, deploy the bounded server compatibility branch first, then the canonical frontend, observe legacy use, and remove the branch. If local/rebuilt clients are the only supported consumers, the frontend-only fix avoids server complexity.
- A server fallback should use strict Base64 validation, an ASCII decimal-list grammar, exact integer checks (`bool` excluded), range `0..255`, count/body/decompressed-size limits, and bounded gzip handling. It must never call `eval` or `literal_eval`.

## External Research

None. Repository code, executable probes, and git history fully identify a local producer/consumer contract regression; no dependency defect or upstream workaround is needed.

## Codex Verdicts

- **Required-label call status:** the bounded read-only calls for `troubleshoot-frameweb-regression` and `troubleshoot-frameweb-fix-safety` did not receive usable objectives because the Windows invocation exposed only the first physical prompt line. The regression follow-up timed out. Every returned response file was read; these calls remain unavailable evidence and were not retried after timeout.
- **Regression-risk supplemental verdict (`troubleshoot-frameweb-regression-oneline`): HIGH until the producer/consumer contract is aligned.** Independently verified details match the repository evidence: a calculation-only canonical producer is LOW risk, a temporary strictly validated CSV fallback is MEDIUM risk, sharing that encoder with print is HIGH risk/rejected, and a raw-gzip protocol rewrite is MEDIUM-HIGH migration risk/deferred.
- **Fix-safety supplemental verdict (`troubleshoot-frameweb-fix-safety-oneline`): CAUTION.** The calculation-only ``btoa(`[${compressed.join(',')}]`)`` change is functionally and security-wise safe with both backend generations, preserves `29df328` hardening, and avoids `Array.from`'s extra multi-million-entry allocation. The overall release is not yet SAFE because request acceptance can expose the separate response/result-schema mismatch. The optional server fallback is acceptable only with strict non-executable parsing, independent size/decompression bounds, telemetry, and a removal criterion.
- Both usable supplemental calls were read-only, completed within the 300-second bound at medium reasoning, and were independently checked against code/history. They add risk corroboration; the introducing-change and blast-radius findings do not depend on them.

## Evidence Summary

- `git show/log/blame` proves the strict-parser introduction, prior behavior, intent, and later masking tests/docs without mutating the worktree.
- A read-only Python probe proves old `eval` accepted both envelopes and current `json.loads` only the canonical array.
- A live Node probe using the Ct preset proves the current body returns the exact HTTP 400 while the canonical envelope round-trips and passes beyond the decoder.
- Full-tree searches found one calculation producer, one separate print producer, one Python calculation consumer, two active C# print consumers, four canonical backend test producers, and the print-only smoke producer.
