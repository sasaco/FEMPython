## Feature Brief: AnalysisResultSet-Only Result Architecture

### Current State
- Architecture: `POST /` has two success shapes: a default single `AnalysisResult` and an explicit legacy/display case map. Nonlinear results duplicate the final state at top level and also retain nested `step_results`.
- Relevant files: `FrameWeb/main.py`, `FrameWeb/src/fem/solver_results.py`, `FrameWeb/src/fem/model.py`, `FrameWeb/src/fem/legacy_results.py`, `FrameWeb/src/fem/legacy_beam.py`, `FrameWeb/src/fem/file_io.py`, and FrameWebforJS result providers/workers.
- Patterns: per-case models are isolated; input order is meaningful; generated analysis nodes and split elements are mapped back to public nodes/members during the current legacy projection; frontend combination/pickup processing assumes a case map.

### Feature Goal
Make `AnalysisResultSet` the only public calculation response for single-case, multi-case, nonlinear, and modal analysis. Delete all pre-release compatibility/display contracts and move useful member/topology projection into one canonical domain result model.

### Scope
- Include: one `AnalysisResultSet v1` result root; ordered case/state snapshots; shared canonical topology; canonical node displacement, support reaction, original-member station force, shell, and solid fields; accepted nonlinear steps as independent results; modal result variants; atomic multi-case execution; frontend direct canonical consumption; deletion of legacy/display result code and tests; deletion of the legacy `rate` display multiplier.
- Exclude: calculation-input redesign, compatibility with previous responses, vendor result media types, saved result migration, linear combination of nonlinear/modal snapshots, solver/eigensolver/convergence algorithm changes, and streaming result histories. Canonical modal ordering and normalization are output postprocessing and remain in scope.

### Complexity Classification (from Codex)
- Classification: COMPLEX
- Estimated files: 20-35 product, test, fixture, preset, and documentation files
- Estimated LOC: 900-1800 lines changed or added, with substantial deletion of compatibility code
- Implementation route: `team-execute` implementation and review after design freeze and user approval

### Integration Points
- Solver snapshots: static, nonlinear accepted steps, and modal modes become discriminated `AnalysisResult` variants.
- Domain postprocessing: current generated-node, support, member aggregation, station, and force-sign logic moves out of `legacy_results.py` into canonical topology/result modules.
- Multi-case application service: cases are solved in request order with isolated mutable state, while only JSON-ready snapshots are accumulated.
- Existing-input enumeration: legacy `load` entries become ordered cases using their string keys and metadata while preserving top-level/case/inference analysis precedence; modern `nodes` input remains one synthetic case `"1"`.
- HTTP root: `main.py` returns one `application/json` success contract with no result negotiation.
- Frontend boundary: one strict TypeScript validator/indexer feeds result views and workers directly from canonical fields.
- Input boundary: existing validated calculation schemas remain unchanged by this refactor; their case order drives result order.
- Rate cleanup: delete the current post-solve/display `rate` property without introducing a replacement result or request field.

### Risks
- Member-force convention becomes a supported canonical contract: lock signs, stations, and split-member aggregation with numerical oracles.
- Multiple case topologies could diverge: preprocess deterministically and reject any public-topology mismatch.
- Support definitions may differ by case: keep geometry shared, declare ordered `support_node_ids` on each result case, and validate reactions against that case-specific set.
- Nonlinear histories can be large: retain result snapshots but release each solved model immediately; keep transport compression.
- Existing workers assume legacy maps: refactor them around explicit case/state selectors rather than recreating the deleted shape.
- Modal output differs from force-bearing states: use a discriminated result/state union and prohibit invalid field combinations.
- Element result identity must be exact: serialize member/shell local frames and topology-owned shell/solid sampling locations, then require result coverage by those IDs and order.
- Breaking backend/frontend landing: integrate both sides together and verify with shared fixtures.
- Cutover risk: build both consumers against fixtures first, then switch HTTP/frontend and delete obsolete paths in one integration change.

### Success Criteria
- Every successful calculation response has `kind: "analysis_result_set"`, `schema_version: "1.0"`, ordered `cases`, one shared `topology`, and ordered `results`.
- One static case produces exactly one result; N static cases produce N results in request order.
- A nonlinear case produces one result per accepted step, zero-based `state.index`, and exactly one final marker without nested `step_results` or duplicated final state.
- Multiple nonlinear cases are uniquely and deterministically addressed by `(case_id, state.kind, state.index)` in case-major order.
- Legacy inputs enumerate every `load` entry in insertion order; modern inputs produce exactly one case `"1"`; case-specific support sets are preserved without duplicating topology.
- Modal modes use a typed mode state and do not pretend to have static force fields.
- Modal `frequency` is in inverse declared time units; zero and degeneracy tolerances are fixed by the normative contract.
- Static/load-step/modal validators enforce complete required and prohibited field sets.
- Existing load values are solved as supplied; no result-level or post-solve multiplier exists. DEFINE/COMBINE/PICKUP coefficients remain separate derived-result operations.
- Root units reproduce existing normalized model metadata, including `consistent_user_defined`/`unspecified` when the input omits declarations; no unit inference or conversion is introduced.
- FrameWebforJS displays displacements, reactions, member forces, shell/solid results, cases, and nonlinear steps directly from canonical fields.
- DEFINE/COMBINE/PICKUP works for static results and visibly rejects nonlinear/modal linear combination.
- `FrameResultSet`, `legacy-cases-v1`, UI backend field abbreviations, old result validators, and post-solve `rate` scaling are absent from active product code.
- Backend tests, nonzero Angular tests, production build, .NET build, and Ct/nonlinear browser checks pass.
