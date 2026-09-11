# MVP Acceptance Gates

## Milestone v0.1 — Foundation (this package)

- [x] SOLIDWORKS add-in boundary defined
- [x] Task Pane UI shell
- [x] Active document context reader
- [x] Feature-tree snapshot
- [x] Persistent-reference service
- [x] Local agent service boundary
- [x] Provider-neutral message contract
- [x] Design-spec contract
- [x] CAD-plan contract
- [x] Build and smoke-test on SOLIDWORKS 2025 SP1.2 (live Windows test, 2026-09-10)

## Milestone v0.2 — First real vertical slice 🚧

Implementation candidate is `0.2.0-dev.4`; the checkboxes below remain open until the user live-tests them in SOLIDWORKS.

Observed progress: the user screenshot confirms dev.3 Task Pane loading and agent communication on SOLIDWORKS 2025 SP1.2. Geometry was blocked by a metadata classification error. Dev.4 addresses that error; actual create/edit acceptance is still open.

A user can type: `Create a 100 x 60 x 5 mm rectangular plate`.

Acceptance:
- Native `.SLDPRT`
- `Sketch1` + native boss/extrude feature (not imported STEP)
- dimensions are editable
- rebuild has no error
- context reader sees the created features
- local verifier confirms measured sketch dimensions, extrusion depth and whole-Part volume
- user can say `change thickness to 8 mm` and the existing native dimension/feature is edited rather than regenerating a dumb body

## Milestone v0.3 — Repair loop

- detect a failed/suppressed/rebuild-error feature
- explain root cause using model context
- generate a repair plan
- preview affected features
- apply repair
- rebuild and verify

## Milestone v0.4 — Drawing image → Design Spec

Scope: simple prismatic machined parts only.

- identify orthographic views
- extract nominal linear dimensions, diameters and radii
- associate dimensions with geometry
- list missing/ambiguous/conflicting parameters
- ask focused clarification
- persist user answers into Design Spec

## Milestone v0.5 — Drawing → Native 3D

- Design Spec → CAD Plan → native SOLIDWORKS Part
- verify dimensions against source spec
- correction loop from chat

## Commercial beta gate

Do not call the product manufacturing-ready until the test corpus has explicit metrics for generation success, dimension accuracy, rebuild success, repair success, unresolved-assumption rate, crash rate and rollback reliability.
