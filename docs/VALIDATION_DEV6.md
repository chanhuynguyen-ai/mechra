# Validation — 0.2.0-dev.6

Development candidate. Date: 2026-09-11. No claim of completed native UI/CAD acceptance.

## Observed user evidence

Five screenshots show dev.5 hosted in SOLIDWORKS with the local planner responding. Visible defects: oversized welcome content in a narrow pane, native white scrollbars, awkward composer layout, overlong diagnostic cards, and rejection of a zero-body Part due to Markups (`InkMarkupFolder`). These screenshots do not prove native plate creation or dev.6 validation.

## Ran in the assistant environment

- Python 3.12.14 / Pydantic 2.13.5.
- **28 Python test methods passed:** 23 planner, 2 contract/verification, 3 rectangle geometry.
- Fixtures: 23 feature-scope cases (including Markups, renamed real geometry and unknown types), 14 measurement vectors, 20 rectangle cases; 384 edge-order/direction combinations and 18 nonfinite-coordinate variations.
- Source audit passed: dev.6 version markers, 16 C# compile items, synchronized metadata types, PowerShell encodings, 6 unchanged generated schemas.
- Python compileall passed.

Reproduction from project root:

```bash
cd src/AgentService
python -m unittest discover -s tests -p test_agent.py -v
PYTHONPATH=.:tests python -m unittest test_contracts.ContractTests.test_rejects_malformed_plans test_contracts.ContractTests.test_all_shared_verification_vectors test_rectangle -v
cd ../..
python scripts/audit-source.py
python -m compileall -q src/AgentService scripts
```

## Implemented, pending Windows execution

- New `test-ui.ps1` compiles the actual UI controls into an STA WinForms harness. It checks pane region bounds, real editor/button rectangles, native scrollbar styles, welcome layout, scroll anchoring, unread replies and bounded details.
- The installer invokes this test before COM registration. A failing test stops registration; its failure text is retained by the installer transcript.
- Optional `-CaptureScreenshots` captures native controls on Windows; no preview image is treated as proof of test success.

This environment has no Windows, C# compiler, PowerShell or SOLIDWORKS. The UI harness and C# source have not been compiled or run here. They must pass on Windows, followed by the actual SOLIDWORKS hosting checks in `UI_DEV6.md`.

Also pending: full 34-method Python suite and real HTTP smoke (FastAPI/Uvicorn/jsonschema unavailable here); PowerShell parsing/process tests; native Check Part/create/edit/rebuild/volume/Undo/rollback.

## Scope

Preserves the existing CAD plan/confirmation/revision/transaction rules. Changes the UI layout/scroll controls and metadata recognition; does not introduce LLM providers or a general CAD repair planner. The milestone v0.2 gate remains open.
