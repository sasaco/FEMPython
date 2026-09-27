$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DebugEventLogger.csproj'
$manifest = [xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source.extension.vsixmanifest') -Raw -Encoding UTF8)
$identity = $manifest.SelectSingleNode('//*[local-name()="Identity"]')
$version = $identity.GetAttribute('Version')
$intermediate = "obj/Debug/net472/package-$($version.Replace('.', '_'))/"
$packageName = "DebugEventLogger-$version.vsix"

$buildOutput = & dotnet build $project -c Debug -t:Rebuild --nologo `
    "-p:IntermediateOutputPath=$intermediate" `
    "-p:TargetVsixContainerName=$packageName"
$buildExitCode = $LASTEXITCODE
$buildOutput | ForEach-Object { Write-Host $_ }
if ($buildExitCode -ne 0) {
    throw "VSIX build failed with exit code $buildExitCode."
}

$package = Join-Path $PSScriptRoot "bin/Debug/net472/$packageName"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entry = $archive.GetEntry('extension.vsixmanifest')
    $reader = [IO.StreamReader]::new($entry.Open())
    try {
        $packagedManifest = [xml]$reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}
finally {
    $archive.Dispose()
}

$packagedVersion = $packagedManifest.SelectSingleNode('//*[local-name()="Identity"]').GetAttribute('Version')
if ($packagedVersion -ne $version) {
    throw "Packaged manifest version $packagedVersion differs from source version $version."
}

Write-Output $package
