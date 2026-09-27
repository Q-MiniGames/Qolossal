# Qolossal: world and game design (v1)

The big picture first: the Qolossal itself, then the world map cut from its body, then the systems, story, characters, levels, art and music that follow from it. This document drives level building from here on, and the Codex request in `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH5.md`.

---

## 0. The pitch

> **The whole world is asleep, and it is one creature.**
>
> Long ago a titan of stone and living wood lay down in the valley and slept so long that a landscape grew over it. Forests rooted in its bark, rain carved channels into its arms, and people built shrines on its brow without ever knowing it was alive. Now a thorny rot is strangling it from the inside. Qori, a seed sprouted from the titan's own heart, wakes in its open palm. To save the valley, Qori must travel the titan's body through its veins, rekindle the seven knots where its life gathers, and wake the Qolossal before the rot reaches its heart.

### Design pillars
1. **One body, one maze.** Every level is a place on the titan's body. The map *is* the titan, and it is revealed piece by piece.
2. **The world moves.** Each knot Qori wakes makes the titan stir. A hand clenches, an arm lifts, a chest breathes. Earlier levels physically change and open new paths.
3. **Abilities are the titan's own functions.** Grip, reach, breath, spring, bloom and sight are the powers of its body parts, returned to it one by one, and lent to Qori.
4. **Quiet wonder, not noise.** The story is told through places, short dream-voices and a few kind (and one sad) characters. Scale is felt, not explained.

---

## 1. The Qolossal: the body first

### 1.1 The titan
- **What it is:** a guardian of the valley, part ancient stone (bones, face, knuckles) and part living wood (bark skin, root sinews, heartwood). Its life glows **mint**, the same glow as Qori, the relics and the seed pods.
- **Size:** about 2,000 Qori heights long. A whole level spans roughly the width of one of its fingers. Its full shape is never seen up close; only from high places, the map and the stir cinematics.
- **Pose (asleep):** lying on its back in the valley, **head to the east**, knees drawn up in the west. Its **left arm rests across its belly with the palm open to the sky**, which is where Qori sprouts. The right arm lies buried under the western hills (reserved for a sequel or a post-game).
- **Condition:** the **Thornrot**, a parasitic bramble, has wound through its veins and knotted its nerves shut. Its creatures (Bramble Crawlers, Thornwings, Pod Spitters) are the rot's spawn. The titan's own defenders (Sentinels, the Wardens) no longer recognise friend from foe.

### 1.2 The body map (side view, as the world map shows it)

```
   WEST (feet)                                                             EAST (head)
                                                                      R5 FALLS SANCTUARY
          R4 KNEE TERRACES            R3 BREATHING GROVE               (brow and crown)
             /\      /\             .-~~~~~~~~~~~~~~~~~-.               .-~~~~~~-.
            /  \    /  \          /  forest on the ribs   \            /    ~~    \
           /    \  /    \________/    [ R7 ROOTDEEP HEART  ]\__________|  (o)  R6 TEARGLASS EYE
    feet  /      \/  thighs  belly      inside the chest ]    neck      \  face   /
  rooted_/________ R1 PALM HOLLOW  (open left hand on the belly) _________\______/
                      \____ R2 AQUEDUCT ARM (forearm and elbow down to the valley floor)
```

### 1.3 The seven regions

