Qolossal: Codex Batch 26j (the listening retreat: the bench and Qori at 45 degrees, with his eye and blink, in all seven places). Stop once at the end for Claude's review.

Written 12 Oct 2026. The user compared your 26t meadow test (A, 45 degrees right; B, 25 degrees left) with the current 18 degrees and chose **A** for every place. It also settles the eye: at 18 degrees a full eye on the edge of the crest looked stuck on (your 26i eye was well painted, but there was no face around it); at 45 degrees his cheek and the side of his face show, so the eye sits in a face. (Batch 27 is another thread's; this is 26j.) Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26j_Input\Open\ and \Terrace\, your Batch26h\<Scene>\ deliveries (the current scenes), your Batch26t\CradleMeadow45\ (the approved look at 45 degrees) and Batch26i\OverlookTerrace\ (the eye's painting)
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26j\<Scene>\ for all seven, each a full copy of its Batch26h scene with the changes below, **same prefixes and file names as now** (Retreat_<Scene>_...)
Copy your context files into Batch26j\Integrator\Context\. Do not write under the live project.

THE TWO LAYOUTS
| Guide folder | Places | Why |
|---|---|---|
| `Open` | CradleMeadow, TerracesRain, CausewayDusk, RibwoodGlade, HeightsClouds | Exactly your 26t A layout: the bench's front turned 45 degrees right, at the same spot |
| `Terrace` | OverlookTerrace, OverlookNight | On the terrace the 45-degree bench would reach into the parapet, so here it stands 0.85 units nearer the camera (a little larger and lower in the frame), clear of the wall; the wall, tiles, tree, planter and pot stay as they are. `Template_Preview.png` shows it on the terrace |

Each folder has the projected bench blockout, Qori's pose guides, `Placement.json` (root-foot points, seat line, feet, head, ear tips) and `check_batch26t.py`.

WHAT YOU PAINT
1. **The bench and Qori, once, then per place.** Start from your 26t CradleMeadow45 bench and Qori (approved look). For `Terrace`, re-register them to that folder's blockout and guides (same shapes, larger). Add, in both layouts:
   - **the neck** from 26h (short bark neck between mantle and crest);
   - **the eye**, painted like your 26i eye (a dark half-oval with a warm brown rim and one small white highlight, smooth edges), set inside the cream cheek at the rig's eye height, sized to the larger cheek at this angle (about 18-24 px wide, 28-34 px tall), never on the outline;
   - **`Qori_Blink`**: the same eye closed, a soft curved lid line on the cream; registered right after Qori_Head with `"parent": "Qori_Head"`.
   Relight the colours to each place's light as in Batch 26; HeightsClouds uses the OverlookTerrace colours. Every place in a layout shares the same alpha for these layers.
2. **The floor under them** (Ground, or Terrace on the overlook): paint out the old bench's shadow and any trace of it, and paint the new contact shadows under the four root feet and the seat, only within x 150-1300, y 820-1296. On the terrace, the tiles keep their grid; the bench simply stands on them at an angle, as a moved bench would.
3. Registration: new head and ear pivots and the Weather exclusion rectangle around the new bench and Qori; everything else as in 26h.

Keep every other layer byte-identical to Batch26h.

CHECK BEFORE YOU DELIVER (each place)
1. `python check_batch26t.py <its guide folder> <its Layers folder> Retreat_<Scene>_`: **0 failed** (bench against its blockout, seat under Qori, all four feet on the floor, Qori's silhouette, head, cheek on the guide's side, ears, shins hidden by the seat, no leftover knee, UI zones clear, no halos).
2. `python check_batch26j.py <Batch26h\<Scene>> <new scene folder>` (in each guide folder): **0 failed** (only the bench, Qori, blink and floor change; the floor only around the bench).

Put both outputs per place in REVIEW_HANDOFF.md, with each new composite, a 1:1 crop of Qori (eye open and blinking) and a 1:1 crop of the four root feet on the floor. MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.
