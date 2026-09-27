# Python.NET debug event logger

This diagnostic VSIX subscribes to `IVsDebugger.AdviseDebugEventCallback` in Visual Studio 2026. It records UTC timestamps, event IIDs, engine GUIDs, process IDs, and program IDs. `IDebugEngineCreateEvent2`, `IDebugProgramCreateEvent2`, and `IDebugLoadCompleteEvent2` have named rows. Other event IIDs are also logged for diagnosis, up to 10,000 per Visual Studio session. No event releases the probe.

The output is `%LOCALAPPDATA%\FrameWeb3\PythonNetDebugProbe\debug-events-<devenv PID>.tsv`. An `advise` row with `hr=0x00000000` confirms subscription. `pEngine` was null in observed callbacks, so the logger uses `pProgram.GetEngineInfo()` to recover the engine GUID. A blank or zero GUID means that neither source identified it; do not interpret it as Python (native).

## Build and install in the experimental instance

Run from the repository root in PowerShell:

```powershell
$vsix = & tools/PythonNetDebugEventLogger/Build.ps1
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe' /quiet /rootSuffix:Exp $vsix
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' /RootSuffix Exp /UpdateConfiguration
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' /RootSuffix Exp tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj
```

Close the experimental instance before installing a new version. `/UpdateConfiguration` is needed on this Visual Studio 2026 installation: the VSIX installer returned success while the package was not loaded until the experimental configuration imported its `.pkgdef`. `/Setup` requires administrator privileges and is not part of this workflow.

Use `ProbeDebugAutomation.ps1 -VisualStudioProcessId <PID> -Action EnsureBreakpoints` to set `Program.cs:42` and `probe_calculation.py:2`. `-Action Inspect` reports the current frame and each breakpoint's `Children` count; `StepInto` and `Continue` invoke the corresponding DTE debugger actions. Use these only with the experimental Visual Studio PID. Run `tools/PythonNetDebugProbe/StartMixedDebug.ps1` with `-Experiment A`, `B`, or `Observe` and `-VisualStudioProcessId <PID>` to reproduce the existing conditions.

## Current observation

The callback logged Managed `ProgramCreate` and `LoadComplete` events for both a successful A run and a failed B run. It did not log an event with the Python (native) engine GUID `{EC1375B7-E2CE-43E8-BF75-DC638DE1F1F9}` in either run, including the successful F11 into `calculate`. Thus the currently observed `LoadComplete` is **Managed** and cannot be used as a Python readiness barrier. See `../PythonNetDebugProbe/DEBUGGING_STATUS_REPORT.md` for the measured runs.
