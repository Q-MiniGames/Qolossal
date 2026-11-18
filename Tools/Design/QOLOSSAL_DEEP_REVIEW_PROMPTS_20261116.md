# Qolossal: prompts for every action in the deep review (16 Nov 2026)

Each prompt matches an ID in section 6 of `Tools/Design/QOLOSSAL_DEEP_REVIEW_20261116.md` ("the review"). Finding IDs such as H1 or C3 point to that file, which holds the evidence and the numbers.

**How to use them**

- **Claude prompts (A..):** paste the *Standing rules* block first, then the prompt. One Unity-lane prompt at a time, in the order of section 6.
- **Codex prompts (X..):** each is complete on its own. Next free batch on disk on 16 Nov was 51; check before sending.
- **Ludo prompts (L..):** paste the prompt text only. Generate L1 first and have it tested in game (A16) before spending on the rest.

---

## Standing rules (paste before every Claude prompt)

```
Standing rules for this task. Follow all of them.
1. Read QOLOSSAL_SESSION_HANDOFF.md (top entries), Tools/Design/QOLOSSAL_DEEP_REVIEW_20261116.md, and CLAUDE.md before anything else. Finding IDs in my prompt refer to that review.
2. Run git status first. Codex and other threads share this folder. If a file you must edit has changes you did not make, stop and tell me which file.
3. One Unity at a time. Check no Unity.exe is running before you open it or run a batch method. Close it when you finish.
4. Every Play-mode or batch run passes -saveFile <a test name>. Never let a run write qolossal_save.json.
5. MRA surface scenes and the underground platforming are generated. Change mra_world.json, layouts.py or the builders and rebuild. Never hand-edit a generated scene. Back up a scene before any rebuild, as earlier threads did.
6. Do not change accepted art bytes, the accepted manifest, music, or anything under Assets/QoriRigTest. All SFX stay off (Sfx.EffectsRemoved).
7. Put every new number in the tuning assets (MovementTuning, WeaponTuning, FeedbackTuning) or in data, not in code constants, so I can tune it in Play mode.
8. After the change: compile with 0 errors, run the checks I name, and save before/after captures or numbers where I ask. Scripts prove logic, not feel: say plainly what only my hands can judge and give me a short list of what to try, with where and which keys.
9. Do not commit or push. Add a dated entry at the top of QOLOSSAL_SESSION_HANDOFF.md saying what changed, what was tested and how, and what was not.
10. If something in my prompt turns out to be wrong or impossible, stop at that step, tell me what you found, and propose the nearest thing that works. Do not guess and do not widen the task.
11. No console or 60 FPS claims without a real build on real hardware.
```

---

## Claude prompts

### A00. Commit and push

```
I say: commit and push. Check git status and sizes first: list any single file over 50 MB and anything under Tools/IncomingArt or Builds that is about to be added (those must stay out). Commit everything else on the current branch (claude/project-thread-6y7wyg) in two commits: one for the ~3,000 .meta import changes from the frame atlas work, one for code, data, scenes and docs. Push that branch only; do not touch main. Report the commit hashes and anything you left out and why.
```

### A01. One rule for hazards and death; hurt blink; empty hearts

```
Fixes H1, H2, H4, H5, H6. Files: PlayerHealth.cs, PlayerMovement.cs, Hazard.cs, ThornHazard.cs, MraTimedHazard.cs, MraWater.cs, MraUgWater.cs, MraUgShelter.cs, QoriWalkFramesOnPlayer.cs, GameHud.cs, UndergroundGuardian.cs.

1. Safe ground. PlayerMovement keeps LastSafeGround: updated each physics step while IsGrounded, not on a mover or crumble (attachedRigidbody is null and the piece is not a crumble), and no hazard collider within 1.5 u of his feet. Shelters and "safe" pockets still set it when entered.
2. Hazard rule. Water, thorns and timed spills all call one new method, PlayerHealth.HazardHit(): if protected, ignore; else lose one heart, HitStop for qoriHurt, fade the curtain 0.12 s, place Qori at LastSafeGround, fade in 0.12 s, 1 s of protection, hearts kept, no knockback. Remove the bounce from ThornHazard and MraTimedHazard and the Respawn() call from Hazard, MraWater and MraUgWater. A fall below resetBelowY uses the same method.
3. Death rule. At 0 hearts the existing DeathBeat runs, then Qori returns to his active Checkpoint (the rest point), NOT to LastSafeGround or a shelter, with full hearts. The room's creatures reset as on a scene load, and a guardian fight resets. Keep PlayerMovement.Respawn for this path only.
4. Enemy and boss hits are unchanged: one heart and knockback.
5. Hurt blink (QoriWalkFramesOnPlayer line ~223): one full-white frame on the hit (use the SpriteWhiteFlash shader the creatures use), then alternate alpha 1.0 and 0.6 at 12 Hz for the protection time. Never below 0.6.
6. Empty hearts. At 2 of 7 hearts the HUD showed two leaves and a bare vine. Find why leafEmpty (Assets/Art/Codex/UI/HUD_Health_Leaf_Empty.png exists) does not show, and fix it so every heart slot up to the maximum is always drawn, full or empty.
7. Data check. Add to UndergroundPlatformingBuilder.CheckBatch: no hazard within 8 u of a room door along the walk line. Report the offenders as a list; do not move anything.
New numbers (fade times, 1.5 u clearance, blink rate, alpha) go in FeedbackTuning.
Verify: extend Polish70Check with: thorn hit costs exactly one heart and lands on safe ground; water costs one heart and no longer refills; death goes to the checkpoint, not the shelter; the blink never drops below 0.6 alpha; 7 heart slots draw at 2 hearts. Rerun UndergroundGuardianTest and UndergroundEnemyTest. For me to try: the first Cradle cave room (thorns 5 u inside the door), A01 water, D05.
```

