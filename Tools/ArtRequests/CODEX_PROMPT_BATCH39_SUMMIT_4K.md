REGION: MR07 (Batch 39)

Qolossal: Codex, native 4K background planes for the ONE region named on the REGION line above. Do only that region. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch<NN>\, where NN is the batch number on the REGION line.

## 0. Before you start (all must hold, or stop and say which failed)
- The REGION line names exactly one region from the table in section 3, and the batch number matches it.
- The Batch<NN> folder does not exist yet or is empty. If it has content, stop; never overwrite a delivery.
- The five output names for this region (section 3) are free in the global name check: case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem.
- Re-read Tools\ArtImport\codex_v2_accepted.json and record its count. Don't trust any number in this prompt.

## 1. Read first, in this order
1. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_86.md: why Batch 28d passed. Then REVIEW_77, 78, 80 and 85: what failed before, and why. Don't repeat any of those failures (section 6 lists them).
2. Your own Batch28d package (Codex_v2_full\Batch28d\): its scripts, layout guides, skyline-mask method, wrap method, QA_INDEX.html and climb proofs. **Reuse that pipeline.** Change only the region's content.
3. The in-game Terraces stack: Assets\Art\Codex\Backgrounds\Terraces4K\ (the five accepted planes) and Assets\MountainReliefAtlas\Editor\MraTerraces4K.cs (how the game places and moves them). It is the quality, viewpoint and registration reference for every region.
4. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (handheld and docked); the original Switch is excluded. Master art is 4K. Don't make platform variants. Mark every device test "not tested".
5. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md: section 2 (the mystery rules) and the region's row in the regions table.
6. The region's current accepted background set and sky (section 3, "Look reference"): for palette, light and mood only. **Don't copy their compositions. These are new paintings.**

## 2. What every region gets: five planes (RGBA 8-bit sRGB, 216 px per world unit, bottom-centre pivot, Repeat U / Clamp V)
The game's frame is 3840x2160 (17.8 x 10 world units). As Qori climbs, each plane slides down the screen at the rate shown, and repeats left to right moving at 1 - 1/depth of the camera.

| Plane | File | Size | Depth | How the game moves it | What it is |
|---|---|---|---|---|---|
| Sky | BG_<Name>_Sky_4K | 3840x3240 | locked to the camera | doesn't move | A soft gradient wash for the region's light, warmer low and paler high. No clouds that read as objects, no land, no horizon, no sun disc. Opaque. |
| Far | BG_<Name>_Far_4K | 3840x2160 | 25 | half real speed; sinks at most 6 u | Distant ranges, seen from the side. The highest peak tip at least 120 px below the top edge; the skyline within the top 25%. Hazed, cool, low contrast, getting calmer toward the bottom. Land continues to the bottom edge. |
| High | BG_<Name>_High_4K | 3840x2160 | 25 | as the Far; fades in over the last 30% of the climb | The view from above for this region: cloud sea, mist or weather below the viewer, with only far peak tips through it. Alpha reaches exactly 0 over its lowest 30%. Its mist colour matches the Far's lower mist (median RGB within 6 per channel). |
| Mid | BG_<Name>_Mid_4K | 3840x2700 | 10 | 0.35 of real speed; skyline starts at 62% of the frame's height from the bottom | A side-on valley wall or ridge for this region. Its stepped skyline sits in the top 700 px; the median skyline row is between 300 and 400. Opaque below the skyline. Softer than the Near, warmer than the Far. |
| Near | BG_<Name>_Near_4K | per section 3 | 5 | real speed (or the rate in section 3); starts with its bottom edge at the frame's bottom | Side-on features that pass by as you climb, at many heights, **in the region's own shape language (section 3, "Near shapes")**. Freestanding vertical columns or spires belong to the Terraces and the Cradle only: in every other region, a near feature with nearly parallel sides more than 4 times taller than wide fails. **Nothing floats:** every feature is grounded, either continuing down out of the canvas's bottom edge or standing on a rock spur, slope or cliff mass that does; an overhang or eave hangs from a cliff mass rising from below. The plane repeats left to right, so it has no frame sides to lean on. **At least 50% of every 3840x2160 window, at any height, is fully transparent (alpha 0).** No window is empty and none is a wall. The nearest and warmest plane, with contrast at about 70% of the gameplay terrain's. |