| # | Region | Body part | Kit (built / new) | Knot and function | Ability gained | Guardian | Mood and accent |
|---|---|---|---|---|---|---|---|
| R1 | **Palm Hollow** | the open left hand on the belly | A0 (built) | **Grip Knot** | Climbing Moss (wall cling and jump) | Knucklebramble (mini-boss) | mossy morning, mint |
| R2 | **Aqueduct Arm** | forearm and elbow | A1 (built) | **Reach Knot** | Living Thread (grapple) | Cistern Matriarch | cool stone and water, aqua |
| R3 | **Breathing Grove** | ribs and upper back | A2 (built) | **Breath Knot** | Wind Leaf (dash) | Grove Warden | warm bark, amber |
| R4 | **Knee Terraces** | bent knees and shins | **A5 (new)** | **Spring Knot** | Bloomfall (pogo bounce) | Hollowhorn | terraced fields, clay and ochre |
| R5 | **Falls Sanctuary** | brow and crown | A3 (built) | **Bloom Knot** | Glidecap (glide) | Brow Sentinel | airy heights, pale gold |
| R6 | **Tearglass Eye** | the closed eye and its socket | **A6 (new)** | **Sight Knot** | Seer's Lantern (light and reveal) | Glassmoth Queen | crystal lake, pale blue |
| R7 | **Rootdeep Heart** | inside the chest | A4 (built) | **the Heart** | the Waking (ending) | The Wilted, then the Thornheart | dark, bright mint |

Each region is **3 levels plus a Knot Chamber**, so 28 playable spaces in all: about 6 hours for the main path and 8–10 hours with secrets.

### 1.4 Ability gating (which knot opens which region)

```
R1 Palm (Grip)
   └─► R2 Arm (needs Grip) ── Reach ──┬─► R3 Grove (needs Reach)  ── Breath ──┐
                                      └─► R4 Knees (needs Reach)  ── Spring ──┤   either order
                                                                              ▼
                                                    R5 Crown (needs Breath + Spring) ── Bloom
                                                                              ▼
                                                    R6 Eye (needs Bloom) ── Sight
                                                                              ▼
                                  R7 Heart (needs Sight, and all six knots awake)
```

Veins (section 3.1) let players *reach* regions early, but each region's deeper levels and its Knot Chamber need the abilities above. Early visits are for scouting, secrets and seeing what is locked.

---

## 2. Story

### 2.1 Backstory (never told in one piece)
The Qolossal kept the valley alive: its breath was the wind, its tears were the rivers, its heartbeat the seasons. When the Thornrot came, the titan lay down to fight it from inside and fell asleep. Over centuries, people forgot, and built on it: aqueducts along its arm, a sanctuary on its brow, terraced fields on its knees. The rot grew. The titan sent out seeds from its heart to wake it. One came before Qori, and failed.

### 2.2 The three acts

| Act | Regions | What happens | Ends with |
|---|---|---|---|
| **I. Sprout** | Palm, Arm | Qori wakes in the palm, meets Old Loam, learns what the world is. The first knot: the hand **clenches**, and the ground itself moves. | The arm **lifts** from the valley floor, and Qori glimpses the titan's face on the horizon. |
| **II. Stir** | Grove, Knees | Two directions, player's choice. The titan **breathes**, and wind returns to the world. A figure of withered leaves is seen fleeing ahead: The Wilted. | The knees **straighten**, and the Wilted reveals itself as the earlier seed, taken by the rot. |
| **III. Wake** | Crown, Eye, Heart | The climb to the head. The crown **blooms**; the eye **opens** and light floods the body. Qori descends into the heart. | Qori frees The Wilted, cuts out the Thornheart, and the Qolossal **wakes**. |

### 2.3 Ending
The titan sits up. The valley rises with it, and the landscape pours from its shoulders like a landslide of forests and ruins. Qori sits in its palm, as at the start. The Wilted, healed, blossoms beside it. The last shot is the titan standing for the first time in a thousand years, with Qori on its shoulder, looking at a world it has not seen since it fell asleep.

*Sequel hook:* the "Risen" world, the same body with the titan standing (the map turned upright), and the right arm freed.

### 2.4 How the story is told
- **Places first.** Aqueducts running along a forearm, shrines on a brow, fields on knees. The player works it out before being told.
- **Echo, the titan's dream-voice:** a mint wisp that speaks one or two lines when a knot wakes. It is old, slow and kind, like something talking in its sleep.
- **Stir cinematics** (8–12 seconds): the camera pulls back, the body part moves, the map redraws itself.
- **Old Loam** at each region's Waymarks: short conversations, never more than four lines.
- **Lore Stones:** carved tablets, one per level, which tell the people's side of the story ("the hill that sighs").
- **No text walls, no narrator.**