### A02. Movement fixes

```
Fixes M1, M2, M5, M6, M8. File: PlayerMovement.cs, MovementTuning.cs, Player.prefab (one field), layouts/validate tools only to re-run them.

1. M1 glide. Set glideNeedsNewPress to true on Player.prefab and make it the default in code. The glide opens only on a Jump press made in the air that did not produce a jump, wall kick or double jump, and stays open while Jump is held. A press within jumpBufferTime before the double jump is still the double jump. After the double jump is spent, the next air press opens the glide. Holding Jump through a normal jump must never open it.
2. M2 air speed. In the airborne steering branch (the MoveTowards toward moveInput * moveSpeed) steer toward RunSpeed, not moveSpeed. Keep "already faster keeps its speed". Add a tuning field airMaxSpeed defaulting to runSpeed.
3. M5 drop-through. Standing on a one-way deck (PlatformEffector2D with useOneWay), Down held (ledgeVerticalInput < -0.5) plus a Jump press: ignore collision with that deck for 0.25 s and do not jump. No effect on solid ground. Add dropThroughSeconds to MovementTuning.
4. M6 corner nudge. While rising, if a ceiling cast of 0.1 u hits and shifting Qori sideways by up to cornerNudge (0.25 u, tuning) in either direction clears it, shift him and keep his upward speed. Never through a one-way deck (those already pass).
5. M8 interiors. In the five Qvale house scenes set ForceWalk through the scene's existing setup component, add interiorSpeed (3 u/s) to MovementTuning used when ForceWalk is set, and in QoriWalkFramesOnPlayer use the run clip (not the walk clip) at its slowest pace there.
Then rerun Tools/Design/UndergroundPlatforming_20261030/validate.py with the new envelope and tell me every room whose reach result changed, and rerun the gorge test (MraPlayTest.Run -mraGorgeOnly): air speed changes jump distance, so the Heights glide gate may now be skippable from further; report the new carry distances.
Verify: Polish70Check additions for each of the five; SlopeMovementCheck; OneWayDeckTest. For me to try: hold Jump through a full jump after owning the Glidecap (must not float), a standing jump then steer, Down+Jump on any A04 deck, a jump into the corner of a block in A03, walking in a Qvale house.
```

### A03. Verbs and controls

```
Fixes M3, M4, P1, P2, C8. Needs my yes to decisions D4 and D5 in the review; if I have not said yes, stop and ask.
Files: PlayerMovement.cs, PlayerCombatInput.cs, PlayerThread.cs, QoriArmory.cs, CameraFollow.cs (look-down binding), ControlsGuide.cs, MovementTuning.cs, the bots and tests that send keys.

1. One bindings class. Create GameBindings (static, one place) that builds every InputAction used by gameplay. Move the bindings out of PlayerMovement, PlayerCombatInput, PlayerThread, QoriArmory and CameraFollow into it. No behaviour change in this step; compile and run Polish70Check before going on.
2. Remove dead verbs (P1): the Growth action (E / RB) and the flower-boost branch in the Jump press. Leave the GrowthPlatform and GrowthFlower classes in place, unreferenced by input.
3. Merge evade and dash (M4): one action, Dash. Without the Wind Leaf it performs today's evade (ground only, cannot be hurt). With the Wind Leaf it performs today's dash, and on the ground Qori cannot be hurt for the first dashProtectSeconds (0.10, tuning). Shellback rule unchanged. Remove the separate evade binding.
4. Sprint into the dash (M3): remove the Run/sprint action. If the move stick is held in the dash direction when a ground dash ends, Qori keeps sprintSpeed (12 u/s) as IsSprinting until he turns, stops, attacks, is hit or leaves the ground for more than 0.3 s. A dash that ends with no direction held settles to run speed as now.
5. Layout: apply exactly the table in section 2.8 of the review for pad and keyboard, keeping mouse left and right as alternates for attack and thread. Thread shorten/lengthen (W/S, LT/LB) must only act while attached, so W can aim and interact otherwise.
6. ControlsGuide: rewrite both columns to the new layout, 13 rows, no dead rows.
7. Update every test and bot that sends the old keys (UndergroundCombatBot, MraTestDriver, Polish70Check, MovementAbilityPlayTest, any other that fails to compile or pass) and rerun them all.
Verify: all of the above tests pass; grep shows no remaining "<Gamepad>/" or "<Keyboard>/" string outside GameBindings and UI scripts (list the UI ones you left). For me to try, with a pad: dash before and after the Wind Leaf, dash held into a run, attack on X while moving, thread on RT with LT/LB.
```

### A04. Attack rules

```
Fixes C1, C2, C6, C7. C2 needs my yes to decision D3; if I have not said yes, do the other three and leave staffSingleAttack as it is.
Files: PlayerCombatInput.cs, PlayerCombat.cs, QoriArmoryFactory.cs, WeaponTuning.cs, QoriWalkFramesOnPlayer.cs, QoriResinShot.cs.

1. C1 lunge. Delete the RunningFor >= 0.5 rule. The lunge (CombatMoveSlot.Dash) starts only when Attack is pressed within lungeWindow (0.25 s, tuning) after a ground dash ends or during it. A press while merely running is the opener.
2. C2 combo. Set staffSingleAttack to false in the asset. Check combo2 and combo3 play their clips with the hit on the contact frame, and that QoriFrameHitShapes has rows for combo2; combo3 uses its circle. Moving during recovery still ends it.
3. C6 charge. When ChargeReady first becomes true: one white flash on Qori's frames, a small ring puff, and the chargeHold loop switches to its last 4 frames only (a visibly "full" pose). Releasing before ready gives a normal opener instead of nothing. Damage multiplier 3 (was 2). Keep breaksArmor.
4. C7 shot. slingDamage 1, slingCooldown 0.6. Confirm in play mode that a shot can hit: a perched Hail Sentry, a Kite Ray, and a guardian's weak point while it is open (it should count as one of the hits in that opening). If any of the three does not register, fix the receiver, not the shot.
Verify: Polish70Check additions (press while running gives the opener; press after a dash gives the lunge; early charge release swings; ready flash fires once); UndergroundCombatBot full run, and give me the win table before and after, since reach and damage rates changed. For me to try: running swings into a Crab, dash then attack, the combo on a Root Snapper, a charged hit on a Bramble Creeper, shots at the Hail Sentry.
```

