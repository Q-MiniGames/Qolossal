# Qolossal world redesign (proposal, v2)

**Status: proposal, not approved.** It does not replace `QOLOSSAL_WORLD_DESIGN.md` until you approve it. Neither design-review proposal is decided.

- v1 was written on 1 Oct 2026.
- v2 was written on 2 Oct 2026 and adds:
  - the titan as a mystery
  - the town
  - side chambers
  - the old man's warnings
  - permission to make the titan bigger

## Your decisions so far

**1 Oct 2026**
- The seated pose.
- One continuous climb.
- Stirs stay, as local moments.
- The colossus is redesigned from scratch.
- Portals, the vein maze and map-wide stirs go.
- Build a Palm prototype first (done: `Assets/Scenes/Proto_Palm.unity`).

**1–2 Oct 2026**
- **The titan is a mystery.** Players only find out they're on a titan in the final level, when they see its head and the complete map reveals the body.
- **A town** on the titan is the place to come back to. It offers:
  - upgrades
  - handing in a currency
  - homes you can enter to talk to people
  - a quiet spot to sit and listen to music
- **An old man knows the truth.** He warns you not to go on, and not to wake "the mountain" (or "the city").
- **Caves and small chambers** sit off the route. In them you solve a small puzzle or find something: a weapon, a person, and so on.
- **No titan in the backgrounds** if it won't look good.
- **The titan can be bigger** if that looks better.

---

## 1. The idea in one paragraph

Qori sprouts in a strange, warm valley and climbs. The land is odd: rounded ridges in rows of five, ravines shaped like creases, ground that is warm and sometimes trembles. Halfway up is a town whose people have lived here for generations without asking why the hills hum. Every time Qori wakes a knot the land shifts, and an old man in the town grows more afraid: *"Don't go higher. Let the mountain sleep."* Only on the last climb, onto the face, does the player see the closed eye and understand. The map fills in its last piece and shows the whole body: everything they walked was a hand, an arm, a lap, a chest. A second playthrough is a different game, because now every ridge is a finger.

## 2. The mystery: rules for everything before the final level

1. **Nothing names the body.** No region, knot, ability, caption, Lore Stone or line of dialogue says hand, arm, eye, heart, giant or titan. Names describe what people *think* the place is (see section 9 for renames).
2. **No titan in the backgrounds.** Backgrounds show sky, weather and the distant valley. The body appears only as the ground you walk on and the cliffs around you. This also avoids a hard art problem, since big background body parts are difficult to make look good.
3. **The shapes are true.** The terrain *is* anatomy, built honestly (section 4). It just reads as landscape at the scale you see it. That makes the reveal fair, not a trick.
4. **Clues are fair and they build up.** An attentive player can start to suspect by the middle of the game. Section 6 lists the clue trail.
5. **The stirs are unexplained.** A stir is an earthquake: the land moves and paths open. People in town feel it too, and argue about what it means.

## 3. Scale: bigger, so it reads as landscape

The v1 scale (hand ≈ 120 u) was chosen so body parts would read as body parts. For a mystery we want the opposite: they should read as hills, and only make sense once you see the whole.

**Proposal: about 2.5× v1.**

| Feature | v1 | v2 | What it looks like in play |
|---|---|---|---|
| The whole titan, seated | ~1,200 u tall | ~3,000 u | never seen until the end |
| The hand | ~120 u | ~300 u | a whole region of 2–3 levels |
| A finger | 8 u thick | ~20 u (2 screens tall) | a long rounded ridge, a level of its own |
| Knuckle bumps | 3–4 u | ~8 u | hills along the ridge |
| Fingernail | 2–3 u | ~6 u | a smooth pale cliff that nothing grows on |
| Palm crease | 4–10 u deep | 10–25 u | a ravine, with water at the bottom |
| Between the fingers | | | deep gorges, perfect for side chambers |

At this size a finger is a ridge you walk along for minutes, which is exactly how the mystery should feel. The Palm prototype is at v1 scale; if you approve v2 I'll rebuild it larger.

## 4. The route (the seated titan, from the ground up)

The titan sits slumped against the valley wall. One hand rests palm-up on the valley floor, and that forearm lies over the raised knee. The player climbs the outside, then goes inside to the heart for the ending.

