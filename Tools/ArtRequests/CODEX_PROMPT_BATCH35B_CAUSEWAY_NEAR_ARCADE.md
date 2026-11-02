Qolossal: Codex Batch 35b. Redo ONLY BG_Causeway_Near_4K for MR02, the Long Causeway. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch35b\
Keep your Batch35 Sky, Far, High and Mid byte-identical. Use them in every proof; don't redeliver or change them.

WHY
The Batch 35 Near is a row of freestanding masonry pillars. The Terraces and the Cradle already use tall spires, so every region was starting to look the same. The Causeway needs its own shapes. The rules in Tools\ArtRequests\CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md, which now lists Near shapes per region and a no-columns rule, apply to this redo.

READ FIRST
1. Tools\ArtRequests\CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md: section 2 (the Near row), section 3 (MR02's row), sections 4-6.
2. Your Batch35 package: reuse its pipeline, layout, masks and proofs.

MAKE: BG_Causeway_Near_4K, 3840x7528, depth 5, real rate, starting with its bottom edge at the frame's bottom (as Batch 35)
- **An arcade, not pillars.** Runs of two to four connected aqueduct arches, side-on, stepping up the slope at different heights through the canvas. Every pier carries an arch or the stump of a broken one.
- Between the runs: a broken span with a fallen arch, the old road on its retaining walls, stone stairs, a milestone, a few olive trees and climbing plants.
- Horizontal and diagonal masonry leads. A feature with nearly parallel sides more than 4 times taller than wide fails.
- Vary the arches' widths, heights and states of ruin. No two runs alike; no even spacing. No long straight limb-like ridge.
- **At least 50% of every 3840x2160 window, at any height, fully transparent (alpha 0)**, and no window empty. The sky and the farther planes show through the arches as well as between the runs.
- Warmer and sharper than the Mid, with contrast at about 70% of the gameplay terrain's. Not washed out: hold the value range of the Batch 34 Cradle Near.
- The method, wrap, mask, mystery and native-pixel rules are as before. No upscaling.

PROOFS
- The climb strip at 0, 15, 30, 50, 75 and 100%, with and without MR02_mid_foreground_rgba.png, with the kept planes.
- The Near window sheet (2160 px windows every 540 px) with transparency share and mystery views.
- 3x repeat with seam numbers; alpha over black, white and magenta; every join.
- Side by side at 30%: the Batch 35 Near and the 35b Near.

DELIVERABLES
The PNG, MANIFEST.json, QA_INDEX.html and QA_SHEET.md, sources\, preservation receipt, name-check log, and REVIEW_HANDOFF.md (with "not tested" for every device). Re-check the next free CLAUDE_REVIEW number; don't write a review.
