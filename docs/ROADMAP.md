# Mechra Roadmap

## v0.1.0 — Foundation ✅
Native SOLIDWORKS add-in, Task Pane, model context, local agent boundary, persistent-reference foundation and shared contracts. Live-tested in SOLIDWORKS 2025 SP1.2.

## v0.2.0 — Text → Native Part 🚧
Candidate `0.2.0-dev.3` adds upgrade diagnostics, verified registration and early nonblank-Part guidance to the reviewed create/edit workflow. Twenty Python test methods pass locally; full HTTP/schema rerun, PowerShell/C# and live SOLIDWORKS acceptance remain pending. Details: `VALIDATION_DEV3.md`.

Acceptance target: `Create a 100 x 60 x 5 mm plate` produces an editable native sketch + extrusion; `change thickness to 8 mm` edits the existing design; rebuild and measured verification pass.

## v0.3.0 — Check & Repair Loop
Read rebuild/sketch/reference failures, produce root-cause explanation and repair plan, preview affected features, apply repair, rebuild and verify.

## v0.4.0 — Drawing Vision → Design Spec
Accept image/screenshot/PDF-derived raster input, identify views/basic dimensions/notes, create structured Design Spec, and ask for missing or ambiguous blocking values.

## v0.5.0 — Technical Drawing → Native 3D
Convert a constrained class of mechanical drawings into native parametric SOLIDWORKS parts through Design Spec → CAD Plan → Execute → Verify.

## v0.6.0 — Advanced Text → 3D
Expand part vocabulary: holes, patterns, fillets/chamfers, revolves, pockets/cuts, ribs, shells, reference geometry and robust modifications.

## v0.7.0 — Native 3D → Drawing
Generate drawing sheets, projected/isometric/section views, dimensions, center marks, notes and templates with verification.

## v0.8.0 — Assembly Agent
Components, mates, assembly reasoning, interference/context checks and controlled editing.

## v0.9.0 — Engineering Intelligence
DFM checks, material/process suggestions, standard parts, calculations, company/project rules and engineering review checklist.

## v1.0.0 — Commercial Beta
Installer/updater, signed binaries, authentication/licensing, provider configuration, crash recovery, telemetry controls, privacy/security model, subscriptions/billing hooks, onboarding, diagnostics, documentation and benchmark suite.

## Post-1.0
Advanced drawings/GD&T, sheet metal depth, enterprise/local-model modes, PDM/PLM connectors, simulation/CAE workflows, CAM handoff and organization-level standards.
