# Codex prompt: listening-bench scenery fixes (Retreat v02)

Written 18 Oct 2026, from Claude Review 79 (`Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_79.md`). The user asked for it ("send the prompt for the fixes"). Not sent: the user sends it. Claude reviews the delivery as the next free CLAUDE_REVIEW number (re-check just before writing).

---

Qolossal: listening-bench scenery fixes, Retreat v02. This fixes the seven listening-bench places you delivered in v01. Do Parts A to D, then Part E if there is time. Stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
v01 inputs (read only, do not modify):
- Art\Codex_v2_full\Retreat4K_NewScene_v01\SeparateLayers_v01 (Overlook Terrace)
- Art\Codex_v2_full\Retreat_OtherScenes_Repaint_v01 (Causeway Dusk, Cradle Meadow, Heights Clouds, Overlook Night, Ribwood Glade, Terraces Rain)
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Retreat_Fixes_v02\<SceneName>\

READ FIRST
1. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_79.md, with its images CLAUDE_REVIEW_79_Seams.jpg and CLAUDE_REVIEW_79_BenchContact.jpg.
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_79_OverlookTerrace_Registration_PROPOSED.json: how the layers will be used in Unity, including drift, wrap, sway pivots, and Qori sitting behind the Bench layer.
3. Workspace AGENTS.md and PlatformDirection\QOLOSSAL_PLATFORM_DIRECTION.md.

RULES
- Keep the approved painterly fantasy style and each place's mood and composition. These are fixes, not redesigns. Do not move, rescale or repaint layers that are not named below.
- Never modify v01 files. A layer you don't change is referenced by its v01 path and SHA-256, not re-exported.
- Use the same coordinate system: top-left origin on the shared 3840 x 2160 canvas. Canvases may now extend past the edges (negative offsets) where Part D asks for it.
- Keep every generated original untouched in Sources\, and record each export's generated size and whether it was enlarged. Do not describe anything as native 4K unless it was generated at that size.
- Make alpha from your own drawn mattes, never from the generator's alpha. Decontaminate edge fringes. Straight (non-premultiplied) alpha.
- The benches stay empty. Do not draw Qori.
- No writes to the Unity project. Do not start Batch 7 and do not touch the music held for audition.

PART A — CONTACT SHADOWS (all seven places)
The benches and props float: none of the scenes has any contact shadow (Review 79, BenchContact image).
1. For each place, add a separate `BenchShadow` layer drawn under the bench.
   - Use dark, slightly cool colour with soft alpha, painted for that place's light direction and ground: paving, grass, rock, wet stone or leaf litter.
   - Each of the four post feet gets a tight, dark contact core that fades out within about 60-120 px.
   - Add a broader, lighter shade under the seat.
   - On rough ground (Heights Clouds, Terraces Rain, Ribwood Glade, Cradle Meadow) the shadow should follow the ground's bumps, not lie as a flat ellipse.
2. Add the same kind of contact shadow for the other free-standing props:
   - `PropShadow_FlowerPot` (Overlook Terrace, Overlook Night);
   - `PropShadow_LanternPost` (Causeway Dusk);
   - the planters at the tree bases if they read as floating.
3. Do not bake shadows into the ground layers. The bench is a separate layer, and its shadow must stay a separate layer under it.
4. Measure: composite each shadow over its ground and report the luminance drop directly under each post foot (target 25-45%) and under the seat centre (target 10-20%).
5. Before/after crops at 100% for every place.

PART B — SEAMS FROM THE TREE LAYERS
1. **Terraces Rain:** `04_Tree` carries its own patch of ground with a fully opaque bottom row. That patch makes a hard horizontal cut at y = 1750 across x 0-2200 (the bench feet stand on it) and a vertical cut near x = 2200.
   - Re-matte the tree to trunk, roots and canopy only.
   - The roots must feather into `03_Ground`, and no ground from the tree layer may remain.
   - If the ground under the bench then needs to continue, fix it in a new `03_Ground` export, not in the tree layer.
