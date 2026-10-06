Qolossal: Codex Batch 14 (backgrounds with real depth). Start now and stop once, at the end, for Claude's Review 25.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch14\
Copy your context files into Batch14\Integrator\Context\, as for Batch 13.

READ FIRST, IN THIS ORDER
1. Tools\ArtRequests\QOLOSSAL_ASSET_REQUESTS_BATCH14_BACKGROUNDS.md: the request. It's authoritative; follow it entry by entry.
2. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md: the targets are Windows PC, PS5, Xbox Series X|S and Switch 2. Mark every device test "not tested".
3. Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md, sections 2 (the mystery rules), 4, 5 and 10–11.
4. Tools\Backgrounds\BACKGROUND_ENHANCEMENT_PROPOSAL.md, with its proposal\ sheets: the current in-game captures, mockups M1–M5, ref_quake_vistas.jpg, and the RGBA gameplay mattes in proposal\mattes\.
5. Tools\Backgrounds\depth_prototype\: compare_stills.jpg and the two .webp clips. They show the depth parallax these layers are made for, prototyped in the Terraces with the accepted BG_Terraces_Far/Mid/Near.
6. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_23.md: why the accepted Terraces set is the model for structure, wrap and edge quality.
7. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest, currently 1132 files. Re-read it; don't trust the count.

WHAT TO MAKE (8 PNGs)
- W1, the Cradle (MR01): BG_Cradle_Far, BG_Cradle_Mid, BG_Cradle_Near.
- W2, the Summit (MR07): BG_Summit_Far, BG_Summit_Mid, BG_Summit_Near.
- W3, Qvale (MR04): BG_Qvale_VaultRim_Mid (a 2560x720 repeating band) and BG_Qvale_Overlook_Vista (4096x1440, non-repeating).
- Every set is one 2560x1440 painting split by depth onto shared, registered canvases: 144 PPU, bottom-centre pivot, Repeat U / Clamp V, RGBA 8-bit sRGB. No sky: the game uses the accepted BG_Sky_Terraces behind them.
- Direction A, Morning Herbarium: warm apricot dawn, limestone, olive greens, hazed distance. Keep the distant valley towns.

NON-NEGOTIABLES
- Depth. The planes now move at different speeds (Far 25, Mid 10, Near 5; the terrain is 1) and zoom differently at lookouts. So each plane must stand on its own at any offset:
  - nothing may continue across planes;
  - a skyline is a natural silhouette, never a cut through an object;
  - no holes under nearer planes.
  - Deliver the depth-offset proof from the request: neighbouring planes offset by 0, 320 and 640 px, and ±48 px vertically.
- Seamless wrap on the repeating layers: the mean RGBA difference between column 0 and the last column is exactly 0, the adjacent-column derivative matches, and the skyline row is equal at both ends (±2 px).
- Mystery rule. Before the final level, nothing may read as a hand, finger, knuckle, nail, knee, lap, chest, rib, shoulder, face, brow, eye, lid or lash:
  - no five parallel rounded ridges;
  - no rounded dome hills;
  - no symmetric pairs;
  - the Summit's lake and reed beds stay out of the background planes (they're terrain);
  - from the Qvale overlook, the Terraces are stepped farmland on ordinary slopes.
  - Add a per-asset mystery check (10% size and blurred) to the QA sheet.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep all 1132 accepted files byte-identical, and include a preservation receipt.
- Paint at native size or downscale from a larger native image. Never upscale.
  - Archive each native output with its exact prompt, seed and settings in Batch14\sources\<name>\.
- No text in any image. Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem) before delivering.

PROOFS (at 1920x1080 and 1280x720)
- Each set stacked over BG_Sky_Terraces at three horizontal offsets.
- The depth-offset proof.
- A 3x horizontal repeat of each repeating layer, with its seam numbers.
- Over-black and over-white alpha checks.
- Each set behind its region's gameplay matte (proposal\mattes\MR01_mid, MR07_mid, MR04_mid, MR04_overlook_seated). Qori must read at least as clearly as in the current capture.

DELIVERABLES
- The 8 PNGs.
- Batch14\MANIFEST.json (sha256, size, PPU, pivot, wrap).
- The QA sheet: edge and skyline numbers, alpha, mystery checks, all the proofs.
- The name-check log and the preservation receipt.
- REVIEW25_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

Work the three workstreams in parallel, then integrate. If anything in the request is ambiguous or impossible, write it in the handoff instead of guessing.
