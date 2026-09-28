# Qolossal: asset request for Codex, Batch 6 (everything that's left, in parallel)

Batch 5 settled the world: the titan's design, the Chart, the knots and veins, the characters, the puzzle mechanics, the first guardian and the A5 kit are all accepted. The remaining work is mostly **more of what's already defined**, so this batch is organised differently from the last ones:

- **Six independent workstreams.** Run them in parallel with separate agents. None depends on another, except where a stream says so.
- **Only two kinds of stop remain:** a new guardian's concept, and the music's motif (already delivered; its choice is below). Everything else goes straight through to finished files.
- **You check your own work** against the self-review checklist (section 2), which collects every problem found in Reviews 04–12. Deliver a filled-in checklist with each workstream.
- **One review at the end:** Review 13 covers the whole batch.

Read first: `Tools/Design/QOLOSSAL_WORLD_DESIGN.md` (especially sections 1, 3.4, 4, 6, 10 and 11), the Batch 5 request `QOLOSSAL_ASSET_REQUESTS_BATCH5.md` for the Phase 5–7 and music briefs referenced below, and the v2 request `QOLOSSAL_ASSET_REQUESTS.md` for the style blocks, formats and per-area set.

**The rules are unchanged:** keep every accepted file byte-identical (the manifest is `Tools/ArtImport/codex_v2_accepted.json`, 662 files), deliver the full QA package, use deterministic post-processing, and never write under `Assets/`.

---

## 1. How to run it with agents

- **One agent per workstream (W1–W6)**, plus **one integrator**.
- **Each workstream writes only into its own folders** (listed per stream) and its own `Batch6/<Wn>/` QA folder.
- **The integrator alone edits the shared files:** `DELIVERY_STATUS.json`, `DELIVERY_AUDIT.json` and `CURRENT_ACCEPTED_MANIFEST.json`. It merges each workstream's manifest into them. This avoids concurrent edits of the same JSON.
- **The integrator also writes the final `REVIEW13_HANDOFF.md`**, with one section per workstream, and runs the preservation check once over everything at the end.
- **If a workstream hits a real blocker** (a design question only the user can answer), it stops and writes the question into its section of the handoff. The other streams carry on.

---

## 2. Self-review checklist (fill it in per workstream: pass/fail with the number measured)

Each item below has already cost a review round.

**Tiling and terrain**
1. Every repeating strip or fill has **exactly matching opposite edges** (0 difference). Deliver a 4×2 repeat proof for every fill and a ×3 proof for every strip.
2. **Fills have no landmark features.** No single stone, root or crack stands out enough to show the repeat grid in the 4×2 proof (Review 11, A5 Fill). Keep the forms small and evenly sized (none larger than about 200 px on a 1024 tile).
3. **Ground_Top:**
   - it is solid from the walk line, **y = 96** (mean alpha at row 96 about 220, like A1 and A5);
   - its **bottom 64 px feather to zero**;
   - the feather's colour follows the Fill's own pattern, with Fill row 0 under Top row 448.
4. **Wall_Side face**, corner-ledge face and the outer corner are **within 10 px of each other**. The corner's ledge walk line equals Ground_Top's.
5. **Slopes:**
   - a 30° rise with the flat last 200 px at y = 96;
   - the **body painted in the same material as the Fill**, blending into it so no line shows where the plateau overlaps (Review 12, A5 slope; Review 06, A4 slope);
   - no chopped or pasted strokes along the crest; run the column-discontinuity scan.

**Alpha and edges**

6. **No straight or rectangular edges inside organic silhouettes:** no compositing patches or crop boxes left in the alpha (Review 12, A5 foreground frame). Check every transparent file on the light/dark and **magenta** backgrounds.
7. Transparent corners are clear, and there are no halos or fringes on the magenta check.

**Registration and scale**

