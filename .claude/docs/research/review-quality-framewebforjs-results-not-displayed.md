# Quality Review — FrameWebforJS legacy-cases-v1 result display fix

## Verdict

**PASS.** No open correctness or maintainability findings were identified in the
seven-file review scope. In particular, there is no HIGH or CRITICAL issue that
blocks completion.

The implementation matches the confirmed diagnosis: the default FrameWeb HTTP
representation remains flat, while an exact opt-in media type returns an ordered
legacy case map; FrameWebforJS requests that representation and rejects a flat,
empty, incomplete, extra-case, or reordered response before result workers run.

## Review Scope

- `FrameWeb/main.py`
- `FrameWeb/src/fem/legacy_results.py`
- `FrameWeb/tests/io/test_legacy_cases_api.py`
- `FrameWeb/docs/wiki/endpoints.md`
- `FrameWebforJS/src/app/app.component.ts`
- `FrameWebforJS/src/app/providers/result-data.service.ts`
- `FrameWebforJS/src/app/providers/result-data.service.spec.ts`

Unrelated dirty-tree changes were intentionally ignored. The historical
projection oracle used for comparison was `FrameWeb2/app/result.py:135-367`.

## Findings

None.

## Compatibility and Correctness Review

- `FrameWeb/main.py:106-125,160-176` gates the compatibility response on the
  exact `application/vnd.frameweb.legacy-cases-v1+json` media type, rejects an
  unknown legacy-cases version with 406, and leaves the pre-existing flat solve
  path as the default.
- `FrameWeb/src/fem/legacy_results.py:33-49` iterates the original `load` mapping
  without sorting, creates a fresh `FemModel` per case, and does not expose a
  partially constructed case map if a later case fails.
- `FrameWeb/src/fem/legacy_results.py:95-100` emits the five historical fields
  (`disg`, `reac`, `fsec`, `shell_fsec`, `size`) in the old order.
- Displacement ordering and labels at `legacy_results.py:110-130` reproduce the
  old original-node-first, then `n`, then `l` convention from
  `FrameWeb2/app/result.py:197-252`. Values receive `rate` exactly once.
- Reaction projection at `legacy_results.py:140-156` preserves the historical
  six-key shape and `fx/fy/fz -> tx/ty/tz` mapping, excludes auxiliary 2D
  restraints, zero-fills absent directions, and forces `tz/mx/my` to zero in
  2D, consistent with `FrameWeb2/app/result.py:269-293`.
- Member-force grouping at `legacy_results.py:160-186,256-278` reconstructs
  `P1..Pn` over notice/rigid boundaries and uses the same i/j sign mapping as
  `FrameWeb2/app/result.py:309-366`. `rate` is applied to force/moment
  components, not to `L`; `size` remains the mesh-node count.
- The beam-only restriction at `legacy_results.py:53-65` is explicit and agrees
  with the recorded design decision; shell/solid input is rejected instead of
  being mislabeled as compatible.
- `FrameWebforJS/src/app/app.component.ts:233-326` derives the expected cases
  from the exact request payload, sends the versioned `Accept`, validates before
  `InputData.getResult` or worker dispatch, and keeps `isCalculated` false on a
  validation failure.
- `FrameWebforJS/src/app/providers/result-data.service.ts:49-80,144-153` also
  validates at the service boundary, so non-HTTP result-loading paths cannot
  silently treat the current flat response or an empty top-level map as a
  successful calculation.
- The API documentation accurately describes representation negotiation,
  beam-only limits, per-case isolation, projection units/signs, atomic failure,
  and independence from the compressed transport envelope.

## Maintainability Review

The compatibility projection is isolated in one typed module rather than mixed
into the default solver or frontend workers. Helpers have narrow
responsibilities, conversion constants are named, and error context preserves
the failing case ID when the diagnostic type supports structured details. The
frontend validator is a pure exported function with focused tests and a single
stable user-facing validation exception.

## Verification

- `uv run --project FrameWeb --locked --extra dev pytest FrameWeb/tests/io/test_legacy_cases_api.py -q`
  - PASS: 8 tests.
- `npx tsc -p tsconfig.app.json --noEmit`
  - PASS.
- `npx tsc -p tsconfig.spec.json --noEmit`
  - PASS.
- `git diff --check -- <seven reviewed files>`
  - PASS; only Git line-ending conversion warnings were emitted.
- Parent-provided broader evidence:
  - backend focused/default/transport suite: 139 passed;
  - frontend application and spec TypeScript compilation: passed.

## Residual Release Check

An actual browser run of the Angular-normalized Ct preset remains valuable as
an end-to-end acceptance check for visible case 1 and case 11 results and for
worker/console cleanliness. This is a test-environment gap, not a discovered
code defect, and it does not change the PASS verdict above.

The prior Codex CLI consultation produced no result before timeout. This review
does not treat that attempt as evidence and did not block on it.
