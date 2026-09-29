# Qolossal: asset request for Codex, Batch 5 ("The Qolossal")

> **Platform direction — user decision, 2026-09-29:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. Original Switch is excluded. Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md) before applying this brief. Preserve accepted source bytes; use measured platform-specific runtime settings. This addendum does not reopen approvals or start a new batch.

The game's world has been redesigned around one idea: **every area is part of the body of a sleeping titan, the Qolossal.** Read `Tools/Design/QOLOSSAL_WORLD_DESIGN.md` sections 0–1 and 6 before starting; they are the brief for everything here.

**Unchanged from v2** (`Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS.md`):
- the style blocks STYLE-CHAR, STYLE-WORLD and STYLE-BG (section 1);
- the format blocks CUTOUT, SPRITE, TILE-H, TILE-FILL, CAP and PARALLAX (section 2);
- scale and resolution (QH = 1.65 world units; about 600 px/u for characters and FX, 120 px/u for terrain and props);
- the delivery checklist and the known pitfalls (section 13);
- the rules from the reviews: keep every accepted file byte-identical, full QA package, deterministic scripts, stop at each gate.

**Outstanding from Review 06:** the `A4_Slope_30` redo. Do it first.

**New in this batch: work in gated phases.** Phase 1 is a small set of concept paintings that decide the whole world's shape. **Stop after Phase 1 for Review 07**; nothing else starts until the titan's design is approved. Later phases each end with a stop, as marked.

---

## 0. The Qolossal (the brief every prompt shares)

**STYLE-TITAN** (add this to STYLE-BG or STYLE-WORLD for any image showing the titan or its body)
> The Qolossal: a colossal sleeping titan of ancient pale stone and living wood, so old a whole landscape has grown over it. Stone for bone (knuckles, brow, jaw, kneecaps); bark, moss and forest for skin; glowing mint-green veins of root just under translucent bark. It lies on its back in a misty valley, head to the right, knees drawn up on the left, its left arm resting across its belly with the palm open to the sky. Ruins built by people who never knew it was alive sit on it: aqueducts along the forearm, a shrine sanctuary on the brow, terraced fields on the knees. Parts of it are strangled by a parasitic bramble rot, desaturated purple-brown with ivory thorns, which never glows. Serene, vast, quiet. Hand-painted gouache, the same world as the reference paintings.

**The regions** (use these names and accents consistently):

| ID | Region | Body part | Existing kit | Accent |
|---|---|---|---|---|
| R1 | Palm Hollow | the open left palm and fingers | A0 | mint |
| R2 | Aqueduct Arm | forearm and elbow | A1 | aqua |
| R3 | Breathing Grove | ribs and upper back | A2 | amber |
| R4 | Knee Terraces | bent knees and shins | **A5, new** | ochre and clay |
| R5 | Falls Sanctuary | brow and crown | A3 | pale gold |
| R6 | Tearglass Eye | the closed eye and its socket | **A6, new** | pale crystal blue |
| R7 | Rootdeep Heart | inside the chest | A4 | bright mint on darkness |

**Scale for all titan images:** the titan is about 2,000 Qori tall. A whole playable level is about one finger's width. When a painting shows a region "at game scale", Qori is a speck; include one small figure only where the entry says so.

---

## Phase 1: the big picture (P1). **Stop for Review 07 after this phase.**

