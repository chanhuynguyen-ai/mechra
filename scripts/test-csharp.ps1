param([string]$MSBuildPath)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Split-Path -Parent $PSScriptRoot
if (!$MSBuildPath) { $MSBuildPath = Find-MSBuild }
$compiler = Join-Path (Split-Path -Parent $MSBuildPath) 'Roslyn\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found at $compiler" }
$outputDir = Join-Path $root '.runtime'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$output = Join-Path $outputDir 'CadValidationTests.exe'
$sources = @('src\SolidWorksAddin\Services\CadContracts.cs','src\SolidWorksAddin\Services\CadValidation.cs','src\SolidWorksAddin\Services\RectangleGeometry.cs','src\SolidWorksAddin\Services\ModelRevision.cs','tests\CadValidationTests.cs') | ForEach-Object { Join-Path $root $_ }
& $compiler /nologo /target:exe /langversion:7.3 "/out:$output" /reference:System.Core.dll /reference:System.Web.Extensions.dll @sources
if ($LASTEXITCODE -ne 0) { throw 'C# pure test compilation failed.' }
& $output (Join-Path $root 'src\Shared\tests\verification-cases.json') (Join-Path $root 'src\Shared\tests\feature-scope-cases.json') (Join-Path $root 'src\Shared\tests\rectangle-cases.json') (Join-Path $root 'src\Shared\tests\equation-state-cases.json')
if ($LASTEXITCODE -ne 0) { throw 'C# contract tests failed.' }
