# Codex brief: Batch 50b, Keepers' Nursery, the B04 slice (Route B)

9 Nov 2026, Claude. The user said go after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_107.md` passed 50a-r1.

**50b paints the Route B kit core and everything B04 needs, at production density, and shows it in your Editor preview. Then stop.** 50c (the rest of Route B) gets its own brief after 50b is reviewed and played.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (handheld and docked). The original Switch is excluded. Every device test is "not tested" unless you actually ran it.

Route names: A, B, C, D. **Internal IDs are unchanged: B03 = C03, B04 = C04, B05 = C05, B09 = C09.** Use B names in file names and documents. Don't rename anything in data, code or folders.

---

## 0. Before you start (all must hold, or stop and say which failed)

- Run `git status`. Other sessions share this checkout. Touch only the paths this brief names.
- Deliver into `Codex_v2_full\Batch50\50b\`. Never overwrite a 50a or 50a-r1 file.
- Every output name in section 4 is free in the global name check: case-insensitive, against `Tools\ArtImport\codex_v2_accepted.json`, its sub-sprite names and every stem under `Assets\`. Re-read the manifest and record its count. Don't trust any number in this brief.
- Unity: the preview needs it. If another session is using Unity, stop and ask the user. Don't force it closed. **Don't open, edit or save `Qolossal_Underground` or any other scene**, and don't touch `Assets/Scripts/Environment/KeepersCistern/` or `Assets/Art/Underground/Backgrounds/KeepersCistern/` (Route A's installed art).

## 1. Read first

1. `CODEX_PROMPT_BATCH50_KEEPERS_NURSERY_ROUTE_B.md` (theme, palette, mystery and readability rules, rooms), `CODEX_PROMPT_BATCH50A_R1_KEEPERS_NURSERY.md` (the fixes and the lilac decision), and Reviews 106 and 107.
2. `CODEX_PROMPT_BATCH49_KEEPERS_CISTERN_ROUTE_A.md` sections 3, 4 and 7: layers and motion, automatic fails, method. They apply unchanged.
3. Reviews 97 to 105, and what 49b-r1 to 49d-r1 changed. Every fix Route A needed is an automatic fail here (section 3).
4. Your Route A preview code, `Assets/Art/Prototypes/KeepersCistern_Batch49/Editor/KeepersCisternPreview*.cs`, and the installed Route A folder for finish, density and light-map format (for example `LM_A04.png`, 1024 × 576 for a 64 × 36 u room).
5. Your approved concepts: `Batch50/50a-r1/B50ar1_CONCEPT_B04.png` is the target look for B04.

## 2. The slice (world units; rects from the data, internal IDs in brackets)

| Space | Rect or path | What it needs |
|---|---|---|
| **B04 Seed Cradle (C04)** | 1564, 270, 64 × 36 | Rear wall, SET, MID, FAR, FG, light map. The hub. |
| **B03-B04 passage (C03_C04_main)** | path (1652, 320) → (1652, 314) → (1604, 314) → (1604, 306), width 4; rect 1594, 298, 68 × 30 | Bent: a short vertical drop from B03, a horizontal run, a vertical drop into B04's ceiling. Shaft ribs on the vertical legs, tunnel ribs on the run. Light map. |
| **B04-B05 tunnel (C04_C05_main)** | (1628, 286) → (1644, 286), width 4; rect 1620, 276, 32 × 20 | Horizontal tunnel. Light map. |
| **B05 lower section (C05)** | 1644, 270 to 1660, 300 | Rear wall only, so the tunnel opens into a finished shaft. The rest of B05 is 50c. |
| B04-B09 optional (C04_C09_optional) | (1564, 294) → (1534, 294), width 3 | **Mouth arch only** on B04's side; the passage itself is 50c. |

**B04's mechanism is the latch valve now in the scene, not the old pull weight.** Nothing in the background may look like a valve, a lever, a pull weight, a bounce pod, a thread ring, thorns, moss or a ledge. Before painting, list every gameplay object in B04 and the passages (decks, anchors, valve, collectibles, enemies, doors) from the scene data, with world positions, and keep painted shelves, jars and furniture clear of each by at least 1.5 u.

**B04 camera positions for every check:** (1596, 278) as in 50a-r1, plus (1574, 296) upper left, (1618, 296) upper right and (1574, 276) lower left. All are inside the camera clamp (x 1572.9-1619.1, y 275-301).

## 3. Automatic fails (in addition to Route A's section 4)

- Any lit or pale horizontal top edge within 4.5 u above any surface Qori can stand on, in any layer except terrain. This includes shelves, drawer tops, jar ledges, terraces, broken walls and furniture. **(Review 103.)**
- A painting whose bottom ends in a straight cut, unless it tucks behind terrain. Use your own irregular alpha contour. **(Review 104.)**
- Rectangular light or shading bands where one placement ends; shading must be continuous across placements. **(49d.)**
- A furniture or jar shelf overlapping a collectible or anchor. **(49d A07.)**
- Repeated wall shading or kit rhythms readable as a pattern at 1280 × 720.
- Rose or pink anywhere. Terracotta on any surface larger than 2 × 2 u. A bloom with a bright centre, or two blooms as a pair.
- A trellis panel that reads as climbable: regular, lit, unbroken, or within 2 u of a moss strip.
- Any texture side that isn't a multiple of 4 px, or exceeds 4096 px. Upscaling, stretching or duplicating rows.

## 4. Files (RGBA 8-bit sRGB PNG, 216 px/u unless marked)

Prefixes: `NK_` = Nursery kit (rear wall), `FG_NK_`, `MID_`, `SET_`, `FAR_`, `LM_`. Numbered variants are separate files. Route A's unused pieces (bedrock, root crawls, glows) are deliberately left out. Light comes from the light maps.

| File | u (w × h) | px | Count | Notes |
|---|---|---|---|---|
| NK_Wall_Masonry | 10 × 10 | 2160 × 2160 | 1 | Opaque. Seamless in both axes. Dark dressed stone: moss-black, slate, umber. Root cracks allowed, no pattern |
| NK_Band_Terrace | 10 × 3 | 2160 × 648 | 1 | Tiles horizontally. Dark planting-terrace fronts for the lower wall; ragged, growth-covered top, **no lip, no lit edge** |
| NK_Rib_Tunnel | 1 × 5 | 216 × 1080 | 3 | Squared stone-and-bronze tunnel frame on the back wall: 4 u clear + 0.5 u tucked into floor and ceiling |
| NK_Rib_Shaft | 5 × 1 | 1080 × 216 | 2 | The same, for vertical passage legs |
| NK_Mouth_Arch | 3 × 6 | 648 × 1296 | 3 | Where a tunnel opens into a room: `_01` facing left, `_02` facing right, `_03` for B04-B09's 3 u passage |
| NK_Pier | 1.5 × 12 | 324 × 2592 | 2 | Squared masonry pier with a capital. Base hides behind terrain or a mound, top in the ceiling |
| NK_Lattice_Span | 16 × 4 | 3456 × 864 | 1 | Ceiling lattice span; top edge tucks into the ceiling terrain; roots push through |
| NK_Drawer_Bank | 6 × 4 | 1296 × 864 | 2 | Seed drawers, dark, some open and spilling. No lit top |
| NK_Shelf_Jars | 4 × 3 | 864 × 648 | 3 | Squat lidded jar groups on a shelf, as approved in 50a-r1. Shelf edge dark and broken. Only above the 4.5 u band or inside a recess |
| NK_Niche_Lantern | 2 × 3 | 432 × 648 | 3 | Wall niche with a Keeper lantern (warm amber) |
| NK_Roots_Curtain | 4 × 6 | 864 × 1296 | 3 | Hangs from the ceiling; branching, irregular; mint-glowing tips |
| NK_Bloom_Cluster | 2 × 2 | 432 × 432 | 3 | Lilac clusters of small open flowers, soft own-mask edge; different shapes |
| NK_Trellis_Panel | 4 × 6 | 864 × 1296 | 2 | Dark bronze (`#4A4230` region), broken, partly overgrown |
| NK_Mound | 6 × 2 | 1296 × 432 | 2 | Soil and rubble with growth; straight bottom edge (hidden). Hides MID bases |
| FG_NK_Roots | 4 × 6 | 864 × 1296 | 2 | Near-black silhouettes from the ceiling |
| FG_NK_Trough | 3 × 5 | 648 × 1080 | 1 | Near-black hanging planter on chains, from the ceiling |
| SET_B04_SeedArchive | 12 × 12 | 2592 × 2592 | 1 | The great seed wall: drawer banks, jar niches, roots, lilac; the composition from the approved concept |
| MID_B04_GranaryTower | 6 × 13 | 1296 × 2808 | 1 | Stacked squat jars in a squared timber-and-stone frame; base hidden by a mound or terrain |
| FAR_B04_Terraces | 43 × 28 | 9288 × 6048 master | 6 tiles | Tiles `_r0c0`…`_r1c2`: 3 columns × 2 rows of **3096 × 3024**, row 0 at the bottom. Dark planting terraces and drawer walls stepping back into black; a few far lanterns and lilac points; **no lit lips** |
| LM_B04 | 64 × 36 | 1024 × 576 (16 px/u) | 1 | Same format and use as `LM_A04` |
| LM_P_B03_B04 | 68 × 30 | 1088 × 480 (16 px/u) | 1 | Covers the passage rect |
| LM_P_B04_B05 | 32 × 20 | 512 × 320 (16 px/u) | 1 | Covers the tunnel rect |

