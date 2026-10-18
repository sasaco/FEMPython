# FrameWebforJS calculation communication error: Phase-1 context

## Repro Command

Run this from the repository root while the existing calculation service is listening on `127.0.0.1:8080`:

```powershell
node -e "const fs=require('fs'),pako=require('./FrameWebforJS/node_modules/pako'); const d=JSON.parse(fs.readFileSync('FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json','utf8')); for(const k of ['define','combine','pickup','three','result']) delete d[k]; d.uid=''; d.production=false; const body=btoa(pako.gzip(JSON.stringify(d))); fetch('http://127.0.0.1:8080/',{method:'POST',headers:{'Content-Type':'application/json','Content-Encoding':'gzip,base64'},body}).then(async r=>{console.log('HTTP '+r.status);console.log(await r.text());process.exit(r.ok?0:1)}).catch(e=>{console.error(e);process.exit(1)})"
```

Observed on 2026-09-18:

```text
HTTP 400
{"error": "Extra data: line 1 column 3 (char 2)", "error_code": "invalid_input", "error_category": "input", "converged": false}
```

The command exits `1`, so it is directly usable as the failing command passed to `.agents/skills/troubleshoot/repro.py`. It uses the named Ct-girder preset and duplicates the calculation request's wire transform (`pako.gzip` followed by `btoa` on the returned `Uint8Array`). Removing save-only sections approximates `getInputJson(0)`; exact Angular provider normalization is not needed to reproduce this failure because the backend fails while decoding the transport envelope, before it can inspect the model JSON.

The backend itself is reachable: `GET http://127.0.0.1:8080/` returned HTTP 200 and `{"results": "Hello World!"}`. This is not a refused connection or CORS-preflight failure.

## Immediate Cause / Flow

1. The calculation link calls `calcrate()` at `FrameWebforJS/src/app/app.component.html:20`. The Ct preset is fetched as text, parsed, any stored `result` is removed, and the input providers are populated at `FrameWebforJS/src/app/components/preset/preset.component.ts:67-79`.
2. `calcrate()` builds calculation-only JSON with `InputData.getInputJson(0)`, then adds `uid` and `production` at `FrameWebforJS/src/app/app.component.ts:207-226`. `getInputJson(0)` reconstructs node/support/member/element/rigid/joint/shell/notice/fix-member/load data and omits save-only `define`, `combine`, `pickup`, `three`, and `result` at `FrameWebforJS/src/app/providers/input-data.service.ts:199-305`. The preset is version 2.0.7 and dimension 3, so the old-version axis conversion and 2-D expansion are not involved.
3. The current local URL is `http://127.0.0.1:8080/` at `FrameWebforJS/src/environments/environment.ts:3-6`. The Angular `local` build replaces that file with `environment.visualstudio.ts`, which spreads the ignored `environment.local.ts`; its current `calcURL` is also `http://127.0.0.1:8080/` (`FrameWebforJS/angular.json:69-76`, `FrameWebforJS/src/environments/environment.visualstudio.ts:1-7`, `FrameWebforJS/src/environments/environment.local.ts:3-6`).
4. The exact current request is `POST /`, `Content-Type: application/json`, `Content-Encoding: gzip,base64`, text response. Its body is produced at `FrameWebforJS/src/app/app.component.ts:229-247` as `btoa(pako.gzip(JSON.stringify(jsonData)))`.
5. `pako.gzip` returns a `Uint8Array`. Passing that object directly to `btoa` converts it to the comma-separated decimal string `"31,139,..."`, then base64-encodes that string. For example, the local Node runtime evaluates `btoa(new Uint8Array([31,139,8,0]))` as `MzEsMTM5LDgsMA==`. The body is therefore `base64(ASCII("31,139,..."))`, not the JSON-array transport `base64(ASCII("[31,139,...]"))` that the backend expects.
6. Flask routes `POST /` to `FEMPython` at `FrameWeb/main.py:41-48`. Any `Content-Encoding` value makes it call `Compressor.decompress` at `FrameWeb/main.py:91-102`. Decompression base64-decodes and immediately executes `json.loads(b)` at `FrameWeb/main.py:153-160`; the bracketless byte list raises `JSONDecodeError: Extra data: line 1 column 3`. No `FemModel` construction or calculation is reached.
7. The generic exception handler sends the error through `diagnostic_payload` at `FrameWeb/main.py:130-132`. A `JSONDecodeError` is a `ValueError`, so it becomes `invalid_input`, HTTP 400 at `FrameWeb/src/fem/diagnostics.py:52-80`. Angular routes every non-2xx response to its HTTP error callback and displays `message.transmission-error` at `FrameWebforJS/src/app/app.component.ts:319-323`; the Japanese string is defined at `FrameWebforJS/src/assets/i18n/ja.json:435-439`.

