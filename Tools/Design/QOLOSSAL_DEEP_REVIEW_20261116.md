# Qolossal: deep review of mechanics, visuals and feel (16 Nov 2026)

This replaces the shorter `QOLOSSAL_FULL_REVIEW_20261116.md` as the working document. The prompts for every action are in `QOLOSSAL_DEEP_REVIEW_PROMPTS_20261116.md`, next to this file.

## 0. What this is based on

| Source | What I did |
|---|---|
| Code, read line by line | `PlayerMovement` (993 lines), `PlayerCombat`, `PlayerCombatInput`, `QoriArmoryFactory`, `WeaponTuning`, `FeedbackTuning`, `HitStop`, `PlayerThread`, `PlayerHealth`, `CameraFollow`, `QoriWalkFramesOnPlayer`, `EnemyBase`, `UndergroundEnemy`, `UndergroundGuardian`, all 11 creature and 6 boss scripts, `ThornHazard`, `Hazard`, `MraTimedHazard`, `MraUgShelter`, `AttackZoneTelegraph`, `GameHud`, `TownShop`, `ControlsGuide` |
| Data, measured by script | The three tuning assets, Qori's 29 clips in `LudoQoriFrames.prefab`, all 89 rooms and 888 pieces in `platforming_v1.json`, the 77 surface route edges and 42 chambers in `mra_world.json`, the scene files |
| Simulation | Qori's jump, replayed step by step from the real gravity and tuning numbers at 60 Hz |
| Play in the editor (current code) | A Cradle cave, A04, the Sluice Warden in A08, the Terraces, Qvale, driven by timed key presses, read as frame strips |
| Play in the Windows build (`Builds/Polish70`) | Response time of jump, attack, dash, evade and move, measured by sampling the screen every 7 ms after a key press; a full-rate run strip |
| Image measurement | Brightness, saturation, hue and detail of Qori against what is behind him, in four captures |

**Limits.** I can't feel a pad in my hands or hear the game, and sound is off anyway. Where a finding rests on judgement and not a number, it says so. The build dates from 3 Nov, so I used it only for response time; everything else is from today's code. The build put up a Windows Firewall prompt (development builds open a profiler port); I pressed Cancel, so nothing was allowed.

All play used a copy of your save (`claude_feel_play.json`). Your save, the scenes and the project settings are untouched.

---

## 1. Diagnosis: the nine root problems

Everything in the later sections hangs off these. They are in the order I would fix them.

| # | Root problem | The evidence in one line |
|---|---|---|
| R1 | **The main route asks almost nothing of the player.** | 70 of the 77 surface route edges are plain walking on slopes and steps. Gaps are authored at 3 u; Qori clears 6.1 u with one jump and 11.75 u with two. No thorns, no moving platforms, 12 enemies in 2,695 u. |
| R2 | **Hearts have no stakes.** | Water teleports Qori to safety *with full hearts*. Death does the same. Thorns cost one heart and bounce him back onto the thorns. Three rules for one idea, and two of them heal. |
| R3 | **Combat punishes the natural input.** | After half a second of running, the attack button gives a 0.6 s lunge that carries Qori into the enemy. Hitting a boss outside its window throws Qori back and locks his attack. The boss's whole body hurts during a strike, but the warning shows only a strip of floor. |
| R4 | **Too many verbs, several dead or doubled.** | 17 inputs. Grow-platform and flower-boost have no content anywhere. Evade and dash do the same job (3.4 u against 3.0 u). Sprint is a held stick-click that no room needs. The 3-hit combo has its frames and is switched off. |
| R5 | **Qori can't be seen on the surface.** | In Qvale his brightness is 46.9% against 48.6% behind him, same hue, same saturation, same amount of detail. Underground it is 28.3% against 21.6%, with twice the detail, and he reads fine. |
| R6 | **Boss fights are long and repetitive.** | 20 hits at 2 per opening is 10 identical loops, 46 s at best with perfect play. Phase 1 is one attack repeated four times. |
| R7 | **The underground is tuned to one jump.** | All 111 ladder steps rise about 3.5 u, which is 80% of a perfect double jump, every time. A05 has 17 in a row, A10 has 13. 58 of 89 rooms have no creature; thorns are the main opponent. |
| R8 | **The picture is unfinished at the seams, not in the paintings.** | Paintings are strong. What sits in front of them is not: flat terrain fill, a coarse cave rock tile over 60% of the screen, thin floating slabs, debug-looking UI (boss bar, route bar, Chart), blurry guardians at 8 frames a second. |
| R9 | **Nothing buys anything.** | About 1,130 Amber underground alone; the shop has one real item. No heal the player controls. |

