# Validation — 0.2.0-dev.4

## Observed user evidence

The latest screenshot shows Mechra v0.2.0-dev.3 loaded in SOLIDWORKS Premium 2025 SP1.2. Its native Task Pane exchanges a request with the agent, but rejects `Part2` because of the `Design Binder` node. The visible Part has default planes/origin and no modeled geometry shown.

This confirms the dev.3 UI loading and agent exchange. It does not prove all installer tests passed, identify the loaded DLL path, or demonstrate a successful CAD operation. The new feature fixture is reconstructed for regression coverage, not presented as a captured full tree from the user's machine.

## Diagnosis and fix

The v0.2 creation guard classified every top-level feature outside a short allowlist as a reason to reject creation. Both Python and C# omitted `DocsFolder` and `EnvFolder`. Metadata folders are now admitted by API type, while native body/sketch inspection remains in place. An extrusion renamed `Design Binder` is still rejected.

The next step within the v0.2 milestone is implemented as **Check Part**: read native preconditions and report create/edit readiness, body counts and feature types. A returned CAD plan also passes this native check before the review card appears. Execution rechecks the actual document at Apply. Inspection does not invoke rebuild, change selection, start an Undo group or intentionally write geometry; the no-mutation behavior must still be checked live.

No generic repair planner, arbitrary existing-model modification, LLM integration or later roadmap milestone is claimed.

## Tests run here

- Python 3.12.14, Pydantic 2.13.5.
- **21 planner test methods passed**, including metadata-template direct create, missing-thickness continuation and edit planning.
- **2 contract/verification test methods passed**, covering malformed inputs and **14 shared measurement vectors**.
- **16 explicit feature-scope cases** cover Design Binder/environment folders, localized metadata names, renamed real features, missing/unknown types, imported bodies, generic folders, native plate edit and downstream cuts.
- Source audit passed: Python/C# metadata policy parity, version consistency, 10 C# compile items, PowerShell text encoding/interpolation and six stable generated schemas.
- Python compileall passed.

These are synthetic planning/measurement tests; they do not execute SOLIDWORKS.

## Prepared and pending

- C# pure tests now consume the same feature-scope fixtures; expected 54 assertions including existing validation/verification checks. They are run by `scripts/test-csharp.ps1` on Windows.
- Windows HTTP smoke now uses a Part context that includes Design Binder/environment metadata and verifies existing-user-feature rejection.
- The full 29-method Python suite, JSON-schema validation and real Uvicorn smoke have not run successfully in this resumed environment; FastAPI/Uvicorn/jsonschema are unavailable. Dependency download attempts in the previous turn were blocked, so no new success is claimed.
- PowerShell execution, dev.4 C# compilation, native Check Part/plan UI, actual sketch/extrusion construction, thickness edit, rebuild, volume verification and Undo remain Windows/SOLIDWORKS gates.

Run `scripts/install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-v0.2.0-dev.3`, then follow `V02_TEST_PLAN.md`. A readiness report is not post-execution VERIFIED evidence.

## Primary reference material

- [Displaying the Design Binder](https://help.solidworks.com/2026/English/SolidWorks/sldworks/t_displaying_the_design_binder.htm): journal, attachments and reports; the folder may be hidden when empty.
- [Design Binder from the SOLIDWORKS product team](https://blogs.solidworks.com/products/solidworks/design-binder-smart-way-to-store-intellectual-data-to-your-parts-and-assemblies/): storage of non-CAD information with the model.
- [GetTypeName2 historical API type table](https://help.solidworks.com/2011/English/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IFeature~GetTypeName2.html): `DocsFolder` identifier in the indexed API reference.

The screenshot does not include a feature type dump. `Check Part` and `Save log` now provide that information if another template node is rejected.