<Name> is the region's file name in section 3 (for example BG_Cradle_Near_4K).

## 3. The regions

| Region | <Name> | Climb (u) | Near size | Near rate | Look reference (palette and light only) | The region in one line | Region rules and Near shapes |
|---|---|---|---|---|---|---|---|
| MR01 | Cradle | 28 | 3840x2160 | 1.0 | BG_Cradle_Far_v2 / Mid_v2 / Near_v2, BG_Sky_Terraces | Where Qori sprouts: sheltered limestone gorges and pale cliffs, green and gentle, apricot dawn. | No five parallel ridges or gorges anywhere (it's the hand). Near shapes: gorge-wall ends and limestone spires, with ferns and small trees (done in Batch 34). |
| MR02 | Causeway | 158 | 3840x7528 | 1.0 | BG_Causeway_Far / Mid / Near, BG_Sky_Terraces | An old road and aqueduct climbing a long slope, the first people. | Near shapes: **an arcade, not pillars.** Runs of two to four connected aqueduct arches stepping up the slope at different heights, broken spans with a fallen arch, the old road on its retaining walls and stairs. Horizontal and diagonal masonry leads; every pier carries an arch. No long straight limb-like ridge. |
| MR04 | Qvale | 22 | 3840x2160 | 1.0 | BG_Qvale_Far / Mid / Near, BG_Qvale_VaultRim_Mid_v2, BG_Sky_A4 | A warm sheltered basin, homes under curved overhangs of warm rock, terraced farms. | No lap-like rounded basin rims or symmetric pairs. **Keep the accepted overlook vista: don't repaint it.** Near shapes: curved rock eaves and overhangs on grounded cliff masses that rise from the canvas bottom, with homes, lanterns and washing lines under them, and small terraced garden ledges. No columns. |
| MR05 | Ribwood | 180 | 3840x8192 | 0.963 | BG_Ribwood_Far / Mid / Near, BG_Sky_Terraces | A forest growing between huge stone walls. | **No parallel walls or ribs in any background plane.** Vary every wall's angle, height and spacing. Near shapes: great trees with trunks leaning at different angles (never parallel, never evenly spaced), spreading roots, broad canopies and branches crossing overhead, with broken stone between them. |
| MR06 | Heights | 216 | 3840x8192 | 0.803 | BG_Heights_Far / Near, BG_Sky_Terraces | Bare, windy ridges, the climb toward the summit. | **Flying enemies must read against the upper frame** (Reviews 26-28 cut the Heights' Mid for hiding them). Mid: only a low ridge whose skyline stays below 40% of the frame's height at all times. Paint it so; I set the placement in game. Near shapes: low and sparse: bare rock outcrops, wind-bent pines leaning one way, and cairns, mostly in the bottom 40% of every window, with nothing in the top 40% except thin tips. No columns. |
| MR07 | Summit | 154 | 3840x7356 | 1.0 | BG_Summit_Far / Mid / Near_v3, BG_Sky_Terraces | Cliffs and falls above everything, the final climb. | **The lake and reed beds are terrain; keep them out of the background.** No face, brow, eye, lid or lash shapes anywhere, including in the falls and cliffs. Near shapes: grounded cliff masses rising from the canvas bottom, with overhanging slabs, thin falls with spray, ledges with hardy shrubs. No columns. |
| MR08 | Descent | 151 | 3840x7224 | 1.0 | BG_A4_Mid / Near, BG_Sky_A4 | After the reveal, the long way down toward the heart. | Before writing a line, read the region's current scene and design notes. If it is interior or underground rather than open sky, stop and say so in the handoff: it may belong to the underground batches (30-33). Near shapes: if it is open sky, descending ledges and slabs on grounded rock masses, never columns. |

Mystery rule for every plane of every region: backgrounds show sky, weather and distant land only. Nothing may read as a hand, finger, knuckle, nail, knee, lap, chest, rib, shoulder, face, brow, eye, lid or lash: no five parallel rounded ridges, no rounded dome hills, no symmetric pairs. Check at full size, at 10% and blurred.

## 4. Method (the Batch 28d pipeline, required)
1. **Layout first.** Make a small full-width layout of each plane (for example 960 px wide) designed to wrap. Paint every section from its window of the layout so all sections agree on distance, scale and light. The layout is a guide only: none of its pixels enter a master. Archive it.
2. **Native pixels only.** Never upscale, stretch, resample up or duplicate rows. Build each canvas from 1:1 native sections with at least 25% overlap, painted outward from real neighbouring pixels and blended across the overlap. Show every join.
3. **Paint opaque, then mask with one drawn silhouette.** Never use the generator's alpha. Draw one full-resolution silhouette per plane (Far and Mid: one skyline curve; Near: the outline of every feature). Alpha is 255 inside and 0 outside, with at most a 2 px anti-aliased edge. Decontaminate edge colour against the fill. No translucent land; nothing opaque in the clear areas.
4. **Cyclic wrap.** The last column section continues from the first column's left edge. At every row of the final master: mean RGBA edge difference 0, derivative matched, skyline ends within ±2 px.
5. **Depth sets the look.** Far: soft, hazed, low contrast, calmer toward the bottom. Report the luminance-gradient mean per 540 px band; it must not rise toward the bottom. Mid: readable but softer than Near. Near: the sharpest and warmest.
6. **Side view only.** Every plane is a side elevation, like the 28d planes. No aerial or top-down ground, no paths or fields seen from above, no ground that fills the frame like a map.
7. **Variety.** No repeated motif or rhythm: vary sizes, angles and spacing. Fewer than three Near features per 3840 px width reads as sparse, more than six as a wall; aim for four or five, each different.

## 5. Gates, in this order. If one fails after a fair second attempt, stop there and deliver what you have with the numbers.
1. **Far gate:** size; alpha (top row all 0; partial alpha only within 2 px of the skyline); wrap 0 at every row; haze bands; highest peak at least 120 px below the top; mystery passed.
2. **Near gate:** size; every 2160 px window, stepped by 540 px, at least 50% fully transparent and never empty; every feature grounded (mark each feature's support in the window sheet); wrap 0; clean edges over black, white and magenta; mystery passed per window.
3. **Mid, High, Sky:** the rules in section 2; High's mist colour matches the Far.
4. **The stack in the climb:** the climb strip at 0, 15, 30, 50, 75 and 100% of the region's climb, placed exactly as section 2 says, with and without the region's gameplay matte (Tools\Backgrounds\proposal\mattes\<REGION>_mid_foreground_rgba.png; MR06 also MR06_encounter; MR08 has none: use MR07's and say so). Qori, the ledges and the route read clearly at every step. The sky shows at every step. Something from the Near or Mid is in view until at least 50%, and the High is seen at 100%.

## 6. Failures from earlier batches. Each one is an automatic fail.
- The generator's alpha used as the mask: dark or black sky blocks, translucent land (28, 28b).
- Wrap edges that don't meet (28b).
- A busy, sharp far plane that doesn't read as distant (28b).
- Aerial or map-like ground, or an opaque near wall that fills the frame and hides the sky (28c).
- Repeated diagonal terrace bands, or one motif repeated (28b, 28c).
- Freestanding columns or spires as the near plane outside the Terraces and the Cradle (35): every region needs its own near shapes.
- Floating near features: arches, rocks or homes hanging in the sky with nothing under them (35b, 36).
- A Mid colder or busier than the region's mood: Qvale is warm and sheltered (36).
- Any upscaling, or a placeholder delivered under a production name.
- Writing under C:\UnityProjects\Qolossal\Assets, or changing any accepted file.

## 7. Deliverables (in Batch<NN>\)
- The five PNGs, named exactly as in section 2 with the region's <Name>.
- MANIFEST.json: sha256, size, PPU 216, pivot, wrap, depth, rate, travel, initial placement, own verdict per plane.
- QA_INDEX.html and QA_SHEET.md: every gate's numbers, the climb strips, all joins, window sheets, alpha checks, 3x repeats, and mystery views.
- sources\<file>\: every native output, section, layout and mask, with exact prompts and settings.
- A preservation receipt (all accepted files byte-identical before and after) and the name-check log.
- REVIEW_HANDOFF.md: your verdict per plane, anything ambiguous (written down rather than guessed), and a platform section that keeps source checks, imported-resource estimates (RGBA8 MiB per plane) and device tests ("not tested") separate. Re-check the next free CLAUDE_REVIEW number, but don't write a review.
