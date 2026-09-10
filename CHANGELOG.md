# Changelog

All notable changes to Mechra are recorded here.

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
- AI provider integration is still a mock/provider-neutral boundary.
- Native model creation/editing begins in v0.2.0.