### A05. Honest guardians

```
Fixes C3, C4, C5, V9. Files: PlayerCombat.cs (PublishHit), WeaponKinds / CombatDamageResponse, UndergroundGuardian.cs, the six boss scripts, UndergroundEnemy.cs, AttackZoneTelegraph.cs, CombatDebugOverlay.cs.

1. C3 two responses. Add CombatHitDisposition.Deflected. Guardian and mini-boss bodies outside their opening, and a Wilted Husk while shut, return Deflected: a pale spark at the contact point, a short staff recoil of the frames only, no knockback, no control loss, no attack lock, no hit-stop. Blocked stays as it is for the Sluice Crab's raised claws, the Bramble Creeper's shell and the Shellback.
2. C4 hurt shapes. Replace "the whole body hurts during Strike" (UndergroundGuardian.HurtsOnContact) with a list per attack of boxes relative to the feet and facing, each with a start and end as a fraction of the strike. Author them against the actual frames with F9 on, for all six bosses and every attack: the limb's sweep, plus the part of the body that moves. A body part that does not move during that attack does not hurt. Floods, shockwaves and falling seeds get their own shapes in the same list.
3. Telegraph from the same list. AttackZoneTelegraph draws every shape in the list during Warn and Strike, so the warning and the hurt are one data source. Add a check to UndergroundGuardianTest: for each attack, sample 200 random points in the arena during Strike; a point hurts if and only if it lies inside a drawn shape.
4. C5 bug. The zone renders as a solid orange box although Warn asks for 4-16% alpha (see Tools/Design/PolishGates/70/zone_G-03_strike.png and Tools/Design/PlaySession_20261116/a08_fight_strip.jpg). Find the cause and tell me what it was.
5. V9 restyle. Floor shapes: a strip 0.35 u tall on the floor line under the shape, soft top edge, ember at 40% alpha filling from the attacker's side across the warning, 2 px ivory edge at 70%, one white frame on impact, gone in 0.1 s. Air and body shapes: ivory outline with a 15% ember fill. Never mint. Drawn behind Qori and creatures, in front of terrain.
Verify: the new check passes for all six; UndergroundCombatBot table before and after; six new zone_*.png captures plus one per flood, shockwave and seed fall. For me to try: stand beside the Warden's body during a sweep (must be safe), swing at it while shut (spark, no bounce), F10 to compare zone levels.
```

### A06. Readability

```
Fixes V1. No art bytes change. Files: ParallaxLayer.cs and its shader/material, the Regions table used by AmbientLife or MraRegion4K (wherever per-region numbers live), QoriWalkFramesOnPlayer.cs, EnemyFramesOnCreature.cs, UndergroundEnemy.cs, UndergroundGuardian.cs, a new editor check.

1. Background grade. For Mid and Far planes add shader parameters: saturation scale, lerp toward the region's sky colour, and a walk-band boost that applies extra strength inside a band of bandHeight (4 u) centred on the camera's walk line, fading out over 2 u above and below. Starting values per plane: Mid saturation 0.85, sky lerp 0.12, band extra 0.10; Far 0.75, 0.22, 0.08. One row per region so they can differ. Near and foreground planes are untouched.
2. Characters. One material for Qori's frames and every creature's frames with a soft dark rim: 2 px at 4K, the region's shadow hue, 45% alpha, drawn outside the sprite's alpha edge. A soft elliptical ground shadow under each grounded character, 0.9 u wide for Qori, fading to nothing by 2 u above ground. One extra texture sample at most; no per-pixel loops.
3. The check. New editor method ReadabilityCheck.Run (-saveFile, -captureDir): in each of the seven surface regions and four underground routes, at six fixed camera places with Qori standing on the walk line, capture and measure inside a box round Qori against a ring 3 u around him: mean luminance difference and edge-energy ratio (same method as section 4.1 of the review: Rec. 709 luminance, Pillow FIND_EDGES equivalent). Pass when the luminance difference is at least 12 points OR the background's edge energy is at most 60% of Qori's. Write a table.
4. A menu switch, Qolossal > Backgrounds > Readability on/off, so I can compare in Play.
Verify: run the check with the grade off (expect the surface to fail: the review measured 1.7 and 0.1 points in Qvale and the Terraces) and on; save before/after captures side by side in Tools/Design/Readability_20261116/; rerun ClimbCapture (0 faults). Tune the per-region rows until the check passes everywhere, then stop for my verdict on the look.
```

### A07. Guardian fights

