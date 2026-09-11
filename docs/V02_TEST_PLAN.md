# Mechra v0.2 live acceptance

Candidate: **0.2.0-dev.3**. Record actual observations and attach the exported Task Pane log. No item below is automatically passed by the Python tests.

| Gate | Steps | Required observation | Status |
|---|---|---|---|
| Environment | Run `scripts/install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-clean` with SOLIDWORKS closed | PowerShell parser/ownership checks pass; old agent migration verified; Python/HTTP tests pass; C# build has no errors; pure C# runner passes; registration path/version verified | Pending |
| Load | Start SOLIDWORKS 2025 SP1.2 and enable Mechra | Task Pane welcome shows v0.2.0-dev.3; Model/New chat/Save log are visible; exported log identifies the expected DLL | Pending |
| Existing user Part | Send create in tesst.SLDPRT with Boss-Extrude1 | No create plan; File > New > Part guidance; original model stays unchanged | Pending |
| Plan | New blank Part; send `Tạo plate 100 x 60 x 5 mm` | Review card appears; no geometry is created before Apply | Pending |
| Cancel | Cancel a generated plan | Part remains unchanged | Pending |
| Create | Generate again and Apply | Native rectangle sketch, editable width/height dimensions and native extrusion; VERIFIED with 100/60/5 mm and 30,000 mm³ | Pending |
| Edit | Send `Đổi chiều dày thành 8 mm`; Apply | Existing extrusion edited, width/height stay 100/60, depth 8, volume 48,000 mm³ | Pending |
| Clarify | New Part; send `Tạo plate 100 x 60 mm`, reply `5 mm` | Missing value is requested; completed plan requires Apply | Pending |
| Square | New Part; create 60 x 60 x 5 mm | Two perpendicular driving dimensions; no duplicate same-axis dimension | Pending |
| Narrow/tall | New Part; create 40 x 80 x 5 mm, edit to 8 | Width/height stay 40/80; not swapped to make checks pass | Pending |
| Stale plan | Generate a plan; switch to a different Part or modify the Part; Apply | Refused; neither Part receives the stale operation | Pending |
| Preconditions | Read-only Part, open sketch edit, extra model features, multiple configurations/bodies | Explicit refusal with no geometry mutation | Pending |
| Undo | After successful Apply, choose its named `Mechra: ...` entry in the native Undo list | That transaction reverts; prior user work is retained | Pending |
| Failure recovery | In a disposable Part, collect an actual failed create/edit case | No success claim; if rollback reports verified, feature/body/dimension/volume checkpoint really matches | Pending |
| Preferences | Enable dimension input dialog before executing | Mechra avoids the dialog during work and restores the setting afterwards | Pending |
| UI | Resize Task Pane and try Windows display scaling 100%/150% | Review text scrolls; Apply and Cancel stay reachable | Pending |

Do not force a failure in valuable models. A failed native API call is reported with diagnostics; rollback success is claimed only if the measured checkpoint comparison passes. Crash recovery, full document checkpoints, repair planning, complete sketch constraints and arbitrary feature modifications are not implemented in this milestone.
