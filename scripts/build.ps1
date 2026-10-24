param([string]$Dotnet = 'dotnet', [switch]$Publish)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    & $Dotnet restore Aegis.sln
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
    & $Dotnet build Aegis.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    & $Dotnet run --project tests/Aegis.Tests -c Release --no-build -- tests/Aegis.Tests/bin/test-data
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
    if ($Publish) {
        & $Dotnet publish src/Aegis.Desktop -c Release -r win-x64 --self-contained true --no-restore -o artifacts/portable
        if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed' }
        & $Dotnet publish src/Aegis.Collector -c Release -r win-x64 --self-contained true --no-restore -o artifacts/portable/collector
        if ($LASTEXITCODE -ne 0) { throw 'Collector publish failed' }
    }
} finally { Pop-Location }
