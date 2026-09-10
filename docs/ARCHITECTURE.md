# Architecture v0.1 → Commercial Target

```text
┌──────────────────────────────── SOLIDWORKS ───────────────────────────────┐
│ Native Add-in                                                            │
│ ┌────────────┐  ┌────────────────┐  ┌─────────────────────────────────┐ │
│ │ Task Pane  │  │ Context Reader │  │ Native CAD Execution            │ │
│ │ Chat/Files │  │ feature/model  │  │ (introduced milestone v0.2)    │ │
│ └─────┬──────┘  └───────┬────────┘  └──────────────┬──────────────────┘ │
│       │                 │                           │                    │
│       └──────────────┬──┴───────────────────────────┘                    │
│                      │ Persistent/Semantic References                   │
└──────────────────────┼───────────────────────────────────────────────────┘
                       │ localhost HTTPS/HTTP + versioned contracts
                       ▼
┌──────────────────────────── Local Agent Runtime ─────────────────────────┐
│ Session/History → Intent Router → Design Spec → CAD Planner → Verifier  │
│                                ↑             │             │             │
│ Vision/Drawings ────────────────┘             └→ Repair ────┘             │
│                                                                         │
│ Model Router: OpenAI / Anthropic / Gemini / local model                 │
└─────────────────────────────────────────────────────────────────────────┘
```

## Why native Add-in first

The commercial product must understand the user's current model, selections and errors without forcing them to leave SOLIDWORKS. The official API supports COM add-ins through `ISwAddin` and .NET controls in the SOLIDWORKS Task Pane. The add-in therefore becomes the stable product boundary.

## Stable entity identity

Do not make face/edge indices the primary identity. Store SOLIDWORKS Persistent Reference IDs where available, plus a semantic fallback signature:

- owning feature
- topology type
- face normal / surface type
- area / length / radius
- centroid / endpoints
- adjacency
- relative position

Resolution order:

1. Persistent reference resolves.
2. Validate semantic signature.
3. If stale, semantic re-resolution proposes candidates.
4. If confidence is low or change is destructive, require preview/user approval.

## Transaction model for CAD mutations

Every mutation should eventually be a transaction:

```text
Plan → Preflight → Checkpoint → Execute → Rebuild → Verify → Commit
                                      └─failure→ Rollback/Repair
```

No AI provider should call raw SOLIDWORKS COM methods directly. Models emit versioned CAD intents; deterministic executors own COM operations.
