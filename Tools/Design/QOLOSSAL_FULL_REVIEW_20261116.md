# Qolossal: full review and tool prompts (16 Nov 2026)

Read-only review of the checkout on `claude/project-thread-6y7wyg` (HEAD `b5bddd7e`, 3,377 uncommitted files). I read the handoff, the master polish plan, `POLISH_PLAN.md`, the roadmap, Reviews 110-117, the gameplay code and data, and saved captures. **I did not open Unity or play the game**, so anything about feel is a reading of code and stills, not a verdict.

Each finding is marked **NEW** (not in the master polish plan) or **PLAN** (already covered; only the next action is given). Prompts are in section 4, ready to paste.

---

## 1. The five things that matter most

| # | Finding | Why it's first |
|---|---|---|
| 1 | **Gate 70 is waiting on you, not on more work.** Seven workstreams are built and script-checked but unplayed (combat reach, death beat, run pace, danger zones, minimap, ambient life, cave depth, Ribwood). New work keeps landing on top of unjudged work. | One 90-minute play session unblocks more than any prompt here. Prompt C1 makes one ordered play script out of the four separate test lists. |
| 2 | **Nothing since 3 Nov is committed, and nothing at all is pushed.** 3,377 changed files and about 19 commits exist only on this PC, with Codex and other threads editing the same folder. | One bad overwrite or disk fault loses two weeks. Needs only your "commit and push". |
| 3 | **Route B art has cost 9 rounds on one room (B04) and is still unusable.** Review 117's diagnosis is right: the grade stack is the problem. | Stop sending B04 to Codex. Claude makes the Review 117 fix in the preview (prompt C2). And change the method for B, C, D (finding N3). |
| 4 | **Amber has nothing to buy.** The shop sells one Heart seed (60), and "upgrades" that say "Prototype: recorded; no stat change yet", three of them for weapons that no longer exist. Meanwhile the 51 underground rewards alone pay out about 1,300 Amber. | Every room's reward is Amber, so the whole exploration loop pays in a currency with no use. Cheap to fix in code, no art needed (prompt C3). |
| 5 | **Qori's speed is the real cause of the foot skating.** He moves 7 u/s on a 1.73 u body: four body-heights a second, a sprinter's pace, at 12 drawn frames a second. No scurrying run cycle can cover that. | Before paying for more run clips, test one **bounding** run (long leaps, both feet off the ground), which suits a long-eared creature and halves the cycles needed (prompt L1, 9 credits). |

---

## 2. New findings (not in the polish plan)

### Mechanics and design

