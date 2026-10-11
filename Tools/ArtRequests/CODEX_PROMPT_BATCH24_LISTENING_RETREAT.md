Qolossal: Codex Batch 24 (a pilot of the music listening retreat: the Qvale Rain Window, seen from behind, as layered painted art). Concept and production art only: no Unity import, no code, no music work. Do all of it, then stop once for Claude's review.

Written 10 Oct 2026, after the user said "yes" to piloting the Rain Window rear view from your round 2 concepts. Not sent: the user sends it. Names checked free in Tools\ArtImport\codex_v2_accepted.json on 10 Oct 2026 (no `Retreat_` names exist).

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch24\
Copy your context files into Batch24\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHY
The retreat is a standalone static-camera scene opened from the existing Qvale listening bench (Town/Town_Qvale_ListeningBench, placed by MraTownBuilder). Qori sits on that bench, with a music playlist and a scenery picker drawn later as separate UI. Your round 2 concept Codex_v2_full\ConceptTests\Qori_Music_Retreat_20261010\Rear_View_Revision\01_Qvale_Rain_Window_Rear.png is the chosen direction: keep its mood, room, window and valley. Your own NOTES.md there lists what still needs fixing, and the user agreed with it. This batch fixes those faults and splits the scene into layers the game can move and animate.

THE FAULTS TO FIX (from round 2)
1. **Head.** Qori's head read as a leaf mound with ears stuck on. From behind, the head must be a distinct rounded shape sitting clearly above the shoulders: the back of the head and crest, with a visible gap or shadow at the neck where the mantle meets it. The ears grow from the head, not from the cloak.
2. **Feet.** They read as front-facing. Seen from behind, the feet show their heels toward the camera and the toes point away, toward the window.
3. **Cloak.** It must drape naturally: resting on the seat between Qori and the backrest, falling over the seat edge, and never passing through the wood. No leaves through the bench planks.
4. **Bench.** It must be the exact game bench seen from behind, not a new design. See THE BENCH below. The leaf emblem is carved on the front of the backrest, so it does not show from behind.
5. **Qori is the current Qori.** Not v3. Qori v3 is on hold (Tools\Design\QOLOSSAL_ROADMAP.md, and Assets\QoriRigTest is test-only). Do not use any Batch 19, 19b or 22 to 22f art.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Your round 2 folder: Codex_v2_full\ConceptTests\Qori_Music_Retreat_20261010\ (ART_DIRECTION.md, Rear_View_Revision\NOTES.md and both rear concepts). The round 1 front views drew the parked v3 Qori; do not use them for Qori.
3. The current Qori: the live rig at Assets\Art\Characters\QoriRig\ (QoriRig.prefab, QoriRigData.json, Parts\), and the assembled reference Codex_v2_full\Batch23b\Integrator\Context\Actors\Qori_144PPU.png. Read the proportions from these.
4. The bench: Assets\Art\Codex\Town\Town_Qvale_ListeningBench.png (432x228).
5. The room: Assets\Art\Codex\Town\Interior_Qvale_HomeA_Room_v3.png (2560x1440) for the sage plaster, pale timber, herb bundles, ceramics and amber lantern.
6. The valley: Assets\Art\Codex\Backgrounds\BG_Qvale_Overlook_Vista.png (4096x1440) for the pale limestone, olive and cypress, terraced hills, the winding river and the small settlements.

PART A: THE REAR-VIEW DRAWING OF QORI (do this first; Part B uses it exactly)

One sheet, `Retreat_Qori_Rear_Sheet.png`, 2560x1440, plain flat background, no text on the art (labels in the handoff instead). It shows the current Qori:
- **Standing, from behind**, full body, at the same height as the current rig's standing Qori placed beside it for scale (the rig's side view, read from the files above, drawn on the same feet line).
- **Seated, from behind**, on the bench (THE BENCH below), at the rear three-quarter angle Part B uses.
- **Seated, from behind, straight on**, as a check that the head, ears and shoulders are symmetrical in structure.

Match the current rig's proportions: head size against the body, ear length against the head, cloak length, and the thin twig limbs. Measure them from the rig parts, and list the ratios you matched (head height to total height, ear length to head height, shoulder width to head width) in the handoff, measured on both the rig and your drawing. Keep the current palette: olive leaf ears with pale undersides, olive leaf hair and crest, the layered leaf cloak, bark-brown limbs.

From behind, show: the back of the crest and head as one rounded shape; both ears rising from the head with their backs (green) toward the camera and only a sliver of pale underside; the neck gap; the cloak falling in layers from the shoulders; the heels and backs of the feet. No face. At most a tiny cream cheek edge on the rear three-quarter view.

