Qolossal: Codex Batch 16 (Qvale house interiors as full-screen rooms). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16\
Copy your context files into Batch16\Integrator\Context\, as for Batch 15.

WHY
The five Qvale homes use their street cutaways as the indoor view: the whole building, rock roof, outside door, awning and steps, floating on black. Cropped or squashed in-game it reads as a mess (outside awning indoors, a huge rock slab over half the room, low ceilings next to Qori). Indoors needs its own art: a room you stand inside.

READ FIRST, IN THIS ORDER
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md: targets are Windows PC, PS5, Xbox Series X|S and Switch 2. Mark every device test "not tested".
2. Tools\MountainReliefAtlas\QA\QvaleInteriors\ (qvale_interiors_before_after.jpg and squash\): what the game shows now and why it fails.
3. The accepted cutaways in Assets\Art\Codex\Town\: Town_Qvale_HomeA/HomeB/HomeC/OldMan/Loft_Cutaway.png. These are the content to keep (furniture, layout, character of each home). Note HomeA, HomeB, HomeC and OldMan were stretched vertically by Batch 8's build_w3.py (PROVENANCE.json resample_xy); use the painted proportions, not the stretched ones.
4. The accepted Qori sprites in Assets\Art\Codex\Player\: the scale reference.
5. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1153 files today). Re-read it; don't trust the count.

WHAT TO MAKE (5 new PNGs, new names, no accepted file replaced)
- Interior_Qvale_HomeA_Room (herbalist), Interior_Qvale_HomeB_Room (mapmaker), Interior_Qvale_HomeC_Room (residents' home), Interior_Qvale_OldMan_Room (old man's home, with his sleeping loft and ladder), Interior_Qvale_Loft_Room (musician's loft, keeping its three-step stair).
- Each is one full-bleed room painting, 2560x1440, opaque, no transparency, no black void. The frame is the inside: back wall, side walls, ceiling (timber beams or the rock underside of the roof seen from below), and a floor that runs edge to edge across the bottom.
- Nothing from outside: no exterior door, awning, outside steps, roof plants, sky or street. The way out is a door painted INSIDE the left wall, seen from inside (closed, warm light under it).
- Keep each home's identity from its cutaway: the same furniture and props, re-staged so the middle of the floor is clear to walk.
- Scale: 144 PPU, bottom-centre pivot. The floor walk line is a flat horizontal row at 160 px from the bottom. Qori (from the accepted sprites, at 144 PPU) is about 1/6 of the frame height; the inside door is 1.6x Qori's height; the ceiling is at least 4x Qori's height above the floor. Furniture is at Qori's scale (a chair seat at his hip).
- Direction A, Morning Herbarium, as the accepted cutaways: warm lamp and hearth light, limestone, olive greens, timber. Each room has one clear light source and keeps the mid-tones calm so Qori and the residents read on top.
- No characters painted in (residents are separate sprites). No text.

NON-NEGOTIABLES
- Registration: for each room write REGISTRATION.json with the floor walk line (px), the inside door rect, raised walkable surfaces as line segments (OldMan's sleeping loft and ladder, the Loft's three stair treads), and 2–3 suggested resident standing spots. All in texture pixels.
- Readability: deliver each room with the accepted Qori sprite and the resident sprite composited at the suggested spots, at 1920x1080 and 1280x720. Qori must read at least as clearly as on the Qvale street.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale, and never resample X and Y independently.
  - Archive each native output with its exact prompt, seed and settings in Batch16\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem) before delivering.

DELIVERABLES
- The 5 PNGs and their REGISTRATION.json files.
- Batch16\MANIFEST.json (sha256, size, PPU, pivot).
- The QA sheet: the readability composites, a side-by-side of each new room next to its accepted cutaway (content kept), and a check that no exterior element remains.
- The name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
