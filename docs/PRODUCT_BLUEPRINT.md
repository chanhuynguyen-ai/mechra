# Mechra Product Blueprint — AI Mechanical Design Environment for SOLIDWORKS

## Product definition

An AI-native mechanical design copilot embedded in SOLIDWORKS. The agent can understand design intent from text, images and technical drawings; identify missing or conflicting engineering information; create and edit native parametric models; generate drawings from 3D; diagnose modeling errors; propose repairs; verify results; and iterate with the engineer until the design is complete.

## Benchmark direction

MecAgent validates demand for in-CAD natural-language automation, macro generation, simple text-to-CAD, 3D-to-drawing automation, engineering assistance and catalog-part search. Our wedge should not be “a chat box that generates CAD macros.” The initial differentiation is a closed design loop:

1. **Understand** technical drawing / image / text.
2. **Represent** known, inferred, assumed, missing, ambiguous and conflicting requirements.
3. **Clarify** only information that blocks or materially changes the design.
4. **Plan** native parametric SOLIDWORKS features.
5. **Act** directly on the native model.
6. **Verify** geometry, references, dimensions and rebuild state.
7. **Repair** failures or accept user corrections and iterate.

## Product pillars

### 1. Drawing/Image → Native 3D
Input: photo, scan, screenshot, PDF/DXF/DWG later.
Output: editable native SOLIDWORKS Part/Assembly with explicit assumptions and unresolved items.

### 2. Text → Native 3D
Natural-language idea/spec → clarification → CAD plan → native features.

### 3. Native 3D → Technical Drawing
View selection, sheet/template, dimensions, annotations, BOM/table primitives, center marks and user/company standards. GD&T is a later gated capability.

### 4. Model Check & Repair
Read rebuild errors, feature tree, sketch status and references; explain the root cause; preview and apply repairs; verify again.

### 5. Engineering Copilot
Design calculations, material/process suggestions, DFM checks, standards lookup and project-specific rules. Recommendations must distinguish verified facts from assumptions.

### 6. CAD Automation
Natural-language batch operations: rename, properties, materials, export, drawing generation, metadata, repetitive features and company workflows.

## The key data model: Design Spec

Every engineering value carries provenance and status, not just a number.

```json
{
  "field": "plate_thickness",
  "value": null,
  "unit": "mm",
  "status": "missing",
  "source": null,
  "confidence": 0.0,
  "required_for": ["base_extrude"]
}
```

Allowed states:

- `confirmed` — explicitly defined by drawing/user/model
- `inferred` — logically inferred with evidence
- `assumed` — chosen default; user-visible
- `missing` — required but absent
- `ambiguous` — multiple plausible interpretations
- `conflicting` — sources disagree

## Safety/product rule

The agent never silently converts an unresolved engineering assumption into a “confirmed” value. Before manufacturing-readiness claims, all fit-, tolerance-, material-, safety- and process-critical items must be explicitly resolved or flagged.
