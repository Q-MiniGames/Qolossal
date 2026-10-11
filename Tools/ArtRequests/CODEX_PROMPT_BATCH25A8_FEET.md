Qolossal: Codex Batch 25a-8 (a touch-up of the passed 25a-7 terrace overlook: Qori's shins and feet, and the olive tree's broken trunk). Two layers change; everything else stays byte-identical. Stop once at the end for Claude's review.

Written 11 Oct 2026, after the user asked "Qori's feet are not showing, is that on purpose?". It was not: Claude's 25a-7 body guide had only his thighs, so his lower legs were never painted. From this camera they hang over the seat's front edge and show below it, in front of the wall. The user then sent a crop of the olive tree: in 25a-7 the old trunk stops at about y 690 and a separate, narrower stump starts below it at about y 700, so the tree reads as broken in two. Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a8_Input\ and your Batch25a7 delivery
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a8\
Copy your context files into Batch25a8\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHAT TO PAINT
Repaint **`Retreat_OverlookTerrace_Qori_Body.png`**: your 25a-7 body, plus both lower legs and feet over the new shapes in `Qori_Guide_Body.png` (the layout is 25a-7's, unchanged; `Template_Preview.png` shows it):
- Two thin bark shins hanging straight down from his knees over the seat's front edge, side by side, slightly apart, as on the rig (`Reference_Qori_Rig_Sit_SideView.png`): the same bark brown, grain and soft olive-brown outline as his arms.
- The ankle wraps and the small root feet at their ends, seen from behind and above: heels toward the camera, the splayed root toes pointing away toward the wall. Feet at `qoriFeet` in `Placement.json`, about y 1040.
- The tips of his leaf cloak may fall a little over the seat's front edge beside his legs.
- Nothing else in the body changes; his head, ears and the bench are untouched.

Repaint the lower part of **`Retreat_OverlookTerrace_Tree.png`**: the trunk must be one continuous olive trunk from the branches down into the stone planter. At y 560 to 620 the existing trunk is about 75–90 px wide and leans down from the left edge; carry that same trunk on down, the same width tapering only slowly, the same twisted, fissured olive bark and light, curving naturally into the planter at about y 960. No gap, no step in width, no visible seam where old and new paint meet: blend the join over y 560 to 700. Keep the tree's branches and leaves above y 560 and the pivot byte-identical, and the planter, pot and paving in the Terrace layer as they are.

Keep every other layer, the registration and the pivots byte-identical to Batch25a7.

CHECK BEFORE YOU DELIVER
Run `python check_batch25a8.py <Batch25a8_Input> <your Layers folder>`. It must report **0 failed**: 25a-7's 33 tests plus two for the feet (each foot painted, at least half of an 18 px patch at its point, and not covered by the bench) and two for the trunk (opaque on every row from y 560 to 940, and its width changing by at most 16 px over any 4 rows). Your 25a-7 layers fail exactly these four. Put the output in the handoff, with a 1:1 crop of Qori's legs and feet under the seat, a 1:1 crop of the whole trunk from the branches to the planter, and the new composite. REVIEW_HANDOFF.md, MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
