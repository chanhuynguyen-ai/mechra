param([ValidateSet('Debug','Release')][string]$Configuration='Debug', [string]$SolidWorksApiDir)
. (Join-Path $PSScriptRoot 'common.ps1')
if (!(Test-Admin)) { throw 'Open PowerShell as Administrator to register the add-in.' }
if (Get-Process SLDWORKS -ErrorAction SilentlyContinue) { throw 'Close SOLIDWORKS before registration.' }
$root = Split-Path -Parent $PSScriptRoot
$dst = Join-Path $root "src\SolidWorksAddin\bin\$Configuration"
$dll = Join-Path $dst 'SwCursor.SolidWorksAddin.dll'
if (!(Test-Path -LiteralPath $dll)) { throw "Build output missing: $dll" }
$version = ([IO.File]::ReadAllText((Join-Path $root 'VERSION'))).Trim()
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion -ne $version) { throw 'DLL version differs from VERSION. Rebuild with setup-and-run.ps1.' }
$api = Find-SolidWorksApiDir $SolidWorksApiDir
foreach ($name in @('sldworks','swconst','swpublished')) { Copy-Item -LiteralPath (Join-Path $api "SolidWorks.Interop.$name.dll") -Destination $dst -Force }
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
& $regasm $dll /codebase /tlb
if ($LASTEXITCODE -ne 0) { throw "Registration failed: $LASTEXITCODE" }
Assert-AddinRegistration $dll $version
Write-Host "Registered DLL: $dll"
Write-Host "Registered version: $version"
Write-Host 'Registered Mechra. Open SOLIDWORKS > Tools > Add-Ins > Mechra.' -ForegroundColor Green
