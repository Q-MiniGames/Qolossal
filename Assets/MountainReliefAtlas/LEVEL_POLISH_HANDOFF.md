# Mountain Relief Atlas: level polish handoff (4 Oct 2026)

A polish pass on the eight Mountain Relief Atlas regions and their 38 side chambers. It fixes the terrain construction your screenshots showed, redesigns the climbs into broad terraces, sets the caves into the landscape, and restores layered regional backgrounds. Nothing is committed; it's waiting for your play-test.

**In one line:** the regenerated levels pass the full automated play test with the live Qori rig: 582 passed, 0 failed (the 570 original checks plus 12 new live enemy checks). The new static route audit reports 0 problems, and all 1090 accepted files are byte-identical.

Start with `Tools/MountainReliefAtlas/QA/LevelPolish/sheets/00_SCREENSHOT_SPOTS_before_after.jpg`: your three screenshot spots, before and after, at the same camera positions.

---

## 1. What was wrong, and why

| Symptom (your screenshots) | Cause in the builder |
|---|---|
| Narrow, repetitive staircases | The route used the package's generated edge points: about 1 u treads with 1.2 u risers, all the way up. `LimitRisers` re-cut any taller riser back into 0.9 u steps. |
| A slope meeting a raised pad with a sheer wall (screenshot 2) | Each beat pad was flattened at the beat's height, but the neighbouring edge was still graded to the beat's *centre*. The slope therefore arrived low at the pad's edge, leaving a riser of slope × half the pad width (about 2 u). |
| A cave poorly connected to its approach (screenshots 1 and 3) | Doors were placed at a fixed offset (beat + 1.8 u) on whatever ground was there: a stair, a slope, or a pad's edge. The art's base floated and its side met a bare riser. |
| Abrupt material joins and exposed rectangles | The top strip simply stopped at every riser, and risers showed raw fill with a hard square corner. Nothing drew a wall face or a lip. |
| Empty sky | The Mid and Near strips were placed at their native 4.8 u, low in the frame. On a climb the terrain covers the lower right, so the layers sat behind the rock and the upper two thirds was bare sky. |

---

## 2. What changed

### 2.1 The route is now designed

New tool: `Tools/MountainReliefAtlas/design_route.py` writes `Resources/MRA/mra_route.json`; the builder reads it. Rebuild with `python design_route.py --plots`, then the builder menu as before. It's deterministic, and the side views are in `QA/route_design/MR0x_side.png`.

- **What stays fixed:**
  - Beats keep the package's positions, so the Chart correspondence is unchanged: the play test measured it at every beat.
  - The modules keep the geometry the play test validated: quake crossing, thread lift, sluice, glide span.
- **What's designed between beats, from the live Player prefab's numbers:**
  - The prefab measures: jump apex 2.45 u; collider about 0.8 × 1.2; a ledge grab with no relic reaches about 3.4 u; walkable slope up to 1.2.
  - **Walls:**
    - *step*: up to 1.9 u, a plain jump;
    - *ledge*: 1.9–3.2 u, a jump and a ledge grab;
    - *climb*: over 3.2 u, Climbing Moss; only after the Cradle's moss shrine (`MR01_N06`), and always clingable.
  - **Treads:** 4–7 u, some level and some gentle walkable slopes up to 0.6. Ramps stay at 0.75–0.9 or less, so the moss strip reads well.
  - **Gentle edges:** one eased slope with a crest into each pad.
  - **Beat pads:** at least 6 u. A beat with a cave gets 11 u, and the cave is set into the wall that rises at the end of the pad, with a full tread on the cave's roof above it.
  - **Combat shelves:** 8–11 u. Each is at least 6 u from a cave mouth and away from beats. Enemies stand at the shelf's centre.
  - **Every non-vertical corner is rounded,** so a slope runs into a terrace as one continuous crest.
  - **Narrow treads are merged:** where two walls end up less than 3.7 u apart, the designer folds them into one wall.
