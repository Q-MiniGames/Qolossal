# Codex brief: underground art 03, the remaining underground art

Date: 2 November 2026. The Route A, C, D and E platform kits are done. This brief covers the rest of the underground's art in one run, delivered as separate sets the user can accept or reject one at a time.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (original Switch excluded). Art is reviewed at 3840x2160.

**Not in this batch:** repainting the rooms' outer terrain, foreground overlays, lighting, health bar art, enemies, Qori, rewards and checkpoints. Don't start them.

## Read first

- `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, the top of `QOLOSSAL_SESSION_HANDOFF.md`.
- `Tools/ArtRequests/CODEX_PROMPT_UG_ART_02_ROUTES_CDE_PLATFORM_KITS.md` and `CODEX_PROMPT_UG_ART_02_REVISION_01.md`: their technical limits and readability rules still apply, except where this brief is stricter.
- `Assets/Art/Underground/PlatformKit/A/`, `C/`, `D/`, `E/`: the finish, density and level of detail to match.
- `Assets/Editor/World/UndergroundArtKit.cs` and `Make` in `UndergroundPlatformingBuilder.cs`: how your files reach the game. Read the code, don't change it.
- `Tools/IncomingArt/Codex_v2/UG_Art_02/revision_01/seam_fix/captures/`: how all 89 rooms look now, including each region's terrain.
- `Tools/IncomingArt/Codex_v2/UG_Art_02/revision_01/seam_fix/heal_solid.py`: read this before painting any tiling face (see "What the last batch taught us").
- For reference only: `Assets/Art/Codex/Underground/Cistern/` (backgrounds, valve, thread anchor) and `Hollow/`, and the sheets in `Tools/IncomingArt/Ludo/EnemyConcepts/Underground_20261031/`.

## Before you start

- Run `git status`. Most recent work is uncommitted and other sessions share this checkout. Touch only the files named below.
- Unity must be closed to run a batch build. If it's open, stop and ask the user to close it. Don't force it closed.
- Copy the scene to `Tools/IncomingArt/Codex_v2/UG_Art_03/before/Qolossal_Underground.unity`, then **run `BuildBatch` once before adding any art** and keep that scene as `before/baseline_built.unity`. The layout data has just changed (27 tall blocks became decks), so your audit must compare against this baseline, not the older scene.
- The code hooks for this batch are new and have never run. Test each on one piece first. If one fails, report what you saw and stop on that piece. Don't patch the code.

## 1. Shared objects: `Assets/Art/Underground/PlatformKit/Shared/`

Any room whose set has no art for a kind falls back to this set. All PNGs at 216 PPU unless stated.

| Kind | Used | Collider, in units | kit.json | How it must read |
|---|---:|---|---|---|
| `anchor` | 64 | 0.6 x 0.6 | `simple: true`, about 216 x 216 px | Thread anchor: a clear ring or knot, visible from a distance. Keep the centre on the sprite's centre |
| `pod` | 13 | 1 x 0.8 | `simple: true`, about 216 x 172 px | Bounce pod for Bloomfall: soft, plump, plainly springy. It squashes at runtime |
| `valve` | 4 | 1.2 x 2 | `simple: true`, about 432 x 432 px | A wheel. The whole sprite spins around its centre when turned, so paint only the wheel, no post |
| `spill` | 9 | 1-2.8 x 4.5-10 | tiled | Falling water sheet that hurts: dark and churning like Route A's water, with a pale edge. Must tile vertically |
| `gust` | 19 | 1.1-16 x 4-12 | tiled | Wind lane. The game tints it at 12, 35 and 75 % opacity for calm, warning and blast, so paint pale streaks that read at all three. Must tile both ways |
| `shelter` | 17 | 3-7 x 2.5-3 | tiled, `order: 3` | Wind-safe niche, drawn behind Qori. Subtle: dark, with a sheltering lip |

`simple` pieces are drawn once at their painted size and may be larger than the collider. Use `offset: [x, y]` only if the art needs it, and never on `anchor`.

## 2. Room backdrops: 12 sets

Add `backdrop_far` and `backdrop_near` pieces to each set's `kit.json`: `A`, `C`, `D`, `E`, `H`, `P`, `MR01`, `MR02`, `MR03`, `MR05`, `MR06`, `MR07`. These are the only additions to the A, C, D and E kit files. They're tiled across each room behind all terrain and platforms, with no parallax yet, and rooms are black today.

| Layer | Canvas | `ppu` | Repeat | What it is |
|---|---:|---:|---|---|
| `backdrop_far` | 2048 x 1024, opaque | 108 | 19 x 9.5 u | Soft, distant depth: the route's deep cavern, in that route's material |
| `backdrop_near` | 2048 x 1024, with transparency | 144 | 14.2 x 7.1 u | Dim silhouettes only, such as roots, columns or pipes, with clear gaps |

- Both must wrap in both directions, with no cut or smear at a join.
- **They must stay dark.** Mean luminance of at most 40 of 255 for far and 50 for near, measured and reported per file. Platforms, thorns, Qori and creatures must read first.
- **Nothing may look standable:** no flat ledge shapes, bright horizontal edges or hazard-like spikes.
- Match each set to its route or region. Route A's existing Cistern backgrounds are a reference, and you may rework them if they meet these rules.

## 3. Cave and pocket platform kits

Create `Assets/Art/Underground/PlatformKit/MR01/`, `MR02/`, `MR03/`, `MR05/`, `MR06/`, `MR07/` and `P/`, in each region's material and terrain colour (see the captures). Use 2 deck variants per set.

| Set | Region | Kinds |
|---|---|---|
| MR01 | Cradle | deck, crumble, solid, rock, thorns, seal |
| MR02 | Causeway | deck, solid, rock, thorns, seal, water, mover |
| MR03 | Terraces | deck, crumble, solid, rock, thorns, seal, water, mover |
| MR05 | Ribwood | deck, crumble, solid, rock, thorns |
| MR06 | Heights | deck, crumble, solid, thorns |
| MR07 | Summit | deck, crumble, solid, rock, thorns, water, mover |
| P | Pockets | deck, crumble, solid, rock, thorns |

Sizes and limits are the same as Routes C, D and E, using revision 01's values: solid 1024 x 1024, thorns 416 x 160 with overhang 0.2407, seal 128 x 128. Water follows Route A's, and must look dangerous. Other points:

- **Rock stands tall here:** it reaches 1 x 13 u in MR05, MR07 and P. Use a 1024 x 1024 canvas, border `[24,24,24,60]`, smooth and bare, with no moss. It must stay different from that set's `solid`, which Qori can cling to.
- **Thorns also stand as columns** up to 0.5 x 10 u in MR02, so they must tile both ways there.
- **Short pieces:** decks and crumbles can be 2 u wide, and solids 1 u tall. Check these sizes.
- A few thorn entries in the layout data are near-zero in size. Report them, but don't change the data.

## 4. The Heart: set `H`

- `deck`, 2 variants, up to 28 u wide. `grille`, up to 28 x 48 u: see-through bars with real transparency, tiling both ways, which block Qori. Read `QuarterDisc()` and `MraHeartQuarter.cs` first.
- `quarterlight`, `fit: true`, canvas 1352 x 2488: one quarter of the big oval heart. The game mirrors it into four, with the round edge facing out, so the joins must meet along the two straight edges. Check how the game colours and lights each quarter, and paint so that still works.
- `quarter`, `simple: true`, about 2 u tall: the object Qori wakes in each sector.

## 5. Room props: `Assets/Art/Underground/Dressing/`

One `<roomId>.json` per room, with PNGs in `Dressing/Art/`. A rebuild replaces them, and they have no colliders.

```json
{ "items": [ { "file": "A08_Sluice_01.png", "x": 26, "y": 2, "scale": 1, "flipX": false, "order": -9 } ] }
```

`x` and `y` are in units from the room's bottom-left corner, placing the sprite's centre. Use 216 PPU and an `order` from -29 to -1, so props sit behind platforms and creatures.

- **Guardian and mini-boss rooms, 3 to 6 props each:** A08 Sluice Warden, C08 Root Matron, D08 Tempest Owl, MR01_C02 Moss Matriarch, MR02_C03 Echo Warden, MR03_C04 Grain Golem. Each should say whose room it is. Use the creature sheets for reference.
- **The Heart:** 4 to 8 props, one or two per sector.
- **Door surrounds:** one piece per route (A, C, D, E), placed at every side door in those routes' rooms using the door data in `rooms.json`, and generated by a small script you keep in the review folder. Floor and ceiling holes don't need one.
- **Props must never look standable, or block the view of a landing, hazard or creature.** Check a room's whole play area against this, and keep them low in contrast.

## What the last batch taught us

- **Tiling faces must truly wrap.** In your last solids, blending 12 pixels at the slice edges forced the numbers to match, but left a visible cut through the rocks and smeared streaks. I rebuilt them with a minimum-error cut through the painting's overlap (`heal_solid.py`). Use that method or paint truly seamless art. Check every tiling piece by repeating it 3 x 3 and looking at it, not by measuring edge difference.
- **Borders join a clipped tile on short pieces.** A dark rim on the side and bottom borders hides this.
- **At game size, hazards must look like hazards.** Judge them at 72 pixels per unit, beside Qori.
- **Don't stop before installing.** Last time the first run ended after the paintings. Finish each set: PNGs, `kit.json`, build, checks and report.

## Technical rules

- PNGs with transparency, every side a multiple of 4, native masters kept, nothing upscaled. No `.meta` settings by hand.
- **Memory:** report imported size per set and the total. Aim for at most 4 MiB per backdrop pair and 6 MiB per cave kit, and say if a set exceeds this.
- Colliders never change. Don't edit scripts, `layouts.py`, `platforming_v1.json`, creature frames, audio, or any existing PNG in the A, C, D and E kits.
- Any hand edit to a `UGP Platforming` group is lost on the next build. Any play-mode run must pass `-saveFile <test name>`.
- **Don't commit or push.** Don't add to the canonical accepted manifest until the user approves.

## Commands

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/UnityProjects/Qolossal" -executeMethod UndergroundPlatformingBuilder.BuildBatch -logFile build.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/UnityProjects/Qolossal" -executeMethod UndergroundPlatformingBuilder.CheckBatch -captureDir "C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/UG_Art_03/captures" -logFile check.log
```

## Deliver in `Tools/IncomingArt/Codex_v2/UG_Art_03/`

- `CODEX_UG_ART_03.md` and an `index.html` review, organised by set, so each can be accepted alone. List every file's size, format and memory, and anything you couldn't check.
- **Room captures at 3840x2160:** A04, A08, C04, C08, D05, D06, D08, E03, E05, HEART, MR01_C02, MR02_C03, MR03_C04, one more cave per region, and two pockets.
- **Readability:** 1280x720 crops for each new kind and backdrop set, with Qori beside the piece, plus greyscale copies.
- **Seam checks:** every tiling piece repeated 3 x 3, plus each kind at its smallest and largest size in the data.
- **A scene audit** against `before/baseline_built.unity`. Colliders, gameplay, creatures, rewards and all existing platform art must be unchanged. List exactly what was added.
- **Platform QA:** say which were checked in imported form and which only as source art. Don't claim any console, frame-rate or hardware result you didn't measure.

Then stop and wait for the user's verdict.