| # | What players call it | What it really is | Notes |
|---|---|---|---|
| 1 | **The Cradle** | the hand | Qori sprouts here. Five ridges, the gorges between them, a pale cliff at each ridge end. First knot. |
| 2 | **The Long Causeway** | the forearm | An old road and aqueduct climbing a long slope. The first people Qori meets. |
| 3 | **The Terraces** | the raised knee | Farms on a round hill, and one mill. |
| 4 | **Qvale (the town)** | the lap | A sheltered warm basin, the hub. See section 5. |
| 5 | **The Ribwood** | the chest | A forest growing between huge parallel stone walls. The ground rises and falls slowly: it breathes, though nobody says so. |
| 6 | **The Windward Heights** | shoulder and collarbone | Bare windy ridges, the climb toward the summit. |
| 7 | **The Summit (final level)** | the face | Cliffs, a lake with a seam, a forest of reeds, falls. Then the reveal. |
| 8 | **The descent (ending)** | inside, to the heart | After the reveal, down through the body to the heart. |

The **town is in the middle** of the route. You reach it after about 30–40 minutes. From then on the climb goes up from it, and you come back down to it.

**Getting back to town.** Portals are gone, but a hub needs an easy way back. Each Waymark (checkpoint shrine) lets you travel to town and to any Waymark you've already lit, as a plain fast-travel menu. This is not the old maze: destinations are known and chosen, never random.

## 5. Qvale, the town

