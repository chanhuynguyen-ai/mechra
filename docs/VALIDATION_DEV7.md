# Validation — 0.2.0-dev.7

Date: 2026-09-11. Development candidate; v0.2 native CAD acceptance remains open.

## Evidence and correction

The latest user screenshots show a loaded, compact dark Task Pane. Creating a plate in the apparently blank Part1 is rejected specifically at `Equations [EqnFolder]`. The screenshots do not expose Equation Manager contents; they cannot establish that its equation count is zero. The subsequent thickness command has no created Mechra extrusion to edit. The empty composer also lacks its prompt.

Source inspection confirms two defects: EqnFolder is absent from both feature-type policies, and the placeholder Label is behind the native TextBox in the control collection.

This candidate:

- Adds EqnFolder to Python and C# template-node classification. Retains DocsFolder, EnvFolder and InkMarkupFolder support. No display-name or generic `*Folder` exception is used.
- Reads `GetEquationMgr().GetCount()`, `GetDisabledEquationCount()` and `LinkToFile`. A fresh Equation Manager is acquired for each capture/check. Counts are kept separate, not summed.
- Requires both counts to be zero and the file link to be false in native preflight. Missing, failed or invalid API reads do not mean zero. Existing equations/global variables, disabled entries and external equation links remain outside this slice. No equations are deleted, disabled, evaluated or unlinked by this update.
- Adds optional `equations` context with strict fields. Legacy requests that omit it can still request a plan; native preflight must obtain a successful empty read before displaying the review and again at Apply. Both processes must be upgraded together.
- Blocks create/edit planning for known populated equation state and clears a pending thickness clarification if an equation appears, including when the update stamp is unchanged. Native revision comparison includes all three equation-state fields.
- Preserves actual body/sketch checks, one configuration, read-only/sketch-edit guards, geometry verification and grouped Undo. The rollback checkpoint also requires the supported empty equation state.
- Check Part details and exported log report equation counts and the file-link state. Unsupported-feature preflight collects all blockers, displaying up to four examples with the total count. The existing full feature list remains in Check Part/Save log.
- Restores the empty, unfocused composer prompt above the native TextBox. It hides on focus so it cannot obscure the caret, and reappears after the draft is cleared and focus leaves the editor. Keeps the dark compact layout.
- Explains that thickness editing requires successful plate creation and Apply first.

## Validation executed in this environment

Python 3.12.14; Pydantic 2.13.5.

| Check | Result |
| --- | --- |
| Existing planner tests | 23 methods PASS |
| New equation tests | 7 methods PASS |
| Contract/measurement tests available here | 2 methods PASS |
| Rectangle geometry tests | 3 methods PASS |
| Total application tests run | 35 methods PASS |
| Source-audit regressions | 5 methods PASS |
| Shared feature-type cases exercised by Python | 31 cases PASS |
| Shared equation-state cases exercised by Python | 10 cases PASS |
| Existing measurement / rectangle fixtures | 14 / 20 cases PASS |
| Rectangle ordering / direction / nonfinite variants | 384 / 18 PASS |
| Source audit | 16 C# compile items, matching policies/version, PowerShell source encoding and 6 stable schemas PASS |
| C# lexical delimiter audit | 18 files PASS; not a compiler |
| Python compileall | PASS |

Commands:

```bash
cd src/AgentService
PYTHONPATH=.:tests python3 -m unittest test_agent test_equations test_rectangle test_contracts.ContractTests.test_rejects_malformed_plans test_contracts.ContractTests.test_all_shared_verification_vectors -v
cd ../..
python3 -m unittest discover -s scripts -p test_source_audit.py -v
python3 scripts/audit-source.py
python3 -m compileall -q src/AgentService scripts
```

FastAPI, Uvicorn, httpx and jsonschema are unavailable here. The full 42-method Python suite, including six HTTP tests and schema validation, has not run for dev.7. The earlier user log's 34-method success applies to dev.6, not this candidate. No C# compiler, Windows Forms runtime or SOLIDWORKS host is available here. Native build, pure C# tests, UI harness and CAD execution have not been verified for dev.7.

## Windows regression gates

`scripts/install.ps1` runs the full Python suite, actual local HTTP smoke, add-in build, pure C# tests, native WinForms layout tests and registration. C# tests now consume the shared equation-state fixtures and test revision changes; the UI harness checks actual placeholder visibility and which control is on top, in addition to layout/scroll tests. These are prepared tests, not claimed passes.

After installation, test the following in SOLIDWORKS 2025 SP1.2:

1. Open a blank single-configuration Part. Check Part must report zero solid/surface bodies, equation count 0, disabled count 0, link false, and ready to create, even if the API tree contains Equations, Markups and Design Binder.
2. Create `100 x 60 x 5 mm`, review/apply and require native features plus VERIFIED at 30,000 mm³.
3. Edit thickness to `8 mm`, review/apply and require the existing extrusion plus VERIFIED at 48,000 mm³.
4. In a separate disposable test Part, add a global variable/equation. Check Part and create planning must explain equation state; no geometry should be changed. Also verify a disabled equation or linked equation file is not accepted as an empty state.
5. Add an equation between review and Apply; the pending plan must be invalidated before mutation. Do not use a production model for this regression.
6. At narrow/wide pane widths and 100/150/200% Windows scaling, verify empty/unfocused prompt, focused caret, typing, clearing, Enter/Shift+Enter, scroll position and plan actions. Check exported log includes the loaded DLL version/path and equation diagnostics.

Do not delete default folders to get past preflight. If a newly encountered type is blocked, the full Check Part log now provides the entire feature list and all equation/body readings for a targeted diagnosis.

## API/design references

- [IModelDoc2.GetEquationMgr](https://help.solidworks.com/2025/english/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IModelDoc2~GetEquationMgr.html): manager belongs to the configuration active when acquired.
- [IEquationMgr members, 2025](https://help.solidworks.com/2025/english/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IEquationMgr_members.html): GetCount, GetDisabledEquationCount, GlobalVariable and LinkToFile.
- [GetDisabledEquationCount](https://help.solidworks.com/2023/English/api/sldworksapi/SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IEquationMgr~GetDisabledEquationCount.html): separate disabled-equation count, available since SOLIDWORKS 2017.
- [IFeature.GetTypeName2](https://help.solidworks.com/2026/english/api/sldworksapi/SOLIDWORKS.Interop.sldworks~SOLIDWORKS.Interop.sldworks.IFeature~GetTypeName2.html): type classification; some tree nodes establish design functionality rather than geometry. EqnFolder identification itself comes from the user's API diagnostic screenshot.
- [Microsoft: layering Windows Forms controls](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-layer-objects-on-windows-forms): BringToFront and z-order.

The public type list is not proof that every SOLIDWORKS template node has been observed. This update deliberately avoids claiming universal template compatibility.