### 2.5 Sample lines
- Echo, at the Grip Knot: *"...a hand. I had forgotten I had hands. Little seed... is that you?"*
- Echo, at the Breath Knot: *"Air. It burns like frost. Keep going. I can feel the thorns listening."*
- Old Loam, first meeting: *"Sprouted in the palm, did you? Then you know where you are, even if you don't know it yet."*
- The Wilted, in Act II: *"It won't wake. I tried. Stay asleep with it. It's quieter."*

---

## 3. Core systems

### 3.1 Veins: the portal network
Portals become **Vein Gates**, the titan's veins, linking places far apart on its body. The player's request: portals send Qori to unexpected spots on the body, with no straight path. For that to feel random *and* still make a learnable maze:
- **Every Vein Gate has a fixed destination, and it is unknown until first used.** It shows as "?" on the map, and once travelled the map draws the vein line between the two places. Destinations deliberately jump across the body (a Palm gate might open in the Crown's lowest level).
- **Wild Veins** (one per region, marked by a flickering membrane) *are* random, every time: each use throws Qori to a random **already-charted** Waymark or unvisited vein end. When that region's knot wakes, its Wild Vein **settles** to a fixed, useful shortcut.
- **Stirs open new veins:** each knot opens one or two new Vein Gates elsewhere on the body (section 3.3).
- **Built today:** the existing `Portal` and `AreaTransition` (portal ids, destination portal, fade, keeps hearts and weapon). The additions are the map record, "unknown until used", and the Wild Vein mode.

### 3.2 The Chart: the map of the titan
- The map screen is a painting of the whole Qolossal, **covered in moss and mist**.
- **Levels are charted by reaching their Waymark** (a small root shrine deep in each level, doubling as a checkpoint). Charting clears the mist over that level's part of the body and shows its rooms as tiles, its vein ends and its secrets count.
- **Waking a knot paints the region in colour** and plays its part of the body moving on the map.
- **Icons** (mostly delivered already): portal, checkpoint, shrine, secret, unexplored and Qori's head; plus new icons for knots, Waymarks, Wild Veins and NPCs.
- **Scribble** the beetle cartographer adds notes to the map: hints about locked paths ("the arm is too heavy to lift... for now").

### 3.3 Wakeknots and Stirs: the heart of the design
**A knot is waking in five beats:**
1. **Reach the Knot Chamber:** a level built around the region's puzzle theme.
2. **Unlock the knot:** a multi-step chamber puzzle using everything the region taught.
3. **The guardian:** the region's boss, a defender the rot has turned.
4. **Rekindle the knot:** Qori places his hand on it. It gives the ability (the relic-shrine moment, already built) and plays Echo's lines.
5. **The Stir:** a short cinematic of the body part moving, then the map redraws. **Earlier levels are now different.**

**Types of change a Stir can make:**

| Change | Example | How it's built |
|---|---|---|
| **Tilt** | the arm lifts, so water runs the other way | two versions of the geometry, and the level loads the one for the current state |
| **Open or close** | a clenched finger closes a pit and opens an arch | stir gates: bark and stone gates with before and after states |
| **Flow** | water drains, sap hardens, wind starts to blow | water, sap and wind objects that exist before or after a stir |
| **New veins** | a new Vein Gate grows in an old level | portals that appear after a stir |
| **Creatures** | Gust Moths appear once the titan breathes | enemies that spawn before or after a stir |

**The stir matrix (what each knot changes, and where):**

| Knot woken | The titan... | Changes in earlier levels | New vein |
|---|---|---|---|
| Grip (R1) | clenches its hand into a loose fist | Palm: finger-ridges curl into climbable arches; the Crease pit closes into a bridge | Palm to Arm (elbow) |
| Reach (R2) | lifts its forearm | Arm: channels drain downhill, so the flooded galleries become walkable, and the waterfall moves. Palm: the wrist rises into a ramp to the Grove | Arm to Knees |
| Breath (R3) | breathes for the first time | **All regions:** wind blows in timed inhale and exhale gusts. Grove: the canopy spreads into new bridges. Heart: its airway opens a way in | Grove to Crown |
| Spring (R4) | flexes its legs | Knees: fields slide to reveal caves. Palm and Arm: tremors collapse crumble bridges into shortcuts | Knees to Eye (low) |
| Bloom (R5) | flowers at the crown | **All outdoor regions:** seeds drift on updrafts, making Glidecap routes. Crown: the falls reroute | Crown to Heart (sealed) |
| Sight (R6) | opens its eye | **All regions:** light shafts pierce dark areas, and Seer's Lantern secrets become visible. Eye: the lake drains into the heart | Eye to Heart (open) |
| Heart (R7) | wakes | the ending (the Risen world is saved for a sequel) | none |

### 3.4 Abilities

| Ability | Relic (art status) | Body meaning | What it does | Gates |
|---|---|---|---|---|
| Climbing Moss | built | the grip of its hand | wall cling, slide and jump | vertical shafts; the Arm |
| Living Thread | built | the tendon of its arm | grapple and swing; **new:** pull tendon anchors | wide gaps; tendon puzzles; the Grove and Knees |
| Wind Leaf | art delivered, **new code** | its breath | a short air dash, reset on landing or on a ring | long gaps, wind currents |
| Bloomfall | built | the spring of its knees | a pogo bounce off enemies and hazards | thorn fields; bounce puzzles |
| Glidecap | art delivered, **new code** | the bloom of its crown | glide while holding jump; ride updrafts | the Eye's lake; tall drops |
| Seer's Lantern | **art needed**, new code | the sight of its eye | light radius in darkness; reveals false walls and hidden platforms | the Heart; secrets everywhere |

Change from what is built: the A0 test room currently hands out three relics at once. In the real Palm levels, R1 gives only Climbing Moss; Living Thread and Bloomfall move to R2 and R4.

### 3.5 Level structure (longer, with puzzles)
**Target per level:** 8–12 minutes on a first run, 350–500 units long (the current A1 room is about 150), with 3–4 checkpoints.

**Every level follows the same beats:**
1. **Arrival:** a Vein Gate, and a safe view of the level's theme.
2. **Teach:** the level's puzzle idea introduced with no danger.
3. **Develop:** that idea combined with combat or platforming.
4. **The locked sight:** a path visibly blocked until a later stir or ability. It plants the reason to come back.
5. **Challenge:** a combat room or platforming gauntlet.
6. **Waymark:** the level is charted, with a checkpoint.
7. **Exits:** one or two Vein Gates onward, plus one secret (a heart seed shard, a Lore Stone or a Sproutling).

**Every region follows the same shape:** level 1 teaches the region's new idea, level 2 combines it with earlier ones, level 3 raises the stakes, and the Knot Chamber is the exam, followed by the boss.

### 3.6 The puzzle toolkit
**Already built:** pressure plates, sling seed switches, root gates, weapon-only breakables (sword cuts thorns, mace breaks rubble, a downward strike breaks weak floors, the spear pierces bark shields), crumbling and moving platforms, falling rocks, false walls, swing rings, water to wade through.

**New, from the titan's body:**

| Mechanic | Region | How it works |
|---|---|---|
| **Pulse bridges** | Heart (and Palm veins) | vein bridges swell on the titan's heartbeat, walkable only on the beat. The beat speeds up as the game goes on |
| **Clench platforms** | Palm | finger-ridges open and close with pressure plates; ride a closing finger up |
| **Sluices and water level** | Arm | open channel gates to raise and lower water; floating logs rise with it |
| **Tendon pulls** | Arm, Grove | hook the Living Thread to a tendon anchor and pull, bending a branch or dragging a root-bound stone |
| **Breath currents** | Grove, then everywhere | wind blows in one direction on the inhale and the other on the exhale; dash and glide with it |
| **Sap valves** | Grove | burst sap sacs with the sling; the sap hardens into amber bridges for a while |
| **Heavy plates** | Knees | lure a Shellback onto a plate, or pogo it into place |
| **Mill wheels** | Knees | root-and-stone wheels turned by wind or weight rotate platforms |
| **Light and mirrors** | Eye | turn crystal mirrors to bend a light beam onto receivers; the Seer's Lantern shows hidden paths |
| **Darkness and glow pods** | Heart | hit a glow pod to light the area around it for a few seconds |

### 3.7 Collectibles and progression
- **Heart Seeds (built)** and **Heart Seed Shards** (art delivered): 4 shards make 1 seed. Each region hides 1 seed plus 2 shards.
- **Sproutlings:** tiny lost seed-spirits, 3 per region. Bring them to Old Loam; every 3 returned grows a new heart-seed shard and a line of backstory.
- **Lore Stones:** 1 per level; each adds a tablet to the journal.
- **Sap Vials** (art delivered): the weapon-upgrade material, found in secrets and dropped by guardians. They upgrade weapons to tier 2 (art delivered).

### 3.8 Combat notes
Weapons stay keys as well as weapons (the sword cuts, the mace breaks, the spear pierces, the whip reaches, the sling triggers switches). Each guardian is a puzzle as much as a fight, and its weak point is always its own **knot**, a mint glow the rot is feeding on.

---

## 4. Characters

| Character | Role | Where | Design notes |
|---|---|---|---|
| **Qori** | the hero, a seed-spirit from the titan's heart | everywhere | built |
| **The Qolossal** | the titan; its voice is **Echo**, a slow mint wisp | knots, cinematics | Echo is a soft mint wisp with a faint face that forms and dissolves |
| **Old Loam** | an ancient snail who carries a small moss shrine on its shell; kind, dry humour; remembers the titan from before | Waymarks in every region | slow, wise, a little funny. The shell shrine is a mini-Waymark (a travel hint) |
| **Scribble** | a bark-beetle cartographer; draws the Chart and scribbles hints on it | one spot per region | excitable, inky mandibles, carries a rolled leaf map |
| **Sproutlings** | lost seedlings, collectibles | hidden, 3 per region | tiny, frightened, and they follow Qori once found |
| **The Wilted** | the seed before Qori, taken by the rot; the rival | glimpsed in R2, fought in R5, freed in R7 | Qori's silhouette, withered to grey-brown, with thorns through its leaves and a dim mint core. Same rig as Qori |
| **The Thornrot** | the antagonist, a spreading parasite with a hive-will | everywhere; its heart at the core | never speaks; felt through its creatures and the thorns that grow back |

**The guardians (bosses):** the titan's defenders, turned by the rot.

| Guardian | Region | Concept | Fight |
|---|---|---|---|
| Knucklebramble | Palm | a knot of bramble fused into a giant Crawler, rooted in a knuckle | teaches reading warnings and attack windows; three thorn-arm swipes, then its knot is exposed |
| Cistern Matriarch | Arm | a huge Ripple Newt in the elbow cistern | dives and resurfaces; the player raises and lowers the water with sluices to strand it |
| Grove Warden | Grove | a stag-boar of roots (art delivered) | charge, stomp shockwave, root spikes; hit its heart knot |
| Hollowhorn | Knees | a ram of terrace stone and clay, with mill-wheel horns | charges across tilting fields; bounce off its back with Bloomfall |
| Brow Sentinel | Crown | a giant stone Bark Sentinel on the brow shrine | a shield only the spear pierces; lure its slam into the falls |
| Glassmoth Queen | Eye | a crystal Gust Moth over the lens lake | gusts and light beams; glide to reach her; aim mirrors to blind her |
| The Wilted | Heart | the earlier seed, in thorn armour | mirrors Qori's own moves; winning frees it |
| The Thornheart | Heart | the rot's core, wound around the titan's heart | three phases on pulse bridges; the finale |

**Creatures, rot or natural:**
- **Rot spawn** (hostile): Bramble Crawler, Thornwing, Pod Spitter, and the Thornheart's brood.
- **The titan's defenders** (hostile until the region's knot wakes, then **calm**): Bark Sentinel, Shellback.
- **Natural fauna** (territorial but not evil): Ripple Newt, Burrow Grub, Gust Moth, Glow Leech, Seed Carrier.

