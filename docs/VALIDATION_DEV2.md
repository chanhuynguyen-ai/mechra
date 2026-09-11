# Validation — 0.2.0-dev.2

## Completed in the assistant environment

- Python 3.12 with FastAPI 0.141.1, Pydantic 2.13.5 and Uvicorn 0.52.4.
- **23 unittest test methods passed**, including parameterized input cases and 24 concurrent conversations.
- Tests cover Vietnamese/English commands, square/tall/small plates at the planner level, decimals, negative/zero/oversized values, unsupported units and extra operations, required confirmation, missing-value provenance, conversation/document/configuration isolation, expiration/cancellation, malformed payloads and JSON schemas.
- **14 shared measurement vectors** cover valid create/edit, dimension and volume mismatches, failed rebuild, missing features, forged expected volume and tiny geometry.
- Real local Uvicorn HTTP smoke passed: UTF-8 clarification → create plan → edit plan, invalid input rejection, verification pass and failure.
- Source/packaging checks validate contract generation, project compile-item coverage, text encodings and version consistency.

The HTTP and measurement inputs above are synthetic. They do not demonstrate that SOLIDWORKS executed any geometry operation.

## Prepared, not executed here

- `tests/CadValidationTests.cs` uses the same JSON measurement vectors and checks the C# operation guard and document-revision comparison. `scripts/test-csharp.ps1` compiles and runs it with Visual Studio's C# compiler on Windows.
- Actual C# add-in compilation against installed SOLIDWORKS Interop assemblies.
- SOLIDWORKS Task Pane rendering, COM creation/editing, driving dimensions, rebuild behavior and grouped Undo/rollback.

No .NET compiler, Windows GUI or SOLIDWORKS installation was available in this environment. The earlier v0.1 live success does not establish v0.2 success. Follow `V02_TEST_PLAN.md`; the v0.2 live gate remains open.

## Implementation decisions

- Local verification runs before returning control to SOLIDWORKS; an HTTP await never leaves an Undo group open.
- A delayed custom Undo button was excluded because a model update stamp does not track every kind of user operation. Named transactions remain available in SOLIDWORKS' native Undo list.
- C# revalidates operation inputs and the reviewed document before mutation.
- Plain-plate scope rejects additional features, so a small downstream cut cannot slip through only because it changes less than the volume tolerance.
- `GetTypeName2` supports multiple boss/extrusion names; Instant3D `ICE` uses `GetTypeName` to identify its underlying type.
- Windows startup compares both package version and a source build fingerprint. It only stops a process whose Python path and Uvicorn command match the specified project.
- The planner remains deterministic. Provider adapters, drawing vision, repair planning and commercial deployment remain future milestones.

## Official API references checked

- [GetUpdateStamp](https://help.solidworks.com/2026/english/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IModelDoc2~GetUpdateStamp.html): model changes tracked; name/color changes have limitations.
- [StartRecordingUndoObject](https://help.solidworks.com/2026/English/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IModelDocExtension~StartRecordingUndoObject.html) and [FinishRecordingUndoObject2](https://help.solidworks.com/2026/english/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IModelDocExtension~FinishRecordingUndoObject2.html): group native Undo-supported operations.
- [GetMassProperties2](https://help.solidworks.com/2025/english/api/sldworksapi/solidworks.interop.sldworks~solidworks.interop.sldworks.imodeldocextension~getmassproperties2.html?format=P&value=): whole-model metric measurements; volume is array index 3.
- [GetTypeName2](https://help.solidworks.com/2026/english/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IFeature~GetTypeName2.html) and [GetTypeName](https://help.solidworks.com/2025/english/api/sldworksapi/SolidWorks.interop.sldworks~SolidWorks.interop.sldworks.IFeature~GetTypeName.html): feature type handling.
- [Dimension driven-state enum](https://help.solidworks.com/2025/english/api/swconst/SolidWorks.Interop.swconst~SolidWorks.Interop.swconst.swDimensionDrivenState_e.html): driving versus reference dimensions.

Sources verify API contracts; they do not replace compilation or live CAD tests.