```
Fixes G1-G5, K1. Do A05 first. Files: UndergroundGuardian.cs, the six boss scripts, UgArenaSeal.cs, CameraFollow.cs / CameraLockZone.cs, BossHealthBar.cs, layouts.py (arena frames as data), FeedbackTuning.cs.

1. G1 length. Guardians: Hits 15, hitsPerWindow 3. Mini-bosses: Hits 9, hitsPerWindow 3. exposedTime 2.6 so three hits are reachable with the combo. Put these in a small GuardianTuning asset per boss, not in code.
2. G2 third attack, from frames each boss already has (no new art; list which segment you reuse and at what timing):
   - Sluice Warden: "double sweep", the sweep to one side then the other 0.5 s later.
   - Root Matron: "seed row", seeds falling in a line that walks across the arena, one gap to stand in.
   - Tempest Owl: "low pass", a dive that skims the floor from one wall to the other; jump or drop through a deck.
   - Each mini-boss: one variant of its existing attack with different reach or timing.
   Phase 1 alternates two attacks; phase 2 uses all three in a fixed order; phase 3 is 1.25x speed with a random order that never repeats an attack three times.
3. G3 weak point. When it opens, pulse the weak point's frames and draw a thin ivory line toward it for 0.4 s. The staff shot counts as a hit (A04 step 4).
4. G4 entrance and frame. When UgArenaSeal shuts: Qori loses control for 1.5 s, the camera blends in 0.6 s to an arena frame stored per room in layouts.py (it must contain the boss's bounds, its weak point at every facing, and the whole fight floor; cap camera size at 7.5 and tell me any arena that does not fit), the boss plays Roar, and BossHealthBar appears on the roar, not before. Blend back when the fight ends or resets.
5. G5 death during a fight returns Qori to the rest point outside the arena (A01 step 3), and the bar hides.
Verify: UndergroundGuardianTest; UndergroundCombatBot on the six bosses with the win table and the median fight length per persona before and after (target: a skilled bot wins each in 45-75 s); a strip of each intro. For me to try: all six fights, with a pad.
```

### A08. Economy, heal, upgrades, reward mix, HUD ring

```
Fixes P3, H3, L7, R9, first-review N1/N2/N11. Two stages. STOP after stage 1 for my approval.

Stage 1 (desk, no Unity). Write Tools/Design/ECONOMY_TABLE.md:
- Income: every Amber source in order of first access, from mra_world.json (chamber rewards, amber finds) and platforming_v1.json (the 51 "reward" pieces, 1,130 Amber in total), with a running total at each region and route.
- Sinks, proposed: Sap vial (1 charge to start, given by Sorrel on the first visit to Qvale; +1 charge at 150 and at 300 Amber); Leaf Staff tiers at Brannick, each a real number in WeaponTuning: Reach (+0.2 u hit shape, 120), Quick charge (charge time 0.6 to 0.45, 180), Piercing shot (the shot passes through one target, 240); Heart seeds 60 each; map pages 40 each.
- Balance target: a player who clears everything affords it all by the Summit; a main-route-only player affords about half. Adjust prices to hit that and show the arithmetic.
- Reward mix: replace 20 of the 51 underground Amber rewards: 9 heart-seed shards (3 shards make a seed), 6 lore fragments, 5 map pages. Say which rooms and why (hardest rooms get shards).
Stage 2 (Unity, after my approval). Build it:
- Heal: hold Heal (RB / F) for 0.8 s on the ground to spend one vial charge and restore 2 hearts; a hit interrupts and the charge is not spent; charges refill at Waymarks, rest points and on death. Until Ludo clip L6 exists use the chargeStart frames as the stand-in pose.
- Shops in MraTownBuilder: remove the sword, mace and spear rows and every "Prototype" description; add the goods above with working effects. Rebuild the town scenes through the builder.
- HUD: the ring shows vial charges (pips round the ring) with the staff-shot cooldown as its fill; Amber count top right on the surface as well as underground, in UI_Counter_Amber_Frame.png.
- Save: new fields with migration; a save from today loads with 1 vial charge and its Amber intact. Add an old-save test.
Verify: Polish70Check additions; QvaleHousesCheck; a table of Amber held and goods affordable at each region for the two player types.
```

### A09. Surface gameplay pass

```
Fixes L1, L2, L3, P4. Needs my yes to decision D7. Two stages; STOP after stage 1.

Background, from the review: 70 of 77 surface edges are plain walking; gaps are authored at 3 u while Qori clears 6.1 u with one jump and 11.75 u with two; there are no hazards or moving platforms and 12 enemies.

Stage 1 (desk). Write Tools/Design/SURFACE_BEATS.md for MR01 Cradle and MR02 Causeway only. For each region list 6-8 beats placed on existing edges, each as: edge id, x range, what the player does, which verb it exercises, pieces used, the fail state (always A01's hazard rule), and its Teach / Test / Twist role. Rules:
- Use only piece kinds that already exist in UndergroundPlatformingBuilder (deck, crumble, mover, thorns, water, spill, anchor, pod, gust, shelter) and creatures that already have frames.
- Cradle: before the Leapseed, single-jump beats sized to 4.5-5.5 u gaps and 1.8-2.2 u rises (75-90% of one jump); after it, two double-jump beats (8-9.5 u gaps or 3.4-3.9 u rises); one moss wall after the Moss shrine with a safe floor under it.
- Causeway: Thread beats after its shrine: one safe swing by the shrine, one over water, one swing into a wall kick.
- Never block the main path with a beat that needs a relic the player may not have at that point; check each against Tools/Design/AbilityAccess/ABILITY_ACCESS.json.
- Leave at least 25 u of plain walking between beats, and keep every vista (VistaZone) clear.
- Creatures: raise Cradle to 4 and Causeway to 5, each guarding a jump or a ledge, never a required landing.
Give each beat a predicted difficulty 1-5 and show the curve per region.
Stage 2 (Unity, after approval). Add the beats as data to mra_world.json in a new "beats" list per edge and build them through MraWorldBuilder using the underground builder's piece code (share it; do not copy it). Art: use the region's existing terrain kit for decks; tell me which pieces have no surface art and are greybox. Run a reach check (the validate.py envelope) proving each beat is passable with the relics owned at that point and impassable hazards have a way round. Rebuild MR01 and MR02 only, with scene backups. Rerun ClimbCapture, AmbientLifeCheck and the ability access checker.
For me to try: both regions start to end, with a fresh save.
```

