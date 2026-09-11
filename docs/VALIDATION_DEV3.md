# Validation — 0.2.0-dev.3

## Evidence from the user's log and screenshot

- The older source in `C:\AI_project\Mechra-clean` built with 0 warnings and 0 errors on 2026-09-10. That is evidence for the older build, not this candidate.
- That setup run skipped COM registration because Administrator rights were missing.
- The attempted dev.2 shutdown refused the listener on port 8765. The uploaded log does not contain its executable path/parent, so the exact cause of the ownership mismatch is not established.
- The screenshot shows the old Task Pane, an existing `Boss-Extrude1`, and a refusal to create in a nonblank Part. It does not show a successful generated plate.
- Independent source inspection found invalid interpolation before a colon in dev.2 setup. Dev.3 uses braced interpolation and supplies a native PowerShell parser gate.

## Executed in the assistant environment for dev.3

- Python 3.12.14 / Pydantic 2.13.5.
- **18 planner test methods passed**, including the screenshot's existing-feature case, accepted blank-template folders/planes and invalidation of a pending clarification when geometry appears.
- **2 contract/verification test methods passed**: malformed plans are rejected and all **14 shared measurement vectors** match their expected result.
- Source audit passed: version consistency, 10 C# compile items, PowerShell text encoding and interpolation regression check, planner/executor template type parity and six stable generated JSON schemas.
- Python compileall passed.

The measurement fixtures and model-context snapshots are synthetic; they do not demonstrate actual SOLIDWORKS geometry creation.

## Not completed here

- The full 26-method Python suite and real Uvicorn HTTP smoke could not be rerun in this resumed environment: FastAPI/Uvicorn/jsonschema are absent and dependency installation was blocked. Earlier dev.2 HTTP results are historical only; they are not reported as dev.3 passes.
- No PowerShell runtime was available. `test-powershell.ps1` parses all 12 PowerShell files and runs 18 synthetic ownership/health assertions on the user's Windows machine; these are prepared, not reported as passed here.
- No C# compiler, Windows registry, GUI or SOLIDWORKS environment was available. C# build, registry readback, real process shutdown/migration, Task Pane rendering and native create/edit/Undo remain pending.

The installer runs these Windows gates and saves its transcript plus `diagnostics.json` to make a failing step reviewable. It will not label a skipped registration as a ready installation.

## Scope of changes

- Fix PowerShell interpolation, recognize a verified Windows venv redirector's immediate child, and reject unrelated/ambiguous listener ownership.
- Use executable path and exact supported Uvicorn arguments; confirm process creation times and listener ownership again before terminating the captured process handle.
- Fail early without Administrator rights when registration is requested. Compare registered DLL path/assembly/version after registration.
- Add a transcript wrapper and read-only diagnosis; exported Task Pane logs identify the loaded DLL.
- Reject a visibly nonblank Part before producing a create plan and give `File > New > Part` guidance. Native preflight remains authoritative at Apply.
- Keep native geometry construction and verification algorithms at the dev.2 scope; no arbitrary-model editing or v0.3 repair claim.

## Primary references

- [PowerShell quoting rules](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_quoting_rules): brace variables before a literal colon.
- [Python venv documentation](https://docs.python.org/3.11/library/venv.html): Windows virtual environments use redirectors.
- [CPython venv launcher](https://github.com/python/cpython/blob/main/PC/venvlauncher.c): launcher process behavior.

These references inform implementation; they do not prove what process was running on the user's machine.
