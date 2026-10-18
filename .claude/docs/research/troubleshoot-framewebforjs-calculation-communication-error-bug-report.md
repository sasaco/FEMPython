## Bug Report: FrameWebforJS Ct-girder calculation communication error

### Error
- Message: UI shows `通信エラーが発生しました。担当営業にお問い合わせください。`; direct HTTP reproduction returns `HTTP 400`, `error_code=invalid_input`, and `Extra data: line 1 column 3 (char 2)`.
- Location: request producer `FrameWebforJS/src/app/app.component.ts:233-245`; failing decoder `FrameWeb/main.py:153-160`.
- Stack trace: the backend base64-decodes the request and `json.loads(b)` raises `JSONDecodeError` before `_read_json_model()` or `FemModel.run()`; Angular receives the non-2xx response as `HttpErrorResponse` and enters `app.component.ts:319-323`.

### Reproduction
- Steps:
  1. Run the existing local frontend at `http://localhost:4200` and Flask calculation service at `http://127.0.0.1:8080/`.
  2. Open the built-in `Ct桁` preset, displayed as `サンプル（Ct桁）.json`.
  3. Click `計算`, then confirm `はい`.
  4. Observe the generic transmission-error dialog.
  5. Independently run `.agents/logs/repro-framewebforjs-calculation-communication-error.cjs`; it reproduces the frontend wire transform and receives the exact HTTP 400 diagnostic.
- Reproducibility: always in the current local environment. `GET /` returns 200, so the calculation server is reachable.

### Immediate Context
- Failing code: `pako.gzip(json)` returns a `Uint8Array`, but `btoa(compressed)` coerces it to bracketless CSV text such as `31,139,8,...`. The backend expects base64 of a JSON integer array such as `[31,139,8,...]` and calls `json.loads` before constructing bytes.
- Call chain: calculation link → `AppComponent.calcrate()` → `InputData.getInputJson(0)` → `AppComponent.post_compress()` → `POST http://127.0.0.1:8080/` → `FEMPython()` → `Compressor.decompress()` → `json.loads()` failure → diagnostic HTTP 400 → Angular HTTP error callback → generic transmission dialog.
- Recent changes: Codex and git history identify commit `29df328` as replacing permissive `eval(b)` with `json.loads(b)`. The old decoder accepted bracketless comma-separated values as a Python tuple; the hardened decoder requires valid JSON, exposing the pre-existing producer/consumer mismatch.

### Affected Area
- Files involved: `FrameWebforJS/src/app/app.component.ts`, `FrameWeb/main.py`, `FrameWeb/src/fem/diagnostics.py`; closest similar producer at `FrameWebforJS/src/app/components/print/print.component.ts`.
- Related tests: `FrameWeb/tests/io/test_axial_force_input.py::test_compressed_http_preserves_Nd_definition_and_result` passes, but manually sends `json.dumps(list(compressed))`, so it masks the browser mismatch. Other backend compressed-route tests use the same correct bracketed envelope. No focused test covers `AppComponent.post_compress()` or the TypeScript-to-Python transport contract.

### Initial Hypotheses (informed by Codex analysis)
1. Compressed-request transport contract regression: confirmed by byte coercion, exact exception, and direct HTTP reproduction — Codex confidence: high (~99%).
2. Frontend/backend deployment-version skew: may explain why older environments worked, because a pre-`29df328` backend accepted the legacy body — Codex confidence: low for the current local incident.
3. Ct-girder model validation, URL/CORS/authentication, or service availability: eliminated as the immediate cause because the structured HTTP 400 occurs before model parsing and the endpoint is reachable — Codex confidence: high.

### Codex Pattern Recognition
- Error pattern: a type-coercion and wire-contract incompatibility surfaced by replacing an unsafe permissive parser with strict JSON parsing.
- Known similar patterns: backend tests construct the consumer-preferred format instead of exercising the real frontend producer; the printing path contains the same `btoa(pako.gzip(...))` expression but targets a different service.
- Recommended investigation priority: prove the exact Angular-normalized payload succeeds with a canonical accepted envelope; then decide compatibility strategy, add a TypeScript-to-Python contract regression, and audit the printing call independently.
