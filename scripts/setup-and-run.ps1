param(
    [ValidateSet('Debug','Release')][string]$Configuration='Debug',
    [switch]$SkipRegister,
    [switch]$AgentOnly,
    [string]$SolidWorksApiDir
)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Split-Path -Parent $PSScriptRoot
$agentDir = Join-Path $root 'src\AgentService'
$venv = Join-Path $agentDir '.venv'
$venvPython = Join-Path $venv 'Scripts\python.exe'
$expectedVersion = ([IO.File]::ReadAllText((Join-Path $root 'VERSION'))).Trim()
Push-Location $root
try {
    Write-Step 'Checking prerequisites'
    $python = Find-Python
    if (!$AgentOnly) {
        if (Get-Process SLDWORKS -ErrorAction SilentlyContinue) { throw 'Close SOLIDWORKS before rebuilding the add-in, then rerun this script.' }
        $api = Find-SolidWorksApiDir $SolidWorksApiDir
        $msbuild = Find-MSBuild
        $net48 = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
        if (!(Test-Path -LiteralPath $net48)) { throw '.NET Framework 4.8 targeting pack is missing.' }
        Write-Host "SOLIDWORKS API: $api"
    }
    Write-Step 'Preparing Python environment'
    $valid = $false
    if (Test-Path -LiteralPath $venvPython) {
        & $venvPython -c 'import sys; sys.exit(0 if sys.version_info >= (3,11) else 1)' *> $null
        $valid = $LASTEXITCODE -eq 0
    }
    if (!$valid -and (Test-Path -LiteralPath $venv)) {
        Move-Item -LiteralPath $venv -Destination ($venv + '.invalid-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    }
    if (!$valid) {
        $pythonArgs = $python.Args
        & $python.File @pythonArgs -m venv $venv
        if ($LASTEXITCODE -ne 0) { throw 'Virtual environment creation failed.' }
    }
    & $venvPython -m pip install -r (Join-Path $agentDir 'requirements-test.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Dependency installation failed.' }
    Push-Location $agentDir
    try {
        Write-Step 'Running agent, HTTP and contract tests'
        & $venvPython -m unittest discover -s tests -v
        if ($LASTEXITCODE -ne 0) { throw 'Agent tests failed.' }
        $expectedBuild = & $venvPython -c 'from app.main import BUILD_ID; print(BUILD_ID)'
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read the agent build identity.' }
    } finally { Pop-Location }
    Write-Step 'Starting the matching local agent'
    $health = Get-AgentHealth
    $match = $health -and $health.PSObject.Properties['service'] -and $health.service -eq 'mechra-agent' -and
        $health.PSObject.Properties['version'] -and $health.version -eq $expectedVersion -and
        $health.PSObject.Properties['build_id'] -and $health.build_id -eq $expectedBuild
    if (!$match) {
        Stop-OwnedAgent $venvPython
        $runtime = Join-Path $root '.runtime'
        New-Item -ItemType Directory -Path $runtime -Force | Out-Null
        $started = Start-Process -FilePath $venvPython -ArgumentList @('-m','uvicorn','app.main:app','--host','127.0.0.1','--port','8765') -WorkingDirectory $agentDir -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime 'agent.log') -RedirectStandardError (Join-Path $runtime 'agent-error.log') -PassThru
        $ready = $false
        for ($i=0; $i -lt 30; $i++) {
            Start-Sleep -Milliseconds 200
            $health = Get-AgentHealth
            if ($health -and $health.PSObject.Properties['build_id'] -and $health.build_id -eq $expectedBuild -and $health.service -eq 'mechra-agent') { $ready = $true; break }
            if ($started.HasExited) { break }
        }
        if (!$ready) { throw "Agent startup failed. Read $runtime\agent-error.log" }
    }
    Write-Step 'Testing real HTTP planning and verification'
    & (Join-Path $PSScriptRoot 'test-agent.ps1')
    if (!$AgentOnly) {
        Write-Step 'Building SOLIDWORKS add-in'
        $project = Join-Path $root 'src\SolidWorksAddin\SolidWorksAddin.csproj'
        & $msbuild $project /t:Rebuild "/p:Configuration=$Configuration" "/p:SolidWorksApiDir=$api" /m
        if ($LASTEXITCODE -ne 0) { throw 'Add-in build failed.' }
        & (Join-Path $PSScriptRoot 'test-csharp.ps1') -MSBuildPath $msbuild
        if (!$SkipRegister) {
            if (Test-Admin) { & (Join-Path $PSScriptRoot 'register-addin.ps1') -Configuration $Configuration -SolidWorksApiDir $api }
            else {
                Write-Host 'Build succeeded. Registration requires an Administrator PowerShell:' -ForegroundColor Yellow
                Write-Host ".\scripts\register-addin.ps1 -Configuration $Configuration -SolidWorksApiDir `"$api`""
            }
        }
    }
    Write-Host "`nMechra $expectedVersion: agent tests and HTTP smoke test passed." -ForegroundColor Green
    if (!$AgentOnly) { Write-Host 'Open SOLIDWORKS -> blank Part -> Mechra. Review the plan and click Apply plan.' }
} finally { Pop-Location }
