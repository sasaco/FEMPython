# Native assertion bridge for transverse corrections

This scoped bridge observes real xUnit tests through the unchanged repository
`run_tests.py` pytest resolver. It does not measure native coverage.

From repository-root PowerShell:

```powershell
$env:TRANSVERSE_NATIVE_PROJECT = 'FrameWebforCS/tests/FrameWebforCS.Tests.csproj'
$env:TRANSVERSE_NATIVE_FILTER = 'FullyQualifiedName~CalculationRequestTests.ExactTestName'
$env:TRANSVERSE_NATIVE_EXPECT = 'CalculationRequestTests.ExactTestName'
$env:TRANSVERSE_NATIVE_LABEL = 'request-red-1'
uv run --project FrameWeb --locked --extra dev python .agents/skills/_shared/run_tests.py --component python --target FrameWebforCS/tests/TransverseParity/test_native.py --expect fail --label request-red-1 --timeout 600
# After the implementation, use a new label and --expect pass.
```

`--component python` selects the configured, locked pytest prefix even though
the bridge lives outside `FrameWeb/`. Do not replace that prefix with global pytest.

| Environment variable | Meaning |
| --- | --- |
| `TRANSVERSE_NATIVE_PROJECT` | Required repository-contained `.csproj`, absolute or relative to the repository root. |
| `TRANSVERSE_NATIVE_FILTER` | Required `dotnet test --filter` expression. Prefer the exact regression method. |
| `TRANSVERSE_NATIVE_EXPECT` | Required semicolon-separated test-name substrings. Every name must execute. A class name can cover a whole class. Failed tests must match these names. |
| `TRANSVERSE_NATIVE_LABEL` | Artifact label; letters, digits, `.`, `_`, `-`; default `native`. |
| `TRANSVERSE_NATIVE_TIMEOUT` | Inner process timeout, 1–400 seconds; default 180. Keep the outer timeout at 600 or greater. |

Each run builds/tests Release with one MSBuild node and no node reuse. A shared
Windows byte lock serializes bridge calls for up to 120 seconds. Other correction
tooling can reuse `native_runner.build_lock()` and `run_owned(command, log_path,
timeout)` for build/harness operations that share outputs. Direct dotnet commands
do not automatically acquire this lock; coordinate those with the lead.

A waiting Python launcher is assigned to an owned Windows job before it can start
dotnet. On completion/failure/timeout, the job terminates all descendants, waits
for zero active processes, and reaps its root. Logs, TRX, cleanup counts and
`observation.json` remain in unique `.tmp/transverse-analysis-print-corrections/native/`
directories. No user process is selected or terminated.

The bridge requires nonzero executed rows, consistent TRX counters, no skipped
selected tests, the xUnit adapter, every expected test name, and matching dotnet
exit status. A valid Red also requires native xUnit assertion text/type. Ordinary
runtime exceptions, build/restore/discovery failures, missing names, zero tests,
timeouts and bridge exceptions exit pytest with code 2. `run_tests.py` rejects
them as `collection_error`; this classification means an invalid observation,
not a product assertion failure. Standard xUnit assertions are supported; an
unrecognized custom assertion signature is conservatively rejected.

Run the real native controls (pass, assertion failure, unmatched filter, skip,
build failure, wrong required name, runtime exception, unrelated assertion and
timeout) with:

```powershell
uv run --project FrameWeb --locked --extra dev python FrameWebforCS/tests/TransverseParity/selfcheck.py
```

This creates a small private xUnit project with the repository's existing package
versions. The summary and per-control wrapper JSON are under
`.tmp/transverse-analysis-print-corrections/runner-selfcheck/`; wrapper logs use
`.agents/logs/transverse-selfcheck-*.log`. Pass and assertion-failure controls must
return wrapper exit 0 with `passed`/`failed`; all invalid controls must return
wrapper exit 2 with `collection_error`. Existing protected verification evidence
is not touched. The fixture source is embedded in `selfcheck.py`, so controls can
be recreated if disposable `.tmp` content is removed.
