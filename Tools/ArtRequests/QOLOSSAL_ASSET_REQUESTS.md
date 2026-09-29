# Qolossal: complete asset request for Codex (v2)

> **Platform direction — user decision, 2026-09-29:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. Original Switch is excluded. Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md) before applying this brief. Preserve accepted source bytes; use measured platform-specific runtime settings. This addendum does not reopen approvals or start a new batch.

This is the full list of artwork Qolossal needs to finish its mechanics and build the first portal-maze loop. It is written for Codex (image generation). Each entry is ready to turn into a prompt.

**How to use this file**
1. Work through it in **priority order**: all P1 items first, then P2, then P3.
2. For each asset, build the prompt from three parts:
   - the shared **style block** for its category (section 1),
   - the **format block** it names (section 2),
   - the entry's own description.
3. Put the deliverables in `Tools/IncomingArt/Codex_v2/<Category>/`, using the exact file names given here.
4. Add a `HANDOFF.md` and a `PROMPTS.md` per category, the same way as `Tools/QoriRigAuthoring/IncomingArt/Codex_v1`.
5. Do **not** edit scenes, prefabs, scripts or existing art. Claude integrates, fits and rigs the art.

---

## 0. World and themes: one valley, five moods

Qolossal is a hand-painted 2D side-scrolling forest adventure. Its areas connect through root-arch portals into a maze the player discovers. **Every area uses the same painting style, line weight and lighting logic.** Each area changes only its materials, palette accent and landmark shapes, so the world feels like one place with distinct moods.

| ID | Area | Role in the maze | Materials | Palette (base + accent) | Background (exists) |
|---|---|---|---|---|---|
| A0 | **Misty Valley** (starting clearing) | Hub; tutorial | Gray-green fractured stone, thick olive moss, ivy, ferns | Blue-gray mist, sage + **mint seed glow** | `MistyValley_Background_v1.png` |
| A1 | **Aqueduct Ravine** | Loop area 1 | Pale weathered limestone blocks, carved channels, algae stains, shallow silver water | Cool teal-gray, silver + **aqua** | `WorldBackground/Aqueduct.png` |
| A2 | **Ancient Grove** | Loop area 2 | Giant living roots and bark, shelf fungus, amber resin, fallen leaves | Warm olive, bark brown + **amber** | `WorldBackground/AncientGrove.png` |
| A3 | **Falls Sanctuary** | Loop area 3; milestone | Pale sandstone shrine masonry, slender broken columns, spray-wet moss, wind-bent grass | Bright airy gray, ivory + **pale gold** | `WorldBackground/FallsSanctuary.png` |
| A4 | **Rootdeep Hollow** (hidden) | Secret 4th area under the valley | Dark wet cave stone, huge root tangles, glowing seed pods, mushroom caps | Deep blue-green shadow + **bright mint glow** | *needs a new background* |

A4 is the one "different theme": darker and underground. It is still the same world and style, lit by the valley's own seed glow.

### Style references (attach them to every prompt in that category)
- Characters and creatures: `Assets/Resources/QoriRig/Qori_ThreeQuarter_Torso_v1.png`, `Qori_ThreeQuarter_Leg_v1.png`, `Qori_HeadAtlas_v1.png`, `Assets/Resources/Creatures/BrambleCrawler_Keyed_v1.png`, `Assets/Art/Creatures/SeedCarrier_Flying_v1.png`.
- Terrain: `Assets/Art/Terrain/MossyStone_DeepCliff_v2.png`, `Assets/Resources/Terrain/Rootbound_HangingPlatform_v1.png`.
- Props and portal: `Assets/Resources/WorldProps/LevelFinish_v1.png`, `Checkpoint_v1.png`, `GrowthFlower_v1.png`, `Assets/Art/Anchors/VineSeed_Anchor_v1.png`.
- Backgrounds: `Assets/Art/Backgrounds/MistyValley_Background_v1.png` plus the 3 in `Assets/Resources/WorldBackground/`.
- Weapons: `Assets/Resources/Armory/Weapons/*.png` (from `Qori_Weapon_*.png` in Codex_v1).

---

## 1. Style blocks (paste one at the start of each prompt)

**STYLE-CHAR** (player, enemies, weapons, pickups, FX)
> Production 2D game art for Qolossal, a hand-painted side-scrolling woodland fantasy game. Match the attached reference exactly in material, palette and line work: delicate hand-painted gouache textures, thin dark-brown painted contour lines, muted olive and sage leaves, ivory/cream petal surfaces, warm tan bark, restrained cyan-mint seed glow. Soft cool daylight from the upper left. Readable sculptural silhouette at small size. Painterly 2D, not 3D render, not vector, not pixel art, not anime cel shading. No text, logos or UI.

**STYLE-WORLD** (terrain, props, hazards)
> Production 2D terrain/prop art for Qolossal, matching the attached reference's grounded, subdued naturalistic painting: weathered stone, dense olive moss lips, trailing fine ivy, fine painterly surface detail, soft cool daylight from the upper left, no bright cartoon outline. Strict side elevation for a 2D platformer; never show the top surface in perspective.

