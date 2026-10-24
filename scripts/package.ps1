param([string]$Dotnet='dotnet',[string]$Makensis='makensis',[string]$Output='artifacts/release')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$target=[IO.Path]::GetFullPath((Join-Path $root $Output))
$allowed=[IO.Path]::GetFullPath((Join-Path $root 'artifacts'))+[IO.Path]::DirectorySeparatorChar
if(-not $target.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be inside artifacts/'}
if(Test-Path $target){throw 'Output already exists. Choose a new empty output path.'}
Push-Location $root
try{
 & $Dotnet restore Aegis.sln --locked-mode
 if($LASTEXITCODE -ne 0){throw 'Restore failed'}
 & $Dotnet build Aegis.sln -c Release --no-restore
 if($LASTEXITCODE -ne 0){throw 'Build failed'}
 & $Dotnet run --project tests/Aegis.Tests -c Release --no-build -- artifacts/package-tests
 if($LASTEXITCODE -ne 0){throw 'Tests failed'}
 & $Dotnet publish src/Aegis.Desktop -c Release -r win-x64 --self-contained true --no-restore -o $target
 if($LASTEXITCODE -ne 0){throw 'Desktop publish failed'}
 & $Dotnet publish src/Aegis.Collector -c Release -r win-x64 --self-contained true --no-restore -o (Join-Path $target 'collector')
 if($LASTEXITCODE -ne 0){throw 'Collector publish failed'}
 Copy-Item LICENSE,README.md,THIRD-PARTY-NOTICES.md -Destination $target
 Copy-Item third-party -Destination (Join-Path $target 'notices') -Recurse
 $tools=Join-Path $target 'tools';New-Item -ItemType Directory -Force $tools | Out-Null
 $pm=Join-Path $tools 'PresentMon.exe'
 Invoke-WebRequest 'https://github.com/GameTechDev/PresentMon/releases/download/v2.6.0/PresentMon-2.6.0-x64.exe' -OutFile $pm
 if((Get-FileHash $pm -Algorithm SHA256).Hash -ne 'B2A706BC6AD475749E3B7E3409263AA1E6906D45BDCF993F6DBC0F660188F1AF'){throw 'PresentMon checksum mismatch'}
 & $Makensis "/DPACKAGE_DIR=$target" (Join-Path $root 'installer/aegis.nsi')
 if($LASTEXITCODE -ne 0){throw 'Installer failed'}
 $setup=Join-Path $root 'artifacts/Aegis-0.2.0-beta.2-Setup.exe'
 $hash=(Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
 "$hash  Aegis-0.2.0-beta.2-Setup.exe" | Set-Content (Join-Path $root 'artifacts/SHA256SUMS.txt') -Encoding utf8
}finally{Pop-Location}
