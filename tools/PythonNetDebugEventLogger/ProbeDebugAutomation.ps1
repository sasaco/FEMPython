param(
    [Parameter(Mandatory)]
    [int]$VisualStudioProcessId,
    [Parameter(Mandatory)]
    [ValidateSet('EnsureBreakpoints', 'EnsurePythonBreakpoint', 'Inspect', 'StepInto', 'Continue', 'Stop')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'
$assemblyDirectory = Join-Path (Split-Path -Parent (Get-Process -Id $VisualStudioProcessId -ErrorAction Stop).Path) 'PublicAssemblies'
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE80;

public static class ProbeVisualStudio
{
    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx context);

    public static DTE2 GetDte(int processId)
    {
        IRunningObjectTable table;
        IBindCtx context;
        GetRunningObjectTable(0, out table);
        CreateBindCtx(0, out context);
        IEnumMoniker monikers;
        table.EnumRunning(out monikers);
        IMoniker[] current = new IMoniker[1];
        string expected = "!VisualStudio.DTE.18.0:" + processId;
        while (monikers.Next(1, current, IntPtr.Zero) == 0)
        {
            string name;
            current[0].GetDisplayName(context, null, out name);
            if (name == expected)
            {
                object instance;
                table.GetObject(current[0], out instance);
                return (DTE2)instance;
            }
        }

        throw new InvalidOperationException("Visual Studio DTE not found: " + expected);
    }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies $references

$dte = [ProbeVisualStudio]::GetDte($VisualStudioProcessId)
$debugger = $dte.Debugger

if ($Action -eq 'EnsureBreakpoints' -or $Action -eq 'EnsurePythonBreakpoint') {
    $probeDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..\PythonNetDebugProbe')).Path
    $locations = @(@{ File = (Join-Path $probeDirectory 'probe_calculation.py'); Line = 2 })
    if ($Action -eq 'EnsureBreakpoints') {
        $locations += @{ File = (Join-Path $probeDirectory 'Program.cs'); Line = 55 }
    }
    else {
        @($debugger.Breakpoints | Where-Object {
            $_.File -ieq (Join-Path $probeDirectory 'Program.cs')
        }) | ForEach-Object { $_.Delete() }
    }

    foreach ($location in $locations) {
        $existing = @($debugger.Breakpoints | Where-Object {
            $_.File -ieq $location.File -and $_.FileLine -eq $location.Line
        })
        if ($existing.Count -eq 0) {
            $null = $debugger.Breakpoints.Add(
                '', $location.File, $location.Line, 1, '',
                [EnvDTE.dbgBreakpointConditionType]::dbgBreakpointConditionTypeWhenTrue,
                '', '', 0, '', 0,
                [EnvDTE.dbgHitCountType]::dbgHitCountTypeNone)
        }
        else {
            $existing | ForEach-Object { $_.Enabled = $true }
        }
    }
}
elseif ($Action -eq 'StepInto') {
    $debugger.StepInto($false)
}
elseif ($Action -eq 'Continue') {
    $debugger.Go($false)
}
elseif ($Action -eq 'Stop') {
    $debugger.Stop($false)
}

$frame = $null
try { $frame = $debugger.CurrentStackFrame.FunctionName } catch {}
$breakpoints = @($debugger.Breakpoints | ForEach-Object {
    [pscustomobject]@{
        File = $_.File
        Line = $_.FileLine
        Enabled = $_.Enabled
        Children = $_.Children.Count
    }
})

[pscustomobject]@{
    Timestamp = (Get-Date).ToUniversalTime().ToString('o')
    Mode = [string]$debugger.CurrentMode
    Frame = $frame
    Breakpoints = $breakpoints
} | ConvertTo-Json -Depth 4
