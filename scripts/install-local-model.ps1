$ErrorActionPreference = 'Stop'
$portableRoot = Split-Path -Parent $PSScriptRoot
$engine = Join-Path $portableRoot 'tools/ollama/ollama.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw 'Moteur Ollama absent du dossier Aegis.' }
$env:OLLAMA_HOST = '127.0.0.1:11539'
$env:OLLAMA_MODELS = Join-Path $portableRoot 'models'
$server = $null
try {
    try { Invoke-RestMethod 'http://127.0.0.1:11539/api/version' -TimeoutSec 2 | Out-Null }
    catch {
        $server = Start-Process -FilePath $engine -ArgumentList 'serve' -WindowStyle Hidden -PassThru
        $ready = $false
        for ($i=0; $i -lt 20; $i++) {
            try { Invoke-RestMethod 'http://127.0.0.1:11539/api/version' -TimeoutSec 2 | Out-Null; $ready=$true; break }
            catch { Start-Sleep -Milliseconds 500 }
        }
        if (-not $ready) { throw 'Moteur local non disponible.' }
    }
    Write-Host 'Téléchargement facultatif Qwen3.5 2B (environ 2,7 Go).'
    Invoke-RestMethod 'http://127.0.0.1:11539/api/pull' -Method Post -ContentType application/json -Body '{"model":"qwen3.5:2b","stream":false}' -TimeoutSec 1800 | Out-Null
    Write-Host 'Modèle prêt. Dans Aegis, Réglages → Activer le pack IA local.'
} finally { if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id } }