2. **Ribwood Glade:** same fault, with `04_Tree`'s bottom row 69% opaque, making a cut at y = 1800. Same fix.
3. Check every layer of all seven places: no layer may end in a straight opaque edge inside the canvas unless that edge is a real object edge.
4. Report a row/column hard-edge scan of each new assembly. Use the fraction of pixels whose mean RGB step to the next row or column is over 30. No row or column may exceed 0.2 except at real object edges, and list those edges by name.

PART C — OVERLOOK NIGHT PROPS, LAMP GLOWS, RAIN
1. Overlook Night's `05_Lantern` and `05_FlowerPot` are the daylight Overlook Terrace files, byte for byte. Make night versions:
   - moonlit cool values to match the night bench and wall;
   - the lantern's flame lit, with the warm light falling on its own frame;
   - a little warm spill on the pot from the lantern above.
   - Same size and position as v01 unless the night lighting needs a few pixels.
2. Add a separate `LanternGlow` layer (soft flame halo on transparency, not baked in) for:
   - the Overlook Terrace lantern;
   - the Overlook Night lantern;
   - the Causeway Dusk lantern post's lamp.
   - Give each glow's offset, a suggested opacity range for flicker (for example 0.65-1.0) and the lamp pivot it hangs from.
3. Terraces Rain: `07_Rain` is a single still frame, with streaks only in rows 21-2092, so it can't scroll. Add `07_Rain_Tile`:
   - a vertically repeating rain tile, for example 3840 x 1080, whose bottom row continues into its top row;
   - same streak angle, density and value as v01;
   - prove the period with a 3-tile stack.
   - Keep the v01 still as well.

PART D — PARALLAX MARGINS AND CLOUD WRAP
Unity drifts the far and middle-distance planes a little sideways and wraps the clouds. The v01 layers are exactly 3840 wide, so the drift would show their edges.
1. Overlook Terrace:
   - extend `03_Mountains` and `04_Valley` by at least 64 px on both sides (canvas 3968 or wider, offset x -64) by painting the continuation. Do not stretch.
   - Make `02_Clouds` wrap horizontally with period 3840: its left and right edges must continue into each other, with mean wrap difference 0 after the seam blend.
2. The six other places: extend `02_MiddleDistance` the same way, at least 64 px each side.
3. Report the new canvases, offsets and wrap measurements.

PART E — SPLIT THE SIX BACKDROPS (do last; skip if short of time and say so)
In the six other places, sky, clouds and far land are baked into one opaque `01_Backdrop`, so the clouds can't drift as they do in the live places. For each of the six, split the backdrop into:
- `01_Sky`: opaque, with no clouds;
- `02_Clouds`: on transparency, wrapping horizontally with period 3840;
- `03_Far`: the distant land, on transparency with a drawn skyline, with the 64 px margins.
Match the v01 backdrop's look. Renumber the later layers in the registration, keeping their files.

DELIVERABLES (per place, under Retreat_Fixes_v02\<SceneName>\)
- Layers\ holds only new or changed PNGs. Sources\ holds the untouched generated originals.
- <SceneName>_Layered.ora: the full v02 stack, with reused v01 layers included.
- Registration_v02.json listing every layer in draw order, back to front. Give each layer:
  - file path (v01 or v02), SHA-256, canvas size and offset;
  - generated size, and enlarged true/false;
  - `changed_from_v01` with a one-line reason;
  - pivots for lanterns and glows;
  - wrap period for clouds and rain;
  - drift margin.
- QA\: an assembled 3840 x 2160 preview, before/after crops at 100% for every fix, the shadow luminance numbers, the seam scan, and the wrap/period proofs.
- At the package root: README.txt, QA_Summary.json, Review.html comparing v01 and v02 per place, and a zip archive. Test the archive and the .ora files.

STOP once everything is delivered, for Claude's review. Report what you could not do and why, and keep untested platform claims out: nothing here has been tested on PC, PS5, Xbox Series X|S or Switch 2.
