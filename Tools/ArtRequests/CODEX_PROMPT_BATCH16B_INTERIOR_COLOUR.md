Qolossal: Codex Batch 16b (Qvale rooms: one colour world per home, and a side-on floor). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16b\
Copy your context files into Batch16b\Integrator\Context\, as for Batch 16.

WHY
The five accepted Batch 16 rooms work, but they share one shell and one palette: the same honey timber ceiling, tan plaster wall, brown floor and wall lamp. Inside, every home looks the same. Batch 16b gives each home its own colours, ceiling and walls, so you know whose house you are in at a glance. It also turns the floor side-on, to match the 2D street (see SIDE-ON FLOOR).

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Your Batch 16 delivery: Batch16\Rooms\*\ (the paintings and each REGISTRATION.json) and Batch16\QA\BATCH16_FULL_GRID.jpg (the sameness to fix).
3. The accepted street fronts and cutaways in Assets\Art\Codex\Town\ (Town_Qvale_*_Closed and _Cutaway): each home's character.
4. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_29.md: the Batch 16 review.
5. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1158 files today). Re-read it; don't trust the count.

WHAT TO MAKE (5 new PNGs, no accepted file replaced)
Interior_Qvale_HomeA_Room_v2, Interior_Qvale_HomeB_Room_v2, Interior_Qvale_HomeC_Room_v2, Interior_Qvale_OldMan_Room_v2, Interior_Qvale_Loft_Room_v2.

Each home gets its own palette, ceiling and wall. All stay inside Direction A, Morning Herbarium (warm, natural, earthy, no neon or saturated primaries), but no two may share a dominant wall colour or a ceiling type:
- HomeA, the herbalist: sage-green limewash walls, pale ash beams, a floor of light scrubbed boards. Accents in lavender, dried-herb gold and olive. The feeling is cool, fresh and airy. Fill the upper wall with drying racks and hanging bundles.
- HomeB, the mapmaker: parchment-cream walls, slate blue-grey painted timbers, a dark stained floor. Accents in map inks (indigo, faded teal) and ochre. The feeling is studious and orderly. Fill the upper wall with pinned unlettered maps, rolled charts and a plan chest.
- HomeC, the residents' home: terracotta and rose plaster, honey-oak beams, a floor of warm clay tiles. Accents in woven reds, mustard yarn and cream. The feeling is busy and lived-in family warmth. Fill the upper wall with woven hangings, a plate rack and baskets.
- OldMan: the ceiling is the rock underside of his roof (pale limestone with moss at the cracks), not timber. Walls of grey-white limewash on stone, a deep walnut floor and furniture. The stove's ember orange is the only warm light, so this is the dimmest, most evening-like room. Fill the upper wall with books, old tools and keepsakes.
- Loft, the musician: a cave room, with a golden sandstone ceiling and walls curving into each other and no straight beams. Floor and stair of worn sandstone. Accents in deep teal and plum textiles with brass instruments. The feeling is cosy, rounded and resonant.

SIDE-ON FLOOR (the second fix)
In Batch 16 the floor is a deep perspective plane: the back wall meets the floor about 150 px above the walk line, so the furniture, door and ladder stand on one depth and Qori walks on another, in front of them. That's a 3/4 stage, not a 2D side view, and it doesn't match the side-on Qvale street.
- The back wall's base (skirting) sits ON the walk line, y 1280. The door sill, the bottoms of the furniture, the stove, the ladder feet, the loft posts and the stair all stand on y 1280, the same line Qori walks on.
- Below y 1280, the 160 px to the bottom edge is the floor seen edge-on: the cut face of the floor (board ends over a joist, or a stone sill and footing for OldMan and the Loft), like the street's ground strip. No receding planks, no floor perspective, no cast shadows that suggest depth in front of the wall.
- The raised lines keep their height above the walk line, so they move down with the wall base.

Each room keeps one clear light source, and keeps its mid-tones calm where Qori walks (the band from the floor up to 2 Qori heights) so Qori and the residents read on top. Put the colour in the walls, ceiling and upper-wall clutter, not in the walking band.

NON-NEGOTIABLES
- Registration is frozen in X and against the walk line. Each _v2 room keeps its accepted Batch 16 REGISTRATION.json: walk line at y 1280, the same door X, the same resident spot X, and the same raised walkable lines (OldMan's sleeping loft and ladder, the Loft's three stair treads) at the same X and the same height above the walk line. Only the door rect's bottom moves down to the walk line. Write each _v2 REGISTRATION.json with that one change, and prove the match with an overlay of the old registration on each new painting.
- Same canvas: 2560x1440, RGB, opaque, 144 PPU, bottom-centre pivot. Qori is about 247 px tall, as in Batch 16.
- Same content: each home keeps its Batch 16 furniture, now standing on the walk line (more may be added on the upper wall). Qori walks in front of the furniture, so keep tall pieces off the door and the resident spots.
- Still a room seen from inside: no exterior door, awning, sky or street. No painted characters, no text. Mystery rule: nothing body-like.
- No visible panel joins.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale, never resample X and Y independently.
  - Archive each native output with its exact prompt, seed and settings in Batch16b\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).

PROOFS
- A colour grid: the five _v2 rooms side by side at the same size, next to the Batch 16 grid. Each home must be identifiable from colour alone at thumbnail size.
- A palette strip per room: its 6 dominant colours (wall, ceiling, floor, two accents, light).
- Readability: each room with the accepted Qori and resident sprites on the registered spots, at 1920x1080 and 1280x720.
- The registration overlay per room.

DELIVERABLES
- The 5 PNGs and their _v2 REGISTRATION.json files (door bottom moved to the walk line, nothing else changed).
- Batch16b\MANIFEST.json (sha256, size, PPU, pivot).
- The QA sheet with all the proofs, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