- **Variety:** each connector picks a style from a rotation (walk, terraces, climb, mixed) with an uneven tread rhythm, so no two ascents repeat. Short runs fall back to a scramble slope, a scramble with one wall, or a single wall.
- **Before / after per region:**

| Region | Risers before (authored) | Walls after: jump / ledge / climb | Median tread after | Caves set into a wall |
|---|---|---|---|---|
| MR01 Cradle | 15 | 2 / 3 / 2 | 11.5 u | 1 / 6 (gentle region; the rest stand free on 11 u pads) |
| MR02 Causeway | 81 | 14 / 8 / 12 | 6.8 u | 5 / 6 |
| MR03 Terraces | 135 | 14 / 9 / 16 | 5.9 u | 4 / 6 |
| MR04 Qvale | 21 | 8 / 0 / 2 | 8.0 u | (town interiors) |
| MR05 Ribwood | 129 | 9 / 21 / 16 | 6.0 u | 6 / 6 |
| MR06 Heights | 191 | 10 / 10 / 29 | 6.5 u | 6 / 6 |
| MR07 Summit | 134 | 4 / 4 / 24 | 7.1 u | 6 / 6 |
| MR08 Descent | 153 | 3 / 8 / 20 | 6.6 u | 0 / 2 (descents: free-standing) |

  The Summit and Heights are climb-heavy because their edges average 0.65–0.78 rise over run: broad treads at that gradient need tall walls. Section 6 asks whether you want that.

### 2.2 The terrain renderer (`Scripts/MraTerrain.cs`; shared, rebuilt on enable)

- **Riser faces:** every wall and open piece end gets a band of the family's face art. Body, Causeway and Terraces use their `Cliff_Face`; the legacy kits use `A*_Wall_Side`, with its ragged painted edge laid on the riser line. The band fades into the fill behind and at its foot. Its depth scales with the wall's height, so short steps don't smudge.
- **Moss lips:** the top strip overhangs every wall by 0.22 u, so there are no square corners.
- **Depth shading:** the fill darkens to 80% over 7 u below the walk line. Large masses read as rock mass, and the textures still tile in world space at their accepted scale.
- **Chamber slabs:** floating platforms and the ceiling get the kit's `Ceiling_Under` band upside down, tinted toward the cave rock, instead of a raw rectangular cut.
- **Collision** is still the walk line exactly (a polygon on the designed points). Nothing is traced from a painting.

### 2.3 Caves (`MraWorldBuilder.Doors`, `MraChamberDoor`, `MraState.Prompt`)

- **Placement:** the mouth sits where the designer put it: embedded 0.5 u into its wall where one rises, or free on the broad pad. The base is sunk 0.14 u, at scale 0.74.
- **Matching rock:** in the bark and slate regions the limestone cave art takes a renderer tint of the regional rock. The image bytes are unchanged.
- **Joins:** the decor sheet's plants hide the joins at the foot and on the wall top.
- **Alignment:** the trigger (2.6 × 3 u), the art and the prompt share the mouth's centre. The return spawns are unchanged: they're the data's beat spots, on the pad.
- **Prompts:**
  - They sit on a dark plate just above the entrance, readable on bright sky and pale rock.
  - The Up action draws as a key cap for the device in use: **W** on the keyboard, **D-pad Up** on a gamepad. It switches on the next input.
  - These caps are text, not platform glyph art (see section 5).

### 2.4 Backgrounds (`MraWorldBuilder.Background`; `ParallaxLayer` gains `texture`, `tint`, `composedForSize`)

