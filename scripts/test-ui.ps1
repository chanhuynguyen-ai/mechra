param([string]$MSBuildPath, [switch]$CaptureScreenshots)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Split-Path -Parent $PSScriptRoot
if (!$MSBuildPath) { $MSBuildPath = Find-MSBuild }
$compiler = Join-Path (Split-Path -Parent $MSBuildPath) 'Roslyn\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "C# compiler not found at $compiler" }
$outputDir = Join-Path $root '.runtime'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$output = Join-Path $outputDir 'UiLayoutTests.exe'
$sources = @('src\SolidWorksAddin\UI\ProductControls.cs', 'src\SolidWorksAddin\UI\ConversationViewport.cs', 'src\SolidWorksAddin\UI\PromptComposer.cs', 'src\SolidWorksAddin\UI\PaneRegions.cs', 'src\SolidWorksAddin\UI\WelcomeCardFactory.cs', 'tests\UiLayoutTests.cs') | ForEach-Object { Join-Path $root $_ }
$manifest = Join-Path $root 'tests\UiLayoutTests.manifest'
& $compiler /nologo /target:exe /langversion:7.3 /codepage:65001 "/win32manifest:$manifest" "/out:$output" /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll @sources
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }
Copy-Item -LiteralPath (Join-Path $root 'tests\UiLayoutTests.exe.config') -Destination ($output + '.config') -Force
if ($CaptureScreenshots) { & $output (Join-Path $outputDir 'ui-captures') }
else { & $output }
if ($LASTEXITCODE -ne 0) { throw 'Native WinForms layout tests failed. See FAIL UI above.' }