FAR size check: 17.778 + (64 − 17.778) × 0.5 + 2 = 42.9 → 43 u wide; 10 + (36 − 10) × 0.6 + 2 = 27.6 → 28 u high. If a motion value changes, recompute before painting.

**Memory estimate:** about 92 MiB at BC7 without mipmaps, of which the FAR is 53.6 MiB. That's within the route's 222 MiB budget. Record the real numbers.

## 5. Import and preview

- Copies go in `Assets/Art/Prototypes/KeepersNursery_Batch50/` only. Import: **RGBA Compressed BC7** on Standalone, PS5 and GameCore Xbox; **mipmaps off**; bilinear; Clamp, or Repeat only on a tiling piece's tiling axis. Record Switch 2 as "not set" unless you have its module documentation.
- **After import, read back each texture's actual format in the Editor and record it.** Route A showed RGBA32 despite BC7 overrides until mipmaps were turned off. If any 50b texture isn't BC7, stop and report it.
- Add a **Keepers' Nursery** mode to your Editor preview, following the KeepersCisternPreview pattern, for B04, both passages, B05's lower section and the B04-B09 mouth, at the fixed motions. No scene edits, isolated save, fully reversible. **F8 toggles the rear-wall kit and FG, with MID and FAR unchanged.**

## 6. QA (deliver with numbers)

