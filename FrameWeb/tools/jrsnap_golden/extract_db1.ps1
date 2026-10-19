param(
    [Parameter(Mandatory = $true)][string]$Db1,
    [Parameter(Mandatory = $true)][string]$OutJson,
    [string]$SnapDbLib
)
# Dump every stored step of a JRSNAP result database (.DB1) through SNAPDBLib.
# SNAPDBLib is 32-bit: run this script with %SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe.
#
# Raw layout version 2 (2026-10-19). Version 1 wrote the joint items and the member end forces only;
# those keys are unchanged. Version 2 adds, read through reflection so that nothing SNAPDBLib exposes
# is left out: the other member items (SCFlg, Outou_M, Outou_FAI_Kaiseki, the stored M-phi points, ...),
# the basic data (KihonInfo: control, step counts, nonlinearity flags), the member data (MemberInfo:
# A, I, E, the nonlinear link M), the joint coordinates, and the array lengths (JointItem is indexed by
# joint number, but JibanHanryoku/BaneFlg in it are indexed by support row, so the lengths say how many
# support rows can be read).
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

function Props($obj, $depth) {
    # Every readable property, in name order; nested objects one level deep; arrays skipped.
    $d = [ordered]@{}
    $names = @($obj | Get-Member -MemberType Property | ForEach-Object { $_.Name } | Sort-Object)
    foreach ($name in $names) {
        $v = $obj.$name
        if ($v -eq $null -or $v -is [System.Array]) { continue }
        $t = $v.GetType()
        if ($t.IsPrimitive -or $v -is [string] -or $v -is [decimal]) { $d[$name] = $v }
        elseif ($depth -gt 0) { $d[$name] = Props $v ($depth - 1) }
    }
    return $d
}

$legacyMember = @('Mi', 'Ni', 'Si', 'Mj', 'Nj', 'Sj')
$steps = New-Object System.Collections.ArrayList
$ctrl = $db.OutputInfo.StepCtrl
$lengths = $null
for ($s = 0; $s -lt $ctrl.Length; $s++) {
    $sc = $ctrl[$s]
    if ($sc.JointItem -eq $null) { continue }
    if ($lengths -eq $null) {
        $lengths = [ordered]@{ joint_item = $sc.JointItem.Length; member_item = $sc.MemberItem.Length;
                               support_info = $db.InputInfo.SupportInfo.Length;
                               joint_info = $db.InputInfo.JointInfo.Length; member_info = $db.InputInfo.MemberInfo.Length }
    }
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
        $row = [ordered]@{ element = $m; Mi = $mi.Mi; Ni = $mi.Ni; Si = $mi.Si; Mj = $mi.Mj; Nj = $mi.Nj; Sj = $mi.Sj }
        $all = Props $mi 1
        foreach ($name in $all.Keys) {
            if ($legacyMember -notcontains $name) { $row[$name] = $all[$name] }
        }
        $members += $row
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
$memberInfo = @()
for ($m = 1; $m -lt $db.InputInfo.MemberInfo.Length; $m++) {
    $info = $db.InputInfo.MemberInfo[$m]
    if ($info -eq $null) { continue }
    $row = [ordered]@{ element = $m }
    $all = Props $info 0
    foreach ($name in $all.Keys) { $row[$name] = $all[$name] }
    $memberInfo += $row
}
$jointInfo = @()
for ($j = 1; $j -lt $db.InputInfo.JointInfo.Length; $j++) {
    $info = $db.InputInfo.JointInfo[$j]
    if ($info -eq $null) { continue }
    $jointInfo += [ordered]@{ joint = $j; x = $info.X; y = $info.Y }
}
$kihon = Props $db.InputInfo.KihonInfo 1
$doc = [ordered]@{ raw_version = 2; step_count = $ctrl.Length; lengths = $lengths; kihon = $kihon;
                   supports = $supports; member_info = $memberInfo; joint_info = $jointInfo; steps = $steps }
$json = $doc | ConvertTo-Json -Depth 8 -Compress
[IO.File]::WriteAllText($OutJson, $json, (New-Object Text.UTF8Encoding($false)))
"steps=$($steps.Count) supports=$($supports.Count) members=$($memberInfo.Count)"
