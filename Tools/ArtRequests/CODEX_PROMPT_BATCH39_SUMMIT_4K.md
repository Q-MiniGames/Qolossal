ON HOLD, 3 November 2026: DON'T SEND. The same prompt produced the Heights art the user rejected. Rule from the user: art comes first, vertical parallax must not cost the art, and the long columns style isn't acceptable any more. Rewrite this prompt before any new Summit painting.

REGION: MR07 (Batch 39)

Qolossal: Codex, make the Summit's background right in the game, end to end. You own the whole job: paint it, install it, place it, tune its vertical parallax, check it in Unity, repair what fails and repeat until it passes. Nobody reviews your work before the user plays it, so don't stop for a review and don't hand back something that only looks right as flat art. Do only this region.

Project: C:\UnityProjects\Qolossal. You may write in it, within section 7.
Working folder for sources and evidence: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch39\.

## 0. Before you start (all must hold, or stop and say which failed)
- The working folder doesn't exist yet or is empty. If it has content, stop; never overwrite a delivery.
- No Unity editor has this project open. The PC is shared: if one has, wait for it to close rather than closing it. Never run two at once.
- Run `git status` and note what is already modified. Other work is in progress in this checkout. Touch none of it, and don't commit, push, stash or switch branch.
- The five output names (section 3) are free: case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem.
- Save a copy of `Assets\Scenes\Game\MRAtlas_MR07_Summit.unity` to the working folder, so the swap can be undone.
- Run the climb capture (section 6) with the tag `before`, for all regions. Those numbers are your regression baseline.

## 1. Read first, in this order
1. `Assets\MountainReliefAtlas\Editor\MraRegion4K.cs`: how the game places and moves every plane. MR07 already has a slot; it switches on when the five files are in.
2. `Assets\Scripts\Environment\ParallaxLayer.cs`: the class notes on vertical parallax, sink limits and zoom.
3. `Assets\Editor\World\ClimbCapture.cs`: the check you must pass.
4. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_86.md: why Batch 28d passed. Then REVIEW_77, 78, 80 and 85: what failed before. Then REVIEW_96 (the Ribwood, the closest match in climb and method).
5. C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch28d\: your pipeline, layout guides, skyline-mask and wrap methods. **Reuse it.** Change only the region's content.
6. The in-game stacks for quality, viewpoint and registration: Assets\Art\Codex\Backgrounds\Terraces4K\ and Ribwood4K\.
7. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (handheld and docked); the original Switch is excluded. Master art is 4K. No platform variants. Every device test is "not tested".
8. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md: section 2 (the mystery rules) and this region's row.
9. BG_Summit_Far, BG_Summit_Mid, BG_Summit_Near_v3: for palette, light and mood only. **Don't copy their compositions. These are new paintings.**
10. Tools\Design\PolishGates\70\climb\after\MR07_climb.jpg: what the region looks like now, and what you are replacing.

## 2. Vertical parallax comes first
This is a 154 u climb, one of the tallest in the game, and most of it is spent moving up. Design every plane for how it moves vertically, not as a still picture. As the camera rises 1 u the terrain moves 1 u down the frame, and each plane moves this far:

| Plane | Vertical speed (terrain = 100%) | Total sink over this climb | What that means for the painting |
|---|---|---|---|
| Sky | 0% | 0 u | Locked to the camera. It must look right without ever moving. |
| Far and High | 2% | 3.1 u | The skyline falls slowly and stays in view throughout. |
| Mid | 3.5% | 5.4 u | Starts high, and is low or gone at the top of the climb. |
| Near | 20% | 30.8 u | Passes by for the whole climb. **It is the plane that makes the climb feel like a climb**, so it needs features at every height of its canvas, each one entering at the top of the frame and leaving at the bottom. |

These match the other regions, and the differences between them are the depth. Rules:
- Nearer planes move faster, always. The Near is at least 15%.
- The Near is a tall canvas because of its speed. Don't let it thin out toward the top: at the top of the climb the player should still see something pass until at least 75%.
- No plane stops short of the top by reaching a sink limit.
- Check motion, not stills: from the game's placement, render a strip of at least 12 frames, 10 u of climb apart, from the middle of the climb. Features must slide at visibly different speeds, and no plane's content may look stuck to another's.
- If the numbers need changing for this region, add a field to the region's `Stack` with today's value as its default, so no other region changes (section 7). If something still feels wrong, fix the painting first.

