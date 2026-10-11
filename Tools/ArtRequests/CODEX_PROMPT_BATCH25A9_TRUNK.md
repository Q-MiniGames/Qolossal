Qolossal: Codex Batch 25a-9 (a touch-up of the passed 25a-8 terrace overlook: the olive tree's broken trunk, grounding the planter and the pot, and hiding Qori's shins behind the seat). Three layers change; everything else stays byte-identical. Stop once at the end for Claude's review.

Written 11 Oct 2026, after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_61.md`. Not sent: the user sends it. Your 25a-8 feet passed; this is the part of the 25a-8 prompt that was added after you had started.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a8_Input\ (refreshed checker) and your Batch25a8 delivery
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a9\
Copy your context files into Batch25a9\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHAT TO PAINT
The user sent a crop of the olive tree: the old trunk stops at about y 690 and a separate, narrower stump starts below it at about y 700, so the tree reads as broken in two.

Repaint the lower part of **`Retreat_OverlookTerrace_Tree.png`** only: the trunk must be one continuous olive trunk from the branches down into the stone planter. At y 560 to 620 the existing trunk is about 75–90 px wide and leans down from the left edge; carry that same trunk on down, the same width tapering only slowly, the same twisted, fissured olive bark and light, curving naturally into the planter at about y 960. No gap, no step in width, no visible seam where old and new paint meet: blend the join over y 560 to 700. Keep the tree's branches and leaves above y 560 and its pivot byte-identical.

The user then sent a crop of the planter and the pot: they look pasted onto the floor. Repaint only that corner of **`Retreat_OverlookTerrace_Terrace.png`** (x 0 to 330, y 820 to 1140); keep the rest of the Terrace byte-identical:
- **The planter in perspective.** It is drawn straight-on, with a flat, horizontal bottom edge, while the paving recedes in perspective. Its base and its rim are circles on the floor, so from this camera (above, looking down 7°) both are ellipses: the near part of the base curves lower than its sides, and the rim's top surface shows a little more. Its bottom ellipse sits on the paving with no straight edge.
- **Contact shadows.** Neither the planter nor the pot casts any shadow, so both float. Paint a soft contact shadow where each meets the paving (darkest right at the base, fading within about 20 px), and a soft cast shadow toward the lower left, matching the bench's shadow (light from the upper right).
- **The pot stands on the paving**, its base an ellipse too, its shadow touching it; no gap between the pot and the floor.
- **Paving joints stop at their bases.** The grout lines must not run visibly under or through the planter or the pot: they meet the base and continue on the other side, still following the grid.

The user then sent a crop of Qori's legs: in 25a-8 his shins are painted over the seat, from its front edge down across the seat plank, as if they hung in front of it. They don't: his knees bend over the seat's front edge, which is the far edge from this camera, so his shins hang down on the far side, and the seat (nearer to us) hides them until they come out below it. In **`Retreat_OverlookTerrace_Qori_Body.png`**, remove the parts of the shins that lie over the seat (BenchBack) below its front edge (`seatFrontEdgeLine` in `Placement.json`); keep the parts below the seat, the ankle wraps and the feet as you painted them, and his cloak resting on the seat top as it is. The refreshed `Qori_Guide_Body.png` shows the result. Paint the shins' upper ends where they emerge below the seat softly shaded, as if in the seat's shadow.

Keep every other layer, the registration and the pivots byte-identical to Batch25a8 (only Tree, Terrace and Qori_Body change).

CHECK BEFORE YOU DELIVER
Run `python check_batch25a8.py <Batch25a8_Input> <your Layers folder>` (the copy in Batch25a8_Input, refreshed with two trunk tests). It must report **0 failed** of 38: the trunk is opaque on every row from y 560 to 940; its width changes by at most 16 px over any 4 rows; and no bark-coloured Qori pixels lie over the seat below its front edge (at most 60). Your 25a-8 layers fail exactly those three. Put the output in the handoff, with a 1:1 crop of the whole trunk from the branches to the planter, a 1:1 crop of the planter and the pot on the paving, a 1:1 crop of Qori's legs under the seat, all before and after, and the new composite. REVIEW_HANDOFF.md, MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
