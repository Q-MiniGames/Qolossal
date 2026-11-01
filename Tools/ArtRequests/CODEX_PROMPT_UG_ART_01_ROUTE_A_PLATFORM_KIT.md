# Codex brief: underground art 01, Route A platform kit (pilot)

Date: 1 November 2026. Requested by the user: Codex paints the underground and adds the assets to the project directly.
This is the style pilot. Do Route A's platform kit only, then stop for the user's verdict. Later sets copy what this one proves.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (original Switch excluded). Art is reviewed at 3840x2160.

## Read first

- `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, the top of `QOLOSSAL_SESSION_HANDOFF.md`.
- `Assets/Editor/World/UndergroundArtKit.cs`: how your files reach the game. Read the code, don't change it.
- `Tools/Design/UndergroundPlatforming_20261030/layouts.py` (the rooms) and its `captures/A01.png` to `A10.png` (how they look now).
- For palette and material only: `Assets/Art/Codex/Underground/Cistern/` and the Route A creature sheets in `Tools/IncomingArt/Ludo/EnemyConcepts/Underground_20261031/`.

## Before you start

- Run `git status`. Most recent work is uncommitted and other sessions share this checkout. Touch only the files named below.
- Unity must be closed to run a batch build. If it's open, stop and ask the user to close it. Don't force it closed.
- Copy `Assets/Scenes/Game/Qolossal_Underground.unity` to `Tools/Design/UndergroundPlatforming_20261030/backup/Qolossal_Underground.before_ug_art_01.unity` before your first build.

## What to paint

Route A is the Cistern: old stone waterworks, wet silt, iron sluice gear, thin moss. Paint these for `Assets/Art/Underground/PlatformKit/A/`:

| Kind | In Route A | What it is | How it must read |
|---|---|---|---|
| `deck` | 66 pieces, 1.5 to 8 u wide (other routes reach 28 u) | A one-way ledge. Qori jumps up through it | Thin and light: a plank walkway or stone lip with open space behind. At least 3 variants |
| `crumble` | 11 | The same size, but breaks when stood on | Clearly unsafe at a glance: cracked, split, sagging. Never mistakable for a deck |
| `mover` | 5 | A moving platform, 3 u wide | Mechanical: a barge or lift tray on iron |
| `solid` | 19 (across the underground, up to 30 u wide and 18 u tall) | A heavy block Qori can wall-cling to | Massive masonry with a walkable top and climbable faces |
| `gate` | 3 | A shut sluice gate | Heavy iron bars or a plate, plainly a barrier |
| `seal` | 2, in the guardian arena | A door that shuts during a boss fight, up to 4 u wide or tall | Dropped iron shutter, reads as "locked for now" |
| `water` | 6 | Shallow water, 0.6 to 1 u deep | CORRECTED 1 Nov: it is a hazard (touching it sends Qori back to the last safe spot), so it must read as dangerous. The first version followed the old wording here |

Don't paint enemies, Qori, backgrounds, the room's outer terrain, thread anchors, pods, valves, rewards, thorns or `rock` blocks in this batch (Route A has no `rock` or thorns).

## The technical contract

1. **Files:** PNGs with transparency, plus one `kit.json`, directly in `Assets/Art/Underground/PlatformKit/A/`. Write no `.meta` settings by hand: the builder sets each PNG's import from `kit.json`.
2. **`kit.json`:**

```json
{
  "set": "A",
  "pixelsPerUnit": 216,
  "pieces": [
    { "kind": "deck", "files": ["Deck_01.png", "Deck_02.png", "Deck_03.png"], "border": [54, 0, 54, 0], "overhang": 0.125 }
  ]
}
```

3. **Density:** at least 216 pixels per unit. Use 216 unless you have a measured reason. Keep native masters; never upscale.
4. **The walk line:** `overhang` is how many world units are painted above the collider's top. The surface Qori's feet touch must sit exactly that far below the sprite's top edge. At 216 PPU and 0.125 u, that's 27 pixels. Anything above that line is light moss or a kerb only.
5. **Tiling:** pieces are drawn with Unity's 9-slice tiled mode. `border` is left, bottom, right, top in pixels. The ends stay fixed and the centre repeats, so the centre must tile with no visible seam or obvious repeat at 28 u. Test the shortest piece too: at 1.5 u the two ends must still look right, so keep left + right borders under 1.5 u (324 px).
6. **Thin pieces** (`deck`, `crumble`, `mover`) keep their painted height and hang from the walk line. The collider is only 0.3 u thick. Keep the painted thickness to 0.6 u or less below the walk line, so a jump up through it looks fair.
7. **Thick pieces** (`solid`) fill their full size. `border` top holds the kerb and the centre tiles in both directions.
8. **Colliders never change.** Art may not imply a wider or taller surface than the collider. Check the ends of every piece.

## Readability rules

- Qori is 0.8 x 1.2 u. He and every creature must stay clearly readable in front of all of this.
- Keep the walk line the brightest or sharpest edge on a piece. A player must see where to land in under a second.
- Deck, crumble and solid must be told apart in a greyscale view at 1280x720.
- Soft, painted, in the approved Qolossal style. No hard outlines unless the existing art has them.

## What you may and may not change

- **Add:** `Assets/Art/Underground/PlatformKit/A/` and a review folder, `Tools/IncomingArt/Codex_v2/UG_Art_01/`.
- **Run:** `UndergroundPlatformingBuilder.BuildBatch`, which rewrites the rooms' `UGP Platforming` groups and the scene. Any hand edit to those groups is lost on the next build, so don't make any.
- **Don't change:** scripts, `layouts.py`, `platforming_v1.json`, colliders, gameplay, other rooms' art, creature frames, the audio, or accepted source files.
- **Don't commit or push.** Don't add to the canonical accepted manifest until the user approves the set.
- Any play-mode run must pass `-saveFile <test name>`, so the user's save isn't touched.

## Batch commands

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/UnityProjects/Qolossal" -executeMethod UndergroundPlatformingBuilder.BuildBatch -logFile build.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/UnityProjects/Qolossal" -executeMethod UndergroundPlatformingBuilder.CheckBatch -captureDir "C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/UG_Art_01/captures" -logFile check.log
```

`UndergroundArtKit` is new and hasn't been run against real art yet. If a piece lands in the wrong place, or the kit won't load, report what you saw and stop on that piece. Don't patch the code.

## Deliver in `Tools/IncomingArt/Codex_v2/UG_Art_01/`

- `CODEX_UG_ART_01.md`: what you made, each file's size, and anything you couldn't check.
- Room captures of A03, A04, A06 and A08 from the real build, at 3840x2160.
- A crop of each kind at 1280x720 (for Switch 2 handheld), and the greyscale check.
- A seam check: `deck` at 1.5, 3, 6 and 28 u, and `solid` at 2 x 2 and 30 x 18 u.
- **Platform QA:** say which were checked in imported form and which only as source art. Don't claim any console, frame-rate or hardware result you didn't measure.

Then stop and wait for the user's verdict. Don't start Route C, backgrounds or props.
