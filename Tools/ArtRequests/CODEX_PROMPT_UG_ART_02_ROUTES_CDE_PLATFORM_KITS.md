# Codex brief: underground art 02, platform kits for Routes C, D and E

Date: 2 November 2026. The user accepted your Route A kit, revision 02, as the style pilot. This batch applies the same method to the three remaining routes. The underground has no Route B.

Paint all three kits in one pass, then stop for the user's verdict. Each route has its own folder, so the user can accept one and reject another.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (original Switch excluded). Art is reviewed at 3840x2160.

## Read first

- `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, the top of `QOLOSSAL_SESSION_HANDOFF.md`.
- `Tools/ArtRequests/CODEX_PROMPT_UG_ART_01_ROUTE_A_PLATFORM_KIT.md`: its technical contract and readability rules still apply, except where this brief is stricter.
- `Tools/IncomingArt/Codex_v2/UG_Art_01/CODEX_UG_ART_01.md` and `Assets/Art/Underground/PlatformKit/A/`: the accepted result. Match its finish, density and level of detail.
- `Assets/Editor/World/UndergroundArtKit.cs`: how your files reach the game. Read the code, don't change it.
- `Tools/Design/UndergroundPlatforming_20261030/captures/C01.png` to `E10.png`: how the rooms look now.

## Before you start

- Run `git status`. Most recent work is uncommitted and other sessions share this checkout. Touch only the files named below.
- Unity must be closed to run a batch build. If it's open, stop and ask the user to close it. Don't force it closed.
- Copy `Assets/Scenes/Game/Qolossal_Underground.unity` to `Tools/IncomingArt/Codex_v2/UG_Art_02/before/Qolossal_Underground.unity` before your first build, and record its SHA-256.
- The reward-template problem you found last time is fixed in the builder. Your audit should now show the rewards unchanged. If it doesn't, reject that build and report it.

## What to paint

Only paint the pieces each route uses. Route C, D and E have no water, so don't paint water.

| Kind | C | D | E | Width x height in units | PNG files per route |
|---|---:|---:|---:|---|---:|
| `deck` | 35 | 40 | 36 | 2.5-10 x 0.3 | 3 variants |
| `crumble` | 8 | 3 | 3 | 3-4 x 0.3 | 1 |
| `solid` | 15 | 12 | 15 | 3-30 x 1.2-16.5 | 1 |
| `thorns` (new) | 13 | 8 | 6 | 1.5-34 x 0.5, and 0.5 x 5 and 0.5 x 12 in C | 1 |
| `gate` | 3 | 1 | none | 1 x 4 and 3.9 x 0.6 | 1 |
| `seal` | 1 | 2 | none | 4 x 0.6 and 0.6 x 4.4 | 1 |
| `mover` | none | 2 | none | 3 x 0.3 | 1 |
| `rock` (new) | none | 6 | none | 7-10 x 1-1.5 | 1 |

That's 24 PNGs: 8 for C, 10 for D, 6 for E. Don't paint enemies, Qori, backgrounds, outer terrain, thread anchors, pods, gusts, shelters, valves or rewards. Don't paint the Heart, the caves or the pockets.

## Each route's material

All three stay in the approved Qolossal painted style, with the olive, cream and ochre base and only a restrained route accent. For palette and material, also look at these sheets in `Tools/IncomingArt/Ludo/EnemyConcepts/Underground_20261031/`.

**Route C, the Garden (living roots)**
Reference: E-13, E-15, G-04.
Woven root and rib wood, bark, cream petals, olive leaves, small amber seeds. Everything here grows rather than being built. Decks are knotted root shelves, the crumble is rotten split root, the gate is a root lattice and the seal is a clenched root knot.

**Route D, the Wind (exposed heights)**
Reference: E-16, E-17, E-18, G-05.
Wind-scoured limestone, weathered timber, rope, parchment-pale cloth. The palette is the palest and driest of the three. Decks are lashed timber and stone lips, the crumble is frayed planks and split rope, the mover is a vane-driven tray on rope and iron, and the gate and seal are storm shutters.

**Route E, the Descent (quiet memory)**
Reference: E-19, E-20.
Worn sanctuary masonry, dried petals, dull ochre, brittle stems. Sorrowful and still, never horror or gore. It should feel like the end of the journey, and it has no gates, seals or movers.

The three routes must look different from each other and from Route A at a glance in a room capture.

## Gameplay readings to protect

- **Thorns are a hazard:** touching them costs a heart and knocks Qori back, so they must read as dangerous from a distance. Use sharp, pale tips and a jagged silhouette, in every route, even though the material changes. They must never be mistaken for moss, grass or decoration.
- **Solid and rock mean different things:** Qori can wall-cling to `solid` but not to `rock`. They appear together in Route D, where the Climbing Moss skill matters. Paint Route D's `solid` rough, with visible moss grip, and paint `rock` smooth, bare and wind-polished, with no moss at all. In C and E, keep some moss or root grip on `solid`.
- **Crumble is unsafe at a glance:** as in Route A, it can never be mistaken for a deck.

## What Route A taught us

- **Water and hazards:** my first brief described water as harmless, and the art had to be redone. Hazards must look like hazards.
- **Decks must read as ledges:** Deck_03's first version read as a thin line. Every deck needs a clear, thick face below the walk line, within the 0.6 u limit.
- **Solid tops:** Route A's solid top edge is fainter than a deck's against the black room. Make the top of `solid` and `rock` clearly visible on black, because the player needs to see where to land.
- **No regular brick courses,** and keep the repeat quiet. Check `solid` at its largest size, which is now 30 u wide in Route E.
- **Clean edges:** rooms are black today and will get painted backdrops later, so no black matte or halo on any edge.

## The technical contract

Follow UG_Art_01's contract: PNGs with transparency and a `kit.json` in `Assets/Art/Underground/PlatformKit/C/`, `/D/` and `/E/`, at 216 pixels per unit, with native masters kept and nothing upscaled. Write no `.meta` settings by hand.

These extra limits come from `UndergroundArtKit.Dress`. A piece counts as thin when its collider is shorter than the painted canvas minus the overhang. A thin piece keeps its painted height and hangs below the walk line, so an oversized canvas draws past its collider.

| Kind | Canvas height | Overhang | Why |
|---|---:|---:|---|
| `deck`, `crumble` | 156 px | 0.125 | Same as Route A |
| `mover` | 136 px | 0.125 | Walk line 27 px down, leaving 109 px below it |
| `solid`, `rock` | 240 px | 0.125 | Must fit the shortest `rock`, 1 u tall |
| `thorns` | 132 px | 0.1111 | Exactly 0.5 u plus 24 px of tips, so it fills the collider and doesn't draw into the floor |
| `gate`, `seal` | 128 px, and 128 px wide or less for `seal` | 0 | Must fit the 0.6 u pieces |

- **Dimensions:** make every PNG's width and height a multiple of 4. Your Route A report says mover, solid and seal imported uncompressed because their sizes prevented block compression. The sizes above fix that. Don't change the Route A files.
- **Gates and seals are both horizontal and vertical:** the same PNG must work at 1 x 4 and 3.9 x 0.6. Route A's 864 px tall gate would draw far below a 0.6 u gate, so paint a compact repeat, like your Route A seal.
- **Thorns in Route C also stand as a 0.5 u wide column,** up to 12 u tall (C03, C04). There's one PNG per kind and no rotation, so paint a dense bramble that tiles in both directions and looks spiky on all sides. Keep left + right borders under 108 px.
- **Borders:** the shortest deck is now 2.5 u, but keep left + right borders under 324 px as before.

Colliders never change. Art must not imply a wider or taller surface than the collider.

## What you may and may not change

- **Add:** `Assets/Art/Underground/PlatformKit/C/`, `/D/`, `/E/`, and the review folder `Tools/IncomingArt/Codex_v2/UG_Art_02/`.
- **Run:** `UndergroundPlatformingBuilder.BuildBatch` and `CheckBatch`, with the commands in UG_Art_01's brief and the capture folder changed to `UG_Art_02`. Any hand edit to a `UGP Platforming` group is lost on the next build, so don't make any.
- **Don't change:** scripts, `layouts.py`, `platforming_v1.json`, colliders, gameplay, Route A's kit, creature frames, the audio or accepted source files.
- **Don't commit or push.** Don't add to the canonical accepted manifest until the user approves.
- Any play-mode run must pass `-saveFile <test name>`, so the user's save isn't touched.

If a piece lands in the wrong place or the kit won't load, report what you saw and stop on that piece. Don't patch the code.

## Deliver in `Tools/IncomingArt/Codex_v2/UG_Art_02/`

- `CODEX_UG_ART_02.md`: what you made, each file's size and import format, memory per route, and anything you couldn't check. An `index.html` visual review like last time.
- **Room captures from the real build at 3840x2160:** C03, C04, C06, C08, D01, D05, D06, D08, E03, E05, E06, E09.
- **One image per route** showing every kind side by side, plus one image showing all four routes' decks, crumbles and solids together, so the user can judge how different the routes look.
- A crop of each kind at 1280x720 (for Switch 2 handheld), and the greyscale check. Deck, crumble, solid, rock and thorns must be told apart in greyscale.
- **Seam checks:** `deck` at 2.5, 3, 8 and 10 u; `solid` at 3 x 1.2, 8 x 14 and 30 x 14 u; `rock` at 7 x 1 and 10 x 1.5 u; `thorns` at 1.5 x 0.5, 5 x 0.5, 34 x 0.5 and 0.5 x 12 u; `gate` at 1 x 4 and 3.9 x 0.6 u; `seal` at 4 x 0.6 and 0.6 x 4.4 u.
- **A scene audit** like revision 02's: changes confined to Art transforms and SpriteRenderers in Routes C, D and E. Route A, the rewards, colliders and gameplay must be unchanged.
- **Platform QA:** say which were checked in imported form and which only as source art. Don't claim any console, frame-rate or hardware result you didn't measure.

Then stop and wait for the user's verdict. Don't start backgrounds, props, the Heart, caves or pockets.
