Qolossal: Codex Batch 25a-11 (the terrace overlook: Qori's seated body painted properly for this camera). One layer changes; everything else stays byte-identical. Stop once at the end for Claude's review.

Written 11 Oct 2026, after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_63.md`. Not sent: the user sends it. Your 25a-10 planter and pot passed; this is the body part that was added to the 25a-10 prompt after you had started.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a8_Input\ (refreshed guide and checker) and your Batch25a10 delivery
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a11\
Copy your context files into Batch25a11\Integrator\Context\, as in earlier batches. Do not write under the live project.

The user, after playing it: "the sitting doesn't feel right, and I believe the old feet are still showing." Both are true, and the cause is the body layer itself: since 25a-7, `Qori_Body` has been your 25a-6 body resized, and that was a side-view seated body (knees pointing right, a folded leg and a wrapped root foot at seat level). From this camera, behind him, that side-view knee and foot show through the gap between the backrest and the seat, beside his cloak; the hanging feet added in 25a-8 are a second pair. So he reads as a side-view figure pasted behind a rear-view bench.

Repaint **`Retreat_OverlookTerrace_Qori_Body.png`** from scratch for this view, over the refreshed `Qori_Guide_Body.png` (`Body_Guide_In_Scene.png` shows it in place):
- We see his back: the leaf mantle and its clasp edge across his shoulders above the backrest, his leaf cloak falling down his back and onto the seat, its leaf tips resting on the seat top and spilling a little over its far edge. The cream tunic shows only as a sliver at his side, if at all.
- He sits squarely on the seat, his weight on it: the cloak is pressed and spreads where it meets the seat top, with a soft shadow on the seat under it. No gap between him and the seat.
- His thighs lie forward on the seat, hidden by his back and cloak. **No knee, leg or foot shows at seat level**, between the backrest and the seat.
- His shins hang straight down beyond the seat's far (front) edge: the seat hides their tops, and below the seat we see the lower shins, the ankle wraps and the root feet, heels toward us, as in your 25a-9 (keep those as they are).
- No arms or hands show, unless as a small hand resting on the seat beside him, in leaf-cloak shadow.
Match the rig in proportions, colours and soft olive-brown edges; your 25a-9 head and ears are unchanged and must sit on the new body exactly (keep the head pivot and the neck painted fully under the head).

Keep every other layer, the registration and the pivots byte-identical to Batch25a10 (only Qori_Body changes).

CHECK BEFORE YOU DELIVER
Run `python check_batch25a8.py <Batch25a8_Input> <your Layers folder>` (refreshed): **0 failed** of 39. The new test allows no bark-coloured body pixels at seat level just above the seat's front edge (at most 40); your 25a-10 body has 951 there and fails it. Put the output in the handoff, with a 1:1 crop of Qori on the bench, before and after, the new body layer alone on a flat background, and the new composite. REVIEW_HANDOFF.md, MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
