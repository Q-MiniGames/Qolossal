# Qolossal: the reclining titan (proposal)

**Status: approved 8 Oct 2026; being built in stages (see `QOLOSSAL_SESSION_HANDOFF.md` section 4.7).** Original note: Written 8 Oct 2026 from your sketch idea (a titan lying back, seen side-on, feet bottom-left, head top-right, the route climbing the shin to the knee, down the thigh into the lap, then up the body to the head, with floating platforms and caves around it). Nothing in the game, scenes or art has been changed. If you approve it, it replaces section 4 (the route) of `QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2). Everything else in v2 stands: the mystery rules (sections 2, 10, 11), Qvale, the old man, Amber, the chambers and the careful ending.

**Map:** `Tools/Design/QOLOSSAL_RECLINING_ROUTE_MAP_DESIGNER_SPOILERS.png` (and `.svg`). It is redrawn from scratch in code by `Tools/Design/reclining_route_map.py`, not traced from the sketch. The cave markers come from the real chamber list in `mra_world.json`. It is a designer spoiler and must never ship.

---

## 1. The idea in one paragraph

The titan lies on its back on the valley floor, propped up against the valley wall, one knee raised. Qori sprouts among its toes, climbs the long straight shin, crosses the round hill of the knee, and runs down the long slope of the thigh into a warm hollow where Qvale sits: the lap. From the town the land climbs again, slowly, up the belly and between the rib walls, over the shoulder, across the dip of the neck, and up a strange stepped cliff (the chin, the mouth, the nose) to a lake with a seam in it and a high pale dome. At the very top the old man's advice pays off: *look down before you look up.* Looking back, the whole route lies below in one line, all the way to the toes, and the shape is unmistakable.

## 2. Why this layout is better than the seated one

1. **The route is one honest line.** Feet to head is the order a 2D side-scroller already moves in: left to right, low to high. The seated pose needed a hand on the floor and a forearm laid over the knee to make one path; this one needs nothing invented.
2. **The rises and falls are real.** Shin up, knee crest, thigh down, lap low, torso up, neck dip, face up. Players feel the body's shape as level pacing long before they understand it.
3. **The reveal gets a better shot.** On the crown the camera pulls back and pans down the whole body to the feet. The player sees, in one move, every place they have been. The seated pose could only show this from outside.
4. **Qvale makes more sense.** A lap between a raised knee and a rising belly is a natural sheltered basin, walled on both sides.
5. **The careful ending gets stronger.** The titan wakes, lowers its knee gently, and its hand closes around the town in its lap.
6. **It keeps almost everything.** The region order, the abilities, the knots, the chambers and the accepted background art all stay. See section 6.

## 3. The journey, region by region

The order and ids are unchanged (MR01 to MR08). Heights below are the current prototype routes in `mra_world.json` (entry y to exit y, 1 u = 1 m).

| # | Name players see | What it really is | The route | Change needed |
|---|---|---|---|---|
| MR01 | **The Cradle** | the feet: toe tips, top of the foot, ankle | Qori sprouts high among five pale-capped pillars (the toes), walks down the top of the foot into the warm hollow at the ankle (the Cradle knot), and climbs out onto the shin. | Small. The current route already starts high, dips to its knot (y 12) and rises to the exit (y 40). The "pale cliffs" become the toenails. |
| MR02 | **The Long Causeway** | the shin | The old road runs straight up the shin bone, a long even ridge. | Almost none. It already climbs 158 u in a long run. |
| MR03 | **The Terraces** | the knee, then down the thigh | Farms up the knee's round hill to the mill on the kneecap (Mill Ridge, the Terraces knot), then a **long descent down the thigh** into Qvale. | **The biggest change.** Today MR03 climbs 150 u and ends near its top (crest y 181, exit y 162). The second half must turn into a downhill: switchbacks, slides and drops along the thigh, ending at the Town overlook above Qvale. |
| MR04 | **Qvale** | the lap | The town in the hollow, with the thigh rising behind it and the belly rising ahead. | None to the town. Its far walls are now the thigh (left) and the belly (right). |
| MR05 | **The Ribwood** | the belly, then the ribs | A long gentle climb out of the lap up the belly, then into the forest between the huge parallel stone walls (the ribs). The swell stays. | Small: the first stretch is open slope (the belly) before the rib walls begin. |
| MR06 | **The Windward Heights** | collarbone, shoulder, neck | Bare windy ridges over the shoulder, then **down into a deep sheltered dip (the neck)** before the last climb. | Small: add the neck dip at the end of the route. |
| MR07 | **The Summit (final level)** | the face, looking up | A stepped cliff (the chin and lips), a long bare prow (the nose), the lake with its seam (the closed eye) fringed by reeds (the lashes), the brow, and the pale dome of the forehead. The reveal is at the crown. | Medium: re-order the current beats to climb chin, mouth, nose, eye, brow (section 3.1). The reveal camera now looks back down the body. |
| MR08 | **The Descent (ending)** | inside, from the head down to the heart | After the reveal, down through the throat into the chest to the heart. | None to the idea. It now travels back left and down, under the route the player climbed. |

### 3.1 The Summit, re-ordered

The current MR07 beats map onto the face cleanly:

| Current beat | On the face |
|---|---|
| Lower falls | water spilling over the chin |
| Pale steps | the lips and teeth (pale, ridged) |
| *(new)* the Prow | the nose: a long bare ridge, warm air rising off its end |
| Reed approach, Reed bed | the lashes along the lower lid |
| Lake shore, Seam ledge | the closed eye and its seam |
| Upper shelf | the brow |
| Summit knot | the forehead |
| Lake lookout, Reveal ledge | the crown: look back down the whole body |

The lake and reeds stay in the terrain, never in background planes, as the mystery rules already require.

## 4. The floating-platform levels (the green dashes)

Four short platforming levels in the air beside the body. Each is its own scene, 4 to 8 minutes, entered from a marked launch point on the main route and returning to it. They use the existing `MovingPlatform`, `CrumblePlatform` and `GrowthPlatform` mechanics.

**What they are in the story:** *drift stones*, slabs of rock that float on the warm air rising off the land. Nobody knows why the air is warm. In hindsight it is body heat, and over the face it is breath.

| Level | Where | Entered from | Ability it asks for | Reward and town effect |
|---|---|---|---|---|
| **P1 Stepping Stones** | above the shin | MR02, after the Living Thread shrine | none (teaches drift stones), thread for the top shelf | song shell, Amber |
| **P2 Lantern Drift** | the sky over Qvale, between knee and belly | the thigh descent (MR03) or the town | Bloomfall (bounce on drifting pods) | a heart seed; the lantern keeper's lost lanterns are recovered and the sky over town lights up |
| **P3 The Swell Isles** | above the chest | MR05, after Wind Leaf | Wind Leaf (dash) | rare find (a weapon tier) |
| **P4 Updraft Stair** | above the shoulder, rising to the face | MR06, after Glidecap | Glidecap (the updraft lifts you) | a Lore Stone, song shell |

**Fair clues they add:** the Swell Isles rise and fall in the same slow rhythm as the Ribwood's swell and the pulsing springs. The Updraft Stair's air comes in long slow gusts, then stops, then comes again.

**Recommended: all four are optional.** The main route is always walked on the body, so the reveal is earned on foot and nobody can skip the shape of the land. If you want more platforming on the critical path, P3 is the one to make mandatory (it shortcuts the steepest part of the chest climb).

## 5. Caves and side chambers (the red marks)

Every existing chamber keeps its puzzle, reward and region. In hindsight each now sits in a body place. The map places them by their entrance nodes.

| Region | Body places for its caves |
|---|---|
| MR01 Cradle (feet) | between the toes (Warm Crack, Moss Pocket), under a toenail (**Pale Alcove**), the ankle hollow (Root Gallery), the heel (High Crevice, Hanging Garden) |
| MR02 Causeway (shin) | under the shin bone's edge (Underarch Workshop, the smith), along the calf (Silt Cistern, Echo Vault, Ring Gallery, Moss Tower, High Arch) |
| MR03 Terraces (knee, thigh) | **inside the kneecap** (Mill Undercroft), the knee's slopes (Farmer Shelter, Grain Store, Sluice Cellar, Wind Shelf) |
| MR05 Ribwood (belly, ribs) | **the Navel Well** (Moss Well moves here: a round wall-climb shaft in the belly), between the ribs (Seed Choir, Root Loom, Canopy Pocket), over the heart (**Stone Drum**), Lost Lantern |
| MR06 Heights (shoulder, neck) | the armpit hollow (Sheltered Nest), the collarbone hollow (Clasp Hollow), Wind Window, Echo Steps, High Drift, Weather Cave |
| MR07 Summit (face) | in the lashes (Reed Shelter), the mouth corner (Falls Pocket), **behind the teeth** (Pale Archive), the ear (Watcher's Hollow), Quiet Terrace, Upper Cleft |
| MR08 Descent | Quiet Alcove, Root Rest (inside) |

**One new big cave: the Under-Arch.** The space under the raised knee is a huge sheltered arch, visible from below on both the shin and the thigh. Recommended: move **Root Hanger** (MR03_C06, the glide trial) there, so it needs no new chamber, only a new entrance and art. It is reachable late (Glidecap), which gives a strong reason to come back.

## 6. What changes if you adopt it

### Stays as it is
- Region order, names, ids and scenes; the knots; the ability order (Climbing Moss, Living Thread, Bloomfall, Wind Leaf, Glidecap); all 38 caves and chambers outside town, plus the smithy and five Qvale homes.
- All accepted background art. Backgrounds still show only sky, weather and the far valley (rule 2), so the Batch 14 and 15 depth sets are unaffected.
- Qvale, the old man and his lines ("look down before you look up" now fits literally), Amber, the route bar, the music, the SFX.

### Game changes
1. **MR03 second half becomes the thigh descent** (route data in the design package, then `MraWorldBuilder` rebuild). This is the largest job and needs a play-test.
2. **MR06 gains the neck dip** at its end; **MR07's beats are re-ordered** (section 3.1) and gain the Prow.
3. **The reveal camera** pans back down the body from the crown instead of pulling out from the face.
4. **The careful ending:** the knee lowers and the hand closes around Qvale.
5. **Four new floating-platform scenes** (P1 to P4) with launch points on the main route, save flags and Chart markers.
6. **Two chambers re-homed:** Moss Well to the Navel Well, Root Hanger to the Under-Arch (new entrance nodes).
7. **Clue trail update** (v2 section 6): the "ridges in fives" clue moves to the toes at the start and returns at the hand beside Qvale; add the drift stones' rhythm.
8. The Palm prototype (`Assets/Scenes/Proto_Palm.unity`) no longer matches the start; it can be retired.

### Art changes
1. **The Chart's final assembly** (the seven patches joining into the body) must be redrawn as a reclining body. Today it is the seated design (`Batch11_A_Chart_Final_7Patches_DESIGNER_SPOILERS`). Each region patch's outline changes with it.
2. **Region relief pages** for MR01 (hand to feet), MR02 (forearm to shin) and MR03 (add the thigh) need new outlines. They are review-only sources, not shipped art.
3. **Reveal art:** `Reveal_Face_02/03` (accepted, not yet wired in) must be checked against a face looking up. They may need a redo.
4. **New kit: drift stones** (floating slabs in the Chalk Orchard look: limestone caps, root-tendon seams, olive moss) for the four platforming levels.
5. **Two cave entrances:** the Under-Arch and the Navel Well.
6. **Optional:** a proper painted world map, for the designer and for the final Chart (prompt below).

## 7. Decisions for you

1. **Adopt the reclining layout?** Recommended: yes.
2. **Floating-platform levels optional or on the main route?** Recommended: optional (P3 can be mandatory if you want more platforming on the way).
3. **The near hand beside Qvale:** keep it (it carries the fives clue and the ending)? Recommended: yes.

## 8. Codex prompt (only if you want painted map art later)

Names checked free in the accepted manifest on 8 Oct 2026.

> **Qolossal: Batch 18, the reclining titan world map (designer spoilers).** Paint `Map_World_Reclining_DESIGNER_SPOILERS` (3840x2160 PNG) and `Chart_Final_Reclining_DESIGNER_SPOILERS` (1920x1080 PNG). Use `QOLOSSAL_RECLINING_ROUTE_MAP_DESIGNER_SPOILERS.png` as the layout reference only: keep its pose, proportions and region order, but paint it properly, do not trace its shapes or copy its flat style. Subject: a colossal titan lying on its back in a valley, seen exactly side-on, feet at the left, one knee raised, head at the upper right propped against the valley wall, face turned up. The body is landscape in the approved Chalk Orchard style: limestone caps, root-tendon seams, olive meadow moss, small farms on the knee, a lantern-lit town (Qvale, Lantern Vaults style) tucked in the lap, a forest between rib-like stone walls, a lake with a seam over the closed eye, reeds along it. Floating drift stones in four loose clusters above the shin, over the lap, over the chest and rising to the face. Warm dawn sky (match `BG_Sky_Terraces`). No text, no route lines, no labels, no UI. For the Chart version, the same pose as seven separate parchment-relief patches that fit together, in the existing Chart style (`Chart_Base`, `Chart_Region_R*`). Deliver with a `MANIFEST.json` (path and sha256 per file) as in earlier batches.
