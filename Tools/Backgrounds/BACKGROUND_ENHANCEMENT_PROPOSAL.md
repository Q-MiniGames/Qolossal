# Background enhancement proposal (6 Oct 2026)

An audit of the in-game backgrounds of all eight Mountain Relief Atlas regions, with a plan to improve them. It's a proposal: no scene, setting or accepted file was changed. All captures are real in-engine renders (scratch copy, Windows editor, batch mode) unless labelled as a mockup.

**Current level.** The last scene open in your editor was **MR04 Qvale**, so Qvale gets particular attention. MR03 Terraces is close behind, because its accepted Direction A set is still waiting to be wired in.

**Recommendation: Option B.** Do the Option A fixes immediately (code and placement only), and send a small targeted art request (8 assets, `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH14_BACKGROUNDS.md`, a draft, not sent) for:
- the two regions whose backgrounds most contradict their approved identity: the Cradle (the game's first impression) and the Summit (its final level);
- Qvale's two scenic gaps.

## 1. What was looked at

| Evidence | File (all under `Tools/Backgrounds/`) |
|---|---|
| Per-region contact sheets: lowest and highest beats, the middle beat, the steepest climb, both camera boundaries, lookout zones at their own zoom, the Qvale overlook's seated framing, an encounter. 1920×1080 renders; 1280×720 for key spots. | `proposal/current_MR0x_game.jpg` |
| The same spots with props and characters hidden (the generated terrain still draws) | `proposal/current_MR0x_bgonly.jpg` |
| Each region's strips over its own sky, at source | `proposal/ref_current_strips.jpg` |
| The approved Direction A regional compositions (the accepted Quake vistas) | `proposal/ref_quake_vistas.jpg` |
| Accepted backgrounds not used in game | `proposal/ref_unused_accepted.jpg` |
| Native-pixel crops of the same scene at 720p, 1080p, 1440p and 2160p | `proposal/resolution_MR0x_native_crops.jpg` |
| Readability around Qori at 1080p and 720p, 1:1 | `proposal/readability_native_1080_vs_720.jpg` |
| Every background's source size, alpha, skyline, wrap, detail and import settings | `BACKGROUND_SOURCES.json` (`measure_backgrounds.py`) |
| Every live layer's world size, magnification at 720p–2160p, horizontal coverage, and screen position at each spot | `BACKGROUND_LAYERS.json` (`MraBackgroundAudit.Run`) |
| Mockups, current versus proposed | `proposal/M1`…`M5_*_compare.jpg`, plus `_proposed_1080.jpg` / `_720.jpg` |

**Tools added:**
- `Assets/MountainReliefAtlas/Editor/MraBackgroundAudit.cs`: an editor-only capture tool; it opens scenes and never saves them.
- `Tools/Backgrounds/measure_backgrounds.py`, `make_sheets.py`, `make_mockups.py`.

**How the live backgrounds are built** (`MraWorldBuilder.Background`, `ParallaxLayer`):
- **Sky:** camera-locked, 24 u tall.
- **Far:** either a non-repeating far painting at 20 u (Cradle, Causeway, Ribwood, Heights), a hazed smaller copy of the Mid at 4.4 u, or `BG_Qvale_Far`.
- **Mid and Near:** at 7.2 and 7.0 u.
- **Motion:** every valley layer moves at one horizontal rate (0.9) and one climb sink (0.6 u). This was your 5 Oct feedback: separate rates made the layers obvious.
- **Haze:** a camera veil in MR05, MR07 and MR08.
- **Zoom:** layers scale with the camera from size 5 (lookouts at 7, the overlook at 6.2).

## 2. Prioritised issues, with evidence

Each issue is marked by cause: **S** = source art, **P** = placement/camera/code, **I** = import.

| # | Issue | Where | Cause | Evidence |
|---|---|---|---|---|
| 1 | **A hard rectangle edge cuts across the sky.** The far painting doesn't repeat, but it moves at the shared 0.9 rate, so it slides 0.1 u per unit of camera travel. At 35.5 u wide it fills the frame for only **38–53%** of each region's camera range (Cradle 53%, Causeway 42%, Ribwood 40%, Heights 38%). Everywhere else its edge shows as a vertical cut, and its top edge shows as a horizontal one. | MR01, MR02, MR05, MR06 | P | `current_MR01_game.jpg` (mid, high, both edges), `MR05` (low, mid, high), `MR06` (low, west, encounter); `BACKGROUND_LAYERS.json` `full_cover_camera_x` |
| 2 | **Most regions aren't in Direction A.** Cradle, Causeway, Ribwood and Heights use the earlier cool grey-blue "misty ruins" strips under cool skies; the Summit uses dark slate with spiky blue peaks. Their own accepted Quake vistas show the approved look: an apricot dawn, limestone, olive groves, layered hazy valleys and hilltop villages. The Summit is the worst case: its approved identity (lake, reed beds, limestone falls) doesn't appear at all. | MR01, 02, 05, 06, **07** | S | `ref_current_strips.jpg` against `ref_quake_vistas.jpg`; `current_MR07_game.jpg` |
| 3 | **The accepted Direction A Terraces set is unused.** `BG_Terraces_Far/Mid/Near` (seamless, Review 23) aren't wired in. MR03 still shows the older A5 strips plus a hazed copy of the A5 Mid as its far ridge: a duller, browner, less detailed valley. | MR03 | P | `current_MR03_game.jpg`; mockup M1 |
| 4 | **Mill landmark at the wrong scale and depth.** It's drawn 5.5 u tall: larger than gameplay cliffs. At the mid beat it appears to stand on the playable cliff top, and it overlaps it. | MR03 | P | `current_MR03_game.jpg` (mid, high, climb) |
| 5 | **Qvale's backgrounds don't show Qvale.** Behind the street and the listening overlook there are generic rolling olive hills under a cool teal sky (`BG_Sky_A5`). The approved "Lantern Vaults" identity (homes under warm limestone overhangs, lantern light in a sheltered basin) never appears behind the town. The overlook was framed toward "the open valley", but shows the same repeating strip as the street, so the scenic payoff is lost. | **MR04** | S + P | `current_MR04_game.jpg` (all spots), `current_MR04_bgonly.jpg` (vista0, overlook_seated) |
| 6 | **Too much plain fill.** With the camera 1 u above Qori, flat ground puts **35–40%** of the frame below the walk line as plain fill. The road surface in Qvale and the Causeway is the flattest; the background gets ~60%. | All; worst in MR02 and MR04 | P | every `current_*_game.jpg` |
| 7 | **Soft at 1440p and 4K; upscaled even at 1080p.** The Mid/Near strips are 576 rows drawn 7.2/7.0 u tall: **1.35× at 1080p, 1.8× at 1440p, 2.7× at 2160p**. The far paintings (941 rows at 20 u) are **2.3× at 1080p and 4.6× at 2160p**. The sky is 2.4× (harmless for clouds). Detail per output pixel falls in proportion to resolution (MR04 crop: 11.9 → 8.3 → 6.4 → 4.4 at 720/1080/1440/2160), so above 1080p the strips have no detail left to show. | All | S (resolution) + P (scale) | `resolution_MR0x_native_crops.jpg`; `BACKGROUND_LAYERS.json` `mag_*` |
| 8 | **Weak separation for Qori.** His olive-green cloak sits on olive-green valley mid-tones in MR04, MR02 and MR03, so the separation comes only from outline and value. MR01/MR05's grey haze separates him well. MR07's dark slate background makes the pale Qori pop, but in the wrong palette. | MR02, MR03, MR04 | S + P | `readability_native_1080_vs_720.jpg` |
| 9 | **No parallax between layers, by design.** It was the right fix for "the layers are obvious". The cost is that depth now comes only from the painting and from the terrain moving in front. The Terraces set (one coherent painting split into three planes) is the art that can take subtle separation back without that problem. | All | P | Polish handoff R2.3 |
| 10 | **Mild repetition.** A strip repeats every 25.6 u of layer movement, about every 256 u of camera travel at the 0.9 rate. It's barely visible in normal play, but on long Qvale and Heights stretches the same hill crowns come back. | MR04, MR06 | S | contact sheets (same hills in vista0, overlook) |
| 11 | **The far ridge is minified 0.83× with no mipmaps.** Its fine detail shimmers slightly when scrolling at 720p. | MR03, MR07, MR08 | I | `BACKGROUND_SOURCES.json` (`mips 0`) |

**Ruled out:**
- **Compression.** The strips are `CompressedHQ` (BC7 on Windows, 8 bpp) and look like a bilinear upscale of the source in the 2160p crops; no block artefacts are visible. `MistyValley_Background_v1` alone uses normal compression (BC3); it's fine at its haze level.
- **Seams.** All Mid/Near/Far strips measure a left/right edge difference of exactly 0. The skies don't (5–24) but never repeat on screen (camera-locked, 42.7 u wide against a 17.8 u view).
- **Vertical drift.** The climb progression holds each skyline at a fixed height on screen at every spot (`BACKGROUND_LAYERS.json` per-spot rows): no drifting horizons.

**Quiet space worth keeping:** the open sky above the Terraces and Summit, and the misty gaps in the Cradle. The fixes below add depth, not clutter.

## 3. Options

Ranked by benefit, effort, cost and art need.

| | **A. Polish with accepted assets** | **B. A + a targeted art request (recommended)** | **C. Strongest treatment** |
|---|---|---|---|
| **What** | A1–A8 below. Code and placement only. | A, plus 8 new layers (§6): Direction A sets for the Cradle and the Summit, and Qvale's vault rim and overlook vista. | B, plus Direction A sets for the Causeway, Ribwood and Heights. Each region gets a fourth depth plane (foreground framing silhouettes) and a landmark; restrained ambient animation; a camera-framing pass. |
| **Visible gain** | Large in MR03 and MR04; fixes the worst defect (#1) everywhere. | Large in the three most important places: first region, current level, final level. | Largest: every pre-reveal region matches its Quake vista. |
| **Effort** | ~1–2 days | A, plus one Codex batch (8 assets, about 1 review round), plus ~1 day of wiring | B, plus ~3 Codex batches (~20 more assets), plus ~4–5 days |
| **Runtime cost** | Per region ~6 → ~9 MB background texture (MR03); no new draw calls except MR04's overlook layer | +~11–17 MB per upgraded region (2560×1440 BC7 with mips); +2 draw calls in MR04 | ~25–30 MB per region; +2–3 draw calls; small CPU for UV-scroll animation |
| **Risk** | Low | Low–medium (art consistency across batches) | Medium (scope; foreground framing must never hide gameplay) |

**Why B:**
- Option A alone can't fix the biggest art-direction problem (#2): five regions in the wrong palette. A tint can only darken or veil a painting; it can't turn grey jungle ruins into olive limestone at dawn.
- Option C repaints three regions (Causeway, Ribwood, Heights) whose current sets are at least coherent and atmospheric. That's worth doing later, but it isn't the next most visible step.
- B spends the art budget where players look longest and judge first: the opening Cradle, the town hub you're working on now, and the final climb before the reveal.

### Option A: immediate changes, no new art

Each is independent; order by value.

1. **A1. Far-painting coverage (issue #1).** Exempt non-repeating far paintings from the shared 0.9 rate: give them their own rate of 0.985 (their original value), recentred per region.
   - At 0.985 a 35.5 u painting fills the frame across ±593 u of camera travel, so 100% of every region's camera range.
   - Better still, add a soft 1.5 u alpha fade on its left and right edges in `ParallaxLayer` (a shader-free UV/vertex fade on the quad), so no edge can ever cut hard, at any zoom.
   - Mockup: `M4_MR01_Cradle_mid_compare.jpg`.
2. **A2. Wire the Terraces set into MR03 (issue #3).** `BG_Sky_Terraces` + `BG_Terraces_Far/Mid/Near` stacked as one painting at one shared scale: 10 u tall, which is **1:1 at 1080p**.
   - Lift the Far and Mid 1.2 u so the valley floor and river show above the walk line; the Near keeps the frame bottom.
   - Retire the A5 strips and the hazed far ridge in MR03.
   - Mockup: `M1_MR03_Terraces_mid_compare.jpg`.
3. **A3. Mill landmark (issue #4).** Draw it 2.8 u tall (from 5.5), seated on the far hill behind the Near, hazed 15–20% toward the sky, sorted between the Far and Mid. It then reads as distant, not as a gameplay object (M1).
4. **A4. Qvale's sky (issue #5, first half).** Use the accepted `BG_Sky_Terraces` dawn sky for MR04 instead of `BG_Sky_A5`'s teal, and tint `BG_Qvale_Far` toward the dawn haze.
   - The Qvale strips are already warm olive, so they sit well under it.
   - This matches the approved `Quake_Qvale` light (M2's sky shows it).
5. **A5. Qvale overlook (issue #5, second half).** While seated, or in the overlook's vista zone, cross-fade to `BG_Terraces_Far` + `Mid` as the view. Narratively, Qori looks back down at the Terraces he has just climbed; Qvale sits directly above them.
   - It's a stand-in until `BG_Qvale_Overlook_Vista` arrives (M3 shows the idea; the placeholder is this same reuse).
6. **A6. Street framing (issue #6).** On flat street and causeway stretches with no drop ahead, raise the camera's vertical offset from 1.0 to about 2.0 u (eased, inside existing CameraLockZone/VistaZone logic). The walk line moves from ~38% to ~29% of frame height.
   - Keep the current offset wherever a drop or climb is near, so landing visibility isn't reduced.
   - This is a gameplay-camera change and needs your play-test.
7. **A7. Qori separation (issue #8).** A depth-haze band behind the gameplay plane: a sky-coloured vertical gradient (the existing haze-veil technique), 8–12%, strongest just behind the walk line and clear at the top.
   - It lifts the valley mid-tones behind Qori by one value step in the olive regions (MR02, MR03, MR04) without touching any painting.
   - Accept only if Qori/background luminance contrast improves (criterion in §5).
8. **A8. Imports (issue #11).**
   - Turn mipmaps on (bilinear) for any layer drawn below 1:1: the far ridges at 0.83×, and the new 1440p-density layers at 1080p and 720p.
   - Move `MistyValley_Background_v1` to `CompressedHQ` like the others.
   - These are import-settings changes only; source bytes and accepted import registration (PPU, pivot, wrap) are unchanged.

**Not used, on purpose:**
- `KneeTerraces` reads plainly as a knee (mystery rule).
- `BG_Transition_A0`–`A5` are framed arch views painted opaque; they suit region-arrival stills, not tiling layers.
- `BG_A0_Near_v2` is a near-identical alternate of `BG_A0_Near`.

### Option B: the targeted art (details in §6 and the request)

| Asset | Region | Solves |
|---|---|---|
| `BG_Cradle_Far`, `BG_Cradle_Mid`, `BG_Cradle_Near` | MR01 | #2: the first region in Direction A, matching `Quake_Cradle` (limestone gorge, almond blossom, olive terraces, hilltop village far off). The sky reuses `BG_Sky_Terraces`. Replaces the far painting and the A0 strips. |
| `BG_Summit_Far`, `BG_Summit_Mid`, `BG_Summit_Near` | MR07 | #2: the Summit's approved high-country dawn, matching the light and palette of `Quake_Summit`: distant ranges over a cloud sea, limestone crags and falls dropping away, reed and wildflower margins. The sky reuses `BG_Sky_Terraces`. **The lake and reed beds stay in the walkable terrain** (the open terrain request): background planes must never carry the face's features (mystery rule 2). |
| `BG_Qvale_VaultRim_Mid` | MR04 | #5: the far rim of the sheltered basin, with homes under warm limestone overhangs and lantern glow, matching `Quake_Qvale` and the accepted Qvale homes (M2). |
| `BG_Qvale_Overlook_Vista` | MR04 | #5: one composed, non-repeating view from the listening tree: the basin rim, the Terraces and the river far below, the Causeway's line and a far hilltop village (M3). |

### Option C: additional ideas (not recommended now)

- Direction A sets for the Causeway (aqueduct country at dawn), Ribwood (olive forest between stone walls; the "swell" stays terrain-only) and Heights (wind-bare limestone ridges, falls).
- A foreground framing plane per region:
  - hanging olive branches or reeds at the top and bottom corners, follow 1.15, 10–20% of the frame, kept out of the middle 60% of the height;
  - shown only where the route is flat, never during combat.
- Restrained animation:
  - sky cloud drift (UV scroll ~0.02 u/s);
  - one haze band that breathes;
  - distant bird flocks (existing `AmbientMotes` pattern);
  - waterfall shimmer in the Heights.
- Lighting accents: a low sun glow sprite at the horizon for the dawn regions; lantern bloom points on Qvale's vault rim.
- Subtle layer separation brought back for Direction A sets only: Far 0.93, Mid 0.9, Near 0.88. The sets are painted as one picture, so a 0.02–0.05 spread reads as depth, not as sliding layers. It needs your A/B test, given the 5 Oct feedback.

## 4. Layer arrangement and camera/parallax behaviour (recommended)

| Order | Layer | Source | Height at size 5 | Horizontal | Vertical | Notes |
|---|---|---|---|---|---|---|
| -110 | Sky | region sky (dawn: `BG_Sky_Terraces`) | 24 u | camera-locked | camera-locked | Unchanged |
| -106 | Landmark (optional) | e.g. the mill, at 2.8 u | 2–3 u | 0.9, placed per beat | climb sink | Hazed 15–20% |
| -105 | Far painting / Far | new Far, or the legacy far painting | 10 u (sets) / 20 u (legacy) | 0.9 (sets), **0.985 (legacy paintings)** | climb sink 0.6 u | Edge fade 1.5 u on non-repeating layers |
| -100 | Vault rim (MR04) | `BG_Qvale_VaultRim_Mid` | 5 u | 0.9 | climb sink | Between Far and Mid |
| -90 | Mid | new Mid / existing | 10 u (sets) / 7.2 u | 0.9 | climb sink | |
| -85 | Depth-haze band | generated gradient | frame | camera-locked | camera-locked | 8–12% sky colour, strongest just behind the walk line |
| -80 | Near | new Near / existing | 10 u (sets) / 7.0 u | 0.9 | climb sink | |
| -78 | Vista (MR04 overlook only) | `BG_Qvale_Overlook_Vista` (A5 stand-in: Terraces Far+Mid) | 10 u | 0.95, non-repeating | fixed | Cross-faded in the overlook's vista zone |

- The camera keeps its zoom behaviour: layers scale with the view from size 5.
- On flat street/causeway stretches, offset y is about 2.0 u (A6).
- Sets painted as one picture stay registered: Far, Mid and Near share one scale and one bottom anchor, offset only by the lift.

## 5. Resolution, import and platform recommendations

**Where the softness comes from:**
1. **Magnification (the main cause):**
   - Mid/Near: 1.35× at 1080p, 2.7× at 4K.
   - Far paintings: 2.3× at 1080p, 4.6× at 4K.
2. **The source itself:** the A-series Mids are painted soft. The detail gradient is 1–2 for A0–A3 Mid against 7–10 for the Nears and 7.1 for `BG_Terraces_Mid`.
3. **Not compression, filtering or wrap** (§2).

Upscaling the existing PNGs can't add the missing detail. Only new masters at the needed density can.

**Required source rows** for a layer drawn h u tall at size 5:
- 1:1 at 1080p: **108 × h**;
- 1:1 at 1440p: **144 × h**.

**Overscan:**
- **Vertical:** the climb sink (0.6 u), a lift (≤1.2 u) and lookout zooms (composedForSize keeps the screen ratio, so no extra source is needed). Paint about 1 u of opaque ground below the lowest visible line; that's the "overscan bottom" in the request.
- **Horizontal:** seamless wrap, so no overscan is needed for repeating layers.
- **Non-repeating vista:** the frame width at its zoom (6.2 → 22 u), plus its camera travel inside the zone (~±3 u at 0.95), plus fades. That gives **≥ 28 u**.

**Recommended sizes for new layers:**

| Layer | Display | Master (px) | PPU | 1080p | 1440p | 2160p |
|---|---|---|---|---|---|---|
| Far/Mid/Near (full-frame set) | 10 u | **2560×1440** | 144 | 0.75× (mips) | 1.0× | 1.5× |
| Vault rim strip | 5 u | **2560×720** | 144 | 0.75× | 1.0× | 1.5× |
| Overlook vista (non-repeating) | 10 u at size 5 (12.4 u seated) | **4096×1440** | 144 | 0.75× | 1.0× | 1.5× |

- **Why 1440p density and not 4K:** 1440p is the highest common PC and console output where the layers can be 1:1. At 4K the hazed background layers tolerate 1.5×, while gameplay terrain stays sharp at its own 120 PPU. A 4K-density master (2.0× the memory per axis) would cost 4× the memory for detail that atmospheric haze mostly hides.
- **Existing assets:** keep their accepted 120 PPU masters and import registration. The accepted Terraces set (1920×1080, 120 PPU) at 10 u is exactly 1:1 at 1080p and 1.33× at 1440p: fine for A2.

**Import settings:**
- New layers:
  - BC7-class high-quality compression on PC/PS5/Xbox (Unity `CompressedHQ`);
  - **mipmaps on** (they draw below 1:1 at 1080p and 720p);
  - bilinear filtering;
  - Repeat U / Clamp V;
  - bottom-centre pivot;
  - no crunch.
- Per-platform max sizes (build profiles, from measurement):
  - Windows, PS5, Series X: full 2560;
  - **Series S**: 2048 (output is usually ≤1440p; measure);
  - **Switch 2**: 2048 (1080p docked; the handheld screen is 1080p). Use the format its module documents. Don't invent a format.
- Existing 2048×576 strips:
  - No change, except mipmaps on the far ridges (A8).
  - Don't upscale.
  - Don't raise max size (it would do nothing: they're 2048 already).

**Texture memory**, background textures resident for the loaded region (BC7 = 1 byte/px, +33% with mips):

| Region | Now | Option A | Option B |
|---|---|---|---|
| MR01 Cradle | 6.0 MB | 6.0 MB | ~16.8 MB (3 × 4.9 + sky 2.1) |
| MR03 Terraces | 5.0 MB | 8.9 MB | 8.9 MB |
| MR04 Qvale | 5.6 MB | 5.6 MB (+5.4 MB for the stand-in vista) | ~13.6 MB (+ rim 2.5 + vista 6.1) |
| MR07 Summit | 4.4 MB | 4.4 MB | ~16.8 MB |

- All are well inside any target's budget, one region at a time.
- **Measure peak memory on device**; not tested.

**Rendering cost:**
- **Today:** about 5–6 transparent full-width quads per frame (each with a below-band) plus one veil. Option B adds 1–2 quads in MR04 and nothing elsewhere.
- **Last clean Windows player measurement** (Round 1, focused, 1920×1080, RTX 3070 Ti): median 0.56–0.64 ms, max 8.8 ms per region. The Round 2 numbers were contaminated by window focus. Rerun focused after the changes.

| Target | 60 FPS with these changes |
|---|---|
| Windows (1920×1080) | to be re-measured |
| Windows 2560×1440 / 3840×2160 | not tested |
| PS5 | not tested |
| Xbox Series X | not tested |
| Xbox Series S | not tested |
| Switch 2 docked | not tested |
| Switch 2 handheld | not tested |

**Readability at 1280×720** (`readability_native_1080_vs_720.jpg`):
- Qori stays legible (about 60 px tall).
- Backgrounds lose fine foliage detail, but keep their value structure.
- Haze matters more at 720p: the olive-on-olive regions (MR02/03/04) are where A7's haze band helps most.

## 6. Acceptance criteria (measurable)

Re-run `MraBackgroundAudit.Run` and the polish checks after each step.

1. **Coverage:** every non-repeating background layer covers 100% of its region's camera x range (`full_cover_camera_x` share = 1.0), or fades to 0 within 1.5 u of its edge. No hard edge is visible in any audit spot.
2. **Scale:**
   - new layers draw at ≤1.05× at 1080p and ≤1.05× at 1440p (`mag_1080`, `mag_1440`);
   - the Terraces set at 1.0× at 1080p;
   - no layer above 1.6× at 1440p, except the camera-locked sky.
3. **Seams:** left/right edge difference 0 and skyline rows equal at x=0 and x=w−1 (±2 px) for every repeating layer, measured by `measure_backgrounds.py`.
4. **Direction A:** each upgraded region's audit sheet, reviewed side by side with its Quake vista (the reference for light and palette, not for content placement):
   - apricot-dawn light;
   - limestone and olive palette;
   - hazed distance;
   - no body read (mystery checklist: no fingers, knees, faces or anatomy in any silhouette before MR07's reveal).
5. **Composition:**
   - skylines hold their screen height at low and high spots within ±3% of frame height (already true);
   - on flat street stretches, the walk line sits at 27–32% of frame height (A6).
6. **Separation:** at every audit spot, the mean luminance contrast between Qori's silhouette and a 0.6 u ring of background around it is ≥ 1.4:1. It must be no worse than today in any region. The baseline isn't measured yet: add the measurement to `MraBackgroundAudit` (Qori's matte against the background-only render) before the first change.
7. **Mill:** never overlaps playable terrain in any audit spot; drawn ≤3 u tall.
8. **Preservation:** all 1132 accepted files remain byte-identical (`verify_preservation.py`); new assets accepted only through the manifest.
9. **Performance:**
   - Windows development player, focused, 1920×1080: median frame within +0.2 ms of the pre-change run; 0 frames over 16.7 ms on the audit route;
   - background texture memory ≤ 32 MB per region;
   - every console row reported, as "not tested" until hardware runs.
10. **Tests:** full play test, polish checks and audio checks all pass with 0 failures.

## 7. Implementation order

| Step | What | Needs art | Effort |
|---|---|---|---|
| 1 | A1 far-painting coverage and edge fade | no | 0.5 day |
| 2 | A2 + A3 Terraces set and mill | no | 0.5 day |
| 3 | A4 + A5 Qvale dawn sky and overlook stand-in | no | 0.5 day |
| 4 | A7 depth haze band; A8 imports | no | 0.5 day |
| 5 | A6 street framing (needs your play-test) | no | 0.5 day |
| 6 | Send Batch 14 (8 assets); review; import | **yes** | 1 review round |
| 7 | Wire the Cradle and Summit sets, Qvale rim and vista; re-audit | after 6 | 1 day |

**Largest visible difference in the current level (Qvale):**
1. **The dawn sky (A4)** turns the cool teal band into the approved apricot morning.
2. **The overlook view (A5 now, the vista asset later)** gives the listening tree the open-valley payoff it was framed for.
3. **The vault rim** (Batch 14) finally puts Qvale itself behind Qvale.

**Across the game,** the single biggest fix is **A1**, which removes the hard sky rectangles from four regions.
