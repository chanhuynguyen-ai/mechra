param([switch]$Foreground)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Split-Path -Parent $PSScriptRoot
$service = Join-Path $root 'src\AgentService'
$python = Join-Path $service '.venv\Scripts\python.exe'
if (!$Foreground) { & (Join-Path $PSScriptRoot 'setup-and-run.ps1') -AgentOnly; return }
if (!(Test-Path -LiteralPath $python)) { throw 'Run .\scripts\setup-and-run.ps1 -AgentOnly first.' }
Push-Location $service
try { & $python -m uvicorn app.main:app --host 127.0.0.1 --port 8765; if ($LASTEXITCODE -ne 0) { throw 'Agent exited with an error.' } }
finally { Pop-Location }
