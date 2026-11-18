# Codex brief: Batch 50a, "The Keepers' Nursery", key frames for Route B (the Garden)

Written 9 Nov 2026 by Claude. The user agreed this direction and asked for stage 50a as recommended.

**This brief covers stage 50a only: concepts, nothing goes into `Assets\`.** Stages 50b (the B04 slice) and 50c (the rest of Route B) will get their own brief, with exact file names and sizes, once the user has approved these key frames. Then stop.

Targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch 2 (handheld and docked). The original Switch is excluded. Every device test is "not tested" unless you actually ran it.

---

## Route names changed (read this first)

On 9 Nov the user renamed the underground routes to **A, B, C, D**. Old C (Garden) is now **B**, old D (Wind) is now **C** and old E (Descent) is now **D**. Use the new names in everything you write.

**Internal IDs haven't changed yet.** Code, the scene, `rooms.json`, `terrain_build_v2.json`, `CavePassageConnections.json`, the captures and the `Assets\` folders still say C01-C10. When you read data, **B01 = C01 … B10 = C10**. Don't rename anything in data, code or folders.

## 0. Before you start (all must hold, or stop and say which failed)

- Run `git status`. Other sessions share this checkout. Touch only the paths this brief names.
- The delivery folder `C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch50\` doesn't exist yet or is empty. Never overwrite a delivery. If 50 is taken, use the next free number and say so.
- 50a writes nothing under `Assets\` and changes no scene. To capture terrain plates you may open Unity only if no other session is using it. If one is, stop and ask the user. Don't force it closed. Leave the scene clean and normal save routing restored.

## 1. Read first

1. `Tools/ArtRequests/CODEX_PROMPT_BATCH49_KEEPERS_CISTERN_ROUTE_A.md`. **Its sections 3 (layers, motion, density), 4 (automatic fails) and 7 (method) apply unchanged to Route B.** This brief only lists what's different.
2. `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_97.md` to `CLAUDE_REVIEW_105.md`: what Route A got wrong and how it was fixed. In particular, background ledges at jump height read as platforms (Review 103), and painting bottoms must not end in a hard edge (Review 104).
3. `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, the top of `QOLOSSAL_SESSION_HANDOFF.md`.
4. `Tools/Design/UndergroundPlatforming_20261030/UNDERGROUND_PLATFORMING_PLAN.md`, "Route B: Garden": what the player does in each room.
5. The Route B gameplay art you must stay clear of: the platform kit `Assets/Art/Underground/PlatformKit/C/` (thorns, decks, crumble, gate, seal), the thread anchor, pull weight and bounce pod art, and the creatures and Root Matron boss used in rooms C01-C10. **Sample their colours** and list them on the palette sheet.
6. The accepted Route A result for finish and density: `Assets/Art/Underground/Backgrounds/KeepersCistern/`.

## 2. The theme

**The Keepers' Nursery.** Under Ribwood, the Keepers built a seed garden fed by the Cistern's water: stepped planting beds, seed vaults, terracotta jars and brass trellis. They are gone. The living roots of Ribwood have broken in from above and taken the garden over. Some beds still flower in the dark.

Three pillars, as in Route A. Every painting must serve all three.

1. **Built and enclosed.** Masonry planting terraces, seed-jar shelving, potting alcoves, squared trellis arches, a lattice ceiling that roots push through. A ceiling, vault or overhang is in view in every frame. Depth ends in darkness within tens of metres. **No sky, forest, canopy-against-light, horizon, glasshouse or outdoor haze.**
2. **The garden tells the story.** Order overtaken: neat terraces cracked by roots, labelled seed drawers spilling, trellis bent by growth, dry irrigation channels from the Cistern. It echoes the route's swing, bounce and crumble beats.
3. **Three lights, each with a job.**
   - **Keeper lanterns** (warm amber, as Route A): safety and rest. A few per room, many in B07.
   - **Mint root-glow** (as Route A): wayfinding toward the Heart. All routes share it. Denser toward B08.
   - **Bloom-light** (soft rose, new): small flowering clusters that glow. Rare in B01, stronger room by room, strongest around B08's gate.
   - B01 alone may have a faint green-filtered daylight through one root-broken hole in the ceiling. It's gone by B02.