- **Layers per region:**
  - the regional sky;
  - a **far painting** where one was designed for these regions: the earlier A0–A3 room far layers. Cradle: `MistyValley_Background_v1`. Causeway: `Aqueduct`. Ribwood: `AncientGrove`. Heights: `FallsSanctuary`, read as a plain texture so its import is untouched. Elsewhere: a hazed, smaller copy of the region's Mid as a far ridge (Qvale uses its accepted `BG_Qvale_Far`);
  - **Mid**, at 7.2 u, its skyline just above the middle of the frame;
  - **Near**, at 7 u, its foot at the frame's bottom, so it sits in the lower third behind the action, not level with the walk line;
  - the Terraces mill landmark, kept over Mill Ridge.
- **Heights on screen** come from each strip's measured skyline, so the valleys, ridges and villages fill the frame.
- **Zoom:** the layers scale with the camera's size (composed for size 5), so lookouts and wide views keep the composition and never show a band edge.
- **Parallax:** horizontal follow is 0.985 / 0.93 / 0.82 / 0.62 for far painting / far ridge / Mid / Near. Vertical follow is about 0.995: the climbs are hundreds of units tall, so the layers stay in frame and drift under a unit across a region.
- **Atmospheric separation:**
  - the far layers are lightly hazed toward the sky;
  - the Ribwood, Summit and Descent get a sky-coloured veil (10%, 30%, 24%) between the valley and the rock, because their slate and deep-green layers were as dark as the terrain. On the Summit it reads as warm dawn haze, in the Morning Herbarium palette.
- **Rock dressing:** in the limestone regions (MR01–MR04), the accepted crease decals are scattered well inside the rock fill: never on a walk line or a wall.
- **Terraces Far/Mid/Near (Review 22 redos) are not used anywhere.** The Terraces use `BG_Sky_Terraces`, a hazed A5 Mid far ridge, A5 Mid/Near and the mill. The seamless redo is in the drafted Batch 12 request (`Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH12.md`, W1).

### 2.5 Tests and audit

- **`MraRouteAudit` was rewritten for the new rules:**
  - wall classes, with climbs only after Climbing Moss and only on clingable walls;
  - slopes 0.9 or less;
  - narrow treads;
  - module gaps;
  - every combat shelf at least 4 u from a beat, at least 6 u from a cave mouth, and at least 8 u wide.
- **`MraTestDriver` gained a live encounter check:**
  - every placed enemy settles on its shelf and keeps to it, with Qori far away;
  - `-mraEncountersOnly` runs that part alone.
- **`MraPolishCapture`** (new) captures before and after shots at camera spots taken from the data only: every beat, every cave, samples every 24 u along each edge, wide views every 80 u, and the chambers. The two sets line up exactly.
- **`polish_sheets.py`** (new) builds the side-by-side sheets.

---

## 3. Files

**New:**
- `Tools/MountainReliefAtlas/design_route.py`
- `Tools/MountainReliefAtlas/polish_sheets.py`
- `Assets/MountainReliefAtlas/Resources/MRA/mra_route.json`
- `Assets/MountainReliefAtlas/Resources/MRA/MRA_HazeVeil.png` (a generated 4×4 white pixel block, not art)
- `Assets/MountainReliefAtlas/Editor/MraRouteDesign.cs`
- `Assets/MountainReliefAtlas/Editor/MraPolishCapture.cs`

**Changed:**

| File | Change |
|---|---|
| `Assets/MountainReliefAtlas/Editor/MraWorldBuilder.cs` | Route from the design; doors; shelves; decor keep-clear; rock dressing; backgrounds; haze veil; cave tint |
| `Assets/MountainReliefAtlas/Editor/MraBuilderArt.cs` | Riser-face art per family |
| `Assets/MountainReliefAtlas/Editor/MraChamberBuilder.cs` | Slab undersides |
| `Assets/MountainReliefAtlas/Editor/MraRouteAudit.cs` | Rewritten for the new rules |
| `Assets/MountainReliefAtlas/Scripts/MraTerrain.cs` | Risers, lips, depth shading, undersides |
| `Assets/MountainReliefAtlas/Scripts/MraState.cs` | Prompt plate and device key caps |
| `Assets/MountainReliefAtlas/Scripts/MraChamberDoor.cs` | `promptHeight` |
| `Assets/MountainReliefAtlas/Scripts/Testing/MraTestDriver.cs` | Encounter check |
| `Assets/Scripts/Environment/ParallaxLayer.cs` | `texture`, `tint`, `composedForSize` (additive; the defaults leave existing scenes unchanged) |

