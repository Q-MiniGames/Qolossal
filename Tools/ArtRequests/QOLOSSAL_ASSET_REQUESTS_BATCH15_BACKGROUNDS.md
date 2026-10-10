# Qolossal: asset request for Codex, Batch 15: the remaining backgrounds

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

Batch 14 (Review 25) put the Cradle, the Summit and Qvale into Direction A, with real depth. This batch finishes the pre-reveal climb: the three regions still on the earlier cool grey-blue "misty ruins" sets, plus the Review 25 touch-ups.

| Region | Assets | Today |
|---|---|---|
| MR02 the Long Causeway | 3 new | `BG_A1_Mid/Near` under `BG_Sky_A1`, plus the non-repeating far painting `Aqueduct.png`, which shows a hard rectangle edge across most of the region |
| MR05 the Ribwood | 3 new | `BG_A2_Mid/Near` under `BG_Sky_A2`, plus `AncientGrove.png` (same edge problem) and a 10% haze veil |
| MR06 the Windward Heights | 3 new | `BG_A3_Mid/Near` under `BG_Sky_A3`, plus `FallsSanctuary.png` (same edge problem) |
| Touch-ups from Review 25 | 5 replacements | `BG_Cradle_Far/Mid/Near`, `BG_Summit_Near`, `BG_Qvale_VaultRim_Mid` |

All three new sets are wired exactly like the Cradle, Terraces and Summit sets (`MraWorldBuilder.DepthSets`), so these planes replace each region's sky, far painting, Mid and Near in one go. Stop once, at the end, for **Review 26**.