**STYLE-BG** (backgrounds). This reuses the approved BackgroundJourney prompt.
> Production 16:9 landscape background painting for Qolossal. The attached image is a STYLE AND WORLD reference only. Match its pale misty blue-gray air, muted sage moss, cool natural daylight, softly detailed hand-painted gouache stone and foliage, atmospheric depth and ancient abandoned ruins. Full bleed, no transparency, no text, no characters, no UI, no playable foreground platforms. Keep the bottom third misty and subdued so the game's platforms and player stay legible. Sky in the upper third, major landmarks in the middle band. Avoid black foreground silhouettes, saturated colors, vignette, or a hard compositional frame.

For areas A1–A4, add one line naming the area's accent color and materials from the table in section 0.

---

## 2. Format blocks

**CUTOUT** (every rig part: player, enemies)
> One part only, in a straight neutral pose, facing RIGHT, centered with a 10% transparent margin. Paint the whole part, including areas normally hidden behind other parts. At each joint add a rounded, overlapping end (like a paper-doll joint) so the part can rotate without showing a gap. Genuine transparent PNG alpha. No backdrop, no checkerboard, no cast shadow, no glow outside the silhouette, no extra pieces.

**SPRITE** (single objects: props, pickups, weapons, icons)
> Single isolated complete object, orthographic side view at gameplay eye level, centered with a 10% margin. Genuine transparent PNG alpha. If alpha is unavailable, use one perfectly flat #FF00FF magenta background, with no magenta inside the object. Never paint a checkerboard. No cast shadow, no external glow cloud; glow stays painted inside the silhouette. Objects that sit on the ground rest on one common flat line at the bottom.

**TILE-H** (horizontally repeating strips: ground tops, hazard strips, water surface)
> A seamless horizontally tiling strip: the left and right edges must match pixel-for-pixel when repeated. Do not show an end or corner. Keep the height consistent across the full width. Transparent above and below the material.

**TILE-FILL** (interior fill textures: rock mass, bark mass, cave wall)
> A square, fully opaque texture that tiles seamlessly on all 4 sides. No large unique features that make repetition obvious, no directional lighting gradient, and even detail density.

**CAP** (ends and corners that finish a tile strip)
> Matches the named strip's material, height and lighting exactly. The inner edge must continue that strip seamlessly. Transparent outside the silhouette.

**PARALLAX** (background layers)
> A wide 32:9 horizontally tiling layer, transparent above the silhouette line and fully opaque below it. Left and right edges must match. Softer and lower in contrast the further back the layer sits.

### Scale and resolution (important; revised after the first test piece)
- **The generator does not keep scale consistent between images, so never rely on canvas size.** Claude normalizes every file, using the proportions listed in each entry.
- Proportions are given in "Qori heights" (QH). One QH is Qori's height without his ears, about **1.65 world units**.
- **Characters, creatures, rig parts, weapons, pickups, icons and effects:** paint at about **600 px per world unit**, so Qori is about 1,000 px tall.
- **Terrain kit (all T-items, the G-01 frames, the X barriers, H hazard strips, water, decor):** match the texel density of `MossyStone_DeepCliff_v2` as it appears in the game, about **120 px per world unit**, so Qori is about 200 px tall. Surface features must be **big**:
  - The moss lip is about 0.25–0.4 QH thick, with a soft, lumpy overhang.
  - Individual rock slabs are 1–2 QH tall.
  - Ivy strands are 0.5–1.5 QH long.
  - Floating and one-way platforms may be painted at 2× that density (240 px per world unit) for sharper edges.
- Keep each file under 2,048 px on its longest side, unless the entry says otherwise.

### Delivery checklist per file
- The exact file name, PNG, RGBA. Alpha is 0 at every corner and there is no halo against both dark and light backgrounds.
- One contact sheet per category (`_ContactSheet.png`) showing every file against a mid-gray background.
- `ALPHA_CHECK.json`, like Codex_v1.

---

## 3. Player: Qori (additions to the existing rig)

The rig already has: head (4 expressions + blink), ears, torso, skirt, legs, upper arm, forearm (fist and open hand), and the layered cloak. The items below fill the gaps found in testing.

