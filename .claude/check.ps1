[CmdletBinding()]
param(
    [switch]$AgentOnly,
    [switch]$IncludeOptionalProduct,
    [switch]$ListGates,
    [switch]$AllowNoGates,
    [string]$ProjectRoot = '',
    [string]$BaselineRef = '',
    [string]$ScopeAllowlist = '',
    [string]$AllowProductPath = '',
    [string]$LogFile = ''
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
$env:PYTHONDONTWRITEBYTECODE = '1'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$script:Tools = [ordered]@{}
$script:Warnings = [System.Collections.Generic.List[string]]::new()
$script:LogLines = [System.Collections.Generic.List[string]]::new()
$script:Passed = 0
$script:Failed = 0
$script:Skipped = 0
$script:PredicateDetail = ''

$AgentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $AgentDir '..'
}

function Write-RawResult {
    param(
        [Parameter(Mandatory = $true)][string]$Overall,
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [Parameter(Mandatory = $true)][string]$Warning
    )

    $Payload = [ordered]@{
        ok = $false
        overall = $Overall
        tools = [ordered]@{}
        log_file = $null
        warnings = @($Warning)
        artifacts = @()
    }
    [Console]::Out.WriteLine(($Payload | ConvertTo-Json -Depth 8 -Compress))
    exit $ExitCode
}

try {
    $Root = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path
}
catch {
    Write-RawResult 'bad_args' 1 "project root is not a directory: $ProjectRoot"
}
if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
    Write-RawResult 'bad_args' 1 "project root is not a directory: $Root"
}

function Convert-ToRepoRelative {
    param([Parameter(Mandatory = $true)][string]$Path)

    $RootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $PathFull = [System.IO.Path]::GetFullPath($Path)
    $Prefix = $RootFull + [System.IO.Path]::DirectorySeparatorChar
    if ($PathFull.Equals($RootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return '.'
    }
    if ($PathFull.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $PathFull.Substring($Prefix.Length).Replace('\', '/')
    }
    return $null
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $Stamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $LogFile = ".agents/logs/check-$Stamp-$PID.log"
}
$LogPath = if ([System.IO.Path]::IsPathRooted($LogFile)) {
    [System.IO.Path]::GetFullPath($LogFile)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $Root $LogFile))
}
$LogReference = Convert-ToRepoRelative $LogPath
if ($null -eq $LogReference) {
    Write-RawResult 'bad_args' 1 '-LogFile must stay inside the project root'
}

function Add-Log {
    param([Parameter(Mandatory = $true)][string]$Text)
    $script:LogLines.Add($Text) | Out-Null
}

function Add-Tool {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Status,
        [AllowNull()][Nullable[int]]$ExitCode,
        [string[]]$Command = @(),
        [string]$Detail = '',
        [hashtable]$Extra = @{}
    )

    $Entry = [ordered]@{
        status = $Status
        exit_code = $ExitCode
        command = @($Command)
        detail = $Detail
    }
    foreach ($Key in $Extra.Keys) {
        $Entry[$Key] = $Extra[$Key]
    }
    $script:Tools[$Name] = $Entry
}

function Complete-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Overall,
        [Parameter(Mandatory = $true)][int]$ExitCode
    )

    Add-Log "RESULT overall=$Overall passed=$script:Passed failed=$script:Failed skipped=$script:Skipped"
    $LogWritten = $false
    try {
        $Parent = Split-Path -Parent $LogPath
        if (-not (Test-Path -LiteralPath $Parent -PathType Container)) {
            New-Item -ItemType Directory -Path $Parent -Force -ErrorAction Stop | Out-Null
        }
        $Body = ($script:LogLines -join [Environment]::NewLine) + [Environment]::NewLine
        [System.IO.File]::WriteAllText(
            $LogPath,
            $Body,
            [System.Text.UTF8Encoding]::new($false)
        )
        $LogWritten = $true
    }
    catch {
        $script:Warnings.Add("cannot write detailed log: $($_.Exception.Message)") | Out-Null
        $Overall = 'external_failure'
        $ExitCode = 3
    }

    $Artifacts = [string[]]@()
    if ($LogWritten) {
        $Artifacts = [string[]]@($LogReference)
    }
    $Payload = [ordered]@{
        ok = ($ExitCode -eq 0)
        overall = $Overall
        tools = $script:Tools
        log_file = if ($LogWritten) { $LogReference } else { $null }
        warnings = @($script:Warnings)
        artifacts = $Artifacts
    }
    [Console]::Out.WriteLine(($Payload | ConvertTo-Json -Depth 10 -Compress))
    exit $ExitCode
}

