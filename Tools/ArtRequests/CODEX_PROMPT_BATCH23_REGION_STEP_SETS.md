Qolossal: Codex Batch 23 (a painted step set for every other region, built the way Qvale's was in Batches 16e to 16g). Do 23a first and stop for Claude's review. Do 23b to 23g only after 23a passes, then stop once more at the end.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch23a\ (then Batch23b ... Batch23g, one folder per region)
Copy your context files into each folder's Integrator\Context\, as for Batch 16g.

WHY
Qvale's steps now look right in the game. Each riser gets one purpose-built painted piece (or an Outer, wall courses and an Inner when tall), every join sits in a painted fade or meets the strip at a natural joint, and nothing is cropped. Every other region still builds its steps from cliff pieces cut to fit, so the corners show the same faults Qvale had: straight vertical and horizontal cuts, doubled edges where a corner overlaps the strip, repeated wall texture and slanted faces where a step sits under a slope. The user wants the Qvale method in every level.

THE REGIONS (one sub-batch each; measured on 10 Oct 2026 from the built scenes)

| Sub-batch | Region | Family prefix (new names) | Accepted surface to match | Risers: short / medium / tall | Slope tops |
|---|---|---|---|---|---|
| 23a | MR01 Cradle | Terrain/Body/Body_Step_ | Body_Top_Strip, Body_Fill, the Body cliff kit | 4 / 2 / 4 | 2 |
| 23b | MR02 Causeway | Terrain/Body/Causeway/Causeway_Step_ | Causeway_Top_Strip, Causeway_Fill, the Causeway cliff kit | 9 / 5 / 21 | 14 |
| 23c | MR03 Terraces | Terrain/Body/Terraces/Terraces_Step_ | Terraces_Top_Strip, Terraces_Fill, the Terraces cliff kit | 10 / 12 / 28 | 24 |
| 23d | MR05 Ribwood | Terrain/A2_Step_ | A2_Ground_Top, A2_Ground_Fill, A2_Wall_Side and A2 corners | 3 / 11 / 26 | 15 |
| 23e | MR06 Heights | Terrain/A3_Step_ | A3_Ground_Top, A3_Ground_Fill, A3_Wall_Side and A3 corners | 7 / 8 / 32 | 19 |
| 23f | MR07 Summit | Terrain/A6_Step_ | A6_Ground_Top, A6_Ground_Fill, A6_Wall_Side and A6 corners | 1 / 5 / 29 | 9 |
| 23g | MR08 Descent | Terrain/A4_Step_ | A4_Ground_Top, A4_Ground_Fill, A4_Wall_Side and A4 corners | 0 / 3 / 28 | 14 |

Short is a riser up to 1.3 u, medium is 1.3 to 2.05 u, and tall is over 2.05 u (up to 8.97 u, in MR07). Heights measured: short 0.90 to 1.30, medium 1.36 to 2.05. Slopes beside a sloped top run from 0.13 to 0.75 rise per unit, with a median of 0.4.

The exact walk lines and every riser (x, top, foot, height, facing, which piece it gets, slope beside its top and its foot) are in Tools\ArtRequests\Batch23\<region>_surface.json. "faces L" means the open face looks left and the upper ground is on the right.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. The Qvale method, in order: Tools\ArtRequests\CODEX_PROMPT_BATCH16E_QVALE_STEP_SET.md, _16F_, _16G_, and Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_42.md to CLAUDE_REVIEW_46.md. Also read your own Batch16e, 16f and 16g deliveries and their REGISTRATION.json files. **This batch copies their canvases, registration and assembly exactly.** Only the material changes.
3. Assets\MountainReliefAtlas\Scripts\MraTerrain.cs: StepParts, StepPiece, Grid, StepSeams, Joint and StripAlpha. That is how the pieces are placed today, and it will be reused unchanged.
4. Tools\ArtRequests\Batch23\<region>_surface.json for the region you are painting.
5. The region's accepted strip, fill and face art (table above), in Assets\Art\Codex\Terrain\, and its current game frames in Tools\MountainReliefAtlas\QA\.
6. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1192 files today). Re-read it; don't trust the count.

WHAT TO MAKE (per region: 17 PNGs, 120 PPU, RGBA, no mipmaps, each with a REGISTRATION.json; no accepted file replaced; all names checked free on 10 Oct)

Use the family prefix from the table, e.g. Terrain/Body/Terraces/Terraces_Step_Short_L.