| ID | Pri | File | Description |
|---|---|---|---|
| P-01 | P1 | `Qori_Forearm_Reach.png` | **CUTOUT.** The same forearm as `Qori_Forearm_Fist`, but **1.6× longer** (length ÷ width ≥ 11.5, measured on the opaque shape), with an open, gripping hand whose fingers curl over an edge. It is used only for hanging from ledges: his current arms are shorter than his head is tall, so he can't reach overhead. Keep the same bark texture, width, elbow cap and leaf wrist wrap. |
| P-02 | P1 | `Qori_UpperArm_Reach.png` | **CUTOUT.** The upper arm, **1.4× longer** (length ÷ width ≥ 9.2), for the same purpose. The shoulder and elbow caps must be identical to `Qori_UpperArm`. |
| P-03 | P1 | `Qori_Face_Hurt.png` | **Face overlay** (revised after review 01): only the eyes, brows and mouth, plus a cream skin patch that covers the neutral features, on the **same 1254×1254 canvas and position as `Codex_v1/Qori_Head_Neutral.png`**. Claude composites it onto the neutral head so the outline never changes. Eyes squeezed shut, a small grimace, cheek leaves flared. Only the face changes. |
| P-04 | P1 | `Qori_Face_Effort.png` | Face overlay, the same format as P-03. Determined eyes narrowed toward the front, mouth slightly open, as if shouting. Used for heavy attacks, ledge pull-ups and wall jumps. |
| P-05 | P2 | `Qori_Face_Surprise.png` | Face overlay, the same format as P-03. Eyes wide, pupils small. Used for a fall with no ground below, and for finding a secret. |
| P-06 | P2 | `Qori_Face_Sleep.png` | Face overlay, the same format as P-03. Eyes closed peacefully. Used for resting at a checkpoint shrine and on the title screen. |
| P-07 | P1 | `Qori_Wilt_Pieces.png` | **SPRITE** sheet for the death effect: 8–10 loose Qori cloak leaves, 3 cream petal shards, and 1 small wisp of the mint seed-spirit. Each piece is separated by clear margins. |
| P-08 | P2 | `Qori_Skirt_Flap_1..4.png` | Already delivered in v1; they only need a redo **if** their scale or style differs from the torso. Check them against the torso first. |
| P-09 | P3 | `Qori_Portrait.png` | A 1:1 bust portrait in 3/4 view for the menu, map and save slots, in STYLE-CHAR. It may have a soft painted vignette background. |

**Note for P-01/P-02:** the long arms only swap in while Qori hangs. Their joint ends must line up exactly with the normal arm parts so the swap doesn't pop.

---

## 4. Weapons and combat

The 4 melee weapons (Leaf Sword, Seedpod Mace, Thorn Spear, Vine Whip handle) and the resin sling already exist. What's missing is the whip lash, the effect textures and the upgrade tiers.

| ID | Pri | File | Description |
|---|---|---|---|
| W-01 | P1 | `Whip_Lash_Segment.png` | **TILE-H**, 1024×128, with the vine about 48 px thick. A braided green vine with small thorns. It repeats along the whip's line in code. |
| W-02 | P1 | `Whip_Lash_Tip.png` | **SPRITE**. The lash's end: a small leaf bud with a thorn. It connects to the segment's height. |
| W-03 | P1 | `FX_SlashArc_Sword.png` | **SPRITE**, a 1024×512 crescent smear: a translucent ivory-to-mint gradient with painted leaf flecks at the trailing edge. **Fully transparent** outside the arc, soft inner edge, crisp leading edge. |
| W-04 | P1 | `FX_SlashArc_Heavy.png` | The same for the mace: a wider, thicker amber-brown arc carrying dust and bark chips. |
| W-05 | P1 | `FX_Thrust_Streak.png` | The spear's straight thrust streak: a narrow ivory-mint line that tapers to a point on the right. |
| W-06 | P1 | `FX_Hit_Spark_01..03.png` | 3 small **SPRITE** impact bursts: star-shaped mint sap splashes with leaf shards. Bright, but inside the silhouette. |
| W-07 | P1 | `FX_Hit_Block.png` | A dull gray stone-chip spark for hits blocked by armor. |
| W-08 | P2 | `Weapon_<Name>_T2.png` ×4 | **Upgrade tier 2** of each weapon, with the same silhouette and grip position: richer materials, golden resin inlay, a faint mint vein glow. The file names are `Weapon_LeafSword_T2`, `Weapon_SeedpodMace_T2`, `Weapon_ThornSpear_T2` and `Weapon_WhipHandle_T2`. |
| W-09 | P2 | `Icon_Weapon_<Name>.png` ×5 | Square 256×256 **SPRITE** icons for the armory menu and HUD: each weapon on the diagonal, no background, with a readable silhouette at 48 px. The fifth icon is the sling. |

---

## 5. Enemies

All enemies are delivered as **cutout parts** so Claude can rig them like Qori. This fixes the stiff bent-painting look the creatures have today. For each enemy, deliver:
- `<Enemy>_Concept.png`: one full-body painting in a clear side pose (the reference for assembly),
- the listed **parts** (CUTOUT format), all at the same scale as the concept.

Size is given in QH. Behavior is included so the art matches what the enemy does.

### 5a. Core enemies (appear in every area, with a palette variant per area)