**Rebuilt:** all 46 scenes, generated roots only. The Player, camera and GameManager are reused; the camera gains a `Haze veil (MRA builder)` child in MR05, MR07 and MR08.

**Regenerating keeps every fix.** `design_route.py` and then **Qolossal ▸ Mountain Relief Atlas ▸ Build All Scenes** rebuild everything above deterministically. This was checked: the scenes were rebuilt from scratch repeatedly during the pass, and the audit, captures and play test ran on the rebuilt scenes, not on hand edits.

---

## 4. Evidence, tests and measurements

**Evidence** is in `Tools/MountainReliefAtlas/QA/LevelPolish/`:
- `sheets/`:
  - `00_SCREENSHOT_SPOTS_before_after.jpg`;
  - per region: caves, wide composition, climb samples and beats, before | after;
  - `Chambers_before_after.jpg`.
- `captures/before` and `captures/after`: in-engine 1920×1080 and 1280×720 JPEGs at matched spots: every cave in both resolutions, wide views, every chamber, and the MR02/MR03 climb samples. The full sets (742 shots) are in `C:\_temp\qp_cap\polish_before` and `polish_after`.
- Results files:
  - `mra_test_results_final.json`: the final full play test, 582 passed, 0 failed;
  - `mra_test_results_encounters.json`: the encounter check run alone;
  - `mra_route_audit.json`: the audit;
  - `mra_profile_1080_polish.json`: the Windows profile;
  - the design side views are in `QA/route_design/`.

**Play test, live Player prefab, virtual keyboard and pad, scratch copy:**
- **Main route:** all eight regions walked by input, start to exit. Times: MR01 40 s, MR02 77 s, MR03 80 s, MR04 28 s, MR05 91 s, MR06 109 s, MR07 86 s, MR08 39 s.
- **Climbs:** the new moss climbs are climbed by the autopilot's wall jumps, and ledge walls are taken by grab and climb.
- **Unchanged checks still pass:** relic gates before and after, all 38 chambers including entry, return to the safe spot, checkpoints and no repeat rewards, Qvale, saving, and Chart correspondence at every beat.
- **Result, final full run on the final build:** **582 passed, 0 failed**: the 570 original checks plus 12 encounter checks, one per enemy. Route audit: 0 problems.

**Platform status:**

| | Status |
|---|---|
| Source checks | All 1090 accepted files byte-identical in staging and `Assets/` (`PRESERVATION_RECEIPT.json` refreshed). Global name check: 0 collisions for the new names. No accepted pivot, PPU, slice or import setting changed. |
| Imported resources (Windows editor) | Play test and captures above. Unchanged texture import settings: no compression formats were inferred. |
| Windows player (64-bit development build, i9-11900K / RTX 3070 Ti, 1920×1080 windowed, vsync off) | Built with 0 errors. Every region: median 0.56–0.64 ms, p95 1.16–1.40 ms, p99 1.75–1.91 ms, max 7.1–8.8 ms, **0 frames over 16.7 ms**; region loads 21–748 ms. The A0 baseline is the same (max 2.5 ms). The earlier ~52 ms spikes didn't occur on this run, which supports the machine-contention reading. |
| PS5 | **not tested** |
| Xbox Series X | **not tested** |
| Xbox Series S | **not tested** |
| Switch 2 handheld | **not tested** |
| Switch 2 docked | **not tested** |

