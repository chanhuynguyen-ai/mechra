param([string]$ProjectRoot)
. (Join-Path $PSScriptRoot 'common.ps1')
if (!$ProjectRoot) { $ProjectRoot = Split-Path -Parent $PSScriptRoot }
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path
$python = Join-Path $ProjectRoot 'src\AgentService\.venv\Scripts\python.exe'
Stop-OwnedAgent $python
Write-Host 'The verified agent for this project is stopped.'