### A10. Underground variety

```
Fixes L4, L5, L6. Two stages; STOP after stage 1.

Stage 1 (desk). From platforming_v1.json write Tools/Design/UNDERGROUND_VARIETY.md:
- Every run of more than 4 consecutive ladder steps of the same rise (the review found 111 steps, median 3.5 u; A05 has 17, A10 13, A09 9). For each, a replacement sequence that mixes: single-jump steps of 2.0-2.3 u, a wall kick, one mover, one dash gap, and at most 4 steps of 3.4-3.6 u in a row. Keep the room's total climb and its doors.
- Deck density per room (decks per 100 u² of open room). List rooms under 1 per 80 u², with D05 first, and for each propose either added decks or a tighter camera frame.
- For each of the 31 rooms with creatures or bosses, whether the creature guards a jump or a floor. Propose moves so perched and flying creatures sit on ladder lines; no creature on a required landing.
- The 58 rooms with no creature: pick 12 where one existing creature design would add the most, by route order.
Stage 2 (Unity, after approval). Edit layouts.py only, regenerate platforming_v1.json, run validate.py (every door, reward and way back still connects), rebuild with scene backup, CheckBatch (0 problems), UndergroundFightAreaCheck, update_map.py then CaveMinimapCheck.
For me to try: A05 and A10 top to bottom, D05.
```

### A11. Camera zoom

```
Fixes K2, K4. Files: CameraFollow.cs, VistaZone.cs, UndergroundRoom / RegionalUnderground (wherever a room sets camera size), MovementTuning or a new CameraTuning asset.
1. List every place that changes Camera.orthographicSize in play and the size each underground room uses now. Give me the table.
2. Cap play zoom at maxPlaySize (7, tuning). Rooms that asked for more keep their wider view only inside look-outs (MraUgLook) and vistas.
3. While Qori is sprinting after a dash (A03 step 4), raise horizontalLookAhead from 1.9 to 3.2 u, eased over 0.3 s, and back when it ends.
Verify: CameraFeelPlayTest; a capture of the first Cradle cave room and D05 before and after; ClimbCapture unchanged on the surface.
```

### A12. Terrain and cave shader pass

```
Fixes V2, V3, V4, V6. No art bytes change. Files: the terrain fill material path (SurfaceTerrainContrast.cs, TerrainMaterial.cs, MraTerrain materials), UndergroundArtKit.cs, CaveDepth.cs and its shader.
1. Surface fill: darken with depth below the nearest walk edge to 70% value at 6 u; world-space value drift of ±6% and hue drift of ±2% at a 9-14 u scale from a small tiling noise texture; a 0.5 u soft contact shadow under the painted lip. One strength row per region.
2. Region caves: halve the world scale of the coarse rock tile (cracks are currently larger than Qori); tint and value-match it to the room's backdrop (sample the backdrop's mean colour at build time and store it per room); a soft inner shadow 0.6 u wide along every cut-out edge where the backdrop shows.
3. Route terrain: per-route tint so Route A's blocks sit in the Cistern's teal range (target: block mean hue within 20° of the wall behind, value 10-20 points above it).
4. Rooms without paintings (start with D05): a vertical value gradient on the far backdrop (20% lighter at the floor than at the ceiling) and offset alternate tiles by half a tile so the vertical seams break.
All behind Qolossal > Backgrounds > Terrain depth on/off. One extra texture sample at most.
Verify: before/after captures at six places per surface region and in the Cradle cave, A04, A08 and D05, saved to Tools/Design/TerrainDepth_20261116/; ReadabilityCheck (A06) still passes; MemoryBaseline unchanged. Stop for my verdict.
```

### A13. Route A dressing and the Qvale repeat

```
Fixes V5, V7. Kit pieces only; no new art.
1. Route A: every floating deck gets a bracket or corbel from the Route A kit under it (the rule: everything stands on something). In A04 move two or three lantern niches and one pier into the play band so the frame is never plain brick for more than one screen width. Do it in the builder data (layouts.py dressing or KeepersCisternBackdrop pieces), not by hand. Run the deck-edge and ledge sweeps so no new piece reads as a platform.
2. Qvale: the Mid plane shows its tree-and-village landmark twice in one 4K screen. Report the plane's tile width against the view width; fix by offsetting or mirroring alternate tiles in ParallaxLayer, or by raising the tile's world scale if it stays at or above 216 px/u.
Verify: captures of A04 at the four standard cameras and of Qvale at three; ClimbCapture MR04.
```

### A14. World effects back on

```
Fixes F5. Needs Codex Batch 55 accepted. Files: QoriFx.cs, QoriWalkFramesOnPlayer.cs, Fx library.
Stop disabling fx.movementEffects while frames show. Replace the old rig-era effects with pooled world-space sprites from Batch 55: landing dust (scaled by LastLandingSpeed, none under 4 u/s), skid dust on a turn above 5 u/s, dash start puff and three trailing streaks, wall-slide grit every 0.12 s, plunge impact ring, deflect spark (A05), heal motes. Each stays where it was spawned. Pool size 24, no allocation after warm-up. Per-surface tint from the region's ground colour.
Verify: AmbientLifeCheck (particle count still within budget); a strip of each effect. For me to try: run, turn, jump, dash, wall slide.
```

### A15. Animation system

