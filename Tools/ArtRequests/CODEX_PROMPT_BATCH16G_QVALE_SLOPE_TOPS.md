Qolossal: Codex Batch 16g (Qvale: step pieces painted for a slope at their top). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16g\
Copy your context files into Batch16g\Integrator\Context\, as for Batch 16f.

WHY
The Batch 16e/16f step set is in the game (Reviews 42-43) and the flat steps look right. Three steps sit at the bottom of a slope: the ground climbs away from the step's edge. The game fits a flat-topped piece there by moving each pixel column up to follow the slope, and that leans the stone face: the blocks of the step's face slant and look stretched (the user's crop: Tools\ArtRequests\Batch16g\user_crop_stretch.png). Shearing can't fix painted masonry. These steps need pieces painted with the slope already in them, so the game places them without any bending.

The three in Qvale (from Tools\ArtRequests\Batch16e\qvale_surface.json):
- Riser 3, x 29.208, 1.635 u high, faces R: the ground rises at 0.293 going left from its top (Medium).
- Riser 6, x 49.547, 1.045 u high, faces R: the ground rises at 0.5 going left from its top (Short).
- Riser 8, x 234.5, 6.25 u high, faces L: the ground rises going right from its top along the surface points (236.12, 18.34), (236.481, 18.578), (236.859, 18.762), (237.255, 18.894), (237.669, 18.974), (238.1, 19.0), then flat (Outer, over the wall courses).

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_42.md and CLAUDE_REVIEW_43.md, and your Batch16e and Batch16f deliveries (the pieces and REGISTRATION.json files these are based on).
3. Current game frames: Tools\MountainReliefAtlas\QA\QvaleInteriors\batch16ef\ (set_riser3_*, set_riser6_*, set_riser8_*, set_slopes_*, set_east_*).
4. Tools\ArtImport\codex_v2_accepted.json: 1185 accepted files today. Re-read it.

WHAT TO MAKE (6 new PNGs under Terrain\Body\Qvale\, 120 PPU, RGBA, no mipmaps, each with a REGISTRATION.json; names checked free on 9 Oct)
For each, the step face is upright masonry exactly like its 16e/16f piece (same stones, same riser line, same lower kerb and lower walk row offset). Only the upper run changes: the kerb stones and the low grass of 16f climb the slope from the step's top corner inland, the way the strip's kerb follows a slope, with each kerb stone sitting level-bedded in its course and the courses stepping up the slope, as a builder would lay them. The top corner stone stays square and upright on the face. The soil under the upper run follows the slope. No raised clumps, no straight edges, the same 192 px fade at the inland end, the same soil fades.

1. Qvale_Step_Short_SlopeTop_R (768 x 768), and its mirror _L: from Qvale_Step_Short_R_v2. Riser line column 512. The upper walk line is at row 416 at the riser and rises 0.5 px per px inland (to the left), reaching row 160 at column 0. Lower walk row 541.
2. Qvale_Step_Medium_SlopeTop_R (768 x 800), and its mirror _L: from Qvale_Step_Medium_R_v2. Riser line column 512. The upper walk line is at row 310 at the riser and rises 0.293 px per px inland, reaching row 160 at column 0. Lower walk row 520. The game may stretch only the face between the two walk rows by up to 8%, as before.
3. Qvale_Step_Outer_SlopeTop_L (768 x 768), and its mirror _R: from Qvale_Step_Outer_L_v2. Riser line column 256. The upper walk line is at row 385 at the riser and follows riser 8's surface points above (1 u = 120 px) inland to the right, then runs flat. Its stone column ends at a mortar joint 288 px below the riser's walk row (row 673), fading over the next 64 rows like the 16e Outer, so the existing wall courses continue under it unchanged.

Registration per piece: canvas, PPU, riser line column, upper walk row at the riser, the upper walk line as a list of (column, row) points every 32 px inland, the lower walk row (or joint row), and the fade bands. The game will place each piece by its riser line and upper walk row and draw it unbent, so the painted walk line must match those points within 2 px.

NON-NEGOTIABLES
- Same colours, stones, grass, soil and scale as the 16e/16f set; the strip (Qvale_Top_Strip_v3) must be able to fade in over the last 192 px of the upper run with no visible change of kerb height or angle.
- No stretched or slanted stones anywhere; no straight edges; nothing pale in the soil; mystery rule: nothing body-like.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt.
- Paint at native size or downscale; never upscale or resample X and Y independently. Archive native outputs with prompts and settings in Batch16g\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).

PROOFS
- Risers 3, 6 and 8 rebuilt from qvale_surface.json with these pieces (unbent) plus the accepted strip, fill, wall courses and Inner, at 1920 x 1080 and 1280 x 720, beside the matching set_riser*_1080.png game frames. Qori standing on the slope just above each step and at its foot.
- A 4x crop of each step's top corner and of the strip join on the slope.
- Each piece at 1:1 over Qvale_Fill and over sky blue, with its registered walk line drawn as a thin overlay (proof copy only).

DELIVERABLES
- The 6 PNGs with REGISTRATION.json files, Batch16g\MANIFEST.json (sha256, size, PPU, pivot, wrap), the QA sheet, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
