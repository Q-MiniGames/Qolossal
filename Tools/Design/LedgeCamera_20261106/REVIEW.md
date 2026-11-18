# Solid ledge camera framing — 7 November 2026

Only the main ground uses the 15% foot baseline. Solid ledges now use the same 40% baseline as platforms, retaining the selected framing through jumps and using the existing smoothing and camera safety bounds.

The scene inventory covered 683 active ground colliders across all 13 playable scenes. The underground authoring group separates raised stone landings and obstacles from the continuous cave and passage floor collision. All supports in that group receive platform framing. Floating TerrainPiece kinds are also recognized. Seven explicit CameraPlatformSurface markers cover the remaining authored solid overhangs, crossing and loft ledges across four surface/interior scenes. Main surface routes, catch floors and room floors remain ground. No height/width threshold or contact-point guessing determines the classification.

`verify_scene_changes.py` checks that scene serialization adds only those markers, their component references, and existing default-valued fields. All previous terrain vertices, colliders, positions, artwork and gameplay components remain unchanged. No map regeneration is required because this changes camera metadata, not geometry.

`playtest.cs` uses an isolated save and actual render-end measurements: solid Cistern ledge idle/jump, walking to and back from a one-way platform, platform jump, main cave floor, a Cradle solid overhang, and Qvale main ground/jump. It restores test input and Editor settings. `QA.json` records the results. The existing Cistern background prototype and disabled cave automatic edge look-down are preserved. Automatic edge anticipation is also suppressed on platforms and ledges; manual look-down and main surface-ground anticipation remain available.

Verification is in the Windows Unity Editor; console builds, PS5, Xbox Series X/S, Switch 2 handheld/docked and hardware performance are not tested. No 60 FPS or console-readiness claim. Original Switch remains excluded.
