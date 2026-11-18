WITHDRAWN, 3 November 2026: DON'T SEND. The user rejected the Heights art outright, so it isn't being repaired.

REGION: MR06 (Batch 38, revision 01)

Qolossal: Codex, the Heights background you installed passes the numbers but doesn't work as a picture in the game. Repair it end to end, under the same rules as `Tools\ArtRequests\CODEX_PROMPT_BATCH38_HEIGHTS_4K.md`, which still applies in full. Don't start Batch 39. Work in `C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch38_R01\`. Keep the old delivery as it is. Same safety checks first: no Unity open, note `git status`, no commit.

Your numbers were reproduced and are fine: `ClimbCapture` FAULTS 0, vertical speeds 2 / 2 / 3.5 / 16%, wrap, coverage. Keep them all. The problems are what the numbers can't see. Look at `QA\unity_motion_strip_12.jpg` and `gameplay_beats.jpg`.

## What to fix

1. **Near: the diagonal bands float (must fix).** On screen it is a huge flat slab crossing the sky, with sky underneath it, and it ends in a cut point and sharp notches. Your check proved each shape reaches the bottom of the canvas, but the player sees one frame at a time, and in that frame it hangs in the air. It also reads as a giant limb crossing the frame, which breaks the mystery rule. Your own doubt about the "ribbon" was right.
   - The user's choice of continuous, grounded slopes stands. Make them slopes that visibly stand on ground: rock masses wider below than above, whose lower part leaves through the bottom of the frame at every height.
   - **New gate, on the master:** in any column, rock with sky directly under it may be at most 200 px tall. Report the worst column for every 2160 px window, stepped by 540 px. Small ledges and overhanging pines are fine; a slab isn't.
   - No cut ends: every feature ends in a natural summit, broken rock or a tree, not a straight cut, sharp polygon corner or notch.
   - Keep the rest: 16% speed, at least 50% clear in every window, something passing until at least 75% of the climb.

2. **Near scale.** The pines are tiny on a huge blank surface, so the plane looks neither near nor far. Give it the scale and surface detail of a near plane, with visible strata, cracks, scrub, and pines large enough to read.

3. **Keep the upper frame readable.** Flying enemies must read against it. Where a Near mass crosses the top 40% of the frame, it should be a thin summit or quiet rock, not a wide bright band. Near is currently brighter than the terrain's mass behind Qori at the start; hold it back so Qori and the ledges come first.

4. **Mid: repaint the surface.** It has soft, blotchy patches like camouflage or a smeared blend, and no real rock structure. It must read as a low, distant ridge, painted, with one believable light direction. Its skyline and 40% placement are right: keep them.

5. **Far: repaint the peaks.** They look faceted, like low-polygon models with a blur. The haze and the layout work. Bring them up to the painted quality of `Terraces4K` and `Ribwood4K` at the same distance.

6. **The whole stack against the terrain.** The rest of the game's backgrounds are rich and painterly, and the terrain here is warm and fully painted. Your stack looks pale, smooth and from another game. Put them side by side with `Ribwood4K` and the terrain. Keep the sky's cool, thin, high-altitude air, which is this region's own, but warm the light on the rock enough that terrain and background share one light. Keep the sky's numbers above 12 from the other skies.

## Finish

- Install, then run the swap, `ClimbCapture` for all regions, the beats and a 12-frame motion strip, exactly as in section 6 of the main prompt. Other regions' lines must stay as they are now.
- Look at each strip frame as if it were the only one. In each, ask: does anything float, end in a cut, or look like a body part? Does Qori read first? Does it look like the same game as the Ribwood? Repair and repeat until the answer is right.
- HANDOFF.md as before, with before and after strips side by side, the new gate's numbers, and your honest doubts. Mark device tests "not tested", and make no claim of feel.
