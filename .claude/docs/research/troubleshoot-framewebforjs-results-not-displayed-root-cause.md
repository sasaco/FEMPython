# Root-cause analysis: FrameWebforJS results not displayed

## Definitive defect, location, and trigger

- **Definitive defect:** the production boundary serializes one modern, flat `FemModel.run()` result, while the unchanged browser consumes an outer load-case map whose values contain `disg`, `reac`, and `fsec`. The producer is `FrameWeb/main.py:106-118`; the consumer forwards the decoded object unchanged at `FrameWebforJS/src/app/providers/result-data.service.ts:87-95`; the three workers silently skip every top-level entry without their expected legacy field at `FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-40`, `FrameWebforJS/src/app/components/result/result-reac/result-reac1.worker.ts:27-41`, and `FrameWebforJS/src/app/components/result/result-fsec/result-fsec1.worker.ts:41-70`.
- **Why the symptom is “calculated but blank”:** each worker posts an empty result with `error === null` (`result-disg1.worker.ts:104-114`, `result-reac1.worker.ts:94-103`, `result-fsec1.worker.ts:243-253`), and the displacement service subsequently marks its work calculated (`FrameWebforJS/src/app/components/result/result-disg/result-disg.service.ts:145-184,239-245`). This is a silent response-contract failure, not a calculation or transport failure.
- **Trigger:** every successful legacy-JSON request sent by this frontend triggers the mismatch because the server returns flat keys such as `node_displacements`, `reaction_forces`, and `element_stresses`, while the frontend iterates those keys as though each were a load case (`FrameWeb/main.py:106-118`; `FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-40`). The direct HTTP reproduction confirms HTTP 200 with zero compatible cases at `.agents/logs/repro-framewebforjs-results-not-displayed.py:25-49`.
- **A shape-only wrapper is insufficient:** legacy parsing calls `select_case(data)` before model construction (`FrameWeb/src/fem/file_io.py:70-79`), and that helper defaults to `next(iter(data['load']))` then replaces `load` with one case (`FrameWeb/src/fem/legacy_beam.py:6-20`). The Ct preset has ordered load IDs `1` through `11`, all non-empty and all rate `1` (`FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json:1`), so the current solve contains only case `1`.

## Execution-flow comparison

1. The old endpoint constructs `Controller` and serializes `controller.results` unchanged (`C:/Users/sasai/Documents/FrameWeb2/main.py:63-79`). `Controller` solves every `inputData.loadCases` entry and assigns `results[id]` in that same iteration (`C:/Users/sasai/Documents/FrameWeb2/app/controller.py:63-102`).
2. Old `InputData` creates cases in input `load` key order and later removes only cases with no effective node/member/thermal/prescribed-displacement load (`C:/Users/sasai/Documents/FrameWeb2/app/inputData.py:250-278,389-406`). Thus the old wire order and IDs are the surviving input case order.
3. The current endpoint calls `_read_json_model`, constructs one `FemModel`, runs once, and JSON-normalizes that flat result (`FrameWeb/main.py:106-118`). `result_to_jsonable` converts types and stringifies keys only; it adds no case hierarchy or legacy names (`FrameWeb/src/fem/file_io.py:817-839`).
4. On success the browser decodes the result, forwards it to `loadResultData`, and sets a global calculated flag immediately (`FrameWebforJS/src/app/app.component.ts:249-289`). The workers then apply the incompatible case-map assumptions described above.

## Hypotheses evaluated

### Confirmed — response schema mismatch