**Cost of the new rendering:**
- per terrain piece, one more mesh (the risers band), plus an underside band on chamber slabs;
- per region, one more parallax quad (the far layer);
- in three regions, one camera-attached veil quad;
- crease decals: about 30 sprites per limestone region.

All of it uses the existing shared material and transparency only; the profile above shows no frame cost.

---

## 5. Remaining dependencies

**Art:**
- **Terraces Far/Mid/Near seamless redo:** accepted in Review 23 (manifest 1096). It isn't wired in yet: MR03 still uses its far-ridge stand-in in `Background`.
- **Summit lake, reeds and waterfall shelf terrain:** still the A6 kit (no such art exists). Reveal continuity exists in the data and the reveal ledge, not yet in the terrain art.
- **Cave entrance art per kit:** bark, slate and deep-green regions use a tinted limestone mouth. A matching mouth per legacy kit would integrate fully.
- **Platform glyph art:** button art per platform (PlayStation, Xbox, Switch 2) to replace the text key caps. A shipping font with full symbols is also needed, as noted in the main handoff.
- **Chamber dressing:** the chambers are better finished but still prototype rooms (pale climbing-wall boxes, placeholder crates, levers and lights).
- **Unchanged from before:** a production final-assembly painting, the Causeway aqueduct and root-bridge props, and the music.

**Design (yours to judge in play):**
- **Climb share in steep regions:** the Heights and Summit have 29 and 24 moss climbs. Broad treads at their gradients need tall walls (up to 9 u). Options:
  1. keep them;
  2. let the treads narrow toward 3.5 u there;
  3. move some beats lower, which changes the Chart registration.
- **Wall feel:** ledge walls (1.9–3.2 u) rely on the ledge grab. Climb walls rely on wall-jumping straight up, which the autopilot does easily; check that it feels good by hand.
- **Encounters:** combat on the shelves hasn't been playtested by a human. The check only shows each enemy settles and holds its shelf.
- **Qvale:** the Qvale approach (E00, E09, E10) now has two 5.7 u moss walls; it could be made gentler for a town if you prefer.

---

# Round 2 (4 Oct 2026, evening): cliffs, grounding, parallax, transitions, the overlook, houses

Your second brief: six issues, from two screenshots and four screen recordings. Evidence is in `Tools/MountainReliefAtlas/QA/LevelPolish/round2/` (sheets 01–06 and `frames/`). Nothing is committed.

## R2.1 Cliff edges: natural limestone contours
- **Cause:** every riser and piece end drew the face art as a straight vertical band, so walls ended in ruler-straight cuts. The Body, Causeway and Terraces kits have no painted cliff side, corner or foot pieces.
- **Fix (`MraTerrain`, shared; regenerated on load):**
  - Each wall in the opaque-face kits (MR01–MR04) gets an irregular limestone contour on its open side: smooth layered undulation, a few rounded ledges, a talus flare at the foot, drawn back under the moss lip.
  - The contour is painted in the kit's own `Cliff_Face`, with a dark weathered rim. It reaches 0–0.68 u past the wall line, averaging about 0.2 u.
  - The legacy kits keep their `Wall_Side` art, whose painted edge is already ragged.
- **Vegetation transitions (`CliffDressing`):** hanging ivy or roots over the lip of walls 1.6 u and taller, and a plant or pebbles at the foot. Both come from each region's accepted decor sheets, and are kept clear of cave mouths.
- **Collision is unchanged:** it stays on the straight wall line, so walls cling and climb exactly as before.
- **Limit:** this is as far as the existing art goes. A precise Codex request for painted cliff sides, top-corner caps, talus feet and ledges for the three limestone kits is in `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH13.md` (W1). It gives canvases, 120 PPU, pivots, the collision line in pixels, overlap rules and assembly proofs. It's a draft and has not been sent.
- **Evidence:** `round2/01_cliffs_before_after.jpg` and `02_cliff_closeups_before_after.jpg`. The "before" is round 1's final capture at the same spots; the round-2 "before" set already rendered the new contour, because terrain meshes regenerate on load.

