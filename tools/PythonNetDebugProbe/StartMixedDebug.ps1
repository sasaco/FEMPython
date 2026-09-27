param(
    [int]$VisualStudioProcessId,
    # A: manual release without the module check; B: module check with immediate release.
    # Observe keeps both checks and waits for manual release.
    [ValidateSet('Observe', 'A', 'B', 'PythonBreakpoint')]
    [string]$Experiment = 'Observe',
    [switch]$BreakpointGate,
    [switch]$PythonNativeOnly
)

$ErrorActionPreference = 'Stop'
$projectDirectory = $PSScriptRoot
$projectFile = Join-Path $projectDirectory 'PythonNetDebugProbe.csproj'
$outputDirectory = if ($Experiment -eq 'PythonBreakpoint') {
    Join-Path $projectDirectory 'bin\PythonBreakpoint\net10.0'
} else {
    Join-Path $projectDirectory 'bin\Debug\net10.0'
}
$executable = Join-Path $outputDirectory 'PythonNetDebugProbe.exe'
$pythonFile = Join-Path $projectDirectory 'probe_calculation.py'
$csharpFile = Join-Path $projectDirectory 'Program.cs'

if ($Experiment -eq 'PythonBreakpoint') {
    $BreakpointGate = $true
    $PythonNativeOnly = $true
}

if ($VisualStudioProcessId -eq 0) {
    $candidates = @(Get-Process -Name devenv -ErrorAction Stop |
        Where-Object { $_.MainWindowTitle -like '*PythonNetDebugProbe*' })
    if ($candidates.Count -ne 1) {
        throw "Expected one Visual Studio window for PythonNetDebugProbe; found $($candidates.Count). Pass -VisualStudioProcessId explicitly."
    }
    $VisualStudioProcessId = $candidates[0].Id
}

$visualStudio = Get-Process -Id $VisualStudioProcessId -ErrorAction Stop
if ($visualStudio.ProcessName -ne 'devenv') {
    throw "PID $VisualStudioProcessId is not Visual Studio."
}

$assemblyDirectory = Join-Path (Split-Path -Parent $visualStudio.Path) 'PublicAssemblies'
$references = @(
    (Join-Path $assemblyDirectory 'envdte.dll'),
    (Join-Path $assemblyDirectory 'envdte80.dll'),
    (Join-Path $assemblyDirectory 'Microsoft.VisualStudio.Interop.dll')
)
foreach ($reference in $references) {
    Add-Type -Path $reference
}

$source = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE80;