| ID | Finding | Evidence | Recommendation | Cost | Tool |
|---|---|---|---|---|---|
| N1 | **Economy has no sink** (see #4 above). `upgrade:staff-t2` and `upgrade:sap-vial` are set by the shop and read by nothing. | `MraTownBuilder.cs:65-69, 100-101`; grep finds no reader | Staff upgrades with real effects at Brannick, a Sap vial that heals, map pages priced to matter. Remove sword, mace and spear rows. Then an Amber balance table (income by region against prices). | M | Claude C3 |
| N2 | **No healing you control.** Hearts come back only from a 40% sap orb drop or by dying (full refill). Dying is the best heal in the game, which makes guardian fights attrition-free and long routes swingy. | `SapOrb.MaybeDrop(.4f)`, `PlayerHealth.RestoreAfterRespawn` | The Sap vial from N1: 1 charge (2 and 3 as upgrades), heals 2 hearts with a 0.8 s hold that an enemy hit interrupts, refilled at Waymarks and rest points. Answers old Q3 at the same time: death stays free, but healing becomes a skill. | M | Claude C3 |
| N3 | **Per-room painted cave backgrounds don't scale.** Route A took Reviews 97-105 and 207 MiB BC7. B04 alone is at 142 MiB and 9 rounds. Four routes this way is about 1 GiB of cave backdrop and months of review rounds, before Series S or Switch 2 have been measured once. | Handoff 9-12 Nov; Reviews 108-117 | For B, C, D: one shared natural rock kit per route (wall, buttress, root, 2 light maps) plus **2 or 3 hero paintings per route** at the landmark rooms only. Gate each brief at 2 revision rounds; after that Claude fixes in-engine or the room ships on kit. | saves weeks | decision, then Codex |
| N4 | **The main route is almost combat-free; all the bosses are optional.** 12 enemies across 7 surface regions, one guardian (Ribwood). The underground has 36 creatures and 6 bosses, all animated and atlased. A player who only climbs meets the game's combat depth almost never, and the Summit has no test before the reveal. | `mra_world.json` encounters: 12; old Q8 never answered | Reuse underground creatures on the surface through encounter slots in `mra_world.json` (no credits, frames exist): 3-4 per region rising to 6, one mini-boss gate before the Summit. Your call first, because it changes the game's tone from calm climb to action climb. | M | decision, then Claude C5 |
| N5 | **Evade and dash are two buttons for one idea.** Evade (Y, ground, cannot be hurt) and Wind Leaf dash (B, air too) overlap, while attack sits on LB and sprint on a held L3. With thread on RT/LT and growth on RB the pad is full. | `PlayerMovement.cs:299-326`, `PlayerCombatInput.cs:14` | Recommended layout to settle old Q2: Attack X, Jump A, Dash B (evade folds into it: before the Wind Leaf it is the ground evade, after it gains distance and air use), Staff shot Y, Thread RT, pull LT, Heal RB hold, sprint by toggle on L3 or always-on at full tilt. Keyboard: J attack, K dash, L shot. | S once decided | decision, then Claude C4 |
| N6 | **A Steam demo slice would focus the polish.** The game is large (7 regions, 89 rooms) and no outside player has seen it. | Roadmap step 6 lists a demo as optional | Make "Cradle, Causeway and Route A" the demo, and close gates 70 and 80 on that slice first. It gives a fixed target for sound, shell and wishlists, and Steam Next Fest needs exactly this. | planning | decision |

### Visual

| ID | Finding | Evidence | Recommendation | Cost | Tool |
|---|---|---|---|---|---|
| N7 | **Surface terrain bodies read as wallpaper.** Below the painted top edge, the interior is one cracked-stone tile repeated over the whole hillside at even brightness. It's the largest area on screen in most surface frames. | `ambient/film/MRAtlas_MR01_Cradle_plants_after_03.jpg`, `climb/after/MR02_climb.jpg` | No new art first: a shader pass on the terrain fill. Darken with depth below the walk line (to about 70% at 6 u), add low-frequency value and hue drift in world space so tiles stop matching, and a soft contact shadow under the lip. Byte-identical art. Codex breakup patches only if that isn't enough. | S-M | Claude C6 |
| N8 | **The danger zone is a flat opaque orange box** that hides Qori's feet and the floor, and reads as a debug rectangle next to painted art. The plan asked for a "soft shape". | `PolishGates/70/zone_G-03_strike.png` | Restyle before you judge it: a floor-hugging strip about 0.35 u tall with a soft top edge, 35-45% alpha, ember fill that grows from the attacker's side, a thin ivory edge line, one white flash on impact. Same data, so it still can't lie. | S | Claude C7 |
| N9 | **Guardians: try a free upscale before spending credits.** Ludo exports 300-400 px frames whatever you ask for, so regenerating in Ludo won't reach 216 px/u on a 5-6 u body. | Handoff 1 Nov ("Ludo frames are 300-400 px, so guardians are upscaled"); plan Appendix B | Test on G-03 only: a local AI upscale (3x) of the existing frames, rebuilt into its atlas, compared side by side in A08. Costs no credits. If it fails, Codex paints 6-8 native key poses per guardian and the game holds poses longer. This bends the "no RGB upscaling" rule for creature frames, so it's your decision. | S test | decision, then Claude C8 |
| N10 | **Platform kit against painted backdrop.** In Route A the backdrop is fine masonry with light, and the playable pillars in front are softer, flatter grey blocks with a hard vertical seam where they meet the wall. The thing you stand on is now the least finished thing in the frame. | `KeepersCistern_Integration_20261109/Captures/A04_middle.png` | Extends the plan's "outer terrain repaint" to the Route A kit: apply the route's light maps and a rim highlight to the kit pieces in the shader first; repaint only what still fails. | M | Claude, later Codex |
| N11 | **The HUD ring shows a weapon that never changes.** With staff only, the 150 px medallion is dead space, and Amber and healing have no home on the surface HUD. | `GameHud.cs` weapon slot | Put the Sap vial charges in the ring (N2), with the staff-shot charge as its fill. Amber count beside it everywhere, not only underground. | S | Claude C3 |
| N12 | **Leapseed still has placeholder art** (128 px squares), and it is the first relic every player sees. | `Assets/Art/Placeholder/` | Codex Batch 51, small. | S | Codex X1 |
| N13 | **No title or store art exists.** The shell (plan WS9) and the Steam page both need a logo and key art, and it has a long lead time. | no logo or key art files in `Assets/Art` or the delivery folders | Codex Batch 52: logo and one key art painting, cut to Steam's capsule sizes. | M | Codex X2 |

### Process

| ID | Finding | Recommendation |
|---|---|---|
| N14 | **Ludo credits: the handoff's own arithmetic says 691, not 800** (792 after the forge, 763 after small assets, 691 after residents). | Check the number on Ludo before the next run. Budget in section 3. |
| N15 | **Stale and conflicting notes for other threads.** Internal IDs still say C/D/E while docs say B/C/D; `AmbientLifeCheck` and `CaveMinimapCheck` reruns are owed; Qvale interiors data and scenes disagree until the restore menu is run. | Fold into the next Unity-lane session (prompt C1 lists them). |

---

## 3. Already in the plan: next action only

| Plan item | State | Next action |
|---|---|---|
| WS2 combat truth | Staff hits follow the drawn staff; reach fell from about 2.2 u to 1.5 u and fights were tuned for the old reach | You judge reach in play. If short, lengthen the hit shape by 0.3 u rather than retune every enemy. Per-enemy box review and fairness table still open. |
| WS3 access | Checker 0 errors; gorge widening to 28 u and the D4 required beats need scene edits | Unity lane, after your play session |
| WS4 minimap, WS5 ambient, WS7 parallax | Built, unplayed | Your verdicts (C1) |
| WS6 Qori clips | Hurt and death are stand-ins; missing: run start and skid, look up and crouch, shrine touch, rope swing. Down-stab exists (three plunge clips), so it is not missing | Ludo L1-L5, 45 credits |
| WS7 Heights and Summit | Prompts 38/39 on hold; "no long columns" not yet applied to a rewrite | Needs your answer: does the rule also cover finished regions? Then one rewritten brief |
| WS8 memory | Atlases done on Windows (underground 4.6 GiB to 1.7 GiB) | Surface 4K backgrounds still RGBA32 because of NPOT plus mipmaps: apply the Route A fix (mips off, BC7). Offered before; say go |
| WS9 shell, WS10 sound, WS12 release | Not started, by design | After gate 70. AI-content disclosure and tool licence check should not wait for 90 |

**Ludo budget (691 credits, to be confirmed):** Qori clips 45, redo reserve 27, guardian decision (N9) 0 now. That leaves about 620 untouched for surface enemy gaps, boss intros and trailer shots. Don't spend on run cycles beyond L1 until it's judged in game.

---

## 4. Prompts

### Claude (code and Unity; one Unity thread at a time)

**C1. One play script for gate 70**
```
Read QOLOSSAL_SESSION_HANDOFF.md and Tools/Design/PolishGates/70/MORNING_TEST_LIST.md, ambient/AMBIENT_TEST_LIST.md and the 3 Nov "user's test list" in the handoff. First, with Unity closed, run the owed checks (AmbientLifeCheck, CaveMinimapCheck, Polish70Check; always pass -saveFile) and Qolossal > Qvale > Restore House Interiors plus QvaleHousesCheck, and fix what fails. Then write Tools/Design/PolishGates/70/PLAY_SESSION.md: ONE ordered route I can play in about 90 minutes with a pad, starting from a prepared test save, that passes every item on those lists once, in travel order (Cradle, Causeway, Ribwood, Heights, Qvale, then A01-A08, C01, D03, the Heart). For each stop: where, what to do, what to look for, and a blank verdict (good / change / reject). Use Scene Finder jumps where walking adds nothing. Don't change game behaviour. Update the handoff.
```

**C2. B04 preview: Review 117 fix**
```
Read Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_117.md and make its recommendation yourself in Assets/Art/Prototypes/KeepersNursery_Batch50/ (preview only, no scene edits, no accepted art changed): return to the r4 shader and light maps, keep only r5's placement fixes (buttresses closing the void, crops breaking the deck-aligned edge), remove the contrast pivot, haze, knee, highlight ceiling and the 430% multiplier, and add one plain multiply on the rear/SET layers with MID and FAR in proportion: F9/F10 in 5% steps from 40% to 100%, F11 saves to grade.json. Report the walk-band ratio as information only. Compile, run the deck-edge and ledge sweeps, save four captures at 60/75/90/100%, and tell me how to open the preview. Check git status first; Codex shares this folder.
```

**C3. Amber sinks, healing and the HUD ring**
```
Design first, then build after I approve the table. Goal: Amber must buy things that matter, and Qori needs a heal he controls. Read MraTownBuilder.cs (shops), TownShop.cs, PlayerHealth.cs, SapOrb.cs, GameHud.cs, QoriArmoryFactory.cs and layouts.py reward amounts.
1. Write Tools/Design/ECONOMY_TABLE.md: total Amber obtainable per region and route in order of access (from mra_world.json rewards and the 51 ugp rewards), and proposed prices so a thorough player affords everything by the Summit and a main-route-only player affords about half.
2. Proposed goods (adjust with reasons): Sap vial (hold RB / F for 0.8 s, heals 2 hearts, interrupted by a hit, refilled at Waymarks and rest points; 1 charge, +1 at two upgrades); Leaf Staff tiers that change numbers in WeaponTuning (reach +0.2 u, charge time -20%, charged shot pierces); Heart seeds; map pages. Remove the sword, mace and spear rows and every "Prototype" description.
3. HUD: the ring shows vial charges with the staff-shot charge as a fill; Amber count on the surface too. Use existing UiSkin art; list any icon Codex must paint.
4. Save: new fields with migration from current saves. Tests: extend Polish70Check. All SFX stay off.
Builder-generated scenes: change MraTownBuilder and rebuild, never hand-edit. Stop after step 1 for my approval.
```

**C4. Control layout (after I pick it)**
```
Apply this layout in PlayerMovement, PlayerCombatInput, PlayerThread and ControlsGuide, keeping the bindings in one new static GameBindings class so remapping can build on it later: pad Attack X, Jump A, Dash/Evade B (one action: without the Wind Leaf it is today's ground evade; with it, today's dash, keeping 0.08 s of the evade's protection at the start on the ground), Staff shot Y (hold to aim), Thread RT, pull LT, Heal RB, sprint toggled by L3. Keyboard: J attack, K dash, L shot, F heal, Shift sprint; mouse buttons stay as alternates. Update every test that sends the old keys (UndergroundCombatBot, MraTestDriver, Polish70Check) and rerun them with -saveFile. List anything that felt ambiguous instead of guessing.
```

**C5. Surface encounters (after I approve the tone change)**
```
Propose, then build after approval: surface encounters using creatures that already have frames and behaviours (surface set plus underground E-10 to E-20), added as data in mra_world.json "encounters" and built by MraWorldBuilder. Target 3-4 per region in MR01-MR03 rising to 5-6 in MR05-MR07, none in Qvale, none on a required jump, none within 6 u of a shrine or Waymark, each on a shelf at least 10 u wide. Each relic's first enemy should be beatable more easily with that relic. Write the table (region, edge, creature, why here) to Tools/Design/SURFACE_ENCOUNTERS.md and stop for approval. After building: UndergroundFightAreaCheck-style ground check on every new shelf, and memory per region from MemoryBaseline, since surface scenes will now load underground creature atlases.
```

**C6. Terrain fill depth (shader only)**
```
The surface terrain interior reads as flat repeated wallpaper (see Tools/Design/PolishGates/70/ambient/film/MRAtlas_MR01_Cradle_plants_after_03.jpg). Without changing any art bytes or colliders, extend the terrain fill material (SurfaceTerrainContrast / TerrainMaterial path) with: (a) darkening by depth below the nearest walk edge, reaching about 70% value at 6 u; (b) world-space low-frequency value drift of ±6% and hue drift of ±2% at a 9-14 u scale so neighbouring tiles differ; (c) a 0.5 u soft contact shadow under the painted lip. One strength value per region, off by default behind Qolossal > Backgrounds > Terrain depth. Save before/after captures at the same six camera places per region to Tools/Design/TerrainDepth_20261116/ and stop for my verdict. Mind Series S and Switch 2: one extra texture sample at most, noise from a small tiling texture, no per-pixel loops.
```

**C7. Danger zone restyle**
```
Restyle AttackZoneTelegraph before I judge it (current look: Tools/Design/PolishGates/70/zone_G-03_strike.png, a flat opaque box over Qori). Keep the data and timing. Floor attacks: a strip 0.35 u tall on the floor line with a soft top edge, 40% alpha ember fill that grows from the attacker's side across the warning time, a 2 px ivory outline at 70%, one white flash frame on impact, gone in 0.1 s. Air or body attacks: the same treatment as an outline with a 15% fill. Never mint. Draw behind Qori and the creature, in front of terrain. Re-save the six zone_*.png captures and add Root Matron's shockwave and seeds and the Warden's flood as their own shapes.
```

**C8. Guardian upscale test (if I allow it)**
```
Test on G-03 only, credits-free: upscale its existing frames 3x with a local Real-ESRGAN (or similar) anime/illustration model into a scratch folder outside Assets, clean alpha edges, import Tight at the correct px/unit, rebuild only its atlas with QoriFrameAtlasBuilder.BuildCreatures, and save side-by-side 4K captures of old and new in A08 at idle, warn and strike, plus the atlas memory before and after. Keep the original frames untouched and make the switch reversible. Stop for my verdict; do not process other creatures.
```

### Codex (art; next free batch on disk is 51)

**X1. Batch 51: Leapseed relic**
```
Qolossal Batch 51: Leapseed relic art. Deliver to Codex_v2_full/Batch51/. Two files only.
1. Relic_Leapseed.png: the world pickup shown on its shrine. A plump twin-lobed seed that looks spring-loaded, like a maple samara folded shut, in the same painted style, line weight and lighting (top-left key) as Assets/Art/Codex/Props/Relic_LivingThread.png and Relic_ClimbingMoss.png. Warm seed-brown and leaf green with one small ivory highlight. No mint, no glow painted in, no face, no eyes, no text. Transparent background, straight alpha, nothing cut off, 24 px clear margin. Canvas exactly 1024 x 1280 px, native, no upscaling. It displays about 1.0 u tall.
2. Icon_Ability_DoubleJump.png: HUD icon, exactly 256 x 256 px, matching Icon_Ability_Dash.png and Icon_Ability_Glide.png in Assets/Art/Codex/Props/ (same frame treatment, stroke weight and palette): the seed with two stacked upward chevrons. Must read at 48 px.
Do not edit any existing file, scene, prefab or the accepted manifest. Include MANIFEST_51.json with sha256 and a contact sheet showing both beside the five existing relics and icons at equal scale. Stop for Claude review.
```

**X2. Batch 52: logo and key art**
```
Qolossal Batch 52: title logo and key art, in two stages. Stage 52a is concepts only; stop for approval before 52b. Deliver to Codex_v2_full/Batch52/.
The game: a small leaf-cloaked, long-eared creature, Qori, climbs a mountain that is secretly a sleeping titan. NEVER show or hint the titan: no faces, hands, ribs or body shapes in the rock. Style: the game's painted look (reference the accepted BG_Terraces and BG_Qvale 4K paintings), dusk-peach sky, pale stone, living green; mint only as tiny wayfinding glints.
52a: three logo concepts for the word QOLOSSAL (hand-drawn lettering, readable at 230 px wide, on transparent, each 2560 x 1440) and three key art thumbnails (1920 x 1080): Qori small in the lower third looking up a vast climb.
52b, after approval, native sizes, no upscaling: KeyArt_Master.png 7680 x 4320 with the focal area kept inside the central 3840 x 1240 band and inside a central 1200 px wide column, so one painting crops to every size; Logo_Master.png 4096 px wide, transparent, plus a one-colour ivory version. Then export crops with the logo placed: 920x430, 462x174, 1232x706, 748x896, 600x900, 3840x1240 (no logo), 1280x720 logo only on transparent, 1438x810 soft background. These are Steam's store sizes as I know them; list them in the manifest so they can be checked against Steamworks before upload. No scene or project edits.
```

### Ludo.ai (12 fps, 3 s, 9 credits a clip; test L1 in game before L2-L5)

Use the same Qori reference image and settings as the accepted idle. Side view facing right, full body, staff in hand, camera locked, character stays in the centre of the frame.

| # | Prompt | Becomes | Credits |
|---|---|---|---|
| L1 | `Qori running with long bounding leaps like a hare, each bound covers twice his body height, both feet leave the ground and tuck up, ears and leaf cloak streaming back, body leaning forward, exactly four even bounds, focused eyes, face facing right, no mouth, seamless loop, runs in place` | Run loop test. 4 bounds in 36 frames is 9 frames a bound; the editor log gives the game's walk as 5 u/s and run as 7.5, so a bound must cover about 3.75 u at walk pace (2.2 body-heights), which is what the prompt asks for | 9 |
| L2 | `Qori gets hit from the front and flinches back one step, eyes squeezed shut, then his knees give way and he collapses onto his side and lies still, eyes closed, no mouth, face facing right` | Hurt (frames 1-8), death (9-36) | 9 |
| L3 | `Qori standing, pushes off into a run with a strong first step leaning forward, then plants both feet and skids to a stop leaning back with dust at his heels, focused eyes, face facing right, no mouth` | Run start, skid and turn | 9 |
| L4 | `Qori standing still, tilts his head back and looks up with wide curious eyes and holds, then lowers into a low crouch and looks down at the ground and holds, feet do not move, no mouth` | Look up, crouch (the holds give loopable poses) | 9 |
| L5 | `Qori swings on a vine rope holding it with both hands above his head, body swinging forward then back like a pendulum, legs trailing, calm eyes, face facing right, no mouth, seamless loop` | Rope swing loop | 9 |

Shrine touch: cut from L4's look-up hold with a raised hand added in code, so no clip is spent on it. Total 45 credits.

---

## 5. Decisions I need from you

1. **Commit and push** the current folder to the branch? (Recommended: yes, today.)
2. **Route B method:** Claude makes the Review 117 fix (C2), and B, C, D move to kit plus 2-3 hero paintings per route (N3)? (Recommended: yes.)
3. **Economy and heal (C3):** go? (Recommended: yes, it needs no art.)
4. **Surface combat (N4):** calm climb or more encounters? (Recommended: more, by reuse.)
5. **Control layout (N5):** accept the proposed layout? (Recommended: yes.)
6. **Guardian upscale test (N9):** allow it on one guardian? (Recommended: yes, as a test.)
7. **Demo slice (N6):** Cradle, Causeway and Route A as the first target? (Recommended: yes.)

---

## 6. Play session, 16 Nov (added after the review)

I opened Unity on a copy of your save (`claude_feel_play.json`; your own save is untouched), played about ten minutes in Play mode and closed Unity again. Nothing in the project was saved or changed. **How I played:** timed key and mouse presses with screen captures every 50-250 ms, which I then read as strips. So I can judge readability, pacing, damage and framing, but not input weight, latency or sound. No pad. Frames are in `Tools/Design/PlaySession_20261116/`.

Played: a Cradle cave, A04, the Sluice Warden in A08, the Terraces and Qvale.

### What worked

- The staff swing reads well: the arc, the leaf burst and the recovery are clear at normal zoom (`a04_attack_run_strip.jpg`).
- A08 with the Cistern art looks like a finished game: gate, lanterns, the Warden's colours.
- The minimap is readable at a glance. The relic row and landing on thin platforms behaved.

### What I'd change, in order

| ID | What I saw | Change | Tool |
|---|---|---|---|
| P1 | **Qori disappears into the surface backgrounds.** In Qvale and the Terraces he shares the background's greens and creams, and the painting is as sharp and contrasty as he is, right down to the walk line (`qvale_camouflage.jpg`). This is the biggest visual problem I met. | Shader only: grade the Mid and Far planes (less saturation, values lifted toward the sky colour, strongest in the band behind the walk line), and give Qori and enemies a soft dark rim and a ground shadow. | Claude P-C1 |
| P2 | **Thorn floors chain-hit.** In the first Cradle cave room I went from 7 hearts to 2 in about seven seconds: each hit knocks Qori back onto more thorns (`cradle_cave_2_hearts.jpg`). The first thorns are about 5 u inside the door. | A thorn or water hit costs one heart and returns Qori to the last safe ground with a quick fade, as the genre does. Keep knockback for enemies only. Keep the first hazard of any room at least 8 u from its door. | Claude P-C2 |
| P3 | **The hurt blink makes Qori nearly invisible** on dark cave walls, at the moment the player most needs to see him. | One white flash frame, then blink between 100% and 60% alpha, never lower. | Claude P-C2 |
| P4 | **Lost hearts vanish** instead of showing empty leaves, so at 2 of 7 the HUD shows two leaves and a bare vine and you can't see your maximum. | Find why the empty-leaf sprite doesn't show; fix or repaint it. | Claude P-C2 |
| P5 | **The Warden fight is framed on Qori, not on the fight.** The boss sat half off the right edge for most of it, its weak point lit above the top of the frame, and about eight of my hits moved nothing on the bar with no sign they were being blocked. The danger box appeared behind me, where neither of us was (`a08_fight_strip.jpg`). | Arena camera that holds both. A clear "blocked" spark on armour hits and a weak-point cue the eye can find. Then recheck the sweep's zone position. | Claude P-C3 |
| P6 | **Placeholder UI is the first thing that says "unfinished".** The boss bar is three red rectangles with Arial text and shows before the boss is on screen; the route bar is a grey box; Amber is a plain number (`a08_boss_bar.jpg`, `terraces.jpg`). | Codex paints the frames; Claude wires them and shows the boss bar when the fight starts. | Codex P-X1, then Claude |
| P7 | **Terrain clashes with the new art.** In Route A the yellow-grey cobbled blocks sit against teal masonry; in region caves a coarse rock tile with cracks larger than Qori fills about 60% of the screen and the backdrop shows through square windows (`cradle_cave_2_hearts.jpg`). | Per-route terrain tint and value match in the shader (C6 extended underground), a smaller rock tile scale, a soft inner shadow at cave cut-outs. Repaint only what still fails. | Claude C6 |
| P8 | **A04's playable view is mostly a flat brick wall** with thin slabs floating in front of it (`a04_flat_wall.jpg`). The arches and lanterns that made Route A's captures look good are out of frame for much of the room, and the slabs break the "everything stands on something" rule. | Brackets or corbels under every floating deck, and two or three lantern niches and a pier moved into the play band. Kit pieces only. | Claude |
| P9 | **The same landmark repeats inside one screen** in Qvale: the big tree and village appear twice. | Check the Mid plane's tile width against the 4K view; offset or mirror alternate tiles, or cover one with a foreground piece. | Claude |
| P10 | **Zoom varies a lot between cave rooms.** In the Cradle cave Qori was about 3% of the screen width and his animation unreadable; in A04 he was a comfortable size. | Cap play zoom-out at about camera size 7 and keep wider views for look-outs. Needs your eye. | Claude |
| P11 | **Running past a cave mouth took me underground without pressing Up**, with a long black screen in the editor. | Confirm in a build. If it enters on touch, require Up: an accidental scene load mid-run is costly. | Claude check |

Not confirmed: foot skating. At close zoom the run looked acceptable in my strip, but 60 ms samples can't show sliding, so L1 stays a test, not a certainty.

### Prompts from the play session

**P-C1. Figure-ground readability (shader only)**
```
On the surface, Qori blends into the Mid and Far background planes (see Tools/Design/PlaySession_20261116/qvale_camouflage.jpg and terraces.jpg). Without changing art bytes: (1) add an atmospheric grade to ParallaxLayer's material for Mid and Far planes: saturation -15% Mid / -25% Far, values lerped 12% / 22% toward the region's sky colour, with the effect strongest in a 4 u band centred on the camera's walk line and half strength elsewhere; one set of numbers per region in the Regions table. (2) Give Qori and every enemy frame renderer a 2 px soft dark rim (the region's shadow hue, 45% alpha) and a soft elliptical ground shadow that fades with height above ground. Put both behind Qolossal > Backgrounds > Readability on/off so I can compare, save before/after captures at the same camera places in all seven regions to Tools/Design/PlaySession_20261116/readability/, rerun ClimbCapture, and stop for my verdict.
```

**P-C2. Hazard hits, hurt blink, empty hearts**
```
Three fixes from play. (1) Environmental hazards (thorns, hazard water, spills: any IContactHazard that is not an enemy or boss) cost one heart, then fade 0.15 s, place Qori at his last safe ground (the last grounded position at least 1.5 u from any hazard, updated while grounded and not on a moving or crumbling piece) and fade in, with the usual 1 s protection and no knockback. Enemy and boss hits keep knockback. Add a check to CheckBatch: no hazard within 8 u of a room door on the walk line; list offenders and do not move them without telling me. (2) Hurt blink on the frame renderer: one full-white frame, then alternate 100% and 60% alpha at 12 Hz, never below 60%. (3) GameHud: lost hearts must show the empty-leaf sprite (at 2 of 7 hearts only two leaves and a bare vine show); find out why leafEmpty does not render and fix it. Extend Polish70Check for all three and run it with -saveFile.
```

**P-C3. Guardian fight framing and feedback**
```
In A08 the Sluice Warden sits half off-screen and its weak point lights above the frame (Tools/Design/PlaySession_20261116/a08_fight_strip.jpg). For all three guardians and three mini-bosses: when UgArenaSeal shuts, blend the camera over 0.6 s to an arena frame that contains the boss's full bounds, its weak point and the whole fight floor (cap at camera size 7.5; if an arena does not fit, tell me which), and blend back when the fight ends. Show BossHealthBar only from that moment. Hits on armour: a pale spark at the contact point, a short staff recoil and no hit-stop, clearly different from a damaging hit. When the weak point opens, pulse it and draw a thin ivory line toward it for 0.4 s so the eye finds it. Recheck the Warden's sweep zone position against the claw frames. Rerun UndergroundGuardianTest and UndergroundCombatBot with -saveFile and save a new strip.
```

**P-X1. Codex Batch 53: HUD frames**
```
Qolossal Batch 53: HUD frames, to replace placeholder rectangles. Deliver to Codex_v2_full/Batch53/. Match the existing HUD ring, vine and leaf hearts (the UiSkin art): carved root-wood, small leaves, ivory and warm brown, no mint, no text baked in, transparent PNG, straight alpha, native size, no upscaling. Sizes are for a 3840x2160 screen.
1. UI_BossBar_Frame.png 1920 x 120: a horizontal root frame with an inner window of exactly 1760 x 40 px centred, 9-slice safe (ends 160 px each, plain middle).
2. UI_BossBar_Fill.png 1760 x 40: ember-red sap fill, tileable horizontally.
3. UI_BossBar_Notch.png 16 x 56: phase divider.
4. UI_RouteBar_Frame.png 2400 x 160: frame for the region progress bar with an inner window of 2160 x 36 px, 9-slice safe, and a soft parchment backing at 60% alpha in place of the grey box.
5. UI_Amber_Icon.png 96 x 96: a cut amber drop with one highlight, readable at 32 px.
6. UI_Leaf_Empty.png 112 x 112: a withered version of the existing full heart leaf at the same outline and size, clearly different in value so it reads in greyscale.
Include MANIFEST_53.json with sha256 and the inner-window coordinates, plus one mock-up of all pieces over a Terraces screenshot and one over a dark cave. No scene, prefab or code edits. Stop for Claude review.
```
