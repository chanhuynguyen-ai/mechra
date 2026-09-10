---
name: mechra-development
description: Develop Mechra, an AI-native mechanical design environment and Cursor-like copilot for SOLIDWORKS. Use for every architecture, coding, debugging, roadmap, CAD-agent, technical-drawing vision, native modeling, repair, verification, UI, packaging, testing, Git/release, and commercialization task for this project.
---

# Mechra Development Skill

## 1. Product definition

Mechra is a **Cursor-like AI Mechanical Design Environment for SOLIDWORKS**. Its target experience is continuous interaction with the engineer and the current native model, not one-shot text-to-mesh or text-to-STEP.

The canonical loop is:

`UNDERSTAND → CLARIFY → PLAN → EXECUTE → VERIFY → REPAIR → ITERATE → COMPLETE`

Primary workflows:
- technical drawing/image → Design Spec → clarification → native 3D;
- text/idea → Design Spec → native parametric 3D;
- native 3D → technical drawing;
- existing model → check/diagnose/repair;
- iterative user corrections/optimization on the same design.

## 2. Current canonical roadmap

Never skip the current milestone just to demo a later capability.

- **v0.1 Foundation**: add-in, Task Pane, context, local agent, persistent-reference foundation, contracts. COMPLETE.
- **v0.2 Text → Native Part**: deterministic create/edit/verify vertical slice.
- **v0.3 Check & Repair**: diagnose, preview, repair, rebuild, verify.
- **v0.4 Drawing Vision → Design Spec**: views/dimensions/notes + missing/ambiguous/conflicting detection.
- **v0.5 Drawing → Native 3D**: constrained drawing class to verified native part.
- **v0.6 Advanced Text → 3D**: expand feature vocabulary and editing.
- **v0.7 3D → Drawing**.
- **v0.8 Assembly Agent**.
- **v0.9 Engineering Intelligence/DFM/standards**.
- **v1.0 Commercial Beta**.

`docs/ROADMAP.md` and `docs/MVP_ACCEPTANCE.md` are the source of truth when they are newer than this summary.

## 3. Architecture boundaries

### SOLIDWORKS Add-in — C#/.NET Framework x64
Owns:
- `ISwAddin` lifecycle;
- native Task Pane/UI integration;
- active document, selections and model context;
- SOLIDWORKS COM calls;
- deterministic CAD executor;
- checkpoints/undo integration;
- persistent/semantic reference resolution;
- rebuild/read-back verification hooks.

### Local Agent Runtime — Python
Owns:
- conversation/session state;
- intent routing;
- Design Spec;
- clarification policy;
- CAD planning;
- model-provider adapters;
- vision/drawing reasoning;
- repair planning;
- high-level verification orchestration.

### Shared Contracts
Own versioned schemas for:
- model context;
- Design Spec;
- CAD Plan/intents;
- execution results;
- verification reports;
- repair plans;
- drawing analysis.

### Hard boundary
**No LLM/provider directly calls raw SOLIDWORKS COM.** The model emits validated, versioned intents; deterministic C# execution owns CAD mutations.

## 4. Design Spec rules

Every engineering fact must retain:
- value;
- unit;
- status;
- source/provenance;
- confidence when inferred;
- what operation depends on it.

Allowed states:
- `confirmed`
- `inferred`
- `assumed`
- `missing`
- `ambiguous`
- `conflicting`

Never silently convert `missing`, `ambiguous`, `conflicting` or an important `assumed` value to `confirmed`.

Ask a clarification question only when the information blocks the build, changes design intent materially, affects fit/tolerance/manufacturing/safety, or confidence is too low. Otherwise proceed with an explicit reversible assumption.

## 5. CAD mutation transaction

All write operations must evolve toward:

`Plan → Validate → Preflight → Checkpoint → Execute → Rebuild → Read-back → Verify → Commit`

Failure path:

`failure → capture diagnostics → rollback when safe → repair plan → preview/approval if needed → retry → verify`

A successful COM call is not a successful CAD task. Success requires post-rebuild verification against intended dimensions/geometry/state.

## 6. Native-first rule

For SOLIDWORKS mode:
- prefer native sketches, dimensions, features, mates, drawings and editable properties;
- do not substitute imported dumb STEP bodies when the requested workflow is native parametric design;
- neutral formats (STEP/STL/DXF/PDF) are import/export/handoff formats, not the primary editable representation.

