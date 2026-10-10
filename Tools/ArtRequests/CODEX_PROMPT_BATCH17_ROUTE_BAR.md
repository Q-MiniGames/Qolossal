Qolossal: Codex Batch 17 (route bar HUD icons). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch17\
Copy your context files into Batch17\Integrator\Context\, as for Batch 16.

WHY
The game now has a route bar at the top middle of the play screen: Qori's progress from the region's entry (left) to its exit (right), the metres left, and markers along the way. The bar itself already uses accepted art (HUD_Resource_Bar frame and HUD_Resource_Bar_Fill), Qori uses Map_Icon_Qori, the shrine Map_Icon_AbilityShrine and the Waymark Map_Icon_Waymark. Three things are stand-ins: caves use the treasure-chest icon (Map_Icon_Secret), which reads as loot rather than a side passage; resting places are plain cream diamonds; and the plate behind the bar is a hard-edged dark rectangle.

READ FIRST, IN THIS ORDER
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md: targets are Windows PC, PS5, Xbox Series X|S and Switch 2 (handheld and docked). Mark every device test "not tested".
2. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md, sections 2, 10 and 11: the mystery rules. Nothing may hint at a body or anatomy.
3. Tools\MountainReliefAtlas\QA\RouteBar\route_bar_MR01_1080.png and _720.png: the bar in game today.
4. Assets\MountainReliefAtlas\Scripts\MraRouteBar.cs: how the bar is drawn (sizes in 1920x1080 reference px).
5. The accepted icons beside it, for style and weight: Assets\Art\Codex\UI\Map_Icon_Qori.png, Map_Icon_AbilityShrine.png, Map_Icon_Unexplored.png, Map_Icon_Checkpoint.png, Assets\Art\Codex\Chart\Map_Icon_Waymark.png, and the frame Assets\Art\Codex\UI\HUD_Resource_Bar.png.
6. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1158 files today). Re-read it; don't trust the count.

WHAT TO MAKE (4 new PNGs, new names, no accepted file replaced)
- HUD_Route_Icon_Cave: a found side passage. A small rock arch or cave mouth in pale limestone with moss, dark opening, warm rim light. No treasure, no chest. 256x256 canvas like the Map_Icon set.
- HUD_Route_Icon_CaveUnfound: a side passage not yet found. The same arch silhouette, quieter: faded or dotted outline with a single small glint in the opening. Same canvas, same registration as HUD_Route_Icon_Cave so the game can swap one for the other in place.
- HUD_Route_Pip_Rest: a resting place, small enough to sit inside the bar's channel. A tiny leaf-and-stone mark that reads at 12 to 16 px tall. 64x64 canvas, centred.
- HUD_Route_Backing: a soft dark plate behind the bar, its icons and the metres label. Warm near-black (about #1A140F) at roughly 40 percent opacity, soft feathered edges, no hard rectangle, no border. 512x128 canvas, 9-slice: give the slice borders in px.
- Direction A, Morning Herbarium, as the accepted Map_Icon set: warm limestone, olive greens, timber, mint accents. Same outline weight and lighting as Map_Icon_AbilityShrine.

NON-NEGOTIABLES
- Readability at size: the cave icons are drawn at 44 px and the rest pip at 12 to 16 px on a 1920x1080 screen, and smaller on Switch 2 handheld. Deliver a proof of each at those sizes, on the bright dawn sky (MR01) and on pale rock, next to Map_Icon_AbilityShrine and Map_Icon_Waymark at the same size, at 1920x1080 and 1280x720. Each must be told apart from the shrine and Waymark at a glance.
- The mystery: no faces, eyes, ribs, bones, teeth or anything that reads as anatomy. No text.
- RGBA 8-bit sRGB, transparent background, 100 PPU, centre pivot (the Map_Icon settings). HUD_Route_Backing gets its 9-slice borders in MANIFEST.json.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale, and never resample X and Y independently.
  - Archive each native output with its exact prompt, seed and settings in Batch17\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem) before delivering.

DELIVERABLES
- The 4 PNGs.
- Batch17\MANIFEST.json (sha256, size, PPU, pivot, 9-slice borders for HUD_Route_Backing).
- The QA sheet: the readability proofs above, and a mock of the full bar (frame, fill, Qori, shrine, Waymark, both cave icons, rest pips, backing) at 1920x1080 and 1280x720.
- The name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