| ID | Pri | Enemy | Size | Behavior (drives the art) | Parts to deliver |
|---|---|---|---|---|---|
| E-01 | P1 | **Bramble Crawler** (a redo of the existing one as parts) | 0.8 QH long, 0.5 QH tall | Patrols; when it spots Qori it lowers its thorny back and charges | `Crawler_Body`, `Crawler_Head` (with a separate `Crawler_Jaw`), `Crawler_LegFront_Upper/Lower`, `Crawler_LegHind_Upper/Lower`, `Crawler_BackThorns` (a separate spine crest that can bristle). The design must match `BrambleCrawler_Keyed_v1.png`. |
| E-02 | P1 | **Seed Carrier** (a redo as parts; harmless) | 0.6 QH | Flies in a figure-8; the seed it carries works as a **grapple anchor** | `Carrier_Body`, `Carrier_Head`, `Carrier_WingFront`, `Carrier_WingBack`, `Carrier_SeedPod` (the glowing pod it carries). It must match `SeedCarrier_Flying_v1.png`. |
| E-03 | P1 | **Thornwing** | 0.5 QH, wingspan 0.9 QH | A hostile flyer that hovers, then swoops in a dive toward Qori | A dark-olive moth/bat hybrid with thorn-edged leaf wings and a pointed beak-snout. Parts: `Thornwing_Body`, `Thornwing_Head`, `Thornwing_WingFront`, `Thornwing_WingBack`, `Thornwing_Tail`. Its silhouette must read clearly as a *threat*, unlike the round, friendly Seed Carrier. |
| E-04 | P1 | **Pod Spitter** | 0.7 QH tall | A stationary plant turret that turns toward Qori, swells, and spits a seed | A bulbous pod with a flower mouth on a rooted stalk. Parts: `Spitter_Base` (the roots), `Spitter_Stalk`, `Spitter_Head_Closed`, `Spitter_Head_Open` (mouth petals open, the same registration as Closed), `Spitter_Projectile` (a hard brown seed with a dark-red tip, small). |
| E-05 | P1 | **Shellback** | 0.9 QH long, 0.6 QH tall | A slow beetle with an armored shell that blocks sword, spear and whip. **Only the mace cracks the shell**; after that it is vulnerable | A stone-and-bark beetle. Parts: `Shellback_Body` (soft underside), `Shellback_Shell_Intact`, `Shellback_Shell_Cracked` (the same outline, with cracks and a glowing mint inside), `Shellback_Shell_Shards` (5–6 loose pieces for the break), `Shellback_Head`, `Shellback_Leg` (one leg, reused 3 times per side). |
| E-06 | P2 | **Burrow Grub** | 0.6 QH | Hides under the soil; the ground bulges, then it bursts up and bites | A pale grub with a ring of root teeth. Parts: `Grub_Body_Segment` (reused), `Grub_Head`, `Grub_Mouth_Open`, `FX_Soil_Burst.png` (a dirt-and-moss explosion sprite). |

**Area palette variants for E-01 to E-05 (P2).** Deliver recolored copies of the parts only: the same shapes and registration, tinted to the area's accent color. File suffixes are `_A1` (aqua-algae), `_A2` (amber-bark), `_A3` (pale-gold stone), `_A4` (dark with mint glow spots).

### 5b. Area-specific enemies

| ID | Pri | Area | Enemy | Behavior | Art |
|---|---|---|---|---|---|
| E-07 | P2 | A1 Aqueduct | **Ripple Newt** | Hides in water channels, leaps out in an arc, and lands on platforms | A slick teal newt with leaf-frill fins. Parts: `Newt_Body`, `Newt_Head`, `Newt_Tail_1/2`, `Newt_Leg`, `FX_Splash.png`. |
| E-08 | P2 | A2 Grove | **Bark Sentinel** | A heavy guard that holds a bark shield in front. **The spear pierces the shield**; other weapons bounce off | 1.3 QH, upright. Parts: `Sentinel_Body`, `Sentinel_Head`, `Sentinel_Arm_Upper/Lower`, `Sentinel_Shield`, `Sentinel_Leg_Upper/Lower`. |
| E-09 | P2 | A3 Falls | **Gust Moth** | Hovers and flaps a wind blast that pushes Qori back, without dealing damage | A pale ivory moth with huge soft wings. Parts: `GustMoth_Body`, `GustMoth_WingFront/Back`, `FX_Gust.png` (painted wind streaks). |
| E-10 | P3 | A4 Rootdeep | **Glow Leech** | Clings to the ceiling in the dark; drops when Qori passes; glows before it drops | A dark leech with mint bioluminescent spots. Parts: `Leech_Body_Segment`, `Leech_Head`, `Leech_Glow` (glow spots on their own layer). |

### 5c. Mini-boss and boss

| ID | Pri | Enemy | Size | Behavior | Art |
|---|---|---|---|---|---|
| B-01 | P2 | **Grove Warden** (mini-boss, A2) | 2.5 QH tall | A stag-boar of roots and bark with antlers of branches. Charges, stomps shockwaves, and summons root spikes; its **glowing heart knot** is the weak point | Parts: `Warden_Body`, `Warden_Head`, `Warden_Jaw`, `Warden_Antler_L/R`, `Warden_LegFront_Upper/Lower`, `Warden_LegHind_Upper/Lower`, `Warden_HeartKnot` (glows on its own layer), `Warden_RootSpike.png` (a separate ground-spike attack sprite). |
| B-02 | P3 | **The Colossus** (the milestone boss, A3; the game is named after it) | Screen-filling, 6–8 QH | A giant ancient stone guardian overgrown by the forest, fought on its body and around it | Concept painting first, **before** any parts: `Colossus_Concept.png`. Its parts will be specified after the design is approved. |

