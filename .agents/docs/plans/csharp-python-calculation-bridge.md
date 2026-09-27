## Implementation Plan: C# to Python Calculation Bridge

### Purpose

Connect the current `FrameWebforCS/` calculation command to FrameWeb's `build_analysis_result_set` through in-process pythonnet. Match `FrameWebforJS` calculation input, show the returned `AnalysisResultSet v1` in desktop result views, and preserve the proven attach → import → breakpoint bind → call sequence for development debugging without blocking the GUI.

### Scope

- New files: one calculation-only request projection, one owned pythonnet runtime service, one development debug gate, one canonical result state/presentation path, and focused tests under `FrameWebforCS/` and `FrameWebforCS.Tests/`. Prefer existing services and controls to a second application layer.
- Modified files: `FrameWebforCS/components/menu/MenuComponent.cs` and its designer as needed; `FrameWebforCS/providers/InputDataService.cs`, `FrameWebforCS/components/input/InputLoadService.cs`, and related input services; `FrameWebforCS/FrameWebforCS.csproj`; result services/views under `FrameWebforCS/components/result/`; and `FrameWebforCS/three/ThreeResultsService.cs` and other calculation-state consumers. Adapt `tools/PythonNetDebugProbe/StartMixedDebug.ps1` through an app-specific launcher.
- Read-only reference: the calculation and result paths under `FrameWebforJS/src/app/`, `FrameWebforCS.v1/Core/Analysis/`, `FrameWeb/src/fem/analysis_result_sets.py`, and `tools/PythonNetDebugProbe/`. `FrameWebforCS.v1/` is a separate .NET 8 application, not a dependency of the current .NET 10 app.
- Dependencies: add a compatible `pythonnet` NuGet reference to the C# app; use the existing uv-managed CPython/runtime and existing Python dependencies. Keep Python input and `AnalysisResultSet v1` output contracts unchanged.
- Excluded: HTTP calculation transport for this C# app, `FrameGConverter/`, print/PDF, authentication, Angular changes, unrelated C# refactoring, input-only project-file migration, and new bridge-specific capacity limits. Python's existing request/result validation still applies.

### Implementation Steps

The steps establish request parity and the Python call first, then connect the GUI and verify the complete result path.

#### Step 1: Pin the calculation contract with cross-client fixtures

- [ ] Capture the *calculation* request by running Angular's `getInputJson(0)` against small fixtures. Cover required empty sections, selector defaults, 2D completion, negative node IDs and prescribed-displacement `/1000` scaling, `m1/m2` ranges and negative IDs, relative `L1/L2`, empty cases, LL parent/child IDs and order, and cases with different selectors and member-load positions. Record invalid-input behavior; do not hand-author expected JSON from the C# implementation.
- [ ] Compare the request with `build_analysis_result_set`'s accepted legacy map and v1 output. Pin case/state order, topology, units, and selected numbers for static, nonlinear, modal, and moving-load fixtures where supported. For Angular parity, omit calculation-only case names and accept Python's case-ID fallback. Keep saved project JSON separate.
- [ ] Inventory reusable request/validator ideas in `FrameWebforCS.v1/` without bringing its `ProjectDocument` or HTTP client into the current application.
- [ ] Add focused C# fixture tests that fail until the new projection matches the agreed Python input shape; test the Python entry separately with the same fixtures.

**Verification**: Golden calculation JSON from Angular, expected v1 output, and invalid cases are available to focused tests. `uv --directory FrameWeb run --locked --extra dev python -m pytest tests/io/test_legacy_cases_api.py -q` confirms the solver contract. A save JSON round trip does not count as request parity.

#### Step 2: Build a calculation-only C# input projection

- [ ] Snapshot current input services into the Python legacy calculation shape, excluding `three`, persisted `result`, and display-only DEFINE/COMBINE/PICKUP payloads. Follow Angular's sequence: form calculation sections and load cases, drop empty cases, validate references and shell geometry, remove unreferenced nodes, then complete 2D data. Do not mutate the saved document.
- [ ] Reproduce the Step 1 load transformations: selector `0 → 1`, negative node ID to prescribed displacement/rotation with `/1000` scaling, ordered member-row expansion to `m`, and LL sweep/child-case ordering. Return an actionable validation error before calling Python.

**Verification**: Focused C# tests compare calculation JSON with Step 1 Angular fixtures, including boundary cases and invalid input. Python accepts the produced requests and returns the expected ordered cases and selected values.

#### Step 3: Establish the Python embedding lifecycle and failure boundary

- [ ] Add `pythonnet` to `FrameWebforCS.csproj` and introduce one owned Python runtime service that resolves the uv Python DLL/home/source path, initializes once, imports the target module, serializes `Py.GIL()` calls, and exchanges JSON-compatible request/result values.
- [ ] Run the blocking FEM call off the UI thread while preserving Python thread/GIL requirements. Distinguish startup/import, solver, and conversion errors. Cancellation suppresses publication while an in-process solve completes; prevent concurrent solves and shutdown races.
- [ ] Validate the returned root as canonical `AnalysisResultSet v1`; do not accept the former `root.result.case` response as a success fallback.

**Verification**: A headless C# integration test invokes a small real FrameWeb fixture twice, asserts the ordered v1 results, and covers failure, cancel-while-solving, and shutdown without a hang. The app builds on the supported Windows environment.

#### Step 4: Add the two-stage development debugger handshake

