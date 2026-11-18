# Codex brief: Batch 49, "The Keepers' Cistern", the new background theme for Route A

Written 7 Nov 2026 by Claude for the user's request: "come up with a better theme to replace the current one in route A ... ask codex to draw the artwork as per your recommendation and exact dimensions making sure it's for 4K game. No mistakes."
It replaces the CisternNetwork_20261107 trial (lake and mountain vistas). The reasons are in `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_97.md`. Read it first.

**Three stages, with a stop after each.** 49a: key frames (concepts). 49b: the A04 slice, painted and placed in the Editor preview. 49c: the rest of Route A. Don't start a stage until Claude has reviewed the previous one and the user has said go. Every dimension for all three stages is in this brief, so nothing will need re-measuring later.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (handheld and docked). The original Switch is excluded. Master art is native 4K. Every device test is "not tested" unless you actually ran it.

---

## 0. Before you start (all must hold, or stop and say which failed)

- Run `git status`. Other sessions share this checkout. Touch only the paths this brief names.
- The delivery folder `C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch49\` doesn't exist yet or is empty. Never overwrite a delivery. If 49 is taken, use the next free number and say so.
- Every output name in section 6 is free in the global name check: case-insensitive, against `Tools\ArtImport\codex_v2_accepted.json`, its sub-sprite names and every stem under `Assets\`. Re-read the manifest and record its count. Don't trust any number in this brief.
- Unity: for 49b only, the Editor preview needs Unity. If another session is using it, stop and ask the user. Don't force it closed.

## 1. Read first

1. `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_97.md`: what failed in the trial and why. Section 4 of this brief lists those failures as automatic fails.
2. `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, the top of `QOLOSSAL_SESSION_HANDOFF.md`, and `PROJECT_GUIDE.md` from "Cave background direction" onward.
3. `CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md`, sections 4 and 6: the native-section pipeline (Batch 28d) is **required** here for every canvas wider or taller than one native generation.
4. `Tools/Design/UndergroundPlatforming_20261030/rooms.json` and `layouts.py` (the rooms, doors and platforms), and `Assets/Resources/CavePassageConnections.json` (the passages).
5. Your own trial for the tooling only: `Assets/Scripts/Environment/CisternNetworkReview.cs`, `Assets/Editor/CisternContinuousPreview.cs` and `Tools/Design/CisternNetwork_20261107/`.
6. Palette and material reference: `Assets/Art/Codex/Underground/Cistern/`, the Route A platform kit `Assets/Art/Underground/PlatformKit/A/`, and the Route A creatures. **Composition reference: `Assets/Art/Prototypes/CisternNetwork_20261107/DrainWell_Far.png`**, the one trial painting that already reads as a cave wall.

## 2. The theme

**The Keepers' Cistern.** Deep under Qvale lies a vaulted waterworks built by the vanished Keepers. Their aqua limestone and verdigris bronze still hold the water, but glowing mint roots have grown through every seam to drink it, and they all lead one way: toward the Heart.

Three pillars. Every painting must serve all three.

1. **Built and enclosed.** Vaults, arches, columns, conduits and machinery. A ceiling, vault or overhang is in view in every frame. Depth ends in **darkness within tens of metres**: no horizon, sky, mountains, lake shore, haze-to-white or distant landscape anywhere.
2. **Water tells the story.** Tide lines (mineral stains from old water levels), still pools that reflect lanterns, falling sheets in the shafts, carved gauge marks. They echo the route's water-level puzzles.
3. **Three lights, each with a job.**
   - **Daylight** (cool white-aqua): only at the arrival, A01's light-well. It is gone by A03, which tells the player they are now underground.
   - **Keeper lanterns** (warm amber): safety and rest. A few per room, many in A07.
   - **Mint root-glow** (cool mint): wayfinding. It grows denser and brighter room by room toward A08, and is brightest in the seams of A08's great sluice gate, with the Heart behind it.

Darkness is part of the look. Large areas of rear wall fall into deep shadow, and the lit features carry the picture. This is also what keeps the memory budget sane.

### Palette (sRGB; a guide, not exact pixels)

