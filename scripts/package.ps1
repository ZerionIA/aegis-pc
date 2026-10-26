param([string]$Dotnet='dotnet',[string]$Makensis='makensis',[string]$Output='artifacts/release')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
[xml]$props=Get-Content (Join-Path $root 'Directory.Build.props')
$releaseVersion=$props.Project.PropertyGroup.VersionPrefix
if($props.Project.PropertyGroup.VersionSuffix){$releaseVersion+='-'+$props.Project.PropertyGroup.VersionSuffix}
$setupName="Aegis-$releaseVersion-Setup.exe"
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
 & $Dotnet publish src/Aegis.Updater -c Release -r win-x64 --self-contained true --no-restore -o (Join-Path $target 'updater')
 if($LASTEXITCODE -ne 0){throw 'Updater publish failed'}
 Copy-Item LICENSE,README.md,THIRD-PARTY-NOTICES.md,PRIVACY.md -Destination $target
 Copy-Item third-party -Destination (Join-Path $target 'notices') -Recurse
 $tools=Join-Path $target 'tools';New-Item -ItemType Directory -Force $tools | Out-Null
 $pm=Join-Path $tools 'PresentMon.exe'
 Invoke-WebRequest 'https://github.com/GameTechDev/PresentMon/releases/download/v2.6.0/PresentMon-2.6.0-x64.exe' -OutFile $pm
 if((Get-FileHash $pm -Algorithm SHA256).Hash -ne 'B2A706BC6AD475749E3B7E3409263AA1E6906D45BDCF993F6DBC0F660188F1AF'){throw 'PresentMon checksum mismatch'}
 $prerequisites=Join-Path $target 'prerequisites';New-Item -ItemType Directory -Force $prerequisites | Out-Null
 $pawn=Join-Path $prerequisites 'PawnIO_setup.exe'
 Invoke-WebRequest 'https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe' -OutFile $pawn
 if((Get-FileHash $pawn -Algorithm SHA256).Hash -ne '1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032'){throw 'PawnIO checksum mismatch'}
 $signature=Get-AuthenticodeSignature $pawn
 if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'CN=namazso.eu'){throw 'PawnIO official signature invalid'}
 & $Makensis "/DPACKAGE_DIR=$target" "/DAPP_VERSION=$releaseVersion" (Join-Path $root 'installer/aegis.nsi')
 if($LASTEXITCODE -ne 0){throw 'Installer failed'}
 $setup=Join-Path (Join-Path $root 'artifacts') $setupName
 $hash=(Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
 "$hash  $setupName" | Set-Content (Join-Path $root 'artifacts/SHA256SUMS.txt') -Encoding utf8
}finally{Pop-Location}
