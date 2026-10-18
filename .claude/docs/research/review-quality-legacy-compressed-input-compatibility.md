# Code Quality Review: Legacy Compressed Input Compatibility

## Recommendation

**PASS** — no open High, Medium, or Low findings. The implementation is small,
keeps the old and canonical transports behind one validation boundary, and
preserves the existing response contract. The previously reported Medium
exception-normalization edge is fixed and covered by focused regression tests.

## Review Scope

- `FrameWeb/main.py`
- `FrameWeb/tests/io/test_compressed_transport.py`
- `FrameWeb/tests/data/transport/legacy-browser-envelope.json`
- `FrameWeb/docs/wiki/endpoints.md`

Focus: correctness, clarity, maintainability, type and error boundaries,
minimality, legacy-contract preservation, documentation accuracy, and the
JSON-to-CSV fallback boundary.

## Open Findings

None.

## Resolved Finding

### [Resolved Medium] Python integer-digit-limit errors are normalized at both JSON stages

- **File**: `FrameWeb/main.py:171`, `FrameWeb/main.py:189`
- **Original issue**: Python 3.13 raises `ValueError`, rather than
  `JSONDecodeError`, when `json.loads()` encounters an integer longer than the
  configured 4,300-digit limit. Direct `Compressor.decompress()` calls leaked
  that exception.
- **Fix inspected**: the outer parser handles `JSONDecodeError` first and uses
  only that branch for legacy CSV. Its later `ValueError` handler raises
  `InputValidationError("Invalid compressed byte sequence JSON")` directly.
  The inner parser normalizes both JSON exception types to
  `InputValidationError("Invalid compressed input JSON")`.
- **Regression coverage**: one outer-boundary test proves that this case does
  not enter the CSV branch, and the inner-boundary test runs against both the
  canonical JSON-array and legacy CSV envelopes.
- **Status**: resolved; focused tests and an independent direct probe confirm
  the expected exception type and stage-specific messages.

## Positive Observations

- The legacy parser is reached only from the outer `json.JSONDecodeError`
  handler. A JSON value that parses successfully but has the wrong container or
  element type is rejected by the common validator and is not reinterpreted as
  CSV.
- CSV validation is intentionally narrow: ASCII only, non-empty 1–3 digit
  tokens, decimal conversion only after lexical validation, exact `int` type,
  and byte range `0..255`. There is no executable-input parser.
- `decompress()` separates Base64, outer envelope, gzip, UTF-8, and inner JSON
  stages, while unexpected decompressor defects remain visible as 500 errors.
- The Node/pako fixture is consumed directly for the legacy branch, and the
  four header/envelope combinations compare against the unchanged plain-JSON
  route.
- The endpoint documentation accurately describes the canonical and legacy
  envelopes, the request/response asymmetry, and the non-`eval` safety boundary.

## Verification

- `uv run --locked --extra dev pytest tests/io/test_compressed_transport.py -q`
  from `FrameWeb`: **110 passed** on independent post-fix re-run.
- Strict-TDD evidence supplied with the fix: **RED 3 failed / 107 passed**,
  **GREEN 110 passed**, and the related regression selection **185 passed**.
- `uv tool run ruff check FrameWeb/tests/io/test_compressed_transport.py`:
  **passed**.
- `uv tool run ruff format --check FrameWeb/tests/io/test_compressed_transport.py`:
  **passed**.
- `git diff --check` for the four reviewed product files: **passed** (Git emitted
  only the existing LF-to-CRLF working-copy warnings).
- `eval(` / `literal_eval` search in the implementation and focused tests:
  **no matches**.
- Independent post-fix boundary probe: both outer and inner 5,000-digit JSON
  integers now raise `InputValidationError` with their respective stage-specific
  messages.

Exact reproduction from `FrameWeb`:

```powershell
@'
import base64, gzip, json
from main import Compressor, app

digits = "9" * 5000
outer = base64.b64encode(f"[{digits}]".encode("ascii"))
inner_gzip = gzip.compress(f'{{"x":{digits}}}'.encode("ascii"), mtime=0)
inner = base64.b64encode(json.dumps(list(inner_gzip)).encode("ascii"))

for name, body in {"outer": outer, "inner": inner}.items():
    try:
        Compressor.decompress(body)
    except Exception as exc:
        print(name, type(exc).__name__)
    response = app.test_client().post(
        "/", data=body, headers={"Content-Encoding": "gzip"}
    )
    print(name, response.status_code, response.get_json()["error_code"])
'@ | uv run --locked --extra dev python -
```

Observed: `outer InputValidationError Invalid compressed byte sequence JSON`
and `inner InputValidationError Invalid compressed input JSON`.

## Codex Consultation

Codex CLI completed successfully in read-only mode (97.634 seconds) and returned
`PASS`, confirming that the CSV fallback is limited to `json.JSONDecodeError`.
The independent review subsequently found the integer-limit edge; that finding
is now resolved by the narrow post-fallback `ValueError` handler and inner JSON
normalization described above. Evidence:
`.agents/logs/codex/20260918T041227Z-quality-review-legacy-compressed-input.md`.