| ID | File | Format | Description |
|---|---|---|---|
| C-01 | `Qolossal_Master_Side.png` | 3072×1152 (8:3), opaque, STYLE-BG + STYLE-TITAN | **The master concept.** The whole sleeping titan in side elevation, seen from far away across the valley, filling the width. Every region must be identifiable by shape alone: the open palm on the belly (R1), the forearm with aqueducts running down to the elbow (R2), the forested rib cage (R3), the terraced knees (R4), the shrine on the brow with falls pouring off it (R5), the closed eye (R6). Pale morning mist in the low valley. Small patches of bramble rot. This image fixes the titan's proportions for everything after it. |
| C-02 | `Qolossal_Master_TopDown.png` | 2048×2048, opaque, STYLE-TITAN | The same titan from directly above, as a map-maker would draw it: readable limbs, the ribs' forest, the face. Same pose, proportions and landmarks as C-01. This becomes the base of the in-game map. |
| C-03 | `Qolossal_Region_Key.png` | 2048×2048 | C-02 with each region washed in a flat, clearly different tint and a thin dark outline around each region (no text). It tells us where each region's boundaries are; not used in game. |
| C-04 | `Qolossal_Stir_Poses.png` | 3072×1024, 7 panels in a row | Seven small side-view thumbnails (C-01's framing), showing the titan's pose after each knot wakes: 1 the left hand clenched, 2 the forearm lifted from the ground, 3 the chest risen with a first breath, 4 the knees flexed, 5 the crown in flower, 6 the eye open and glowing, 7 sitting up (the ending). Loose but consistent in proportions. Divided by thin gaps; no text. |
| C-05 | `Qolossal_Details_Sheet.png` | 2048×2048 | A detail sheet at closer range: a stone knuckle with moss, a patch of bark skin with mint veins glowing underneath, the rot wrapping a vein, and a knot (a fist-sized tangle of glowing roots where nerves meet), with no text. It sets the material language for everything below. |

### Phase 1b: your design ideas (P1, delivered with Phase 1)

You know visual design better than we do, so treat the design document as a starting point, not a fixed spec. **Challenge it and improve it.** Deliver:

| ID | File | Description |
|---|---|---|
| D-01 | `ART_DIRECTION_PROPOSALS.md` | Your own proposals, each with a short reason and the design-doc section it would change. For example: a stronger silhouette or pose for the titan; how each region could look more distinct and memorable; how the rot could look more unsettling without becoming horror; ideas for the characters, guardians and creatures; how the stirs could look more dramatic; a visual language for the Chart; anything we missed. Rank them by how much they would improve the game. |
| D-02 | `Qolossal_Alt_Concepts.png` | 2–3 **alternative** titan designs side by side (3072×1024), each a genuinely different take: for example a different pose, a different balance of stone and wood, a different creature (humanoid, beast, or something stranger). Paint C-01 as the design brief describes it; paint these as your own proposals. |
| D-03 | `Region_Moodboards.png` | One small painted mood thumbnail per region (7 in a row, 3072×512): palette, light and one signature landmark each, especially for the two new regions (A5 Knee Terraces, A6 Tearglass Eye). |

We'll choose between the brief's design and your alternatives at Review 07, so make the alternatives real contenders.

Deliver with a `PHASE1_HANDOFF.md` noting any design choices made. **Stop.**

---

## Phase 2: the Chart and knots (P1, after Review 07). Stop for Review 08.

### 2a. The Chart (map screen)

| ID | File | Format | Description |
|---|---|---|---|
| W-01 | `Chart_Base.png` | 2048×2048, opaque | C-02 repainted as a hand-drawn parchment map: fine ink contours over soft watercolour, the titan fully painted. This is what the map looks like when everything is charted. |
| W-02 | `Chart_Fog.png` | 2048×2048, RGBA | The same parchment covered in painted moss and drifting mist, lighter at the edges. The game cuts holes in it as levels are charted. |
| W-03 | `Chart_Region_R1.png` … `Chart_Region_R7.png` | 7 files, 2048×2048, RGBA | For each region, a mask: opaque white exactly over that region's area of `Chart_Base`, transparent elsewhere, with a soft 8 px edge. Registered to W-01 pixel-for-pixel (R7 is the chest interior; draw it as a hidden inner outline). |
| W-04 | `Chart_Vein_Line.png` | TILE-H, 512×64 | A glowing mint root line for vein connections drawn on the map. Seamless horizontally. |
| W-05 | `Map_Icon_Knot_Dormant.png`, `Map_Icon_Knot_Awake.png`, `Map_Icon_Waymark.png`, `Map_Icon_WildVein.png`, `Map_Icon_NPC.png` | 256×256 SPRITE each | In the style of the existing `Map_Icon_*` set (read at 48 px): a closed dim knot / a glowing knot, a small root shrine, a flickering vein, and a snail-shell symbol. |
| W-06 | `Chart_Frame_9Slice.png` | 1024×1024, 9-slice | A carved-bark frame around the map, matching `UI_Panel_9Slice`. Corners must not stretch. |

### 2b. Knots, Waymarks and Vein Gates

| ID | File | Format | Description |
|---|---|---|---|
| K-01 | `Wakeknot_Dormant.png`, `Wakeknot_Awake.png`, `Wakeknot_Glow.png` | SPRITE, same canvas and registration, about 2.5 QH tall | The knot: a heart-sized tangle of root-nerves grown into the chamber wall. Dormant: grey, wrapped in bramble rot. Awake: rot gone, roots glowing mint. Glow: the mint light alone, on its own layer (inside the silhouette). |
| K-02 | `KnotChamber_Decor_Sheet.png` | 1024×1024, packed like `Decor_A*_Sheet` | 10–12 pieces for knot chambers: nerve-root bundles along walls, rot tendrils (3 sizes), mint vein cracks, a root arch, fallen stone. |
| K-03 | `Waymark_Dormant.png`, `Waymark_Lit.png` | SPRITE, same registration, about 1.2 QH | A small root shrine where levels are charted: a curled root holding a closed seed that opens and glows when lit. It should read as related to the checkpoint shrines but clearly different. |
| K-04 | `Vein_Gate_Frame.png`, `Vein_Membrane_Stable.png`, `Vein_Membrane_Wild.png` | SPRITE, same registration, about 3 QH | The portal as a vein: an arch of pulsing root-vessel. Stable membrane: calm mint swirl. Wild membrane: flickering, uneven, broken colours. Area variants later; this one is neutral. |
| K-05 | `Stir_R1_Clench.png` … `Stir_R6_Eye.png`, `Stir_R7_Wake.png` | 7 files, 16:9 1920×1080, STYLE-BG + STYLE-TITAN | The cinematic backdrop for each stir: the moving body part seen from inside the region, huge and close (the hand's fingers curling over the horizon; the forearm lifting the aqueducts; the rib forest rising with breath; the knees' fields sliding; the crown bursting into seed-flowers; the eye opening, light pouring through; the titan sitting up, landscape falling from its shoulders). Keep the bottom third quiet. |

**Stop for Review 08.**

---

## Phase 3: characters (P1 for Old Loam, P2 for the rest). Stop for Review 09.

For each character, **deliver a concept painting first** (`<Name>_Concept.png`), then the CUTOUT parts listed. Parts go straight along their bones with rounded overlapping joint caps, as for the enemies.

| ID | Character | Size | Parts (CUTOUT) | Description |
|---|---|---|---|---|
| N-01 | **Old Loam** | 1.6 QH long | `Loam_Body`, `Loam_Head`, `Loam_EyeStalk` (×1, reused), `Loam_Shell_Shrine` | An ancient, gentle snail. Its shell carries a tiny moss-covered root shrine with a lantern seed. Kind eyes, slow and dignified. Muted cream body, moss-green shell. |
| N-02 | **Scribble** | 0.5 QH | `Scribble_Body`, `Scribble_Head`, `Scribble_Leg` (reused ×6), `Scribble_MapRoll` | An excitable bark beetle cartographer, with ink-stained mandibles, a tiny leaf satchel and a rolled leaf map. |
| N-03 | **Sproutling** | 0.3 QH | SPRITE × 3: `Sproutling_Idle`, `Sproutling_Scared`, `Sproutling_Follow` | Tiny lost seedlings: a cream seed body, two leaf ears, dot eyes. Clearly Qori's kin, but much smaller and simpler. |
| N-04 | **The Wilted** | 1 QH, Qori's rig | **The same part list, canvases and joint positions as the current Qori rig** (`Assets/Art/Characters/QoriRig/Parts/*.png`: Torso, Head_Neutral/Blink/Hurt/Effort, EarUpper/Lower, UpperArm, Forearm, Thigh, Shin, Foot, Skirt, Cape1–3 and Lower), prefixed `Wilted_` | The seed before Qori, taken by the rot: Qori's silhouette, withered grey-brown, leaves curled and cracked, thorns through its cloak, a dim flickering mint core in the chest. Sad rather than evil. **Edit Qori's parts**; don't redesign them, so the rig fits exactly. Plus `Wilted_ThornArmor_Torso`, `Wilted_ThornArmor_Head` for its boss form, and `Wilted_Healed_Torso` for the ending (blossoming). |
| N-05 | **Echo** | effect | `FX_Echo_Wisp.png` (SPRITE), `FX_Echo_Face.png` (SPRITE, same registration) | The titan's dream-voice: a soft mint wisp; the face variant shows a faint, kind, ancient face forming in the wisp. |
| N-06 | **Portraits** | 1:1, 1024×1024 | `Portrait_OldLoam.png`, `Portrait_Scribble.png`, `Portrait_Wilted.png` | Busts for dialogue boxes, matching `Qori_Portrait`. |
| UI-10 | **Dialogue box** | 9-slice, 1024×512 | `UI_Dialogue_9Slice.png`, `UI_NamePlate.png` | A bark-and-parchment speech panel matching `UI_Panel_9Slice`, and a small name plate. |

**Stop for Review 09.**

---

## Phase 4: the new puzzle mechanics (P2)

All SPRITE unless stated, at terrain density (120 px/u), STYLE-WORLD. State variants share one canvas and registration.

| ID | File(s) | Size | Description |
|---|---|---|---|
| M-01 | `Pulse_Bridge_Rest.png`, `_Swell.png`, `_Peak.png` | 3 u wide | A bridge of vein-root that swells with the heartbeat: rest (thin and sagging, not walkable), swell, peak (plump, glowing, walkable). |
| M-02 | `Clench_Finger_Open.png`, `Clench_Finger_Closed.png` | 4 u | A stone-and-moss finger-ridge that curls: open (flat), closed (arched, forming a ramp). |
| M-03 | `Sluice_Gate_Closed.png`, `Sluice_Gate_Open.png` | 1.5×3 u | A carved limestone channel gate with a root wheel, for R2 water puzzles. |
| M-04 | `Tendon_Anchor.png`, `Tendon_Block.png` | anchor 0.7 u; block 2×2 u | A glowing tendon knot to hook the Living Thread onto; a heavy root-bound stone block that can be dragged. |
| M-05 | `Breath_Vent_Inhale.png`, `Breath_Vent_Exhale.png`, `FX_Breath_Current.png` (TILE-V, 256×1024) | vent 2 u | A bark pore in the ground that sucks and blows; a soft painted air current with drifting leaves. (Reuse `Wind_Vent` for stone areas.) |
| M-06 | `Sap_Sac_Full.png`, `Sap_Sac_Burst.png`, `Sap_Bridge.png` (TILE-H) | sac 1 u | An amber sap blister on bark (burst with the sling), and a hardened amber resin bridge strip. |
| M-07 | `Mill_Wheel.png` | 4 u diameter | A root-and-stone wheel with platform paddles, for the Knee Terraces. |
| M-08 | `Crystal_Mirror_0.png`, `_45.png`, `_90.png`, `_135.png`, `Light_Beam.png` (TILE-H), `Light_Receiver_Off.png`, `_On.png` | mirror 1 u | Tearglass crystal mirrors at four angles, a soft light beam strip, and a crystal flower that blooms when lit. |
| M-09 | `Stir_Gate_A0.png` … `Stir_Gate_A6.png`, each `_Closed` / `_Open` | 2×4 u | A body-part gate per region that opens when the titan stirs: finger-bone (A0), aqueduct arch (A1), rib-bark (A2), sandstone (A3), heart root (A4), terrace wall (A5), crystal lash (A6). |
| M-10 | `Relic_SeersLantern.png` + `Icon_Ability_Sight.png` | relic 0.5 QH; icon 256 | The sight relic: a small lantern-seed with an eye-shaped glow, matching the other relics; and its HUD icon. |

---

## Phase 5: the two new regions (P2)

For each of **A5 Knee Terraces** and **A6 Tearglass Eye**, deliver the **full per-area set**, exactly as for A1–A4 in v2 sections 6–9, with the Review 04 terrain conventions (walk line y = 96; the bottom 64 px of Ground_Top feathered; corner faces within 10 px of Wall_Side; the slope's upper end level at the walk line). A stack test with Qori comes first, as before.

- **Terrain kit:** T-01 to T-12.
- **Backgrounds:** the far painting (`KneeTerraces.png`, `TearglassEye.png`), `BG_<A>_Mid`, `BG_<A>_Near`, `BG_Sky_<A>`, `BG_Transition_<A>`.
- **Decor:** `Decor_<A>_Sheet` (12 pieces) and `Decor_Foreground_Frame_<A>`.
- **Props:** `Portal_Gate_<A>` and membrane, `Checkpoint_Shrine_<A>`, `Anchor_Ring_<A>`, `Barrier_Rubble_Intact_<A>` and pieces, `Wall_Secret_Overlay_<A>`, `Hazard_Thorns_Floor_<A>`, `Platform_Moving_<A>`.
- **Enemy palettes:** the E-01 to E-05 variants, `_A5` and `_A6`.

**Materials:**
- **A5 Knee Terraces:** a kneecap of pale stone under terraced fields; ochre clay walls, hay, stone retaining walls held by root sinews; little abandoned windmills. Warm late-afternoon light. The terrain edge shows layered earth over bark.
- **A6 Tearglass Eye:** the eye socket, with long lashes like a reed forest and a crystal lens lake. Pale blue-white crystal, wet dark stone, reflections, soft caustic light. Calm and dreamlike, readable against a darker background.

---

## Phase 6: guardians (P2–P3). Concept first for each, then stop for review, then parts.

| ID | Guardian | Size | Concept brief | Parts after approval |
|---|---|---|---|---|
| B-00 | **Knucklebramble** (R1) | 2 QH | A mass of bramble rot fused into a giant Crawler, rooted in a stone knuckle; three thorn-arms; the Grip knot glows through its back. | body, head and jaw, 3 thorn-arms (upper and lower), the knot glow |
| B-03 | **Cistern Matriarch** (R2) | 3 QH long | A huge ancient Ripple Newt, algae-covered, with a leaf-frill crown; the Reach knot tangled in its frills. | head and jaw, body ×2, legs, tail ×3, frill, knot glow |
| (B-01) | Grove Warden (R3) | delivered | no change | |
| B-04 | **Hollowhorn** (R4) | 2.5 QH | A ram of terrace stone and clay; curled horns like mill wheels; hay and roots for fleece. | head, horns L/R, body, legs ×4 (upper and lower), fleece, knot glow |
| B-05 | **Brow Sentinel** (R5) | 4 QH | A giant stone Bark Sentinel grown into the brow shrine: a sandstone body, a shield of carved shrine doors. | as for the Bark Sentinel, plus `Brow_Shield`, plus a `Brow_Spear` (the E-08 Sentinel is also missing its spear: please add `Sentinel_Spear`) |
| B-06 | **Glassmoth Queen** (R6) | wingspan 4 QH | A crystal Gust Moth with prism wings that throw light. | body, wings front and back, antennae, the prism glow layer |
| B-07 | **The Thornheart** (R7) | screen-filling | The rot's core: a pulsing mass of purple-brown bramble wound around a mint-glowing heart of wood. Three states: fully wrapped; half torn open; dying. | concept first; parts to be specified after review |

---

## Phase 7: cinematic stills and title (P3)

| ID | File | Description |
|---|---|---|
| S-01 | `Intro_01_Seed.png` | A tiny mint seed falls through mist into a moss-filled hollow; the scale is ambiguous (it's the palm). |
| S-02 | `Intro_02_Sprout.png` | Qori sprouts and opens its eyes; giant stone fingertips rise like cliffs around the hollow. |
| S-03 | `Intro_03_Reveal.png` | Qori on the palm's edge; on the far horizon, a vast sleeping stone face under a forest. |
| S-04 | `Wilted_Reveal.png` | The Wilted turning, its dim core visible through thorns. |
| S-05 | `Ending_01_Rise.png`, `Ending_02_Standing.png` | The titan sitting up with forests pouring from its shoulders; then standing at dawn with Qori on its shoulder. |
| S-06 | `Title_Background_v2.png` | Replaces `Title_Background`: Qori standing in the titan's palm at dawn, the sleeping face on the far horizon, the Colossus's shape hinted but not obvious. Same Qori model fix rules as before. |

---

## Music track: the waking score (runs in parallel with the art, from Phase 2 on)

Read section 7 of the design document first. The core idea: **one main theme that gains a layer with every knot woken**, over the titan's heartbeat.

**How to make it.** Compose original music as code: write each cue as MIDI with deterministic scripts (for example Python with `mido` or `music21`), render previews with a software synthesizer (for example FluidSynth) using **only instruments whose licence allows use in a commercial game** (CC0, or a permissive licence you name). Record the source and licence of every instrument or sample in `Music/PROVENANCE.md`. No imitation of any existing game or film soundtrack; everything is original.

**Deliver per cue**, in `Tools/IncomingArt/Codex_v2/Music/`:
- `<ID>_<Name>.mid`: type 1 MIDI, one named track per instrument or layer, with tempo and key set.
- `<ID>_<Name>.ogg`: a rendered preview, 44.1 kHz stereo, loudness about −16 LUFS, no clipping.
- For looping cues, **stems** as `<ID>_<Name>_<Layer>.ogg`: every stem exactly the same length, looping sample-accurately (no gap or click at the loop point).
- `CUE_SHEET.md`: key, tempo, bars, loop points, layers, and how each cue should be used.

| ID | Pri | Cue | Brief |
|---|---|---|---|
| MU-01 | P1 | **The Qolossal motif** | The slow four-note figure the whole score is built on, in D Dorian: noble, sleepy, ancient and kind. Deliver 4–6 short variations of it (kalimba, strings, choir, music box, minor key, reversed) so we can pick the core. **Stop for review after MU-01.** |
| MU-02 | P1 | **The waking theme** (adaptive) | 76 bpm, 32 bars, loopable, in **8 stems** that stack: heartbeat drum (always), kalimba (from the start), harp (+Reach), wooden flutes (+Breath), fiddle and bodhrán (+Spring), wordless choir (+Bloom), glass harmonica (+Sight), taiko and full orchestra (+Heart). Every combination of the first N stems must sound complete. |
| MU-03 | P2 | Palm Hollow | kalimba, soft felt piano, warm strings; 76 bpm; a gentle morning |
| MU-04 | P2 | Aqueduct Arm | harp, water glass, hang drum; 84 bpm; flowing arpeggios |
| MU-05 | P2 | Breathing Grove | wooden flutes, marimba, cello swells; 72 bpm; swells in and out every 6 bars, like breathing |
| MU-06 | P2 | Knee Terraces | fiddle, bodhrán, low whistle; 96 bpm; a folk work song |
| MU-07 | P2 | Falls Sanctuary | wordless choir, celesta, open fifths; 66 bpm; airy, stone-chapel reverb |
| MU-08 | P2 | Tearglass Eye | glass harmonica, reversed piano, bowed vibraphone; free time; the motif played backwards |
| MU-09 | P2 | Rootdeep Heart | taiko heartbeat, deep drone, bell chimes; in 3 tempo stems (60, 66, 72 bpm) for the rising pulse |
| | | *each region theme:* | 2–3 minutes, loopable, in two stems: **explore** and **tension** (the tension stem adds percussion and dissonance, for combat nearby) |
| MU-10 | P2 | Heartbeat bed | the titan's heartbeat alone, a soft low drum; loops at 48, 54, 60, 66 and 72 bpm |
| MU-11 | P2 | Guardian battle | driving percussion with thorny dissonance (col legno strings, prepared piano), with the **lead as a separate stem** so each region's lead instrument can take over; a short resolution ending for when the knot wakes |
| MU-12 | P2 | Stir stinger | 8–12 seconds: a huge low earth-rumble and brass swell into the motif |
| MU-13 | P2 | The Chart | the motif on a music box, in 8 stems matching MU-02's layers |
| MU-14 | P3 | The Wilted | the motif in a minor key on a slightly detuned music box; sad, not menacing |
| MU-15 | P3 | Title | calm and inviting, 1–2 minutes, loopable |
| MU-16 | P3 | Knot awakening | a short 3–5 second swell for the moment the knot lights |
| MU-17 | P3 | Ending | the full motif with orchestra and choir, 2–3 minutes |

**Sound signatures (P3, same rules):** `SFX_Titan_Breath` (a low wind swell, about 4 s), `SFX_Wood_Groan` ×3, `SFX_Vein_Hum` (a loop), `SFX_Echo_Voice` ×4 (a soft formant pad with no real words), `SFX_Rot_Rustle` ×3.

**Honest limit:** if your environment can't render audio well, deliver the MIDI files, cue sheet and stems as MIDI tracks; we'll render and mix them. Say so in the handoff.

## Delivery order and stops

1. The `A4_Slope_30` redo (Review 06).
2. **Phase 1** (C-01 to C-05, plus your design ideas D-01 to D-03). **Stop for Review 07.**
3. **Phase 2** (the Chart, knots, Waymarks, Vein Gates, stir vistas). **Stop for Review 08.**
4. **Phase 3** (characters; concepts first). **Stop for Review 09.**
5. Phase 4 and **Phase 5 A5**, then A6, each with its stack test first.
6. Phase 6, one guardian at a time: concept, stop, then parts.
7. Phase 7.
8. **The music track** runs alongside, from Phase 2: MU-01 first (**stop for review**), then MU-02, then the rest in priority order.

**Existing assets are reused, not re-requested:** `Collectible_LoreStone`, `Wind_Vent`, `FX_Wind_Streaks`, `FX_Ability_Unlock_Burst`, `Pickup_HeartSeed(_Shard)`, `Pickup_SapVial`, `Relic_WindLeaf`, `Relic_Glidecap`, the ability icons, the `Map_Icon_*` set, `Map_Room_Tile`, the Grove Warden, the Glow Leech, `GlowPod_Light_*`, the A0–A4 kits and backgrounds. `Colossus_Concept` stays as a reference; C-01 supersedes it as the design of the titan.