The old per-case value is explicitly `{disg, reac, fsec, shell_fsec, size}` (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:135-180`), whereas the current documented HTTP contract explicitly says it returns one case and does **not** return `case.disg`/`case.fsec` (`FrameWeb/docs/wiki/results.md:35-43`). This directly explains the blank output.

### Confirmed — first-case selection loses cases 2–11

`select_case` deep-copies the input, chooses the first key unless an ID is supplied, and narrows referenced `element`, `fix_node`, `joint`, and `fix_member` cases (`FrameWeb/src/fem/legacy_beam.py:6-20`). Therefore wrapping the current flat response as `{"1": ...}` would display only one basic case and make the Ct define/combine/pickup calculations incomplete; the frontend builds those later stages from the received load keys (`FrameWebforJS/src/app/providers/result-data.service.ts:97-159`).

### Eliminated as primary cause — asynchronous readiness timing

The global `ResultData.isCalculated` is set before workers finish (`FrameWebforJS/src/app/app.component.ts:285-289`), but navigation is gated by the displacement service's separate flag (`FrameWebforJS/src/app/app.component.ts:334-376`), which is set only after worker processing (`result-disg.service.ts:239-245`). Waiting cannot add fields that every worker has already skipped, so timing can confuse status but cannot cause or repair this deterministic empty result.

## Exact `legacy-cases-v1` projection semantics

The compatibility boundary must reproduce the old wire contract, not merely rename three modern keys.

- **Outer map:** solve every surviving legacy `load` case independently, preserving its string ID and input insertion order (`C:/Users/sasai/Documents/FrameWeb2/app/inputData.py:250-274,389-406`; `C:/Users/sasai/Documents/FrameWeb2/app/controller.py:90-102`). For each case, select its referenced `element`, `fix_node`, `joint`, and `fix_member` definitions (`FrameWeb/src/fem/legacy_beam.py:13-20`). Preserve `_all_member_loads` across selections so all cases use stable subdivision coordinates (`FrameWeb/src/fem/legacy_beam.py:10-14,66-76`).
- **Per-case keys:** return exactly `disg`, `reac`, `fsec`, `shell_fsec`, and `size` for old-client compatibility (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:135-180`). `size` is the total solver-node count including subdivision nodes (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:174-180`).
- **Case `rate`:** parse invalid/missing rate as `1.0`, and multiply every displacement, reaction, beam-force, and shell-force output component after solving (`C:/Users/sasai/Documents/FrameWeb2/app/inputData.py:272-274`; `C:/Users/sasai/Documents/FrameWeb2/app/controller.py:91-95`; `C:/Users/sasai/Documents/FrameWeb2/app/result.py:165-179,205-212,284-291,317-366`). Do not scale the input loads as a substitute; that is not equivalent for nonlinear analysis.
- **`disg`:** keys are original numeric node labels plus generated `{member}n{ordinal}` notice/rigid points and active-case `{member}l{ordinal}` load points (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:197-253`; `FrameWeb/src/fem/legacy_beam.py:148-174`). Values contain `dx,dy,dz,rx,ry,rz`, multiplied only by `rate` (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:205-212,243-251`). For legacy FrameWeb input the wire values are metres and radians: the old solver defines them as m/rad (`C:/Users/sasai/Documents/FrameWeb2/frame_analysis/frame_calculation.py:15-24`), and the browser converts both translations and rotations by `×1000` to mm/mrad (`FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:59-79`). The contradictory mm/mrad prose in `C:/Users/sasai/Documents/FrameWeb2/app/result.py:11-24` must not drive another conversion.
- **`reac`:** map modern `fx,fy,fz,mx,my,mz` to legacy `tx,ty,tz,mx,my,mz` without sign inversion, then apply `rate`; units are kN and kNm in the legacy convention (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:33-52,269-293`). Emit support nodes in ascending numeric order, six keys per node, zero-filling absent components; in 2D omit auxiliary supports and force `tz,mx,my` to zero (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:269-293`). The frontend deliberately displays the returned sign unchanged (`FrameWebforJS/src/app/components/result/result-reac/result-reac1.worker.ts:51-68`).
- **Modern end-force source:** `element_stresses[element_id].i_end/j_end` is the local nodal resisting-force order `[N,Vy,Vz,T,My,Mz]`, with both ends expressed in the same right-handed local basis (`FrameWeb/src/fem/elements/bar_element.py:125-128`).
- **`fsec` grouping/order:** sort original members by numeric member ID, traverse each member from its input i-node to j-node, and emit `P1..Pn` for intervals bounded by original ends, notice points, and rigid-zone boundaries; load-only subdivisions inside an interval do not create another reported `P` (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:309-367`). Use the first contained modern element's `i_end`, the last contained element's `j_end`, and interval length `L` in metres; the repository's comparison view implements that exact grouping at `FrameWeb/tests/support/section_cut_view.py:17-77`.
- **`fsec` keys/signs/units:** for `i=[N,Vy,Vz,T,My,Mz]` and `j` in the same order, emit `fxi=-i0`, `fyi=+i1`, `fzi=+i2`, `mxi=-i3`, `myi=-i4`, `mzi=+i5`; `fxj=+j0`, `fyj=-j1`, `fzj=-j2`, `mxj=+j3`, `myj=+j4`, `mzj=-j5`; multiply all by `rate` and append `L` without rate. The old implementation is `C:/Users/sasai/Documents/FrameWeb2/app/result.py:317-366`, and the same sign vector is independently encoded at `FrameWeb/tests/support/section_cut_view.py:68-76`. Force components are kN, moments kNm, and `L` metres (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:55-93`).
- **Do not promote the test helper unchanged:** it does not apply `rate`, returns `shell_results` instead of the old `shell_fsec`, and adds a synthetic 2D reaction key `"0"` (`FrameWeb/tests/support/section_cut_view.py:78-107`). Its member/beam projection is a strong tested basis, but production code needs an explicit legacy contract implementation.

## Fix-boundary comparison

### Approach A — replace the current `/` success schema

**Pros:** no frontend request change; the existing workers immediately receive their old shape (`FrameWebforJS/src/app/providers/result-data.service.ts:87-95`). **Cons:** breaks the documented modern one-case HTTP contract and consumers/tests that directly access `node_displacements`, `reaction_forces`, and `element_stresses` (`FrameWeb/docs/wiki/results.md:35-43,78-91`; `FrameWeb/tests/io/test_http.py:18-34`). It also discards modern metadata and optional fields unless two incompatible models are mixed into one unversioned payload. **Verdict:** reject.

### Approach B — adapt the frontend to the modern flat schema

**Pros:** preserves the modern backend response (`FrameWeb/docs/wiki/results.md:35-43`). **Cons:** the response still contains only the first legacy case because selection happens before solving (`FrameWeb/src/fem/file_io.py:70-79`; `FrameWeb/src/fem/legacy_beam.py:6-20`). Correct adaptation would require 11 case-specific requests or a new multi-case backend anyway, plus duplicating node-label, member-segment, and section-force sign rules currently derived from server-side mesh topology (`FrameWeb/src/fem/legacy_beam.py:37-76,148-209`; `FrameWeb/tests/support/section_cut_view.py:13-77`). **Verdict:** reject as the primary fix; add frontend schema rejection only as hardening.

### Approach C — explicit/versioned compatibility adapter

Keep the current flat result as the default contract, and let FrameWebforJS explicitly request `legacy-cases-v1` through a versioned endpoint or negotiated response media type. That adapter performs per-case selection/solve/projection and returns the old case map; unrelated modern clients retain the current response (`FrameWeb/docs/wiki/results.md:35-43`). The existing CORS contract already permits the standard `Accept` header if media-type negotiation is chosen (`FrameWeb/main.py:70-85`). **Pros:** correct ownership of mesh-derived conversion, complete 11-case behavior, backward compatibility, independently testable contract. **Cons:** repeats model construction/solve per case and requires one small frontend request change plus a maintained versioned DTO. **Verdict:** recommended.

## Recommendation and required acceptance boundary

Implement **Approach C** as a production adapter separate from `result_to_jsonable`; reuse/refactor the geometry/sign logic proven by `section_cut_result_view`, but correct its rate, 2D reaction, and `shell_fsec` differences (`FrameWeb/tests/support/section_cut_view.py:6-107`). FrameWebforJS should explicitly request this version and fail visibly when the returned outer map has no valid `disg/reac/fsec` case, instead of treating empty workers as success (`result-disg1.worker.ts:29-40,104-114`).

The minimum proof is: Ct response keys are exactly `1..11` in order; each case contains non-empty `disg/reac/fsec`; selected numeric values and signs match an old-backend oracle; define/combine/pickup consumers receive all base IDs; and the current unversioned flat HTTP tests remain unchanged (`FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json:1`; `FrameWebforJS/src/app/providers/result-data.service.ts:97-159`; `FrameWeb/tests/io/test_http.py:18-34`).

## Remaining unknowns

1. **General shell compatibility:** Ct contains no shell data (`FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json:1`), while the old contract requires `shell_fsec` edge forces and the current helper exposes a different `shell_results` value (`C:/Users/sasai/Documents/FrameWeb2/app/result.py:370-409`; `FrameWeb/tests/support/section_cut_view.py:98-104`). Shell semantics need a separate oracle before claiming `legacy-cases-v1` supports shell models.
2. **Operational cost/error policy for multi-case solves:** old behavior aborted the whole request when any case failed (`C:/Users/sasai/Documents/FrameWeb2/app/controller.py:63-102`), but current diagnostics are structured HTTP errors (`FrameWeb/main.py:120-134`). The adapter should retain all-or-nothing behavior unless a versioned partial-result policy is explicitly designed, and Ct's 11 sequential solves need a measured timeout/memory budget.

## Codex status

One bounded read-only low-effort consultation was attempted with `.agents/logs/codex/prompt-troubleshoot-results-display-root-cause.md`; it timed out without a response. The empty response artifact is `.agents/logs/codex/20260918T053932Z-troubleshoot-results-display-root-cause.md`. Per the lead instruction and the earlier session timeouts, Codex is unavailable and was not retried; no Codex output is used as evidence.
