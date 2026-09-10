param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $root "src\SolidWorksAddin\bin\$Configuration\SwCursor.SolidWorksAddin.dll"
if (!(Test-Path $dll)) { throw "Build output not found: $dll" }
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
if (!(Test-Path $regasm)) { throw "RegAsm.exe not found: $regasm" }
& $regasm $dll /codebase /tlb
if ($LASTEXITCODE -ne 0) { throw "RegAsm failed with exit code $LASTEXITCODE" }
Write-Host "Registered: $dll"
Write-Host "Restart SOLIDWORKS, then check Tools > Add-Ins > Mechra."
