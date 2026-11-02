Qolossal: Codex Batch 35d. Repaint BG_Causeway_Mid_4K and BG_Causeway_Near_4K for MR02, the Long Causeway: the aqueduct moves into the Mid. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch35d\
Keep the Batch35 Sky, Far and High byte-identical and use them in every proof. Don't redeliver them.

WHY (Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_91.md; the user picked option 1)
Arcades on the tall Near plane either float (35b) or hang on thin rock ribbons (35c). An aqueduct belongs on the valley wall, standing on real ground. So it moves to the Mid, and the Near becomes broad grounded rock.

READ FIRST
1. CLAUDE_REVIEW_90.md and CLAUDE_REVIEW_91.md.
2. Tools\ArtRequests\CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md, sections 2 and 4-6: the rules not changed here.
3. Your Batch35b package: reuse its arcade designs (the arch shapes, ruin states, palette and planting) as the design source for the Mid aqueduct. Repaint them at the Mid's scale and softness; don't paste 35b pixels.

MAKE
1. **BG_Causeway_Mid_4K, 3840x2700, depth 10, its median skyline at rows 300-400, moving at 0.35 of the real rate** (as Batch 35).
   - The far side of the valley, side-on: a long slope of limestone, scrub and olive groves.
   - **The old aqueduct marches along it on real ground**: runs of arches standing on the slope and its crest, stepping up as the slope rises, with one or two broken gaps. The old road follows below it on retaining walls.
   - Where the aqueduct runs along the crest, its arches form part of the skyline. The sky and the Far show through the arch openings (alpha 0 inside them).
   - Every pier stands on painted ground. Below the skyline the plane is opaque, apart from those arch openings.
   - Softer and slightly cooler than the Near, warmer than the Far. The arches read clearly at game size but stay quieter than the gameplay terrain.
   - Varied arch widths, heights and ruin; never an evenly spaced row. No long straight limb-like ridge.
2. **BG_Causeway_Near_4K, 3840x7528, depth 5, real rate, its bottom edge at the frame's bottom at the start.**
   - **Two or three broad rock buttresses** rising from the canvas's bottom edge: warm limestone, stepped and irregular, each 900-1600 px wide, leaning and tapering differently, with ledges carrying scrub, olive trees, a ruined wall stub or a milestone. **Nothing floats; no arches in the Near.**
   - Their outlines step in and out with ledges and overhangs, so no stretch of a buttress reads as a column with straight parallel sides. The no-columns rule applies to any 2160 px window.
   - Wide gaps between them: at least 50% fully transparent in every 2160 px window, and no window empty.
   - The nearest and warmest plane, with contrast at about 70% of the gameplay terrain's.

All other rules as in the master prompt: native pixels only, layout first, one drawn mask (Mid: the skyline plus the arch openings), wrap 0 at every row, clean edges over black, white and magenta, mystery checks.

PROOFS
- The climb strip at 0, 15, 30, 50, 75 and 100%, with and without MR02_mid_foreground_rgba.png, with the kept Sky, Far and High.
- A Mid sheet: the aqueduct at 1:1, with every pier's footing marked.
- A Near window sheet: each buttress's support and transparency share.
- Side by side at 30%: 35b and 35d.
- 3x repeats with seam numbers; every join.

DELIVERABLES
The two PNGs, MANIFEST.json, QA_INDEX.html and QA_SHEET.md, sources\, preservation receipt, name-check log, and REVIEW_HANDOFF.md (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review.
