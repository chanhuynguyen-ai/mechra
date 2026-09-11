Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Step([string]$Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }
function Find-Python {
    foreach ($candidate in @(@{File='py'; Args=@('-3.11')}, @{File='py'; Args=@('-3')}, @{File='python'; Args=@()})) {
        try {
            $command = Get-Command $candidate.File -ErrorAction Stop
            $candidateArgs = $candidate.Args
            & $command.Source @candidateArgs -c 'import sys; sys.exit(0 if sys.version_info >= (3,11) else 1)' *> $null
            if ($LASTEXITCODE -eq 0) { return $candidate }
        } catch { }
    }
    throw 'Python 3.11+ was not found. Install Python and reopen PowerShell.'
}
function Find-SolidWorksApiDir([string]$ExplicitPath) {
    if (!$ExplicitPath) { $ExplicitPath = $env:SOLIDWORKS_API_DIR }
    if ($ExplicitPath) {
        foreach ($name in @('sldworks','swconst','swpublished')) {
            if (!(Test-Path -LiteralPath (Join-Path $ExplicitPath "SolidWorks.Interop.$name.dll"))) {
                throw "Missing SOLIDWORKS API assembly in: $ExplicitPath"
            }
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }
    $root = Join-Path $env:ProgramFiles 'SOLIDWORKS Corp'
    if (Test-Path -LiteralPath $root) {
        $pick = Get-ChildItem -LiteralPath $root -Filter 'SolidWorks.Interop.sldworks.dll' -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.DirectoryName -match '[\\/]api[\\/]redist$' } | Select-Object -First 1
        if ($pick) { return Find-SolidWorksApiDir $pick.DirectoryName }
    }
    throw 'SOLIDWORKS API not found. Pass -SolidWorksApiDir with your api\redist folder.'
}
function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found -and (Test-Path -LiteralPath $found)) { return $found }
    }
    try { return (Get-Command msbuild -ErrorAction Stop).Source } catch { }
    throw 'MSBuild not found. Install Visual Studio Build Tools, .NET desktop tools and .NET Framework 4.8 targeting pack.'
}
function Test-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
function Get-AgentHealth {
    try { return Invoke-RestMethod -Uri 'http://127.0.0.1:8765/health' -TimeoutSec 2 } catch { return $null }
}
function Test-OwnedAgentProcess($Process, [string]$PythonPath) {
    if (!$Process -or !$Process.CommandLine) { return $false }
    $command = [string]$Process.CommandLine
    $escapedPath = [regex]::Escape($PythonPath)
    return $command -match ('^\s*"?' + $escapedPath + '"?\s') -and
        $command -match '\s-m\s+uvicorn\s+app\.main:app\b' -and
        $command -match '--host\s+127\.0\.0\.1\b' -and $command -match '--port\s+8765\b'
}
function Stop-OwnedAgent([string]$PythonPath) {
    $listeners = @(Get-NetTCPConnection -LocalPort 8765 -State Listen -ErrorAction SilentlyContinue)
    if ($listeners.Count -eq 0) { return }
    $ownerIds = @($listeners | Select-Object -ExpandProperty OwningProcess -Unique)
    if ($ownerIds.Count -ne 1) { throw 'Port 8765 has ambiguous listeners. Inspect them manually.' }
    $agentProcess = Get-CimInstance Win32_Process -Filter "ProcessId = $($ownerIds[0])"
    if (!(Test-OwnedAgentProcess $agentProcess $PythonPath)) {
        throw 'Port 8765 belongs to an unverified process or another Mechra folder. Close that agent console before retrying.'
    }
    # Check process creation time again so a reused PID cannot stop a different process.
    $current = Get-CimInstance Win32_Process -Filter "ProcessId = $($ownerIds[0])"
    if (!$current -or $current.CreationDate -ne $agentProcess.CreationDate -or !(Test-OwnedAgentProcess $current $PythonPath)) {
        throw 'Agent process identity changed. Retry after inspecting port 8765.'
    }
    Stop-Process -Id $ownerIds[0] -ErrorAction Stop
    for ($i=0; $i -lt 20; $i++) {
        if (!(Get-NetTCPConnection -LocalPort 8765 -State Listen -ErrorAction SilentlyContinue)) { return }
        Start-Sleep -Milliseconds 200
    }
    throw 'Agent has not released port 8765 yet.'
}
function Invoke-AgentJson([string]$Path, $Payload) {
    $json = ConvertTo-Json -InputObject $Payload -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    return Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:8765$Path" -ContentType 'application/json; charset=utf-8' -Body $bytes -TimeoutSec 15
}
