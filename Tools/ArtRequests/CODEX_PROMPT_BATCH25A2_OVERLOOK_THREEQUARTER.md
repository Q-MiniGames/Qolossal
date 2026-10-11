Qolossal: Codex Batch 25a-2 (the Qvale overlook retreat again, now on the terrace from your round 2 concept, seen from behind and to the side: the bench at an angle, Qori mostly in profile). Layers for the game; no Unity import, no code, no music work. Stop once at the end for Claude's review.

Written 11 Oct 2026. Not sent: the user sends it. **This replaces Review 55's go-ahead for 25b and 25c: hold them** until the user approves this view. Names `Retreat_OverlookTerrace_` and `Retreat_Qori_ThreeQuarter_` checked free in Tools\ArtImport\codex_v2_accepted.json on 11 Oct 2026.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a2\
Copy your context files into Batch25a2\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHY
The user likes the setting of your round 2 concept `02_Qvale_Overlook_Rear` (copied as Tools\ArtRequests\Batch25\Concept_Overlook_Terrace_UserPick.png): the paved limestone terrace, the low stone parapet, the olive tree with a hanging lantern at top left, the potted flowers, the valley in golden light. But not straight from behind: the user asked for "not totally from behind, like a side-viewed bench". From straight behind Qori loses everything that makes him him. Seen mostly from the side, he keeps his profile: the cream face, the big dark eye, the long swept ears with pale undersides, the leaf cloak. That is how the game shows him, so it can match.

THE VIEW
- Camera behind and to the left of the bench, at about 45° to it, a little above seated head height. The bench is seen at an angle: its near left end (the root-twined post and splayed root feet) closest to the camera, the plain back of the backrest receding toward the valley, the seat's top surface and front edge partly visible. It is the same accepted bench, Assets\Art\Codex\Town\Town_Qvale_ListeningBench.png, in perspective: same wood, waved backrest top, posts, root feet, moss and small leaves. The leaf emblem is on the front of the backrest, so it does not show from this side.
- Qori sits on the bench facing right and slightly away, looking out over the valley. We see him **mostly in profile from his left side, turned about 20–30° away**: the side of his cream face with the big dark eye and a little of the nose visible, the leaf crest and hair behind, both long ears sweeping back and up (green tops, pale undersides, the far one partly hidden by the near one), the leaf mantle, the cloak falling over the seat edge, the thin bark arm resting on his lap or the seat, the thin legs over the seat's front edge with the root feet and ankle wraps showing below it.
- The in-game seated Qori is the model: Tools\ArtRequests\Batch25\InGame_Qori_Seated_Closeup.jpg (the real rig in its Sit pose on this bench) and Codex_v2_full\Batch23b\Integrator\Context\Actors\Qori_144PPU.png. Match his proportions, shapes, palette and soft olive-brown edges exactly; only the angle turns. No staff in this scene. Do not use Batch 19, 19b, 22–22f or 24–24d Qori art (v3 is on hold; 24 was the rejected rear view).
- Setting: from the concept, keep the terrace paving, the low limestone parapet wall with ivy and small white flowers, the olive tree and hanging lantern at top left, the terracotta pot of white flowers at the left edge, the soft foreground leaves. Remove the aqueduct and the hilltop tower town; keep only small Qvale-style houses. The valley matches Assets\Art\Codex\Backgrounds\BG_Qvale_Overlook_Vista.png and your passed 25a (`Codex_v2_full\Batch25a`): you may reuse 25a's Sky, Clouds, Far, Mid and Mist paintings where they fit this camera.
- Composition: Qori and the bench in the lower left third, the bench about a third of the frame width, Qori with ears about 36–40% of the frame height. The valley, the river glint and the light open to the right. The right quarter (x 1920–2560) and the bottom 10% (y 1296–1440) stay calm: soft sky, soft distance, plain paving.
- Painted 2D in the game's style. No 3D render look, no photographic blur.

PART A: THE QORI CHECK SHEET (first; Part B uses it exactly)
`Retreat_Qori_ThreeQuarter_Sheet.png`, 2560x1440, flat background, no text on the art: on the left, the in-game seated Qori cut from InGame_Qori_Seated_Closeup.jpg; beside it at the same scale, your seated Qori at the scene's angle on the bench; and the same three-quarter Qori standing, beside the rig's standing figure (Qori_144PPU.png), on one feet line. In the handoff, list head height to total height, ear length to head height and eye size to head height, measured on the rig and on your drawing.

PART B: THE LAYERED SCENE
Every layer is a 2560x1440 RGBA PNG registered at (0,0), straight alpha, no halo, except where noted. Names start `Retreat_OverlookTerrace_`.

| # | File | Contents | Notes |
|---|---|---|---|
| 01 | `Sky` | Sky, opaque | |
| 02 | `Clouds` | Clouds only | Tiles horizontally |
| 03 | `Far` | Far ridges and haze | 2624x1440 at offset -32, for a little drift |
| 04 | `Mid` | Valley, river, groves, houses | 2624x1440 at offset -32 |
| 05 | `Mist` | Low mist bands only | Tiles horizontally |
| 06 | `Terrace` | Paving, parapet wall, flowers along it, the pot | Opaque across the bottom |
| 07 | `Tree` | The olive tree's trunk and branches at top left, without the lantern | For a slow sway: give a pivot at the trunk |
| 08 | `Lantern` | The hanging lantern and its cord | Pivot at the cord's top, for a small swing |
| 09 | `LanternGlow` | The flame and its warm glow only | For flicker |
| 10 | `Bench` | The bench in perspective | Painted whole, also where Qori covers it |
| 11 | `Qori_Body` | Seated Qori without head and ears | The neck painted fully under the head |
| 12 | `Qori_Head` | Head, face and crest, without ears | Painted fully where the ears cover it |
| 13 | `Qori_EarFar`, 14 `Qori_EarNear` | Each ear | Base painted fully, tucking under the crest |
| 15 | `Qori_Blink` | The eye closed, sitting exactly on the head | For blinking |
| 16 | `Foreground_01`–`_04` | Soft leaves and grass at the edges, each on its own small canvas | Base pivots; clear of Qori, the bench and the UI zones |
| 17 | `Weather` | Drifting seed-fluff motes, 2560x480 | Top and bottom rows match |

Also deliver `Retreat_OverlookTerrace_Composite.png`, `_Composite_UIZones.png`, `_MotionProof.png` (head ±3°, each ear ±8°, lantern ±4°, no gaps), and `_Registration.json` (order, sizes, offsets, pivots, motion ranges, wrap directions, weather period).

FOR CLAUDE'S REVIEW
REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches), a name-collision check, and a source archive with your prompts, as in earlier batches. In the handoff: the Part A ratios; a 1:1 crop of Qori in the scene beside the in-game closeup at the same scale; a crop of the bench beside the accepted front-view bench. Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested"; original Switch excluded.

Then stop for Claude's review.
