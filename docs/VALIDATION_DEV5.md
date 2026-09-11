# Validation — 0.2.0-dev.5

Development candidate, not a completed v0.2 milestone. Date: 2026-09-11.

## Completed in the assistant environment

Python 3.12.14 / Pydantic 2.13.5:

- 21 planner test methods passed.
- 2 contract/measurement methods passed.
- 3 rectangle geometry methods passed: 20 shared geometry fixtures, 384 edge order/direction combinations and 18 nonfinite-coordinate variations.
- Existing suites cover 16 shared feature-scope fixtures and 14 verification vectors.
- Source audit: consistent dev.5 versions, 12 C# compile items, synchronized Python/C# metadata allowlists, PowerShell source encodings and 6 unchanged generated schemas.
- Python source compilation passed.
- SVG layout preview rendered to PNG and visually inspected. This is illustrative, not native UI execution.

Reproduce available checks from the project root:

```bash
cd src/AgentService
python -m unittest discover -s tests -p test_agent.py -v
PYTHONPATH=.:tests python -m unittest test_contracts.ContractTests.test_rejects_malformed_plans test_contracts.ContractTests.test_all_shared_verification_vectors test_rectangle -v
cd ../..
python scripts/audit-source.py
python -m compileall -q src/AgentService scripts
```

The Python rectangle implementation independently checks fixture geometry. C# consumes the same fixtures on Windows; Python passing does not prove C# or COM execution passed.

## Pending

- Full 32-method Python suite (HTTP/schema tests) and real HTTP smoke: FastAPI, Uvicorn and jsonschema are absent here. Earlier dependency download attempts were blocked; no success is claimed.
- Windows PowerShell parsing/process-ownership tests.
- C# compilation against SOLIDWORKS 2025 API and execution of C# pure validation/geometry tests. No C# compiler, Windows or SOLIDWORKS is available here.
- Dev.5 native Task Pane layout, resize, keyboard, disposal and DPI checks in `UI_DEV5.md`.
- Blank-template Check Part, create 100×60×5 mm, edit thickness to 8 mm, rebuild, measured volumes and grouped Undo/rollback.

Only the user's **dev.3** screenshot proves a Task Pane loaded and talked to the agent on SOLIDWORKS Premium 2025 SP1.2. That screenshot exposed the Design Binder false rejection; it does not verify dev.4/dev.5 or native creation.

## Meaningful dev.5 changes

- Native card UI replaces a single RichTextBox log and fixed plan panel. Plan/revision/confirmation boundaries remain in place.
- Rectangle validation now requires four unique connected boundary edges; equal opposite lengths alone are insufficient. Rejects disconnected edges, duplicates, diagonals, nonplanar points and unsupported sizes.
- Execution trace records phase/time and failure phase. Trace/UI callbacks cannot throw into the CAD transaction.
- Existing Design Binder/environment metadata fix remains; unknown geometry is not silently accepted.

Acceptance remains open. Run `scripts/install.ps1` and `V02_TEST_PLAN.md` on Windows before accepting or tagging v0.2.0.