## Notes
- **Folder:** `Batch15/` for QA, handoffs and manifests.
- **Read first:**
  - `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md` (issues #1, #2, #6, #8; Option C) and its `proposal/` sheets: `current_MR02/05/06_game.jpg` and `_bgonly.jpg`, `ref_quake_vistas.jpg`;
  - `Tools/Backgrounds/depth_prototype/` (the depth parallax these planes are made for);
  - `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_25.md` (the Batch 14 verdicts and the touch-ups) and `CLAUDE_REVIEW_23.md` (the Terraces set: the structure to follow);
  - `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2, sections 2, 4, 5, 10–11).
- **Model to match:** the accepted Batch 14 sets (`BG_Cradle_*`, `BG_Summit_*`) for structure, edge quality, haze and detail. They are what the game's other depth regions look like, so these three must sit beside them as one world.
- **Direction A, Morning Herbarium:** warm apricot dawn, limestone, olive greens, soft hazed distance.
- The accepted manifest holds **1140** files. Re-read `Tools/ArtImport/codex_v2_accepted.json` rather than trusting this count.

## The rules (unchanged)
- Keep all accepted files byte-identical (except the five touch-ups, which replace their own accepted file only after Review 26), and never write under `Assets/`.
- Use deterministic post-processing. Archive every native source and the **exact prompt, seed and settings** for each, in `Batch15/sources/<name>/`.
- Fill in the Batch 6 self-review checklist per workstream.
- Run the **global name check** (case-insensitive, against accepted names, accepted sub-sprite names and every `Assets/` stem) before delivering.
  - I checked the nine new names on 7 Oct: all free. Re-check before you deliver. The five touch-ups keep their accepted names on purpose.
- **No text** in any image.
- **The mystery rule (strict; these are the body's forearm, chest and shoulder):**
  - Backgrounds show sky, weather and the distant valley. The body appears only as the ground Qori walks on.
  - No silhouette, skyline, ridge set, rock mass or arrangement may read as a hand, fingers, knuckles, nails, wrist, forearm, tendons, knee, lap, chest, ribs, sternum, collarbone, shoulder, neck, face, brow, eye, lid or lashes.
  - Specifically:
    - no five parallel rounded ridges, and no regular series of parallel walls, arches or ribs;
    - no pale rounded caps at ridge ends;
    - no smooth rounded dome hills;
    - no symmetric pairs;
    - no long straight cords or tendon-like ridges running the length of a plane.
  - Add a **mystery check** line per asset in the QA sheet: "Viewed at 10% size and blurred: any body read? no/yes."
- **The platform section in each handoff:** keep source checks, imported-resource notes and device tests (all **not tested**) separate.

## Shared specification (the nine new layers)

Identical to Batch 14's full-frame sets. In short:

- **Canvas:** **2560×1440**, 144 PPU, bottom-centre pivot, Repeat U / Clamp V, RGBA 8-bit sRGB. Drawn 10 u tall in game.
- **One painting split by depth:** the Far, Mid and Near share one registered canvas; stacked with no offset they make the whole intended view.
- **No sky.** The game uses the accepted `BG_Sky_Terraces` (apricot dawn) behind them.
- **Transparency:** fully transparent above each layer's skyline; no semi-transparent sky wash; clean alpha (no halo over black or white); opaque below the skyline to the canvas bottom, with **no holes**.
- **Overscan:** the lowest 144 px (1 u) is opaque ground continuing the painting. Keep important content above the lowest 360 px.
- **Seamless wrap:** mean RGBA difference between column 0 and column 2559 = **0**, the adjacent-column derivative matches, and the skyline row is equal at x=0 and x=2559, ±2 px. Blend a wrap band; no mirroring, no smear.
- **Depth:** Far 25, Mid 10, Near 5 (the terrain is 1). On screen they move at 0.04, 0.10 and 0.20 of the terrain's speed and zoom differently at lookouts, so **each plane must stand on its own at any offset**:
  - nothing (road, aqueduct, river, wall, cloud bank) continues from one plane into another;
  - where something passes behind a nearer plane, it ends behind a natural occluder painted in the nearer plane;
  - every skyline is a natural silhouette, never a cut through a building, an arch or a tree;
  - every plane is painted fully under where nearer planes usually cover it.
- **Edges (Review 25's lesson):** tree crowns, krummholz, reeds and grass end in natural leaf, needle and blade edges. **No stair-stepped or column-block mattes.** Report the count of skyline jumps of 12 px or more between adjacent columns for every layer; the target is under 5, each one explained (a cliff face, a trunk).
- **Light and palette:** low warm light from the right. Far layers hazed toward the sky's apricot-cream, with cool lavender shadow only, never midday blue. Near layers carry the most contrast but stay hazed and less saturated than the region's gameplay terrain, and clearly separate from it in value, so the walk line, Qori and enemies read in front. The terrains differ: the Causeway's and the Heights' are pale limestone (keep the Near a value step below and cooler in shadow), the Ribwood's is dark bark (keep the Near a step lighter). Check against the region's matte.
- **Detail:** painterly, about equal to `BG_Cradle_Mid` (horizontal luminance gradient ~7 over opaque pixels, measured by `Tools/Backgrounds/measure_backgrounds.py`). Paint at native size or downscale from a larger native; never upscale.
- **Proofs, at 1920×1080 and 1280×720:**
  - the set stacked over `BG_Sky_Terraces` at offsets 0, 640 and 1280 px;
  - the depth-offset proof: neighbouring planes offset by 0, 320 and 640 px, and ±48 px vertically. Nothing may break, double, or show a hole;
  - a 3× horizontal repeat of each layer, seam columns marked, with the edge-difference numbers;
  - over-black and over-white alpha checks;
  - **the assembly proof with gameplay:** the set behind the region's in-engine foreground, using the new RGBA mattes in `Tools/Backgrounds/proposal/mattes/` (`MR02_mid`, `MR05_mid`, `MR06_mid`, plus `MR06_encounter` and `MR05_low`; terrain, props and Qori over transparency, 1920×1080). Qori must read at least as clearly as in the current capture.
- **Platform notes (for the handoff, not the art):** the game imports these with mipmaps, high-quality compression and per-platform maximum sizes chosen from measurement (planned: 2560 on PC/PS5/Series X, 2048 on Series S and Switch 2). **Not tested** on any device.
- **Acceptance (shared):** the wrap, skyline and edge numbers above; alpha clean; no body read; Direction A light beside the region's Quake vista and beside the Batch 14 sets; Qori contrast no worse than today; all 1140 accepted files byte-identical.

---

## W1: The Long Causeway (MR02), three layers (P1)

**Problem:**
- The second region, where Qori meets the first people. Its backgrounds are the A1 strips and the `Aqueduct.png` far painting under the cool `BG_Sky_A1`: grey-blue, and the far painting's edge cuts across the sky for 58% of the region.
- The approved `Quake_LongCauseway` shows an old road and aqueduct climbing a long warm slope at dawn.

**Mystery, critical here:** the Causeway is the forearm. The aqueduct and the road are **ground and props**, not background lines. In the background planes:
- no long straight aqueduct, road or ridge running the full width of a plane (it reads as a tendon);
- aqueduct arches only as short broken fragments, irregularly spaced, never a regular row;
- slopes break into varied spurs and gullies, never parallel rounded folds.

**Match:** `Quake_LongCauseway` for light, palette and vegetation; the accepted Causeway kit (`Causeway_Pale_Cliff`, `Prop_Causeway_Aqueduct_Arch_Broken`) for stone colour; the Batch 14 sets for structure.

### `BG_Causeway_Far`
- **Layer:** order -105, depth 25.
- **Content:** a wide dawn valley far below and beyond: hazed limestone ranges, terraced slopes, a river, one far **hilltop village** (40–70 px tall) off-centre near x 900.
- **Composition:** skyline at rows 540–640, gently varied; no peak more than 260 px above the skyline median.

### `BG_Causeway_Mid`
- **Layer:** order -90, depth 10.
- **Content:** the slope country the road climbs through: olive groves and cypress, dry-stone field walls at irregular angles, **two short broken aqueduct fragments** (2–4 arches each, different heights, far apart), a roadside shrine or milestone.
- **Composition:** skyline at rows 640–780; two or three clear masses with hazy gaps between them.

### `BG_Causeway_Near`
- **Layer:** order -80, depth 5.
- **Content:** the near roadside below the route: wild fennel, broom, rock-rose, olive shrubs, tumbled dressed stones from the old road, a broken channel block or two.
- **Composition:** skyline at rows 820–960, broken by two or three shrub or tree crowns rising to row ~720; the top 120 px of content stays sparse where the walk line usually falls (screen 55–65%).

## W2: The Ribwood (MR05), three layers (P1)

**Problem:**
- The A2 strips and `AncientGrove.png` under `BG_Sky_A2`: misty grey-green ruins, a 10% haze veil, and the far painting's hard edge for 60% of the region.
- The approved `Quake_Ribwood` shows a warm olive forest between great stone walls, with dawn light slanting through.

**Mystery, critical here:** the Ribwood is the chest. Its "huge parallel stone walls" are the ribs, so **the background must not show them as a series**:
- stone walls appear at most as one or two irregular, broken masses per plane, at different heights and angles, with forest between them;
- no repeated vertical slabs, curved bands or arches at a regular spacing;
- no slow rounded swelling skyline (the "swell" is terrain-only).

**Match:** `Quake_Ribwood` for light, palette and vegetation (light only for the walls: never copy their rhythm); the Batch 14 sets for structure.

### `BG_Ribwood_Far`
- **Layer:** order -105, depth 25.
- **Content:** the forest canopy rolling away under dawn haze, with irregular limestone outcrops breaking it, and distant hazed hills beyond. Shafts of warm light through mist may be painted in, softly.
- **Composition:** skyline at rows 520–640, an irregular canopy line.

### `BG_Ribwood_Mid`
- **Layer:** order -90, depth 10.
- **Content:** the forest interior at middle distance: old olive and holm-oak trunks, ivy, ferns, mist between the trunks, **one** large broken stone wall mass, mossy and leaning.
- **Composition:** skyline at rows 600–760 (canopy). Trunks at irregular spacing and lean; no picket rhythm.

### `BG_Ribwood_Near`
- **Layer:** order -80, depth 5.
- **Content:** the near forest floor: ferns, roots, fallen leaves, mossy stones, a fallen trunk, low shrubs.
- **Composition:** skyline at rows 820–960, with two or three fern or shrub masses rising to row ~720. Keep it a step lighter than the dark bark walkable kit (check against `MR05_mid`).

## W3: The Windward Heights (MR06), three layers (P1)

**Problem:**
- The A3 strips and `FallsSanctuary.png` under `BG_Sky_A3`: cool and grey, and the far painting's hard edge for 62% of the region.
- The approved `Quake_WindwardHeights` shows bare, windy limestone ridges and falls on the climb toward the summit.
- It must lead into the Summit's accepted set (`BG_Summit_*`): slightly lower altitude, the same warm limestone and dawn.

**Mystery, critical here:** the Heights are the shoulder and collarbone. In the background planes:
- no single long, smooth, gently curving ridge across a plane (a collarbone);
- no broad rounded shoulder dome;
- ridges are jagged, broken and varied in height, with no parallel repeats.

**Match:** `Quake_WindwardHeights` for light, palette and vegetation; `BG_Summit_Far/Mid` for how the climb ends; the Batch 14 sets for structure.

### `BG_Heights_Far`
- **Layer:** order -105, depth 25.
- **Content:** high ranges in dawn haze, cloud forming in the valleys below (lower and thinner than the Summit's cloud sea), far valley farmland through the gaps.
- **Composition:** skyline at rows 520–620.

### `BG_Heights_Mid`
- **Layer:** order -90, depth 10.
- **Content:** wind-bare limestone ridges and crags, **two or three** thin falls at different heights (never a symmetric pair), wind-bent pines, grassy ledges, blown mist.
- **Composition:** skyline at rows 600–780.

### `BG_Heights_Near`
- **Layer:** order -80, depth 5.
- **Content:** the near wind-blown margin: feathergrass and thyme leaning one way, broken limestone blocks, a few stunted pines.
- **Composition:** skyline at rows 840–980, grass clumps rising to row ~740 in two or three loose clumps.

## W4: Review 25 touch-ups (same names, same canvases, swap in place)

1. **`BG_Cradle_Far`:** two dark smear fringes on the skyline, at about x 1925 and x 2325, rows 585–620 (dark wedges on the sunlit side of two peaks). Repaint those edges in the ridge's own light. Change nothing else.
2. **`BG_Cradle_Near`** and **`BG_Summit_Near`:** the tree crowns and the krummholz have stair-stepped, rectangular silhouettes. The matte was cut in column blocks: 49 and 39 skyline jumps of 12 px or more. Re-cut the crowns from the archived native panels so they end in natural leaf and needle edges. Keep the skyline ends and the seam unchanged.
3. **`BG_Cradle_Mid`** and **`BG_Qvale_VaultRim_Mid`** have a few of the same steps (14 and 9). Fix them while you're there.

For each: the same canvas size, registration, wrap and skyline ends as the accepted file; report the source checks, the before/after jump counts, a pixel-difference map against the accepted file (changes only near the fixed edges), and native 1080/720 crops of each fixed edge.

---

## Delivery checklist
- Nine new PNGs and five touch-up PNGs, plus `Batch15/MANIFEST.json` (sha256, size, PPU, pivot, wrap).
- `Batch15/sources/` with each native output, prompt, seed and settings.
- The QA sheet: edge, skyline and jump numbers, alpha checks, mystery checks and all the proofs above.
- The global name check log.
- A preservation receipt for all 1140 accepted files (the five touch-ups' accepted versions untouched in the project).
- `REVIEW26_HANDOFF.md` with the platform section: source checks, imported-resource notes, and devices **not tested**.