if ($AgentOnly -and $IncludeOptionalProduct) {
    $script:Warnings.Add('-AgentOnly and -IncludeOptionalProduct cannot be used together.') | Out-Null
    Complete-Check 'bad_args' 1
}

function Invoke-CommandLine {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Command
    )

    Add-Log "RUN  $Name"
    Add-Log ("CMD  " + ($Command -join ' '))
    $Executable = $Command[0]
    $Arguments = if ($Command.Count -gt 1) {
        $Command[1..($Command.Count - 1)]
    }
    else {
        @()
    }
    $Output = ''
    $Exit = $null
    try {
        $PreviousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Captured = & $Executable @Arguments 2>&1
            $Exit = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $PreviousPreference
        }
        if ($null -eq $Exit) {
            $Exit = 0
        }
        $Output = ($Captured | Out-String).TrimEnd()
    }
    catch {
        $Output = $_.Exception.Message
    }
    if ($Output) {
        Add-Log $Output
    }
    if ($Exit -eq 0) {
        Add-Tool $Name 'pass' 0 $Command
        $script:Passed += 1
        Add-Log "PASS $Name"
        return
    }
    Add-Tool $Name 'fail' $Exit $Command $Output
    $script:Failed += 1
    Add-Log "FAIL $Name exit=$Exit"
}

function Invoke-Predicate {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Check
    )

    Add-Log "RUN  $Name"
    $script:PredicateDetail = ''
    try {
        $Ok = [bool](& $Check)
    }
    catch {
        $Ok = $false
        $script:PredicateDetail = $_.Exception.Message
    }
    if ($script:PredicateDetail) {
        Add-Log $script:PredicateDetail
    }
    if ($Ok) {
        Add-Tool $Name 'pass' 0 @() $script:PredicateDetail
        $script:Passed += 1
        Add-Log "PASS $Name"
        return
    }
    Add-Tool $Name 'fail' 2 @() $script:PredicateDetail
    $script:Failed += 1
    Add-Log "FAIL $Name"
}

function Skip-Gate {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Reason,
        [string[]]$Command = @()
    )

    Add-Tool $Name 'skipped' $null $Command $Reason
    $script:Skipped += 1
    Add-Log "SKIP $Name - $Reason"
}

function Read-DeclaredGates {
    $ConfigPath = Join-Path $Root '.agents/repository.toml'
    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw '.agents/repository.toml does not exist'
    }
    $Items = [System.Collections.Generic.List[object]]::new()
    $Current = $null
    foreach ($RawLine in (Get-Content -LiteralPath $ConfigPath -Encoding UTF8)) {
        $Line = $RawLine.Trim()
        if (-not $Line -or $Line.StartsWith('#')) {
            continue
        }
        if ($Line -match '^\[\[([^]]+)\]\]$') {
            if ($null -ne $Current) {
                $Items.Add([pscustomobject]$Current) | Out-Null
            }
            $Current = if ($Matches[1] -eq 'gates') {
                [ordered]@{
                    id = $null
                    component = $null
                    classification = $null
                    optional = $false
                    command = @()
                }
            }
            else {
                $null
            }
            continue
        }
        if ($null -eq $Current -or $Line -notmatch '^([A-Za-z_]+)\s*=\s*(.+)$') {
            continue
        }
        $Key = $Matches[1]
        $Value = $Matches[2]
        switch ($Key) {
            'id' { $Current.id = [string]($Value | ConvertFrom-Json) }
            'component' { $Current.component = [string]($Value | ConvertFrom-Json) }
            'classification' { $Current.classification = [string]($Value | ConvertFrom-Json) }
            'optional' {
                if ($Value -notin @('true', 'false')) {
                    throw "invalid gate optional value: $Value"
                }
                $Current.optional = ($Value -eq 'true')
            }
            'command' {
                $ParsedCommand = ConvertFrom-Json -InputObject $Value
                $Current.command = [string[]]@($ParsedCommand)
            }
        }
    }
    if ($null -ne $Current) {
        $Items.Add([pscustomobject]$Current) | Out-Null
    }
    foreach ($Gate in $Items) {
        if (-not $Gate.id -or -not $Gate.classification -or $Gate.command.Count -eq 0) {
            throw 'each [[gates]] entry requires id, classification, and command'
        }
    }
    return $Items.ToArray()
}

function Normalize-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    $Value = $Path.Trim().Replace('\', '/')
    if ($Value.StartsWith('./')) {
        $Value = $Value.Substring(2)
    }
    return $Value.TrimEnd('/')
}

