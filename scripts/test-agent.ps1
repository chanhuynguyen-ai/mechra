. (Join-Path $PSScriptRoot 'common.ps1')
$session = [guid]::NewGuid().ToString('N')
$scopeCases = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'src\Shared\tests\feature-scope-cases.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$metadataFeatures = @($scopeCases[0].features)
$context = @{document_title='Smoke-Part'; document_type='part'; document_id=$session; update_stamp=1; configuration='Default'; features=$metadataFeatures}
$context.equations = @{count=0; disabled_count=0; linked_to_file=$false}
# ASCII payload avoids PowerShell 5.1 source-encoding ambiguity.
$reply = Invoke-AgentJson '/v1/chat' @{message='Create a plate 100 x 60 x 5 mm'; session_id=$session; context=$context}
if ($reply.action -ne 'execute_cad_plan' -or !$reply.requires_confirmation -or $reply.plan.operations[0].inputs.thickness_mm -ne 5) { throw 'Create-plan smoke test failed.' }
$first = Invoke-AgentJson '/v1/chat' @{message='Create plate 100 x 60 mm'; session_id=$session; context=$context}
if ($first.action -ne 'none') { throw 'Missing-thickness test failed.' }
$second = Invoke-AgentJson '/v1/chat' @{message='5 mm'; session_id=$session; context=$context}
if ($second.action -ne 'execute_cad_plan') { throw 'Clarification continuation failed.' }
$context.features = $metadataFeatures + @(@{name='Boss-Extrude1'; type_name='Extrusion'})
$blocked = Invoke-AgentJson '/v1/chat' @{message='Create plate 100 x 60 x 5 mm'; session_id=$session; context=$context}
if ($blocked.action -ne 'none' -or $blocked.plan) { throw 'Existing user geometry was incorrectly accepted.' }
$context.features = $metadataFeatures + @(@{name='Mechra-Plate-Extrude'; type_name='Extrusion'})
$edit = Invoke-AgentJson '/v1/chat' @{message='Change thickness to 8 mm'; session_id=$session; context=$context}
if ($edit.plan.operations[0].kind -ne 'modify_plate_thickness') { throw 'Thickness-edit test failed.' }
foreach ($equations in @(@{count=1; disabled_count=0; linked_to_file=$false}, @{count=0; disabled_count=1; linked_to_file=$false}, @{count=0; disabled_count=0; linked_to_file=$true})) {
    $context.equations = $equations
    $blockedEdit = Invoke-AgentJson '/v1/chat' @{message='Change thickness to 8 mm'; session_id=$session; context=$context}
    if ($blockedEdit.plan) { throw 'Equations must block thickness editing.' }
}
$context.features = $metadataFeatures
$context.equations = @{count=1; disabled_count=0; linked_to_file=$false}
$blockedCreate = Invoke-AgentJson '/v1/chat' @{message='Create plate 100 x 60 x 5 mm'; session_id=$session; context=$context}
if ($blockedCreate.plan) { throw 'Equations must block plate creation.' }
$snapshot = @{operation='create_plate'; rebuild_ok=$true; expected_width_mm=100; expected_height_mm=60; expected_thickness_mm=5; measured_width_mm=100; measured_height_mm=60; measured_thickness_mm=5; expected_volume_mm3=30000; measured_volume_mm3=30000; feature_names=@('Mechra-Plate-Sketch','Mechra-Plate-Extrude')}
if (!(Invoke-AgentJson '/v1/verify' $snapshot).passed) { throw 'Verification pass case failed.' }
$snapshot.measured_volume_mm3 = 25000
if ((Invoke-AgentJson '/v1/verify' $snapshot).passed) { throw 'Verification incorrectly accepted wrong volume.' }
Write-Host 'HTTP tests PASS: observed template folders including EqnFolder, empty/populated equations, create, clarify, nonblank refusal, edit, valid/invalid measurements.' -ForegroundColor Green
Write-Host 'These are synthetic HTTP tests. Native SOLIDWORKS acceptance is separate.'