What is already good, so it isn't broken by the fixes: response time (jump 28 ms, attack 16 ms, dash 37 ms, all within two frames); coyote time and jump buffer (0.12 s each); the variable jump (0.98 u tap to 2.37 u hold); the camera's hold-the-takeoff-view logic; the staff swing's arc and leaf burst; the tuning assets; the checks and bots.

---

## 2. Mechanics

### 2.1 Movement: measured

| Thing | Value | Verdict |
|---|---|---|
| Run (default pace) | 7.5 u/s, reached in 0.15 s | Good |
| "Walk" | 5 u/s, only seen for the first 0.1 s of a run | Clip is dead weight (F3) |
| Sprint (hold Shift / L3) | 12 u/s | Not needed by any room data; awkward input (M3) |
| Jump, held | 2.37 u high, 0.82 s, 6.13 u far at a run | Good |
| Jump, tapped | 0.98 u high | Good range |
| Double jump | 4.35 u high at best, or 11.75 u far | Strong; dwarfs the surface's 3 u gaps (R1) |
| Dash | 3.04 u in 0.16 s, 0.3 s rest, no protection | Overlaps evade (M4) |
| Evade | 3.38 u in 0.26 s, cannot be hurt, ground only | Overlaps dash (M4) |
| Glide | falls at 1.8 u/s, 7.5 u/s across | Opens by itself (M1) |
| Response: jump / attack / dash / evade | 28 / 16 / 37 / 41 ms | Good. No latency problem |

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| M1 | **The Glidecap opens on any held jump.** Once the player owns it, every full-height jump turns into a slow float on the way down, because holding Jump for height also arms the glide. | `glideNeedsNewPress: 0` in `Player.prefab:395`; `PlayerMovement.cs:747` | Glide needs its own press in the air, then hold. The code path exists; switch it on and test the buffer. |
| M2 | **Air steering tops out at walking speed.** In the air Qori accelerates toward 5 u/s, not 7.5. A standing jump covers 4.1 u, a running jump 6.1 u, and turning round in the air costs a third of his speed for the rest of the jump. | `PlayerMovement.cs:639-643` uses `moveSpeed` | Steer toward the run speed in the air. |
| M3 | **Sprint is a held stick-click.** L3 held while steering is tiring and imprecise, and no layout data or validator mentions sprint, so nothing is built for it. | `PlayerMovement.cs:313-315`; no "sprint" in `layouts.py` or `validate.py` | Remove the button. Give its speed to the Wind Leaf: a dash that is held into a run keeps 12 u/s until Qori turns, stops or is hit. One input fewer, and the relic gains a second use. |
| M4 | **Evade and dash are one verb on two buttons.** Evade goes further and protects; dash works in the air. | `MovementTuning` values above | One button. Before the Wind Leaf: today's ground evade. After: the dash, keeping protection for its first 0.1 s on the ground. |
| M5 | **No way down through a deck.** 348 one-way decks and crumbles, and the only way to descend is to walk off an edge. | No drop-through anywhere in `Assets/Scripts` | Down + Jump drops through a one-way deck. |
| M6 | **No corner forgiveness.** Clipping the corner of a solid block with Qori's head stops the jump dead. | No nudge in the jump code | If the head hits a corner with less than 0.25 u overlap, slide round it. |
| M7 | **One wall is a free ladder.** A wall jump with no direction held goes straight up at full jump speed, and the wall can be re-gripped at once, so any clingable wall of any height is climbed with one button. This bypasses the ladder rooms wherever a wall is near. | `PlayerMovement.cs:663-673` | Your decision. I'd keep it (it is the relic's fantasy) but make the straight-up kick 70% of a jump and let Qori slide after three kicks on the same wall without touching ground. |
| M8 | **Indoors Qori would skate badly.** `ForceWalk` is in the code for house interiors but nothing sets it, so interiors run at 7.5 u/s in a 30 u room. | No caller of `ForceWalk =` in the game | Interiors: 3 u/s, played with the run clip slowed, which covers about 90% of the ground. |

### 2.2 Combat: frame data

Leaf Staff, from `WeaponTuning` defaults (the asset overrides nothing). Times in seconds.