| Piece | Canvas | Registration (same as Qvale) |
|---|---|---|
| Step_Short_L / _R | 768 x 512 | riser line col 256 (L; 512 for R), upper walk row 160, lower walk row 285 |
| Step_Medium_L / _R | 768 x 640 | riser col 256 / 512, upper walk row 160, lower walk row 370 |
| Step_Outer_L / _R | 768 x 512 | riser col 256 / 512, upper walk row 160, face ends at a joint at row 448, fades to alpha 0 by row 512 |
| Step_Wall_A / _B / _C | 384 x 240, Repeat V | riser col 128 (L, mirrored for R); rows 0 and 239 join seamlessly to any other variant; soil side fades out over the last 96 columns |
| Step_Inner_L / _R | 768 x 512 | face fades in over rows 0 to 64, lower walk row 352, lower run 256 px |
| Step_Short_SlopeTop_L / _R | 768 x 768 | riser col 256 / 512, upper walk row 416 at the riser, climbing 0.4 px per px inland, lower walk row 541 |
| Step_Medium_SlopeTop_L / _R | 768 x 800 | riser col 256 / 512, upper walk row 310 at the riser, climbing 0.4 px per px inland, lower walk row 520 |
| Step_Outer_SlopeTop_L / _R | 768 x 768 | riser col 256 / 512, upper walk row 385 at the riser, climbing 0.4 px per px inland; face ends at a joint 288 px below (row 673), then fades over 64 rows |

Each SlopeTop REGISTRATION.json lists its painted upper walk line as (column, row) points every 32 px inland. The game places a slope-top piece unbent. Where the real slope differs from 0.4 (0.13 to 0.75), the game bends only the upper run by the difference, never the face, so keep the inland 512 px free of strong vertical features.

MATERIAL (the one thing that differs from Qvale)
- **The face is the region's own rock, not Qvale's masonry.** Use the region's accepted cliff kit (Body, Causeway, Terraces) or its Wall_Side and corner art (A2, A3, A4, A6): the same rock, colours, light, crack density and edge character. Qvale's steps are built town steps; these are natural ledges and outcrops. Show no dressed blocks unless the region's own cliff art has them (the Causeway road edge may).
- **The upper run reproduces the region's own top strip.** From the riser line inland, the run's grass, moss, edge and soil are the accepted strip's own columns 0 onward, row for row (mirrored on R pieces). The game lays each tread's strip so its column 0 sits on the riser line, and then cuts from piece to strip at a natural joint 1.0 to 2.55 u inland (StepSeams and Joint). So the two must be the same pixels there. The A-family strips are 2048 px wide: use their columns 0 to 511.
- **Name the strip's joints.** For each region, list in REGISTRATION.json the strip columns where a cut is least visible (a gap between grass clumps, a crack or joint in the edge). Give at least one every 120 px, like Qvale's mortar joints. The game cuts only at those columns.
- The lower run starts against the foot of the face with a natural tuft or stone, never a straight start. It runs 256 px in the strip's look and ends in a soft fade or a low tuft.
- Grass height matches the strip's own grass line. Use no raised hedge blocks or flat-topped clumps (Review 42). At the step edge, the grass and moss spill over in a few irregular tufts.
- **Stretch:** the game stretches the face between the two walk rows to fit the real riser. Short pieces stretch 0.87 to 1.25x and Medium pieces 0.78 to 1.17x. Paint those faces so a stretch can't be seen: irregular rock with no stacked horizontal courses and no round features.
- Below the kerbs is the region's fill soil, fading to alpha 0 over the bottom 96 rows and the 96 columns at each side.

NON-NEGOTIABLES
- No piece edge shows as a straight line anywhere. Every join is a painted fade or meets the strip at a registered joint.
- Scale (120 PPU), colour and texture match the region's accepted strip, fill and face. Nothing pale or glaring in the soil (Qvale's pale cracks glared).
- Mystery rule (QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md sections 2, 10, 11): nothing body-like, no anatomy, no faces in the rock.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt.
- Paint at native size or downscale. Never upscale, and never resample X and Y independently. Archive native outputs with their prompts and settings in Batch23x\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).
- If a strip itself makes a clean join impossible, say so in the handoff and propose a `_v2` strip. Don't change the accepted one.

PROOFS (per region)
- The region's surface from <region>_surface.json, assembled exactly as MraTerrain.StepParts does, with the accepted fill, strip, sky and depth layers. Render at 1920 x 1080 and 1280 x 720. Include three windows chosen to show the most risers, plus a camera-height crop of the five tallest risers, five slope tops and every short step. Show Qori (accepted 144 PPU composite) at the top and foot of three steps.
- The same windows from today's game (from QA frames, or rendered with the current cliff kit) beside them.
- A 4x crop of every kind of join: piece to strip, piece to fill, wall to wall, outer to wall, wall to inner, and a slope-top's strip join.
- Each piece at 1:1 over the fill and over sky, plus the three walls stacked in every order.
- The stretch extremes: a Short at 0.87x and 1.25x, and a Medium at 0.78x and 1.17x.

DELIVERABLES (per sub-batch)
- The 17 PNGs with REGISTRATION.json files (canvas, PPU, riser column, walk rows, joint rows, fade bands, slope-top walk lines, strip joint columns), plus Batch23x\MANIFEST.json (sha256, size, PPU, pivot, wrap).
- The QA sheet, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

ORDER
23a (MR01 Cradle) first, then stop. It is the pilot for natural rock: 10 risers, the first steps players see. After Claude's review passes it, do 23b to 23g in any order, each in its own folder, and stop once at the end.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
