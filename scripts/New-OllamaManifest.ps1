<#
Génère src/Aegis.Infrastructure/Resources/ollama-engine.json : liste des fichiers CPU du moteur Ollama
officiel (sans CUDA, Vulkan ni MLX) avec leur SHA-256, calculés depuis l'archive officielle.
L'archive entière est comparée au condensat publié par GitHub avant toute écriture.
#>
param(
    [string]$Version = '0.35.1',
    [string]$ZipPath = '',
    [string]$Output = (Join-Path (Split-Path -Parent $PSScriptRoot) 'src/Aegis.Infrastructure/Resources/ollama-engine.json')
)
$ErrorActionPreference = 'Stop'
$asset = 'ollama-windows-amd64.zip'
$release = Invoke-RestMethod "https://api.github.com/repos/ollama/ollama/releases/tags/v$Version" -Headers @{ 'User-Agent' = 'aegis-build' }
$meta = $release.assets | Where-Object name -eq $asset
if (-not $meta) { throw "Asset $asset introuvable pour v$Version" }
$official = $meta.digest -replace '^sha256:', ''
if (-not $ZipPath) {
    $ZipPath = Join-Path ([IO.Path]::GetTempPath()) "aegis-$asset"
    if (-not (Test-Path $ZipPath)) { Invoke-WebRequest $meta.browser_download_url -OutFile $ZipPath -UseBasicParsing }
}
$actual = (Get-FileHash $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $official.ToLowerInvariant()) { throw "Condensat de l'archive différent du condensat officiel ($actual / $official)" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $files = foreach ($e in $zip.Entries) {
        if ($e.FullName.EndsWith('/')) { continue }
        if ($e.FullName -match '^lib/ollama/(cuda_|vulkan|mlx|rocm)') { continue }
        $sha = [Security.Cryptography.SHA256]::Create()
        $s = $e.Open()
        try { $hash = ([BitConverter]::ToString($sha.ComputeHash($s)) -replace '-', '').ToLowerInvariant() } finally { $s.Dispose(); $sha.Dispose() }
        [pscustomobject]@{ name = $e.FullName; size = $e.Length; compressedSize = $e.CompressedLength; sha256 = $hash }
    }
} finally { $zip.Dispose() }
$manifest = [ordered]@{
    id = 'ollama-engine'; version = $Version
    source = $meta.browser_download_url
    archiveSize = $meta.size; archiveSha256 = $official.ToLowerInvariant()
    files = @($files | Sort-Object { $_.name })
}
$json = $manifest | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($Output, $json, (New-Object Text.UTF8Encoding($false)))
"{0} fichiers, {1:N0} octets décompressés, {2:N0} octets compressés" -f $manifest.files.Count, ($manifest.files | Measure-Object size -Sum).Sum, ($manifest.files | Measure-Object compressedSize -Sum).Sum