```
Fixes F3, F4, E1, E2. Files: QoriWalkFramesOnPlayer.cs (rename), LudoFramesInstaller.cs, LudoQoriFrames.prefab data, UndergroundEnemy.cs.
1. Clip audit. Editor method QoriClipAudit.Run: render frame 0 and the tallest frame of each of the 29 clips at a common foot line with a 1.727 u height guide, into one sheet at Tools/Design/ClipAudit_20261116/. List clips whose standing-equivalent head height differs from the idle's by more than 4% (the review suspects sprintAttack 326.9 px, upAttack 349.1, doubleJump 368.2, wallJump 387.7). Fix figureHeightPx in data for those; do not touch frames.
2. Rates. Loops play at 12 or 15 fps only; one-shots keep their timing. List what changed.
3. Remove the walk clip from play (M8 uses the slowed run) and delete the unused slash, strike and codexWalk clips from the prefab data (keep the PNGs).
4. Garbage: cache the Player's renderers once instead of GetComponentsInChildren every LateUpdate; set the camera culling mask once; make All a cached array; in UndergroundEnemy.Seg replace the per-frame string array with a static one.
5. Rename the class to QoriFrames and the object to "Qori frames", update every reference, keep the prefab's serialized data (use FormerlySerializedAs / MovedFrom).
Verify: Polish70Check; a Profiler capture in a development build showing 0 B per frame from these scripts in A04 with three creatures.
```

### A16. Install new Ludo clips

```
Fixes F2, F6, F7. Run once per delivery, starting with L1 alone.
For each new clip in Tools/IncomingArt/Ludo/<name>/: cut with the existing LudoRun scripts (export_ludo_segments.py pattern), clean, register at the common foot line, import Tight, add to LudoQoriFrames.prefab, rebuild the Qori atlas (QoriFrameAtlasBuilder.Build), and add QoriFrameHitShapes rows for any attack frames.
L1 (bounding run) acceptance, measured not judged: strideCanvasPx * scale must be between 3.2 and 3.7 u per cycle; with cycleSeconds set so stride / cycle = 7.5 u/s and runPace 1.0, the logged "distance-locked" rate must be within 10% of the played rate. Then capture a 60 fps strip of the feet against the ground in A04 and in Qvale. If the stride is under 3.2 u, do not install it: tell me the measured stride and stop, so I can regenerate.
Hurt: plays for hurtHold; death: plays through DeathBeat, last frame held to the fade. Start and skid: start plays on the first 0.15 s from standstill; skid plays when input opposes velocity above 5 u/s. Look up and crouch: held after 0.4 s of Up or Down while standing. Rope swing replaces the hang loop while swinging faster than 2 u/s. Heal: plays over the 0.8 s hold.
Verify: Polish70Check; strips of each new clip in game beside the clip it replaces.
```

### A17. Interface wiring

```
Fixes U1-U4. Needs Codex Batches 53 and 54 accepted. Files: GameHud.cs, BossHealthBar.cs, MraRouteBar.cs, MraChart.cs / ConnectedAtlas.cs, MraState.Prompt and its callers, ControlsGuide.cs, GamePauseMenu.cs, UiSkin.
1. Boss bar, route bar and Amber counter use the Batch 53 frames; no rectangle or built-in font remains on the HUD. Safe area respected.
2. Chart: Batch 54 skin; pad-first controls (left stick pans, LT/RT zoom, A travels to the selected Waymark, B closes, LB/RB change region); remove the on-screen buttons and the mouse help line on a pad, keep mouse drag and wheel as extras on PC.
3. Prompts: one pooled UGUI prompt above the nearest interactable, with a glyph slot fed by the active device; remove OnGUI from every in-world prompt. Tell me how many of the 27 OnGUI scripts remain and which.
4. Controls page: out of the play view, into the pause menu.
5. Teaching lines, shown once each at a fixed place and saved: the up-swing at the first creature overhead, the plunge at the first pod, the charge at the first Bramble Creeper or Shellback, the shot at the first perched creature, drop-through at the first deck above a door, heal the first time hearts are at 2 or fewer with a charge held.
Verify: every screen captured at 1280x720, 1920x1080 and 3840x2160; the whole pause menu and Chart driven by a virtual pad in a test.
```

### A18. Guardian frames

```
Fixes F9, V8. Needs my yes (first review, decision 6). On G-03 only: upscale its frames 3x with a local illustration upscaler into a scratch folder outside Assets, clean the alpha edge, import Tight at the right px/unit, rebuild only its atlas, and capture old and new side by side at 4K in A08 at idle, warn and strike, with atlas memory before and after. Keep the originals and make the switch reversible. Separately, raise every guardian's and mini-boss's Idle and Exposed loops from 8 and 10 fps to 12. Stop for my verdict; do not process other creatures.
```

### A19. Engineering hygiene

```
Fixes E3, E4, E5.
1. GameSave: mark dirty on change; write at checkpoints, room and scene changes, pause, quit and at most every 20 s while dirty; keep the temp-and-backup swap. Test: 200 Amber pickups in 10 s cause at most one write.
2. Find...ByType: list all 163 call sites, mark each as once-per-scene, per-enemy or per-frame, and fix the per-frame and per-enemy ones with a PlayerRef static set in PlayerMovement.OnEnable.
3. Playtest builds: a build method that makes a non-development Windows build (no profiler port, so no firewall prompt) with an in-game debug toggle (F9/F10 behind a -debugKeys launch flag).
Verify: MemoryBaseline and worst-frame numbers before and after in all 8 scenes.
```

### A20. Demo slice

```
After phases 1 and 2. Build the demo: Cradle, Causeway and Route A, ending at the Sluice Warden's gate with a "to be continued" card. A fresh save only. Non-development build (A19). Write Tools/Design/DEMO_PLAY_SCRIPT.md: an ordered 45-minute route for me with a verdict box per stop, and a one-page sheet for outside players (what to do, what to tell us: where they died, where they were lost, what felt unfair, the minute they first smiled). Log deaths by room and time per room to a local file the player can send. No network calls.
```