| Move | Wind-up | Hits | Recovery | Total | Damage | Notes |
|---|---|---|---|---|---|---|
| Swing (press) | 0.06 | 0.10 | 0.18 | 0.37 | 1 | 2.7 hits a second. The only ground attack in use |
| Up swing (Up + press) | 0.04 | 0.10 | 0.12 | 0.29 | 1 | |
| Combo hits 2 and 3 | 0.05 / 0.08 | 0.10 / 0.16 | 0.16 / 0.24 | | 1 | **Switched off** (`staffSingleAttack = true`), frames exist |
| Lunge (press after 0.5 s of running) | 0.10 | 0.24 | 0.26 | 0.60 | 1 | Pushes forward 4 u/s and keeps 130% of run speed |
| Charged sweep (hold 0.95 s) | 0.08 | 0.20 | 0.36 | 1.59 with the hold | 2 | Breaks shells. 1.3 damage a second against the swing's 2.7 |
| Plunge (Down + press in air) | 0.10 | until landing | 0.15 | | 1 + 1 landing | Dives at 24 u/s, bounces 10 u/s |
| Staff shot (hold Q / X, aim, release) | 0.28 | | 0.24 | 0.72 between shots | 0.35 | 0.5 damage a second |

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| C1 | **The lunge fires when the player wants a swing.** Running is the default pace, so "ran for 0.5 s" is true almost every time Qori approaches an enemy. The player presses attack, gets a 0.6 s commitment and is carried into the enemy's body, which hurts on contact. | `PlayerCombatInput.cs:38-39` | The lunge becomes a chosen move: attack within 0.25 s after a dash. A plain press while running is always the swing. |
| C2 | **Qori has one ground attack.** The three-hit combo (step forward, backswing, all-round spin finisher with its own freeze and shake) is built, tuned and drawn, and turned off since 20 Oct. | `WeaponTuning.staffSingleAttack` | Your decision, since you chose the single swing. My recommendation: turn the combo back on. It costs nothing and it is the difference between mashing and having a rhythm. |
| C3 | **Hitting armour throws Qori back.** A blocked hit knocks him 3.5 u/s backward, takes control for 0.2 s and locks the attack for 0.25 s. On a guardian, *every* hit outside the 2.2 s opening counts as blocked, so a player who tries to fight is bounced around the arena. | `PlayerCombat.cs:219-223`; `UndergroundGuardian.ReceiveCombatHit` | Two responses. **Deflect** (guardian body, shut husk): a pale spark and a clink, no knockback, no lock. **Block** (a raised crab claw, a shell): today's recoil, because there a rule is being taught. |
| C4 | **The warning lies.** During a strike the guardian's whole body hurts (9 x 5 u for the Warden). The danger zone shows only the claw's strip of floor. Standing beside the body where no claw goes still costs a heart. | `UndergroundGuardian.HurtsOnContact`; `SluiceWardenBoss.StrikeZone` | The body hurts only where a limb moves through. Each attack lists its hurt shapes per frame range, and the same list draws the warning. |
| C5 | **The danger zone renders as a solid orange box** although its code asks for 4-16% alpha. | `AttackZoneTelegraph.Warn`; captures `zone_G-03_strike.png`, `a08_fight_strip.jpg` | Find the cause (I suspect the material), then restyle as a floor strip. |
| C6 | **The charge has no "ready" cue and isn't worth using.** Nothing shows or sounds when 0.6 s has passed, and letting go early does nothing at all. Its damage rate is half the swing's. | No user of `ChargeReady` outside input; table above | A flash and a held pose when ready; 3 damage; releasing early gives a normal swing. |
| C7 | **The staff shot is a third of a swing every 0.72 s.** It has a job only if it reaches what the staff can't. | `slingDamage .35` | 1 damage, and it is the tool for flyers, perched creatures, far switches and high weak points. |
| C8 | **Up-aim shares its key with three other things.** W is aim up, thread pull, ledge climb and interact. Attack is on the mouse only on keyboard, and on LB on a pad. | `PlayerCombatInput.cs:14`, `PlayerThread.cs:144` | Solved by the control layout (section 2.8). |

### 2.3 Enemies and guardians

