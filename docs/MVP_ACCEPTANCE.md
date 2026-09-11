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

Implementation candidate is `0.2.0-dev.7`; the checkboxes below remain open until the user live-tests them in SOLIDWORKS.

Observed progress: latest screenshots show the compact dark pane loading and agent replies, but Part1 is blocked by Equations (`EqnFolder`) and no native plate has been demonstrated. Dev.7 separates folder classification from actual equation-state inspection and restores the missing prompt. Native UI/CAD acceptance remains open.

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

## dev.5 candidate status

26 Python test methods passed; native card UI, rectangle topology checks and execution trace implemented. C# build, UI/DPI and actual CAD acceptance remain open. See `VALIDATION_DEV5.md` and `UI_DEV5.md`. No milestone is promoted by this update.

## dev.6 candidate status

28 Python test methods passed. Windows UI regression tests are implemented and run by the installer before registration; their execution here is pending. C# build, native UI/DPI and CAD acceptance remain open. See `VALIDATION_DEV6.md`.

## dev.6 build fix 1

User log confirms the full 34 Python tests and HTTP smoke passed. C# compilation failed with CS1513 in WelcomeCardFactory.cs; the missing namespace brace is now corrected. Local lexical checks pass for 18 C# files, but native C# build, UI tests, registration and CAD acceptance remain open.

## dev.7 candidate status

35 Python application methods and five source-audit regressions passed here. Added equation-state and placeholder regression gates. Full dev.7 HTTP/schema, C# build/tests, native UI harness and SOLIDWORKS create/edit remain pending. See `VALIDATION_DEV7.md`.
