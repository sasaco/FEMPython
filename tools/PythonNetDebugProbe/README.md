# Python.NET mixed-mode debugger probe

This isolated console program checks whether this machine's Visual Studio 2026
can step from a C# pythonnet call into CPython and show Python locals. A working
command-line run establishes only embedding; the three debugger behaviors must
be checked in Visual Studio.

## Run from the repository root (PowerShell)

```powershell
dotnet restore tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj
dotnet build tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj -c Debug
$pythonHome = uv --directory FrameWeb run --locked python -c "import sys; print(sys.base_prefix)"
$env:PYTHONNET_PYDLL = Join-Path $pythonHome 'python312.dll'
dotnet run --project tools/PythonNetDebugProbe/PythonNetDebugProbe.csproj -c Debug --no-build
```

The program prints its PID and waits for Enter after initializing CPython. It
then calls `probe_calculation.calculate(7, 5)` and checks for `Python result: 27`.

## Visual Studio 2026 debugger check

On the machine inspected on 2026-09-27, Visual Studio Community 2026 18.10.2
has the Python development workload, but the optional **Python native
development tools** component (`Microsoft.ComponentGroup.PythonTools.NativeDevelopment`)
is absent. Install that component before attempting mixed-mode debugging. The
uv-managed CPython 3.12.12 installation also has no `python312.pdb`; check that
Visual Studio can obtain symbols matching its `python312.dll` build.

1. Open `PythonNetDebugProbe.csproj` and `probe_calculation.py` in Visual Studio.
   Install the **Python development** workload if Python debugging is absent.
2. Start the command above and leave the process at the Enter prompt. Attach to
   the printed PID with **Debug > Attach to Process**. Select **Python code**,
   **Managed (.NET Core, .NET 5+) code**, and **Native code** in the code-type
   selector. Confirm all requested code types are active before continuing.
3. Set a C# breakpoint at `Program.cs`, the `calculate.Invoke(...)` line. Set a
   Python breakpoint at `probe_calculation.py`, the `combined = span + load`
   line. Press Enter in the console. The C# breakpoint should hit first.
4. At the C# breakpoint, press **F11**. Record whether the debugger lands in
   `probe_calculation.py` or only in pythonnet/native frames. Continue if needed
   and record whether the Python breakpoint hits. At that breakpoint, add
   `span`, `load`, `combined`, and `doubled` to Watch. `combined` should become
   `12` after stepping over its assignment; `doubled` should become `24` after
   the next line.

The Python breakpoint and Watch test can succeed even if F11 cannot cross the
pythonnet bridge. Record the three observations separately. Python native
symbols must match the exact `python312.dll` build for native CPython frames;
the command-line result does not establish symbol availability.
