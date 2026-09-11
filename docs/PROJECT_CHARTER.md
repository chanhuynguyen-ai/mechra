# Mechra — Project Charter

## Product identity

**Name:** Mechra  
**Category:** AI Mechanical Design Environment / SOLIDWORKS Copilot  
**Primary platform:** SOLIDWORKS on Windows  
**Commercial intent:** packaged product for personal use first, then commercial distribution.

## Product promise

Mechra helps a mechanical designer move from intent to verified native SOLIDWORKS geometry through a continuous AI-assisted loop:

`Understand → Clarify → Plan → Execute → Verify → Repair → Iterate → Complete`

The product is not a generic chatbot and is not a STEP-only text-to-CAD generator. The target is a native, editable, parametric SOLIDWORKS workflow.

## Core use cases

1. **Technical drawing / image → native 3D**: understand views, dimensions, notes and engineering symbols; explicitly detect missing, ambiguous or conflicting information; ask focused questions; then build an editable native model.
2. **Text / idea → native 3D**: turn natural-language mechanical intent into a Design Spec, clarification dialogue, CAD plan and native parametric features.
3. **Native 3D → technical drawing**: generate views, dimensions, annotations and later company standards/GD&T with explicit verification gates.
4. **Check & repair**: inspect feature tree, sketches, rebuild state and references; explain root cause; preview repairs; apply; rebuild and verify.
5. **Iterative optimization**: respond to user corrections and improve the same model instead of throwing away design intent.
6. **Engineering copilot**: later add calculations, DFM, standard-part lookup, materials/process guidance and project/company rules.

## Differentiation

The initial wedge is:

**Technical Drawing → Clarification → Verified Native SOLIDWORKS Model**

The product must be especially strong at knowing what is *not* known. Every engineering value is tracked with provenance and one of: `confirmed`, `inferred`, `assumed`, `missing`, `ambiguous`, `conflicting`.

## Non-negotiable engineering principles

- Native SOLIDWORKS features first; neutral CAD export is secondary.
- The LLM never calls raw COM operations directly.
- CAD mutations use deterministic, versioned intents/contracts.
- Every mutation follows `Plan → Preflight → Checkpoint → Execute → Rebuild → Verify → Commit`; failures lead to rollback or repair.
- Persistent references plus semantic fallback are preferred over raw face/edge indices.
- Missing or critical engineering data is never silently promoted to confirmed.
- Destructive/low-confidence edits require preview or explicit approval.
- Model providers are replaceable; the CAD engine must not depend on one vendor.
- Claims of correctness require measurable verification.
- Commercial code remains clean-room: do not copy AGPL/proprietary implementation code into the product core.

## Definition of “done”

A milestone is complete only when its acceptance gate passes on a real Windows + SOLIDWORKS installation and the result is recorded in the changelog/roadmap. A demo that produces geometry but cannot edit, rebuild, verify or recover is not considered complete.