## 7. Reference stability

Never make raw face/edge list indices the long-term primary identity.

Preferred resolution:
1. SOLIDWORKS Persistent Reference ID where available;
2. validate against semantic signature;
3. semantic fallback candidate search using owner feature, topology/surface type, normal/direction, area/length/radius, centroid/endpoints, adjacency and relative position;
4. if confidence is insufficient or edit is destructive, show preview/ask approval.

## 8. Drawing/image → 3D discipline

Do not use a single vision model output as unquestioned geometry truth.

Pipeline concept:
`Input → view/layout analysis → geometry primitives → dimensions/text/symbols → association → projection reasoning → Drawing Graph → Design Spec → clarification → CAD Plan → build → verify`

Start with bounded mechanical parts and basic orthographic drawings. Expand symbols/standards only after benchmarked reliability.

The agent must explicitly distinguish what the drawing states from what it inferred.

## 9. Check & Repair discipline

When asked to “check”, inspect before modifying. Gather:
- document/rebuild status;
- feature tree and dependencies;
- sketch definition/error state;
- selections/references;
- dimensions/parameters;
- failed/suppressed features;
- relevant geometry signatures.

Return root cause, affected features and proposed change. For destructive or low-confidence fixes, preview first. After applying, rebuild and verify again.

## 10. UI/UX direction

Mechra should feel like a native professional design environment, not a debug form.
- Primary experience lives inside SOLIDWORKS Task Pane/add-in.
- Dark, modern, compact, engineering/business-oriented UI.
- Conversation is contextual to current part/assembly/drawing/selection.
- Show status: analyzing, clarification required, plan ready, executing, verifying, passed/failed.
- Support plan/preview/apply/undo rather than opaque autonomous edits.
- Expose assumptions, warnings and verification results clearly.

## 11. AI/provider strategy

Provider-neutral core. Implement adapters rather than coupling CAD logic to a model vendor.
Potential providers include OpenAI, Anthropic, Gemini and local models. Provider changes must not require rewriting the SOLIDWORKS executor or contract layer.

## 12. Commercial/clean-room rules

Mechra is intended to become a distributable commercial product.
- Prefer official SOLIDWORKS API documentation and independently written implementation.
- Open-source MIT/Apache components may be evaluated under their terms.
- Treat AGPL projects such as SolidPilot as reference/benchmark unless licensing strategy explicitly permits incorporation.
- Do not copy proprietary competitors such as MecAgent.
- Track third-party dependencies and licenses.
- Before commercial launch: signed binaries, installer/updater, secrets handling, auth/licensing, privacy/security, crash recovery, diagnostics, telemetry controls, compatibility matrix and EULA/privacy review.

## 13. Development workflow for every task

When working on Mechra:
1. Inspect the current repository state and read `VERSION`, `CHANGELOG.md`, `docs/ROADMAP.md`, `docs/MVP_ACCEPTANCE.md`, and relevant code before changing anything.
2. Identify the current milestone and its smallest missing acceptance criterion.
3. Implement the smallest end-to-end vertical slice that moves that criterion to passing.
4. Preserve architecture boundaries and backward compatibility unless a migration is intentional/documented.
5. Add/update tests and contracts before claiming success.
6. Distinguish tests that ran in the assistant environment from tests that require the user's Windows/SOLIDWORKS machine.
7. Provide exact Windows commands for the live test.
8. Never claim a live SOLIDWORKS behavior passed until the user supplies successful output/screenshot or a connected execution environment proves it.
9. On success, update acceptance gate, changelog and roadmap; then recommend a clean commit/tag.
10. Avoid broad rewrites while a vertical slice is not yet stable.

## 14. Git/release discipline

- `main` = accepted milestone/release state.
- Develop next milestone on `dev` or feature branches.
- Conventional Commits.
- Semantic version tags.
- Do not commit `.venv`, `bin`, `obj`, generated TLBs, secrets, API keys or local SolidWorks binaries/interops.
- Commit source/contracts/docs/scripts only.

## 15. Response/implementation quality

For this project, do not stop at high-level advice when implementation is requested. Produce working source/patches/files and a deterministic run/test path.

Do not hide uncertainty. If SOLIDWORKS API behavior, provider API, licensing or current external product capability can have changed, verify against current authoritative sources before locking an implementation decision.

Keep the user's end goal visible: a reliable personal tool that can mature into a sellable product, not a one-off demo.