The defenders calming after a knot is a stir change too: that region becomes safer to revisit.

---

## 5. Level plan

| Region | Level 1 | Level 2 | Level 3 | Knot Chamber |
|---|---|---|---|---|
| R1 Palm Hollow | **Seedbed** (tutorial: move, jump, sword, checkpoint) | **Lifeline Crease** (the palm's crease is a ravine; plates and rubble; weapons) | **Thumb Rise** (climbing; a first glimpse of the titan's face) | Grip Knot: clench platforms; Knucklebramble |
| R2 Aqueduct Arm | **Wrist Channels** (water, Newts, first rings) | **Vein Galleries** (the existing A1 room, lengthened) | **Elbow Cistern** (sluices and water level) | Reach Knot: tendon pulls; Cistern Matriarch |
| R3 Breathing Grove | **Ribwood** (the existing A2 room, lengthened) | **Resin Lungs** (sap valves; breath currents after the stir) | **Heartwood Canopy** (high branches, Sentinels) | Breath Knot: Grove Warden |
| R4 Knee Terraces | **Terrace Steps** (heavy plates, Shellbacks) | **Kneecap Mill** (mill wheels) | **Shinfall** (a long vertical descent) | Spring Knot: Hollowhorn |
| R5 Falls Sanctuary | **Brow Stair** (wind vents, Gust Moths) | **Weeping Shrines** (the falls reroute; first fight with The Wilted) | **Crown Gardens** (updrafts, glide) | Bloom Knot: Brow Sentinel |
| R6 Tearglass Eye | **Lash Reeds** (a reed forest, darkness) | **Lens Lake** (mirrors and light beams) | **Iris Deep** (under the lake) | Sight Knot: Glassmoth Queen |
| R7 Rootdeep Heart | **Arterial Roots** (glow pods, Leeches) | **Pulse Chambers** (pulse bridges) | **The Last Valve** (everything at once) | Heart Chamber: The Wilted, then the Thornheart |

