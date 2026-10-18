param(
    [Parameter(Mandatory = $true)][string]$Db1,
    [Parameter(Mandatory = $true)][string]$OutJson,
    [string]$SnapDbLib
)
# Dump every stored step of a JRSNAP result database (.DB1) through SNAPDBLib.
# SNAPDBLib is 32-bit: run this script with %SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe.
$ErrorActionPreference = 'Stop'
if (-not $SnapDbLib) {
    # Default: the copy built with Aggre-SE in the structural-mcp checkout that contains FEMPython.
    $SnapDbLib = Join-Path $PSScriptRoot '..\..\..\..\Batch\Aggre-SE\Headless\bin\Release\SNAPDBLib.dll'
}
if ([IntPtr]::Size -ne 4) { throw 'Run under 32-bit PowerShell (SysWOW64).' }
Add-Type -Path (Resolve-Path $SnapDbLib)
$db = New-Object SNAPDBLib.CSNAPDB
$err = $db.DataRead((Resolve-Path $Db1).Path)
if ($err -ne 0) { throw "SNAPDBLib DataRead returned $err" }

$steps = New-Object System.Collections.ArrayList
$ctrl = $db.OutputInfo.StepCtrl
for ($s = 0; $s -lt $ctrl.Length; $s++) {
    $sc = $ctrl[$s]
    if ($sc.JointItem -eq $null) { continue }
    $joints = @()
    # Index 0 is unused; JRSNAP joint and element numbers start at 1.
    for ($j = 1; $j -lt $sc.JointItem.Length; $j++) {
        $ji = $sc.JointItem[$j]
        $joints += [ordered]@{ joint = $j; dx = $ji.DeltaX; dy = $ji.DeltaY; rz = $ji.DeltaT; dg = $ji.DeltaG;
                               jiban_hanryoku = $ji.JibanHanryoku; bane_flg = $ji.BaneFlg; outou_delta = $ji.Outou_Delta }
    }
    $members = @()
    for ($m = 1; $m -lt $sc.MemberItem.Length; $m++) {
        $mi = $sc.MemberItem[$m]
        $members += [ordered]@{ element = $m; Mi = $mi.Mi; Ni = $mi.Ni; Si = $mi.Si; Mj = $mi.Mj; Nj = $mi.Nj; Sj = $mi.Sj }
    }
    [void]$steps.Add([ordered]@{ index = $s; lo_step_no = $sc.LoStepNo; joints = $joints; members = $members })
}
$supports = @()
foreach ($sp in $db.InputInfo.SupportInfo) {
    if ($sp -eq $null -or $sp.JoNO -eq 0) { continue }
    $supports += [ordered]@{ joint = $sp.JoNO; ik = $sp.IK;
                             K1_S = $sp.K1_S; K2_S = $sp.K2_S; K3_S = $sp.K3_S; Rmit1_S = $sp.Rmit1_S; Rmit2_S = $sp.Rmit2_S;
                             K1_F = $sp.K1_F; K2_F = $sp.K2_F; K3_F = $sp.K3_F; Rmit1_F = $sp.Rmit1_F; Rmit2_F = $sp.Rmit2_F }
}
$doc = [ordered]@{ step_count = $ctrl.Length; supports = $supports; steps = $steps }
$json = $doc | ConvertTo-Json -Depth 6 -Compress
[IO.File]::WriteAllText($OutJson, $json, (New-Object Text.UTF8Encoding($false)))
"steps=$($steps.Count) supports=$($supports.Count)"
