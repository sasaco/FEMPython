# Security Review: Legacy Compressed Input Compatibility

## Recommendation

**PASS** — no open Critical, High, or Medium findings. The compatibility path
does not restore executable parsing: it strictly validates Base64, ASCII decimal
bytes, gzip, UTF-8, and a top-level JSON object before analysis. One Low,
pre-existing availability-hardening gap is recorded below.

## Review Scope

- `FrameWeb/main.py`
- `FrameWeb/tests/io/test_compressed_transport.py`
- `FrameWeb/tests/data/transport/legacy-browser-envelope.json`
- `FrameWeb/docs/wiki/endpoints.md`

Focus: strict Base64 handling, canonical JSON-array versus legacy decimal-CSV
selection, byte type/range validation, non-execution guarantees, gzip/UTF-8/JSON
error boundaries, unexpected-exception classification, `Content-Encoding`
routing, resource exhaustion, and public error/header leakage.

## Open Findings

### [Low] Compressed request and decompressed JSON sizes remain unbounded

- **File**: `FrameWeb/main.py:156`, `FrameWeb/main.py:162`
- **Issue**: neither the Flask application nor `Compressor.decompress()` sets an
  application-level request-size or decompressed-output limit. An oversized
  encoded byte list or high-ratio gzip member can therefore consume substantial
  memory before model validation.
- **Scope assessment**: this is not introduced by the legacy CSV fallback. The
  prior canonical JSON-array path already accepted unbounded request bodies and
  called unbounded `gzip.decompress()`. The new CSV path preserves that exposure
  and adds comparable linear token/list allocation. The analysis endpoint itself
  can also be computationally expensive for valid large models. Severity is Low
  for this compatibility patch, but should be reassessed as Medium if the service
  is internet-facing without gateway/body-size, timeout, and memory controls.
- **Recommended follow-up**: define a deployment-informed maximum request size,
  enforce it before Base64 decoding, and use bounded/streaming gzip expansion
  with an explicit maximum decompressed JSON size. Test limit-1, limit, limit+1,
  and a high-compression-ratio payload in a separate hardening change.

## Resolved Finding

### [Resolved Low] Python integer digit-limit errors are normalized without CSV fallback

- **File**: `FrameWeb/main.py:172`, `FrameWeb/main.py:201`
- Python can raise `ValueError`, rather than `JSONDecodeError`, for an overlong
  integer token. The current implementation maps that case to
  `InputValidationError` at both outer and inner JSON boundaries. Critically,
  only `JSONDecodeError` enters the legacy CSV branch; the digit-limit
  `ValueError` is rejected directly. Focused tests cover both envelope formats.

## Security Properties Verified

- `base64.b64decode(..., validate=True)` rejects non-alphabet bytes and malformed
  padding before envelope parsing.
- Canonical input must be a JSON list whose members satisfy
  `type(value) is int` and `0 <= value <= 255`; booleans, floats, strings,
  nested values, negative values, and values above 255 are rejected.
- Legacy fallback is reached only after outer JSON syntax failure. Tokens must be
  non-empty, 1–3 ASCII decimal digits, and the common byte-range validator still
  applies. Spaces, signs, decimals, exponents, Unicode digits, and Python-shaped
  expressions are rejected.
- No `eval`, `literal_eval`, dynamic import, command execution, or filesystem
  operation is used to interpret the request. The code-shaped-input regression
  test verifies that attacker text is not executed.
- Known Base64, gzip, UTF-8, and JSON failures become stable 400
  `invalid_input` responses. Unexpected decompressor defects remain 500
  `analysis_failure` and the shared diagnostic layer replaces their text with a
  generic public message.
- Both documented `Content-Encoding: gzip` and the deployed
  `Content-Encoding: gzip,base64` route through the same validation boundary.
  Plain JSON behavior remains covered separately.
- Error responses do not echo request bytes, exception causes, stack traces, or
  secrets. The changed files contain no credentials or sensitive fixture data.

## Verification Evidence

- Focused compressed-transport suite: **110 passed**.
- Related regression selection: **185 passed**.
- Measured `FrameWeb/main.py` coverage: **84%**.
- Node v24.13.0 / pako 2.2.0 fixture matches the real legacy producer envelope.
- Search for `eval(` and `literal_eval` in the implementation and focused tests:
  **no matches**.
- The focused tests exercise four header/envelope combinations, exact byte
  boundaries, malformed Base64/gzip/UTF-8/JSON, expression-shaped input, generic
  500 sanitization, and the Python integer digit-limit boundary.