---

## 6. Art direction

- **The body must be readable at every scale.** Backgrounds hint at the body: the Palm's far layer shows the curve of giant fingers against the sky; the Arm's aqueducts run parallel toward a distant shoulder; the Crown looks down on the ribs' forest.
- **Stone for bone, wood for flesh.** Knuckles, the brow and the jaw are stone. The skin is bark and moss. Veins are glowing mint roots under translucent bark. This gives every region the same visual logic.
- **The mint glow means the titan is alive.** Knots, veins, relics, Qori and Waymarks glow mint. The rot is **desaturated purple-brown with ivory thorns**; it should never glow mint.
- **The Chart** is a hand-painted parchment of the titan, covered in painted moss and mist that peels away as levels are charted.
- **Stir vistas:** one 16:9 painting per knot showing that body part moving, which doubles as the cinematic background.
- **Key stills:** the intro (a seed falling into a moss-filled palm), the Wilted's reveal, and the ending (the titan sitting up).
- **The title background v2:** Qori standing on the titan's palm, with its sleeping face on the far horizon in morning mist. It replaces "Qori looking at a distant Colossus".

---

## 7. Music and sound

**The core idea: the soundtrack wakes up with the titan.** One main theme, the "Qolossal motif" (a slow four-note figure in D Dorian), runs through everything. Each knot woken **adds a layer** to it everywhere in the world, so by the end the full orchestra plays what started as a lone kalimba. Under everything is the titan's **heartbeat**, a soft low drum at 48 bpm, which speeds up as the game goes on (to 72 bpm in the Heart).