THE BENCH
Town_Qvale_ListeningBench seen from behind, exactly: the same weathered pale wood; one thick horizontal seat plank; one broad backrest plank with the same gently waved top edge; the two root-twined end posts that run down into splayed root feet; the same small olive leaves and moss patches. Same proportions (the front view is 432x228; keep the seat length to post height ratio). The back of the backrest is plain weathered wood: no emblem, no carving. It is a two-person bench: Qori sits on the left half, leaving the right half empty.

PART B: THE LAYERED SCENE

The composition is your round 2 Rain Window, with the fixes above: a rear three-quarter camera a little above seated head height; Qori and the bench in the lower left, the bench about a third of the frame width and Qori (with ears) about 36% of the frame height; a wide recessed timber-framed window; a Qvale room in sage plaster and pale timber with one herb bundle and one wall lantern on the left; beyond the rain-streaked glass, the real Qvale valley from BG_Qvale_Overlook_Vista under blue-grey rain and mist. Warm amber inside, cool valley outside. Painted 2D in the game's style. No 3D render, no castles or towers, no generic fantasy, no hollow-tree room.

**Calm zones for UI.** Keep the right quarter (x 1920 to 2560) and the bottom 10% (y 1296 to 1440) quiet: soft valley, sky, window frame and floor only, no strong detail, no bright highlights, no figures or props. Nothing important in those zones on any layer.

**Canvas.** Every layer is a 2560x1440 RGBA PNG (the size of Interior_Qvale_HomeA_Room_v3), all registered to each other, so stacking them in order at (0,0) gives the finished scene. Straight (non-premultiplied) alpha, no halo at cut edges. 16:9.

**Layers, back to front** (names start `Retreat_RainWindow_`):

| # | File | Contents | Notes |
|---|---|---|---|
| 01 | `_Sky` | Rain sky only, opaque, full canvas | Painted to every edge, also where the frame and room will cover it |
| 02 | `_FarValley` | Far hills, ridges and distant settlements, transparent above | Painted wider than the window opening, so a small parallax shift never shows an edge |
| 03 | `_NearValley` | Nearer terraces, olive and cypress, river, nearer houses | Same: painted past the window opening on every side |
| 04 | `_Mist` | Low mist banks only, soft, on transparent | Tiles horizontally (left and right edges match), so it can drift |
| 05 | `_RainGlass` | Droplets and short streaks on the glass only, on transparent | Inside the window opening only |
| 06 | `_RainStreaks` | Running rain streaks only, on transparent | Tiles vertically (top and bottom edges match), so it can scroll; inside the window opening only |
| 07 | `_Room` | Window frame, sill, walls, ceiling beam, floor, rug, shelf, herb bundle, pots, and the lantern fitting with the lamp unlit | Opaque except the window opening, which is fully transparent |
| 08 | `_LanternGlow` | The lit flame and its warm glow on the wall and frame only, on transparent | Separate, so the game can flicker it |
| 09 | `_Bench` | The bench from behind, on transparent | Painted whole, also where Qori covers it |
| 10 | `_Qori_Body` | Seated Qori without the head and ears: cloak, mantle, limbs and feet | The neck area under the head is painted fully, so a small head bob never shows a gap |
| 11 | `_Qori_Head` | The head and crest from behind, without ears | Painted fully where the ears cover it |
| 12 | `_Qori_EarFar` | The far ear | Its base painted fully, tucked under the crest |
| 13 | `_Qori_EarNear` | The near ear | Same |
| 14 | `_Foreground` | Optional: one soft out-of-focus leafy plant edge at the lower left, as in round 2 | Keep it out of the calm zones |

The ear layers sit at the order that looks right (an ear may go behind the head); give the order you used in the registration.

Also deliver:
- `Retreat_RainWindow_Composite.png`: all layers stacked, 2560x1440, as the preview.
- `Retreat_RainWindow_Composite_UIZones.png`: the same with the calm zones outlined, for the review only.
- `Retreat_RainWindow_Registration.json`: the layer order, and for layers 10 to 13 a pivot in canvas pixels (the head's pivot at the neck, each ear's at its base) and a safe rotation range (the head ±3°, each ear ±8°) inside which no gap or cut edge shows.
- `Retreat_RainWindow_MotionProof.png`: a strip of the Qori area at head −3° and +3° and each ear at −8° and +8°, proving no gaps.

FOR CLAUDE'S REVIEW
Deliver REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches), a name-collision check of every new name, and a source archive with your prompts, as in earlier batches. In the handoff:
- The proportion ratios (Part A), on the rig and on your drawing.
- How each of the five round 2 faults was fixed, with a crop for each.
- A check that every layer is 2560x1440 and stacks to the composite exactly, that the mist tiles horizontally and the streaks tile vertically, and that the calm zones hold no strong detail.
- Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested". The layer count and sizes are listed for later import planning; make no performance claim. Original Switch is excluded.

Then stop. Claude reviews Part A and Part B together before anything goes into the game.
