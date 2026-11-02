Qolossal: Codex Batch 37e, four small repairs to the Ribwood Near. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch37e\
Preserve Batch37, 37b, 37c and 37d byte-identical; read them only.

READ FIRST: `Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_96.md`.

MAKE: BG_Ribwood_Near_4K.png, starting from Batch37d's Near (sha256 4153d1336a2687afda5b574e1e0dfcf60ab088e3cc79f084a0b803483c7d58e7). Repaint the four straight leaf cuts that Batch 37d disclosed, natively at the same scale:
1. x 1702-1816, y 5344
2. x 2339-2404, y 3904
3. x 1820-1874, y 3184
4. x 2503-2551, y 6784

For each one, paint the canopy edge through the cut as a natural, irregular leaf silhouette and redraw the mask to follow it. Don't extend any repair more than 120 px beyond its listed bounds. Keep every other pixel unchanged.

GATES
- A pixel preservation map: no changed pixel outside the four repair boxes (plus their 120 px margins).
- The straight-cut scan: none left inside the four areas.
- The Near gate: at least 50% clear in every 3840x2160 window, wrap 0 at every row, nothing floating (every opaque island reaches the bottom row, wrap-connected).
- Before/after at 100% for each repair; the whole Near at 10% and blurred.

DELIVERABLES
The Near PNG, MANIFEST.json, QA_INDEX.html, QA_SHEET.md, sources\, preservation receipt, name-check log, REVIEW_HANDOFF.md (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review.
