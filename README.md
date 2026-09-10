# Mechra v0.1.0 — Foundation

> **Project status:** v0.1.0 has been live-tested in SOLIDWORKS 2025 SP1.2 on Windows. It is intentionally read-only; native CAD mutation starts in v0.2.0.

> **Internal compatibility note:** the initial C# namespace/assembly still uses the early `SwCursor` codename to preserve the exact v0.1 integration path that was live-tested. Product-facing naming is Mechra. Migrate those internal identifiers as a controlled refactor in v0.2.


Clean-room foundation for Mechra, an AI-native SOLIDWORKS copilot. This starter deliberately does **not** copy code from SolidPilot or MecAgent. It uses the official SOLIDWORKS API surface and a provider-neutral local agent service.

## What Step 1 delivers

- Native SOLIDWORKS COM add-in skeleton (`ISwAddin`)
- SOLIDWORKS Task Pane chat/check UI
- Active-model context reader (document + feature tree)
- Persistent-reference service for stable entity identity
- Local provider-neutral agent service (FastAPI)
- Design-spec / CAD-plan contracts for future drawing-to-3D and text-to-3D
- PowerShell scripts for local development
- Product blueprint and MVP acceptance gates


## Project governance

- `docs/PROJECT_CHARTER.md` — canonical product definition and non-negotiable principles
- `docs/ROADMAP.md` — milestone roadmap through commercial beta
- `docs/MVP_ACCEPTANCE.md` — acceptance gates
- `docs/ARCHITECTURE.md` — system architecture
- `docs/RELEASE_PROCESS.md` — Git/version/release discipline
- `skills/mechra-development/SKILL.md` — standing AI development skill for this project
- `docs/GITHUB_SETUP.md` — clean first-push and branch workflow

## Architecture

```text
SOLIDWORKS
  └─ Native C# Add-in (.NET Framework 4.8)
      ├─ Task Pane UI
      ├─ Model Context Reader
      ├─ Persistent Reference Service
      └─ Local Agent Client (HTTP)
             ↓
        Agent Service (Python/FastAPI)
             ├─ Conversation / intent layer
             ├─ Design Spec state
             ├─ CAD plan contract
             └─ Model-provider adapter (mock in v0.1)
```

The next milestone replaces the mock agent with a real model provider and adds the first deterministic native Part command: create/edit a simple plate while verifying the result.

## Requirements

- Windows 10/11 x64
- SOLIDWORKS (target: 2024–2026; API references resolved from the installed copy)
- Visual Studio 2022 with .NET desktop build tools / .NET Framework 4.8 targeting pack
- Python 3.11+

## 1. Start the agent service

```powershell
cd src\AgentService
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --host 127.0.0.1 --port 8765
```

Check: `http://127.0.0.1:8765/health`

## 2. Build the SOLIDWORKS add-in

Open a Visual Studio Developer PowerShell:

```powershell
$env:SOLIDWORKS_API_DIR="C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
msbuild .\src\SolidWorksAddin\SolidWorksAddin.csproj /p:Configuration=Debug
```

If your SOLIDWORKS API DLLs live elsewhere, point `SOLIDWORKS_API_DIR` to the directory containing:

- `SolidWorks.Interop.sldworks.dll`
- `SolidWorks.Interop.swconst.dll`
- `SolidWorks.Interop.swpublished.dll`

## 3. Register the add-in (Developer mode)

Run PowerShell as Administrator:

```powershell
.\scripts\register-addin.ps1 -Configuration Debug
```

Then start SOLIDWORKS and enable **Mechra** under `Tools > Add-Ins` if it is not auto-enabled.

## Current v0.1 behavior

The Task Pane can read the active document and feature tree and send that context to the local agent service. The included provider is intentionally a mock: Step 1 proves the product boundary before any AI is allowed to mutate CAD.

## Commercial direction

This foundation is intentionally provider-neutral and clean-room. Keep model providers, CAD execution, vision, licensing, telemetry and billing behind interfaces so they can evolve independently.
