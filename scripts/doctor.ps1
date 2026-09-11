param(
    [string]$PreviousProjectRoot,
    [ValidateSet('Debug','Release')][string]$Configuration='Debug',
    [string]$OutputPath
)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Split-Path -Parent $PSScriptRoot
if (!$OutputPath) { $OutputPath = Join-Path $root '.runtime\diagnostics.json' }
$notes = New-Object 'System.Collections.Generic.List[string]'
$processes = @()
$registrations = @()
try {
    $ownerIds = @((Get-AgentListeners) | Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($ownerId in $ownerIds) {
        $pair = Get-AgentProcessPair $ownerId
        $currentPython = Join-Path $root 'src\AgentService\.venv\Scripts\python.exe'
        $currentProof = Get-AgentOwnership $pair.process $pair.parent $currentPython
        $previousProof = $null
        if ($PreviousProjectRoot) {
            $previous = (Resolve-Path -LiteralPath $PreviousProjectRoot -ErrorAction Stop).Path
            $previousProof = Get-AgentOwnership $pair.process $pair.parent (Join-Path $previous 'src\AgentService\.venv\Scripts\python.exe')
        }
        $processes += [pscustomobject]@{
            listener_pid=$ownerId
            process=($pair.process | Select-Object ProcessId,ParentProcessId,CreationDate,ExecutablePath,CommandLine)
            parent=($pair.parent | Select-Object ProcessId,ParentProcessId,CreationDate,ExecutablePath,CommandLine)
            current_project_ownership=$currentProof
            previous_project_ownership=$previousProof
        }
    }
} catch { $notes.Add('Listener inspection: ' + $_.Exception.Message) }
try { $registrations = @(Get-AddinRegistrations) } catch { $notes.Add('Registration inspection: ' + $_.Exception.Message) }
$dll = Join-Path $root "src\SolidWorksAddin\bin\$Configuration\SwCursor.SolidWorksAddin.dll"
$expectedVersion = ([IO.File]::ReadAllText((Join-Path $root 'VERSION'))).Trim()
$binaryVersion = $null
$registrationMatches = $false
if (Test-Path -LiteralPath $dll) {
    try {
        $binaryVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion
        Assert-AddinRegistration $dll $expectedVersion
        $registrationMatches = $true
    } catch { $notes.Add($_.Exception.Message) }
} else { $notes.Add('The add-in has not been built in this folder.') }
$report = [ordered]@{
    captured_utc=[DateTime]::UtcNow.ToString('o')
    project_root=$root
    expected_version=$expectedVersion
    powershell_version=$PSVersionTable.PSVersion.ToString()
    process_is_64bit=[Environment]::Is64BitProcess
    administrator=(Test-Admin)
    health=(Get-AgentHealth)
    listeners=$processes
    built_dll=$dll
    binary_version=$binaryVersion
    registrations_64bit=$registrations
    registration_matches=$registrationMatches
    solidworks_running=@(Get-Process SLDWORKS -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
    notes=@($notes.ToArray())
    live_cad_verified=$false
}
$parentDir = Split-Path -Parent $OutputPath
if ($parentDir) { New-Item -ItemType Directory -Path $parentDir -Force | Out-Null }
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Diagnostic report: $OutputPath"
Write-Host "Expected version: $expectedVersion | Built DLL: $binaryVersion | Registration matches: $registrationMatches"
foreach ($note in $notes) { Write-Host $note -ForegroundColor Yellow }