---

## Codex prompts

### X1. Batch 51: Leapseed relic

```
Qolossal Batch 51: Leapseed relic art. Deliver to Codex_v2_full/Batch51/. Two files only.
1. Relic_Leapseed.png: the world pickup shown on its shrine. A plump twin-lobed seed that looks spring-loaded, like a maple samara folded shut, in the same painted style, line weight and top-left lighting as Assets/Art/Codex/Props/Relic_LivingThread.png and Relic_ClimbingMoss.png. Warm seed-brown and leaf green with one small ivory highlight. No mint, no painted glow, no face, no eyes, no text. Transparent background, straight alpha, nothing cut off, 24 px clear margin. Canvas exactly 1024 x 1280 px, painted at that size, no upscaling. It displays about 1.0 u tall.
2. Icon_Ability_DoubleJump.png: exactly 256 x 256 px, matching Icon_Ability_Dash.png and Icon_Ability_Glide.png in Assets/Art/Codex/Props/ (same frame treatment, stroke weight, palette): the seed with two stacked upward chevrons. Must read at 48 px.
Do not edit any existing file, scene, prefab or the accepted manifest. Include MANIFEST_51.json with sha256 and one contact sheet showing both beside the five existing relics and icons at equal scale. Stop for Claude review.
```

### X2. Batch 52: logo and key art

```
Qolossal Batch 52: title logo and key art, two stages. Stage 52a is concepts only; stop for approval before 52b. Deliver to Codex_v2_full/Batch52/.
The game: a small leaf-cloaked, long-eared creature, Qori, climbs a mountain that is secretly a sleeping titan. NEVER show or hint the titan: no faces, hands, ribs or body shapes in the rock. Style: the game's painted look (reference the accepted Terraces and Qvale 4K backgrounds), dusk-peach sky, pale stone, living green; mint only as tiny glints.
52a: three logo concepts for the word QOLOSSAL (hand-drawn lettering, readable at 230 px wide, on transparent, each 2560 x 1440) and three key art thumbnails (1920 x 1080): Qori small in the lower third, looking up a vast climb. In every thumbnail Qori must be at least 15 points lighter or darker than what is directly behind him.
52b, after approval, painted at size, no upscaling: KeyArt_Master.png 7680 x 4320 with the focal area inside the central 3840 x 1240 band and inside a central 1200 px wide column, so one painting crops to every size; Logo_Master.png 4096 px wide, transparent, plus a one-colour ivory version. Then export crops with the logo placed: 920x430, 462x174, 1232x706, 748x896, 600x900, 3840x1240 (no logo), 1280x720 logo only on transparent, 1438x810 soft background. These are Steam's sizes as I know them; list them in the manifest so they can be checked against Steamworks before upload. No scene or project edits.
```

### X3. Batch 53: HUD frames

```
Qolossal Batch 53: HUD pieces that are still rectangles. Deliver to Codex_v2_full/Batch53/. Match the existing HUD exactly: Assets/Art/Codex/UI/HUD_Ring.png, HUD_Vine_Segment.png, HUD_Health_Leaf_Full.png, UI_Counter_Amber_Frame.png (carved root-wood, small leaves, ivory and warm brown). No mint, no text, transparent PNG, straight alpha, painted at the stated size, no upscaling.
1. UI_BossBar_Frame.png 1920 x 120: horizontal root frame, inner window exactly 1760 x 40 px centred, 9-slice safe (ends 160 px each, plain repeatable middle).
2. UI_BossBar_Fill.png 1760 x 40: ember-red sap, tileable horizontally, with a 6 px lighter top edge.
3. UI_BossBar_Chip.png 1760 x 40: the same shape in pale ivory (the delayed damage chip).
4. UI_BossBar_Notch.png 16 x 56: phase divider.
5. UI_RouteBar_Frame.png 2400 x 160: frame for the region progress bar, inner window 2160 x 36 px, 9-slice safe, soft parchment backing at 60% alpha in place of the current grey box.
6. UI_Vial_Full.png and UI_Vial_Empty.png, 96 x 96 each: a small sap vial pip to sit round the HUD ring, readable at 32 px, clearly different in value so they read in greyscale.
7. UI_Amber_Icon.png 96 x 96: a cut amber drop with one highlight, readable at 32 px.
Include MANIFEST_53.json (sha256, inner-window coordinates for 1 and 5) and two mock-ups of all pieces placed: one over a bright Terraces screenshot, one over a dark cave. No scene, prefab or code edits. Stop for Claude review.
```

### X4. Batch 54: Chart and menu skin

```
Qolossal Batch 54: the Chart and menu skin. Two stages; 54a is one concept sheet, stop for approval. Deliver to Codex_v2_full/Batch54/.
The Chart is Qori's hand-drawn map of the climb. It currently looks like a developer panel (see Tools/Design/PolishGates/70/minimap/chart_MR04_half_explored.png). It must look like an object from the world: aged parchment in a root-wood frame, ink lines, small painted markers. NEVER draw the titan or any body shape; the map shows only what the player has found. No mint except the "you are here" mark.
54a: one sheet, 3840 x 2160, showing the Chart open with the map area, region tabs along the bottom, the legend and the travel prompt, and the pause menu beside it in the same material.
54b, after approval, transparent PNG, straight alpha, painted at size:
1. UI_Chart_Parchment.png 3200 x 1800, 9-slice safe (240 px borders), tileable paper centre.
2. UI_Chart_Frame.png 3360 x 1960, 9-slice safe, window 3200 x 1800.
3. UI_Chart_Tab.png and UI_Chart_Tab_Selected.png, 440 x 140 each.
4. Markers, 96 x 96 each: Qori, Waymark lit, Waymark unlit, rest point, cave mouth, shut gate, guardian, reward, relic shrine, unexplored exit.
5. UI_Chart_InkLine.png 256 x 16, tileable, for cave walls; UI_Chart_InkFill.png 256 x 256, tileable hatching for solid rock.
6. UI_Menu_Panel.png 1400 x 1600, 9-slice safe, same wood and parchment.
Include MANIFEST_54.json with sha256 and every 9-slice border, and a mock-up at 1920 x 1080 to prove it reads at half size. No scene, prefab or code edits. Stop for Claude review.
```

