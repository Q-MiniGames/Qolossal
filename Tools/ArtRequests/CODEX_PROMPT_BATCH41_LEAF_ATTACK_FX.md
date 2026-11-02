BATCH: 41

Qolossal: Codex, leaf effects for Qori's Leaf Staff attacks and his double jump. One sheet of separate effect sprites; the game spawns,
times, scatters and scales them in code. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch41\

## 0. Before you start (all must hold, or stop and say which failed)
- The Batch41 folder does not exist yet or is empty. Never overwrite a delivery.
- Every output name in section 3 is free: case-insensitive, against Tools\ArtImport\codex_v2_accepted.json (accepted
  names and sub-sprite names) and every stem under Assets\.
- Re-read codex_v2_accepted.json and record its count. Don't trust any number in this prompt.

## 1. Read first
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md (PC, PS5, Xbox Series X|S, Switch 2; 60 FPS target;
   mark device checks "not tested").
2. The look to match: Qori's Ludo frames in Assets\QoriWalkTest\ArtLudo\ (Combo1, Combo3, ChargeRelease, UpAttack:
   the leaf cloak, its olive greens, cream undersides, warm brown veins and soft dark outline) and the current
   effects in Assets\Art\Codex\FX\ and Assets\Resources\FX\Slash\ (sizes and how strips are unrolled).
3. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md section 2 (the mystery rules: no faces, no anatomy, no text).

## 2. The goal
The attacks feel weak because nothing shows the force of a swing. These effects must be **clearly visible at game
size** (Qori is about 1.7 world units, about 370 px tall on a 4K screen) over both the light terraces and the dark
underground, and must read as **leaves torn loose by the staff**, not magic or fire. Bold shapes, strong silhouettes,
a soft dark outline like Qori's, readable in one frame. Mint glow only where section 3 says (the charge): mint means
life in this world.

## 3. Outputs (RGBA 8-bit sRGB PNG, transparent, nothing touching the canvas edge: at least 8 px clear margin)

| File(s) | Canvas | What it is | Used for |
|---|---|---|---|
| FX_Leaf_Single_01..06 | 96x96 each | Six single leaves, different shapes and turns (side-on, three-quarter, curled, edge-on), each centred | Scattered from every hit and swing, spun and tinted in code |
| FX_LeafSwipe_Light_Strip | 1024x192 | The swing trail unrolled flat, left to right: thin fading tail on the left, a dense leading edge of overlapping leaves on the right; top edge is the staff tip side | Combo hits 1-2, up attack (the code bends it along the staff's path) |
| FX_LeafSwipe_Heavy_Strip | 1024x256 | As above, thicker, more and bigger leaves, a brighter leading edge | Spin finisher, sprint lunge, charged sweep |
| FX_LeafSwipe_Arc_01..04 | 512x512 each | The same light swipe drawn as a ready curved arc (about 140 degrees, sweeping clockwise, top-left to right), 4 frames: forming, full, breaking into leaves, fading | Fallback where a bent strip isn't used |
| FX_LeafSwipe_UpArc_01..04 | 512x512 each | The up attack's swipe: an arc sweeping from behind and below, up over the top and down in front (about 200 degrees, the top of the arc at the canvas top), 4 frames: forming, full, breaking into leaves, fading | Up attack (the staff passes over Qori's head) |
| FX_LeafStreak_Dive_01..04 | 192x512 each | The plunge's dive: a vertical streak of leaves and wind trailing straight up from a point at the canvas bottom-centre (where the staff tip leads), dense at the bottom, thinning and scattering toward the top; 4 frames that loop while Qori falls | Plunge, during the dive |
| FX_LeafPuff_AirJump_01..05 | 320x192 each | The double jump's push-off in mid-air: a flat, downward-opening fan of leaves and a soft air ring under a point at the canvas top-centre (Qori's feet), spreading out and down, 5 frames from a sharp pop to drifting and fading | The second jump, under his feet |
| FX_LeafBurst_Hit_01..05 | 256x256 each | A small burst of leaves and leaf flecks from the centre, 5 frames from a tight pop to scattered and fading | Every landed hit |
| FX_LeafBurst_Heavy_01..06 | 384x384 each | A bigger burst with a short shockwave ring of leaves, 6 frames | Spin finisher, charged sweep, plunge landing (it bursts outward along the ground there) |
| FX_LeafSwirl_Charge_01..08 | 256x256 each | Leaves orbiting a centre point, with a soft mint glow at the middle that grows over the loop; frame 8 flows back into 1 | Held around the staff tip while charging |
| FX_LeafRing_Release_01..06 | 768x768 each | The charge released: a wide ring of leaves bursting outward with a mint flash at the start, 6 frames | The charged sweep's release |

Frames of one effect share one canvas and one centre (the effect's origin at the canvas centre, except the strips).
Light comes from the upper left, as in the game.

## 4. Rules
- Leaves only: olive, sage and cream with warm brown veins, as in Qori's cloak. No sparks, stars, fire, lightning,
  water or text. No faces or creatures hidden in the shapes.
- Even alpha edges (no halos or white fringes); fully transparent background (alpha 0, RGB 0).
- Don't upscale; paint at the stated size.
- Each multi-frame effect must read as one motion when played at 20 fps.

## 5. Deliver
- The PNGs, MANIFEST.json (file, size, sha256, frame count, origin), PROMPTS.md (what you asked the generator),
  and QA_INDEX.html showing every effect over a light terrace crop and a dark underground crop at 1x game size,
  plus each sequence as a looping strip.
- Then stop for Claude's review. Don't copy anything into the project.
