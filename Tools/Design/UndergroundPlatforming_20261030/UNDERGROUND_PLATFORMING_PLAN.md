# Underground platforming layouts: plan (draft, 30 Oct 2026)

> **Route names changed 9 Nov 2026 (user):** routes are now A, B, C, D. Old C (Garden) = **B**, old D (Wind) = **C**, old E (Descent) = **D**. Room labels in this file use the new letters (B01 = old C01). **Internal IDs in code, scene, JSON data and art folders still use the old C/D/E letters** until a dedicated refactor; translate when reading `rooms.json`, `terrain_build_v2.json`, captures (`captures/C01.png` = B01) or `Assets/`.

Target: `Assets/Scenes/Game/Qolossal_Underground.unity` (89 rooms), pending the user's choice of base.
Greybox only. Final art comes later and must fit these collision shapes, not the other way round.

## 1. Metrics (units, 1 u = 216 px)

Measured on 17 Oct (phase 1) unless marked *est.* Re-measure on the current movement before building; any change rescales the table below.

| Move | Value | Design use |
|---|---|---|
| Body | 0.8 x 1.2 | Headroom minimum 2.4 under any ceiling you're expected to jump under |
| Single jump rise (held / tapped) | 2.39 / 0.87 | "Comfort ledge" 2.0, never 2.3+ without a grab |
| Double jump total rise | ~4.3 *est.* | "Double ledge" 3.75; 4.0 is a skill test |
| Flat gap, walking / running | 4.08 / 6.13 | Comfort gap 3.0 walking, 5.0 running |
| Running + double jump gap | ~9 *est.* | 7.5 is the hard ceiling for required gaps |
| Ledge grab on clingable wall | 3.75 | Moss walls: one grab per 3.5 u climbed if no wall jump |
| Glide | 4.46 u forward per 1 u dropped | Glide spans sized at 3.5 u/u for margin |
| Living Thread range | 4.0 | Anchors at 3.4-3.8 from the cast spot |
| Air dash (Wind Leaf) | 19 u/s for 0.16 s = ~3 u | Dash gaps 2.5 added to a running jump |

Rules: every required jump has 15 % margin; no blind drops (camera sees the landing or a lip marks it); no fall damage, so a miss costs time (a climb back) not health, except over hazards, which respawn at the room's last safe point.

## 2. Platform kit (greybox)

- **Ledge** 3 x 0.5 standard, 1.5 short (skill), 6+ rest ledge. Pale lip on every standable top.
- **One-way deck** (jump up through; no drop-through exists, so every deck run ends in a walk-off gap).
- **Moss wall** (clingable, green tint); **plain rock** (not clingable, grey).
- **Thread anchor** (ring), **pull weight**, **bounce pod** (Bloomfall), **gust lane** (blue, timed), **shelter niche**.
- **Hazards:** water (respawn), thorn strip (damage + knockback), crumble ledge (1 s then falls, resets on respawn), Shellback rubble (charged sweep breaks; hides optional routes only).
- **Rest beats:** every room has at least one flat 6 u "breathing" floor near its arrival.

## 3. Route grammar

| Route | Theme | Gate skill(s) | Platforming identity | Signature hazard |
|---|---|---|---|---|
| A Cistern (Qvale) | Water works | none (intro route) | Single and double jumps, timing over water, wells climbed by zigzag decks | Water level and sluice flow |
| B Garden (Ribwood) | Living roots | Living Thread, Bloomfall | Swing chains, bounce pods to reach high decks, root ladders | Thorns, crumble roots |
| C Wind (Heights) | Gusts | Climbing Moss, Wind Leaf, Glidecap | Glide spans, timed gusts, dash through calms, moss chimneys | Gust push-back, long drops |
| D Descent | Quiet memory | all | Mixed mastery: every skill once, gently, downward flow | none lethal; tension from height |
| MRxx caves | Region pockets | that region's skill | One lesson room (teach, test, twist) per region, the rest exploration | per region |
| PC pockets | Secrets | varies | One short trick (8-14 u wide), reward on completion | none |

Difficulty curve: A is easiest (taught with jump and double jump only). B and C are mid-game and use two or three skills. D is the finale and combines them. In every route, rooms 01-03 teach, 04-06 test, 07 rests, 08 is the guardian arena, and 09-10 are optional harder challenges.

## 4. Room beats, connected scene (room-local, origin at room bottom-left)

Doors come from `terrain_build_v2.json` connections; heights are left at the existing door positions.