| Region | Lead instruments | Tempo and feel | Notes |
|---|---|---|---|
| Palm Hollow | kalimba, soft felt piano, warm strings | 76 bpm, gentle morning | the motif at its simplest |
| Aqueduct Arm | harp, water glass, hang drum | 84 bpm, flowing | arpeggios like running water |
| Breathing Grove | wooden flutes, marimba, cello swells | 72 bpm, breathing | swells on the titan's breath: in and out every 6 bars |
| Knee Terraces | fiddle, bodhrán, low whistle | 96 bpm, folk and rustic | a work song for the old farmers |
| Falls Sanctuary | wordless choir, celesta, open fifths | 66 bpm, airy and sacred | reverb like a stone chapel |
| Tearglass Eye | glass harmonica, reversed piano, bowed vibraphone | free time, dreamlike | shimmering; the motif played backwards |
| Rootdeep Heart | taiko heartbeat, deep drone, mint bell chimes | follows the pulse (60 to 72) | the heartbeat is the tempo |

**Other cues:**
- **Guardian fights:** the region's lead instrument played over driving percussion, with thorny dissonance (col legno strings, prepared piano). It resolves when the knot wakes.
- **The stir stinger:** a huge low earth-rumble and brass swell, with the new layer of the motif entering.
- **The Chart:** a music-box version of the motif, playing as many layers as knots woken so far.
- **The Wilted:** the motif in a minor key on a detuned music box.
- **Ending:** the full motif, the orchestra and choir.

