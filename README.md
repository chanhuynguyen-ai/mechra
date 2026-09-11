# Mechra for SOLIDWORKS

A mechanical design copilot embedded in the SOLIDWORKS Task Pane.

**Current package: 0.2.0-dev.4 — development candidate, pending live SOLIDWORKS acceptance.**

Dev.4 fixes false rejection of Design Binder in an empty Part and adds a read-only Check Part action plus native preflight before plan review. See [Windows instructions](RUN_WINDOWS.md).

This candidate builds the first deterministic text-to-native-Part workflow:

1. Describe one rectangular plate in millimetres.
2. Resolve missing thickness in the same conversation and Part.
3. Review the plan and choose **Apply plan**.
4. Create an editable native sketch and blind extrusion, or edit that extrusion's thickness.
5. Rebuild and compare sketch dimensions, extrusion depth and volume with the plan.
6. On failure, attempt a grouped native Undo and compare the pre-edit checkpoint.

## Try it on Windows

Read [RUN_WINDOWS.md](RUN_WINDOWS.md). Python 3.11+, SOLIDWORKS, Visual Studio Build Tools and the .NET Framework 4.8 targeting pack are required for the add-in.

```powershell
.\scripts\install.ps1
```

Open a blank Part, enable Mechra, select **Check Part**, then send:

```text
Tạo plate 100 x 60 x 5 mm
Đổi chiều dày thành 8 mm
```

Each supported mutation first produces a plan. The add-in checks the document identity, configuration, update stamp and feature names again when you choose Apply. A changed Part requires a new plan.

**This version uses a deterministic planner. It has no connected LLM, drawing vision, arbitrary geometry generation or automatic repair planner.** These remain later roadmap milestones. No API key is required for the v0.2 slice.

## Scope and limits

- One Part, one configuration, one plain rectangular plate; millimetres only.
- The first reference plane is shown as the chosen construction plane.
- Bare dimensions assume mm and the conversation explicitly identifies that assumption.
- Negative/zero/oversized dimensions, unsupported units and extra requested operations do not produce partial executable plans.
- Existing extra features, surface bodies, multiple solid bodies, read-only documents and an open sketch edit session block execution.
- Width/height are driving dimensions; full sketch definition/anchoring has not been demonstrated.
- Native Undo groups are named `Mechra: ...`. Use SOLIDWORKS' Undo list to choose the intended group. No delayed blind Undo button is exposed.
- Rollback is best effort and verifies the feature list, body count, plate dimensions and volume; it is not a byte-for-byte document recovery guarantee.
- No automatic save or export of a `.SLDPRT`; save the verified Part through SOLIDWORKS.

## Validate

```powershell
.\scripts\setup-and-run.ps1 -AgentOnly
.\scripts\test-csharp.ps1
```

Cross-platform Python tests:

```bash
cd src/AgentService
python -m pip install -r requirements-test.txt
python -m unittest discover -s tests -v
cd ../..
python scripts/smoke-http.py
```

[Validation report](docs/VALIDATION_DEV4.md) separates completed Python/HTTP checks from the pending C# build and actual CAD tests. [Live test steps](docs/V02_TEST_PLAN.md) define acceptance. `main` remains the accepted milestone; test candidates on `dev` or an isolated source folder before tagging a release.

## Project map

- `src/SolidWorksAddin`: native UI, context, deterministic COM executor, local verifier.
- `src/AgentService`: validated planner, scoped clarification, HTTP verification.
- `src/Shared/contracts`: versioned schemas.
- `src/Shared/tests`: verification cases used by Python and C# tests.
- `scripts`: Windows setup, registration, process management and tests.
- `docs/PROJECT_CHARTER.md`, `docs/PRODUCT_BLUEPRINT.md`: agreed product direction.
- `skills/mechra-development/SKILL.md`: standing development contract.
