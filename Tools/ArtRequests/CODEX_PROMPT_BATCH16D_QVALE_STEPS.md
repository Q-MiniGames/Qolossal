Qolossal: Codex Batch 16d (Qvale street: umber step pieces and a darker kerb). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16d\
Copy your context files into Batch16d\Integrator\Context\, as for Batch 16c.

WHY
Batch 16c gave Qvale its umber ground (Qvale_Top_Strip, Qvale_Fill), now in the game. Two problems remain on the street:
1. Steps. Wherever the street steps down or up, the game draws a cliff kit at the riser: corners at the top of the step, a wall face down it, and a foot where it meets the lower ground. Qvale borrowed the Causeway kit, which is pale sand, so each step showed a pale ghost kerb fading into the umber soil. It has been switched off, so each step is now a raw vertical cut of soil with the kerb ending square. Qvale needs its own kit in the umber soil.
2. The kerb is still the brightest thing on screen. The user wants the kerb stones toned down.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_40.md (Batch 16c review) and your Batch16c delivery.
3. The accepted Causeway cliff kit in Assets\Art\Codex\Terrain\Body\Causeway\: Causeway_Cliff_Face, Causeway_Cliff_Side_L/R, Causeway_Corner_Top_L/R, Causeway_Corner_Foot_L/R, with their entries in Tools\ArtImport\codex_v2_accepted.json. The new Qvale pieces copy their canvas sizes, pivots, wrap modes, PPU (120) and the position of every painted edge exactly. Only the material and colour change.
4. Assets\MountainReliefAtlas\Scripts\MraTerrain.cs (Wall, Emit and the cliff-kit fields): how the pieces are laid. Side strips tile vertically down the riser, the collision line is 64 px from the open edge, corners cap the top of a riser where it meets the strip, feet sit where the riser meets the lower ground and fade out into it.
5. Tools\MountainReliefAtlas\QA\QvaleInteriors\batch16c\: the street and rooms in the game now, and street_MR04_steps_nokit_1080.png (the raw step).
6. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1165 files today). Re-read it; don't trust the count.

COLOURS
- Soil: exactly Batch 16c's (mean about #7A5A3E, darks to #4A3426), so the kit joins Qvale_Fill and the strip's soil with no visible line.
- Kerb stones: a warm, mid limestone, about #B39C78 on average, with darker joints and undersides about #7E6A50. Clearly lighter than the soil, so the walk line still reads, but no longer the brightest thing on screen: the house fronts, Qori and the lanterns should be brighter. Keep the moss and grass on top as they are.

WHAT TO MAKE (8 new PNGs under Terrain\Body\Qvale\, no accepted file replaced; names checked free on 9 Oct)
- Qvale_Top_Strip_v2: Qvale_Top_Strip with the kerb stones retoned to the colours above. Same size (1024 x 512 RGBA), same alpha channel byte for byte, same soil rows, seamless left to right.
- Qvale_Cliff_Face (512 x 1024, Repeat V): the riser wall seen face-on. Umber soil with a few embedded flat stones in the new kerb colour, set low-contrast. No pale rock.
- Qvale_Cliff_Side_L and _R (512 x 1024, Repeat V): the open edge of a riser, the same geometry as the Causeway sides (ragged edge, collision line 64 px from the open side), with the soil and a few stones.
- Qvale_Corner_Top_L and _R (512 x 512): the kerb turning down the riser. The stone course and moss wrap round the corner and stop cleanly, in the new kerb colour, joining Qvale_Top_Strip_v2 without a step in colour or height.
- Qvale_Corner_Foot_L and _R (512 x 384): where the riser meets the lower ground: soil and a few stones, fading into Qvale_Fill exactly as the Causeway feet fade. No pale band, no grass line inside the soil.

NON-NEGOTIABLES
- Every piece matches its Causeway counterpart's canvas, pivot, wrap, PPU and painted edge positions, so the game can use it with no code change except the strip name.
- Nothing pale in the soil: no sand, no limestone faces, no white cracks. Mystery rule: nothing body-like.
- Seamless where tiled: the strip in U, the faces and sides in V, and every piece against Qvale_Fill.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt.
- Paint at native size or downscale; never upscale or resample X and Y independently. Archive native outputs with prompts and settings in Batch16d\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).

PROOFS
- A street mock-up with real steps: the MR04 entry stairs (route points MR04_N00 to MR04_N01, steps of about 1.2 u every 2.4 u, then a slope) built from the new strip, fill and kit, at 1920 x 1080 and 1280 x 720, with Qori (accepted 144 PPU composite) standing on a step. Show it next to the same view with the Causeway kit and with no kit.
- Each piece over Qvale_Fill at 1:1, and the tiled pieces 3 times along their repeat axis.
- Colour samples of kerb, joints and soil against the targets above.

DELIVERABLES
- The 8 PNGs, Batch16d\MANIFEST.json (sha256, size, PPU, pivot, wrap), the QA sheet, the name-check log, the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