### 5.1 What it is
A small community living in a warm, sheltered basin (really the titan's lap). Their homes are built into the curved ground and from fallen stone. They farm the terraces below and trade with the causeway. They've always known the ground is warm, that the springs pulse, and that the hills hum on quiet nights. They call it "the mountain's mood."

### 5.2 The town grows as you play
People Qori finds in the side chambers (section 7) move to Qvale. Each new resident opens a home, a service or a story. By the end it's a busy little town, which makes the ending's question harder: what happens to them if the mountain wakes?

### 5.3 Places in town

| Place | Who | What it does |
|---|---|---|
| **The smithy** | a smith (found in a chamber in the Causeway) | upgrades weapons with **Amber** and rare finds |
| **The herbalist** | | heart-seed upgrades, healing |
| **The mapmaker** | Scribble the beetle | sells map pieces of explored places. The map shows regions as separate patches and never joins them, until the end. |
| **The listening spot** | a musician | a bench under a tree. Sit, the HUD fades, the camera eases out, and you choose a track. Tracks are **song shells** found around the world, so the Suno music becomes a collectible. |
| **Homes** | residents | walk in to talk; each home has a small secret or a story line |
| **The old man's house** | the old man | warnings and the truth (section 6) |
| **The town's Waymark** | | fast travel |

### 5.4 Entering homes
Entering a home peels away its front wall: the camera stays in the same scene and the interior is revealed in place. That is cheaper than separate interior scenes, and it keeps the town feeling continuous. Separate interior scenes are the alternative if you want bigger interiors.

### 5.5 Currency: Amber
Hardened sap, warm to the touch. Enemies drop it, and it is found in chambers and hidden places. You spend it at the smithy and the herbalist. It also hands over to the townspeople: some homes have requests ("bring me 20 amber for the bell"), and finishing a request changes the town (a bell tower, a lit lantern street, a new bridge). The old design's Sap Vials become rare finds that unlock weapon tiers, alongside Amber.

## 6. The old man and the clue trail

**The old man** is the town's oldest resident, and the only one who knows the truth. He was there the last time the mountain stirred, or his grandfather was. He doesn't explain it; he warns. After each knot his warning is sharper. (A name to choose; placeholder: **Grandfather Tallow**.)

| When | What he says (draft tone) |
|---|---|
| First meeting | *"Climbing, are you? The higher you go, the louder it hums. Some things hum because they're sleeping."* |
| After the 1st knot (the first earthquake) | *"You felt that. Everyone did. Don't do it again."* |
| After the 2nd | *"My grandfather said the last time the ground moved like this, the river ran backwards for a week. And then it lay down again. Lay down. That's what he said."* |
| After the 3rd | *"There's a reason this valley is warm. Let it sleep, child. If it wakes, where do you think we'll be standing?"* |
| Before the summit | *"Then go. But when you get to the top, look down before you look up."* |
| After the reveal | He is the one waiting, and the one who decides to trust Qori. |

**The fair clues elsewhere** (each innocent alone, convincing together):
- The ridges come in fives. The fifth is shorter and set apart (the thumb).
- The ground is warm. Some springs pulse in a slow rhythm, the same rhythm everywhere.
- The Ribwood floor rises and falls very slowly. Townspeople call it "the swell."
- Each earthquake moves something that looks like it *bends*, not breaks.
- Lore Stones from the old builders: "We built on the sleeping hills, and asked them for patience."
- Children's rhymes in town about "the mountain who dreams."
- The map: each patch the mapmaker sells has an odd shape. The shapes only fit together at the end.

**The story question this gives us:** the town lives on the titan. Waking it could destroy everything the player has come to care about. The old man is right to be afraid. The approved "careful ending" answers this: the titan wakes slowly and holds the town safe in its palm or lap. That's the emotional payoff.

## 7. Side chambers and caves

Small spaces off the main route. Each takes 3–8 minutes and has one purpose.

| Kind | Example | Reward |
|---|---|---|
| **Puzzle chamber** | weights and plates, a water level, a light beam, a sequence of switches | Amber, a heart-seed shard, a song shell |
| **Weapon chamber** | a guarded hall with a small trial | the mace, the spear, or the sling, found rather than owned from the start |
| **Person chamber** | someone trapped or lost: rescue them, then they move to town | a new resident (a service, a home, a story) |
| **Secret** | a cracked wall, a hidden drop | a Lore Stone or a rare find |

In hindsight they are body places: the hollow between two knuckles, a gap under a tendon, a pore-like cave, the ear canal ("the Whispering Cave"). They are drawn as caves.

Six to eight chambers per region. One or two are visible from the main route but locked behind an ability you don't have yet, as a reason to come back. In the Palm prototype, the gorges between the fingers are the natural place for the first ones.

## 8. Stirs, now unexplained

A stir is still the body part you're standing on moving, and it still opens the way on. It just isn't named:

- **Waking a knot:** the light goes into Qori (the ability), the ground shakes hard, the camera pulls back, the land moves, and it settles.
- **The caption** says only what changed: *"The ground has shifted."* There is no "the titan stirs."
- **The Palm prototype's thumb fold** becomes "the great stone arch fell across the ravine." Nobody calls it a thumb.
- **The town reacts after every stir:** people talk about it, the old man warns, and something in town changes (a crack, a spring running stronger).

## 9. What changes from the current design and the Palm prototype

### Renames (nothing names the body until the end)

| Now | Proposed |
|---|---|
| Palm Hollow | The Cradle |
| Aqueduct Arm | The Long Causeway |
| Knee Terraces | The Terraces |
| Breathing Grove | The Ribwood |
| Falls Sanctuary and Tearglass Eye | The Summit (final level) |
| Rootdeep Heart | the descent (the ending) |
| Knot names | **Grip Knot, Breath Knot, Sight Knot…** become names by look or place: e.g. "the Cradle Knot," "the Swell Knot." |
| Echo's lines | rewritten: a dreamy voice, no body words |
| Stir captions | "The ground has shifted." |

### Keep
- Qori, movement, abilities, combat, enemies
- the guardians, as local creatures (renamed where their names give it away)
- the region tile kits
- the builders, tests and saving
- Waymarks (now also fast travel)
- Lore Stones, heart seeds
- Old Loam: could merge with the old man, or stay as a separate character
- the healed Wilted
- the careful ending

### New systems needed
- the town and its NPCs
- a dialogue system
- homes you can enter
- the listening spot and song shells
- Amber, shops, upgrades and requests
- Waymark fast travel
- the map as separate patches with a final assembly
- side-chamber puzzles

### Remove from the Palm prototype
- the bowed head over the palm
- the toes and shin in the background
- the "hand grips" caption

## 10. Decided on 2 Oct 2026

1. **The ending:** the player climbs onto the face. The final level is on the face, where they see it and the map completes. After the reveal, the descent to the heart.
2. **The old man:** a new character, separate from Old Loam.
3. **Interiors:** in-place cutaway.
4. **Scale:** about 2.5× approved (hand ≈ 300 u). The Palm prototype is being rebuilt at this size, without the face, with an unexplained stir and one side chamber.

## 11. Concept picks (Review 14, 2 Oct 2026)
- **The titan:** A, the Chalk Orchard. Limestone caps, root-tendon seams and olive meadow moss; a broad, sheltering mass.
- **Qvale:** B, Lantern Vaults. Homes tucked under curved overhangs of warm rock.
- **The old man:** B, the Lichen Moth. The other townspeople's concepts are approved: the smith (beetle), herbalist (newt), musician (cricket), child (mouse), farmer (hare) and lantern keeper (weevil).
