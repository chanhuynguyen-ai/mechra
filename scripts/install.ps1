param(
    [string]$PreviousProjectRoot,
    [ValidateSet('Debug','Release')][string]$Configuration='Debug',
    [string]$SolidWorksApiDir,
    [switch]$SkipRegister,
    [switch]$AgentOnly
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runtime = Join-Path $root '.runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$log = Join-Path $runtime ('install-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$transcribing = $false
try {
    Start-Transcript -LiteralPath $log | Out-Null
    $transcribing = $true
    & (Join-Path $PSScriptRoot 'setup-and-run.ps1') @PSBoundParameters
} catch {
    Write-Host ('Setup stopped: ' + $_.Exception.Message) -ForegroundColor Red
    throw
} finally {
    try { & (Join-Path $PSScriptRoot 'doctor.ps1') -PreviousProjectRoot $PreviousProjectRoot -Configuration $Configuration }
    catch { Write-Host ('Diagnostic report could not be written: ' + $_.Exception.Message) -ForegroundColor Yellow }
    if ($transcribing) { Stop-Transcript | Out-Null }
    Write-Host "Setup log: $log"
}