function Test-SafeRepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $false
    }
    $Parts = (Normalize-RepoPath $Path).Split('/')
    return -not ($Parts -contains '..')
}

function Test-ProductPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    $Value = Normalize-RepoPath $Path
    foreach ($ProductRoot in @('FrameWeb', 'FrameWebforJS', 'FramePrintPDF', 'FrameGConverter', 'tools')) {
        if ($Value -eq $ProductRoot -or $Value.StartsWith($ProductRoot + '/')) {
            return $true
        }
    }
    return $false
}

function Test-AllowedPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$Allowed = @()
    )
    $Value = Normalize-RepoPath $Path
    foreach ($Entry in $Allowed) {
        if ($Value -eq $Entry -or $Value.StartsWith($Entry + '/')) {
            return $true
        }
    }
    return $false
}

function Read-ScopeAllowlist {
    $Allowed = [System.Collections.Generic.List[string]]::new()
    $DirectEntries = @(
        $AllowProductPath.Split(';') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    foreach ($Entry in $DirectEntries) {
        if (-not (Test-SafeRepoPath $Entry)) {
            throw "allowlisted path is not repository-relative: $Entry"
        }
        $Allowed.Add((Normalize-RepoPath $Entry)) | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($ScopeAllowlist)) {
        $AllowlistPath = if ([System.IO.Path]::IsPathRooted($ScopeAllowlist)) {
            $ScopeAllowlist
        }
        else {
            Join-Path $Root $ScopeAllowlist
        }
        try {
            $Parsed = Get-Content -LiteralPath $AllowlistPath -Raw -Encoding UTF8 |
                ConvertFrom-Json
        }
        catch {
            throw "cannot read scope allowlist JSON: $($_.Exception.Message)"
        }
        $Entries = if ($Parsed -is [System.Array]) {
            @($Parsed)
        }
        elseif ($null -ne $Parsed.allowed_product_paths) {
            @($Parsed.allowed_product_paths)
        }
        else {
            throw 'scope allowlist must be a JSON array or contain allowed_product_paths'
        }
        foreach ($Entry in $Entries) {
            if ($Entry -isnot [string] -or -not (Test-SafeRepoPath $Entry)) {
                throw "allowlisted path is not repository-relative: $Entry"
            }
            $Allowed.Add((Normalize-RepoPath $Entry)) | Out-Null
        }
    }
    return [string[]]@($Allowed | Sort-Object -Unique)
}

function Invoke-GitPathList {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    try {
        $PreviousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Output = & git -c 'core.quotePath=false' -C $Root @Arguments 2>&1
            $Exit = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $PreviousPreference
        }
    }
    catch {
        return [pscustomobject]@{ ok = $false; paths = @(); error = $_.Exception.Message }
    }
    $StandardOutput = @(
        $Output | Where-Object {
            $_ -isnot [System.Management.Automation.ErrorRecord]
        }
    )
    $StandardError = @(
        $Output | Where-Object {
            $_ -is [System.Management.Automation.ErrorRecord]
        }
    )
    if ($StandardError.Count -gt 0) {
        Add-Log (($StandardError | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    }
    if ($Exit -ne 0) {
        return [pscustomobject]@{
            ok = $false
            paths = @()
            error = (($Output | Out-String).Trim())
        }
    }
    $Paths = @(
        $StandardOutput |
            ForEach-Object { Normalize-RepoPath ([string]$_) } |
            Where-Object { $_ }
    )
    return [pscustomobject]@{ ok = $true; paths = $Paths; error = '' }
}

function Invoke-ScopeIsolation {
    param([string[]]$Allowed = @())

    $Changed = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase
    )
    $Queries = [System.Collections.Generic.List[object]]::new()
    if (-not [string]::IsNullOrWhiteSpace($BaselineRef)) {
        $Queries.Add(@('diff', '--name-only', "$BaselineRef...HEAD", '--')) | Out-Null
    }
    $Queries.Add(@('diff', '--name-only', '--')) | Out-Null
    $Queries.Add(@('diff', '--cached', '--name-only', '--')) | Out-Null
    $Queries.Add(@('ls-files', '--others', '--exclude-standard', '--')) | Out-Null

    foreach ($Query in $Queries) {
        $Result = Invoke-GitPathList ([string[]]$Query)
        if (-not $Result.ok) {
            Add-Tool 'scope-isolation' 'fail' 2 @('git', '-C', $Root) $Result.error
            $script:Failed += 1
            Add-Log "FAIL scope-isolation - $($Result.error)"
            return
        }
        foreach ($Path in $Result.paths) {
            $Changed.Add($Path) | Out-Null
        }
    }

    $ChangedPaths = @($Changed | Sort-Object)
    $ProductPaths = @($ChangedPaths | Where-Object { Test-ProductPath $_ })
    $Disallowed = @(
        $ProductPaths | Where-Object { -not (Test-AllowedPath $_ $Allowed) }
    )
    $Status = if ($Disallowed.Count -eq 0) { 'pass' } else { 'fail' }
    $Detail = if ($Disallowed.Count -eq 0) {
        "checked $($ChangedPaths.Count) changed path(s); product scope is isolated"
    }
    else {
        'unapproved product changes: ' + ($Disallowed -join ', ')
    }
    $Extra = @{
        baseline_ref = if ($BaselineRef) { $BaselineRef } else { $null }
        changed_paths = $ChangedPaths
        product_paths = $ProductPaths
        allowed_product_paths = @($Allowed)
        disallowed_product_paths = $Disallowed
    }
    $ScopeExit = if ($Status -eq 'pass') { 0 } else { 2 }
    Add-Tool 'scope-isolation' $Status $ScopeExit @() $Detail $Extra
    Add-Log "SCOPE $Detail"
    if ($Status -eq 'pass') {
        $script:Passed += 1
    }
    else {
        $script:Failed += 1
    }
}

function Test-LiveScriptReferences {
    $Excluded = '\\(logs|checkpoints|research|reviews|plans)\\'
    $Patterns = @(
        '\.agents/skills/[A-Za-z0-9_./-]+\.(py|ps1|sh)',
        '\.agents/check\.(ps1|sh)'
    )
    $Missing = [System.Collections.Generic.List[string]]::new()
    $Markdown = Get-ChildItem -LiteralPath (Join-Path $Root '.agents') -Recurse -File -Filter '*.md' |
        Where-Object { $_.FullName -notmatch $Excluded }
    $RootDocs = @('AGENTS.md', 'README.md') |
        ForEach-Object { Join-Path $Root $_ } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { Get-Item -LiteralPath $_ }
    foreach ($File in @($Markdown) + @($RootDocs)) {
        foreach ($Pattern in $Patterns) {
            foreach ($Match in (Select-String -LiteralPath $File.FullName -Pattern $Pattern -AllMatches)) {
                foreach ($Value in $Match.Matches.Value) {
                    $Candidate = Join-Path $Root ($Value.Replace('/', '\'))
                    if (-not (Test-Path -LiteralPath $Candidate -PathType Leaf)) {
                        $Missing.Add("$($File.FullName): $Value")
                    }
                }
            }
        }
    }
    if ($Missing.Count -gt 0) {
        $script:PredicateDetail = ($Missing | Sort-Object -Unique) -join '; '
        return $false
    }
    return $true
}

function Test-ForeignReferences {
    $Targets = @(
        (Join-Path $Root '.agents/STATE.md'),
        (Join-Path $Root '.agents/docs/DESIGN.md'),
        (Join-Path $Root 'PROGRESS.md')
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
    $Pattern = 'tick.?replay|duckdb|minute-context|daily-context|stocks_daily|SMA25|SMA200|src/tickreplay'
    $Matches = @(Select-String -LiteralPath $Targets -Pattern $Pattern -AllMatches -CaseSensitive:$false)
    if ($Matches.Count -gt 0) {
        $script:PredicateDetail = @(
            $Matches | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
        ) -join '; '
        return $false
    }
    return $true
}

function Test-NoAgentCaches {
    $CheckedAgentDir = Join-Path $Root '.agents'
    $Pyc = @(Get-ChildItem -LiteralPath $CheckedAgentDir -Recurse -Force -File -Filter '*.pyc')
    $CacheDirs = @(Get-ChildItem -LiteralPath $CheckedAgentDir -Recurse -Force -Directory -Filter '__pycache__')
    if ($Pyc.Count -gt 0 -or $CacheDirs.Count -gt 0) {
        $script:PredicateDetail = @($Pyc.FullName) + @($CacheDirs.FullName) -join '; '
        return $false
    }
    return $true
}

try {
    $DeclaredGates = @(Read-DeclaredGates)
    $AllowedProductPaths = @(Read-ScopeAllowlist)
}
catch {
    $script:Warnings.Add($_.Exception.Message) | Out-Null
    Complete-Check 'bad_args' 1
}
if (-not [string]::IsNullOrWhiteSpace($BaselineRef)) {
    $BaselineCheck = Invoke-GitPathList @(
        'rev-parse', '--verify', "$BaselineRef^{commit}"
    )
    if (-not $BaselineCheck.ok) {
        $script:Warnings.Add("invalid -BaselineRef: $BaselineRef") | Out-Null
        Complete-Check 'bad_args' 1
    }
}

$PythonPrefix = @(
    'uv', 'run', '--project', 'FrameWeb', '--locked', '--extra', 'dev', 'python'
)
$AgentGateCommands = [ordered]@{
    'agent pytest' = $PythonPrefix + @('-m', 'pytest', '.agents/tests', '-q')
    'repository detector' = $PythonPrefix + @('.agents/skills/init/detect_stack.py', '--project-root', '.')
    'STATE contract' = $PythonPrefix + @('.agents/skills/_shared/validate_doc.py', '--contract', 'state-doc', '--file', '.agents/STATE.md')
    'DESIGN contract' = $PythonPrefix + @('.agents/skills/_shared/validate_doc.py', '--contract', 'design-doc', '--file', '.agents/docs/DESIGN.md')
    'plan contract' = $PythonPrefix + @('.agents/skills/_shared/validate_doc.py', '--contract', 'plan-doc', '--file', '.agents/docs/plans/agents-repository-alignment.md')
    'live script references resolve' = @('internal:Test-LiveScriptReferences')
    'foreign project references absent' = @('internal:Test-ForeignReferences')
    'agent caches absent' = @('internal:Test-NoAgentCaches')
    'git diff --check' = @('git', 'diff', '--check')
}

Push-Location $Root
try {
    Invoke-ScopeIsolation $AllowedProductPaths

    if ($ListGates) {
        foreach ($Entry in $AgentGateCommands.GetEnumerator()) {
            Add-Tool $Entry.Key 'listed' $null ([string[]]$Entry.Value) 'dry-run; not executed'
            Add-Log ("LIST $($Entry.Key): " + ($Entry.Value -join ' '))
        }
        if ($AgentOnly) {
            Skip-Gate 'product gates' 'AgentOnly requested'
        }
        else {
            foreach ($Gate in @($DeclaredGates | Where-Object { $_.classification -eq 'product' })) {
                $GateDetail = if ($Gate.optional) {
                    'optional product gate'
                }
                else {
                    'required product gate'
                }
                Add-Tool ("product: " + $Gate.id) 'listed' $null ([string[]]$Gate.command) $GateDetail
                Add-Log ("LIST product: $($Gate.id): " + ($Gate.command -join ' '))
            }
        }
    }
    else {
        Invoke-CommandLine 'agent pytest' ([string[]]$AgentGateCommands['agent pytest'])
        Invoke-CommandLine 'repository detector' ([string[]]$AgentGateCommands['repository detector'])
        Invoke-CommandLine 'STATE contract' ([string[]]$AgentGateCommands['STATE contract'])
        Invoke-CommandLine 'DESIGN contract' ([string[]]$AgentGateCommands['DESIGN contract'])
        Invoke-CommandLine 'plan contract' ([string[]]$AgentGateCommands['plan contract'])
        Invoke-Predicate 'live script references resolve' { Test-LiveScriptReferences }
        Invoke-Predicate 'foreign project references absent' { Test-ForeignReferences }
        Invoke-Predicate 'agent caches absent' { Test-NoAgentCaches }
        Invoke-CommandLine 'git diff --check' @('git', 'diff', '--check')

        if ($AgentOnly) {
            Skip-Gate 'product gates' 'AgentOnly requested'
        }
        else {
            foreach ($Gate in @($DeclaredGates | Where-Object { $_.classification -eq 'product' })) {
                if ($Gate.optional -and -not $IncludeOptionalProduct) {
                    Skip-Gate ("product: " + $Gate.id) 'optional; use -IncludeOptionalProduct' ([string[]]$Gate.command)
                    continue
                }
                Invoke-CommandLine ("product: " + $Gate.id) ([string[]]$Gate.command)
            }
        }
    }
}
finally {
    Pop-Location
}

$ProductGates = @($DeclaredGates | Where-Object { $_.classification -eq 'product' })
if (-not $AgentOnly -and $ProductGates.Count -eq 0) {
    if ($AllowNoGates) {
        $script:Warnings.Add('no product gates declared; accepted by -AllowNoGates') | Out-Null
    }
    else {
        Complete-Check 'no_gates' 2
    }
}
if ($script:Failed -gt 0) {
    Complete-Check 'fail' 2
}
Complete-Check 'pass' 0