| Role | Hex |
|---|---|
| Deepest shadow | `#0E1E21` |
| Deep teal shadow | `#17332F` |
| Wet slate | `#2C3B3D` |
| Limestone, mid | `#6E9A92` |
| Limestone, lit | `#A9C4B8` |
| Verdigris bronze | `#4E8A74` |
| Lantern amber | `#F0A848` |
| Mint root-glow | `#92F2C8` |
| Daylight (A01 only) | `#DCEFF0` |

### Mystery rule (automatic fail)

The world is a sleeping titan, and players must not suspect it. Nothing may read as a body: no ribs, bones, vertebrae, skin, fingers, knuckles, faces, eyes, lids, brows or veins-under-skin. Roots are plant roots: irregular, branching, never parallel bundles. Columns are squared, built masonry with capitals and arches, never rounded or tapering limb-like forms. Check every piece at full size, at 10% and blurred.

### Readability rules

- Qori is 0.8 × 1.2 u. Route A creatures are cream, green and orange. Nothing behind gameplay may use cream-green at mid values.
- Background luminance stays at or below 70% of the walk line's, except light sources (lanterns, glows, beams), which are small.
- The walk line stays the brightest, sharpest edge on screen. Greyscale check at 1280 × 720.
- Soft, painted, the approved Qolossal style. No hard outlines unless the existing art has them.

## 3. Layers, motion and density (fixed; use exactly these)

Camera: orthographic half-height 5, so a 3840 × 2160 frame is **17.778 × 10 world units at 216 px per unit**. The camera is clamped to each room's rectangle. Motion is given as the share of terrain motion (100% = attached to terrain).

| Layer | Horizontal | Vertical | What lives here |
|---|---|---|---|
| Foreground (FG) | 110% | 105% | Dark silhouettes hanging from ceilings only. Never crossing a jump path or covering Qori's band. |
| Rear wall (CK_, SET_) | 100% | 100% | Attached to the room's geometry: the wall behind the play plane. Most of each frame. |
| Middle (MID) | 88% | 94% | One landmark per room, in front of the far. **Its base is always hidden** behind terrain, water, a platform or a `CK_Mound`. |
| Far (FAR) | 50% | 60% | Only in A04 and A06, the two open chambers. Seen through and between rear-wall arches and columns. |

Far placement, per room: `farCentre = roomCentre + (cameraCentre − roomCentre) × (1 − motion)`, per axis. The far sizes in section 6 are worked out from this formula with 1 u of margin on each side. If anyone changes a motion value, the far sizes must be recalculated before painting.

**Density: every file is 216 px per world unit, native.** Never upscale, stretch, resample up or duplicate rows. Every size in section 6 is a whole number of half-units, so every edge is a multiple of 108 px and divides into BC7's 4 × 4 blocks. **No file exceeds 4096 px on a side.** Larger pieces are delivered as named tiles, each cut exactly from one native master, with no overlap and no gap.

## 4. Automatic fails (from Review 97 and earlier batches)

