# Python.NET mixed-mode debugger probe

This isolated console program checks Visual Studio 2026 debugging of Python
code hosted by a C# pythonnet process. The preferred workflow stops at a Python
breakpoint without stepping from C#.

## FrameWebforCS calculation debugger

Open `FrameWebforCS/FrameWebforCS.csproj` in Visual Studio, then run from the
repository root:

```powershell
& tools/PythonNetDebugProbe/StartMixedDebug.ps1 -CalculationApp -VisualStudioProcessId <VS_PID>
```

The script builds and launches the desktop app with the development gate enabled.
Start a calculation in the app. The app holds after CPython initialization and
exposes `FrameWebCalculationDebug-<app PID>` to the launcher. The launcher sets
the Python breakpoint at `FrameWeb/src/fem/analysis_result_sets.py:46`, checks
for the local matching `python312.dll` and `python312.pdb`, then attaches
**Python (native) + Native** to this app PID. The app imports
`fem.analysis_result_sets` only after the attach acknowledgement. The launcher
then waits for the breakpoint's `Children > 0` and acknowledges the second
gate before the solver is invoked. `-BreakpointFile` and `-PythonLine` select a
different source location. Attach, import, breakpoint binding, and call are
independently acknowledged by the PID-scoped local pipe.

Normal app launches bypass both gates. The app keeps its UI thread responsive
while waiting. Cancel a calculation in the app to stop waiting; an active FEM
solve itself remains non-interruptible, so cancellation discards its result.
If attach or binding fails, the gate times out and reports an error. This
launcher has not yet demonstrated an actual stop in the desktop app; the
existing probe result below is separate evidence.

## One-command Python breakpoint run

Open the probe project in Visual Studio, then run this from the repository root
in PowerShell. Pass the PID of that Visual Studio instance if more than one is
open:

```powershell
& tools/PythonNetDebugProbe/StartMixedDebug.ps1 -VisualStudioProcessId <VS_PID> -Experiment PythonBreakpoint
```

The command builds the probe, sets a breakpoint at `probe_calculation.py:2`,
temporarily disables probe C# breakpoints, starts the process, and attaches **Python
(native) + Native**. The probe then imports the Python module. Only after the
Python breakpoint reports `Children > 0` does the command release
`calculate(7, 5)`. Visual Studio stops in `calculate()` without F11. Continue
in Visual Studio to let the command print `Python result: 27` and exit.

The separate output directory `bin/PythonBreakpoint/net10.0` prevents a prior
debugged probe from locking the executable used for this run. This is a probe
workflow; it does not change the C# project's ordinary F5 configuration.

## Run from the repository root (PowerShell)

```powershell
dotnet restore tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj
dotnet build tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj -c Debug
dotnet run --project tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj -c Debug --no-build
```

The program prints its PID and waits for Enter after initializing CPython. It
then calls `probe_calculation.calculate(7, 5)` and checks for `Python result: 27`.
It reads the Python home from `FrameWeb/.venv/pyvenv.cfg`, including when
Visual Studio starts the program. Set `PYTHONNET_PYDLL` only to override that
local environment.

## Visual Studio 2026 debugger check

On the machine inspected on 2026-09-27, Visual Studio Community 2026 18.10.2
has the **Python native development tools** component. A matching
`python312.pdb` from the Astral 20260203 full distribution was placed beside
the uv-managed CPython 3.12.12 `python312.dll`; reload that module's symbols
in Visual Studio after attaching.

1. Open `PythonNetDebugProbe.csproj` and `probe_calculation.py` in Visual Studio.
   Install the **Python development** workload if Python debugging is absent.
2. Start the command above and leave the process at the Enter prompt. Attach to
   the printed PID with **Debug > Attach to Process**. Select **Python (native)**,
   **Managed (.NET Core, .NET 5+)**, and **Native** in the code-type selector.
   The plain **Python** adapter can pick up a stale Visual Studio interpreter
   setting; confirm all three requested code types are active before continuing.
3. Set a C# breakpoint at `Program.cs`, the `calculate.Invoke(...)` line. Set a
   Python breakpoint at `probe_calculation.py`, the `combined = span + load`
   line. Press Enter in the console. The C# breakpoint should hit first.
4. At the C# breakpoint, press **F11**. The debugger enters
   `probe_calculation.py`. The Python breakpoint on line 2 also hits. At that
   breakpoint, Watch shows `span = 7`; after **F10**, `combined = 12`.

All three checks above succeeded on this machine on 2026-09-27. Starting this
C# project with **F5** and the **Managed + Native (.NET Core/.NET 5+)** launch
engine hits the C# breakpoint, but does not enable Python (native): F11 moves
to the next C# line and the Python breakpoint reports missing symbols. The C#
launch profile's debug-engine list does not offer Python (native), so use the
three-engine attach procedure above for this probe.

If a C# breakpoint says its source differs from the original version, rebuild
the Debug project and restart the process. Do not bypass the source check. The
`python312.pdb` must match the installed `python312.dll` build for Python native
debugging.
