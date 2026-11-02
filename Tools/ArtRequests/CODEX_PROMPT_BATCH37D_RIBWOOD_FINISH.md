Qolossal: Codex Batch 37d. Finish the Ribwood (MR05): repair four local Near defects, then paint the Mid, High and Sky. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch37d\
**Preserve Batch37, 37b and 37c byte-identical.** Read only.

READ FIRST
1. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_94.md: **Claude passes your 37c Near design.** Only the four listed defects need repair; don't redesign the trees.
2. Tools\ArtRequests\CODEX_PROMPT_BATCH37C_RIBWOOD_4K.md: the Ribwood brief for Mid, High and Sky. It still applies in full.
3. Your Batch37c package: start from its Near master, sources and masks.

STEP 1: repair the Near (BG_Ribwood_Near_4K, 3840x8192). Native repaints at the same scale only; keep every other pixel of the 37c master.
1. The leaf cut at y ≈ 4624 (x 1380-2300): repaint the crown across the straight horizontal edge.
2. The squared bark joins near y ≈ 5040 (x 540-1430): repaint across the rectangular seams on the trunks and branch.
3. The support edge at x 3120-3710, y 6000-7030: redraw the silhouette along the painted bark (not along an authored line), and repaint the edge's shading.
4. The root floor at the canvas's bottom-left (visible at 0% climb): replace the straight vertical edge and flat horizontal strip with roots and boulders ending in a natural silhouette.
- Then re-run the Near gate:
  - every 2160 px window at least 50% fully transparent and never empty;
  - wrap 0 at every row;
  - partial alpha only within 2 px of edges;
  - no unsupported pixels;
  - mystery at full size, 10% and blurred.
- Show before and after for each repair at 1:1, and a scan for any other straight-line cuts in the mask.

STEP 2: paint BG_Ribwood_Mid_4K, BG_Ribwood_High_4K and BG_Ribwood_Sky_4K exactly as the 37c brief says. Carry the Batch 37 Far byte-identical (sha256 b1a17de96e772a4e4670e5aab65eb169a18f7d097471b5e835c72eb2326dd037).

STEP 3: the full-stack proofs. The climb strip at 0, 15, 30, 50, 75 and 100% of the 180 u climb, with and without MR05_mid_foreground_rgba.png and MR05_low_foreground_rgba.png. Also the High/Far mist match, 3x repeats with seam numbers, alpha over black, white and magenta, and every join.

DELIVERABLES
The five PNGs (the Far carried), MANIFEST.json, QA_INDEX.html and QA_SHEET.md, sources\, a preservation receipt (Batch37, 37b and 37c unchanged), the name-check log, and REVIEW_HANDOFF.md (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review. If a plane fails after a fair second attempt, stop and deliver what you have, with the numbers.
