# Qolossal: asset request for Codex, Batch 14: backgrounds (DRAFT: not sent)

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

This is Option B of `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md`: eight background paintings for the three places where placement and import fixes can't reach the approved look.

| Region | Assets | Why placement can't fix it |
|---|---|---|
| MR01 the Cradle (first region) | 3 | Its only layers are the earlier cool grey "misty ruins" set |
| MR07 the Summit (final level) | 3 | Its layers are dark slate with blue spiky peaks |
| MR04 Qvale (current level) | 2 | Nothing in its layers shows the town's own basin, and its overlook has no view |

Everything else in the proposal (far-painting coverage, wiring the accepted Terraces set, mill scale, the Qvale dawn sky, framing, haze, imports) is code and placement and isn't requested here.

Work the three workstreams in parallel, and stop once, at the end, for **Review 25**.

## Notes
- **Folder:** `Batch14/` for QA, handoffs and manifests.
- **Read first:**
  - `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md`, with its `proposal/` sheets: current captures, mockups M1–M5, and `ref_quake_vistas.jpg`;
  - `CLAUDE_REVIEW_23.md` (the accepted seamless Terraces set: the model to follow);
  - `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2, sections 2, 4, 5, 10–11).
- **Direction A, Morning Herbarium:** warm apricot dawn, limestone, olive greens, soft hazed distance, olive survey ink.
- **Distant valley towns stay** wherever the approved compositions have them (your decision).
- The art manifest holds **1132** accepted files. Re-read `Tools/ArtImport/codex_v2_accepted.json` rather than trusting this count.

## The rules (unchanged)
- Keep all accepted files byte-identical, and never write under `Assets/`.
- Use deterministic post-processing. Archive every native source (the generator's original output, before any crop, scale or alpha cut) and the **exact prompt, seed and settings** for each, in `Batch14/sources/<name>/`.
- Fill in the Batch 6 self-review checklist per workstream.
- Run the **global name check** (case-insensitive, against accepted names, accepted sub-sprite names and every `Assets/` stem) before delivering.
  - I checked these eight names on 6 Oct against 8,039 names: all free. Re-check before you deliver.
- **No text** in any image.
- **The mystery rule (strict for this batch):**
  - Backgrounds show sky, weather and the distant valley. The body appears only as the ground Qori walks on.
  - No silhouette, skyline, ridge set, rock mass or arrangement may read as a hand, fingers, knuckles, nails, knee, lap, chest, ribs, shoulder, face, brow, eye, lid or lashes.
  - Specifically:
    - no five parallel rounded ridges;
    - no pale rounded caps at ridge ends;
    - no smooth rounded dome hills (the unused `KneeTerraces` fails this);
    - no symmetric pairs;
    - no lake-under-a-brow arrangements.
  - Add a **mystery check** line per asset in the QA sheet: "Viewed at 10% size and blurred: any body read? no/yes."
- **The platform section in each handoff:** keep source checks, imported-resource notes and device tests (all **not tested**) separate.

## Shared specification (all eight)

- **Density and canvas:**
  - 144 px per world unit: 1:1 at 2560×1440 output, 0.75× at 1080p (mipmapped), 1.5× at 4K;
  - the full-frame layers are 10 u tall in game: **2560×1440**;
  - each size is justified in its entry.
- **Registration:**
  - bottom-centre pivot, 144 PPU;
  - Repeat U / Clamp V for the repeating layers; Clamp/Clamp for the vista;
  - RGBA PNG, 8-bit, sRGB.
- **The three-layer sets (Far, Mid, Near) are one painting split by depth:**
  - **Register them on the same 2560×1440 canvas**, like the accepted Terraces set, so that stacked with no offset they make the whole intended view.
  - The game draws them at one shared scale and moves them together.
- **Transparency:**
  - every layer is fully transparent (alpha 0) above its own skyline;
  - no semi-transparent sky wash;
  - clean alpha edges with no dark or light halo (check over black and over white);
  - each layer is opaque below its skyline down to the canvas bottom: **no holes** where a farther layer would show through unintentionally.
- **Overscan:**
  - the lowest **144 px (1 u)** of every layer is opaque ground continuing the painting: the climb sink and the lift can show it;
  - keep important content above the lowest 360 px: gameplay terrain covers the lower ~35% of the frame.
- **Seamless wrap (repeating layers):**
  - the left and right edges must match: **mean RGBA difference between column 0 and column 2559 = 0**, and the adjacent-column derivative must also match (Review 23's measure);
  - the skyline (first opaque row) must be equal at x=0 and x=2559, ±2 px;
  - blend a wrap band; no mirroring and no smear.
- **Depth parallax (new, 6 Oct; being prototyped in the Terraces):**
  - The game now gives each plane a real depth: Far 25, Mid 10, Near 5 (the terrain is 1).
  - So on screen, as the camera travels, the Far moves 0.04, the Mid 0.10 and the Near 0.20 of the terrain's speed. The planes **slide against each other** by up to tens of units across a region.
  - Each plane also zooms like a camera pulling back at lookouts (size 5 to 7): the Far keeps almost its full size on screen, while the Near shrinks about 7%.
  - So, although the planes share one registered canvas, **each must stand on its own at any offset**:
    - no road, river, wall, terrace line or cloud bank may continue from one plane into another;
    - where something passes behind a nearer plane, it ends behind a natural occluder painted in the nearer plane (a tree mass, a crest, a crag), not at an exact pixel line;
    - every skyline is a natural silhouette (a tree line, a ridge, a crag edge), never a cut through a building or a tree;
    - every plane is painted fully under where the nearer planes usually cover it: no holes, no "it's hidden anyway" blanks.
  - **Proof:** the stack at relative offsets of 0, 320 and 640 px between neighbouring planes, plus vertical offsets of ±48 px. Nothing may break, double, or show a hole.
- **Light and palette:**
  - the sky is **not** delivered: the game uses the accepted `BG_Sky_Terraces` (apricot dawn) behind these;
  - the light comes from low and warm, from the right;
  - Far layers are hazed toward the sky's apricot-cream with cool lavender shadow only, never midday blue (Review 22's note on the first Terraces Far);
  - the Near layers carry the most contrast, but stay a step lighter and less saturated than the gameplay terrain, so Qori and enemies read in front.
- **Detail:**
  - painterly, matching the accepted Terraces set and `BG_Qvale_Near`;
  - detail density about equal to `BG_Terraces_Mid` (horizontal luminance gradient ~7 over opaque pixels, measured by `Tools/Backgrounds/measure_backgrounds.py`);
  - no noise-sharpening, no upscaled generator output: paint at native size, or downscale from a larger native.
- **Proofs, at 1920×1080 and 1280×720:**
  - the set stacked over `BG_Sky_Terraces` at three horizontal offsets (0, 640, 1280 px);
  - a 3× horizontal repeat of each repeating layer, with the seam columns marked and the edge-difference numbers;
  - an over-black and over-white alpha check;
  - the depth-offset proof above;
  - an **assembly proof with gameplay**: the set behind the region's current in-engine frame. Use the RGBA foreground mattes in `Tools/Backgrounds/proposal/mattes/` (`MR01_mid`, `MR07_mid`, `MR04_mid`, `MR04_overlook_seated`: terrain, props and Qori over transparency, 1920×1080), with Qori at the mid beat.
- **Platform notes (for the handoff, not the art):**
  - the game imports these with mipmaps, high-quality compression and per-platform maximum sizes chosen from measurement;
  - planned: 2560 on PC/PS5/Series X, 2048 on Series S and Switch 2;
  - **not tested** on any device.
- **Acceptance criteria (shared):**
  - the wrap and skyline numbers above;
  - alpha clean;
  - no body read;
  - Direction A light side by side with the region's Quake vista;
  - in the assembly proof, Qori/background contrast is no worse than the current frame;
  - all 1132 accepted files byte-identical.

---

## W1: The Cradle (MR01), three layers (P1)

**Problem:**
- The Cradle is the game's first region. Its backgrounds are the earlier A0 set: a cool grey-blue misty valley with stone arches (`MistyValley_Background_v1`, `BG_A0_Mid/Near`).
- The approved `Quake_Cradle` shows a warm limestone gorge country at dawn: almond blossom, olive terraces, cypress, mist in the gorges, a far hilltop village.
- See `proposal/current_MR01_game.jpg` and `ref_quake_vistas.jpg`.

**Match:**
- `Quake_Cradle` for light, palette and vegetation;
- the accepted `BG_Terraces_Far/Mid/Near` for layer structure, edge quality and detail;
- `BG_Sky_Terraces` as the sky.

### `BG_Cradle_Far`
- **Region:** MR01.
- **Layer:** order -105, at 10 u tall; horizontal 0.9 with the set; climb sink 0.6 u.
- **Content:**
  - distant limestone ranges and gorges under morning mist;
  - one far **hilltop village** (a cluster of pale houses and a bell tower, about 40–70 px tall on the canvas), placed off-centre, about x 1650;
  - haze heavy toward the horizon.
- **Composition:** skyline at rows 560–640 (first opaque row), gently varied, never a flat line; no peak taller than 260 px above the skyline median.
- **Size:** 2560×1440. The full frame is 10 u at 144 PPU, 1:1 at 1440p.
- **Acceptance:** shared criteria. The village is readable at 720p as a village (not a texture); no five-ridge rhythm anywhere.

### `BG_Cradle_Mid`
- **Region:** MR01.
- **Layer:** order -90, at 10 u tall, with the set.
- **Content:**
  - the middle valley: a winding gorge with mist in its floor;
  - olive and almond terraces on the slopes, cypress lines;
  - one ruined shepherd's hut and a dry-stone wall.
- **Composition:** skyline at rows 640–760; content reads in two or three clear masses with soft mist gaps between them, keeping the Cradle's quiet space.
- **Size:** 2560×1440 (shared canvas).
- **Acceptance:** shared criteria. Its detail gradient is between the Far's and the Near's.

### `BG_Cradle_Near`
- **Region:** MR01.
- **Layer:** order -80, at 10 u tall, with the set.
- **Content:**
  - the near hillside below the route: wild thyme, white rock-rose, almond trees in blossom, broken limestone outcrops (not ridges), a path fragment;
  - darker olive shadow at its base.
- **Composition:**
  - skyline at rows 820–960, broken by two or three tree crowns rising to row ~700 (to frame, not to wall in);
  - keep its top 120 px of content sparse where the walk line usually falls (screen 55–65%).
- **Size:** 2560×1440 (shared canvas).
- **Acceptance:** shared criteria; Qori contrast check at the MR01 mid-beat proof.

## W2: The Summit (MR07), three layers (P1)

**Problem:**
- The Summit is the last level before the reveal. Its backgrounds are the A6 set: dark slate rock, crystal clumps and blue spiky peaks under an apricot sky.
- That contradicts the approved Summit (the warm limestone high country of `Quake_Summit`), and as `current_MR07_game.jpg` shows, it reads as another world.

**Mystery, critical here:**
- The Summit's lake, reed bed and waterfall shelves are the brow, lashes and lid at the reveal (`Reveal_Face_01_Brow`). **They belong to the walkable terrain, not to these background planes.**
- These layers show only what lies beyond and below the summit:
  - distant ranges and the cloud sea far below;
  - limestone crags and falls dropping away;
  - pines;
  - reed and wildflower margins at the Near's foot.
- **Never** an oval lake, a curved reed line over water, a brow-like overhang, or two of anything side by side.

**Match:** `Quake_Summit` for light and palette only; `Reveal_Face_01_Brow` for which features to **avoid**; the Terraces set for structure.

### `BG_Summit_Far`
- **Region:** MR07.
- **Layer:** order -105, at 10 u tall, with the set.
- **Content:**
  - high distant ranges, snow-dusted peaks catching the dawn;
  - a sea of cloud in the valleys below, with far, faint valley farmland through gaps;
  - optionally one tiny far village far below.
- **Composition:** skyline at rows 520–620; cloud sea occupying rows 700–1000.
- **Size:** 2560×1440 (shared canvas).
- **Acceptance:** shared criteria; it reads as high altitude (cloud below the viewer).

### `BG_Summit_Mid`
- **Region:** MR07.
- **Layer:** order -90, at 10 u tall, with the set.
- **Content:**
  - the summit's own country falling away: pale limestone crags, two or three thin falls dropping into the cloud, stunted pines, ledges with grass;
  - warm rim light on the crags.
- **Composition:** skyline at rows 600–780; falls never in a symmetric pair.
- **Size:** 2560×1440 (shared canvas).
- **Acceptance:** shared criteria plus the mystery check at 10% size.

### `BG_Summit_Near`
- **Region:** MR07.
- **Layer:** order -80, at 10 u tall, with the set.
- **Content:**
  - the near margin: tufts of reeds and feathergrass, wildflowers, broken limestone blocks, a few wind-bent pines;
  - no open water.
- **Composition:** skyline at rows 840–980; reed tufts may rise to row ~720 in two or three loose clumps (not a continuous line).
- **Size:** 2560×1440 (shared canvas).
- **Acceptance:** shared criteria; Qori contrast check at the MR07 mid-beat proof.

## W3: Qvale (MR04), two layers (P1)

**Problem:**
- Behind the town's street and its listening overlook there are only generic rolling olive hills (`BG_Qvale_Far/Mid/Near`; kept).
- The approved Qvale ("Lantern Vaults": homes tucked under curved overhangs of warm rock in a sheltered basin, `Quake_Qvale`) never appears behind the town.
- The overlook, framed "toward the open valley", shows the same repeating strip as the street.
- See `current_MR04_game.jpg` and mockups M2 and M3.

**Match:** `Quake_Qvale`; the accepted Qvale home exteriors (`Town_Qvale_HomeA/B/C_Closed`, `Town_Qvale_OldMan_Closed`) for architecture and lantern colour; the accepted `BG_Qvale_Near` for palette.

### `BG_Qvale_VaultRim_Mid`
- **Region:** MR04.
- **Layer:** a new repeating layer at order -100, between `BG_Qvale_Far` and `BG_Qvale_Mid`; 5 u tall; horizontal 0.9 with the others; climb sink 0.6 u.
- **Content:**
  - the far side of the sheltered basin: low curved limestone overhangs with small homes tucked beneath them (round doors, lantern-lit windows, hanging herbs);
  - terraced gardens, a stair, a few cypress and olive trees;
  - warm lantern points against the dawn shade.
- **Composition:**
  - homes about 40–90 px tall on the canvas, so they read as distant;
  - skyline (the overhang tops) at rows 120–260, gently undulating;
  - repeating, so no single dominant building.
- **Size:** **2560×720**. That's 17.8 × 5 u at 144 PPU; 5 u is the band behind the street's horizon in M2 (screen 37–50% from the top).
- **Wrap:** seamless (shared criteria).
- **Acceptance:**
  - shared criteria;
  - at 1280×720 the homes read as homes;
  - lantern points stay small (no bloom baked in);
  - **no** overhang shape that reads as a lap, thighs or a body: keep the overhangs irregular and natural.

### `BG_Qvale_Overlook_Vista`
- **Region:** MR04: the listening-tree overlook, beyond `MR04_N09`.
- **Layer:**
  - non-repeating, Clamp/Clamp, order -78;
  - shown only in the overlook's vista zone and while seated, cross-faded in over 1.4 s as the seated camera eases out (frame size 6.2);
  - horizontal 0.95, fixed vertical, anchored bottom-centre at the bench's x;
  - the remaining Qvale layers fade under it.
- **Content:** the view down from the town:
  - the basin rim in the foreground corners;
  - below, the Terraces (stepped farmland on slopes, olive rows, the mill as a small far shape) and the river winding through the valley;
  - the line of the Long Causeway's aqueduct far off;
  - a far hilltop village;
  - the morning cloud breaking.
- **Mystery:** the Terraces must read as stepped farmland on ordinary slopes; **no rounded dome hill** (the reason `KneeTerraces` is excluded).
- **Composition:**
  - the eye path runs from the bench (bottom centre, kept clear: rows 1100–1440 at x 1600–2500 are gentle ground only) out to the valley;
  - the strongest light is on the river bend at about x 2700, row 820;
  - leave the top 35% as calm sky-coloured haze for the listening UI.
- **Size:** **4096×1440**. That's 28.4 × 10 u at 144 PPU. Seated at size 6.2 the frame is 22.0 × 12.4 u, and the layer scales with the view (×1.24): 35.3 × 12.4 u. That covers the frame width, plus the ±3 u of camera travel inside the zone, plus a 1.5 u edge fade each side.
- **Acceptance:**
  - shared criteria (except wrap);
  - no hard edge visible at the seated framing;
  - at 1920×1080 seated, the river, mill and village are all readable;
  - proofs: seated framing at 1920×1080 and 1280×720 with the tree, bench and Qori from the matte `MR04_overlook_seated`.

---

## Delivery checklist
- Eight PNGs plus `Batch14/MANIFEST.json` (sha256, size, PPU, pivot, wrap).
- `Batch14/sources/` with each native output, prompt, seed and settings.
- The QA sheet with the edge and skyline numbers, alpha checks, mystery checks and all the proofs above.
- The global name check log.
- A preservation receipt for all 1132 accepted files.
- `REVIEW25_HANDOFF.md` with the platform section: source checks, imported-resource notes, and devices **not tested**.
