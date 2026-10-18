# FrameWebforJS result display: Phase-1 context

## Reproduction Evidence

- The direct Flask-client reproduction completes calculation and transport with HTTP 200, then decodes a result whose top-level keys are `analysis_type`, `constitutive_element_stresses`, `displacement`, `displacement_correction`, `element_stresses`, `force_recovery`, `metadata`, `node_displacements`, and `reaction_forces`. It finds zero values shaped as a legacy load case containing all of `disg`, `reac`, and `fsec`, so its final assertion fails: `.agents/logs/repro-framewebforjs-results-not-displayed.py:27-49`. This is a response-contract failure after successful calculation, not the earlier compressed-request failure.

## Old Backend Contract

- The old endpoint assigns `Controller.results` directly to the HTTP result and serializes that object unchanged (`C:/Users/sasai/Documents/FrameWeb2/main.py:65-78`). `Controller` builds `results[id]` once for every `inputData.loadCases` entry (`C:/Users/sasai/Documents/FrameWeb2/app/controller.py:90-102`), while `make_result` gives each case the keys `disg`, `reac`, `fsec`, `shell_fsec`, and `size` (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:135-179`). The effective wire shape is therefore `{ "1": { "disg": ..., "reac": ..., "fsec": ..., ... }, "2": {...}, ... }`.

## Current Backend Contract

- The current endpoint constructs one `FemModel`, calls `run()` once, and serializes that result directly (`FrameWeb/main.py:106-118`). `result_to_jsonable` only converts NumPy/scalar/container values; it does not project or wrap the result into the old case schema (`FrameWeb/src/fem/file_io.py:817-839`). For legacy JSON input, `_read_json_model` invokes `select_case` before building the model (`FrameWeb/src/fem/file_io.py:70-79`), and `select_case` defaults to the first load key and replaces `data['load']` with that single case (`FrameWeb/src/fem/legacy_beam.py:6-20`). The Ct preset contains 11 load cases (`1` through `11`), so a shape-only wrapper around the one current solve would still lose cases 2-11.

## Frontend Consumption Flow

- On HTTP success, the app decodes the gzip JSON, removes only top-level numeric accounting fields, calls `InputData.getResult`, passes the remaining object unchanged to `ResultData.loadResultData`, and immediately sets the global `ResultData.isCalculated = true` (`FrameWebforJS/src/app/app.component.ts:249-289`). `loadResultData` performs no response-schema validation and forwards the same top-level object to the displacement, reaction, and section-force services (`FrameWebforJS/src/app/providers/result-data.service.ts:87-95`).
- Each first-stage Worker assumes every top-level object is a load case and silently skips it unless it contains its legacy field: `disg` (`FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-39`), `reac` (`FrameWebforJS/src/app/components/result/result-reac/result-reac1.worker.ts:27-40`), or `fsec` (`FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:41-69`). All three then post an empty result with `error === null` (`result-disg1.worker.ts:105-114`, `result-reac1.worker.ts:95-103`, `result-fsec1.worker.ts:244-253`). The downstream services treat that as success; for example displacement copies zero keys and later marks itself calculated (`FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:145-184,239-245`). Thus the observed response is expected to produce no exception or alert: the UI becomes calculated/enabled but its per-case tables and drawings are empty.

## Immediate Cause

- The immediate non-display cause is a producer/consumer schema mismatch: the backend emits one flat solver result, whereas the unchanged frontend consumes a map of legacy load cases with nested `disg`/`reac`/`fsec`. The Workers deliberately `continue` on every current top-level field and report empty maps as successful work. This precisely explains “calculation completed, but nothing is displayed.”

## Candidate Hypotheses

- **Confirmed — production needs an explicit compatibility projection.** Merely changing HTTP encoding cannot help; the projection must create each legacy case and map reaction names (`fx/fy/fz` to `tx/ty/tz`) plus section forces (`element_stresses` end arrays to member `P1...Pn` records). A test-only implementation of this family of mapping already converts `node_displacements`, `reaction_forces`, and `element_stresses` into `disg`, `reac`, and `fsec` (`FrameWeb/tests/support/section_cut_view.py:6-12,63-104`), but it is not used by the endpoint.
- **Confirmed — multi-load-case execution is part of the defect, not a later enhancement.** Old code calculates every load case; current legacy parsing selects only the first. Restoring the old response contract therefore requires per-case selection/solve/projection (or an equivalent multi-case API), preserving original case IDs and case-specific fix/material/joint/load data.
- **Secondary — readiness timing can mask diagnosis but is not the cause.** The global calculated flag is set before the asynchronous Workers finish (`app.component.ts:287-289`), and result links use the displacement service's separate flag (`app.component.ts:334-376`). Even after that flag becomes true, the confirmed empty Worker output leaves pages blank; waiting longer cannot populate the missing cases.

## Existing Coverage/Gaps

- Backend HTTP tests intentionally assert the flat solver schema (`FrameWeb/tests/io/test_http.py:18-34`), and compressed-transport tests assert that legacy and canonical request envelopes return that same flat schema (`FrameWeb/tests/io/test_compressed_transport.py:60-79`). The legacy view helper is exercised by physics/comparison tests (for example `FrameWeb/tests/harness/test_comparison.py:28-38`), but no production endpoint test asserts the old outer case map. Repository search found no frontend spec covering `loadResultData`, the three first-stage Workers, empty-case rejection, or Ct multi-case navigation. Missing acceptance coverage is: HTTP result contains all 11 Ct case IDs; every case has non-empty `disg`/`reac`/`fsec`; Workers reject an incompatible flat response instead of reporting empty success; and each result page renders a selected case.

## Files/Lines

- Old contract: `C:/Users/sasai/Documents/FrameWeb2/main.py:65-78`; `C:/Users/sasai/Documents/FrameWeb2/app/controller.py:90-102`; `C:/Users/sasai/Documents/FrameWeb2/app/result.py:135-179`.
- Current contract: `FrameWeb/main.py:106-118`; `FrameWeb/src/fem/file_io.py:70-79,817-839`; `FrameWeb/src/fem/legacy_beam.py:6-20`.
- Frontend assignment: `FrameWebforJS/src/app/app.component.ts:249-289`; `FrameWebforJS/src/app/providers/result-data.service.ts:87-95`.
- Silent consumers: `FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-39,105-114`; `result-reac/result-reac1.worker.ts:27-40,95-103`; `result-fsec/result-fsec1.worker.ts:41-69,244-253`.
