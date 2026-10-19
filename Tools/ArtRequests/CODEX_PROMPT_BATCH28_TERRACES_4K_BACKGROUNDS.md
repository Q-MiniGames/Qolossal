# Codex prompt: Batch 28, the Terraces backgrounds at 4K with vertical depth (pilot)

Written 18 Oct 2026. The user is targeting 4K gameplay and approved a pilot: repaint one region's backgrounds at 4K, built for horizontal and vertical parallax, test it in game, then order the other seven regions. Batch 28 and the names below were checked free on 18 Oct 2026 (re-check before sending; the listening bench repaint may claim 28 first). Not sent: the user sends it. Claude reviews the delivery as the next free CLAUDE_REVIEW number (77 on 18 Oct; re-check just before writing).

**Why.** The game now sinks each background plane as you climb (`ParallaxLayer`, 18 Oct): screen speed 1 / depth, with the terrain at 1. The accepted Terraces set (1920x1080, drawn 10 u tall) has two problems:
- At 4K it is stretched 2x.
- It is only one screen tall. On a 146 u climb the near plane would leave the frame after about a third of the region, so the game currently slows the movement to make the art last.

The pilot paints taller planes so the climb can move at close to real speed for the whole region, and adds a high-altitude plane for the top.

---

Qolossal: Codex Batch 28 (the Terraces backgrounds at 4K, with vertical depth). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch28\
Copy your context files into Batch28\Integrator\Context\, as for Batch 14.

READ FIRST, IN THIS ORDER
1. This prompt. It is the request; follow it entry by entry.
2. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. The targets are Windows PC, PS5, Xbox Series X|S and Switch 2. Master art is 4K. The game derives smaller platform copies through import settings, so paint for 4K and don't make your own platform variants. Mark every device test "not tested".
3. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md, sections 2 (the mystery rules), 4, 5 and 10–11.
4. Tools\ArtRequests\QOLOSSAL_ASSET_REQUESTS_BATCH14_BACKGROUNDS.md and CODEX_PROMPT_BATCH14.md: the depth-plane rules this batch inherits.
5. Tools\Backgrounds\depth_prototype\ (compare_stills.jpg and the two .webp clips) and Tools\Backgrounds\proposal\mattes\MR03_mid_foreground_rgba.png.
6. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_23.md: why the accepted Terraces set is the model for structure, wrap and edge quality.
7. Tools\IncomingArt\Codex_v2\VerticalParallax_Mock_Terraces.jpg and _v2.jpg: the accepted set sinking as you climb, and where it runs out.
8. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest, currently 1226 assets. Re-read it; don't trust the count.

STYLE AUTHORITY
- The accepted BG_Sky_Terraces, BG_Terraces_Far, BG_Terraces_Mid, BG_Terraces_Near and BG_Terraces_Mill_Landmark (Assets\Art\Codex\Backgrounds\).
- Direction A, Morning Herbarium: warm apricot dawn, limestone, olive greens, soft hazed distance.
- This is a repaint at higher resolution and greater height of an approved look, not a new direction. The same skylines, landmarks, palette and haze per plane must read as the same place.

SCALE
- 216 px per world unit. The 4K frame is 10 u tall (3840x2160).
- Plane depths, unchanged: Far 25, Mill landmark 14, Mid 10, Near 5. The terrain is 1.

WHAT TO MAKE (5 PNGs, RGBA 8-bit sRGB, bottom-centre pivot, Repeat U / Clamp V unless noted)

The accepted mill landmark stays: at 800 px for 2.8 u it is already sharper than 4K needs. Paint the new planes so it still sits on the far valley between Far and Mid.

| Name | Size | What it is |
|---|---|---|
| BG_Terraces_Near_4K | 3840x5400 (25 u) | The hillside being climbed: valley-floor terraces at the bottom, rising through olive terraces, dry-stone walls and scrub to limestone outcrops and a ridge skyline near the top. |
| BG_Terraces_Mid_4K | 3840x4320 (20 u) | The valley's far side: farmland and villages low, slopes and cliffs higher, its skyline in the top fifth. |
| BG_Terraces_Far_4K | 3840x2808 (13 u) | The distant ranges and the snow peak, hazed. The same silhouette as the accepted Far. |
| BG_Terraces_High_4K | 3840x2160 (10 u) | Above the valley: a sea of soft morning cloud and mist with only far peak tips through it. It fades in over the far plane near the top of the climb, so it has no hard bottom edge (alpha fades out over its lowest 30%). |
| BG_Sky_Terraces_4K | 3840x5184 (24 u) | The sky, apricot dawn low, shifting gradually to a paler, cooler blue-apricot high. No horizon, land or sun disc, and no clouds that read as objects. Repeat U. |