## R2.2 Grounding props by their painted contact
- **New: `MraGrounding.GroundProp`.** It finds each sprite's lowest opaque row and the opaque width of its lowest rows from the decoded source pixels, samples the real terrain across that width, and moves the prop so its painted base touches the ground, bedded in 0.05–0.12 u.
  - A wide prop on uneven ground (more than 0.35 u difference over more than 1.2 u) would get a level stone footing. None was needed.
  - Every placement is logged in `Tools/MountainReliefAtlas/QA/grounding_audit.json`.
- **What gets grounded:** checkpoints, Waymarks, ability shrines, knots, cave mouths and their plants, decor, the sluice wheel, threshold arches, house fronts (by their door-threshold pivot), the smithy, street lamps, the listening tree and bench, and cliff plants.
- **What it found:**
  - every Waymark floated 0.32 u;
  - every cave mouth floated about 0.28 u (the old fixed −0.14 sink wasn't enough);
  - every knot was sunk 0.29 u;
  - hanging decor (resin strands, root stalactites, cable roots, cloth banners) had been standing on treads. The ground decor pass now places only things that rest on the ground.
- **The screenshot-2 checkpoint (Cradle):** it had no accepted art, so at run time the old lantern was drawn 0.8 u up. It now uses the accepted `Checkpoint_Shrine_A1`, grounded.
- **Evidence:** `round2/03_grounded_props_before_after.jpg`.

## R2.3 Stable parallax
- **Causes:**
  1. The layers updated in `LateUpdate` before `CameraFollow` moved the camera, so they lagged it by a frame on every vertical move: the bob in your recording.
  2. Each layer followed the camera vertically at its own rate (0.994–0.997), so the depths drifted independently.
- **Fixes (`ParallaxLayer`, shared, additive):**
  - It runs after the camera (`DefaultExecutionOrder(10000)`).
  - A new **climb progression** mode replaces the per-layer vertical follow. Each layer holds a fixed height on screen and sinks by a set amount across the region's climb, measured on one shared, slowly smoothed (2.2 s) camera height. Jumps, landings and framing leads don't move it. A sustained climb lowers the near layer most and the far painting least: near 1.1, mid 0.7, landmark 0.55, far ridge 0.4, far painting 0.25 u across a whole region.
  - Horizontal parallax, wrapping and zoom scaling are unchanged. The sky stays camera-locked.
- **Measured live** (play test, MR01):

| Situation | Result |
|---|---|
| Standing still | 0.001 u |
| Jumping and landing for 3 s (camera moved 2.33 u) | 0.033 u |
| A climb across the region | near layer sank 0.69 u, far 0.16 u, in depth order and bounded |

- **Evidence:** `round2/frames/parallax_1..4`.

**Update (5 Oct, after play-test feedback):**
- **Feedback:** the three layers were still obvious, especially while zooming.
- **Moving as one:** the valley layers now move as **one painting**. Every region uses one horizontal rate (0.9) and one climb sink (0.6 u) for the far painting, far ridge, Mid, Near and landmark. Only the camera-locked sky differs.
- **Zoom fix (`ParallaxLayer`):** while zoomed, a layer's horizontal offset now scales with it, so the whole background zooms about the camera centre instead of each layer sliding.
- **Measured:** standing still, 0.000 u; jumping, 0.020 u; a climb sinks every layer by the same 0.37 u. Frames are in `round2/frames_background/`.
- **Trade-off:** there is no parallax between the layers any more. The depth comes from the painting itself and from the terrain moving at full speed in front.

## R2.4 Readable level transitions
- **Thresholds:** every region boundary has an accepted arch the route passes through, grounded by its feet: the Causeway aqueduct arch at the Cradle → Causeway crossing, the stone arch (`Portal_Gate_A5`) and the vine arch (`Portal_Gate_Milestone`) elsewhere. They sit at both ends of each crossing.
- **Signs:** near an exit, a plate names where it leads ("The Long Causeway ›" / "‹ The Cradle", or "(closed)" when locked).
- **Area-name card (`AreaTransition`, shared):**
  - It's on a dark plate, warm white, and shown **once per place within 90 s**. Crossing back and forth, or coming out of a cave or house into the same region, doesn't repeat it.
  - Entering a cave or house shows that place's name.
  - The fade stays the existing short 0.35 s, only on real scene changes.
- **Arrivals:** positions and camera framing were checked on each crossing and each house return.
- **Measured:** crossing on names the Causeway once, and two more back-and-forth crossings show no repeated names.
- **Evidence:** `round2/frames/transition_1_arch_approach`, `transition_2_arrival_name`.

## R2.5 The listening overlook
- **Before:** the bench sat under the tree, crowded by the loft's floating limestone steps and its song shell, with climbing terrain right behind.
- **Now:**
  - The designer gives `MR04_N09` a **19 u level shelf** (`RESTING` in `design_route.py`): 4 u before the beat to 15 u after. The bench and tree stand mid-shelf on level ground.
  - The loft's climb and shell moved **indoors** (see R2.6), and its doorway stands at the shelf's far left. Fennel stands beyond the bench.
  - No decor is scattered on the shelf. Qvale has no enemies or hazards.
  - The seated view is framed toward the open valley (frame centre left of the bench, size 6.2).
- **Measured:** ground level within 0.00 u from the shelf's start to past the bench. Walked to the bench without a jump, Up sits, Cancel stands (after the 1.4 s view ease), and Qori walks on freely.
- **Evidence:** `round2/04_overlook_before_after.jpg` and `frames/overlook_seated_view`.

## R2.6 Houses are enterable instances
- **Data:** in `convert_spec.py`, the user's decision is recorded. `MR04_C02` (Herbalist Home), `_C03` (Mapmaker Room), `_C04` (Residents' Home A), `_C05` (Old Man's Home) and `_C06` (Musician Loft) are now `house-instance`s, each with its own scene `MRAtlas_MR04_Cxx`. The smithy stays an open-fronted forge on the street.
- **Outside (`MraTownBuilder.HouseFront`):**
  - each home's accepted `_Closed` exterior stands on the street by its door-threshold pivot;
  - a door (`MraChamberDoor`, Up, "▲ Enter …" prompt) sits at the measured painted doorway. HomeC's is up its steps;
  - the walk-up discovery is kept, and a safe landing waits right in front of the door.
- **Inside (`BuildHouse`):**
  - the home's accepted `_Cutaway` painting is the room, on a dark rock floor at the door threshold, with a wall at the back;
  - the same residents, variants and shops as before (state is in the save, unchanged);
  - "‹ Out to Qvale" by the door. Walking back out of the door returns Qori to the street at the same house.
- **The loft:** no loft paintings exist. Inside, warm cave rock, the musician's corner and **one solid stair** (the old floating steps had gaps Qori could catch in) up to the song shell. Outside, a stand-in rock doorway with the musician's corner. Codex request W2 (Batch 13) covers the loft's exterior and interior.
- **Measured, for each of the five:**
  - the door stands on the street;
  - it's discovered at the door, and Up enters the right interior;
  - Qori arrives clear of the walls, and the resident is home (Marrow too, after his rescue);
  - the Scribble shop works inside;
  - the loft's stair is climbed by input to the shell;
  - walking out lands Qori at his own door, framed by the camera.
- **Evidence:** `round2/05_houses_before_after.jpg` and `frames/house_*`.

## R2 files

| File | Status | What |
|---|---|---|
| `Assets/MountainReliefAtlas/Editor/MraGrounding.cs` | new | Grounding by painted contact |
| `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH13.md` | new, draft | The Codex request |
| `Assets/MountainReliefAtlas/Scenes/MRAtlas_MR04_C02.unity` … `_C06.unity` | new | The house interiors |
| `Scripts/MraTerrain.cs` | changed | Contour |
| `Scripts/MraChamberDoor.cs`, `MraChamberReturn.cs`, `MraExit.cs` | changed | Signs |
| `Scripts/MraData.cs` | changed | `IsHouse` |
| `Editor/MraWorldBuilder.cs` | changed | Grounding calls, arches, cliff dressing, climb-progression backgrounds, decor rules |
| `Editor/MraTownBuilder.cs` | changed | Houses, overlook |
| `Editor/MraBuilderArt.cs` | changed | Cradle shrine, contour reach |
| `Editor/MraChamberBuilder.cs` | changed | Return signs |
| `Editor/MraPolishCapture.cs` | changed | Overlook and house spots |
| `Scripts/Testing/MraTestDriver.cs` | changed | House, overlook, transition, parallax and climb-repeat checks, gameplay frames, `-mraPolishOnly` |
| `Tools/MountainReliefAtlas/convert_spec.py` | changed | House instances |
| `Tools/MountainReliefAtlas/design_route.py` | changed | `RESTING` shelf |
| `Assets/Scripts/Environment/ParallaxLayer.cs` | changed, shared | Execution order, climb progression |
| `Assets/Scripts/Progress/AreaTransition.cs` | changed, shared | Name plate, once-per-place memory, test hook |
| `ProjectSettings/EditorBuildSettings.asset` | changed | Five house scenes added |
| `Editor/MraWindowsBuild.cs` | changed | Measurement builds run without window focus; the project setting is restored afterwards |

## R2 validation

**Play tests** (live Player prefab, virtual keyboard and pad, scratch copy):

| Run | Result |
|---|---|
| Final full run | **637 passed, 0 failed**: all regions walked by input, 38 caves, the 5 houses, the overlook, saving, the Chart, the encounters, transitions and parallax. `round2/mra_test_results_round2_final.json`. |
| Polish checks alone (`-mraPolishOnly`) | **71 passed, 0 failed**: `round2/mra_test_results_polish_checks.json`. Includes five repeats of the Terraces 4.9 u + 7.8 u climb with only the main-route relics, each passing in 3.0 s. |
| Earlier full run | One main-route stall at that same climb's lip (MR03, x 194.4): an autopilot timing interaction, not reproduced in the repeats or the final run. Worth trying by hand. |

**Platform status:**

| | Status |
|---|---|
| Source checks | All **1096** accepted files byte-identical in staging and `Assets/` (the receipt is refreshed). New names: 0 collisions. No accepted pivot, PPU, slice or import setting changed. |
| Imported resources (Windows editor) | The play tests above; captures at 1920×1080 and 1280×720 (`round2/`). |
| Windows player (development build, 1920×1080) | Built with 0 errors and run. **This round's frame timings are not a clean measurement:** once the player window went to the background, every region **and the existing A0 baseline** showed the same ~55 ms frames. MR01, measured first, had a median of 0.81 ms, p95 2.08 ms and 10 frames over 16.7 ms; round 1's quiet, focused run had a worst frame of 8.8 ms everywhere. Rerun focused on a quiet machine. `round2/mra_profile_1080_round2.json`. |
| PS5 | **not tested** |
| Xbox Series X | **not tested** |
| Xbox Series S | **not tested** |
| Switch 2 handheld | **not tested** |
| Switch 2 docked | **not tested** |

**Remaining art requests:** Batch 13 (draft, not sent):
- W1: limestone cliff sides, top corners, talus feet and ledges;
- W2: the musician's loft exterior and interior;
- W3: cave mouths per legacy kit;
- W4: input glyphs.

Still pending from before: wiring in Review 23's art, Chart round 2, Summit lake/reed terrain, and the music.