**Sound signatures:** the titan's distant breathing (a low wind swell every 12 seconds, once breath returns), wood groaning during stirs, the Vein Gate hum (a mint shimmer), Echo's voice (a soft pad of formants, no real words), and the rot (dry rustling and snapping twigs).

---

## 8. From what's built to this design

| Built now | Becomes |
|---|---|
| A0 test room | stays as the mechanics gallery. R1's three real levels are built with the A0 kit |
| A1 Aqueduct room | R2 level 2, **Vein Galleries**, lengthened |
| A2 Grove room (step 10a) | R3 level 1, **Ribwood**, lengthened; the Warden arena moves to the Breath Knot chamber |
| Portals and area travel | Vein Gates: add the unknown-until-used record, Wild Veins, and veins that appear after a stir |
| Relic shrines | the knot's reward moment; relics reassigned per section 3.4 |
| `GameSave` | add knots woken, levels charted, veins travelled, Sproutlings and Lore Stones |
| Area room builders | add **stir variants**: parts of a level built only before or after a given knot |
| Checkpoints | Waymarks (a checkpoint that also charts the level) |

**New code, in order of need:**
1. World state and stir variants in the builders.
2. The Chart map screen.
3. Vein records and Wild Veins.
4. The Knot Chamber flow and the stir cinematic.
5. Wind Leaf dash.
6. Glidecap glide.
7. Seer's Lantern.
8. Dialogue boxes for NPCs.
9. The new puzzle mechanics (section 3.6).

---

## 9. Production plan

| Phase | Content | Proves |
|---|---|---|
| **1. The big picture** | Codex paints the Qolossal master concept and the map; lock the region layout | the world reads as a body |
| **2. Vertical slice** | R1 (3 levels, Knot Chamber, Knucklebramble), the Chart, Vein records, the first stir changing R1 | the whole loop: explore, wake, stir, return |
| **3. Act I** | R2 in full (reworking A1), the Matriarch, the second stir | stirs across regions |
| **4. Act II** | R3 (reworking A2), the Warden; R4 with the new A5 kit; Wind Leaf | non-linear order, the breath currents |
| **5. Act III** | R5, R6 (new A6 kit), R7, the final bosses, Glidecap and Seer's Lantern | the ending |
| **6. Polish** | music layers, cinematics, secrets pass, balance | |

---

## 10. Decisions (27 Sep 2026)

**Decided:**
1. **Act II order:** the Grove and the Knees can be played in either order.
2. **Wild Veins:** random every time they are used, until their region's knot wakes and settles them into a fixed shortcut.
3. **The Wilted:** healed at the end; it blossoms beside Qori in the final scene.
4. **Death:** no cost. Qori respawns at the last checkpoint with full hearts and keeps everything collected.
5. **Post-game:** the game ends with the ending. The "Risen world" (the titan standing, every region revisited from new angles, the right arm freed) is saved for a **sequel**.