### 5d. Shared enemy effects
| ID | Pri | File | Description |
|---|---|---|---|
| E-FX1 | P1 | `FX_EnemyDeath_Puff_01..03.png` | Leaves, bark chips and a soft mint seed-wisp that bursts out when an enemy dies. |
| E-FX2 | P1 | `FX_Telegraph_Glint.png` | A small bright 4-point star glint that flashes before an enemy attacks. |
| E-FX3 | P1 | `Drop_SapOrb.png` | A small glowing amber sap orb dropped by enemies; it restores health. |

---

## 6. Environment: modular terrain kit (per area)

Right now the terrain is only a few one-off paintings. Building many areas needs a **modular kit** that Claude can assemble in Unity with Sprite Shapes and tiles. **Deliver the full set for A0 (P1), then A1–A3 (P2), then A4 (P3).** File names start with the area prefix: `A0_`, `A1_`, and so on.

| # | File | Format | Description |
|---|---|---|---|
| T-01 | `<A>_Ground_Top.png` | TILE-H, **2048×512** (about 17×4.3 world units) | The walkable top edge.<br>- The **walk line is at y = 96 px** and stays level across the whole width, but the moss surface itself is soft and lumpy, undulating ±6–10 px around it. It must not be a ruler-straight band.<br>- Only grass tufts and fern tips rise above the walk line.<br>- The moss lip is thick (30–50 px) and rolls over the front edge, with ivy draping 60–180 px down the face.<br>- Below that, the **same large fractured vertical rock slabs as the cliff reference** continue down to the **bottom edge fully opaque, with no scalloped underside**, because this strip overlaps the Fill texture.<br>- The bottom 64 px must use the T-02 Fill material so the two join invisibly. |
| T-02 | `<A>_Ground_Fill.png` | TILE-FILL, 1024×1024 (about 8.5 world units) | The interior mass of the ground and walls: fractured rock, or bark, masonry or cave stone depending on the area. In A0 these are large, vertical, angular slabs with dark crevices, like the reference cliff face, **not rounded cobbles or boulders**. |
| T-03 | `<A>_Wall_Side.png` | TILE-H, **rotated**: a vertical strip, 512×2048 | The vertical wall-face edge for the right side of a block (it is mirrored for the left). Hanging ivy and chipped edges. The inner (left) 64 px must blend into the Fill. |
| T-04 | `<A>_Ceiling_Under.png` | TILE-H, 2048×384 | The underside edge: dripping roots, small hanging vines, a darker overhang. The top 64 px must blend into the Fill. |
| T-05 | `<A>_Corner_Outer_TopRight.png` | CAP, 512×512 | Joins Ground_Top to Wall_Side; the moss rolls over the edge. This is the ledge corner that Qori grabs, so the lip must look grippable. |
| T-06 | `<A>_Corner_Inner_TopRight.png` | CAP, 512×512 | The inner corner where the ground meets a wall that rises up. |
| T-07 | `<A>_Corner_Outer_BottomRight.png` | CAP, 512×512 | Joins Wall_Side to Ceiling_Under. |
| T-08 | `<A>_Slope_30.png` | SPRITE, 2048×1280 | A top edge rising at 30° that joins Ground_Top at both ends. |
| T-09 | `<A>_Platform_OneWay.png` | SPRITE, 1024×256 | A thin, jump-through platform: a branch or plank bridge in A0/A2, a stone lintel in A1/A3, a mushroom shelf in A4. The top is perfectly flat. |
| T-10 | `<A>_Platform_Floating_S/M/L.png` | SPRITE, 3 widths (2, 3.5, 5 world units) | Floating islands like `Rootbound_HangingPlatform_v1`, each with a finished underside. |
| T-11 | `<A>_Wall_Climbable.png` | TILE-FILL | A wall texture that clearly signals **wall slide and wall jump are allowed**: vertical root lattice or chiseled grip holds. Its visual language must differ from normal walls. |
| T-12 | `<A>_Wall_Slippery.png` | TILE-FILL | A wet, smooth stone signal meaning **no wall cling** here. Used for puzzle routing. |

**Before painting any T-item:** place the reference cliff and one Qori (about 200 px tall at this density) on the same canvas. Check that the details are cliff-sized, not pebble-sized.

**Area material notes (add them to each T-prompt):**
- **A0 Misty Valley:** match `MossyStone_DeepCliff_v2` exactly.
- **A1 Aqueduct:** squared pale limestone blocks with mortar lines, water stains and algae, and carved channel grooves. The tops are flat paving with a moss edge.
- **A2 Grove:** the ground is living wood. Ground_Top is root bark with fallen leaves, Fill is layered bark and heartwood rings, and the platforms are shelf fungus and root bridges.
- **A3 Falls:** pale sandstone masonry with carved shrine bands, spray-darkened lower edges, wind-bent grass tops, and a few gold lichen patches.
- **A4 Rootdeep:** black-green wet cave stone with thick root cables. Ground_Top is packed earth with small glowing mint seed-sprouts. Keep contrast readable against a dark background.

---

## 7. Mechanics props, hazards and puzzle objects

These are the objects that make the maze work: gates, ability locks, secrets and hazards. Existing: growth flower, growth platform, checkpoint lantern, root arch (level finish), vine-seed anchor.

