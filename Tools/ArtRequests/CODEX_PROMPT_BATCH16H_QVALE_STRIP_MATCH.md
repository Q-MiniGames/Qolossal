Qolossal: Codex Batch 16h (Qvale kerb strip colour-matched to the step set). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16h\
Copy your context files into Batch16h\Integrator\Context\, as for Batch 16g.

WHY
The Qvale step set (Batches 16e, 16f, 16g) is in the game and every step now joins the kerb strip at a mortar joint. The user's in-game screenshot (Tools\ArtRequests\Batch16h\user_colour_difference.png) shows the remaining problem: the strip's kerb stones (Qvale_Top_Strip_v3) are paler, cooler and flatter than the step pieces' stones, so each run of strip between two steps reads as a different material. The band of soil just under the strip's kerb is also flatter than under the pieces' kerbs.

Measured on 9 Oct (opaque pixels, per channel p10 / p50 / p90):
- Step pieces' upper kerb rows (e.g. Qvale_Step_Short_L_v2 rows 165-212, columns 300-560): R 97/165/189, G 84/141/164, B 38/104/125.
- Strip v3 kerb rows 100-147: R 117/179/195, G 101/156/171, B 71/120/132.
- Pieces' soil band under the kerb (rows 214-264): R 108/128/183, G 79/96/151, B 52/65/111.
- Strip v3 band under the kerb (rows 150-200): R 108/123/141, G 79/92/110, B 54/63/80.
The pieces are the reference: they carry most of the stone on screen (every step face and corner).

WHAT TO MAKE (1 new PNG under Terrain\Body\Qvale\, name checked free on 9 Oct)
Qvale_Top_Strip_v4: Qvale_Top_Strip_v3 with its kerb stones, mortar joints, moss on the stones and the band under the kerb regraded to match the step set, so a stone in the strip and a stone in a step piece side by side read as the same stone from the same quarry:
- Same warmth, value and contrast as the pieces' kerb stones: darker, crisper mortar joints and shadowed undersides, warmer highlights. Match the reference percentiles above within about 6 levels per channel, measured the same way.
- The band under the kerb matches the pieces' (stones' undersides and contact shadow), then runs into the umber soil (Qvale_Fill) exactly as v3 does.
- Keep it as sharp as the pieces at 1:1 (no softening).
- Everything else identical to v3: canvas 1024 x 512, the alpha channel byte for byte, the grass and flowers above the walk line (row 96) untouched, the stone shapes and joint positions unchanged (the game places step seams on v3's joint columns 20, 69, 145, 230, 297, 389, 477, 571, 587, 666, 740, 854, 937, 985), seamless in U.
- Paint or grade only; no geometry change.

NON-NEGOTIABLES
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt (1191 accepted today; re-read the manifest).
- Archive any native outputs with prompts and settings in Batch16h\sources\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).
- Mystery rule: nothing body-like.

PROOFS
- The Qvale street at the user's spot (risers 5 and 6: Tools\MountainReliefAtlas\QA\QvaleInteriors\batch16g\slope_riser6_1080.png is the current game frame) and at risers 3 and 4, rebuilt with v4 beside the same with v3, at 1920 x 1080 and 1280 x 720.
- A 4x crop of four strip-to-piece seams (two on an upper run, two on a lower run).
- The measured percentiles of v4's kerb rows and under-kerb band next to the targets above.
- A difference mask proving the alpha channel, the grass above row 96 and the stone outlines are unchanged.

DELIVERABLES
- The PNG, Batch16h\MANIFEST.json (sha256, size, PPU 120, pivot [0.5, 0], Repeat U / Clamp V), the QA sheet, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
