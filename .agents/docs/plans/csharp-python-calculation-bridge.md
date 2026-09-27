## Implementation Plan: C# to Python Calculation Bridge

### Purpose

Connect the FrameWebforCS calculation command to FrameWeb's `build_analysis_result_set`, using the calculation input semantics of FrameWebforJS, and publish the returned `AnalysisResultSet v1` in the desktop result views. Preserve the proven attach → import → breakpoint bind → call sequence for development debugging without blocking the GUI.

### Scope

- New files: calculation request projection/validation, Python runtime bridge, development debug gate, and focused tests under `FrameWebforCS/` and `FrameWebforCS.Tests/` (exact class/file names chosen during implementation); shared calculation fixtures only if existing fixtures cannot cover parity.
- Modified files: `FrameWebforCS/components/menu/MenuComponent.cs` and, if required, its designer; `FrameWebforCS/providers/InputDataService.cs`, `FrameWebforCS/components/input/InputLoadService.cs`, and related input services; `FrameWebforCS/FrameWebforCS.csproj`; result state/services and views under `FrameWebforCS/components/result/` plus `FrameWebforCS/components/menu/SidebarComponent.cs` and `FrameWebforCS/three/ThreeService.cs` where they consume calculation state; the development debugger launcher derived from `tools/PythonNetDebugProbe/StartMixedDebug.ps1` or an app-specific sibling script. Change `FrameWeb/src/fem/analysis_result_sets.py` only if a demonstrated contract gap requires it.
- Read-only reference: `FrameWebforJS/src/app/providers/input-data.service.ts`, `components/input/input-load/input-load.service.ts`, `providers/result-data.service.ts`, `providers/analysis-result-set.ts`, and `tools/PythonNetDebugProbe/`.
- Dependencies: add a compatible `pythonnet` NuGet reference to the C# app; use the existing uv-managed CPython/runtime and existing Python dependencies. Keep Python input and `AnalysisResultSet v1` output contracts unchanged.
- Excluded: `FrameGConverter/`, print/PDF, authentication, Angular behavior changes, and unrelated C# refactoring. The pre-existing changes to `.agents/docs/DESIGN.md` and `.agents/docs/plans/csharp-dimension-switch.md` belong to other work.

### Implementation Steps

The steps establish request parity and the Python call first, then connect the GUI and verify the complete result path.

#### Step 1: Pin the calculation contract with cross-client fixtures

- [ ] Select small, representative existing model fixtures and capture the *calculation* request by executing Angular's `getInputJson(0)` in a test harness, including required empty sections, selector defaults, 2D completion, prescribed displacement scaling, member-load ranges/relative positions, and moving-load child cases. Record expected case IDs/order and invalid-input behavior; do not hand-author the expected JSON from the implementation under test.
- [ ] Compare these fixtures with `build_analysis_result_set`'s accepted legacy map and `AnalysisResultSet v1` output. For Angular parity, omit calculation-only case names and accept Python's case-ID fallback. Keep the persisted project format separate.
- [ ] Add focused C# fixture tests that fail until the new projection matches the agreed Python input shape; test the Python entry separately with the same fixtures.

**Verification**: Fixture comparisons identify every transformed input field and case order; `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_legacy_cases_api.py -q` confirms the solver contract. Do not treat a save JSON round trip as request parity.

#### Step 2: Build a calculation-only C# input projection

- [ ] Project the current input services into the Python legacy calculation shape, excluding `three`, persisted `result`, and display-only DEFINE/COMBINE/PICKUP payloads. Reuse existing typed input and expansion code only where its semantics match the Angular calculation path.
- [ ] Apply validation before conversion: required nodes, member or shell, nonempty calculated loads, valid references/selectors, shell geometry, and removal of unreferenced nodes. Report actionable errors without mutating the saved document.
- [ ] Reproduce 2D completion and load processing in the same order as Angular: negative node prescribed displacements, member range/relative-distance expansion to `m`, per-case selector defaults, removal of empty cases, and LL child-case sweep/ordering. Bound case and expansion work to the Python limits.

**Verification**: Focused C# tests compare canonical request JSON against Step 1 fixtures for 2D/3D, static/moving load, edge positions, missing selectors, and malformed references. Python accepts the produced requests and returns expected ordered cases.

#### Step 3: Establish the Python embedding lifecycle and failure boundary

- [ ] Add `pythonnet` to `FrameWebforCS.csproj` and introduce one owned Python runtime service that resolves the uv Python DLL/home/source path, initializes once, imports the target module, serializes `Py.GIL()` calls, and converts request/result through safe JSON or equivalent bounded values.
- [ ] Run the blocking FEM call off the UI thread while preserving Python thread/GIL requirements; make startup, import, solver, and serialization errors explicit. Define cancellation as stopping UI publication while a non-interruptible in-process solve completes, unless a safe interruption is demonstrated, and prevent concurrent solves/shutdown races.
- [ ] Validate the returned root as canonical `AnalysisResultSet v1`; do not accept the former `root.result.case` response as a success fallback.

**Verification**: A headless C# integration test invokes a small real FrameWeb fixture twice on a worker thread, reacquires the GIL, asserts case IDs/result schema, and covers idle, cancel-while-solving, failure, and shutdown without a hang. App build and Python tests pass on the supported Windows environment.

#### Step 4: Add the two-stage development debugger handshake

