# Codex brief: Batch 50b-r2, the Overgrown Nursery: B04 turns natural

10 Nov 2026, Claude. **This replaces the earlier 50b-r2 brief of the same name (FAR and light pass), which was never sent.** Route names A-D; B04 = internal C04.

Read first: `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_109.md` (its five fixes still apply), the 50b and 50b-r1 briefs (technical rules unchanged), and section 1 below.

Deliver into `Codex_v2_full\Batch50\50b-r2\`. Never overwrite earlier files. No scene edits, prototype folder `Assets/Art/Prototypes/KeepersNursery_Batch50/` only, mipmaps off, BC7, read back the imported format. Unity: if another session is using it, stop and ask the user.

**Two stages with a stop between them.** r2a is concepts only. r2b paints at production density, and only after Claude has reviewed r2a and the user has said go.

---

## 1. The direction change (user-approved, 10 Nov)

The underground is a journey **from the people's world into the titan's own**. In the lore, people forgot the titan was alive and built on it. The deeper the player goes, the less of their work is left and the more the living world has taken over:

| Route | Built : natural | Look |
|---|---|---|
| A Cistern | mostly built | Unchanged. Done. |
| **B Garden** | **about 1 : 2** | **The people's garden swallowed by raw rock and living roots. Only fragments are left.** |
| C Wind | traces only | Wind-carved natural caverns; cairns, ropes, chimes. |
| D Descent | almost none | Ancient stone, wood grain, mint glow, silence. Ambiguous, older than people. |

**For Route B this means:**
- **Natural rock is the base.** Uneven cave rock with root-split cracks, soil, moss and living roots is most of every frame. Masonry appears only as broken fragments: a piece of wall, a buried arch, a cracked terrace edge.
- **Built remnants are few and half-swallowed.** At most one or two per camera frame: a planting bed, jars wedged in a root-split niche, a bent trellis, a lantern niche cut into the rock. **Drawers become rare:** the seed archive is the room's single drawer landmark, partly buried by rock and roots. No other drawer bank in B04.
- **Light shifts from the people's to the living world.** Fewer lanterns, as the last remnants of the Keepers. More mint root-glow and lilac blooms, which now carry most of the colour. Mint still points toward the Heart.
- **The mystery rule still holds, and matters more now.** Natural forms are where body reads creep in. No veins, no ribs, no skin-like smooth surfaces, no limb-like roots, no fleshy masses. Roots are irregular, branching and knotted. Rock is rock: fractured, layered, angular.

## 2. Stage r2a: concepts (no production files)

Over the real terrain plates already captured in 50b-r1, at concept resolution, labelled **CONCEPT**:

1. **B04 centre** (1596, 278) and **B04 upper left** (1574, 296), painted to section 1.
2. **A revised route strip, B01-B08**, at about 1 : 2 built to natural, so the user can see the whole route. B06 is the brightest bloom room; B08's sealed gate is the one big built structure left, grown through with roots, with mint and lilac in its seams (lilac kept out of floor to floor + 6 u).
3. **A one-row A-B-C-D strip**: one thumbnail per route showing the gradient from built to natural. A uses an existing Route A capture; C and D are rough mood thumbnails only.
4. Greyscale, mystery (full, 10%, blurred) and reserved-shape checks for the two B04 concepts.
5. **A kit plan:** which 50b and 50b-r1 pieces stay, which are retired, and the new pieces section 3 needs, with sizes and memory.

**Stop.** Claude reviews; the user says go.

## 3. Stage r2b: production (after go)

**Repaint and add** to match the approved r2a concepts. Expected changes (confirm or adjust in your r2a kit plan):

| File | u (w × h) | px | Notes |
|---|---|---|---|
| NK_Wall_Rock_01, _02 | 10 × 10 | 2160 × 2160 | **New base wall.** Opaque, seamless both axes. Natural cave rock, root-split cracks, soil and moss. Two variants that alternate |
| NK_Rock_Buttress_01, _02 | 2 × 12 | 432 × 2592 | New. Irregular rock columns, replacing the squared piers in B04. Top into the ceiling, base hidden |
| NK_Rock_Overgrowth_01, _02 | 6 × 4 | 1296 × 864 | New. Rock and root masses that bury built pieces (overlay the seed archive's edges, masonry fragments) |
| NK_Niche_RockJars_01-03 | 4 × 3 | 864 × 648 | New. Squat jars wedged in root-split rock niches. Replaces NK_Shelf_Jars in B04 |
| MID_B04_SeedFall | 8 × 12 | 1728 × 2592 | **New MID landmark, replacing the granary tower in B04:** where a great root mass has broken through the ceiling, carrying soil, seed jars and lilac with it. Hangs from above; its top is hidden in the ceiling. Its lower end is ragged roots and soil fall, never a ledge, and stays out of the 4.5 u band above every deck across the camera range (prove with a sweep) |
| FAR_B04_Terraces | 43 × 28 | 9288 × 6048, six 3096 × 3024 tiles | **Repaint** (same name and tiles): a natural cavern receding into black, with rock columns, root curtains, soil slopes, a few buried terrace fragments and the last far lanterns. Review 109 fixes 1, 2 and 5 apply: no ledges, FAR objects at about half the rear-wall scale, low contrast with slight cool haze, P99.9 15-25% of the walk rim |
| LM_B04, LM_B04_Lilac, LM_P_B03_B04, LM_P_B04_B05 | as 50b-r1 | as 50b-r1 | Re-lit: radial pools only (Review 109 fix 3), fewer lantern pools, more mint and lilac |

Kept from 50b/50b-r1: SET_B04_SeedArchive (now partly buried by NK_Rock_Overgrowth), NK_Roots_Curtain, NK_Bloom_Cluster, NK_Trellis_Panel (bent, sparse), NK_Niche_Lantern (fewer), NK_Mound, FG_NK_Roots, FG_NK_Trough, the tunnel and shaft ribs, and the mouth arches. **Mouth arches and tunnel ribs:** make them read as broken masonry framing a natural rock tunnel, or replace them with rock versions; say which in the kit plan. Retired from B04: NK_Wall_Masonry / _02 as the base, NK_Pier, NK_Drawer_Bank, NK_Shelf_Jars, MID_B04_GranaryTower (keep the files; they may fit B07, the Keepers' last refuge, in 50c).

Every name must pass the global name check. All sizes are multiples of 4 px, at most 4096 per side, 216 px/u, never upscaled. Keep the route under its 222 MiB budget and report the new total.

**r2b QA:** everything from 50b-r1, plus: the four concept side-by-sides (now against the r2a concepts), the FAR ledge sweep and the MID SeedFall sweep, a check that no kit piece appears twice in one frame unless flipped, cropped or partly hidden, and a built-to-natural area estimate per camera (target about 1 : 2).

Deliver `MANIFEST_50b-r2.json`, the QA, and `REVIEW_HANDOFF_50b-r2a.md` / `_r2b.md` (verdict per item; re-check the next free CLAUDE_REVIEW number; don't write a review). Leave the preview ready for the user to play.

**Stop after r2b.** Claude reviews and the user plays before 50c.

---

## r2a review result (10 Nov, Claude Review 110): conditions for r2b

The natural direction is approved. **Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_110.md`.** Its six conditions are automatic fails in r2b:

1. No outdoor read: depth ends against cave walls, columns or hanging rock and fades to black, never ridgelines into haze or bright tunnel ends.
2. No scale-like rock: no uniform cell or crackle pattern, no overlapping plate or shingle slabs. Rock is fractured, layered and gritty.
3. Mint only at root tips and in cracks where a visible root is, never as branching lines on stone.
4. No parallel or braided root pairs.
5. Within 4.5 u above any deck, natural rock tops slope more than 30° or stay unlit and broken (covered by the FAR and MID sweeps).
6. Single flowers 0.2-0.4 u.

**Measurement rule:** painting may use image generation. Every r2b check (value gate, sweeps, repetition, built-to-natural area, imported format, concept side-by-sides) must be computed from actual Unity captures and the delivered PNGs, not generated images.
