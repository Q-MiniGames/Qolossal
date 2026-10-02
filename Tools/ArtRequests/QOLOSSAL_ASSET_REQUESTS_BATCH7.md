# Qolossal: asset request for Codex, Batch 7 (the new direction)

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md) before applying this brief.
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

## 0. What changed, and why this batch is different

After play-testing, the user has **redesigned the world**. Read `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2) first; it replaces parts of `QOLOSSAL_WORLD_DESIGN.md`. In short:

1. **The titan is a mystery.**
   - Players don't know they are on a titan until the **final level, on its face**. There they see it, and the map completes into the whole body.
   - Until then, everything reads as **strange landscape**: rounded ridges, bowls, ravines, cliffs.
   - **No titan in any background**, and no body part visible as a body part at play scale.
   - The anatomy is real but only recognisable in hindsight.
2. **A new titan: seated**, slumped against the valley wall.
   - One hand rests palm-up on the valley floor. That forearm lies over the raised knee.
   - The player climbs from the fingers up the outside (hand, forearm, knee, lap, chest, shoulder, face), then descends inside to the heart.
3. **Scale: 2.5× the v1 numbers.**
   - The hand is about 300 u, so a finger is a ridge about 20 u tall (two screens).
   - The camera shows 10 u of height. Qori is about 1.4 u.
   - A playable placeholder exists: `Assets/Scenes/Proto_Palm.unity`, with screenshots in `Tools/Prototype/PalmCaptures/`. Use it as the geometry reference.
4. **A town, Qvale,** in the warm basin of the lap. It's the hub:
   - shops and upgrades for **Amber** (the currency);
   - homes you enter by **cutaway** (the front wall peels away in place);
   - a **listening spot** where you sit and play collected **song shells**.
5. **New characters.** The most important is **the old man**, who knows the truth and warns Qori not to wake "the mountain".
6. **Side chambers and caves** off the route: small puzzles, weapons to find, people to rescue.
7. **Stirs are unexplained quakes.** The land moves, nobody names it.

**What this means for the art already accepted:**
- **Keep:** Qori, the weapons, enemies, effects, props, mechanics and the A0–A5 tile kits. All of them are still used.
- **On hold, not cancelled:**
  - the old titan master concepts;
  - `Chart_Base` and the Chart region and stir overlays;
  - the stir vistas;
  - the cinematic stills;
  - the A6 backgrounds.

  They were painted for the lying titan that is seen. Don't edit or delete them. Keep every accepted file byte-identical (the manifest is `Tools/ArtImport/codex_v2_accepted.json`, 732 files).

**The rules are unchanged:**
- deliver the full QA package;
- use deterministic post-processing;
- never write under `Assets/`;
- fill in the **self-review checklist from Batch 6, section 2**.

Concept workstreams also note their own checks.

---

## 1. How to run it

Use the same parallel setup as Batch 6:
- one agent per workstream, plus one integrator;
- each stream writes only to its own folders and its own `Batch7/<Wn>/` QA folder;
- the integrator alone edits `DELIVERY_STATUS.json`, `DELIVERY_AUDIT.json` and `CURRENT_ACCEPTED_MANIFEST.json`, and writes `REVIEW14_HANDOFF.md`.

**Stops.**
- **W1, W3 and W4 are concept rounds:** deliver the sheets and stop. The user picks a direction before any production.
- **W2 starts only after the W1 pick.** Until then it delivers only its shape study (section W2, step 1).
- **W5, W6 and W7 go straight through** to finished files.

---

## 2. The workstreams

### W1: The new titan: concept round (STOP; P1)
Write to: `Concepts/Titan/`.

**One sheet with 3–4 distinct directions** for the seated titan. For each direction:

1. **The side silhouette, seated,** in pure black at thumbnail size (256 px tall). It must read as a seated giant: back against a cliff, one hand palm-up on the ground, that forearm over the raised knee, head bowed.
2. **Materials and surface:**
   - what the "skin" is (stone, living wood, earth over stone, or something new);
   - how moss, grass and small trees grow on it **at Qori's scale**, so it reads as overgrown land.
   - The mint life-glow stays (knots, Qori). The rot stays dull purple-brown with ivory thorns, and never glows.
3. **A play-scale mock of the hand**, as in `Proto_Palm`: one 1920×1080 frame at camera height 10 u, with Qori to scale on a finger ridge. **It must not look like a finger.** It should look like a long rounded hill of this material. Include a second frame of the same spot after the reveal, with a light overlay showing the anatomy, so we can judge "readable in hindsight".
4. **The face (the final level):** one 1920×1080 key frame of Qori arriving on the face, the moment of the reveal.
5. **The map reveal:** how the explored regions (separate patches during play) assemble into the whole body at the end. A small sketch is enough.

**Its own checks:**
- no direction shows recognisable anatomy at play scale;
- every silhouette reads as a seated figure at 256 px;
- all frames are at the stated sizes.

### W2: The Cradle body-terrain kit (P1; production starts after the W1 pick)
Write to: `Terrain/Body/`, `Decor/`.

**Step 1 (now, no stop): a shape study.** Paint the prototype's main forms as untextured grey value studies at the exact `Proto_Palm` geometry, to show how the forms read with light and shadow alone:
- the ridge with its dips;
- the bowl;
- the crease ravine;
- the cliff (the heel);
- the fallen pillar (the thumb);
- the dip (the wrist);
- the causeway slope.

Render at camera height 10 u.

**Step 2 (after the W1 pick): the kit, in the chosen material, at 120 px/u.** The game draws body terrain as **arbitrary curved outlines**, not grid tiles, so the kit is:

| ID | Piece | Notes |
|---|---|---|
| BT-01 | `Body_Top_Strip` | a horizontally repeating strip that is laid along any curve; walk line at y = 96, solid below, moss and grass edge above |
| BT-02 | `Body_Fill` | repeating 1024 fill for the body's interior; no landmarks (checklist item 2) |
| BT-03 | `Body_Fill_Shadow` | a darker variant for deep interiors and the far side of the forms |
| BT-04 | `Body_Cliff_Face` | a vertical repeating strip for steep faces (the cliff, ravine walls) |
| BT-05 | `Body_Pale_Cliff` | a smooth pale face where nothing grows (the fingernail and thumbnail). The player should read it as odd pale stone. |
| BT-06 | `Body_Crease_Decals` | 6–8 crease and crack decals of different lengths, for dips and folds |
| BT-07 | `Cave_Fill` + `Cave_Edge_Strip` | the inside of a side chamber (back wall, plus an edge strip for openings) |
| BT-08 | `Pillar_Segment` ×2 + `Pillar_Joint` | the toppling pillar (the thumb), registered to rotate: 35 u and 27.5 u segments, radii 7.5 u and 6.5 u |
| BT-09 | `Decor_Body_Sheet` | 12 named Qori-scale pieces that grow on the body (moss cushions, little ferns, warm-spring vents, root nests, the moss nest Qori wakes in) |
| BT-10 | `Warm_Spring` | a pulsing vent prop, 2 states plus a glow layer (a clue: it pulses in the same slow rhythm everywhere) |

**Proofs:**
- ×3 strip repeats;
- 4×2 fill repeats;
- the top strip laid along a curve with 40° and −40° slopes;
- the whole `Proto_Palm` route re-dressed in the kit at three camera positions.

### W3: Qvale, the town: concept round (STOP; P1)
Write to: `Concepts/Town/`.

**2–3 directions on one sheet.** For each:
- **The town in side view** (a 1920×1080 frame at camera height 10 u, and one pulled out at 25 u). Homes are built into the curved warm ground and from fallen stone, in a sheltered basin. Show warm light, gardens, and a few lanterns.
- **One home in cutaway:** the same home closed, then with its front wall peeled away showing the interior.
- **The listening spot:** a bench under a tree, with the musician's corner.
- **The smithy and the old man's house**, each recognisable from the outside.

Rules:
- nothing in the town shows or names the titan;
- the town has clues only (warm springs, children's chalk drawings of "the mountain who dreams").

### W4: New characters: concept round (STOP; P1)
Write to: `Concepts/Characters/`.

Concept sheets (front and side, scale beside Qori, expression or pose variants) for:

| Character | Brief |
|---|---|
| **The old man** (placeholder name: Grandfather Tallow) | the town's oldest; the only one who knows the truth; kind but frightened; warns Qori, more urgently after each quake. He must read as wise and worried, not villainous. |
| **The smith** | rescued from a side chamber on the causeway; upgrades weapons |
| **The herbalist** | upgrades heart seeds and healing |
| **The musician** | plays at the listening spot; collects song shells |
| **3 residents** | a child, a farmer from the terraces, a lantern keeper |

Use the same creature-folk world as Old Loam and Scribble (already accepted). The species is your choice; offer 2 options for the old man.

### W5: Small pieces the new systems need (no stop; P1)
Write to: `Props/`, `UI/`, `Effects/`.

| ID | Piece | Notes |
|---|---|---|
| N-01 | `Pickup_Amber` | hardened warm sap: small, medium and large; plus `FX_Amber_Collect` (4–6 frames) |
| N-02 | `UI_Icon_Amber` | HUD currency icon, plus a counter frame |
| N-03 | `Pickup_SongShell` | a pale spiral shell, idle shimmer, plus `UI_Icon_SongShell` |
| N-04 | `UI_ListeningSpot` | a simple track-list panel: 9-slice frame, track row, playing and locked states |
| N-05 | `UI_FastTravel` | a Waymark travel panel: 9-slice frame, destination row, a "you are here" marker |
| N-06 | `UI_Dialogue` | a dialogue box (9-slice), a name plate, a continue arrow |
| N-07 | `UI_Shop` | shop panel and item row, with price and "can't afford" states |
| N-08 | `Prop_Chamber_Entrance` ×2 | a crack lip in the ground and a cave mouth in a wall, at the body-terrain scale |

All UI is at 100 px/u. Check readability at 1280×720 (checklist item 11).

### W6: Guardian parts B-03 to B-07 (no stop; P2)
As already planned for Batch 7 (Review 13): body, head and jaw, every attack limb as upper and lower segments, `KnotGlow`, `KnotExposed`, `KnotGlow_Bare`, at 600 px/u, with pivots in a registration JSON and an assembly proof beside Qori. B-07's three states share one canvas.
- **B-05 note:** a clearly larger knot glow, and push the crown-mask and the shrine-door shield so it can't be mistaken for the ordinary Bark Sentinel.
- **Mystery rule:** guardians are local creatures. No guardian should show or name the titan.

### W7: Redos kept from Review 13 (no stop; P1)
- **`A5_Slope_30`:** don't blend two stone layers near the crest. Let the Fill run up under it, and paint transition slabs as single opaque stones.
- **`Shellback_Shell_Cracked_A6`:** recolour the central plates to match `Intact_A6`, and protect only the mint crack glow. The alpha stays identical.

**Cancelled from Review 13** (they were painted for the old design):
- the A6 background redos (`TearglassEye`, `BG_Sky_A6`, `BG_Transition_A6`);
- the `Ending_01_Rise` redo.

The face and ending art will be re-briefed after the W1 pick.

---

## 3. Platform readiness (in the handoff)
Targets: Windows PC, PS5, Xbox Series X|S, Switch 2. Keep these separate:
- source-art checks;
- imported-resource notes (any source larger than 2048 px);
- device tests, marked **not tested**.

Include:
- a 1920×1080 composition proof for every W1 and W3 key frame;
- a 1280×720 readability proof for every W5 UI piece and every guardian's knot glow.

## 4. Delivery and stop
- **Stop after W1, W3 and W4 deliver their sheets,** and after W2's step-1 shape study. Do the rest in parallel: W5, W6 and W7 to finished files.
- The integrator writes `REVIEW14_HANDOFF.md` with one section per workstream, the filled checklists, and the preservation check over all 732 accepted files.
- Review 14 will carry the user's picks for W1, W3 and W4, and the go-ahead for W2's kit.