- [ ] Adapt the Probe's sequence into a debug-only, awaitable two-gate handshake: after Python initialization expose gate 1; release it only after Visual Studio attaches Python (native) + Native; import `fem.analysis_result_sets`; expose gate 2; release it only after the breakpoint at the actual `build_analysis_result_set` source line reports `Children > 0`; then permit the calculation call.
- [ ] Give the app-specific launcher a PID-scoped local control channel, explicit stage acknowledgements, timeout/cancel/error handling, and a configurable Python file/line, defaulting to `FrameWeb/src/fem/analysis_result_sets.py:46`. Update both `EnsurePythonBreakpoint` and `PythonBreakpointChildren` checks to match that exact file and line. Launch the app without a pre-attached Managed debugger, resolve its current PID and x64 Python symbols, then attach the required engines. In ordinary runs bypass the handshake; never use `Console.ReadLine()`, fixed sleeps, or F11 as a readiness signal.
- [ ] Keep the form responsive and show a cancelable waiting state through both gates. Dispose the handshake when the calculation or document lifetime ends.

**Verification**: On a Debug x64 run, the launcher attaches, observes a bound breakpoint, releases gate 2, and Visual Studio stops on the requested Python line. Cancel/attach failure leaves the GUI usable and does not invoke Python.

#### Step 5: Publish `AnalysisResultSet v1` into C# result views

- [ ] Parse and semantically validate the complete v1 result, prepare the three tables, 3D scene, page selection, and derived views, then replace result state once on the UI thread. A failure must not publish partial new results. Map canonical string IDs, support reactions, topology stations, member segments, and case/state keys directly; verify member-end signs and 3D coordinates rather than feeding the old `root.result.case` parser.
- [ ] Follow the old result screens: first/last static case, every nonlinear step, every modal mode (mode shape in displacement; no reaction/force rows), 2D/3D switching, LL parent/child grouping with signed max/min direction and source case, and DEFINE/COMBINE/PICKUP from static base results. Keep derived values outside the canonical response.
- [ ] Read v1 `units` before formatting. Convert only when a declared, recognized unit supports the conversion; otherwise show the raw value with an unspecified-unit label instead of assuming mm, kN, or kN·m.
- [ ] Inventory `GetSaveJson`, `JsonDataOpen`, and dimension result tests. Preserve old-file open/save behavior until successful recalculation; then clear stale legacy result fields. Do not serialize runtime v1 data as legacy `result`/`resultDimension`. Leave input-only persistence migration to separate work.

**Verification**: C# tests compare page labels/order and table/3D numbers with Step 1 fixtures, including nonlinear/modal states, LL envelope/direction, member stations/signs, and units. Malformed v1 or failed presentation leaves no partial new state. Old-file open → recalculate → save clears stale legacy results without serializing v1 as legacy data. Manual checks exercise result tabs, pages, LL direction, and 3D.

#### Step 6: Wire the calculation command to document and result state

- [ ] Connect the calculation menu action to request projection, validation, the Python bridge, and the Step 5 result publisher. Capture an immutable request snapshot and an input edit generation before dispatch; increment the generation on every relevant node/member/load/property/support edit as well as document replacement. Reject duplicate commands and ignore results after an edit, replacement, or cancellation.
- [ ] Display progress and errors on the UI thread. Match the old calculation command's result-clear timing, and define one consistent state for old saved results during failure/cancellation. Keep the form responsive during preprocessing, both debug gates, and the solver call.

**Verification**: UI/service tests cover success, invalid input, solver failure, duplicate click, cancellation, an input cell edit during the solve, and an old calculation completing after document replacement. A manual run reaches `build_analysis_result_set` and visible results from the menu.

#### Step 7: End-to-end acceptance and focused gates

- [ ] Run representative 2D static and 3D/moving-load calculations from the GUI. Compare case/state order, selected numerical values, units, table pages, LL presentation, and 3D with the fixtures; verify persistence.
- [ ] Repeat with the development attach script and confirm the Python line breakpoint hits after bind, then continue to visible results. Document the environment setup, control channel, target file/line, and normal-run behavior.
- [ ] Run the changed C# test project/build and relevant FrameWeb pytest cases; inspect `git diff` for scope, generated files, and unrelated user changes.

**Verification**: Input → pythonnet → `AnalysisResultSet v1` → required desktop views passes; the debugger stops on the Python line and resumes to the same results; relevant component gates pass.

### Risks & Considerations

- The user selected in-process pythonnet for this `FrameWebforCS/` bridge, superseding the earlier private-loopback-HTTP desktop calculation choice. Keep printing and other applications' transports separate; do not add an HTTP fallback to this feature.
- Python execution inside the WinForms process can block or destabilize the app if GIL, thread ownership, native dependency resolution, shutdown, or cancellation are mishandled. Prove the lifecycle early with a real fixture; isolate errors and prevent concurrent calls.
- `GetSaveJson` and raw C# `m1/m2` load rows are not solver inputs. Angular's validation, 2D completion, prescribed displacement, member expansion, and LL sweep carry meaning; fixture parity is required before trusting calculations.
- Existing C# result services consume old case dictionaries and fixed unit labels, whereas Python emits v1 case/state results with topology and possibly unspecified units. Prepare one canonical result state and explicit table/3D presentation before publishing it.
- Current C# save/open code persists legacy `result` and `resultDimension`, despite the documented input-only project contract. Preserve old-file behavior in this scoped feature; perform any migration under a separate decision and test plan.
- A Python breakpoint is usable only after the target module is imported and the bound child exists. Visual Studio attach return alone is insufficient. Normal releases must never wait for a debugger.
- `DocumentRevision` currently advances on file open but not ordinary input edits, so it alone cannot prevent stale result publication. The command needs a separate input edit generation or equivalent snapshot identity wired to all relevant edits.
- Unrelated C# dimension and rendering edits are active in the worktree. Inspect ownership and the relevant diff before implementation; do not overwrite those edits.

### Open Questions

- None for transport or capacity policy: pythonnet is selected, and new bridge-specific capacity limits are deferred. Resolve fixture-specific presentation choices during Step 1 before implementing those views.