Regular creatures: 2-4 hits (0.7-1.5 s to kill), warnings of 0.5-0.65 s, a long recovery (the Crab's is 1.2 s) in which they are safe to touch. These are sound. The Crab's rule (jump it, hit its back; or wait out the snap) is the best piece of enemy design in the game.

Guardian loop, from `UndergroundGuardian`:

| Step | Seconds |
|---|---|
| Pause | 0.9 |
| Warn | 0.8 (Warden 1.1) |
| Strike | 0.4 |
| Recover | 0.5 |
| Open | 0.35 |
| Exposed, until 2 hits land | 2.2 at most; about 0.8 with good play |
| Close | 0.35 |
| **One loop** | **about 4.1-5.5** |

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| G1 | **Ten loops to win.** 20 hits at 2 per opening. Flawless play is 46 s including roars; real play will be 90 s or more of the same two attacks. Mini-bosses: six loops. | `Hits 20`, `hitsPerWindow 2` | 15 hits at 3 per opening is five loops. Shorter fights need a third attack to stay interesting (G2). |
| G2 | **Phase 1 is one attack.** The Warden sweeps four times before anything changes; the Echo Warden has one attack for its whole fight. | `NextAttack` in each boss | Each guardian gets a third move from frames it already has, and phase 1 mixes two. |
| G3 | **The Warden's weak point is 3.1 to 6.1 u up.** A jump plus an up-swing reaches it, and nothing tells the player that. A plain swing from the floor can never hit it. | `WeakOffset (-1, 4.6)`, radius 1.5 | Keep the height, but pulse the valve and let the staff shot hit it (C7), so there are two answers. |
| G4 | **No entrance, no frame.** The bar shows before the boss is seen. The camera stays on Qori, so the Warden sat half off screen for most of my fight. | Play session; no lock zone in the arena | An arena camera, a 1.5 s intro, the bar on the roar. |
| G5 | **Leaving the room resets the fight to full.** With doors sealed this is mostly moot, but a death also resets it, and death costs nothing (H1), so the fight is a war of patience, not of hearts. | `ResetFight` | Fine once H1 gives death a cost in time: respawn at the rest point outside, not at the arena's safe pocket. |

### 2.4 Health, damage and death

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| H1 | **Three rules for getting hurt, two of them heal.** | | One rule, below |
| | Water: teleports to the last safe pocket, **full hearts back** | `Hazard.cs` calls `Respawn()`, which calls `RestoreAfterRespawn()` | |
| | Thorns and spills: one heart, bounced 9 u/s up and 3.5 u/s away from the *centre* of the strip, so on a long strip he lands on thorns again | `ThornHazard.cs`; I lost 5 of 7 hearts in 7 s in the first Cradle cave room | |
| | Death: 0.8 s beat, back at the last safe pocket (not the rest point), full hearts, nothing lost | `PlayerHealth.DeathBeat`, `MraUgShelter.SetRespawn` | |
| H2 | **So the best move at low health is to jump in water or die.** Hearts are decoration in every platforming room. | Follows from H1 | **One rule:** any hazard costs one heart and puts Qori back on the last safe ground, hearts kept. At zero hearts he returns to the last *rest point* with full hearts and the room's creatures back. |
| H3 | **No heal the player controls** (40% orb drop only). | `SapOrb.MaybeDrop(.4f)` | The Sap vial, with the economy (section 2.6). |
| H4 | **The hurt blink drops Qori to 30% alpha**, nearly invisible on cave walls. | `QoriWalkFramesOnPlayer.cs:223` | White flash, then 100% / 60%. |
| H5 | **Lost hearts vanish from the HUD** instead of showing as empty leaves. | Play session, 2 of 7 hearts | Fix the empty-leaf sprite. |
| H6 | **41 of 89 rooms have no safe pocket**, and in the first Cradle cave the first thorns are 5 u from the door. | Layout data | With H2, safe ground is tracked automatically, so pockets matter less. Keep 8 u clear inside every door. |

### 2.5 Level design: measured

**Surface (the main route).**

| Measure | Value |
|---|---|
| Route edges | 77, of which 70 are "walk" (38 stone steps, 32 slopes) |
| Edges that need a relic | 2 (one thread span, one glide span) |
| Authored jump gap / rise | 3 u / 1.6 u (`metrics` in `mra_world.json`) |
| Qori's reach | 6.1 u / 2.4 u with one jump; 11.75 u / 4.35 u with two |
| Hazards, moving platforms, lock zones | none found in the seven scenes |
| Enemies | 12 (two per region, none in Qvale) |
| Shape of every region | 12 nodes, 11 edges, 6 chambers, 2 encounters |
| Total width | 2,695 u, six minutes of running |

| ID | Finding | Fix |
|---|---|---|
| L1 | **The surface is a walk with a view.** The climb is the game's pitch and its first hour, and it has the least play in it. A relic is found on the surface and then has nothing to do there. The paintings carry it; the hands are idle. | A surface gameplay pass, by data: each region gets 6-8 authored beats built from its own relic and the one before, using pieces that already exist underground (decks, crumbles, movers, anchors, pods, gust lanes, thorns). Teach, Test, Twist from the polish plan becomes real rooms on the main path. |
| L2 | **Every region has the same skeleton.** Twelve stops, six caves, two fights. Players feel a template by the third region. | Vary it in the same pass: one region with a long uninterrupted run, one that folds back on itself, one with its knot off the main path. |
| L3 | **First hour = weakest hour.** Cradle, Causeway and Terraces hold 6 enemies and no hazards. This is also the natural demo. | The demo slice is Cradle, Causeway and Route A, after L1 has been applied to those two regions. |

**Underground.**

| Measure | Value |
|---|---|
| Rooms / pieces | 89 / 888 |
| Decks and crumbles | 348, median width 3.0 u, 36 of them 2 u or less |
| Ladder steps (a deck 2.6-4.3 u above the last) | 111, median rise 3.5 u, **all above a single jump** |
| Longest ladders | A05: 17 steps. A10: 13. A09: 9 |
| Thorn strips | 76, in 46 rooms. D05 has 50 u of thorn floor against 49 u of standing room |
| Creatures | 30 placed, 3 mini-bosses, 3 guardians. 58 rooms have none |
| Rewards | 51, all Amber (20 or 40) |

| ID | Finding | Fix |
|---|---|---|
| L4 | **One jump, 111 times.** A 3.5 u rise is a double jump with 0.85 u to spare, so every step is the same press-press with the same timing. A05 asks for it 17 times in a row. | No more than 4 identical steps in a row. Mix in 2.0-2.3 u single-jump steps, wall kicks, a mover, a dash gap. Rises of 3.5 u stay as the "hard" step, not the only step. |
| L5 | **Big dark rooms with little in them.** D05 is about 60 x 35 u with 11 decks. From its capture it is a black box with specks. | Either shrink the room's view (camera) or fill it: 1 deck per 60-80 u² is a good density; D05 is at 1 per 190. |
| L6 | **Thorns are the opponent.** 76 strips against 36 creatures and bosses. Combat is a small part of what the underground asks. | Not wrong for a platformer, but then creatures should guard *jumps*, not floors: perched and flying creatures on the ladder lines (the Leech and the Kite Ray already work this way). |
| L7 | **Every reward is Amber.** Finding the tenth pile of the same currency is not a reward. | Mix: vial upgrades, heart-seed shards (3 make a seed), map pages, lore, and Amber. Needs the economy first. |

### 2.6 Progression, verbs and economy

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| P1 | **Two verbs with no content.** "Grow temporary platform" (E / RB) and "flower boost" are on the controls page; no scene holds a growth platform or flower. | 0 `GrowthPlatform`, 0 `GrowthFlower` in all 13 scenes | Remove both from input and the controls page. RB is freed. |
| P2 | **17 inputs.** After P1, M3 and M4 it is 13, and each of the six relics has one clear button or none. | `ControlsGuide.cs` | Section 2.8 |
| P3 | **Amber has nothing to buy and there is no heal.** | First review, N1 and N2 | Economy table, real staff upgrades, Sap vial. |
| P4 | **Relics are keys before they are tools.** On the surface the Thread and Glidecap each open one gate and are otherwise unused until the player goes underground. | Edge data | Solved by L1. |

### 2.7 Camera

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| K1 | **Guardian fights are not framed.** | Play session; G4 | Arena frame holding boss, weak point and floor. |
| K2 | **Zoom varies widely between cave rooms.** Qori was 3% of the screen width in the Cradle cave and comfortable in A04. At 3% his animation can't be read and thorns are specks. | Play session | Cap play zoom at camera size 7; wider only at look-outs. |
| K3 | The follow itself is careful work: takeoff view held through a jump, platform framing, downhill preview, 1.9 u look-ahead. | `CameraFollow.cs` | Keep. |
| K4 | At camera size 5 the view is 17.8 u wide, so at 7.5 u/s Qori sees 1.4 s ahead, and 0.9 s at the old sprint speed. Enemy warnings are 0.5-0.65 s. It works at run speed and not at 12 u/s. | Arithmetic | Another reason to fold sprint into the dash (M3) and widen the lead while it lasts. |

### 2.8 Controls: proposed layout

Thirteen inputs, no dead ones, every relic placed.

| Action | Pad | Keyboard | Note |
|---|---|---|---|
| Move | Left stick / D-pad | A D | |
| Jump, wall kick, ledge up, glide (second press, hold) | A | Space | |
| Attack (Up: up swing; Down in air: plunge; hold: charge) | X | J, or left mouse | Off the shoulder button |
| Dash (evade before the Wind Leaf) | B | K, or Shift | One button |
| Staff shot (hold to aim) | Y | L, or Q | Right stick or mouse aims |
| Thread: cast / release | RT | E, or right mouse | |
| Thread: shorten / lengthen | LT / LB | W / S while attached only | No clash with aim |
| Heal (Sap vial, hold) | RB | F | New |
| Interact | D-pad up or stick up | W | |
| Look down / drop through a deck | Down (+ Jump to drop) | S (+ Space) | |
| Map size | R3 | Tab | |
| Chart | Select | M | |
| Pause | Start | Esc | |

---

## 3. Feel

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| F1 | **Response time is good.** Jump 28 ms, attack 16 ms, dash 37 ms, evade 41 ms from key to first changed pixel, in the windowed build at 60 Hz. | Measured, `probe` runs | Nothing. Stop worrying about latency. |
| F2 | **The run clip can't keep up, by arithmetic.** One cycle moves the planted foot 1.71 u (one body-height). At 7.5 u/s the feet would need 4.4 cycles a second; they play 2.8. Coverage is 63% at run and at sprint. | `run`: stride 420 px, figure 424.8 px, cycle 0.46 s, pace 1.28 | A run whose cycle covers **two** body-heights (3.45 u) matches 7.5 u/s at the cycle time already in use. That is a bounding run. Ludo L1. |
| F3 | **The walk clip covers 16% and is seen for a tenth of a second.** | `walk`: stride 192 px, cycle 0.8 s at 5 u/s | Drop it from play. Interiors use the slowed run (M8). |
| F4 | **Clip rates run from 6 to 32 frames a second**, and each clip is scaled by its own measured figure height (326 to 580 px), so a crouched pose can come out larger than a standing one. | Clip table from the prefab | One audit: overlay every clip's first frame at a common foot line and head height; fix scale in data. Loops at 12 or 15 only. |
| F5 | **World effects are switched off while the frames show.** Landing dust, dash gust and wall scrape are disabled because the Ludo frames "draw their own", so nothing is left behind in the world; any dust is glued to Qori. | `QoriWalkFramesOnPlayer.cs:232-233` | World-space puffs back on for landing, skid, dash start, wall slide and plunge. Codex paints the sprites (X5). |
| F6 | **Hurt is the first frame of the fall; death is the middle of the evade.** Stand-ins, as planned. | `PickSpecial` | Ludo L2. |
| F7 | **No turn.** Facing holds until speed passes zero, then flips. Acceptable, but with no skid the stop reads as a slide. | `turnHoldSpeed` | Ludo L3. |
| F8 | **Hit feedback numbers are sensible** (swing: 0.06 s own pause, 0.035 s freeze, 0.06 u shake; kill 0.08 s; hurt 0.12 s). With sound off they can't be judged fully. | `FeedbackTuning` | Re-judge when sound arrives. |
| F9 | **Guardians idle at 8 frames a second** from frames drawn at a quarter of the 4K density. The largest thing on screen is the least alive. | `UndergroundGuardian.LateUpdate`; polish plan Appendix B | 12 fps, and the upscale test (first review, N9). |

---

## 4. Visuals

### 4.1 Can the player see the character? (measured)

Mean brightness, saturation, hue and edge detail inside a box round Qori, against the area around him.

| Capture | Qori: brightness / saturation / hue / detail | Behind him | Separation |
|---|---|---|---|
| Qvale | 46.9% / 49% / 40° / 52.9 | 48.6% / 45.5% / 38° / 49.4 | **None** |
| Terraces | 48.4% / 46% / 41° / 48.1 | 48.5% / 44.8% / 41° / 44.0 | **None** |
| A04 (Cistern) | 28.3% / 40% / 164° / 31.8 | 21.6% / 38.5% / 172° / 14.3 | Brightness +6.7, detail x2.2 |
| Cradle cave | 28.3% / 34% / 52° / 28.5 | 21.6% / 29% / 51° / 16.1 | Brightness +6.7, detail x1.8 |

| ID | Finding | Fix |
|---|---|---|
| V1 | **On the surface Qori and the background are the same brightness, hue, saturation and busyness.** A cream-and-green character on cream-and-green terraces painted as sharply as he is. Enemies in the same palette will vanish the same way. | Target in the 4 u band behind the walk line: background at least 12 points darker or lighter than Qori, and detail at most 60% of his. Get there with a grade on the Mid and Far planes and a rim and ground shadow on characters. No art changes. The same two numbers become a check that runs on captures. |

### 4.2 Layer by layer

| ID | Layer | Finding | Fix |
|---|---|---|---|
| V2 | Surface terrain body | One tile repeated at even brightness over the largest area of the screen (Cradle, Causeway). Heights: identical crystal columns with the same carved triangle every tile. | Shader: darken with depth, slow value and hue drift, contact shadow under the lip. Then breakup patches from Codex only where it still fails (X6). |
| V3 | Region-cave rock | A coarse rock tile whose cracks are larger than Qori covers about 60% of the screen; the painted backdrop shows through square cut-outs with hard edges. | Halve the tile's world scale, tint it to the backdrop's hue, soft inner shadow on the cut-outs. |
| V4 | Route A terrain | Yellow-grey cobbled blocks against teal masonry. | Tint and value match per route in the same shader. |
| V5 | Route A play band | In A04 the frame is mostly flat brick with thin slabs floating in front. The arches and lanterns are out of frame. Slabs stand on nothing, against your own rule. | Brackets under every deck; two or three set pieces moved into the play band. Kit pieces only. |
| V6 | Routes without paintings | D05: a black box with visible vertical seams in a dark tile, grey woven border. | Until Route D has art, give it the cave-depth treatment with a value gradient (lighter low, darker high) and hide the seams with large rock silhouettes from existing kits. |
| V7 | Backgrounds | Qvale's Mid plane repeats its landmark (the tree and village) twice in one screen. | Check tile width against the 4K view; offset or mirror alternate tiles. |
| V8 | Guardians | 50-73 px/u against a 216 target, 8 fps idle. | F9 |
| V9 | Danger zone | Solid box (C5). | Floor strip. |

### 4.3 Interface

| ID | Finding | Fix |
|---|---|---|
| U1 | **The HUD ring, vine and relic row are finished art; everything else is not.** Boss bar: red rectangles, Arial. Route bar: grey box. Amber: plain number. | Codex X3, then wiring. |
| U2 | **The Chart reads as a developer tool.** Flat dark panel, outline map, buttons labelled "Whole map", "-", "+", a help line about clicking, dragging and the mouse wheel. It is the object the game's story is named after. | Codex X4 (parchment, frame, markers), and a pad-first control scheme: stick pans, triggers zoom, A travels, no on-screen buttons. |
| U3 | **27 scripts still draw with IMGUI**, including the controls page that covers the lower-left of the screen in play, and every "press Up" prompt. | One pooled prompt view; controls page moves into the pause menu. (Polish plan UI-02; unchanged, still open.) |
| U4 | **Nothing teaches the charge, the shot, the plunge or the up-swing in play.** | One-line prompts the first time each becomes useful, tied to a place: the plunge at the first pod, the shot at the first perched creature. |

---

## 5. Engineering

| ID | Finding | Evidence | Fix |
|---|---|---|---|
| E1 | **Per-frame garbage in the hot path.** Qori's frame player collects every child renderer each frame; every underground creature builds a new string array each frame to pick its clip; `All` builds a 32-entry array on each call. | `QoriWalkFramesOnPlayer.cs:230`, `:99`; `UndergroundEnemy.Seg` | Cache once. Matters on Switch 2 handheld. |
| E2 | **Qori is drawn by a class that calls itself "Test-only"**, creates an object named "Young Adventurer frames (test)", rewrites the camera's culling mask every frame, and keeps the hidden rig animating underneath. | Same file | Production name, own layer set once, rig retired after hit shapes cover every move (polish plan WS6; still open). |
| E3 | **The save flushes to disk on every change** (each pickup, flag, Amber). | `GameSave.Write`, `stream.Flush(true)` | Dirty flag; write at rest points, room changes, pause and quit. |
| E4 | **Development builds raise a firewall prompt** on first run, which is what an outside tester will meet first. | Seen today on `Polish70` | Playtest builds are non-development, with an in-game debug toggle if needed. |
| E5 | **163 scene searches (`Find...ByType`)** across gameplay scripts; many are cached, some run on every scene load or per enemy. | grep | A `PlayerRef` and service statics; fix the per-frame ones first. |
| E6 | **Three hazard classes and two enemy generations** (surface prefabs on `EnemyBase`, underground on `UndergroundEnemy`) with different contact rules. | Section 2.4; creature scripts | H2 unifies hazards. Enemies: move the surface six onto the underground base when they are next touched. |
| E7 | **Nothing since 3 Nov is committed; nothing is pushed.** | `git status`: 3,378 files | Your word needed. |

---

## 6. Action plan

Four phases. Each action has a prompt with the same ID in the prompts file. "Lane" is Unity (one thread at a time, holds the editor) or Desk (no Unity). Sizes: S under a day, M 1-3 days, L a week.

### Phase 0: today, no Unity

| ID | Action | Lane | Size | Needs from you |
|---|---|---|---|---|
| A00 | Commit and push the folder | Desk | S | "Commit and push" |
| | Decisions D1-D8 (section 7) | | | Your answers |

### Phase 1: fair and readable (about a week)

Small code changes that remove what currently feels unfair or can't be seen. No art needed.

| ID | Action | Fixes | Lane | Size |
|---|---|---|---|---|
| A01 | One rule for hazards and death; hurt blink; empty hearts | H1, H2, H4, H5, H6 | Unity | M |
| A02 | Movement fixes: glide press, air speed, drop-through, corner nudge, interiors | M1, M2, M5, M6, M8 | Unity | M |
| A03 | Verbs and controls: remove dead verbs, merge dash and evade, sprint into the dash, new layout | M3, M4, P1, P2, C8 | Unity | M |
| A04 | Attack rules: chosen lunge, combo on, charge cue, useful shot | C1, C2, C6, C7 | Unity | M |
| A05 | Honest guardians: deflect, hurt shapes, danger-zone bug and restyle | C3, C4, C5, V9 | Unity | M |
| A06 | Readability: background grade, character rim and shadow, the capture check | V1 | Unity | M |

### Phase 2: worth playing (two to three weeks)

| ID | Action | Fixes | Lane | Size |
|---|---|---|---|---|
| A07 | Guardian fights: 5 loops, third attack, arena camera, intro, bar | G1-G5, K1 | Unity | L |
| A08 | Economy, Sap vial, staff upgrades, reward mix, HUD ring | P3, H3, L7, R9 | Desk then Unity | L |
| A09 | Surface gameplay pass, region by region, Cradle and Causeway first | L1, L2, L3, P4 | Desk then Unity | L |
| A10 | Underground variety: ladders, density, door clearance, creatures on jumps | L4, L5, L6 | Desk then Unity | L |
| A11 | Camera zoom caps and room framing | K2, K4 | Unity | S |

### Phase 3: looks finished (runs alongside phase 2 on the Desk lane)

| ID | Action | Fixes | Tool | Size |
|---|---|---|---|---|
| X1 | Batch 51: Leapseed relic and icon | first review N12 | Codex | S |
| X3 | Batch 53: HUD frames (boss bar, route bar, Amber, empty leaf, vial) | U1 | Codex | S |
| X4 | Batch 54: Chart and menu skin | U2 | Codex | M |
| X5 | Batch 55: world effect sprites (dust, sparks, splash) | F5, C3 | Codex | S |
| X2 | Batch 52: logo and key art | first review N13 | Codex | M |
| X6 | Batch 56: terrain breakup patches, only if A12 is not enough | V2 | Codex | M |
| L1-L6 | Qori clips: bounding run first, then hurt and death, start and skid, look and crouch, rope swing, heal | F2, F6, F7 | Ludo | 54 credits |
| A12 | Terrain and cave shader pass | V2, V3, V4, V6 | Claude, Unity | M |
| A13 | Route A dressing with kit pieces; Qvale tile repeat | V5, V7 | Claude, Unity | M |
| A14 | World effects back on, using Batch 55 | F5 | Claude, Unity | S |
| A15 | Animation system: clip audit, rates, garbage, production name | F3, F4, E1, E2 | Claude, Unity | M |
| A16 | Install the Ludo clips; hit-shape rows; in-game check | F2, F6, F7 | Claude, Unity | M |
| A17 | Interface wiring: HUD, boss bar, Chart, prompts, teaching lines | U1-U4 | Claude, Unity | L |
| A18 | Guardian frames: upscale test on one, 12 fps idle | F9, V8 | Claude, Unity | S |

### Phase 4: prove it

| ID | Action | Lane | Size |
|---|---|---|---|
| A19 | Engineering hygiene: save writes, searches, non-development playtest build | Unity | M |
| A20 | Demo slice build (Cradle, Causeway, Route A), one play script for you, an outside-player checklist | Unity | M |

**Order inside the Unity lane:** A01, A02, A03, A04, A05, A06, then A11, A07, A08, A09, A10, with A12-A18 slotted in as art arrives. A01-A05 touch the same four files (`PlayerMovement`, `PlayerCombat`, `PlayerHealth`, `PlayerCombatInput`), so they must run one after another, never in parallel.

**What to drop or pause while this runs:** Route B, C and D painted backgrounds (first review N3). They are the most expensive thing in the project and the player's biggest problems are not there.

---

## 7. Decisions I need

| # | Decision | My recommendation |
|---|---|---|
| D1 | Commit and push now? | Yes |
| D2 | One rule for hazards and death (H2)? Death returns to the rest point and resets the room. | Yes |
| D3 | Turn the 3-hit combo back on (C2)? | Yes |
| D4 | Merge evade and dash, and fold sprint into the dash (M3, M4)? | Yes |
| D5 | The control layout in 2.8? | Yes |
| D6 | The wall kick (M7): leave free, or 70% height with three kicks per wall? | Three kicks |
| D7 | A surface gameplay pass that adds hazards, movers and more creatures to the main route (L1)? It changes the calm climb. | Yes, Cradle and Causeway first, then judge |
| D8 | Pause painted backgrounds for Routes B, C, D until phases 1 and 2 are done? | Yes |

The seven decisions in the first review still stand; D1, D4 and D5 here replace its numbers 1 and 5.
