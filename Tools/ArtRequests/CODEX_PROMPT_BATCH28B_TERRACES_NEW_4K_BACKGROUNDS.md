# Codex prompt: Batch 28b, new Terraces backgrounds painted at native 4K (pilot)

Written 18 Oct 2026, after Batch 28 was blocked (Review 77: the image tool returned about 1672x941 whatever was asked; my Near height was also too short). The user: "dont ask for repaint, codex cannot upscale, just ask for new painting, no need to stick to the same art."

So this is new art, not a repaint of the accepted set. The composition is free. It still follows the project's art direction and mystery rules. Nothing is upscaled. Not sent: the user sends it. Claude reviews the delivery as the next free CLAUDE_REVIEW number (78 on 18 Oct; re-check just before writing).

---

Qolossal: Codex Batch 28b (new Terraces backgrounds, painted at native 4K, for vertical and horizontal parallax). Work in two stages. Stop after stage 1 only if it fails; otherwise continue and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch28b\

READ FIRST
1. This prompt. It is the request.
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_77.md and your own Batch28\REVIEW_HANDOFF.md: what blocked Batch 28.
3. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md (targets: Windows PC, PS5, Xbox Series X|S, Switch 2). Master art is 4K; don't make platform variants. Mark every device test "not tested".
4. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md, section 2 (the mystery rules).
5. Tools\Backgrounds\depth_prototype\ and Tools\Backgrounds\proposal\mattes\MR03_mid_foreground_rgba.png: how the planes sit behind the gameplay.
6. Tools\ArtImport\codex_v2_accepted.json: re-read the count. Keep every accepted file byte-identical.

WHAT IT IS
The Terraces (region MR03): a warm Mediterranean mountain valley climbed from the valley floor to a high ridge. Qori climbs about 146 world units. The background shows the climb: at the start you are low in the valley with the terraced hillside rising above you; at the top you look down on the valley, the far ranges and a sea of morning cloud.
- New paintings. The composition, landmarks and skylines are yours to design. Don't copy the accepted BG_Terraces_* planes.
- Keep the art direction: Direction A, Morning Herbarium. Warm apricot dawn, limestone, olive greens, dry-stone terraces, cypress, soft hazed distance, painterly not photographic.

HOW THE GAME USES THEM
- 216 px per world unit. The 4K frame is 3840x2160 (17.8 x 10 u).
- Each plane slides down the screen as Qori climbs, at climb / depth (the terrain is depth 1). Each plane also repeats left to right and moves horizontally at 1 - 1/depth of the camera.
- Behind everything is the sky, locked to the camera.

WHAT TO MAKE (5 PNGs, RGBA 8-bit sRGB, bottom-centre pivot, Repeat U / Clamp V unless noted)

| Name | Size | Depth | Travel over the climb | What it is |
|---|---|---|---|---|
| BG_Terraces_Near_4K | 3840x7000 | 5 | 6307 px | The hillside being climbed: valley-floor terraces at the bottom, rising through olive terraces, dry-stone walls and scrub to limestone outcrops and a ridge skyline in the top 700 px. At the start the frame shows the bottom 2160 px; at the end its skyline sits at the frame's bottom. |
| BG_Terraces_Mid_4K | 3840x3900 | 10 | 3154 px | The valley's far side: farmland, a river and a village low, slopes and cliffs higher, its skyline in the top 700 px. |
| BG_Terraces_Far_4K | 3840x2160 | 25 | 1261 px | Distant ranges with one snow peak, hazed. The skyline is in the top third, and land continues to the bottom edge. |
| BG_Terraces_High_4K | 3840x2160 | 25 | fades in over the last 30% of the climb | A sea of soft morning cloud and mist below the viewer, with only far peak tips through it. Alpha fades to exactly 0 over the lowest 30% of the canvas, so it has no bottom edge. |
| BG_Sky_Terraces_4K | 3840x3240 | locked | none | The dawn sky: apricot low, paler and cooler higher. Soft, with no clouds that read as objects, and no horizon, land or sun disc. |

PAINTING AT NATIVE SIZE (the Batch 28 blocker)
- Never upscale, stretch, duplicate rows or fabricate detail by resampling. Every pixel is painted at its final scale.
- If the generator returns smaller images, build each canvas from native sections at 1:1 scale:
  - overlap neighbours by at least 25%, and paint each new section continuing from its neighbours (outpainting / inpainting from the real pixels);
  - blend across the overlap;
  - show every join in the QA sheet.
- A painting tool or your own painted code (brushwork, layered shapes, procedural texture) is allowed, as long as the result is painterly and native.
- **Stage 1 (feasibility):** make BG_Terraces_Far_4K first. If you can't reach a native 3840x2160 that passes the rules below, stop. Deliver it with a handoff that says what limits you, and don't attempt the rest.

RULES
- Tall planes: opaque from the skyline down to the bottom edge.
  - Any 3840x2160 window at any height and offset reads as a natural view at that depth: no second horizon, and no band where the style, scale, haze or light changes.
  - No horizontal seams or repeated rows.
  - The same detail density from top to bottom.
  - Lower means lower on the same slopes.
- Near and Mid skylines are irregular, with at least one low saddle per width, so the farther planes and the sky show through.
- Depth independence: each plane stands on its own at any offset. Nothing continues across planes, a skyline never cuts through an object, and there are no holes.
- Seamless wrap on every Repeat U plane at every row: the mean RGBA difference between column 0 and the last column is 0, the adjacent-column derivative matches, and the skyline row is equal at both ends (±2 px).
- Clean alpha: no translucent terrain, no white or coloured fringes. Check over black, white and magenta.
- Mystery rule. Before the final level, nothing may read as a hand, finger, knuckle, nail, knee, lap, chest, rib, shoulder, face, brow, eye, lid or lash:
  - no five parallel rounded ridges;
  - no rounded dome hills;
  - no symmetric pairs.
- No text in any image. Run the global name check before delivering.
- Never write under C:\UnityProjects\Qolossal\Assets. Include a preservation receipt.
- Archive every native output and section with its exact prompt and settings in Batch28b\sources\<name>\.

PROOFS (3840x2160, plus 1920x1080 downscales)
- The climb strip: all planes stacked over the sky at 0, 25, 50, 75 and 100% of the climb, each sunk by the travel in the table, with High fading in over the last 30%. Repeat it with MR03_mid_foreground_rgba.png on top; Qori and the route must read clearly at every position.
- Depth-offset proof: neighbouring planes offset by 0, 640 and 1280 px horizontally.
- A 3x horizontal repeat of each plane, with seam numbers at the top, middle and bottom rows.
- A window contact sheet for each tall plane, in 2160 px windows every 540 px, with the mystery check (10% size and blurred) per window.

DELIVERABLES
- The 5 PNGs and Batch28b\MANIFEST.json (sha256, size, PPU 216, pivot, wrap, depth, travel).
- The QA sheet with all numbers and proofs.
- The name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with source checks, imported-resource notes (memory per plane) and device tests (all "not tested") kept separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
