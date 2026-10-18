# Test Review: Legacy Compressed Input Compatibility

## Recommendation

**PASS.** No Critical, High, Medium, or Low test-coverage gaps were found in the approved transport-compatibility scope.

## Review Scope

- `FrameWeb/main.py`
- `FrameWeb/tests/io/test_compressed_transport.py`
- `FrameWeb/tests/data/transport/legacy-browser-envelope.json`
- `FrameWeb/docs/wiki/endpoints.md`
- Approved acceptance criteria in `.agents/docs/plans/legacy-compressed-input-compatibility.md`

Measured focused coverage is **84% for `main.py`** (118 statements, 19 missed), above the project target of 80%. The terminal missing-lines report did not identify an uncovered line in the newly added `Compressor.decompress` or `_parse_byte_sequence` logic.

## Coverage of Required Behavior

| Requirement | Evidence | Result |
|---|---|---|
| Actual browser producer provenance | Regenerating the fixture with Node `v24.13.0`, pako `2.2.0`, and `btoa(pako.gzip(JSON.stringify(payload)))` produced an exact byte-for-byte match; decoded prefix is `31,139,8,0,...`. | Pass |
| Both headers and both envelopes | `test_http_accepts_both_envelopes` covers the 2 × 2 matrix: `gzip` / `gzip,base64` and JSON byte array / legacy CSV. The legacy `gzip,base64` case consumes the saved Node/pako body directly. | Pass |
| Equivalent successful responses | Every compressed matrix entry is decoded and compared with the plain-JSON response, then checks analysis type, displacement, reaction, and stress output. | Pass |
| Existing plain JSON contract | Header omitted and explicit `Content-Encoding: json` both receive HTTP 200 with the expected result. | Pass |
| Canonical JSON byte validation | Non-list values, booleans, floats, strings, nested values, negatives, and values above 255 are rejected; exact integer typing is exercised. | Pass |
| Legacy CSV lexical boundaries | Empty input/tokens, leading/trailing/double commas, whitespace, signs, decimals, exponents, non-ASCII text, four-digit/huge tokens, and out-of-range values are rejected. Leading zeroes are explicitly accepted. | Pass |
| Transport and payload failures | Invalid Base64 alphabet/padding, non-gzip data, truncated/checksum/deflate damage, invalid UTF-8, invalid inner JSON, and valid non-object JSON values all produce `InputValidationError`; HTTP assertions require 400 `invalid_input`. | Pass |
| Python integer digit limits | A 5,000-digit outer JSON integer is classified as invalid outer JSON without entering CSV fallback. A 5,000-digit inner JSON integer is classified as invalid compressed JSON for both wire formats. Direct and HTTP assertions cover all three cases. | Pass |
| Code-shaped input safety | `1+1` and `__import__(...)` are rejected, and a monkeypatched `os.getcwd` proves the code-shaped payload is not invoked. | Pass |
| Unexpected defect classification | A monkeypatched unexpected `RuntimeError` escapes the decoder and remains HTTP 500 `analysis_failure`, rather than being hidden as a 400 input error. | Pass |
| Test independence and mocking | Tests use fresh Flask clients and immutable fixture data; `monkeypatch` restores global functions after each test. No network or external service is required. | Pass |

## Test Execution Results

- Final focused suite and measured coverage: **110 passed, 0 failed in 0.83 s; `main.py` 84%**.
- Final related regression set: **185 passed, 0 failed in 18.55 s** across compressed transport, input routes, spatial/general-plane, axial-force input, slip-support history, and HTTP tests.
- Node/pako fixture regeneration: exit 0, exact match confirmed with the versions recorded in the fixture.

## Findings

None. The earlier Quality Review observation concerning `json.loads()` raising `ValueError` at Python's integer-string digit limit is resolved and covered at both outer and inner JSON stages.

The approved plan explicitly excludes compressed/decompressed size limits and the Ct result-schema/multiple-load-case contract. Those remain separate product risks, not missing coverage for this transport parser change.
