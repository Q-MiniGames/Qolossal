Qolossal: Codex Batch 25a-4 (the terrace overlook: Qori and the bench facing the field, seen three-quarter from behind, painted over a projected 3D blockout and pose guide). Layers only; no Unity import, no code, no music work. Stop once at the end for Claude's review.

Written 11 Oct 2026, after the user rejected 25a-3 and said: "I want him facing the other way, like a 3/4 view but looking to the field, so the bench and Qori have to face the field in a correct way." This replaces the earlier 25a-4 draft (side-on bench with the game-drawn rig), which was not sent. Not sent: the user sends it. Names checked free in Tools\ArtImport\codex_v2_accepted.json on 11 Oct 2026.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a4_Input\
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a4\
Copy your context files into Batch25a4\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHY THE LAST BENCHES FAILED
The geometry, not the painting. In 25a-2 and 25a-3 Qori faced right while the bench ran left to right across the frame, so he sat along it: an L-shaped bench, then a single plank he perched on. A bench and its sitter always face the same way. Here both face the field: the bench's front and Qori's face point into the scene, toward the river; we look at the plain back of the bench and at Qori from behind and a little to the side.

WHAT IS DECIDED (do not change)
1. **The camera.** A camera 5.9 u behind Qori and 0.35 u to his right, at eye height 1.38 u (about his head), looking 7° down, horizon at y 660 as in the 25a-3 background. The bench's front is turned 45° to the right of straight ahead, so the bench runs diagonally: its near end on the right (bigger, lower), its far end on the left (smaller, higher). All numbers are in `Placement.json`; 365 px per unit at Qori.
2. **The bench shape** is the accepted bench, Assets\Art\Codex\Town\Town_Qvale_ListeningBench.png, built as a 3D model from its measurements (at 120 PPU from foot row 204: seat top 0.75 u, seat plank 0.12 u, backrest plank 0.95 to 1.40 u, posts to 1.47 u, length 3.19 u; seat depth 0.5 u) and projected through that camera: `Bench_Blockout_Front.png` (nearer than Qori: the backrest and the two back posts with their root feet) and `Bench_Blockout_Back.png` (beyond him: the seat and the two front legs with their root feet). Paint over these shapes; keep their silhouettes.
3. **Qori's pose** is the game's Sit pose (the rig rendered in Unity: `Reference_Qori_Rig_Sit_SideView.png`, landmarks in `Qori_Sit_Landmarks.json`), turned to face the field and projected through the same camera: `Qori_Guide_Body.png`, `Qori_Guide_Head.png`, `Qori_Guide_EarNear.png`, `Qori_Guide_EarFar.png`. He sits 0.95 u from the near end, hip on the seat. From here the backrest hides most of his body: his head and ears show above it, his cloak and back through the gap between seat and backrest.
4. `Template_Preview.png` shows all of it on the 25a-3 terrace. `Background_25a3_NoBench.png` is that background.

WHAT YOU PAINT
**The bench**, over the blockout: the same weathered pale wood, the same waved top edge along the backrest, the same root-twined posts with splayed root feet, moss and small olive leaves, the same soft dark-brown outline and texture as Town_Qvale_ListeningBench.png. We see the plain back of the backrest (no emblem: it is carved on the front), the seat's top beyond it, the near end, and the far end smaller. Two supports per end (a back post and a front leg), four in all, and no others. Warm light from the upper right; the seat top is the brightest wood, the backrest's back face in soft shade.

**Qori**, over the pose guide, in a **lost profile**: seen from behind and a little to his left, his head turned toward the river. Show the back of his head as the leaf crest (broad crest leaves of the rig's shape, lying back from the crown and flaring past the outline), with only a sliver of his cream cheek on the river (right) side, as the cream area in `Qori_Guide_Head.png` marks: about 5–25% of the head, and at most the edge of his eye. Both long ears rise from the top of his head and sweep back toward the camera and up, their green backs toward us with a thin pale rim; the near ear partly in front of the far one. The leaf mantle at his shoulders, the cloak falling behind him onto the seat. Match the rig and your passed 25a-2 Qori exactly in proportions, colours, leaf shapes and soft olive-brown edges (`Codex_v2_full\Batch25a2\Layers\Retreat_OverlookTerrace_Qori_Head.png` and its ears passed Review 56 as his near-profile; this is the same head turned about 60° further away). Paint his whole body, also where the backrest covers it.

**The terrace** (`Retreat_OverlookTerrace_Terrace.png`), repainted only where needed: move the parapet wall back so the bench stands on paving in front of it. Its base must run at about y 1000 or higher behind the bench (the far end's root feet stand on the paving at y 1060–1080, so the wall must be visibly behind them), lower and smaller with distance, with the same stones, ivy and white flowers. Keep the pot and flowers at the left edge and the bottom 10% plain paving.

LAYERS (each 2560x1440 RGBA at (0,0), straight alpha, no halo; names start `Retreat_OverlookTerrace_`)
| File | Contents |
|---|---|
| `Terrace` | Repainted as above |
| `BenchShadow` | Soft contact shadow on the paving under the root feet and the seat |
| `BenchBack` | The seat and the two front legs (painted fully, also under Qori) |
| `Qori_Body` | Cloak, mantle, tunic, the arm and leg on the seat, painted fully also behind the backrest |
| `Qori_Head` | Head and crest without ears, with the cheek sliver |
| `Qori_Blink` | Only if the eye's edge shows: that edge closed |
| `Qori_EarFar`, `Qori_EarNear` | Each ear, base painted fully |
| `BenchFront` | The backrest and the two back posts with their root feet |

Draw order in the game: background, Terrace, BenchShadow, BenchBack, Qori_Body, Qori_Head, Qori_EarFar, Qori_EarNear, BenchFront, foreground pieces, Weather. Pivots in a `_Registration.json`: the head at the neck, each ear at its base (head ±3°, ears ±8°, no gaps). Keep Sky, Clouds, Far, Mid, Mist, Tree, Lantern, LanternGlow, Foreground_01–04 and Weather byte-identical to Batch25a3.

CHECK BEFORE YOU DELIVER
Run `python check_batch25a4.py <Batch25a4_Input> <your Layers folder>`. It must report **0 failed**. Its 30 tests cover the bench silhouette against the blockout, the seat solid under Qori, all four root feet on the paving, Qori's visible silhouette against the guide, the head's position and size, the lost profile (cream cheek 3–30% of the head, on the river side), the backrest not covering his head, both ears rising and sweeping back with their tips near the guide's, the UI zones clear and no dark halos. The guides themselves pass; 25a-3's layers fail 18. Put the full output in the handoff.

ALSO DELIVER
`Retreat_OverlookTerrace_Composite.png` (full stack), `_Composite_UIZones.png`, `_MotionProof.png`, a 1:1 crop of Qori beside your 25a-2 near-profile Qori, REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches), a name-collision check, and a source archive with your prompts, as in earlier batches. Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested"; original Switch excluded.

Then stop for Claude's review. 25b and 25c stay on hold.
