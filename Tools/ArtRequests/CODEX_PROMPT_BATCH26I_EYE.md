Qolossal: Codex Batch 26i (Qori's eye in the listening retreat, repainted). Seven scenes, the same change in each: Qori_Head and Qori_Blink only. Stop once at the end for Claude's review.

Written 12 Oct 2026. Your 26h neck passed and is in the game. The 26h eye did not: the user said "the eye is odd". It is a thin, jagged dark slit right at the cheek's outline, about 61 px, so it reads as a crack in his face rather than an eye. Until it is repainted the game uses the heads without an eye. Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26_Input\ (refreshed checker; `Reference\Eye_Rig_vs_26h.png`) and your Batch26h\<Scene>\ deliveries
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26i\<Scene>\ for all seven scenes (OverlookTerrace, OverlookNight, CradleMeadow, TerracesRain, CausewayDusk, RibwoodGlade, HeightsClouds), each a full copy of its Batch26h scene with only Qori_Head and Qori_Blink changed
Copy your context files into Batch26i\Integrator\Context\. Do not write under the live project.

THE EYE (paint it once on OverlookTerrace, then apply the same shapes to every scene with its relight; HeightsClouds uses the OverlookTerrace colours)
Look at `Reference\Eye_Rig_vs_26h.png`. On the rig, Qori's eyes are big, round and dark: a deep brown-black oval with a warm brown rim and a white highlight. In the side view his far eye shows as a half-oval at the edge of his face. That half-oval is what we should see here, from behind and to the side:
- A **dark half-oval** on the cream cheek, its flat side toward the back of his head, its round side along the face's edge, set just inside the outline (a thin line of cream between the eye and the outline), not cut by it.
- About **14–18 px wide and 26–32 px tall** at this scale (the 26h slit is about 6 by 22), smooth antialiased edges, no jagged pixels.
- A warm brown rim, a deep brown-black centre, and **one small white highlight** near its top, as on the rig.
- Sitting at the height of the rig's eye: in the upper half of the cream cheek.
The head's outline, crest and ear bases stay as they are.

**`Qori_Blink`**: the same eye closed: cream cheek with a soft curved lid line where the eye was (a gentle downward arc, as the rig's blink head shows), covering the whole eye. Same registration as 26h (right after Qori_Head, parent Qori_Head).

Keep every other layer, the registration and the pivots byte-identical to Batch26h.

CHECK BEFORE YOU DELIVER (each scene)
1. `python check_batch26h.py <passed scene folder> <new scene folder> 200`, with the same passed folders as 26h (Batch25a11, Batch26f, Batch26g\<Scene>): **0 failed**. With 200, the blink must cover at least 200 px of eye on the cheek (26h covered 61), at most 700, with a dark eye under it and a cream lid on it.
2. `python check_batch26.py <Batch26_Input> <new scene folder> <Batch26i\OverlookTerrace>`: **0 failed**.

Put both outputs in REVIEW_HANDOFF.md with, for each scene, a 4x nearest-neighbour crop of the head (eye open and blinking) and the new composite. MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