8. **State variants share canvas, pivot and (where the shape doesn't change) alpha.** Examples: open/closed, off/on, sealed/exposed, and palette variants.
9. **Densities:** terrain, props, decor and mechanics at **120 px/u** (1 QH = 198 px); characters, enemies and guardians at **600 px/u**; UI at **100 px/u** (1 px = 1 reference px).
10. **Rigged parts:** straight along the bone, with rounded overlapping joint caps. Give pivots in a registration JSON, and include an assembly proof beside Qori at the same scale.

**Readability and process**

11. **Check readability at display size:** map overlays at map size, icons at 48 px, FX at game scale (Review 09: the Chart ghosts were invisible at map size).
12. **Never generate from a reduced preview** of a reference. Use the full-resolution file (Review 08, W-01 soft). If a tool fails to read it, say so in the handoff.
13. **Name decor slices descriptively** in the slices JSON (`fern`, `broken_pillar` …), not by number, and give the top-left and the Unity bottom-left rects, as the A1–A4 sheets do.
14. **Signature landmarks go in the background layers, not the decor** (A5's single mill).
15. **Nothing whip-related, ever** (the whip was removed from the game).

---

## 3. The workstreams

### W1: Finish A5 Knee Terraces (small; P1)
Write to: `Terrain/`, `Decor/`.
- **`A5_Slope_30`:** redo it per Review 12, with the body in the new Fill's slab-and-root material and no seam at the plateau.
- **`Decor_Foreground_Frame_A5`:** rebuild it without the rectangular patch in its alpha.
- Re-run the slope/plateau and layered-camera proofs.

### W2: A6 Tearglass Eye, the full area set (large; P1)
Write to: `Terrain/`, `Backgrounds/`, `Decor/`, `Props/`, `Hazards/`, `Enemies/`.

The complete per-area set, **exactly as delivered for A5**:
- **Terrain:** T-01 to T-12, following items 1–5 of the checklist.
- **Backgrounds:** `TearglassEye.png`, `BG_A6_Mid`, `BG_A6_Near`, `BG_Sky_A6`, `BG_Transition_A6`.
- **Decor:** `Decor_A6_Sheet` (12 pieces, named) and `Decor_Foreground_Frame_A6`.
- **Props:** `Portal_Gate_A6` and its membrane, `Checkpoint_Shrine_A6`, `Anchor_Ring_A6`, `Barrier_Rubble_Intact_A6` and its pieces, `Wall_Secret_Overlay_A6`.
- **Hazards:** `Hazard_Thorns_Floor_A6` and `Platform_Moving_A6`.
- **Enemy palettes:** all 29 E-01 to E-05 parts with the `_A6` suffix.

Materials (Batch 5, Phase 5, plus design-doc section 11, item 4):
- the eye socket as a landscape-sized **stone shutter** over a shallow **lens lake**;
- **lash roots** as a reed forest;
- pale blue-white crystal and wet dark slate;
- soft caustic light, calm and dreamlike, readable against a darker background;
- **no open, glowing eye** (it opens only with the Sight stir).

**The stack test is not a stop this time.** Do it first, check it against checklist items 1–5, then continue with the rest. The first stack test goes in the handoff so we can see the process.

### W3: Guardian concepts (P1 for B-03 and B-04, P2 for the rest)
Write to: `Guardians/`.

One concept painting each, as briefed in Batch 5 Phase 6, with the Review 07 and 11 conventions:
- **the region's knot is the weak point**, as one clearly visible glow the fight can expose;
- **every attack limb** can be cut into upper and lower segments;
- **the guardian reads as a local organ of its body part** (design doc, section 11, item 10).

The concepts:
- **B-03 Cistern Matriarch** (R2): the knot tangled in its leaf-frill crown.
- **B-04 Hollowhorn** (R4): mill-wheel horns, terrace stone and clay, with hay and roots for fleece.
- **B-05 Brow Sentinel** (R5): a crown fragment as its shield, from the carved shrine doors.
- **B-06 Glassmoth Queen** (R6): prism wings and a broken shutter-ring.
- **B-07 The Thornheart** (R7): three states in one sheet: wrapped, half torn open, and dying.

Each concept also gets a scale proof beside Qori. **Stop after the concepts:** parts come in Batch 7, once each concept is approved. This is the one stop in this batch.

### W4: Cinematic stills and the title (P2)
Write to: `Cinematics/`.

S-01 to S-06 as briefed in Batch 5 Phase 7, updated to the approved design:
- **the titan is C-01's reclining guardian with the sheltering right arm over the palm**, and a forked **crown-mask, never a human face**;
- **S-03** shows the crown-mask on the horizon, not "a stone face";
- **S-05 Ending_01** follows section 11, item 7: forests shed leaves and soil, the inhabited terraces are held by roots, a huge hand steadies an aqueduct, and **the palm stays raised and safe with Qori in it**;
- **S-06 Title_Background_v2:** Qori in the palm at dawn, the sleeping crown-mask on the far horizon, and the arch of the sheltering arm framing the sky.

All at 1920×1080, opaque, STYLE-BG plus STYLE-TITAN, with the bottom third kept quiet wherever text goes over it (the title and the intro stills).

### W5: Small pieces the game needs next (P1)
Write to: `Weapons/`, `Enemies/`, `Effects/`, `UI/`.
- **`Sentinel_Spear`:** the E-08 Bark Sentinel's missing spear (it's still unpainted), at the Sentinel rig's density. Include an assembly proof in its hand.
- **Ability pieces for the new movement:**
  - `Glidecap_Held.png`: the Glidecap relic opened as a leaf-and-mushroom canopy that Qori holds overhead while gliding. It needs a grip pivot for his hands, at the character density.
  - `FX_Dash_Burst_01…04.png`: a 4-frame flipbook of wind and leaves, for the Wind Leaf dash start.
  - `FX_Dash_Trail.png`: a TILE-H strip.
  - `FX_Lantern_Light.png`: a soft radial light-pool sprite in warm mint, for the Seer's Lantern's radius (512×512 at UI density; the game scales it).
- **`Icon_Weapon_LeafSword` check:** compare it with the in-game sword art (`Assets/Resources/Armory/Weapons/LeafSword.png`). If the icon doesn't match, repaint it to match (**an authorised replacement**). If it matches, say so.

### W6: Music (P1 once the motif is chosen)
Write to: `Music/`.

**The motif choice goes in the prompt that starts this batch.** If the prompt names no motif, W6 does nothing and says so. Once the motif is chosen, deliver in this order, as briefed in Batch 5 ("Music track"):
1. **MU-02, the adaptive waking theme** (8 stacking stems; every first-N combination must sound complete).
2. **MU-10, the heartbeat bed.**
3. **MU-12, the stir stinger**, and **MU-16, the knot awakening.**
4. **MU-03 to MU-06**, the first four region themes, each with explore and tension stems.
5. **MU-11, the guardian battle**, with the lead as a separate stem.
6. The remaining cues: MU-07 to MU-09, MU-13 to MU-15 and MU-17, plus the P3 sound signatures.

Use only instruments licensed for commercial use, and record them in `Music/PROVENANCE.md`. MIDI plus a cue sheet is fine where rendering isn't good enough; say so. If the whole list can't fit, stop after item 4 and say where you stopped.

---

## 4. Delivery and stop

When every workstream is finished (or blocked, with its question written down):
- **The integrator merges the manifests** and runs the preservation check once.
- **It writes `REVIEW13_HANDOFF.md`**, with sections W1–W6. Each section lists the files, the filled checklist, and where to start reviewing.
- **Then stop for Review 13.**

Batch 7 will be the guardian parts (for the approved concepts), then anything Review 13 sends back.
