# Qolossal: asset request for Codex, Batch 11

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

Work through every workstream below in parallel (one agent per workstream plus the integrator, as in Batch 10), and stop only once, at the end, for **Review 22**.

## Notes
- **Folder name:** use `Batch11/` for this run's QA, handoffs and manifests.
- **Read first:**
  - `CLAUDE_REVIEW_21.md` (the Batch 10 verdict, the Terraces background redo and the W5 pick);
  - `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2, sections 10–11);
  - `Concepts/Titan/Batch8_ChalkOrchard_Model_Reference.png` (the canonical seated pose and head);
  - your own Batch 10 W5 sheets in `Concepts/Batch10/` and their sources in `Batch10/W5/Sources/`.
- **The user's pick: direction A, Morning Herbarium, for everything** (warm apricot dawn, olive survey ink). Every workstream below paints in A. Don't carry B's blue-hour palette into anything.
- The art manifest holds **1075** accepted files (Batch 10's 79 are in).

## The rules (unchanged)
- Keep all **1075** accepted files byte-identical (`Tools/ArtImport/codex_v2_accepted.json`).
- Never write under `Assets/`.
- Use deterministic post-processing, and archive native sources and exact prompts.
- Fill in the Batch 6 self-review checklist per workstream.
- Run the **global name check** against the manifest, including sub-sprite names, before delivering.
- **The mystery rule:**
  - Nothing before the final level shows or names the titan's body.
  - Everything that shows the body is labelled a spoiler in its file name or QA sheet, never inside the production pixels.
- **No text baked into production images.**
- **The Leaf Staff** in any painting matches the accepted `Weapons/Qori_Weapon_LeafStaff` exactly: a dark grainy shaft and one broad curved leaf blade about a quarter of its length. Not the branched sapling in the W5 concepts.
- **Qori** matches the accepted rig, including the v2 mantle and cloak.
- **The platform section in each handoff:** source checks, imported-resource notes and device tests marked **not tested**, kept separate. Add 1280×720 readability proofs.

---

## W1: Terraces backgrounds, redo (production, P1)
See Review 21. Keep the accepted paintings' content, and cut each layer to transparent above its own skyline:

| File | Contents | Transparent above |
|---|---|---|
| `BG_Terraces_Far` | distant ridges and haze | the far ridgeline |
| `BG_Terraces_Mid` | the cliffs and valley floor | the cliff tops |
| `BG_Terraces_Near` | the olive-terrace slope | the slope's crest |
| `BG_Sky_Terraces` (new) | the sky alone, opaque | none |

- Same canvas (1920×1080), 120 PPU, bottom-center pivot and wrap as the accepted `BG_A*` / `BG_Qvale_*` layers. Clean alpha edges along the skylines: no sky-colored halo, and no hard crop lines.
- **Proofs:** the four layers stacked at three horizontal parallax offsets; each layer over light, dark and magenta; the stack at 1080 and 720 with Qori at 1.4 u.
- These replace the unaccepted Batch 10 files of the same names, which were never accepted, so this isn't a collision. `BG_Terraces_Mill_Landmark` is accepted; don't touch it.

## W2: The new Chart, round 2 (concepts only, direction A)
Batch 10's C10 patches were bare contour diagrams. Keep A's paper, olive ink, the accepted `Chart_Frame_9Slice` and the canonical final silhouette. Change the following:
- **Each patch is a drawn map of its region,** in survey-ink style, so the player knows the place at a glance:
  - **the Cradle:** the palm basin and the hut;
  - **the Long Causeway:** the road, the aqueduct arches and the root bridge;
  - **the Terraces:** dry-stone terraces, crops and the mill;
  - **Qvale:** the town's houses and lanterns;
  - **the Ribwood:** parallel ridges with trees;
  - **the Windward Heights:** the wind shelves;
  - **the Summit:** the lake, the reeds and the shelves.
  - Small, hand-drawn details: no anatomy, no labels inside the paint.
- **During play, patch outlines must not hint at the final silhouette.** A patch's shape only makes sense once the patches slide together.
- **Deliver:**
  - the 2-, 4- and 6-patch states;
  - the seven-patch final assembly;
  - a three-frame slide-in sequence;
  - a proposed production breakdown, listing each patch's file, canvas, its play-time position and its final-assembly position.
  - Label every final-assembly frame DESIGNER SPOILERS.

## W3: The reveal (production, P1, spoiler)
Paint direction A's C11 Final Reveal as full-frame production stills. Each frame is a native full-frame painting, not a crop or a resample of a 2×2 exploration.

| File | Frame |
|---|---|
| `Reveal_Face_01_Brow` | Qori at the brow, looking down and back; strange land all round |
| `Reveal_Face_02_Pullout` | camera pulled back: the shelves and lake start to read as a brow and a closed eye |
| `Reveal_Face_03_Whole` | the whole bowed face, Qori minute at the brow |

- **Format:** 1920×1080, opaque, 100 PPU (UI still), like `Cinematics/Intro_0*`.
- The head matches the approved Chalk Orchard model: the two-pillar crown, the closed eye shutter, root cords.
- If a source is painted larger, archive the master and deliver 1920×1080.

## W4: Title screen (production, P1)
Direction A's C12 Title Dawn, as parallax layers plus one flat composite:

| File | Contents |
|---|---|
| `Title_A_Sky` | opaque sky and sun |
| `Title_A_Far` | the far valley, the river and mist, transparent above |
| `Title_A_Ridge` | the near moss ridge, the rock and the flowers, transparent above |
| `Title_A_Composite` | everything, including a painted Qori with the correct staff |

- Qori isn't on a layer: at runtime the live rig stands on the ridge. Mark Qori's foot line and position in the registration.
- Leave the upper-left third quiet for the logo, and mark that area in QA.
- **No titan:** the ridge reads as rock.
- 1920×1080, 100 PPU, and the same registration across all four files.

## W5: Ending stills (production, P1, spoiler)
Three full-frame native paintings in direction A. Head Lift and Shoulder Companions are new full-frame paintings, not the Batch 10 letterboxed strips.

| File | Frame |
|---|---|
| `Ending_Careful_01_HeadLift` | the titan lifting its head, dawn |
| `Ending_Careful_02_Lap` | the hand rising with Qvale safe in the lap (from A's Careful Lap) |
| `Ending_Careful_03_Shoulder` | Qori and the **blossom-healed** Wilted on the moss shoulder, looking out |

- **Format:** 1920×1080, opaque, 100 PPU.
- **The healed Wilted** is healed all over: green ears and head leaves, a green skirt, and the Healed mantle and cloak from Batch 10. Batch 10's accepted healed parts kept a withered head and skirt; the painting shows the intended look, and a separate request will follow for those parts.

## W6: Quake vistas (production, P1)
Direction A's seven C14 quake paintings as production stills, one per region:

`Quake_Cradle`, `Quake_LongCauseway`, `Quake_Terraces`, `Quake_Qvale`, `Quake_Ribwood`, `Quake_WindwardHeights`, `Quake_Summit`

- **Format:** 1920×1080, opaque, 100 PPU.
- **What to show:** an earthquake, never a body moving: dust, a tilting road, shifting ledges, roots straining. Qori holds the correct staff.
- **Vary the compositions:** in Batch 10, Qori stood in the lower-left foreground of every frame. Move Qori around: small in the middle distance in at least two frames, and absent in one.
- **The old `StirVistas/Stir_R*` files stay accepted and untouched.** These are new files in a new `QuakeVistas/` folder.

---

## Delivery
- **The integrator:**
  - merges the manifests;
  - runs the 1075-file preservation check and the global name check;
  - writes `REVIEW22_HANDOFF.md`, with one section per workstream, each with its filled checklist and platform section;
  - **builds clean, labelled summary sheets:** a full grid with every frame named. Batch 10's C14 sheets left half the canvas empty.
- **Production workstreams (W1, W3–W6)** deliver finished candidates. **W2** delivers concept sheets only.
- **One stop, at the end:** Review 22.