- Any sky, horizon, mountain range, lake shore, open valley or outdoor haze. **This was the trial's main failure.**
- A frac()-tiled or flat material visible as a surface on its own: the trial's vault and passage stone.
- A hard horizontal or vertical edge where one painting ends: the trial's A03 and A05 bottom.
- Two paintings cross-faded so one shows through the other: the trial's glide double exposure.
- A middle-layer object whose base touches Qori's floor line or a water surface: the trial's Keeper house and pump island.
- A painting enlarged to cover more area.
- Generator alpha used as a mask. Paint opaque, then mask with your own drawn silhouette (at most a 2 px anti-aliased edge). Exception: overlays named "soft alpha" in section 6 (tide stain, glow, beam, water sheet) may have graded alpha, but from your own mask, never the generator's.
- Repeated motifs or a regular rhythm in unique pieces. Kit pieces may repeat as separate placements; whole compositions never repeat.
- Anything under the mystery rule.
- Writing under `Assets\` in stage 49a, or changing an accepted file in any stage.

## 5. The rooms (from `rooms.json`; world units, origin bottom-left)

| Room | Rect (x, y, w, h) | Type | Layers | Signature (rear wall unless marked MID) | Light |
|---|---|---|---|---|---|
| A01 Sluice Arrival | 1480, 170, 36, 22 | Enclosed arrival | rear wall, MID | Inlet hall: a ceiling light-well grate with daylight beams, the first sluice gate. MID: sluice winch | Daylight + 2 lanterns |
| A02 Drain Well | 1528, 126, 16, 60 | Shaft | rear wall, MID | Top: aqueduct spout pouring down the wall (the DrainWell_Far reference). Middle: drain winch and chains (MID). Bottom: churned plunge pool, root mats. A side crack toward the A06 shortcut at y 172-176 | Lanterns, first mint roots |
| A03 Silt Walk | 1490, 102, 48, 12 | Enclosed low corridor (compression) | rear wall only | Low vault, silt banks, half-buried column capitals, root curtains, tide lines. **No vista, no far, no MID** | Dim; a few roots |
| A04 Cistern Court | 1552, 84, 64, 36 | Open chamber, the hub (release) | rear wall, MID, FAR | A vaulted reservoir hall: rows of columns going back into dark water. MID: the Keepers' gauge tower with carved depth marks (echoes the valve that lowers the water). FAR: the colonnade receding into darkness, lantern points on far arches | Lanterns + roots on columns |
| A05 Spillway Rise | 1628, 92, 16, 48 | Shaft | rear wall, MID | Left half: a stepped weir (MID) that matches the timed spill. Top: the spill head where the water starts. Right: dry masonry with old Keeper ladder irons | Spray-lit cool, a few lanterns |
| A06 Pump Gallery | 1554, 154, 60, 32 | Open chamber | rear wall, MID, FAR | Two great beam engines and bronze pipes. MID: a flywheel. FAR: engine bays receding through arches | Lanterns, verdigris sheen |
| A07 Keeper Refuge | 1628, 170, 42, 14 | Enclosed rest | rear wall only | The Keepers' quarters carved into the wall: alcoves, shelves, a bell, bedding niches. **Everything at 100%; nothing at MID** | The warmest room |
| A08 Sluice Guardian Arena | 1684, 168, 52, 28 | Enclosed arena | rear wall | A monumental closed sluice gate, 16 × 20 u, centred on the rear wall. Mint light leaks through its seams; the Heart is behind it. The fight floor stays visually quiet | Mint, strongest |

Passages: horizontal tunnels are 4 u tall (doors `width 4`), vertical ones 3-4 u wide. All are enclosed and use the tunnel kit, so the rear wall continues through them in the same light and grade. A tunnel looks darker because it is occluded, not because its material changes. Arch ribs every 2.5-4 u (vary the spacing) give motion something to read against.

**Water datum (corrected 7 Nov by Review 98):** two shared high-water marks (`CK_Band_Tide`, centred on these world heights) run through every connected space of their band. Lower band: **world y 109** across A03, the A03-A04 tunnel, A04, the A04-A05 tunnel and A05. Upper band: **world y 179** across A06, the A06-A07 tunnel, A07, the A07-A08 tunnel and A08. Optional local tide lines in A04 and A06 at their water surface +1 u and +2 u (A04 surface y 85, A06 surface y 154.6).

## 6. Files (all RGBA 8-bit sRGB PNG, 216 px/u)

Names: `CK_` = Cistern kit (rear wall), `FG_`, `MID_`, `SET_` = unique rear-wall set pieces, `FAR_`. Numbered variants are separate files (`_01`, `_02`…).

### Stage 49b: the A04 slice (the kit core plus everything A03's right end, A04, the A03-A04 and A04-A05 tunnels and A05's lower half need)

| File | u (w × h) | px | Count | Notes |
|---|---|---|---|---|
| CK_Wall_Masonry | 10 × 10 | 2160 × 2160 | 1 | Opaque. Seamless in both axes. Fitted wet limestone blocks |
| CK_Wall_Bedrock | 10 × 10 | 2160 × 2160 | 1 | Opaque. Seamless in both axes. Rough carved rock |
| CK_Band_Tide | 10 × 1.5 | 2160 × 324 | 1 | Soft alpha. Tiles horizontally. Mineral stain line |
| CK_Band_Silt | 10 × 3 | 2160 × 648 | 1 | Tiles horizontally; straight bottom, irregular top. Silt bank on the lower wall |
| CK_Rib_Tunnel | 1 × 5 | 216 × 1080 | 3 | Tunnel arch rib on the back wall: 4 u clear + 0.5 u tucked into the floor and ceiling |
| CK_Mouth_Arch | 3 × 6 | 648 × 1296 | 2 | The arch frame where a tunnel opens into a room: `_01` facing left, `_02` facing right |
| CK_Column | 1.5 × 12 | 324 × 2592 | 2 | Squared masonry with a capital. Base hides in water or silt, top in the vault |
| CK_Vault_Span | 16 × 4 | 3456 × 864 | 1 | Ceiling arch span; top edge tucks into the ceiling terrain |
| CK_Window_Arch | 6 × 9 | 1296 × 1944 | 1 | An opening onto darkness: the inside is a near-black gradient, not scenery |
| CK_Niche_Lantern | 2 × 3 | 432 × 648 | 3 | Wall niche with a Keeper lantern (warm) |
| CK_Pipe_H | 10 × 1 | 2160 × 216 | 1 | Tiles horizontally |
| CK_Pipe_V | 1 × 10 | 216 × 2160 | 1 | Tiles vertically |
| CK_Pipe_Elbow | 2 × 2 | 432 × 432 | 1 | Joins Pipe_H and Pipe_V exactly (pipe diameter and centre line match) |
| CK_Pipe_Valve | 2 × 2 | 432 × 432 | 1 | Same diameter as the pipes |
| CK_Roots_Curtain | 4 × 6 | 864 × 1296 | 3 | Hangs from the ceiling; mint-glowing tips |
| CK_Roots_Crawl | 8 × 3 | 1728 × 648 | 2 | Roots across the wall |
| CK_Chain_V | 0.5 × 10 | 108 × 2160 | 1 | Tiles vertically |
| CK_Water_Sheet_V | 2 × 10 | 432 × 2160 | 1 | Soft alpha. Tiles vertically. Falling water |
| CK_Mound | 6 × 2 | 1296 × 432 | 2 | Silt/rubble; straight bottom edge. Hides MID bases |
| CK_Glow_Mint | 4 × 4 | 864 × 864 | 1 | Soft alpha, additive glow |
| FG_Roots | 4 × 6 | 864 × 1296 | 2 | Near-black silhouettes from the ceiling |
| FG_Chain_Hook | 2 × 8 | 432 × 1728 | 1 | Near-black silhouette |
| MID_A04_GaugeTower | 5 × 13 | 1080 × 2808 | 1 | Gauge column with carved depth marks; base hidden by water/silt |
| MID_A05_WeirStair | 6 × 10 | 1296 × 2160 | 1 | Stepped weir, A05 left half |
| FAR_A04_Colonnade | 43 × 28 | 9288 × 6048 master | 6 tiles | Tiles `_r0c0`…`_r1c2`: 3 columns × 2 rows of **3096 × 3024**, row 0 at the bottom. Dark, deep, low contrast; detail falls away into black |

### Stage 49c: the rest of Route A

| File | u (w × h) | px | Count | Notes |
|---|---|---|---|---|
| CK_Rib_Shaft | 5 × 1 | 1080 × 216 | 2 | Rib for the vertical passages (A02-A03, A04-A09, A05-A06, A08-A10) |
| CK_Light_Beam | 4 × 10 | 864 × 2160 | 1 | Soft alpha, additive daylight (A01 only) |
| MID_A01_SluiceWinch | 6 × 5 | 1296 × 1080 | 1 | |
| MID_A02_DrainWinch | 6 × 6 | 1296 × 1296 | 1 | |
| MID_A06_Flywheel | 8 × 8 | 1728 × 1728 | 1 | |
| SET_A01_InletGate | 12 × 14 | 2592 × 3024 | 1 | Light-well grate above, inlet sluice gate |
| SET_A02_AqueductSpout | 16 × 12 | 3456 × 2592 | 1 | Top of the shaft, full room width |
| SET_A02_PlungePool | 16 × 8 | 3456 × 1728 | 1 | Bottom of the shaft |
| SET_A05_SpillHead | 16 × 10 | 3456 × 2160 | 1 | Top of the shaft |
| SET_A06_BeamEngine | 8 × 14 | 1728 × 3024 | 2 | Two different engines, not a mirrored pair |
| SET_A07_KeeperQuarters | 20 × 10 total | 2 tiles of 2160 × 2160 | 2 | `_L` and `_R`, cut from one 4320 × 2160 master |
| SET_A08_GreatSluice | 16 × 20 total | 4 tiles of 1728 × 2160 | 4 | 2 × 2 tiles `_r0c0`, `_r0c1`, `_r1c0`, `_r1c1` (row 0 at the bottom), cut from one 3456 × 4320 master. Seams of mint light |
| FAR_A06_EngineDepths | 41 × 26 | 8856 × 5616 master | 6 tiles | 3 × 2 tiles of **2952 × 2808**, named and ordered as FAR_A04 |

**Memory (projection, not a measurement):** about 123 MiB for 49b and 157 MiB for 49c, so **about 280 MiB for all of Route A** as BC7 with mipmaps at full size. About 48% of that is the two far sections. With Unity's texture quality one step down (globalTextureMipmapLimit = 1) on Series S and Switch 2, it's about 70 MiB. Every platform depends on Route A's textures loading per route, not for the whole underground at once. That's integration work, not yours. Just record the numbers.

## 7. Method

1. **Layout first** for every SET and FAR: a small full-composition layout, then native sections painted from it, with at least 25% overlap, blended. Archive the layouts. None of their pixels go in a master.
2. **Tiling kit pieces:** prove seamlessness with a 3 × 3 repeat. Then, for the walls, also a 40 × 20 u wall built from both materials plus kit overlays, to show it doesn't look like wallpaper.
3. **Cut tiles last**, from the finished master, exactly on the sizes above. Prove the tiles reassemble byte-identically to the master.
4. **Darkness by painting, not by tinting in game.** Paint each piece at its intended value. The game won't darken it.

## 8. Stages and what each delivers

### 49a: key frames (concepts only; nothing goes into Assets)

- Three key frames over the real terrain, at the trial's camera positions from `Tools/Design/CisternNetwork_20261107/Views/layout.json`: **A03** (1515, 106), **A04_middle** (1588, 99) and **A05_bottom** (1636, 101). Use your existing capture tooling for a terrain-and-Qori-only plate. Concept resolution is fine, labelled **CONCEPT**, never upscaled.
- A palette and value sheet, greyscale versions of the three frames, and the mystery check.
- **Stop.** Claude reviews; the user says go.

### 49b: the A04 slice

- Every 49b file in section 6, with the QA from section 7.
- Copies in `Assets/Art/Prototypes/KeepersCistern_Batch49/` only. Import: RGBA Compressed BC7 on Standalone, PS5 and GameCore Xbox; mipmaps on; bilinear; Clamp, or Repeat only on a tiling kit piece's tiling axis. Record the Switch 2 setting as "not set" unless you have its module documentation.
- A new mode in your persistent Editor preview, **Keepers' Cistern**, for A03's right end, A04, both tunnels and A05's lower half, at the section 3 motions. No scene edits, isolated save, fully reversible. **F8 switches the rear-wall kit and FG on and off, with MID and FAR unchanged.** This tests whether the near layers fix the stationary glide.
- Captures at 3840 × 2160 and 1920 × 1080: the three key-frame positions, a glide across A04 (5 frames over 2 s), a walk through the A03-A04 tunnel (5 frames), and A05's lower half; each with F8 on and off.
- A seam sheet: every FAR tile join at 100%, and every kit repeat.
- **Stop.** Claude reviews; the user plays it.

### 49c: the rest

- The 49c files, the same QA, and the preview extended to all of A01-A08 and their passages (A09, A10 and the Heart stay as they are). Captures for every room, including shafts at top, middle and bottom.
- **Stop.**

## 9. Delivery folder (every stage): `Batch49\`

- The PNGs, named exactly as section 6.
- `MANIFEST.json`: per file, sha256, px, u, PPU 216, layer, motion, pivot, tiling axes, own verdict.
- `QA_INDEX.html` and `QA_SHEET.md`: every check above with numbers, the seams, the greyscale and the mystery views.
- `sources\<file>\`: every native output, section, layout and mask, with exact prompts.
- A preservation receipt: accepted files byte-identical before and after, plus the name-check log.
- `REVIEW_HANDOFF.md`: your verdict per file, anything ambiguous (written down, not guessed), and a platform section that keeps source checks, imported-memory measurements and device tests ("not tested") separate. Re-check the next free CLAUDE_REVIEW number, but don't write a review.

Don't commit or push. Don't add anything to the accepted manifest. Don't change the scenes, `layouts.py`, colliders, the camera, the old trial's files, or any accepted art.
