param(
  [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
  [switch]$SkipRegister
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Step([string]$Message) {
  Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Find-Python {
  $candidates = @(
    @{ File = 'py'; Args = @('-3.11') },
    @{ File = 'py'; Args = @('-3') },
    @{ File = 'python'; Args = @() }
  )
  foreach ($c in $candidates) {
    try {
      $cmd = Get-Command $c.File -ErrorAction Stop
      & $cmd.Source @($c.Args) --version *> $null
      if ($LASTEXITCODE -eq 0) { return $c }
    } catch {}
  }
  throw 'Python 3.11+ was not found. Install Python from python.org, then reopen PowerShell.'
}

function Find-SolidWorksApiDir {
  $roots = @(
    (Join-Path $env:ProgramFiles 'SOLIDWORKS Corp'),
    (Join-Path $env:ProgramFiles 'SOLIDWORKS Corp\SOLIDWORKS'),
    (Join-Path ${env:ProgramFiles(x86)} 'SOLIDWORKS Corp')
  ) | Where-Object { $_ -and (Test-Path $_) }

  $found = @()
  foreach ($root in $roots) {
    $found += Get-ChildItem -Path $root -Filter 'SolidWorks.Interop.sldworks.dll' -File -Recurse -ErrorAction SilentlyContinue |
      Where-Object { $_.DirectoryName -match '[\\/]api[\\/]redist$' }
  }
  $pick = $found | Select-Object -First 1
  if (!$pick) {
    throw 'Could not find SolidWorks.Interop.sldworks.dll under a SOLIDWORKS api\redist folder. Make sure SOLIDWORKS is installed with API components.'
  }
  return $pick.DirectoryName
}

function Find-MSBuild {
  $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
  if (Test-Path $vswhere) {
    $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if ($path -and (Test-Path $path)) { return $path }
  }
  try { return (Get-Command msbuild -ErrorAction Stop).Source } catch {}
  throw 'MSBuild was not found. Install Visual Studio 2022 Build Tools with .NET desktop build tools and the .NET Framework 4.8 targeting pack.'
}

function Test-Admin {
  $id = [Security.Principal.WindowsIdentity]::GetCurrent()
  $p = New-Object Security.Principal.WindowsPrincipal($id)
  return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$root = Split-Path -Parent $PSScriptRoot
$agentDir = Join-Path $root 'src\AgentService'
$project = Join-Path $root 'src\SolidWorksAddin\SolidWorksAddin.csproj'

Step 'Checking prerequisites'
$python = Find-Python
$swApi = Find-SolidWorksApiDir
$msbuild = Find-MSBuild
Write-Host "Python command : $($python.File) $($python.Args -join ' ')"
Write-Host "SOLIDWORKS API : $swApi"
Write-Host "MSBuild        : $msbuild"

$net48Ref = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
if (!(Test-Path $net48Ref)) {
  throw '.NET Framework 4.8 Targeting Pack was not found. Install the .NET Framework 4.8 Developer Pack / targeting pack, then rerun this script.'
}
Write-Host ".NET Framework : v4.8 targeting pack OK" -ForegroundColor Green

Step 'Preparing Python agent service'
Set-Location $agentDir
$venvDir = Join-Path $agentDir '.venv'
$venvPython = Join-Path $venvDir 'Scripts\python.exe'

# Never reuse a virtual environment created on Linux/macOS or copied from another machine.
# A valid Windows venv must have Scripts\python.exe and its interpreter must start cleanly.
$recreateVenv = $false
if (Test-Path $venvDir) {
  if (!(Test-Path $venvPython)) {
    $recreateVenv = $true
  } else {
    try {
      & $venvPython --version *> $null
      if ($LASTEXITCODE -ne 0) { $recreateVenv = $true }
    } catch {
      $recreateVenv = $true
    }
  }
}

if ($recreateVenv) {
  Write-Host 'Existing .venv is not a valid Windows virtual environment; recreating it.' -ForegroundColor Yellow
  Remove-Item -Recurse -Force $venvDir
}

if (!(Test-Path $venvPython)) {
  & $python.File @($python.Args) -m venv $venvDir
  if ($LASTEXITCODE -ne 0 -or !(Test-Path $venvPython)) {
    throw 'Failed to create Windows Python virtual environment.'
  }
}

& $venvPython -m pip install --upgrade pip
if ($LASTEXITCODE -ne 0) { throw 'pip upgrade failed.' }
& $venvPython -m pip install -r requirements.txt
if ($LASTEXITCODE -ne 0) { throw 'Python dependency installation failed.' }

Step 'Starting local agent service on 127.0.0.1:8765'
$existing = $null
try { $existing = Invoke-RestMethod -Uri 'http://127.0.0.1:8765/health' -TimeoutSec 2 } catch {}
if (!$existing) {
  $agentLog = Join-Path $agentDir 'agent.log'
  $agentErr = Join-Path $agentDir 'agent-error.log'
  Start-Process -FilePath $venvPython -ArgumentList @('-m','uvicorn','app.main:app','--host','127.0.0.1','--port','8765') -WorkingDirectory $agentDir -WindowStyle Minimized -RedirectStandardOutput $agentLog -RedirectStandardError $agentErr | Out-Null
  $ready = $false
  for ($i=0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    try {
      $health = Invoke-RestMethod -Uri 'http://127.0.0.1:8765/health' -TimeoutSec 2
      if ($health.ok) { $ready = $true; break }
    } catch {}
  }
  if (!$ready) { throw "Agent did not become healthy. Check $agentErr" }
} else {
  $health = $existing
}
Write-Host "Agent health    : OK ($($health.version))" -ForegroundColor Green

Step 'Smoke-testing agent chat endpoint'
$body = @{
  message = 'foundation smoke test'
  context = @{
    document_title = $null
    path = $null
    document_type = 'none'
    features = @()
  }
} | ConvertTo-Json -Depth 5
$reply = Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:8765/v1/chat' -ContentType 'application/json' -Body $body
Write-Host "Agent reply     : $($reply.message -replace "`r?`n", ' ')" -ForegroundColor Green

Step "Building SOLIDWORKS add-in ($Configuration)"
Set-Location $root
& $msbuild $project /t:Rebuild "/p:Configuration=$Configuration" "/p:SolidWorksApiDir=$swApi" /m
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
$dll = Join-Path $root "src\SolidWorksAddin\bin\$Configuration\SwCursor.SolidWorksAddin.dll"
if (!(Test-Path $dll)) { throw "Build reported success but DLL is missing: $dll" }
Write-Host "Built DLL       : $dll" -ForegroundColor Green

if (!$SkipRegister) {
  Step 'Registering SOLIDWORKS add-in'
  if (!(Test-Admin)) {
    Write-Host 'Registration requires Administrator rights.' -ForegroundColor Yellow
    Write-Host 'Build and agent smoke test succeeded.' -ForegroundColor Green
    Write-Host 'Open PowerShell as Administrator in this folder and run:' -ForegroundColor Yellow
    Write-Host "  Set-ExecutionPolicy -Scope Process Bypass"
    Write-Host "  .\scripts\register-addin.ps1 -Configuration $Configuration"
  } else {
    & (Join-Path $root 'scripts\register-addin.ps1') -Configuration $Configuration
  }
}

Write-Host "`n===============================================" -ForegroundColor Green
Write-Host 'Mechra v0.1.0 smoke test completed.' -ForegroundColor Green
Write-Host 'Next: restart SOLIDWORKS -> Tools > Add-Ins -> Mechra.'
Write-Host 'Open a Part and click Check model in the Task Pane.'
Write-Host '===============================================' -ForegroundColor Green