## 3. What this region gets: five planes (RGBA 8-bit sRGB, 216 px per world unit, bottom-centre pivot, Repeat U / Clamp V)
The game's frame is 3840x2160 (17.8 x 10 world units). Each plane repeats left to right, moving at 1 - 1/depth of the camera.

Install to `Assets\Art\Codex\Backgrounds\Summit4K\`.

| Plane | File | Size | Depth | Placement | What it is |
|---|---|---|---|---|---|
| Sky | BG_Summit_Sky_4K | 3840x3240 | locked to the camera | fills the frame | **This region's own sky.** It has borrowed the Terraces sky until now. A soft gradient wash for this region's light and height. No clouds that read as objects, no land, no horizon, no sun disc. Opaque. Next to the Terraces and Ribwood skies it must clearly be different air: mean RGB differs by at least 12 in a channel, and show them side by side. |
| Far | BG_Summit_Far_4K | 3840x2160 | 25 | 0.4 u above the frame's bottom | Distant ranges, seen from the side. The highest peak tip at least 120 px below the top edge; the skyline within the top 25%. Hazed, cool, low contrast, getting calmer toward the bottom. Land continues to the bottom edge. |
| High | BG_Summit_High_4K | 3840x2160 | 25 | as the Far; fades in over the last 30% of the climb | The view from above for this region: cloud sea, mist or weather below the viewer, with only far peak tips through it. Alpha reaches exactly 0 over its lowest 30%. Its mist colour matches the Far's lower mist (median RGB within 6 per channel). |
| Mid | BG_Summit_Mid_4K | 3840x2700 | 10 | its median skyline starts at 62% of the frame's height from the bottom | A side-on valley wall or ridge for this region. Its stepped skyline sits in the top 700 px; the median skyline row is between 300 and 400 (set the true value in the region's `Stack`). Opaque below the skyline. Softer than the Near, warmer than the Far. |
| Near | BG_Summit_Near_4K | 3840x7356 (Near rate 1.0) | 5 | bottom edge at the frame's bottom | Side-on features that pass by as you climb, in this region's own shape language (below). **Nothing floats:** every feature is grounded, either continuing down out of the canvas's bottom edge or standing on a rock spur, slope or cliff mass that does; an overhang hangs from a cliff mass rising from below. The plane repeats left to right, so it has no frame sides to lean on. **At least 50% of every 3840x2160 window, at any height, is fully transparent (alpha 0).** No window is empty and none is a wall. The nearest and warmest plane, with contrast at about 70% of the gameplay terrain's. |

**The region:** Cliffs and falls above everything, the final climb.

**Region rules:** **The lake and reed beds are terrain; keep them out of the background.** No face, brow, eye, lid or lash shapes anywhere, including in the falls and cliffs. Near shapes: grounded cliff masses rising from the canvas bottom, with overhanging slabs, thin falls with spray, ledges with hardy shrubs. No columns.

**Mystery rule for every plane:** backgrounds show sky, weather and distant land only. Nothing may read as a hand, finger, knuckle, nail, knee, lap, chest, rib, shoulder, face, brow, eye, lid or lash: no five parallel rounded ridges, no rounded dome hills, no symmetric pairs. Check at full size, at 10% and blurred.

## 4. Method (the Batch 28d pipeline, required)
1. **Layout first.** Make a small full-width layout of each plane (for example 960 px wide) designed to wrap. Paint every section from its window of the layout so all sections agree on distance, scale and light. The layout is a guide only: none of its pixels enter a master. Archive it.
2. **Native pixels only.** Never upscale, stretch, resample up or duplicate rows. Build each canvas from 1:1 native sections with at least 25% overlap, painted outward from real neighbouring pixels and blended across the overlap. Show every join.
3. **Paint opaque, then mask with one drawn silhouette.** Never use the generator's alpha. Draw one full-resolution silhouette per plane (Far and Mid: one skyline curve; Near: the outline of every feature). Alpha is 255 inside and 0 outside, with at most a 2 px anti-aliased edge. Decontaminate edge colour against the fill. No translucent land; nothing opaque in the clear areas.
4. **Cyclic wrap.** The last column section continues from the first column's left edge. At every row of the final master: mean RGBA edge difference 0, derivative matched, skyline ends within ±2 px.
5. **Depth sets the look.** Far: soft, hazed, low contrast, calmer toward the bottom. Report the luminance-gradient mean per 540 px band; it must not rise toward the bottom. Mid: readable but softer than Near. Near: the sharpest and warmest.
6. **Side view only.** Every plane is a side elevation. No aerial or top-down ground, no paths or fields seen from above, no ground that fills the frame like a map.
7. **Variety.** No repeated motif or rhythm: vary sizes, angles and spacing. Fewer than three Near features per 3840 px width reads as sparse, more than six as a wall; aim for four or five, each different.

## 5. Failures from earlier batches. Each one is an automatic fail, to repair and not to deliver.
- The generator's alpha used as the mask: dark or black sky blocks, translucent land (28, 28b).
- Wrap edges that don't meet (28b).
- A busy, sharp far plane that doesn't read as distant (28b).
- Aerial or map-like ground, or an opaque near wall that fills the frame and hides the sky (28c).
- Repeated diagonal terrace bands, or one motif repeated (28b, 28c).
- Freestanding columns or spires as the near plane (35).
- Floating near features: arches, rocks or homes hanging in the sky with nothing under them (35b, 36).
- Any upscaling, or a placeholder under a production name.

## 6. Install, check, repair, repeat
Unity: `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe -batchmode -projectPath C:\UnityProjects\Qolossal ...`. A run with `-quit` is only for the swap; the checks exit by themselves. Send each `-logFile` to the working folder and read it. Any compile error or exception is a failure.

1. Copy the five PNGs to the install folder. Set the true Mid skyline row in the region's `Stack`.
2. Swap: `-quit -executeMethod MraRegion4K.SwapAndCapture -mra4kRegion MR07 -captureDir <working folder>\frames`. It replaces only the background planes and saves the scene.
3. Check: `-executeMethod ClimbCapture.Run -climbTag <try name> -captureDir <working folder>\climb` (all regions). Must show, for MR07:
   - `FAULTS 0`: no uncovered frame, no cut plane edge inside the frame, no nearer plane slower than a farther one.
   - `PASS the nearest plane moves at...`, and no `WARN` lines for its planes.
   - Vertical speeds as in section 2.
   - Share of the frame filled at 0/15/30/50/75/100%, near what the accepted stacks show: Far about 84 falling to 55; Mid about 60 falling to near 0; Near about 45 falling to near 0; High 0 until 75%, then about 50 at the top.
   - Every other region's lines exactly as in your `before` run.
4. Look at `MR07_climb.jpg` with your own eyes. Top row: as played. Bottom row: the planes alone. Qori, the ledges and the route read clearly at every step. The sky shows at every step. The High is seen at 100%.
5. Check readability against gameplay: `-quit -executeMethod MraCapture.Run -captureDir <working folder>\beats -mraOnly MR07` and look at every beat, against the gameplay matte Tools\Backgrounds\proposal\mattes\MR07_mid_foreground_rgba.png, and above all the reveal: `08_reveal_lookback` and `09_reveal_pullout` zoom far out, so no plane may leave a gap or a cut edge there, and nothing may give the reveal away before it happens.
6. Anything fails: repair the plane, not the check, and go back to step 1. Don't change `ClimbCapture` to pass. Stop only after a fair third attempt at the same failure, and then say exactly what is failing and why.

## 7. What you may change
- New: the five PNGs and their `.meta` files, in the install folder. As with the other 4K stacks, they aren't added to the accepted manifest, so the importer's "not in the accepted manifest" warning is expected.
- `MRAtlas_MR07_Summit.unity`: only its background planes, through `MraRegion4K`.
- `MraRegion4K.cs`: only the MR07 entry and optional new `Stack` fields whose defaults leave other regions unchanged.
- Nothing else. No accepted file changes byte, no other scene, no other region's values, no terrain, no gameplay, no commit. Prove it with a preservation receipt and a before and after `git status`.

## 8. Deliverables, in C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch39\
- MANIFEST.json: sha256, size, PPU 216, pivot, wrap, depth, vertical speed, sink, placement, per plane.
- QA_INDEX.html and QA_SHEET.md: numbers for sections 2, 3 and 4, the climb sheets of every try, the motion strip, all joins, window sheets, alpha checks, 3x repeats, mystery views, and the three skies side by side.
- sources\<file>\: every native output, section, layout and mask, with exact prompts and settings.
- The `before` and final `climb_report.txt`, the scene copy, the preservation receipt, the name-check log.
- HANDOFF.md, for the user and in plain language: what's in the game now, the final numbers, how many tries it took and what you repaired, your honest doubts, how to undo it, and what to look at when climbing. A platform section keeps source checks, imported-resource measurements and device tests ("not tested") apart. No claim of feel: nobody has played it until the user does.