### 7a. Portals and progression (P1 for the A0 versions, P2 for the rest)
| ID | File | Description |
|---|---|---|
| G-01 | `Portal_Gate_<A>.png` ×5 | **The area portal.** It evolves from `LevelFinish_v1` into a **rooted arch with a swirling doorway membrane**. Deliver the arch **frame** (SPRITE, with the opening fully transparent) and the membrane separately (G-02). Each area uses its own material and accent: mossy root (A0), limestone with an aqua keystone (A1), living bark with amber resin (A2), sandstone with gold inlay (A3), black root with mint glow (A4). 3.2 world units tall. |
| G-02 | `Portal_Membrane_<A>.png` ×5 | The doorway fill: a soft painted swirl in the area's accent color that fits the opening shape exactly. Semi-transparent at the edges. Code animates it. |
| G-03 | `Portal_Gate_Milestone.png` | A larger, grander arch (4.5 u tall) for major progress points: twin trees braided together, with a crown of glowing seed lanterns. |
| G-04 | `Portal_Gate_Hidden.png` | An overgrown, collapsed and **dormant** arch, half buried in moss. It's easy to miss; this is the secret route to A4. It comes with `Portal_Gate_Hidden_Awake.png`, the same arch with the moss peeled back and the glow awake. |
| G-05 | `Checkpoint_Shrine_<A>.png` ×4 | Area variants of `Checkpoint_v1` (the amber seed lantern) for A1–A4. The existing one is the A0 version. |

### 7b. Ability pickups (metroidvania gates)
Each ability needs an in-world shrine where it is found and a HUD icon.

| ID | Pri | File | Ability | Description |
|---|---|---|---|---|
| U-01 | P1 | `Shrine_Ability.png` | (shared) | A small stone pedestal wrapped in roots, with a hovering relic floating above it. Deliver the pedestal and the relic slot separately. |
| U-02 | P1 | `Relic_LivingThread.png` + `Icon_Ability_Thread.png` | Grapple | A coiled glowing vine spool. |
| U-03 | P1 | `Relic_ClimbingMoss.png` + `Icon_Ability_WallJump.png` | Wall jump | A tuft of clinging moss shaped like a hand. |
| U-04 | P1 | `Relic_Bloomfall.png` + `Icon_Ability_Pogo.png` | Pogo / down-thrust bounce | A downward-pointing flower bud. |
| U-05 | P2 | `Relic_WindLeaf.png` + `Icon_Ability_Dash.png` | Dash | A spinning pale leaf with wind curls. |
| U-06 | P2 | `Relic_Glidecap.png` + `Icon_Ability_Glide.png` | Glide / double jump | A dandelion-seed parasol. |
| U-07 | P2 | `Pickup_HeartSeed.png` + `Pickup_HeartSeed_Shard.png` | Max-health upgrade | A heart-shaped seed and a quarter shard of it (collect 4 shards to make 1 seed). |
| U-08 | P2 | `Pickup_SapVial.png` | Weapon upgrade material | A small amber resin vial. |

Icons are 256×256 **SPRITE**s, and each must read clearly at 48 px.

### 7c. Barriers, switches and secrets
| ID | Pri | File(s) | Mechanic | Description |
|---|---|---|---|---|
| X-01 | P1 | `Barrier_Rubble_Intact.png`, `Barrier_Rubble_Pieces.png` | **The mace breaks it** | A cracked stone wall section, 1.5×3 u, plus 6–8 separate falling chunks (a sprite sheet with margins). One per area material (`_A0`–`_A4`) in P2. |
| X-02 | P1 | `Barrier_Thorns_Intact.png`, `Barrier_Thorns_Cut.png` | **The sword cuts it** | A thorny bramble curtain, and a cut-and-wilted version. |
| X-03 | P1 | `Floor_Weak.png`, `Floor_Weak_Pieces.png` | **A pogo or down-thrust breaks it** | A thin cracked floor slab, 3 u wide, and its broken pieces. |
| X-04 | P1 | `Switch_Seed_Off.png`, `Switch_Seed_On.png` | **Hit with the sling from range** | A closed bud on a wall mount, and the same bud open and glowing. |
| X-05 | P1 | `Switch_Plate_Up.png`, `Switch_Plate_Down.png` | Pressure plate | A stone plate with a moss border, in raised and pressed states. |
| X-06 | P1 | `Gate_Root_Closed.png`, `Gate_Root_Open.png` | Opened by switches | Vertical interlocked roots blocking a passage, 1×3 u, and the same roots withdrawn into the ground and ceiling. Deliver as **top and bottom halves** so they can slide apart. |
| X-07 | P2 | `Anchor_Ring_<A>.png` ×4 | Grapple point | Area variants of `VineSeed_Anchor_v1`. |
| X-08 | P2 | `Wall_Secret_Overlay_<A>.png` ×5 | A hidden passage | A foreground wall section painted to match the area Fill, which fades out when Qori walks in. Include subtle hints: a small crack or a wisp of mint glow. |
| X-09 | P2 | `Collectible_LoreStone.png` | Lore and secret reward | A small carved tablet with a glowing glyph (no readable text). |

