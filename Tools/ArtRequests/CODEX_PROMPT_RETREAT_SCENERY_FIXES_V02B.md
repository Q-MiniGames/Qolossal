# Codex prompt: listening-bench scenery fixes, Retreat v02b

Written 18 Oct 2026, from Claude Review 81. Not sent: the user sends it. Claude reviews the delivery as the next free CLAUDE_REVIEW number (re-check just before writing).

---

Qolossal: Retreat v02b, a narrow redo of three things from Retreat v02. Everything else from v02 is accepted for use and must not change. Stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Read only, do not modify: Art\Codex_v2_full\Retreat4K_NewScene_v01, Art\Codex_v2_full\Retreat_OtherScenes_Repaint_v01, Art\Codex_v2_full\Retreat_Fixes_v02
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Retreat_Fixes_v02b\<SceneName>\

READ FIRST
- Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_81.md
- Its images: CLAUDE_REVIEW_81_Skylines.jpg (v01 left, v02 right) and CLAUDE_REVIEW_81_OverlookTerrace_Ridge_Clouds.jpg

WHAT WENT WRONG IN v02
- The side margins replaced the painted skylines with smooth traced curves, flattening peaks, cutting crags and treetops, and smearing pixels vertically above the old edge. This hit Overlook Terrace Mountains and Valley and the MiddleDistance of Causeway Dusk, Cradle Meadow, Overlook Night, Ribwood Glade and Terraces Rain.
- The repaired clouds gained dark brown fringes, harder edges, stronger orange and a blue patch inside the big cloud.
- The rain tile's streaks are long dark hairlines that read as wires.

1. SIDE MARGINS (the seven layers above)
   - Start from the v01 PNG of each layer. Make a 3968-wide canvas at offset x -64.
   - Columns 64-3903 must equal the v01 layer **byte for byte, RGB and alpha**. No re-matting, no skyline tracing, no colour-bleed cleanup, no stretching.
   - Paint only the 64 columns at each side, continuing the v01 silhouette, colour, haze and texture across the join.
   - The new margin's alpha comes from its own drawn matte and must meet v01's alpha at the join without a step.
   - Proof: compare v02b columns 64-3903 with v01 and report the max difference, which must be 0. Include 100% crops of both joins on each layer, and an assembly at the drift extremes (±12 px for the far plane, ±21 px for the middle plane) showing no edge.
2. OVERLOOK TERRACE CLOUDS
   - Start from v01 `02_Clouds.png`. Leave it unchanged everywhere except a band of at most 96 px at each end, where you fade or cross-blend so that the left and right columns wrap exactly (period 3840).
   - No RGB repair. No new fringes.
   - Proof: the max difference to v01 outside the end bands (must be 0), the wrap difference (must be 0), and a 100% three-tile strip.
3. TERRACES RAIN TILE
   - Repaint `07_Rain_Tile` (3840 x 1080, vertical period 1080) with short, soft, light streaks like the v01 still: roughly 30-80 px long, never darker than the sky behind them, and no continuous lines.
   - Keep the v02 lean and an average coverage close to v01's.
   - Proof: the loop difference (must be 0), a 100% crop over the sky next to the same v01 crop, and a three-tile strip.

DELIVERABLES
- Only the changed PNGs in Layers\.
- An updated Registration_v02b.json per affected scene. It references the accepted v02 layers (shadows, trees, night props, glows) and v01 layers by path and SHA-256.
- Updated .ora stacks.
- A QA folder with the proofs above, plus README.txt and a zip archive. Test the archive.
- No Unity writes. Do not start Batch 7, and do not touch the music held for audition. No platform-readiness claims. Nothing here is native 4K.