TALL-PLANE RULES (Near, Mid, Far; new in this batch)
- Each plane is opaque from its skyline down to its bottom edge. As you climb, the game slides the whole plane down the screen, so every part of its height is seen.
- Any 3840x2160 window at any height and any horizontal offset must read as a natural view at that plane's distance:
  - no second horizon, no band where the painting changes style, scale, haze or light;
  - no horizontal seams, stacked strips or repeated rows;
  - the same detail density from top to bottom.
- Height reads as altitude, not distance: lower = lower on the same slopes. Scale and haze stay those of the plane's depth all the way down.
- Near and Mid skylines are irregular and climb and dip across the width. At least once per width each has a saddle that dips at least 12% of its height below its highest point, so farther planes and sky show through while the near hills still stand high. The skyline height is equal at both ends (±2 px) for the wrap.
- The top 2160 px of each tall plane, where its skyline is, must match the accepted plane's composition when scaled to the same width: same silhouette family, same landmarks, same haze.

NON-NEGOTIABLES (inherited from Batch 14)
- Depth independence. Each plane stands on its own at any offset:
  - nothing continues across planes;
  - a skyline is a natural silhouette, never a cut through an object;
  - no holes under nearer planes.
- Depth-offset proof: neighbouring planes offset by 0, 640 and 1280 px horizontally, and by 0, 1080, 2160 and 3240 px vertically.
- Seamless wrap on every Repeat U plane, at every row of the full height:
  - the mean RGBA difference between column 0 and the last column is 0;
  - the adjacent-column derivative matches;
  - the skyline row is equal at both ends (±2 px).
- Mystery rule. Before the final level, nothing may read as a hand, finger, knuckle, nail, knee, lap, chest, rib, shoulder, face, brow, eye, lid or lash:
  - no five parallel rounded ridges;
  - no rounded dome hills;
  - no symmetric pairs.
  - The taller planes give more room for this to slip, so check every 2160 px window.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep all accepted files byte-identical (the _4K files are new; the accepted Terraces set stays as it is) and include a preservation receipt.
- Paint at native size, or downscale from a larger native image. Never upscale.
  - Archive each native output with its exact prompt, seed and settings in Batch28\sources\<name>\.
  - If a canvas is too tall to generate in one piece, paint it in overlapping sections and blend them. Show the join rows in the QA sheet; they must pass the no-seam rule above.
- No text in any image. Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem) before delivering.

PROOFS (at 3840x2160, plus 1920x1080 downscales)
- The climb strip: the full set stacked over BG_Sky_Terraces_4K at five climb positions (0, 25, 50, 75 and 100% of a 146 u climb).
  - Each plane sunk by climb / depth, with Near and Mid starting with their skylines above the frame, so that Near's skyline reaches the frame's bottom at 100%.
  - BG_Terraces_High_4K fading in over the last 30%.
  - Repeat the strip with MR03_mid_foreground_rgba.png laid over it. Qori and the route must read at least as clearly as in the current capture at every position.
- The depth-offset proof.
- A 3x horizontal repeat of each Repeat U plane, with its seam numbers at the top, middle and bottom rows.
- Window checks: each tall plane cut into 2160 px windows at every 540 px step, as a contact sheet, with the mystery check (10% size and blurred) for each window.
- Over-black and over-white alpha checks; the High plane's fade shown over the Far plane.
- The accepted set and the 4K set side by side at the same frame, for the style match.

DELIVERABLES
- The 5 PNGs.
- Batch28\MANIFEST.json: sha256, size, PPU (216), pivot, wrap, and the depth each plane is painted for.
- The QA sheet: edge, skyline and join numbers, alpha, window and mystery checks, and all the proofs.
- The name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes (texture size and memory per plane at 4K, and the downscale each platform would need) and device tests (all "not tested") separate.

Work the planes in parallel, then integrate. If anything here is ambiguous or impossible, for example a canvas height the generator can't hold together, write it in the handoff instead of guessing.
