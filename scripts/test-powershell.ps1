$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$files = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' -File)
$errorsFound = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in $files) {
    $tokens = $null
    $parseErrors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    foreach ($parseError in $parseErrors) { $errorsFound.Add($file.Name + ': ' + $parseError.Message) }
}
$testFile = Join-Path $root 'tests\ProcessOwnershipTests.ps1'
$tokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($testFile, [ref]$tokens, [ref]$parseErrors)
foreach ($parseError in $parseErrors) { $errorsFound.Add('ProcessOwnershipTests.ps1: ' + $parseError.Message) }
if ($errorsFound.Count -gt 0) { throw ($errorsFound -join "`n") }
Write-Host "PASS: parsed $($files.Count + 1) PowerShell files."
& $testFile
