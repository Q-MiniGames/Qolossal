Qolossal: Codex Batch 25a-7 (the terrace overlook, zoomed out so the view leads: a smaller bench and Qori, the paving laid square to the wall and the bench, and both ears as full leaves). Same method as 25a-4 to 25a-6: paint over a projected blockout and guides, then pass the checker. Layers only; no Unity import, no code, no music work. Stop once at the end for Claude's review.

Written 11 Oct 2026, from the user's notes on the 25a-6 scene in the game: "I want the main focus to be on the view. (1) The bench is almost half of the screen; make a zoomed-out version, a quarter or a third of the screen. (3) The tiles to be fixed to align with the wall and the bench, and have the bench sit on it properly. (4) The right ear of Qori is very thin." (Their note 2, the song list, is Claude's code and is done.) Not sent: the user sends it. Same names as 25a-6.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a7_Input\ and your Batch25a6 delivery
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a7\
Copy your context files into Batch25a7\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHAT IS DECIDED (do not change)
1. **The camera moves back.** Same lens, pitch (7° down) and horizon (y 660) as 25a-5/25a-6, but 9.6 u behind Qori instead of 6.5, and at eye height 2.3 u instead of 1.7, looking a little more over the wall. So the sky and the distant valley are unchanged, while everything on the terrace gets smaller: the bench is now about 29% of the frame's width (25a-6: about 40%), 203 px per unit at Qori (was 300). All numbers in `Placement.json`; `Template_Preview.png` shows the layout.
2. **The bench and Qori**: the same bench and the same lost-profile Qori as your passed 25a-6, at the new size and the slightly higher angle, over `Bench_Blockout_Back.png`, `Bench_Blockout_Front.png` and `Qori_Guide_*.png` (still 1.35x his game size, head turned 32° toward the river). Use your 25a-6 layers as the paint master: same wood, waved backrest top, root-twined posts, moss; same crest, cheek and mantle.
3. **The wall and the paving are geometry now**, on the bench's own axes:
   - `Wall_Blockout.png`: the parapet runs parallel to the bench, its front face 0.45 u beyond the seat's front edge, 0.8 u high and 0.4 u thick with its top face showing. Lines in `Placement.json` (`parapetBaseLine`, `parapetTopLine`).
   - `Paving_Grid_Guide.png`: square limestone slabs 0.75 u wide. One set of joints runs parallel to the wall and the bench; the other runs square to them, converging toward their vanishing point. **Every joint in your paving follows this grid**: this is what makes the bench sit on the floor. In 25a-6 the slabs ran on their own diagonal, unrelated to the wall.
4. **Both ears are full leaves.** The guides now turn both ears so their green backs face the camera. In 25a-6 the far (right) ear was seen edge-on, a sliver about 10 px wide against the near ear's 29. Paint both as full leaves of the same width, green backs with a thin pale rim, the near one partly in front of the far one.

WHAT YOU PAINT
- **Terrace**: repainted to the new geometry. The paving over the grid (same warm limestone, slight colour variation per slab, soft grout lines, a little wear), the parapet over the wall blockout (same stones, ivy and small white flowers along its foot), and the terracotta pot of white flowers at the left edge, at its new smaller size, standing on the paving against the wall. Keep the bottom 10% and the right quarter plain paving.
- **Tree**: only its lower trunk changes. At the new distance the trunk must reach the ground: extend it down to stand in a low stone planter (same limestone as the wall) on the paving at the left edge, behind the pot. Keep the branches, the leaves and the lantern's branch byte-identical above y 700, and the tree's pivot.
- **BenchShadow, BenchBack, BenchFront**: the bench at the new size. The four root feet stand on the paving at the points in `Placement.json` (`groundUnderBackPosts`, `groundUnderFrontLegs`), with a soft contact shadow under each and under the seat.
- **Qori_Body, Qori_Head, Qori_EarFar, Qori_EarNear**: Qori at the new size, both ears full leaves (above).
- Keep Sky, Clouds, Far, Mid, Mist, Lantern, LanternGlow, Foreground_01–04 and Weather byte-identical to Batch25a6.
- Update the registration: the new head and ear pivots, the tree's (unchanged) pivot, and the Weather layer's actor exclusion rectangle around the new bench and Qori (they sit within x 323–1006, y 638–1166).

CHECK BEFORE YOU DELIVER
Run `python check_batch25a7.py <Batch25a7_Input> <your Layers folder>`. It must report **0 failed**. Its 33 tests are 25a-5's 30 (bench silhouette, seat under Qori, root feet on the paving, Qori's silhouette, head position and size, the cheek on the river side, the ears' tips, UI zones, halos) plus three for the ears: each ear's mean width at least 80% of the guide's, and the two ears within 30% of each other. The guides pass all 33; your 25a-6 layers fail 15, as they should. Also put `Paving_Grid_Guide.png` over your Terrace at 50% in the handoff, so the joints can be seen to follow it, and a 1:1 crop of the bench's feet on the paving.

ALSO DELIVER
`Retreat_OverlookTerrace_Composite.png`, `_Composite_UIZones.png`, `_MotionProof.png`, a 1:1 crop of Qori, REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches) and a name-collision check, as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