### X5. Batch 55: world effect sprites

```
Qolossal Batch 55: small effect sprite sheets, hand-painted in the game's style, to be left behind in the world as Qori moves. Deliver to Codex_v2_full/Batch55/. Transparent PNG, straight alpha, each sheet a single row of equal square cells, frame 1 at the left, no cell touching its border, painted at size, no upscaling, neutral warm-grey so the game can tint it. No mint. No hard outlines.
1. FX_LandDust.png: 8 cells of 256 x 256 (2048 x 256). A low puff spreading both ways from the centre bottom and fading.
2. FX_SkidDust.png: 6 cells of 256 x 256. A puff thrown one way (to the right), low.
3. FX_DashStreak.png: 5 cells of 384 x 128 (1920 x 128). A horizontal leaf-wind streak thinning out.
4. FX_WallGrit.png: 5 cells of 128 x 128. A few falling grains.
5. FX_PlungeRing.png: 8 cells of 384 x 192. A ground ring expanding from the centre bottom.
6. FX_DeflectSpark.png: 5 cells of 192 x 192. A pale ivory star-spark, sharp then gone.
7. FX_HealMotes.png: 8 cells of 256 x 256. Small rising sap-green motes (leaf green, not mint).
Display density 216 px per world unit, so a 256 px cell is about 1.2 u. Include MANIFEST_55.json with sha256, cell size and frame count per sheet, and one animated preview GIF per sheet at 12 fps on mid-grey. No scene or code edits. Stop for Claude review.
```

### X6. Batch 56: terrain breakup patches (send only if Claude's shader pass is not enough)

```
Qolossal Batch 56: breakup patches for large terrain faces. Send only after Claude reports that the terrain depth shader (A12) leaves visible repetition. Deliver to Codex_v2_full/Batch56/.
For each of Cradle, Causeway and Heights, 6 patches that sit ON TOP of the existing terrain fill to break its repetition: a root crossing, a seam of darker stone, a moss stain, a crack with shadow, a cluster of embedded stones, a weathered streak. Each must match that region's accepted terrain kit in palette, line weight and light direction exactly (reference the accepted terrain PNGs for that region), and must read as part of the rock face, never as a ledge: no flat lit tops, no horizontal edges longer than 0.5 u.
Each patch: transparent PNG, straight alpha, soft irregular edge that fades to nothing over at least 40 px, 1296 x 1296 px (6 x 6 u at 216 px/u), painted at size, no upscaling. File names Patch_<Region>_<01-06>.png. Heights patches must not use long vertical columns.
Include MANIFEST_56.json with sha256 and a mock-up of each region's six patches placed on a screenshot of its terrain. No scene or code edits. Stop for Claude review.
```

---

## Ludo.ai prompts

Settings for every clip: the same Qori reference image and settings as the accepted idle; side view facing right; full body; staff in hand; camera locked; character stays centred; 12 fps, 3 s. 9 credits each. Check the credit balance first: the handoff's arithmetic gives 691.

| # | Prompt | Becomes | Accept only if |
|---|---|---|---|
| L1 | `Qori running in place with long bounding leaps like a hare. Each bound is a big leap: both feet leave the ground, legs stretch far forward and far back, then tuck. Each bound covers twice his own body height in distance. Exactly four identical bounds in the clip, evenly spaced. Body leaning forward, ears and leaf cloak streaming straight back, staff held low in one hand. Focused eyes, face facing right, no mouth. Seamless loop.` | Run loop (one bound, 9 frames) | The front foot lands at least one body-height ahead of the hips and the back foot leaves at least one body-height behind. Claude measures the stride (A16); under 3.2 u is a redo. |
| L2 | `Qori is hit from the front: he flinches back one step with his eyes squeezed shut and his ears flat. Then his knees give way and he collapses onto his side on the ground and lies still with eyes closed. Face facing right, no mouth.` | Hurt (frames 1-8), death (9-36) | The flinch is complete by frame 8; the last 6 frames are still. |
| L3 | `Qori stands still, then pushes off hard into a run with one strong first step, leaning forward. Then he plants both feet and skids to a stop, leaning back, heels digging in, cloak swinging forward. Focused eyes, face facing right, no mouth.` | Run start, skid | The start and the skid are clearly separate halves. |
| L4 | `Qori stands still. He tilts his head back and looks straight up with wide curious eyes and holds the pose. Then he lowers into a low crouch, looks down at the ground and holds the pose. His feet never move. No mouth.` | Look up, crouch | Each hold lasts at least 8 frames. |
| L5 | `Qori swings on a vine rope, holding it with both hands above his head. His body swings forward and then back like a pendulum, legs trailing behind the motion, cloak following. Calm eyes, face facing right, no mouth. Seamless loop.` | Rope swing loop | One full forward-and-back swing that loops. |
| L6 | `Qori stands still, lifts a small glass vial to his chest with his free hand and holds it there while soft green motes rise around him, eyes closed and calm. Then he lowers the vial and opens his eyes. Face facing right, no mouth, feet never move.` | Heal (the 0.8 s hold and release) | The lift is done by frame 8 and the hold lasts at least 12 frames. |

Total 54 credits. Do not generate L2-L6 until L1 has passed A16 in game.
