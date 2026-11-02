Qolossal: Codex Batch 26t (a test in the Cradle meadow: the bench and Qori at two new angles). Two variants of one scene. Stop once at the end for Claude's and the user's review.

Written 12 Oct 2026. The user asked to try a more three-quarter view by turning the bench, and after seeing Claude's block-outs (`Tools/ArtRequests/Batch26/Tilt_Options.jpg`) said "try both". Today the bench's front is turned 18° to the right of straight ahead. Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26t_Input\Angle45\ and \AngleLeft25\, and your passed Batch26g\CradleMeadow scene
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26t\CradleMeadow45\ and \CradleMeadowLeft25\, each a full scene in the Batch 26 format
Copy your context files into Batch26t\Integrator\Context\. Do not write under the live project.

THE TWO ANGLES (the camera, horizon and scale stay as before; only the bench and Qori turn)
| Variant | Prefix | Bench | What we see of Qori |
|---|---|---|---|
| A | `Retreat_CradleMeadow45_` | Front turned **45°** right: its right end comes toward us, and it slants back to the left | He turns further toward the stream: more of his cream cheek and the outer part of his big dark eye; a true three-quarter rear view |
| B | `Retreat_CradleMeadowLeft25_` | Front turned **25° left**: it slants the other way | He faces away up the meadow: the back of his head and crest, ears sweeping back, no face |

Each input folder has the projected `Bench_Blockout_Back.png`, `Bench_Blockout_Front.png`, the `Qori_Guide_*.png` pose guides, `Placement.json` and `Template_Preview.png`. (The previews show the terrace and its wall behind; ignore those: this is the meadow.)

WHAT YOU PAINT (each variant)
- **The bench**, over its blockout: the same accepted bench and paint as before (weathered wood, waved backrest, root-twined posts and feet, moss), seen at the new angle. Two supports per end. Its four root feet stand on the meadow at the points in `Placement.json`, with a soft contact shadow, and no slab.
- **Qori**, over the guides: the same Qori as the approved scenes (crest, ears with green backs and pale rims, leaf mantle and cloak, thin bark shins and root feet hanging below the seat), seen at the new angle. Paint him a **short bark neck** between the mantle and the crest, so the head does not float. In variant A, show the outer edge of his eye on the cheek as the guide's cream area allows (no more than about half the eye), and add a **`Qori_Blink`** layer (the same eye closed, cream lid; parent `Qori_Head`). In variant B there is no face and no blink layer.
- **The ground**: repaint the meadow only where the bench and its shadow were and now are, so no trace of the old bench or its shadow remains. Everything else in the meadow stays byte-identical to Batch26g\CradleMeadow.
- Registration, pivots (head at its neck, ears at their bases), draw order and the weather exclusion around the bench and Qori as in Batch 26; update them to the new positions.

CHECK BEFORE YOU DELIVER (each variant)
`python check_batch26t.py <its Batch26t_Input folder> <its Layers folder> <its prefix>`: **0 failed**. It tests the bench against its blockout, a solid seat under Qori, all four root feet on the ground, Qori's silhouette, head position and size, the face as the guide shows it (variant A: a cream cheek on the stream side; variant B: none), both ears as full leaves where the guide puts them, the shins hidden by the seat with feet below, no leftover knee at seat level, the UI zones clear and no halos. The guides pass all of it; your current meadow layers fail it, as they should. Put the output in REVIEW_HANDOFF.md with each composite and a 1:1 crop of Qori.

MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop: the user picks between A, B and the current angle.