### 7d. Hazards and moving objects
| ID | Pri | File | Description |
|---|---|---|---|
| H-01 | P1 | `Hazard_Thorns_Floor.png` | TILE-H, 1024×192. A floor of sharp dark thorn brambles. It must read as **danger** at a glance: darker values and ivory points against the moss. Area variants `_A1`–`_A4` in P2: sharp limestone shards (A1), resin thorns (A2), broken marble spikes (A3), crystal root spikes (A4). |
| H-02 | P1 | `Hazard_Thorns_Wall.png`, `Hazard_Thorns_Ceiling.png` | The same as H-01, oriented for walls and ceilings. |
| H-03 | P1 | `Platform_Crumble.png` + `_Pieces.png` | A platform that shakes, then crumbles 0.5 s after Qori lands. It must look fragile: cracks and loose pebbles. |
| H-04 | P1 | `Platform_Moving_<A>.png` | A moving platform per area: a root-woven raft (A0), a stone block on chains (A1), a hollow log (A2), a carved slab (A3), a mushroom cap (A4). |
| H-05 | P2 | `Water_Surface.png`, `Water_Body.png` | TILE-H surface line with a soft highlight, and a TILE-FILL deep-water body, semi-transparent teal-silver. For A1 and A3. |
| H-06 | P2 | `Waterfall_Column.png` | A vertically tiling 512×1024 falling-water strip, plus `Waterfall_Splash_Base.png`. For A3. |
| H-07 | P2 | `Wind_Vent.png` + `FX_Wind_Streaks.png` | A stone vent that pushes Qori upward, and its painted wind streak texture. For A3. |
| H-08 | P2 | `Hazard_FallingRock.png` + `FX_Dust_Warning.png` | A loose ceiling rock and its dust trickle warning. For A1. |
| H-09 | P3 | `GlowPod_Light.png` | A4 darkness mechanic: a seed pod that lights up when hit and lights the area around it. Deliver Off and On states. |

---

## 8. Decoration (makes each area feel alive)

For each area, deliver a **decor sheet** of 10–14 separate **SPRITE**s with margins between them: `Decor_<A>_Sheet.png`. It must include 3 foreground plants (ferns, grass clumps), 2 rocks, 2 ruin fragments (a broken pillar, a carved block), 2 hanging vines or roots, 1 mushroom cluster, and 1 small ambient critter such as a snail, beetle or butterfly (drawn as a static sprite).

- **A0:** ferns, ivy, mossy boulders, a fallen mossy log, small white flowers.
- **A1:** water reeds, broken aqueduct pillar bases, algae-covered blocks, a small stone basin, water lilies.
- **A2:** giant fallen leaves, shelf fungus clusters, resin drips, roots curling over masonry, a hollow stump.
- **A3:** wind-bent grass, broken slender columns, prayer-stone stacks, bell-shaped flowers, a torn cloth banner (no symbols or text).
- **A4:** glowing mushrooms, hanging root tangles, cave crystals in mint, dripping stalactite roots.

Plus `Decor_Foreground_Frame_<A>.png` (P2): a dark, **soft-focus** foreground silhouette layer (leaves and branches) to frame the screen edges. It is placed in front of the player at low opacity.

---

## 9. Backgrounds and parallax

The far background paintings for A0–A3 already exist (see section 0). The scene needs **depth layers** between those far paintings and the playfield.

| ID | Pri | File | Description |
|---|---|---|---|
| BG-01 | P1 | `BG_<A>_Mid.png` for A0–A3 | **PARALLAX**. The middle-distance layer: soft tree lines, ruins and hills in the area's materials, at about 60% of the far painting's contrast. Transparent above the silhouette line. |
| BG-02 | P1 | `BG_<A>_Near.png` for A0–A3 | **PARALLAX**. The near layer just behind the playfield: darker, more detailed trunks, ruin arches and vines, with a silhouette that rises and falls. Keep the vertical middle band mostly open so the platforms read clearly. |
| BG-03 | P2 | `BG_Sky_<A>.png` for A0–A4 | A 16:9 plain sky gradient with soft clouds only, placed behind the far painting for tall vertical rooms. |
| BG-04 | P3 | `RootdeepHollow.png` | **The new A4 far background**, 16:9, in STYLE-BG. "A vast underground hollow beneath the valley. Colossal tree roots descend from a dark earthen ceiling like pillars. Ancient carved stone stairs and fragments of a buried shrine. Hundreds of small mint-glowing seed pods and pale mushrooms light the gloom. Deep blue-green shadow, a faint silver shaft of daylight through one distant crack. Dark, but readable and calm, not horror." |
| BG-05 | P3 | `BG_A4_Mid.png`, `BG_A4_Near.png` | Parallax layers for A4, as in BG-01 and BG-02. |
| BG-06 | P2 | `BG_Transition_<A>.png` | Optional "gateway vista" paintings for portal transitions: a view through the arch into the next area. 16:9. |

---

## 10. User interface