- Captures at 3840 × 2160 and 1920 × 1080 at the four B04 cameras, plus: a glide or swing across B04 (5 frames over 2 s), a walk through the B04-B05 tunnel (5 frames), the descent through the B03-B04 passage (5 frames) and B05's lower section; each with F8 on and off.
- Value gate on every capture, the same method as 50a-r1: P99.9 ≤ 70% of the walk rim, no bright patch over 0.05 u².
- Reserved-shape check (section 3's lists) at every B04 camera, with the gameplay-object list from section 2 overlaid.
- Mystery check: full, 10% and blurred, background only.
- Seam sheet: every FAR tile join at 100%, every tiling kit piece in a 3 × 3 repeat, and a 40 × 20 u wall built from the kit, to show it doesn't read as wallpaper.
- Proof that every FAR tile reassembles byte-identically to its master.
- Imported format and memory per texture (section 5).
- A short advancing-Play smoke check in the preview, through B04 and both passages, with captured errors. It is an Editor check, not a hardware test.

## 7. Deliver and stop

`Batch50\50b\`: the PNGs named exactly as section 4, `MANIFEST_50b.json` (per file: sha256, px, u, PPU, layer, motion, pivot, tiling axes, own verdict), `QA_INDEX_50b.html`, `QA_SHEET_50b.md`, `sources\` (layouts, native sections, masks, exact prompts), a preservation receipt (accepted files, manifest, scenes and saves byte-identical; name-check log), and `REVIEW_HANDOFF_50b.md` with your verdict per file, anything ambiguous, and a platform section where untested devices say "not tested". Re-check the next free CLAUDE_REVIEW number, but don't write a review.

**Stop.** Claude reviews; the user plays B04 in the preview and says go before 50c.
