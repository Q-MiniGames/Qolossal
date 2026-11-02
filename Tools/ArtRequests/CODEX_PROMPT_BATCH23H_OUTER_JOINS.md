Qolossal: Codex Batch 23h (the tall-wall top pieces for the six natural step sets: make their bottom meet the wall courses). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch23h\
Copy your context files into Batch23h\Integrator\Context\.

WHY
The Causeway set (23b) is in the game, and the user play-tested it. On every wall taller than 2.05 u there is a small ledge where the Outer piece meets the first wall course (example: Tools\ArtRequests\Batch23\ledge_MR02_x270_1080.png).

The cause is the art. Measured from the riser line (negative means overhanging the open side):
- **Outer pieces:** over rows 410 to 448 (their last 40 rows before the joint at row 448), every `*_Step_Outer_L` overhangs by 47 to 61 px.
- **Wall courses:** `*_Step_Wall_A/B/C` start at -1 to +15 px.

The Cradle (23a) and Qvale sets don't have the problem: there the Outer's bottom and the walls' row 0 match within a few px.

Claude tried to hide it in the game by fading the Outer's bottom. That exposed the fill's straight edge along the riser line (the Outer's bulge had been covering it), so the user rejected it. This has to be fixed in the art.

WHAT TO MAKE (24 PNGs, `_v2` of the accepted-for-review 23b to 23g Outers; names checked free on 10 Oct)

For each family prefix:
- `Terrain/Body/Causeway/Causeway_Step_`
- `Terrain/Body/Terraces/Terraces_Step_`
- `Terrain/A2_Step_`
- `Terrain/A3_Step_`
- `Terrain/A6_Step_`
- `Terrain/A4_Step_`

repaint:
- `Outer_L_v2`, `Outer_R_v2` (from Outer_L/R)
- `Outer_SlopeTop_L_v2`, `Outer_SlopeTop_R_v2` (from Outer_SlopeTop_L/R)

Rules:
1. **Change only the bottom of the face:** rows 360 to 512 of Outer, and the matching 152 rows above and below its joint (row 673) on Outer_SlopeTop. Everything above stays byte-identical: the corner, the upper run, the strip pixels and the grass.
2. **Match the walls exactly at the joint:**
   - From about 60 rows above the joint down, the open-side outline narrows smoothly back to the riser line. At the joint row it sits within 4 px of that family's Wall_A/B/C row 0 outline (riser column 128 on the walls equals riser column 256 on the Outer).
   - The stones on the joint row are the walls' row 0 pixels (the three walls share rows 0 and 239), so any wall course stacks under the Outer with no seam.
   - No ledge, lip, shelf or horizontal stone course at the joint.
3. **Keep the open side covered all the way.** The rock must stay opaque from the riser line out to its outline on every row. The game's fill has a straight edge on the riser line, so no gap or fade may let it show.
4. **Keep the rest unchanged:** canvas, registration, pivots, PPU, the joint at row 448 (673 on SlopeTop), the 64-row fade below it, exact L/R mirrors, the region's material and detail level (Review 49/50 checks), the mystery rule, native or uniform-downscale sources only, the name check, and the preservation receipt (re-read the accepted manifest).

PROOFS (per family)
- Outer over Wall_A, then B, then C, at 1:1 and 4x, with the riser line marked (proof copy only).
- The region's three tallest risers, assembled as `MraTerrain.StepParts` does today (walls from the Outer's joint row 448 down, Inner at the foot), 1920x1080. Show them before (the 23b-g Outer) and after (v2) side by side.
- An outline plot per piece: the open-side outline column per row for rows 360 to 512, overlaid on the walls' row 0 value.
- A difference mask showing only the allowed rows changed.

DELIVERABLES
- The 24 PNGs with REGISTRATION.json, `Batch23h\MANIFEST.json`, the QA sheet, the name-check log and the preservation receipt.
- `REVIEW_HANDOFF.md`, with the platform section (all device tests "not tested").

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
