Qolossal: Codex Batch 25a-6 (a touch-up of the passed 25a-5 terrace overlook: the backrest's waved top, and Qori's cheek). Two layers change; everything else stays byte-identical. Stop once at the end for Claude's review.

Written 11 Oct 2026, at the user's request, after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_58.md`. Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a5_Input\ and your Batch25a5 delivery
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a6\
Copy your context files into Batch25a6\Integrator\Context\, as in earlier batches. Do not write under the live project.

25a-5 passed: the composition, the bench and Qori are approved. Change only these two things.

1. **`Retreat_OverlookTerrace_BenchFront.png`: the backrest's waved top edge.** The accepted bench (Assets\Art\Codex\Town\Town_Qvale_ListeningBench.png) has a gently waved top on its backrest plank: it rises and falls about 16 px over its 432 px width, two soft crests with a dip in the middle. In 25a-5 the plank's top is straight. Repaint only the top of the backrest plank so it follows that same wave along its whole length, in this view's perspective (about 25–35 px of rise and fall across the plank here). Keep the plank's thickness, wood, moss and outline, the posts, the silhouette checks and everything below the top edge as they are.
2. **`Retreat_OverlookTerrace_Qori_Head.png`: the cheek.** The cream sliver on the river side is a thin bright crescent at the head's edge, so it reads as a highlight. Paint a softer, slightly wider curve of his cream cheek turning away from us: the cheek's rounded edge under the crest leaves, gently shaded, about 9–16% of the head (25a-5 has 8%) and at least 26 px across at its widest (25a-5 has 20). Keep the crest, the head's outline, size and position, and the ear bases exactly as they are. At most the very edge of his eye may show; no full eye.

Keep every other layer, the registration and the pivots byte-identical to Batch25a5. If the head's alpha changes, re-run the motion proof (head ±3°, ears ±8°, no gaps).

CHECK BEFORE YOU DELIVER
Both must report **0 failed**:
1. `python check_batch25a5.py <Batch25a5_Input> <Batch25a6 Layers>`: the 30 placement tests, as before.
2. `python check_batch25a6_touchup.py <Batch25a5 Layers> <Batch25a6 Layers>` (in Batch25a5_Input): only BenchFront and Qori_Head changed; the backrest top rises and falls at least 15 px after removing the perspective slope (25a-5 measures 7); the cheek is 9–16% of the head and at least 26 px wide. 25a-5's layers fail its three art tests, as they should.

Put both outputs in the handoff, with a 1:1 crop of the backrest top before and after, a 1:1 crop of Qori's head before and after, and the new composite. REVIEW_HANDOFF.md, MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