- [ ] Adapt the Probe's sequence into a debug-only, awaitable two-gate handshake: after Python initialization expose gate 1; release it only after Visual Studio attaches Python (native) + Native; import `fem.analysis_result_sets`; expose gate 2; release it only after the breakpoint at the actual `build_analysis_result_set` source line reports `Children > 0`; then permit the calculation call.
- [ ] Give the app-specific launcher a PID-scoped local control channel, explicit stage acknowledgements, timeout/cancel/error handling, and a configurable Python file/line, defaulting to `FrameWeb/src/fem/analysis_result_sets.py:46`. Update both `EnsurePythonBreakpoint` and `PythonBreakpointChildren` checks to match that exact file and line. Launch the app without a pre-attached Managed debugger, resolve its current PID and x64 Python symbols, then attach the required engines. In ordinary runs bypass the handshake; never use `Console.ReadLine()`, fixed sleeps, or F11 as a readiness signal.
- [ ] Keep the form responsive and show a cancelable waiting state through both gates. Dispose the handshake when calculation/document lifetime ends.

**Verification**: On a Debug x64 run, the launcher attaches, observes a bound breakpoint, releases gate 2, and Visual Studio stops on the requested Python line. Cancel/attach failure leaves the GUI usable and does not invoke Python.

#### Step 5: Publish `AnalysisResultSet v1` into C# result views

- [ ] Parse and semantically validate the complete v1 result before one atomic state change. Map ordered cases/states and topology to displacement, reaction, section-force, and 3D result views; preserve case IDs and moving-load grouping. Replace legacy `root.result.case` assumptions at this boundary instead of adding a second success schema.
- [ ] Apply DEFINE/COMBINE/PICKUP and moving-load presentation from canonical base results where current views require them, without altering the Python response.
- [ ] Inventory `InputDataService.GetSaveJson`, `JsonDataOpen`, and dimension result tests before changing result services. Preserve existing project-file save/open behavior for old files until a new calculation succeeds; then clear stale legacy result fields so a later save cannot misrepresent them as the new v1 result. Ensure the v1 runtime state does not leak into legacy `result`/`resultDimension` save fields. Treat the documented input-only persistence migration as separate work.

**Verification**: C# result tests render first/last static cases and moving child cases, reject malformed/incomplete v1 without changing prior state, and check that displayed values match the canonical fixture. Existing project save/open tests remain passing before recalculation. A new test opens an old file with persisted results, recalculates to v1, saves, and confirms stale legacy results were cleared and v1 was not serialized as legacy data. A manual view smoke test shows nonempty displacement/reaction/force and 3D state from a fixture.

#### Step 6: Wire the calculation command to document and result state

- [ ] Connect the calculation menu action to request projection, validation, the Python bridge, and the Step 5 result publisher. Capture an immutable request snapshot and an input edit generation before dispatch; increment the generation on every relevant node/member/load/property/support edit as well as document replacement. Reject duplicate commands and ignore results after an edit, replacement, or cancellation.
- [ ] Display progress and errors on the UI thread; clear or retain prior results according to the existing document/result lifecycle without publishing partial results. Keep the form responsive during preprocessing, both debug gates, and the solver call.

**Verification**: UI/service tests cover success, invalid input, solver failure, duplicate click, cancellation, an input cell edit during the solve, and an old calculation completing after a new/open document transition. A manual smoke test starts from the calculation menu and reaches `build_analysis_result_set` and visible results.

#### Step 7: End-to-end acceptance and focused gates

- [ ] Run a representative 2D static and 3D/moving-load calculation from the GUI and compare case order and selected numerical values with the same Python fixture; verify the Step 5 persistence boundary.
- [ ] Repeat with the development attach script and confirm the Python line breakpoint hits after bind, then continue to visible results. Document the environment setup, control channel, target file/line, and normal-run behavior.
- [ ] Run the changed C# test project/build and relevant FrameWeb pytest cases; inspect `git diff` for scope, generated files, and unrelated user changes.

**Verification**: End-to-end input → Python call → `AnalysisResultSet v1` → UI display passes; debugger run stops on the specified Python line and resumes to the same results; relevant component gates pass.

### Risks & Considerations

- The older `.agents/docs/DESIGN.md` decisions require a private loopback HTTP runtime, while the latest Python debugging decision and this request imply in-process pythonnet. Confirm the architectural replacement before implementation; the plan assumes the latter and does not silently keep both transports.
- Python execution inside the WinForms process can block or destabilize the app if GIL, thread ownership, native dependency resolution, shutdown, or cancellation are mishandled. Prove the lifecycle early with a real fixture; isolate errors and prevent concurrent calls.
- `GetSaveJson` and raw C# `m1/m2` load rows are not solver inputs. Angular's validation, 2D completion, member expansion, and LL sweep carry meaning; fixture parity is required before trusting calculations.
- Existing C# result services consume the old `root.result.case` shape, whereas Python emits `AnalysisResultSet v1`. No result should be published until full validation and view preparation succeed.
- Current C# save/open code persists legacy `result` and `resultDimension`, despite the documented input-only project contract. Preserve old-file behavior in this scoped feature; perform any migration under a separate decision and test plan.
- A Python breakpoint is usable only after the target module is imported and the bound child exists. Visual Studio attach return alone is insufficient. Normal releases must never wait for a debugger.
- `DocumentRevision` currently advances on file open but not ordinary input edits, so it alone cannot prevent stale result publication. The command needs a separate input edit generation or equivalent snapshot identity wired to all relevant edits.
- The current worktree contains unrelated changes to the design document, another plan, and ongoing C# dimension work that touches menu and result files; inspect ownership and rebase the implementation plan against those changes before editing.

### Open Questions

- Confirm that this feature replaces the previously approved private HTTP calculation boundary with in-process pythonnet, as the requested debugging flow implies.