Darkness is part of the look. Large areas of rear wall fall into deep shadow; lit features carry the picture.

### Palette (sRGB; a guide, not exact pixels)

| Role | Hex |
|---|---|
| Deepest shadow | `#12160F` |
| Moss-black | `#1E2A1E` |
| Root bark, umber | `#4A3626` |
| Terracotta, faded | `#8A5A44` |
| Terracotta, lit | `#B07E62` |
| Dull brass | `#7A6A3E` |
| Lantern amber | `#F0A848` |
| Mint root-glow | `#92F2C8` |
| Bloom rose | `#E88AA6` |
| Filtered daylight (B01 only) | `#D8E8C8` |

**Colour conflicts are yours to check, not to decide.** If the rose, the moss-black or any palette value collides with the thorn hazard, the bounce pod, the thread anchor or a Route B creature at mid values, show the clash and propose an alternative on the palette sheet. Don't silently change the palette.

### Mystery rule (automatic fail, stricter here)

The world is a sleeping titan, and players must not suspect it. Nothing may read as a body: no ribs, bones, vertebrae, skin, fingers, knuckles, faces, eyes, lids, brows, veins or hair. **This route is called Ribwood and is full of roots, so it is the highest-risk route.**

- Roots are plant roots: irregular, branching, knotted, never parallel curved bundles and never arcs repeating like a ribcage.
- Trellis and arches are squared, built joinery and masonry, never rounded, tapering or limb-like.
- **Rose appears only in petals and light, never on a large surface.** No pink, fleshy or glossy organic masses. Bloom clusters have readable petals, never smooth bulbs (they'd read as eggs or organs, and as bounce pods).
- Check every piece at full size, at 10% and blurred.

### Readability rules (Route A's, plus these)

- Qori is 0.8 × 1.2 u. Nothing behind gameplay may use Qori's or Route B creatures' colours at mid values.
- Background luminance stays at or below 70% of the walk line's, except small light sources. The walk line is the brightest, sharpest edge on screen. Greyscale check at 1280 × 720.
- **Gameplay shapes are reserved.** Within 3 u of any walk line or jump path, the background never shows: thorn-like spikes, rings or loops (thread anchors), round pods or bulbs (bounce pods), green moss walls (climbable), or a lit horizontal top edge (a ledge). Planting beds and terraces behind play are dark, broken and lipless.
- Trellis grids must not read as climbable. Keep them dark, partly hidden and low-contrast.

## 3. Layers and density

Exactly Route A's section 3: FG 110%/105% (ceiling silhouettes only), rear wall 100%, MID 88%/94% (one landmark per room, base always hidden), FAR 50%/60% **only in B04 and B06**, the two open chambers. 216 px per unit, never upscaled, max 4096 px per side.

**Memory (applies from 50b; plan for it now).** Unity 6.3 imports non-power-of-two textures that have mipmaps as RGBA32, ignoring the BC7 override. Route A had to switch mipmaps off afterwards (1,092 MiB down to 207 MiB). Route B will import with **mipmaps off and BC7** from the start, and every size will be a multiple of 4 px. Large dark areas should be shared tiling kit, not unique paintings. In 50a, propose a texture budget for the whole route (estimated MiB at BC7, by layer) beside the key frames.

## 4. The rooms (internal ID in brackets; rect from `rooms.json`, world units, origin bottom-left)

| Room | Rect (x, y, w, h) | Type | Layers | Signature (rear wall unless marked MID) | Light |
|---|---|---|---|---|---|
| B01 Garden Arrival (C01) | 1546, 330, 36, 24 | Enclosed arrival | rear wall, MID | The nursery's entrance hall: the first planting terraces, a root-broken ceiling hole with faint daylight. MID: a tall Keeper trellis gate, half-strangled by roots | Daylight + 2 lanterns, first rose |
| B02 Canopy Walk (C02) | 1588, 366, 42, 12 | Low corridor (compression) | rear wall only | Hanging planter troughs on chains under a low lattice ceiling; root curtains. **No vista, no far, no MID** | Dim |
| B03 Root Ladder (C03) | 1644, 320, 16, 58 | Shaft | rear wall, MID | An espalier wall: roots trained up old brass frames, now running wild; the B10 side door at mid-height. MID: an irrigation bucket chain | Lanterns, mint in root seams |
| B04 Seed Cradle (C04) | 1564, 270, 64, 36 | Open chamber, the hub (release) | rear wall, MID, FAR | The great seed vault: walls of seed drawers and jar shelving. MID: a granary tower of stacked jars (base hidden). FAR: dark terraces stepping back into darkness, a few lanterns on far terraces, no lit lips | Lanterns + rose clusters |
| B05 Rootfall Well (C05) | 1644, 248, 16, 58 | Shaft | rear wall, MID | Where Ribwood's roots broke through: shattered masonry, soil falls, old terraces hanging in pieces | Mint, sparse |
| B06 Bloom Court (C06) | 1674, 272, 54, 30 | Open chamber | rear wall, MID, FAR | The flowering court: a lattice dome with bloom clusters. MID: a dry Keeper fountain-planter overgrown with blooms. FAR: flowering arcades receding into darkness | Rose, strongest so far |
| B07 Gardener Rest (C07) | 1742, 278, 28, 16 | Enclosed rest | rear wall only | The gardeners' potting room: tools, seed drawers, benches, bedding niches. Everything at 100% | The warmest room |
| B08 Root Guardian Arena (C08) | 1742, 236, 40, 28 | Enclosed arena | rear wall | A monumental sealed nursery gate overgrown with roots; rose and mint light through its seams; the Heart is behind it. The fight floor stays quiet | Rose + mint, strongest |

B09 (C09) and B10 (C10) are optional rooms and out of scope until the user says otherwise. Passages use a tunnel kit as in Route A.

## 5. Stage 50a: what to deliver

1. **Three key frames over the real terrain**, from a terrain-and-Qori-only plate captured with your existing tooling at 3840 × 2160 (orthographic half-height 5, so 17.778 × 10 u). Concept resolution is fine for the painting, labelled **CONCEPT**, never upscaled.
   - **B01** at camera (1562, 335): the arrival and the first anchor over the thorn bed.
   - **B04** at camera (1596, 281): the hub's lower half, with FAR visible.
   - **B06** at camera (1701, 280): the swing chain over thorns, with FAR visible.
   If a position doesn't show the beat named, move it within the room's camera clamp and record why.
2. **One "route strip"**: a small composition thumbnail for each of B01-B08, side by side, so the user can see the light and colour progress from arrival to B08.
3. **A palette and value sheet**, with the sampled gameplay and creature colours and any clash (section 2).
4. **Greyscale versions** of the three key frames at 1280 × 720, with the walk line marked.
5. **A mystery check sheet**: each key frame at full size, at 10% and blurred.
6. **A reserved-shape check**: each key frame with a 3 u band around every walk line and jump path, showing nothing in section 2's reserved list falls inside it.
7. **A texture budget estimate** for the whole route (section 3).
8. `REVIEW_HANDOFF_50a.md`: your verdict per item and anything ambiguous, written down rather than guessed. Re-check the next free CLAUDE_REVIEW number, but don't write a review.

**Stop.** Claude reviews and the user says go before any 50b work.

---

## Direction change, 10 Nov 2026 (user-approved; supersedes the "built" emphasis above)

The underground is a journey from the people's world into the titan's own. Built : natural is roughly **A mostly built, B about 1 : 2, C traces only (cairns, ropes, chimes in wind-carved caverns), D almost none (ancient stone, wood grain, mint, silence)**. Route B keeps its palette, lilac, lights and rules, but natural rock and living roots are now the base, built pieces are half-swallowed fragments, drawers are rare, and mint and lilac carry more of the light than lanterns. See `CODEX_PROMPT_BATCH50B_R2_KEEPERS_NURSERY_B04.md` section 1.