public static class PythonNetMixedDebugger
{
    private static readonly List<Tuple<string, int>> disabledCSharpBreakpoints =
        new List<Tuple<string, int>>();

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx context);

    private static DTE2 GetDte(int visualStudioProcessId)
    {
        IRunningObjectTable table;
        IBindCtx context;
        GetRunningObjectTable(0, out table);
        CreateBindCtx(0, out context);
        IEnumMoniker monikers;
        table.EnumRunning(out monikers);
        IMoniker[] current = new IMoniker[1];
        string expected = "!VisualStudio.DTE.18.0:" + visualStudioProcessId;

        while (monikers.Next(1, current, IntPtr.Zero) == 0)
        {
            string name;
            current[0].GetDisplayName(context, null, out name);
            if (name != expected)
            {
                continue;
            }

            object automationObject;
            table.GetObject(current[0], out automationObject);
            return (DTE2)automationObject;
        }

        throw new InvalidOperationException("The requested Visual Studio DTE instance was not found.");
    }

    public static void EnsurePythonBreakpoint(int visualStudioProcessId, string pythonFile, string csharpFile)
    {
        Debugger2 debugger = (Debugger2)GetDte(visualStudioProcessId).Debugger;
        bool found = false;
        foreach (EnvDTE.Breakpoint breakpoint in debugger.Breakpoints)
        {
            if (string.Equals(breakpoint.File, csharpFile, StringComparison.OrdinalIgnoreCase))
            {
                if (breakpoint.Enabled)
                {
                    breakpoint.Enabled = false;
                    disabledCSharpBreakpoints.Add(Tuple.Create(breakpoint.File, breakpoint.FileLine));
                }
            }
            else if (string.Equals(breakpoint.File, pythonFile, StringComparison.OrdinalIgnoreCase)
                && breakpoint.FileLine == 2)
            {
                breakpoint.Enabled = true;
                found = true;
            }
        }

        if (!found)
        {
            debugger.Breakpoints.Add("", pythonFile, 2, 1, "",
                EnvDTE.dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue,
                "", "", 0, "", 0, EnvDTE.dbgHitCountType.dbgHitCountTypeNone);
        }
    }

    public static bool RestoreCSharpBreakpoints(int visualStudioProcessId)
    {
        try
        {
            Debugger2 debugger = (Debugger2)GetDte(visualStudioProcessId).Debugger;
            foreach (EnvDTE.Breakpoint breakpoint in debugger.Breakpoints)
            {
                foreach (Tuple<string, int> saved in disabledCSharpBreakpoints)
                {
                    if (string.Equals(breakpoint.File, saved.Item1, StringComparison.OrdinalIgnoreCase)
                        && breakpoint.FileLine == saved.Item2)
                    {
                        breakpoint.Enabled = true;
                    }
                }
            }
            disabledCSharpBreakpoints.Clear();
            return true;
        }
        catch (COMException exception)
        {
            if (exception.HResult == unchecked((int)0x80010001))
            {
                return false;
            }

            throw;
        }
    }

    public static int PythonBreakpointChildren(int visualStudioProcessId, string pythonFile)
    {
        Debugger2 debugger = (Debugger2)GetDte(visualStudioProcessId).Debugger;
        foreach (EnvDTE.Breakpoint breakpoint in debugger.Breakpoints)
        {
            if (string.Equals(breakpoint.File, pythonFile, StringComparison.OrdinalIgnoreCase)
                && breakpoint.FileLine == 2 && breakpoint.Enabled)
            {
                return breakpoint.Children.Count;
            }
        }

        throw new InvalidOperationException("The enabled Python breakpoint at line 2 was not found.");
    }

    public static string DebuggerPosition(int visualStudioProcessId)
    {
        try
        {
            EnvDTE.Debugger debugger = GetDte(visualStudioProcessId).Debugger;
            string function = "";
            if (debugger.CurrentMode == EnvDTE.dbgDebugMode.dbgBreakMode)
            {
                try { function = debugger.CurrentStackFrame.FunctionName; }
                catch (COMException) { }
            }

            return debugger.CurrentMode.ToString() + ":" + function;
        }
        catch (COMException exception)
        {
            if (exception.HResult == unchecked((int)0x80010001))
            {
                return "busy";
            }

            throw;
        }
    }

    public static void Attach(int visualStudioProcessId, int probeProcessId, bool pythonNativeOnly)
    {
            Debugger2 debugger = (Debugger2)GetDte(visualStudioProcessId).Debugger;
            foreach (EnvDTE.Process process in debugger.LocalProcesses)
            {
                if (process.ProcessID != probeProcessId)
                {
                    continue;
                }

                Process2 target = (Process2)process;
                if (target.IsBeingDebugged)
                {
                    throw new InvalidOperationException("The probe is already being debugged.");
                }

                string[] engines = pythonNativeOnly
                    ? new string[]
                    {
                        "{EC1375B7-E2CE-43E8-BF75-DC638DE1F1F9}", // Python (native)
                        "{3B476D35-A401-11D2-AAD4-00C04F990171}"  // Native
                    }
                    : new string[]
                    {
                        "{EC1375B7-E2CE-43E8-BF75-DC638DE1F1F9}", // Python (native)
                        "{3B476D35-A401-11D2-AAD4-00C04F990171}", // Native
                        "{2E36F1D4-B23C-435D-AB41-18E608940038}"  // Managed .NET Core
                    };
                target.Attach2(engines);
                return;
            }

            throw new InvalidOperationException("The probe process is not visible to Visual Studio.");
    }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies $references