| ID | Pri | File | Description |
|---|---|---|---|
| UI-01 | P1 | `HUD_Health_Leaf_Full.png`, `_Empty.png`, `_Half.png` | Health pips: a fresh green leaf (full), a withered brown leaf outline (empty) and a half-green leaf. 128×128 **SPRITE**. |
| UI-02 | P1 | `HUD_Frame.png` | A small root-and-leaf frame that holds the health pips and the weapon icon, in the top-left corner. |
| UI-03 | P2 | `HUD_Resource_Bar.png` + `_Fill.png` | A bar for the ability resource: a resin tube with a glowing sap fill. |
| UI-04 | P1 | `UI_Panel_9Slice.png` | A menu panel with a parchment-bark center and a root border, built for **9-slice** use (corners must not stretch). 512×512. |
| UI-05 | P1 | `UI_Button_Normal/Hover/Pressed.png` | 9-slice buttons that match UI-04. |
| UI-06 | P2 | `Map_Room_Tile.png`, `Map_Icon_*.png` | The map screen: a parchment room-block tile, plus icons for portal, checkpoint, ability shrine, unexplored "?", secret, and Qori's position marker (his head). |
| UI-07 | P2 | `Title_Logo_Qolossal.png` | The game logo "QOLOSSAL", hand-lettered in carved wood with leaf and vine accents and a small mint seed glow in the "O". **This is the one asset that must contain text; spell it exactly QOLOSSAL.** Transparent background. |
| UI-08 | P2 | `Title_Background.png` | A 16:9 title screen: Qori sitting on a mossy ledge (use `Qori_Head_Sleep` for his face), looking across the misty valley at a distant silhouette of the Colossus. |
| UI-09 | P3 | `UI_Cursor_Leaf.png`, `UI_Selector_Glow.png` | A menu cursor and a selection highlight. |

---

## 11. General effects (particles and feedback)

All are **SPRITE**s on transparent backgrounds. Anything with several frames is delivered as numbered separate files, not a sheet.

| ID | Pri | File | Description |
|---|---|---|---|
| FX-01 | P1 | `FX_Dust_Land_01..04.png` | A 4-frame dust puff for landing: moss flecks and soft earth, growing and fading. |
| FX-02 | P1 | `FX_Dust_Run_01..03.png` | A small kick-up puff for running and turning. |
| FX-03 | P1 | `FX_Leaf_Single_01..05.png` | 5 single loose leaves in different shapes and olive tones, used by the particle system for the cloak shed, ambient fall and hits. |
| FX-04 | P1 | `FX_WallSlide_Scrape.png` | Small stone and moss debris for wall slides. |
| FX-05 | P1 | `FX_Checkpoint_Motes.png` | A soft mint seed-mote that floats up (one sprite, used as a particle). |
| FX-06 | P1 | `FX_Portal_Swirl_01..06.png` | 6 frames of leaves and motes spiraling into a doorway, for area transitions. |
| FX-07 | P2 | `FX_Ambient_Mist.png` | A large, very soft horizontal mist wisp that drifts across the screen. |
| FX-08 | P2 | `FX_Ambient_Firefly.png` | A tiny glowing mote for the A2 and A4 ambience. |
| FX-09 | P2 | `FX_Water_Ripple.png`, `FX_Water_Drip.png` | Water feedback for A1 and A3. |
| FX-10 | P2 | `FX_Ability_Unlock_Burst.png` | A large radial bloom of leaves and light for picking up a relic. |

---

## 12. Delivery order (suggested batches for Codex)

1. **Batch 1: finish the mechanics** (all P1 items in sections 3, 4, 5a, 5d, 7 and 10, plus the A0 set from section 6).
   - This covers the player gaps, the weapon effects, the 5 core enemies as parts, the full A0 terrain kit, the portal and ability shrines, the barriers, switches, gates and hazards, the HUD and the base effects.
2. **Batch 2: A0 polish and the backgrounds.** The A0 decor, BG-01 and BG-02 for A0–A3, and the remaining P1 effects.
3. **Batch 3: the maze loop** (the P2 items).
   - The A1–A3 terrain kits, decor, props and hazard variants, E-06 to E-09, B-01 Grove Warden, the rest of the ability relics, the map and title screen, and the weapon upgrades.
4. **Batch 4: secret and finale** (the P3 items).
   - The A4 Rootdeep kit and background, E-10, the Colossus concept, the portrait and the remaining UI.

**Before starting each batch:** send one test piece first. Include a composite showing it at game scale next to Qori and the reference cliff. Claude checks the scale, alpha and style fit in Unity before the rest are generated. The best test piece is the first file of the batch, for example `A0_Ground_Top.png`.

## 13. Known pitfalls from v1 (read before generating)
- **Scale drifts between images.** Include the proportion notes in every prompt anyway. Claude rescales using the joint landmarks.
- **Expression heads must keep the same outline.** Last time the blink head drifted. For every head variant, ask for an *edit* of `Qori_Head_Neutral` in which only the facial features change.
- **Checkerboards and halos.** Always ask for real alpha, with magenta as the only fallback. Check edges against dark and light backgrounds.
- **Tiles must actually tile.** After generating, offset the image by half its width (and half its height for fills) and repaint the seam before delivering.
- **No text** anywhere except the title logo (UI-07).
- **Keep it subdued.** The game's look is calm, misty and naturalistic. Avoid saturated colors, heavy black outlines, glossy 3D rendering and cartoon exaggeration, except for the small, intentional mint and amber glows.
