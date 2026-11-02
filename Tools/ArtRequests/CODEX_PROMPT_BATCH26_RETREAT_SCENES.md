Qolossal: Codex Batch 26 (more listening-retreat scenes, in the direction the user approved: Qori on the same bench, seen from behind, looking out over a view). Six scenes, 26a to 26f. Do 26a first and stop for Claude's review; do 26b to 26f only after 26a passes, then stop once more at the end. Layers only; no Unity import, no code, no music work.

Written 11 Oct 2026, after the terrace overlook was finished (Batches 25a-6 to 25a-11, Claude reviews 59 to 64) and committed. The user: "follow the same direction with different scenes/views." These replace the on-hold 25b and 25c. Not sent: the user sends it. `Retreat_` names are not in the accepted manifest (checked 11 Oct 2026).

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26_Input\
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26a\ (then Batch26b ... Batch26f, one folder per scene)
Copy your context files into each folder's Integrator\Context\, as in earlier batches. Do not write under the live project.

THE METHOD (what stays the same in every scene)
The player chooses a scene; Qori and the bench never move. So every scene uses the **same camera, the same bench and the same Qori** as the approved terrace overlook:
- `Batch26_Input\Actors\`: the approved 25a-11 layers BenchShadow, BenchBack, Qori_Body, Qori_Head, Qori_EarFar, Qori_EarNear, BenchFront, and their registration (pivots, parents, draw order). Copy them into each scene **with the same alpha, pivots and order**. You may relight their colours to the scene's light (dawn warmth, dusk orange, night blue with a warm lamp side, rain grey); shapes, edges and alpha stay byte-identical.
- The camera: 9.6 u behind Qori, eye height 2.3 u, 7° down, horizon at y 660, about 203 px per unit at Qori (`Placement.json`). Paint every scene for that camera: the horizon stays at about y 660.
- **The ground under the bench is level**, the plane in `Reference\Paving_Grid_Guide.png`: the four root feet stand on it at the points in `Placement.json` (`groundUnderBackPosts`, `groundUnderFrontLegs`). Any paving, planks, stone or grass under the bench follows that plane and its perspective; anything with a straight edge near the bench (a step, a kerb, a railing, a log) runs parallel or square to the bench, as the terrace wall does.
- The view leads: the bench and Qori sit in the lower left, the distance opens to the right, the right quarter and the bottom 10% stay calm for the player bar (`Reference\InGame_With_Player.png`).
- The same layer set and registration format as `Batch25a11\Retreat_OverlookTerrace_Registration.json`: Sky; Clouds (tiles horizontally); Far and Mid (2624 wide at offset -32, for a little drift); Mist (tiles horizontally, optional); **Ground** (the floor under the bench, opaque below its horizon edge; replaces Terrace); up to three **Prop_NN** layers for things that move (a tree, a lantern, a banner, reeds), each with a pivot; LanternGlow if there is a lamp; then the seven actor layers; then up to four **Foreground_NN** pieces with base pivots; then **Weather** (a 2560x480 tile repeating vertically, `clip_rect` [0,0,1920,1296] and `exclude_rects` [[300,620,1030,1190]]). Names start `Retreat_<Scene>_`.
- Same painted style, palette handling and soft brushwork as the approved terrace and the region's accepted backgrounds. No spoilers: nothing that shows the land is a body (no titan shapes, faces, ribs read as ribs, hands), no castles, towers or grand ruins, no telescopes.

THE SIX SCENES
| Sub-batch | Prefix | Scene | Light and weather | Match these accepted files |
|---|---|---|---|---|
| 26a (pilot) | `Retreat_CradleMeadow_` | The Cradle meadow: the bench on a flat, mossy stone slab in a wildflower meadow at the edge of a gentle rise, rolling green hills and a stream below, low mist in the hollows | Dawn: pale gold sky warming at the horizon, dew; Weather: drifting pollen motes | Backgrounds\BG_Cradle_Far_v2, BG_Cradle_Mid_v2, BG_Cradle_Near_v2, BG_Sky_A0 |
| 26b | `Retreat_TerracesRain_` | The Terraces: the bench on a flat dry-stone terrace step, olive terraces stepping down, the mill in the distance | Soft rain, grey-green light, a little warmth low in the sky; Weather: rain in the air (not on glass) | BG_Terraces_Far/Mid/Near, BG_Sky_Terraces, BG_Terraces_Mill_Landmark |
| 26c | `Retreat_CausewayDusk_` | The Causeway: the bench on a stone landing where the long causeway sets out across still water, small lanterns along it | Dusk: orange and violet sky reflected in the water, the first lanterns lit; Weather: a few fireflies | BG_Causeway_Far/Mid/Near |
| 26d | `Retreat_RibwoodGlade_` | Ribwood: the bench on flat, mossy flagstones in a forest glade, tall trees framing a view down a valley | Late afternoon: light shafts through the canopy; Weather: slowly falling leaves | BG_Ribwood_Far/Mid/Near |
| 26e | `Retreat_HeightsClouds_` | The Heights: the bench on a flat rock ledge high up, a sea of clouds below, distant peaks above it | Clear cold morning, crisp light; Weather: thin cloud wisps drifting past | BG_Heights_Far/Near, BG_Sky_A4, BG_Sky_A5 |
| 26f | `Retreat_OverlookNight_` | The Qvale overlook terrace, the same place as the approved scene | Night: a starry sky, the valley dark blue with the small lit windows of the settlements, the hanging lantern the warm key light on Qori's left; Weather: a few fireflies | Your approved Batch25a11 scene (reuse its Terrace, Tree and Lantern layers relit; keep their alpha) |

CHECK BEFORE YOU DELIVER (every scene)
Run `python check_batch26.py <Batch26_Input> <scene folder>`. It must report **0 failed**. It tests: every registered file exists with the prefix and its registered size; the seven actor layers are present in order with alpha identical to 25a-11 and the same pivots; solid ground (not sky) under all four root feet; nothing drawn after BenchFront over Qori or the bench; Clouds/Mist tile left to right and Weather top to bottom, clipped as above; and the right quarter and bottom 10% of the composite stay as calm as the approved terrace (at most 1.5x its measured detail). The approved terrace passes everything except the new-prefix test.

ALSO DELIVER (every scene)
`Retreat_<Scene>_Composite.png` (the full stack), `_Composite_UIZones.png`, `_Registration.json`, a 1:1 crop of the bench's feet on the ground, a 1:1 crop of Qori, REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches) and a name-collision check, as in earlier batches. Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested"; original Switch excluded.

Stop after 26a for Claude's review. After it passes, do 26b to 26f and stop once more.