### Route A: Cistern
| Room | Size | Beat |
|---|---|---|
| A01 Sluice Arrival | 36x22 | Rest floor at the left door, three 3 u ledges rising 2.0 each to the right door at y 10; one gap over a shallow water channel (teach: water respawns). |
| A02 Drain Well | 16x60 | Vertical climb down then up: zigzag decks every 3.5 u (needs double jump), two rest ledges, a side crack to the A06 shortcut at the bottom. |
| A03 Silt Walk | 48x12 | Low corridor: floor gaps of 3, 4 and 5 u over silt water, a ceiling at 4.5 u so jumps stay flat (teach: running jump). |
| A04 Cistern Court | 64x36 | Hub: a water floor with a valve (lowers the water to open the A09 hatch), island ledges, upper ring deck back to the A05 door at y 24. Replaces the current stair zigzag. |
| A05 Spillway Rise | 16x48 | Climb with a timed spill: water sheet pulses every 2.5 s down the left half; climb the right decks in the gaps. |
| A06 Pump Gallery | 60x32 | Moving pump pistons (vertical platforms, 3 s cycle) across a flooded gallery; shortcut back to A02 opens from this side. |
| A07 Keeper Refuge | 42x14 | Rest: checkpoint, flat floor, one optional high shelf (double jump) with lore. |
| A08 Sluice Guardian Arena | 52x28 | Flat 40 u fight floor, two 6 u side ledges at 3.5 u, door to the Heart sealed until the guardian. |
| A09 Settling Cache | 28x20 | Optional: crumble ledges over water to a reward. |
| A10 Keeper Archive | 30x20 | Optional: a 4 u double-jump ladder with a 1.5 u ledge finale. |

### Route B: Garden
| Room | Size | Beat |
|---|---|---|
| B01 Garden Arrival | 36x24 | Rest floor, a single anchor over a thorn bed (teach the swing safely: catch floor under). |
| B02 Canopy Walk | 42x12 | Two anchors in a row, then a bounce pod into the exit. |
| B03 Root Ladder | 16x58 | Root ladder up: alternating moss strips and bounce pods; B10 side door at mid-height. |
| B04 Seed Cradle | 64x36 | Hub puzzle (from phase 2): pull weight drops the cradle into its detent, bounce pod opens the grate; B09 door behind thorns. |
| B05 Rootfall Well | 16x58 | Descent with crumble roots: drop, land, move on before they fall. |
| B06 Bloom Court | 54x30 | Test: three-anchor swing chain over thorns, then pod-to-thread combo (bounce, cast at the apex). |
| B07 Gardener Rest | 28x16 | Rest and checkpoint. |
| B08 Root Guardian Arena | 40x28 | Fight floor plus two anchors for dodging. |
| B09 Hidden Nursery | 44x26 | Optional: thread swing with no catch floor. |
| B10 Hanging Garden | 32x22 | Optional: pods on crumble roots. |

### Route C: Wind
| Room | Size | Beat |
|---|---|---|
| C01 Windward Shelter | 40x24 | Rest, one gust lane with a shelter niche (teach: wait in the calm). |
| C02 Leeward Well | 16x58 | Moss chimney climb, a gust pulse every 3 s pushes Qori to the opposite wall. |
| C03 Vane Court | 56x30 | Timed gusts across 3 islands; dash through on the calm; C09 off the top right. |
| C04 Wind Bridge | 44x12 | Long glide span (20 u from 6 u drop) with a crosswind. |
| C05 Updraft Hall | 72x40 | Hub: updraft columns lift the glide; reach three high decks; C10 door up. |
| C06 Baffled Drop | 16x66 | Glide down between baffles (zigzag glide), no floor hazards. |
| C07 Still-Air Rest | 32x18 | Rest. |
| C08 Wind Guardian Arena | 40x28 | Fight floor with gust lanes the boss toggles. |
| C09 Glider Roost | 36x22 | Optional: glide + dash chain with no catch. |
| C10 Cloud Archive | 32x22 | Optional: updraft timing to a high shelf. |

### Route D: Descent
| Room | Size | Beat |
|---|---|---|
| D01 Quiet Arrival | 36x24 | Rest; quiet. |
| D02 Memory Well | 16x64 | Downward: moss wall slide, drop to ledges, thread catch midway. |
| D03 First Memory | 56x30 | Mixed: bounce pod, swing, glide out over a wide floor. |
| D04 Processional Walk | 44x12 | Flat, slow, no jumps (pacing beat). |
| D05 Wilted Sanctuary | 72x40 | Finale test: every skill once in a loop, generous catches. |
| D06 Return Stair | 16x64 | Climb back up with moss + double jump. |
| D07 Last Refuge | 30x18 | Rest. |
| D08 Final Threshold | 32x28 | Short walk into the Heart. |
| D09 / D10 | 32x22 | Optional mastery rooms. |

### Heart (84x48)
Four sector arrival shelves (A low-left, B/C top, D right) joined by a central climb that only opens after all routes are done. Sectors stay sealed from each other until their lifts are latched, as in phase 1/2.

### Region caves (MR01-MR07, 6 each) and pockets (PC01-PC12)
Each region keeps one skill-lesson cave (Moss Pocket, Echo Vault, Grain Store, Seed Choir, Sheltered Nest) using the phase 2 lesson layouts, scaled to the room. The other caves get one beat each: a short loop using the skills owned by that point, plus one optional reward ledge. Each PC pocket gets a single trick (a 1.5 u ledge, a hidden crack or a glide drop) to its reward.

## 5. Build approach

- An editor builder (`Qolossal > Underground > Build Platforming`) reads `platforming_v1.json` (room-local shapes) and writes only into each room's `Platforms` and `Gameplay` groups. It's idempotent: re-running it replaces its own objects, tagged `UGP_`, and leaves terrain, art, caves and backing untouched.
- Every required jump is checked against the metrics in section 1 by a validator that runs before the build.
- Play-test harness: a flood fill for reachability per skill set, plus driven runs through A, B, C and D, reusing the phase 2 test driver ideas.
