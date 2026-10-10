Qolossal: Codex Batch 16c (one umber ground for Qvale, outside and in: a Qvale street ground, and the five rooms turned side-on). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16c\
Copy your context files into Batch16c\Integrator\Context\, as for Batch 16b.

WHY
Two things, one fix.
1. The Qvale street is too bright. It borrows the Causeway's pale sand ground (Causeway_Fill and Causeway_Top_Strip), which is about as bright as Qori's body, so it pulls the eye and Qori doesn't stand out on it.
2. The rooms still have a deep floor. In Batch 16 and 16b the back wall meets the floor about 150 px above the walk line, so the door, furniture, ladder and loft posts stand on one depth and Qori walks on another, in front of them. Batch 16b kept that (registration copied unchanged).

The user picked the "dollhouse cutaway" layout for the rooms: everything stands on the walk line, a thin lit floor edge runs along it, and below it you see the ground in section. The user also wants the street ground and the rooms' ground to match. Claude's in-game mock-up of the colour is Tools\MountainReliefAtlas\QA\QvaleInteriors\ground_mock\ground_umber_mock_sheet.jpg. Read it as a colour target only; the flat recolour has no texture and the room mock still has the deep floor.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. The mock-up sheet above, and Tools\MountainReliefAtlas\QA\QvaleInteriors\batch16b\ (the 16b Old Man room in the game: Qori by the door, at a resident spot, on the sleeping loft).
3. Your Batch 16b delivery: Batch16b\Rooms\*\ and Batch16b\QA\COLOUR_GRID.png. Each home's 16b palette, ceiling and wall are the starting point and stay as they are above the floor.
4. Tools\ArtRequests\CODEX_PROMPT_BATCH16B_INTERIOR_COLOUR.md, the SIDE-ON FLOOR section. Batch 16b was sent an earlier copy without it; this batch carries it out.
5. The accepted Causeway ground: Assets\Art\Codex\Terrain\Body\Causeway\Causeway_Top_Strip.png and Causeway_Fill.png, and their entries in Tools\ArtImport\codex_v2_accepted.json. The new Qvale pair copies their size, layout and import settings exactly. Only the colour and material change.
6. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1158 files today). Re-read it; don't trust the count.

THE GROUND (shared by the street and the rooms)
Warm umber earth: packed soil with fine grain, small pebbles and a few flat stones, in sRGB:
- mid-tone (the average of the soil): about #7A5A3E;
- darks (stone gaps, shadow under the edge): down to about #4A3426;
- grain highlights no brighter than about #9A7854.
Keep the texture low-contrast, so the ground reads as one calm band under the characters. No moss, roots or plants on the soil itself. Nothing body-like (no skin, folds, pores or flesh tones; it's earth).

The lit edge on top (the line Qori walks on) is the brightest part of the ground, about #CDBB98, a little less bright than today's cream kerb.

WHAT TO MAKE (7 new PNGs, no accepted file replaced; names checked free on 9 Oct)

A. Qvale street ground, two files under Terrain\Body\Qvale\:
- Qvale_Top_Strip: 1024 x 512, RGBA, 120 PPU, bottom-centre pivot, Repeat U, Clamp V, seamless left to right. Same rows as Causeway_Top_Strip so the walk line doesn't move: transparent above about row 40; grass tips and the moss line about rows 40 to 90; the kerb (one course of laid stones, toned to about #CDBB98, moss in the joints) about rows 90 to 155; the umber soil from about row 155 down, fading out over rows 460 to 512 exactly as the Causeway strip fades into its fill.
- Qvale_Fill: 1024 x 1024, RGBA (opaque), 120 PPU, bottom-centre pivot, Repeat U and V, seamless both ways. The same umber soil as the strip's lower part, so the strip's fade joins it with no visible line.

B. The five rooms, side-on, as _v3 under Rooms\:
Interior_Qvale_HomeA_Room_v3, Interior_Qvale_HomeB_Room_v3, Interior_Qvale_HomeC_Room_v3, Interior_Qvale_OldMan_Room_v3, Interior_Qvale_Loft_Room_v3.
- Start from each home's 16b painting: keep its palette, ceiling, walls and upper-wall clutter.
- The back wall's base sits ON the walk line, y 1280. The door sill, the bottoms of all furniture, the stove, the ladder feet, the loft posts and the stair stand on y 1280, the same line Qori walks on. No receding floor planks, no floor perspective, no cast shadows that suggest depth in front of the wall.
- Along y 1280, a thin lit floor edge, about 8 to 14 px tall, in the home's own floor material (HomeA scrubbed boards, HomeB dark stained boards, HomeC clay tiles, OldMan limestone, Loft sandstone), all at about the brightness of #CDBB98, so the walk line reads the same in every home and outside.
- Below the edge, the 160 px to the bottom of the canvas is the umber ground above in section: a footing of a few flat stones set in umber soil. Same colours in all five rooms and on the street. Keep the stones dark and quiet; they must not pull the eye from the room.
- Each room keeps one clear light source and calm mid-tones in the band from the floor edge up to two Qori heights.

NON-NEGOTIABLES
- Registration is frozen in X and against the walk line. Each _v3 room keeps its accepted Batch 16 REGISTRATION.json: walk line at y 1280, the same door X, the same resident spot X, and the raised walkable lines (OldMan's sleeping loft deck and ladder rungs, the Loft's three stair treads) at exactly the same pixels. Only the door rect's bottom moves down to y 1280. Write each _v3 REGISTRATION.json with that one change, and prove the match with an overlay of the old registration on each new painting.
- Rooms: 2560 x 1440, RGB, opaque, 144 PPU, bottom-centre pivot, Clamp. Qori is about 247 px tall.
- Same content: each home keeps its furniture, now standing on the walk line. Keep tall pieces off the door and the resident spots, because Qori walks in front of them.
- Still rooms seen from inside: no exterior door, awning, sky or street. No painted characters, no text. Mystery rule: nothing body-like, in the rooms or the ground.
- No visible panel joins. The two ground tiles must tile with no seam at 3 x 3 repeats.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale, never resample X and Y independently. Archive each native output with its exact prompt, seed and settings in Batch16c\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).

PROOFS
- Ground: the strip over the fill, tiled 4 across and 2 down, at 1:1; a 3 x 3 tiling of each file; a colour sample of the soil mid-tone, dark and edge against the targets above.
- Street: the accepted Qvale street with the Causeway ground swapped for the Qvale pair (Qori and a house front on the walk line), at 1920 x 1080 and 1280 x 720, next to the same frame with the Causeway ground.
- Rooms: the five _v3 rooms side by side with the five 16b rooms; each room with the accepted Qori and resident sprites on the registered spots, and Qori on OldMan's loft and the Loft's top tread, at 1920 x 1080 and 1280 x 720; a 4x crop of Qori's feet on the floor edge at 720p in every room.
- One sheet with a street frame and a room frame side by side, showing the ground matches.
- The registration overlay per room.

DELIVERABLES
- The 2 ground PNGs, the 5 room PNGs and their _v3 REGISTRATION.json files (door bottom moved to the walk line, nothing else changed).
- Batch16c\MANIFEST.json (sha256, size, PPU, pivot, wrap).
- The QA sheet with all the proofs, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
