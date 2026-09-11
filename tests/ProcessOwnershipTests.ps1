# Synthetic process snapshots only. These tests never inspect or terminate real processes.
. (Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts\common.ps1')
$count = 0
function Assert-Case([bool]$Condition, [string]$Name) {
    if (!$Condition) { throw ('FAILED: ' + $Name) }
    $script:count++
}
$python = 'C:\AI project\Mechra-clean\src\AgentService\.venv\Scripts\python.exe'
$arguments = '-m uvicorn app.main:app --host 127.0.0.1 --port 8765'
function Snapshot([int]$ProcessId, [int]$ParentId, [string]$Executable, [string]$Command, [int]$Second) {
    [pscustomobject]@{ ProcessId=$ProcessId; ParentProcessId=$ParentId; ExecutablePath=$Executable; CommandLine=$Command; CreationDate=([datetime]'2026-09-11T00:00:00Z').AddSeconds($Second) }
}
$direct = Snapshot 100 10 $python ('"' + $python + '" ' + $arguments) 1
Assert-Case ([bool](Get-AgentOwnership $direct $null $python)) 'quoted path with spaces'
$relative = Snapshot 100 10 $python ('python.exe ' + $arguments) 1
Assert-Case ([bool](Get-AgentOwnership $relative $null $python)) 'relative argv with verified executable path'
$child = Snapshot 101 100 'C:\Python313\python.exe' ('"C:\Python313\python.exe" ' + $arguments) 2
Assert-Case ((Get-AgentOwnership $child $direct $python).kind -eq 'venv-child') 'venv redirector child'
Assert-Case (!(Get-AgentOwnership $child $null $python)) 'missing parent refused'
Assert-Case (!(Get-AgentOwnership $child $direct 'C:\Other\python.exe')) 'another project refused'
$laterParent = Snapshot 100 10 $python ('"' + $python + '" ' + $arguments) 3
Assert-Case (!(Get-AgentOwnership $child $laterParent $python)) 'reused parent PID refused'
$wrongParent = Snapshot 999 10 $python ('"' + $python + '" ' + $arguments) 1
Assert-Case (!(Get-AgentOwnership $child $wrongParent $python)) 'unrelated parent refused'
foreach ($badArguments in @(
    '-c "print(1)"',
    ($arguments + ' --reload'),
    ($arguments + ' --app-dir C:\Other'),
    ($arguments -replace '8765','87650'),
    ($arguments -replace '127.0.0.1','0.0.0.0'),
    ($arguments -replace 'app.main:app','other.main:app')
)) {
    $bad = Snapshot 100 10 $python ('"' + $python + '" ' + $badArguments) 1
    Assert-Case (!(Get-AgentOwnership $bad $null $python)) ('unsupported command refused: ' + $badArguments)
}
$hidden = Snapshot 100 10 '' ('"' + $python + '" ' + $arguments) 1
Assert-Case (!(Get-AgentOwnership $hidden $null $python)) 'missing executable path refused'
$spoofed = Snapshot 100 10 'C:\Other\python.exe' ('"' + $python + '" ' + $arguments) 1
Assert-Case (!(Get-AgentOwnership $spoofed $null $python)) 'argv path alone is insufficient'
Assert-Case (!(Test-MatchingAgent ([pscustomobject]@{version='0.2.0'}) '0.2.0-dev.3' 'abc')) 'legacy health rejected without missing-property exception'
$health = [pscustomobject]@{service='mechra-agent';version='0.2.0-dev.3';build_id='abc'}
Assert-Case (Test-MatchingAgent $health '0.2.0-dev.3' 'abc') 'matching health'
Assert-Case (!(Test-MatchingAgent $health '0.2.0-dev.3' 'different')) 'stale build rejected'
Write-Host "PASS: $count process ownership and health checks (synthetic)."
