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
function Read-AgentCommand([string]$CommandLine) {
    # Recognize only the exact launcher form shipped with Mechra. Do not accept
    # substrings, other apps, reload/workers or trailing executable arguments.
    $match = [regex]::Match($CommandLine, '^\s*(?:"(?<exe>[^"\r\n]+)"|(?<exe>[^\s"]+))\s+(?<arguments>.*?)\s*$')
    if (!$match.Success) { return $null }
    $tail = $match.Groups['arguments'].Value
    if ($tail -cnotmatch '^-m\s+uvicorn\s+app\.main:app\s+--host\s+127\.0\.0\.1\s+--port\s+8765$') { return $null }
    return $match.Groups['exe'].Value
}
function Test-OwnedAgentProcess($Process, [string]$PythonPath) {
    if (!$Process -or !$Process.CommandLine -or !$Process.ExecutablePath) { return $false }
    $exe = Read-AgentCommand ([string]$Process.CommandLine)
    if (!$exe) { return $false }
    # Win32_Process.ExecutablePath is authoritative; argv[0] may be relative.
    return [string]::Equals([string]$Process.ExecutablePath, $PythonPath, [StringComparison]::OrdinalIgnoreCase)
}
function Get-AgentOwnership($Process, $Parent, [string]$PythonPath) {
    if (!$Process -or !$Process.CreationDate) { return $null }
    if (Test-OwnedAgentProcess $Process $PythonPath) {
        return [pscustomobject]@{ kind='direct'; owner_id=$Process.ProcessId; owner_created=$Process.CreationDate; parent_id=$null; parent_created=$null }
    }
    # Windows venv's redirector can launch a base interpreter as its child.
    # Prove the immediate parent's project path, exact argv and lifetime too.
    if (!$Parent -or !$Parent.CreationDate -or !$Process.ExecutablePath -or
        !(Read-AgentCommand ([string]$Process.CommandLine)) -or
        !(Test-OwnedAgentProcess $Parent $PythonPath)) { return $null }
    $leaf = ([string]$Process.ExecutablePath -split '[\\/]')[-1]
    if ($leaf -notmatch '^python(?:\d+(?:\.\d+)*)?t?\.exe$' -or
        $Process.ParentProcessId -ne $Parent.ProcessId -or
        $Parent.CreationDate -gt $Process.CreationDate) { return $null }
    return [pscustomobject]@{ kind='venv-child'; owner_id=$Process.ProcessId; owner_created=$Process.CreationDate; parent_id=$Parent.ProcessId; parent_created=$Parent.CreationDate }
}
function Get-AgentListeners {
    # An inspection failure is not evidence that the port is free.
    return @(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object { $_.LocalPort -eq 8765 })
}
function Get-AgentProcessPair([int]$OwnerId) {
    $process = Get-CimInstance Win32_Process -Filter "ProcessId = $OwnerId" -ErrorAction Stop
    $parent = $null
    if ($process -and $process.ParentProcessId) {
        $parent = Get-CimInstance Win32_Process -Filter "ProcessId = $($process.ParentProcessId)" -ErrorAction Stop
    }
    return [pscustomobject]@{ process=$process; parent=$parent }
}
function Stop-OwnedAgent([string]$PythonPath) {
    $listeners = @(Get-AgentListeners)
    if ($listeners.Count -eq 0) { return }
    $ownerIds = @($listeners | Select-Object -ExpandProperty OwningProcess -Unique)
    if ($ownerIds.Count -ne 1) { throw 'Port 8765 has ambiguous listeners. Run scripts\doctor.ps1 to inspect them.' }
    $pair = Get-AgentProcessPair $ownerIds[0]
    $proof = Get-AgentOwnership $pair.process $pair.parent $PythonPath
    if (!$proof) {
        throw "Cannot verify the owner of port 8765 (PID $($ownerIds[0])) for $PythonPath. Run scripts\doctor.ps1 -PreviousProjectRoot with the old project path. No process was stopped."
    }
    # Hold the process handle, then recheck both identities and port ownership.
    $handle = Get-Process -Id $ownerIds[0] -ErrorAction Stop
    try {
        $null = $handle.Handle
        $currentPair = Get-AgentProcessPair $ownerIds[0]
        $current = Get-AgentOwnership $currentPair.process $currentPair.parent $PythonPath
        $newIds = @((Get-AgentListeners) | Select-Object -ExpandProperty OwningProcess -Unique)
        if (!$current -or $current.owner_created -ne $proof.owner_created -or
            $current.parent_id -ne $proof.parent_id -or $current.parent_created -ne $proof.parent_created -or
            $newIds.Count -ne 1 -or $newIds[0] -ne $proof.owner_id) {
            throw 'Agent identity or listener changed. No process was stopped; rerun diagnostics.'
        }
        Write-Host "Stopping verified Mechra agent PID $($proof.owner_id) ($($proof.kind))."
        $handle.Kill()
    } finally { $handle.Dispose() }
    for ($i=0; $i -lt 20; $i++) {
        if (@(Get-AgentListeners).Count -eq 0) { return }
        Start-Sleep -Milliseconds 200
    }
    throw 'Agent has not released port 8765 yet. Run scripts\doctor.ps1.'
}
function Test-MatchingAgent($Health, [string]$Version, [string]$Build) {
    return $Health -and $Health.PSObject.Properties['service'] -and $Health.service -eq 'mechra-agent' -and
        $Health.PSObject.Properties['version'] -and $Health.version -eq $Version -and
        $Health.PSObject.Properties['build_id'] -and $Health.build_id -eq $Build
}
function Get-AddinRegistrations {
    $subpath = 'Software\Classes\CLSID\{8B107B9C-15E8-42F7-A338-D58DA2F266B2}\InprocServer32'
    foreach ($hive in @([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryHive]::LocalMachine)) {
        $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($hive, [Microsoft.Win32.RegistryView]::Registry64)
        try {
            $key = $base.OpenSubKey($subpath)
            if (!$key) { continue }
            try {
                $codebase = [string]$key.GetValue('CodeBase', '')
                $path = $codebase
                if ($codebase) { try { $uri = [uri]$codebase; if ($uri.IsFile) { $path = $uri.LocalPath } } catch { } }
                [pscustomobject]@{ scope=$hive.ToString(); assembly=[string]$key.GetValue('Assembly',''); codebase=$codebase; path=$path }
            } finally { $key.Dispose() }
        } finally { $base.Dispose() }
    }
}
function Assert-AddinRegistration([string]$DllPath, [string]$Version) {
    $entries = @(Get-AddinRegistrations)
    if ($entries.Count -eq 0) { throw 'The 64-bit Mechra COM registration is missing.' }
    foreach ($entry in $entries) {
        if (![string]::Equals($entry.path, $DllPath, [StringComparison]::OrdinalIgnoreCase)) {
            throw "A Mechra registration points to another DLL ($($entry.scope)): $($entry.path). Run scripts\doctor.ps1."
        }
        $expectedAssembly = [Reflection.AssemblyName]::GetAssemblyName($DllPath).FullName
        if ($entry.assembly -ne $expectedAssembly) { throw "Registered assembly identity differs from the built DLL: $($entry.assembly)" }
    }
    $builtVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($DllPath).ProductVersion
    if ($builtVersion -ne $Version) { throw "Built DLL is version $builtVersion; expected $Version. Rebuild before registering." }
}
function Invoke-AgentJson([string]$Path, $Payload) {
    $json = ConvertTo-Json -InputObject $Payload -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    return Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:8765$Path" -ContentType 'application/json; charset=utf-8' -Body $bytes -TimeoutSec 15
}
