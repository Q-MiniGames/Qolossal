Qolossal: Codex Batch 26k (the listening retreat at 45 degrees: Qori's face, repainted). Seven places, Qori_Head and Qori_Blink only. Stop once at the end for Claude's review.

Written 12 Oct 2026, after Claude's review of Batch 26j. Your 26j passes every check: the 45-degree bench and Qori, the neck, the floors and all seven places are right. One thing is not: **the face reads as an eyeball.** The visible cream patch is filled almost entirely by the eye, painted with a white ring and a big brown iris, so it looks like a googly eye stuck on the crest (`Batch26j_Input\Face_Rig_vs_26j.png`). Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: your Batch26j\<Scene>\ deliveries; `Batch26j_Input\Open\` and `\Terrace\` (refreshed with `check_batch26k.py`); `Batch25a8_Input\Reference_Qori_Rig_Sit_SideView.png`
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26k\<Scene>\ for all seven, each a full copy of its Batch26j scene with only Qori_Head and Qori_Blink changed
Copy your context files into Batch26k\Integrator\Context\. Do not write under the live project.

THE FACE (paint it once, then apply it to each place with its relight, as in 26j)
On the rig, Qori's face is a smooth cream face, and each eye is a **dark brown-black oval** with a thin warm-brown rim and one small white highlight: **no white around it**, and about a quarter of the head's height. Seen from behind at 45 degrees we see the side of that face:
- A **cream face** curving round from under the crest, clearly larger than the eye: cream above, below and in front of the eye.
- **One eye**, the near side of his face: a dark oval about 0.2-0.28 of the head's height (about 20-26 px here), slightly foreshortened, set in the cream with cream on every side. Thin brown rim, one small highlight. No white sclera, no brown iris in a white ring.
- Optionally the tip of his small nose at the face's front edge, as on the rig.
- The crest, the head's outline and the ear bases stay as in 26j.

**`Qori_Blink`**: covering exactly the eye: cream lid with a soft curved lid line. Same registration as 26j.

Keep every other layer and the registration byte-identical to Batch26j.

CHECK BEFORE YOU DELIVER (each place)
1. `python check_batch26k.py <Batch26j\<Scene>> <Batch26k\<Scene>>` (in either Batch26j_Input folder): **0 failed**. The eye (where the blink covers it) is 0.18-0.30 of the head's height (26j's is 0.35, with its white ring painted as cream), the cream face is at least 2.5 times the eye's area, the eye is mostly dark, there is cream on every side of it, and the blink is a cream lid. Your 26j heads fail the size and face tests.
2. `python check_batch26t.py <its guide folder> <its Layers folder> Retreat_<Scene>_`: still **0 failed**.

Put both outputs in REVIEW_HANDOFF.md with, per place, a 4x crop of the head (open and blinking) beside the rig's head. MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
