Qolossal: Codex Batch 25 (the listening retreat, outdoors: three painted bench views, side-on, in the game's own style). Backgrounds only: no bench, no Qori, no Unity import, no code, no music work. Do 25a first and stop for Claude's review. Do 25b and 25c only after 25a passes, then stop once more at the end.

Written 11 Oct 2026. Not sent: the user sends it. Names checked free in Tools\ArtImport\codex_v2_accepted.json on 11 Oct 2026 (no `Retreat_` names are accepted; the Batch 24 `Retreat_RainWindow_` set is not used and not accepted).

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a\ (then Batch25b, Batch25c)
Copy your context files into each folder's Integrator\Context\, as in earlier batches. Do not write under the live project.

WHY
The user tried Batch 24's Rain Window in the game and turned down both the room and the rear-view Qori: the retreat should be outdoors, and Qori must look like Qori. From behind he loses everything that makes him him (the cream face, the big dark eyes, the long swept ears with pale undersides, the cloak), and a new rear drawing never matches the rig. So the retreat changes approach:
- The view is **side-on**, like the game. Qori sits on the bench facing right, looking out over the view.
- **The game draws the bench and Qori itself**: the accepted bench sprite (Town/Town_Qvale_ListeningBench) and the real in-game Qori rig in its Sit pose, so he looks and moves exactly as in play (blinks, ear sway, breathing). See Tools\ArtRequests\Batch25\InGame_Qori_Seated_Closeup.jpg: that is how he will look.
- **You paint only the place**: sky, distance, the ground the bench stands on, and a little foreground, as layers.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Tools\ArtRequests\Batch25\Placement_Guide.png (2560x1440): the bench drawn where the game will put it (front view, 2x, x 328 to 1192, its feet on the line y = 1190), the blue box where seated Qori will be (x 520 to 1000, y 640 to 1190), and the two UI zones in red (right quarter x 1920 to 2560, bottom 10% y 1296 to 1440).
3. Tools\ArtRequests\Batch25\InGame_Qori_Seated_Closeup.jpg and InGame_Overlook_Seated_View.jpg: the real seated Qori on that bench, and the game's current Qvale overlook.
4. The accepted art for each place (below), for palette, shapes and paint handling. Match it; this is the same world.
5. Your Batch 24 notes, for what carried over: the rain repeat tile, the horizontally wrapping mist, the calm zones and the package checks. The room and the rear-view Qori are dropped.

THE THREE PLACES

| Sub-batch | Name prefix | Place and light | Match these accepted files |
|---|---|---|---|
| 25a (pilot) | `Retreat_Overlook_` | The Qvale overlook: a level limestone-edged grassy shelf above the valley, golden late afternoon, soft haze, the river glinting, small settlements | Backgrounds\BG_Qvale_Overlook_Vista, BG_Qvale_Far/Mid/Near, your round 2 concept 02_Qvale_Overlook_Rear for the mood (not its camera) |
| 25b | `Retreat_Meadow_` | A Cradle meadow at dawn: dew, low mist in the hollows, pale sky warming to gold, wildflowers | Backgrounds\BG_Cradle_Far_v2, BG_Cradle_Mid_v2, BG_Cradle_Near_v2, BG_Sky_A0 |
| 25c | `Retreat_Terraces_` | The Terraces in soft rain: stepped olive terraces, dry-stone walls, the mill in the distance, grey-green light, a little warmth low in the sky | Backgrounds\BG_Terraces_Far/Mid/Near, BG_Sky_Terraces, BG_Terraces_Mill_Landmark |

No spoilers: nothing that shows the landscape is a body (no titan shapes, faces or hands), no castles, towers, ruins of grand fantasy, telescopes, moons or new buildings beyond the small Qvale-style houses already in the game.

COMPOSITION (all three)
- Side-on, eye level slightly above the bench seat, wide and calm. The bench box and the Qori box in the guide sit on a small level patch of ground (grass, a stone edge, a few flowers or roots) that clearly carries a bench. Nothing painted inside the Qori box or the bench box except ground below y = 1190 and sky/distance behind.
- Qori looks right, so the view opens to the right: the distance, the light and the interest are in the middle and right of the frame.
- The right quarter and the bottom 10% stay quiet (soft sky, soft distance, soft ground; no strong detail or highlights), as in Batch 24.
- Painted 2D in the game's style, matching the accepted backgrounds' brushwork and palette. No 3D render look, no photographic blur.

LAYERS (each place; back to front)

Every full layer is a 2560x1440 RGBA PNG, registered at (0,0), straight alpha, no halo. Stacking them in order gives the scene.

| # | File (after the prefix) | Contents | Notes |
|---|---|---|---|
| 01 | `Sky` | Sky only, opaque, full canvas | |
| 02 | `Clouds` | Clouds only, on transparent | Tiles horizontally (left and right edges match), so it can drift |
| 03 | `Far` | The far distance (ridges, haze) | Painted 32 px past each side (canvas 2624x1440, offset -32), so it can drift a little |
| 04 | `Mid` | The middle distance (hills, terraces, river, houses) | Same: 2624x1440, offset -32 |
| 05 | `Mist` | Low mist bands only, on transparent | Tiles horizontally |
| 06 | `Near` | The near slope or edge beyond the bench patch | 2624x1440, offset -32 |
| 07 | `Ground` | The bench patch and the ground across the bottom, opaque below the ground line | The bench's four feet stand on y = 1190; the ground under them is level |
| 08 | `Foreground_01` to `_06` | Up to six separate tufts of grass, flower stems or a leafy branch tip, each on its own small canvas, for swaying | Each with a base pivot in the registration; keep them clear of the bench and Qori boxes and of the UI zones |
| 09 | `Weather` | 25c only: a rain repeat tile, 2560x480, top and bottom rows matching. 25a: a few drifting seed-fluff motes as a tile. 25b: none, or a light dew-sparkle tile | Scrolls; must read as weather in front of the distance, not on glass |
| 10 | `Light` | Optional warm light (sun glow, a soft shaft), on transparent | For a slow pulse; nothing across the Qori box brighter than his own lighting |

Also deliver per place:
- `<prefix>Composite.png`: the layers stacked, with the bench pasted at the guide position and the in-game seated Qori (cut from InGame_Qori_Seated_Closeup.jpg, scaled into the blue box) for the preview only. Mark the preview "bench and Qori: game-drawn" in the handoff, not on the art.
- `<prefix>Composite_UIZones.png`: the same with the UI zones outlined.
- `<prefix>Registration.json`: layer order, canvas sizes and offsets, wrap directions, the weather tile period, each foreground piece's canvas position and base pivot, and the bench placement (x 328, foot line y 1190, scale 2.0 of Town_Qvale_ListeningBench) as the ground truth the game will use.

FOR CLAUDE'S REVIEW
REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches), a name-collision check, and a source archive with your prompts, as in earlier batches. In the handoff: a crop of the bench patch at 1:1 with the bench pasted (the feet must sit on the ground, not float or sink), proof of the horizontal and vertical seams, and a short note on how each place matches its accepted backgrounds. Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested"; original Switch excluded.

Stop after 25a for Claude's review. After it passes, do 25b and 25c and stop once more.
