param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $root "src\SolidWorksAddin\bin\$Configuration\SwCursor.SolidWorksAddin.dll"
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
& $regasm $dll /unregister /tlb
