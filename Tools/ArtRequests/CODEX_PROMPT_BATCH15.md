Qolossal: Codex Batch 15 (the remaining backgrounds, plus the Review 25 touch-ups). Start now and stop once, at the end, for Claude's Review 26.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch15\
Copy your context files into Batch15\Integrator\Context\, as for Batch 14.

READ FIRST, IN THIS ORDER
1. Tools\ArtRequests\QOLOSSAL_ASSET_REQUESTS_BATCH15_BACKGROUNDS.md: the request. It's authoritative; follow it entry by entry.
2. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md: the targets are Windows PC, PS5, Xbox Series X|S and Switch 2. Mark every device test "not tested".
3. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md, sections 2 (the mystery rules), 4, 5 and 10–11.
4. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_25.md: the Batch 14 verdicts and the touch-ups (W4).
5. The accepted Batch 14 sets in Assets\Art\Codex\Backgrounds\ (BG_Cradle_*, BG_Summit_*): the model for structure, haze, detail and edges. The new sets must sit beside them as one world.
6. Tools\Backgrounds\BACKGROUND_ENHANCEMENT_PROPOSAL.md, with proposal\current_MR02/05/06_game.jpg and _bgonly.jpg, ref_quake_vistas.jpg, and the new RGBA gameplay mattes in proposal\mattes\ (MR02_mid, MR05_mid, MR05_low, MR06_mid, MR06_encounter).
7. Tools\Backgrounds\depth_prototype\: the depth parallax these layers are made for.
8. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest, currently 1140 files. Re-read it; don't trust the count.

WHAT TO MAKE (9 new PNGs and 5 touch-ups)
- W1, the Long Causeway (MR02): BG_Causeway_Far, BG_Causeway_Mid, BG_Causeway_Near.
- W2, the Ribwood (MR05): BG_Ribwood_Far, BG_Ribwood_Mid, BG_Ribwood_Near.
- W3, the Windward Heights (MR06): BG_Heights_Far, BG_Heights_Mid, BG_Heights_Near. It leads into the accepted Summit set.
- W4, touch-ups, same names, same canvases, swapped in place: BG_Cradle_Far (two skyline smears), BG_Cradle_Near and BG_Summit_Near (stair-stepped crowns), BG_Cradle_Mid and BG_Qvale_VaultRim_Mid (a few of the same steps).
- Every new set is one 2560x1440 painting split by depth onto shared, registered canvases: 144 PPU, bottom-centre pivot, Repeat U / Clamp V, RGBA 8-bit sRGB. No sky: the game uses the accepted BG_Sky_Terraces behind them.
- Direction A, Morning Herbarium: warm apricot dawn, limestone, olive greens, hazed distance. Match each region's accepted Quake vista for light and palette.

NON-NEGOTIABLES
- Depth. The planes move at different speeds (Far 25, Mid 10, Near 5; the terrain is 1) and zoom differently at lookouts. So each plane must stand on its own at any offset:
  - nothing may continue across planes;
  - a skyline is a natural silhouette, never a cut through an object;
  - no holes under nearer planes.
  - Deliver the depth-offset proof: neighbouring planes offset by 0, 320 and 640 px, and ±48 px vertically.
- Natural edges. No stair-stepped or column-block mattes (Review 25's defect). Report skyline jumps of 12 px or more per layer; under 5, each explained.
- Seamless wrap: the mean RGBA difference between column 0 and the last column is exactly 0, the adjacent-column derivative matches, and the skyline row is equal at both ends (±2 px).
- Mystery rule. These regions are the forearm, the chest and the shoulder, so this batch is the strictest yet. Before the final level, nothing may read as any body part:
  - Causeway: no long straight aqueduct, road or ridge across a plane; arches only as short broken fragments, irregularly spaced;
  - Ribwood: no series of parallel walls, slabs or arches; at most one or two irregular broken wall masses per plane; no slow rounded swelling skyline;
  - Heights: no long smooth curving ridge, no rounded shoulder dome; jagged, varied ridges;
  - everywhere: no five parallel rounded ridges, no rounded domes, no symmetric pairs.
  - Add a per-asset mystery check (10% size and blurred) to the QA sheet.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep all 1140 accepted files byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale.
  - Archive each native output with its exact prompt, seed and settings in Batch15\sources\<name>\.
- No text in any image. Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem) before delivering. The five touch-ups keep their accepted names on purpose.

PROOFS (at 1920x1080 and 1280x720)
- Each new set stacked over BG_Sky_Terraces at three horizontal offsets.
- The depth-offset proof.
- A 3x horizontal repeat of each layer, with its seam numbers.
- Over-black and over-white alpha checks.
- Each new set behind its region's gameplay mattes. Qori must read at least as clearly as in the current capture.
- For each touch-up: before/after jump counts, a pixel-difference map against the accepted file, and native 1080/720 crops of each fixed edge.

DELIVERABLES
- The 14 PNGs.
- Batch15\MANIFEST.json (sha256, size, PPU, pivot, wrap).
- The QA sheet: edge, skyline and jump numbers, alpha, mystery checks, all the proofs.
- The name-check log and the preservation receipt.
- REVIEW26_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

Work the four workstreams in parallel, then integrate. If anything in the request is ambiguous or impossible, write it in the handoff instead of guessing.