## Related Tests

- Focused existing compressed-route test:

  ```powershell
  cd FrameWeb
  uv run --locked --extra dev pytest tests/io/test_axial_force_input.py::test_compressed_http_preserves_Nd_definition_and_result -q
  ```

  Observed: `1 passed in 2.15s`. This test does **not** reproduce the browser defect. It correctly serializes the bytes as `json.dumps(list(compressed))` before base64 at `FrameWeb/tests/io/test_axial_force_input.py:173-181`.
- The same correct array envelope appears at `FrameWeb/tests/integration/test_input_routes.py:83-89`, `FrameWeb/tests/integration/test_spatial_general_plane.py:95-101`, and `FrameWeb/tests/regression/test_slip_support_history.py:35-39`.
- `FrameWeb/tests/io/test_http.py:16-30` covers ordinary JSON POST behavior, not the JavaScript encoding boundary.
- No focused `FrameWebforJS` spec was found for `AppComponent.post_compress`, `calcrate`, or the `message.transmission-error` branch. Consequently, no existing focused test reproduces this frontend/backend wire mismatch; only the direct Node/HTTP command above does.

## Similar Patterns

- `FrameWebforJS/src/app/components/print/print.component.ts:1006-1008` repeats `btoa(pako.gzip(...))`. It targets a different service and is outside this calculation diagnosis, but it is the closest same-code pattern to audit before applying a broad shared transport change.
- All backend compressed-input tests encode a JSON array of integer bytes. The frontend calculation call is the only located calculation producer that omits the brackets. This explains why backend tests are green while the browser call gets HTTP 400.
- The server response compression is conventional raw gzip bytes followed by base64 (`FrameWeb/main.py:163-179`), and the frontend success path correspondingly performs `atob` then `pako.ungzip` (`FrameWebforJS/src/app/app.component.ts:257-268`). The incompatibility is specifically on request encoding.

## Files Read/Written

Read:

- `FrameWebforJS/src/app/app.component.html`
- `FrameWebforJS/src/app/app.component.ts`
- `FrameWebforJS/src/app/components/preset/preset.component.ts`
- `FrameWebforJS/src/app/components/preset/preset.service.ts`
- `FrameWebforJS/src/app/providers/input-data.service.ts`
- `FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json`
- `FrameWebforJS/src/assets/i18n/ja.json`
- `FrameWebforJS/src/environments/environment.ts`
- `FrameWebforJS/src/environments/environment.local.ts`
- `FrameWebforJS/src/environments/environment.visualstudio.ts`
- `FrameWebforJS/angular.json`
- `FrameWeb/main.py`
- `FrameWeb/src/fem/diagnostics.py`
- `FrameWeb/tests/io/test_axial_force_input.py`
- `FrameWeb/tests/io/test_http.py`
- `FrameWeb/tests/integration/test_input_routes.py`
- `FrameWeb/tests/integration/test_spatial_general_plane.py`
- `FrameWeb/tests/regression/test_slip_support_history.py`
- `scripts/smoke-local.py`

Written:

- `.agents/docs/research/troubleshoot-framewebforjs-calculation-communication-error-context.md` (this Phase-1 analysis only)

No product source, test, preset, environment, dependency, or running process was changed.