if ($Experiment -eq 'PythonBreakpoint') {
    & dotnet build $projectFile -c Debug --nologo "-p:OutputPath=$outputDirectory"
}
else {
    & dotnet build $projectFile -c Debug --nologo
}
if ($LASTEXITCODE -ne 0) {
    throw "The probe build failed with exit code $LASTEXITCODE."
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $executable
$startInfo.WorkingDirectory = $outputDirectory
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
if ($BreakpointGate) {
    $startInfo.Arguments = '--breakpoint-gate'
}
$probe = [System.Diagnostics.Process]::Start($startInfo)

try {
    if ($Experiment -eq 'PythonBreakpoint') {
        [PythonNetMixedDebugger]::EnsurePythonBreakpoint($VisualStudioProcessId, $pythonFile, $csharpFile)
        Write-Output "Python breakpoint prepared: ${pythonFile}:2"
    }

    $firstLine = $probe.StandardOutput.ReadLine()
    $secondLine = $probe.StandardOutput.ReadLine()
    if ($null -eq $secondLine) {
        throw "The probe exited before attach: $($probe.StandardError.ReadToEnd())"
    }

    Write-Output $firstLine
    Write-Output $secondLine

    Write-Output "Experiment=$Experiment; probe PID=$($probe.Id)"
    if ($Experiment -ne 'A') {
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        $pythonModule = $null
        do {
            if ($probe.HasExited) {
                throw "The probe exited before python312.dll was detected: $($probe.StandardError.ReadToEnd())"
            }

            try {
                $pythonModule = Get-Process -Id $probe.Id -ErrorAction Stop |
                    Select-Object -ExpandProperty Modules |
                    Where-Object { $_.ModuleName -ieq 'python312.dll' } |
                    Select-Object -First 1
            }
            catch [System.ComponentModel.Win32Exception] {
                throw "Cannot inspect modules for probe PID $($probe.Id): $($_.Exception.Message)"
            }

            if ($null -eq $pythonModule) {
                Start-Sleep -Milliseconds 100
            }
        } until ($null -ne $pythonModule -or [DateTime]::UtcNow -ge $deadline)

        if ($null -eq $pythonModule) {
            throw "Timed out waiting for python312.dll in probe PID $($probe.Id)."
        }

        Write-Output "$(Get-Date -Format o) python312.dll loaded in PID $($probe.Id): $($pythonModule.FileName)"
    }

    [PythonNetMixedDebugger]::Attach($VisualStudioProcessId, $probe.Id, [bool]$PythonNativeOnly)
    Write-Output "$(Get-Date -Format o) Attach2 returned for PID $($probe.Id); engine readiness is not yet verified."
    if ($Experiment -ne 'B' -and $Experiment -ne 'PythonBreakpoint') {
        Read-Host 'Inspect Visual Studio Modules, code types, and Python breakpoint. Press Enter here only when ready to release the probe'
    }

    Write-Output "$(Get-Date -Format o) Releasing probe PID $($probe.Id)"
    $probe.StandardInput.WriteLine()
    $probe.StandardInput.Flush()

    if ($BreakpointGate) {
        $readyRead = $probe.StandardOutput.ReadLineAsync()
        if (-not $readyRead.Wait([TimeSpan]::FromSeconds(30))) {
            throw "Timed out waiting for Python module import. Debugger: $([PythonNetMixedDebugger]::DebuggerPosition($VisualStudioProcessId))"
        }
        $moduleReady = $readyRead.Result
        if ($moduleReady -ne 'PYTHON_MODULE_READY') {
            throw "The probe did not reach the Python module gate: $moduleReady"
        }
        Write-Output $moduleReady
        if ($Experiment -eq 'PythonBreakpoint') {
            $deadline = [DateTime]::UtcNow.AddSeconds(30)
            do {
                $children = [PythonNetMixedDebugger]::PythonBreakpointChildren($VisualStudioProcessId, $pythonFile)
                if ($children -gt 0) { break }
                if ($probe.HasExited) { throw 'Probe exited before Python breakpoint binding.' }
                Start-Sleep -Milliseconds 100
            } while ([DateTime]::UtcNow -lt $deadline)
            if ($children -eq 0) {
                throw "Python breakpoint did not bind. Debugger: $([PythonNetMixedDebugger]::DebuggerPosition($VisualStudioProcessId))"
            }
            Write-Output "$(Get-Date -Format o) Python breakpoint bound (Children=$children)."
        }
        else {
            Read-Host 'Inspect Python breakpoint binding. Press Enter to invoke calculate'
        }
        $probe.StandardInput.WriteLine()
        $probe.StandardInput.Flush()
    }

    if ($Experiment -eq 'PythonBreakpoint') {
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        do {
            $position = [PythonNetMixedDebugger]::DebuggerPosition($VisualStudioProcessId)
            if ($position -eq 'dbgBreakMode:calculate') { break }
            if ($probe.HasExited) { throw 'Probe exited without hitting the Python breakpoint.' }
            if ($position -like 'dbgBreakMode:*' -and $position -ne 'dbgBreakMode:') {
                throw "Debugger stopped outside the Python breakpoint: $position"
            }
            Start-Sleep -Milliseconds 100
        } while ([DateTime]::UtcNow -lt $deadline)
        if ($position -ne 'dbgBreakMode:calculate') {
            throw "Timed out waiting for the Python breakpoint. Debugger: $position"
        }
        Write-Output "$(Get-Date -Format o) Python breakpoint hit in calculate(). Continue in Visual Studio to finish the probe."
    }

    Write-Output $probe.StandardOutput.ReadToEnd()
    $probe.WaitForExit()
    if ($probe.ExitCode -ne 0) {
        throw "The probe exited with code $($probe.ExitCode): $($probe.StandardError.ReadToEnd())"
    }
}
finally {
    if ($Experiment -eq 'PythonBreakpoint') {
        try {
            $restoreDeadline = [DateTime]::UtcNow.AddSeconds(30)
            do {
                $restored = [PythonNetMixedDebugger]::RestoreCSharpBreakpoints($VisualStudioProcessId)
                if ($restored) { break }
                Start-Sleep -Milliseconds 100
            } while ([DateTime]::UtcNow -lt $restoreDeadline)
            if (-not $restored) { Write-Warning 'Timed out restoring probe C# breakpoints.' }
        }
        catch {
            Write-Warning "Could not restore probe C# breakpoints: $($_.Exception.Message)"
        }
    }
    if (-not $probe.HasExited) {
        $probe.Kill()
    }
    $probe.Dispose()
}
