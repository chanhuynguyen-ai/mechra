# Changelog

All notable changes to Mechra are recorded here.

## [0.2.0-dev.3] — 2026-09-11 (upgrade and preflight hotfix candidate)

- Fixed invalid PowerShell variable interpolation before a colon in setup.
- Added `install.ps1` with persistent setup transcript and read-only diagnostic report on success/failure.
- Added exact process executable/argv and immediate venv-parent ownership validation; retained refusal of unrelated listeners.
- Added `-PreviousProjectRoot`, early Administrator check and registration path/assembly/version readback.
- Added native PowerShell parsing plus 18 prepared process ownership/health assertions; automatic setup gate on Windows.
- Create requests in Parts with existing model features now explain `File > New > Part` before producing a plan; C# still rechecks actual geometry at Apply.
- Task Pane log export includes the actual loaded DLL path.
- 20 Python test methods passed here, including 14 measurement vectors; source audit and compileall passed.
- Full HTTP/schema suite could not run in the resumed environment due to missing dependencies and blocked installation. PowerShell/C#/Windows/SOLIDWORKS acceptance remains pending. See `docs/VALIDATION_DEV3.md`.

## [0.2.0-dev.2] — 2026-09-10 (development candidate)

### Changed
- Plan preview with explicit Apply/Cancel, model context, status and session-log export in the native Task Pane.
- Session/document/configuration-bound clarification with expiry, cancellation and a bounded concurrent store.
- Strict operation contracts, explicit unit assumptions and per-field source provenance.
- Rejection of unsupported units, negative/zero/nonfinite values and requests containing extra operations.
- C# validates the plan and current document revision before mutating; supports one reviewed operation only.
- Synchronous native undo group, post-rebuild local verifier and measured rollback checkpoint comparison.
- Replaced delayed blind custom Undo with named native SOLIDWORKS Undo transactions.
- Resolve absorbed sketches through Part feature lookup; dimension square plates using perpendicular geometry.
- Restore dimension and feature-error dialog preferences after execution; reject extra features/read-only/unsupported Part states.
- ASCII PowerShell setup and smoke payloads; explicit UTF-8 HTTP; version/build matching; verified process shutdown; location restoration.
- Added shared verification vectors, HTTP/contract tests, Windows C# test runner and live acceptance instructions.

### Validation
- 23 Python test methods passed; real local HTTP smoke passed.
- Native C# compilation, Task Pane rendering and SOLIDWORKS create/edit/rollback remain pending on Windows.
- This is not an accepted v0.2.0 release and does not include an LLM provider.

## [Unreleased] — v0.2.0 candidate

### Added
- Deterministic text intent planner for the first native Part vertical slice.
- Vietnamese/English commands for `create_plate` and `modify_plate_thickness`.
- Focused clarification loop when plate thickness is missing.
- Version `0.2` CAD Plan contract restricted to the supported mutation vocabulary.
- Native SOLIDWORKS `CadExecutor` boundary; the agent never calls COM directly.
- Native rectangular sketch generation with driving width/height dimensions.
- Native blind Boss-Extrude generation named `Mechra-Plate-Extrude`.
- Existing extrusion definition editing through `IExtrudeFeatureData2` for thickness changes.
- Read-back verification snapshot using sketch geometry, extrusion depth and mass-property volume.
- `/v1/verify` endpoint with deterministic dimension/rebuild/volume checks.
- Agent unit tests for create, clarify, edit and verification flows.
- Registration script now copies required SOLIDWORKS Interop dependencies automatically.

### Validated in assistant environment
- Python source compiles.
- 5 agent/verification unit tests pass.
- JSON contract files parse successfully.

### Pending live acceptance
- Build the C# add-in against the user's installed SOLIDWORKS API.
- Create a native `100 x 60 x 5 mm` plate in a blank Part.
- Confirm driving sketch dimensions are editable.
- Confirm `VERIFY ✓` reports width, height, thickness and 30,000 mm³ volume.
- Change thickness to `8 mm` by editing the same native extrusion and verify 48,000 mm³ volume.

## [0.1.0] - 2026-09-10

### Added
- Native SOLIDWORKS COM add-in foundation using `ISwAddin`.
- Right-side Mechra Task Pane with chat and **Check model** action.
- Active-document and top-level feature-tree context capture.
- Persistent-reference service foundation for stable model entity identity.
- Local provider-neutral FastAPI agent runtime on `127.0.0.1:8765`.
- Versioned Design Spec and CAD Plan JSON contracts.
- Windows setup, build, register and unregister scripts.
- Product blueprint, architecture and milestone acceptance gates.

### Validated
- Built successfully against SOLIDWORKS 2025 API on Windows using .NET Framework 4.8.
- Registered successfully as a SOLIDWORKS add-in.
- Loaded successfully in SOLIDWORKS 2025 SP1.2.
- Mechra Task Pane successfully read an active Part and exchanged messages with the local agent service.

### Limitations
- v0.1.0 is read-only: CAD mutation is intentionally disabled.
- AI provider integration is still a provider-neutral boundary.
- Native model creation/editing begins in v0.2.0.
