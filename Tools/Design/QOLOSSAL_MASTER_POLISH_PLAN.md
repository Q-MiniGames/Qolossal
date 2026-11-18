# Qolossal: master polish plan (70% → 80% → 90% → 100%)

Written 3 November 2026 from a read-only audit of the repo at `fe83c03d`. Nothing was opened in Unity and no scene was edited. **Status: approved by the user on 2 Nov 2026, including every recommendation in section 6. Gate 70% is in progress; `Tools/Design/PolishGates/70/MORNING_TEST_LIST.md` has what's done and what's left.**

Numbers marked *measured* come from the project's files and logs. Numbers marked *estimate* are worked out and need a real measurement. Anything about feel needs the user's hands on it.

This plan sits on top of `POLISH_PLAN.md` (10 Oct, backlog IDs such as FEEL-03 and UI-02) and `Tools/Design/QOLOSSAL_ROADMAP.md`. Section 8 says what changed in those two.

---

## 1. How the gates work

A percentage here is a promise about what a player gets, not a share of tasks done.

| Gate | Promise | In one line |
|---|---|---|
| **70% Honest** | The game tells the truth. What you see is what hits, what the map says is what you can reach, and it plays start to finish without placeholders that mislead. | Fix what is wrong. |
| **80% Alive** | The game feels and looks as intended in every region and cave. Every enemy tuned by hand, Qori's clip set complete, the world moving. | Fix what is dull. |
| **90% Shippable on PC** | A stranger can install it on Windows, play it to the end with a pad or keyboard, and never need us. Title, settings, saves, remapping, stable 4K 60 on the target PC. | Fix what is missing. |
| **100% Released** | PC first, then each console on real hardware. No A or B bugs, certification passed. | Prove it. |

**A gate closes only when all three are true:**

1. Every exit check in section 4 for that gate passes, with the evidence saved in `Tools/Design/PolishGates/<gate>/`.
2. A Windows build of that gate is made and kept. Each gate leaves a playable build behind, so we can always compare.
3. You play it and say go. Scripted checks prove logic, never feel.

Then the gate gets a git tag (`polish-70` and so on) and we push on. Work may start on the next gate early only where it doesn't reopen a closed one.

Console items can't close without kits and hardware. So 100% splits into **100-PC** and **100-Console**, and no document will claim console readiness before a real build has run on the device (project rule).

---

## 2. What the audit found

The biggest findings, most important first. Each one changes the plan.

| # | Finding | Evidence | Why it matters |
|---|---|---|---|
| 1 | **Qori's hits come from an invisible staff.** Since the Ludo frames replaced the rig, the old rig still animates hidden underneath, and `PlayerCombat.LateUpdate` samples its weapon points for the opener, the backswing and the up attack. Only the contact moment is synced to the frames. | `PlayerCombat.cs:190-196`, `QoriWalkFramesOnPlayer.cs:6` | The old plan's "the drawn blade is the one that hits" is no longer true. This is the first thing to fix in combat. |
| 2 | **Qori's and the enemies' frames use several GiB of memory, and two of the three sets load in every scene.** Nearly all import as raw RGBA32: Qori's on purpose ("test only"), most others because their sides aren't multiples of 4. | *Measured 3 Nov in the editor, which counts about double a build*: Qori 2,863 MiB, surface enemies 2,193 MiB, underground 4,764 MiB. See `Tools/Design/PolishGates/70/`. `LudoFramesInstaller.Awake` loads Qori and the surface enemies always. | The largest risk to 4K, Series S and Switch 2, and it grows with every clip we add. Measure first, then fix with sprite atlases, which leave the source files untouched. |
| 3 | **Taking a hit doesn't show on Qori.** The hurt flash only reaches the hidden rig. There's no hurt clip, and death is an instant teleport. | `IsDamageFlashVisible` is read by `QoriAnimator` and `QoriVisual`, never by the frame player. `PlayerHealth.TakeDamage` calls `Respawn()` at once. | Players can't read that they were hit. Cheap to fix and a huge gain. |
| 4 | **Qori's feet skate.** The run clip's feet cover about 49% of the ground Qori actually crosses. The walk clip covers about 16%. | *Measured*: `Editor.log` says the walk would need 7.6 cycles/s and the run 4.4; they play 1.25 and 2.17. | Same cause as the failed Batch 27b walk. Try retiming before paying for new frames. |
| 5 | **Lesson caves can only be reached after the last relic.** The MR01, MR02 and MR03 cave networks (18 rooms, including Moss Pocket, Echo Vault and Grain Store) have one door each, and it needs the Glidecap from MR06. Fast travel exists from a lit Waymark in the Chart (`MraChart.TryTravel`), which softens the walk back (corrected 3 Nov). | `AtlasData.json` caves and passages. | A moss lesson you can only reach with every relic teaches nothing, even with Waymark travel. |
| 6 | **Three files disagree about what opens what.** `mra_world.json` says MR03_C05 needs the Wind Leaf, the atlas says that network needs the Glidecap, and `layouts.py` allows every skill. Leapseed appears in none of the requirement data. | Appendix A | The access map can't be trusted until there's one source. |
| 7 | **The world is almost still.** The 8 shipped scenes contain 0 particle systems, 1 flat global light each, 0 post-processing volumes, and no fire or foliage sway code at all. `AmbientMotes`, `WaterPool`, `WaterfallSplash`, `ScrollingStrip`, `MraWater` and `MraUgWater` exist but aren't placed directly in any of them. | *Measured* on scene files. Things inside prefabs or spawned at run time aren't counted, so the first ambient task is a look in play. | Backs your "bring the game to life" ask with a real baseline. |
| 8 | **The upper regions never got the new background method.** MR01 to MR04 have the 5-plane 4K stack. MR05 Ribwood still shows its old planes although the 4K set is imported. MR06 Heights has 3 old planes and no 4K art. MR07 Summit has 4 old planes and no 4K art. MR05, MR06 and MR07 all borrow the Terraces sky. | *Measured* per scene, Appendix C | This is your "upper backgrounds" problem, and part of it (Ribwood) is an easy swap. |
| 9 | **Guardians are about 4x under the 4K rule.** At camera size 5, 4K means 216 px/unit. Guardians are 50-73 px/u and mini-bosses 77-116. Regular creatures are 188-375. | *Measured* from `UndergroundCreatureFrames.asset` | The biggest things on screen are the blurriest. Needs new frames, not settings. |
| 10 | **All game sound has been off since 16 Oct.** Only music and Ludo's per-clip sounds play. | `Sfx.EffectsRemoved = true` | Hit feel is judged in silence until the new sound set arrives at 90%, so it gets one more review then. |
| 11 | **17 commits, including the whole folder, exist only on this PC.** | `git rev-list origin/main..HEAD` | One disk failure from losing it. Push before polish starts. |
| 12 | **The weapon menu is a small job, with one catch.** It is `QoriArmory`'s Tab menu plus an always-on hint box. Only the staff has frames. | `QoriArmory.cs:121-150`. Sentinels only take frontal hits from the Spear (`SentinelEnemy.cs:59`), so with the staff alone their front always blocks, except from above. | Removal also needs that Sentinel rule reviewed, and 3 test files updated. |

---

## 3. Workstreams

Twelve workstreams. Each one has a target for each gate in section 4.

### WS1. Remove the weapon menu

Remove: the Tab and right-stick menu, the on-screen hint, keys 1-4, the armory rows in `ControlsGuide`. Keep: the staff, its charged and aimed shot (Q / X). The hold-to-aim hint moves to the controls page. Right-stick click is freed for the minimap toggle.

Dormant Sword, Mace and Spear data stays compiled until 90% in case you want later weapons, then gets deleted. `ArmoryPlayTest`, `MraProductionProbe` and `QoriArmoryValidation` must change with it. **Sentinel:** decide how its guard is beaten with the staff alone. My recommendation: the charged sweep (`breaksArmor`) breaks it, same as the Shellback.

### WS2. Combat truth: hits, hitboxes, telegraphs

**The rule:** what the player sees is what hits, in both directions.

1. **See it first.** A developer overlay (one key, never in shipping builds) draws every live box: Qori's body, his attack shape, each enemy's body and strike, with the phase name. It is the tool for everything below, and it runs without any scene edits.
2. **Qori's attacks.** Replace the hidden-rig sampling with hit shapes authored per clip and per frame range, stored as data next to the clip. So when a clip is redrawn, only its data needs re-checking.
3. **Qori's body.** It is a 0.8 x 1.2 box on a figure about 1.73 u tall. Check head, feet, wall and ledge cases on the overlay. Generous is right for a player's hurtbox, but it needs to be a decision.
4. **Enemies.** Today one fixed box per creature is both the place you can hit and the place that hurts. For every enemy kind, underground and surface, compare it with the art frame by frame, and fix per-phase where the shape changes a lot. Strike zones get checked against the frame where the arm actually lands.
5. **Reactions.** Enemy hit-stun, which was left open on 1 Nov, plus damage direction: knockback currently comes from the hazard's position, which can push the wrong way on large bodies.
6. **Fairness table.** One row per attack: warning time, active time, recovery, reach, safe spot, punish window. Warnings today are 0.5-0.7 s on regular enemies, 0.8 on guardians, 1.1 on the Sluice Warden, shown by a small glint and the Warn frames. No bot has beaten the Sluice Warden yet, and one flood can cost up to 3 hearts.

**Pre-hit telegraph visuals: my recommendation.** Three layers, and only the first is everywhere:

| Layer | Used on | What it is |
|---|---|---|
| Body language | Everything | The Warn frames plus a stronger glint, a flash on the frame before the hit. Most small enemies should need only this. |
| Danger zone | Guardians, mini-bosses, and any wide or floor attack (sweeps, floods, shockwaves, dives, rising thorns) | A soft shape on the real strike zone that fills during the warning, flashes on impact and vanishes. It is drawn from the same data as the hit, so it can't lie. Ember and ivory, never mint, because mint means the titan is alive. |
| Assist option | Everyone, in Settings | Attack zones: Off / Big attacks (default) / All. Good for accessibility, and safe to leave in. |

Showing a box on every enemy would clutter the art and teach players to watch boxes instead of creatures, which is why it is not the default. **Test first:** one regular enemy and one guardian, in a build for you to compare, before rolling it out.

### WS3. Ability locations and access

The full map is in **Appendix A**. The plan: one source of truth (a generated `ABILITY_ACCESS.json`), a checker that also proves the negative (can't get through *without* the relic, by simulating every other relic), and the design fixes below.

The design rule for each relic is **Teach, Test, Twist**: a safe first use next to the shrine, a hazard test within the same region, then a combination later. Appendix A shows the current gaps per relic.

Issues to fix: lesson caves behind the Glidecap, making Waymark travel easy to find, Route A ignoring the four relics you already own, Bloomfall and Wind Leaf never needed on the main route, the atlas showing A, C, D, E as one network although grilles seal them, and three conflicting sources of data. Also check whether atlas requirements are actually enforced or only descriptive.

### WS4. Cave minimap

Good news: most of it exists. `AtlasData.json` already holds all 89 rooms with bounds and 629 underground outlines, and `UndergroundRoom.WorldBounds` tells us where Qori is.

- **Where:** in `Qolossal_Underground` only, top right, inside the safe area. The armory hint's old space.
- **Shows:** the current room and its neighbours as outlines, Qori's marker, doors, rest points, and shut gates. Later: rewards not yet taken and the boss.
- **Reveal:** rooms appear when first entered, saved per file. The full Chart shows the same discoveries. It must follow the map's existing spoiler rules for the Heart and Route E.
- **Controls:** toggle with right-stick click or a key, three sizes, hidden during cutscenes and the Heart reveal.
- **Build:** one mesh in a masked canvas, reusing `AtlasGraphic`, so it costs one draw call and stays sharp at 4K. No scene edits needed.
- **Keep in mind:** `update_map.py` must be rerun after any layout change. Add a check that fails if the map and the scene disagree.

### WS5. Ambient life

**Step one is a filmed census** of every region and route. It records what moves, because the file audit can't see prefab or run-time effects. Then a shared `AmbientKit` with one pooled particle system, one wind shader, one water shader and a budget per scene. Shaders and overlays leave accepted art bytes unchanged.

| Family | Target |
|---|---|
| **Water** | Surface shimmer and scroll, ripples and splashes when Qori lands, drips in caves, moving sheets for falls and spills, caustic light on cave walls. Hazard water must read as dangerous. |
| **Fire and light** | None exists today. Lantern and shrine flicker with small 2D lights, embers at the smithy and rest points, warm glow in Qvale at dusk. |
| **Wind** | A sway shader for grass, ferns, reeds, vines, banners and hanging chains, driven by one wind value per region. Leaf and dust streaks on gust lanes, in time with their pulse, so the gust's warning is seen and not only a colour change. |
| **Thorns** | The underground's 76 thorn hazards have no idle motion in their script. Slow idle creep, a shiver as Qori nears, sparks and a recoil when hit. Must stay readable as danger. |
| **Backgrounds** | Cloud drift on sky planes, distant birds, haze breathing, light shafts. Underground: drifting spores and dust in front of the backdrops, plus a slow depth layer, since caves currently have no parallax. |
| **Creatures and touch** | Butterflies and fireflies (`CradleFireflies` is currently in the underground only), birds that flush when Qori runs past, grass that parts, footprints and dust by surface. |
| **Signature idea (optional)** | "The breath": every 20-30 s dust falls, water trembles and lights dim very slightly, region-wide. Before the reveal it reads as weather; after it, the player realises what it was. It must never give the titan away early, so you decide after seeing it. |

Rule: nothing ambient affects gameplay, and every effect has a quality tier for Series S and Switch 2 handheld.

### WS6. Qori's animation

Qori is 32 frame clips on top of a hidden rig. **Your note about missing clips, corrected against the code:**

| Status | Clips |
|---|---|
| **Missing entirely** | Hurt, death and respawn, run start, skid and turn, look up, crouch or look down, shrine or knot touch, rope swing (only a hang loop exists) |
| **Exists, has flaws** | Run and walk (feet skate, section 2). Landing while running (the one-shot plays and may skate; check in play). Rise to fall (hard switch at 0.5 u/s, no apex). Mixed playback rates from 6 to 40 fps. Possible size jumps: each clip is scaled by its own measured figure height, from 327 to 580 px, so crouched poses can come out too big. Check with a clip-to-clip overlay. |
| **Exists, fine for now** | Down-stab: the plunge has its own three clips, so it isn't missing. Double jump, evade, dash, glide, wall slide and jump, ledge hang and climb, combo 1-3, up attack, sprint lunge, charge, staff shot |
| **Unused, remove** | `slash`, `strike` and `codexWalk` (63 frames, once the armory is gone) |

Order of attack, following the lesson from the failed walk test: **cheapest test in the game first.** Retime the run and judge it before ordering anything. Fix code-side flaws (hurt flash on frames, facing flip) before new clips. For new Ludo clips, give the required stride and speed in the prompt, and test one clip in game before the batch. As of 1 Nov about 800 Ludo credits remain, and that needs re-checking.

Then retire the hidden rig once WS2's hit shapes no longer need it. It costs CPU every frame and is a trap for future bugs. `QoriWalkFramesOnPlayer` still calls itself "Test-only" and needs a production name. This also closes `POLISH_PLAN.md`'s rig-based ANIM items.

### WS7. Backgrounds and parallax

Your "upper backgrounds" can mean two things, and the plan covers both:

- **Higher regions:** swap Ribwood's imported 4K stack in (`MraRegion4K` already has its entry), get Batches 38 and 39 painted for Heights and Summit, give each its own sky, then tune in play.
- **The upper part of the frame:** the top edge and far planes during a climb. `sinkLimit` holds far planes at 6 u; on tall climbs (Causeway 158 u, Terraces 146 u, Heights 216 u, Ribwood 180 u) check for planes leaving a gap, drifting too little or seams appearing. Use a scripted climb capture per region so each change can be compared.

Region table in **Appendix C**. The Descent's background is dropped (the user, 3 Nov): no scene of its own, and Route E carries that part. Art comes before vertical parallax (the user, 3 Nov), and the long columns style is not acceptable; `ClimbCapture` measures vertical speed per plane, for information. Also fix the mesh rebuild in `ParallaxLayer.Refresh`, which still allocates per layer per frame (PERF-01).

### WS8. 4K, memory and performance

**The rule:** camera size 5 shows 10 u, so 2160 / 10 = **216 px/u** at 4K. Any art at or above it is native 4K. Below it needs a recorded reason or new art. Densities are in **Appendix B**.

1. **Baseline first.** A new Windows build (the last is Review99, from before the current underground) and a Memory Profiler capture in the worst cases: Underground with its heaviest room, Qvale, and a guardian fight. This replaces the estimates in section 2.
2. **Frame pipeline.** Sprite Atlases per creature and per Qori clip family, loaded per room or region and released afterwards, not all at scene start. Atlases compress regardless of the sprite's own size and leave the source PNGs alone. `LudoFramesInstaller`'s `FindObjectsByType` twice a second also goes.
3. **Import profiles per platform** (`CodexArtImporter` has one cap today), as the platform direction asks. Series S and Switch 2 get derived resources through import or mip settings, never edited art, and never with invented format names.
4. **Quality levels.** Replace the six template levels with named real ones, with vsync and frame rate set per platform.
5. **UI at 4K.** 26 scripts still draw with IMGUI, and text is legacy Arial on a 1080p scaler. Handled in WS9.
6. **Budgets.** Working budgets get set from the baseline, not guessed now. Starting proposal to be confirmed: Qori's frames under 150 MiB, a room's creatures under 200 MiB. These are our own targets, not platform figures. Validate Series S and Switch 2 handheld and docked separately once kits exist.

No claim of 4K output on any console follows from this. It is an art and memory target until hardware says otherwise.

### WS9. Game shell, input and UI

Mostly the existing backlog, unchanged: boot and title (UI-01, SAVE-01), one input asset with remapping and glyphs (UI-08, UI-05, Q2 still needs your answer), scalable prompts replacing IMGUI (UI-02), a real font (UI-03), safe area (UI-07), full settings and accessibility (UI-04), no Quit on console (UI-06), save migration and platform adapter (SAVE-03). Also: a death beat (FEEL-03), a camera decision (FEEL-04, Q9), text ready for localisation, and a boss intro and win beat.

### WS10. Audio

All SFX have been off since your 16 Oct decision. **Your decision (3 Nov): a totally new sound set, requested at a later stage, so this work starts after 80%.** The old ElevenLabs library isn't brought back or auditioned, and nothing gets wired before then. Keep a running list of needed cues as the other workstreams finish. Order when it starts: hit and blocked-hit, hurt, death, jump, land, footsteps by surface, ambience beds to match WS5, boss cues, UI. Then re-review hit feel with sound on. Music: it stays held until you resume it, and the D-A-B-A motif and stem timing rules hold when you do. Eight-stem decode is profiled on hardware.

### WS11. Art completion and overall look

Guardian and mini-boss frames at density (Appendix B). Outer underground terrain repaint (the weakest art now that rooms have kits, per the 2 Nov handoff). Reclining Chart and reveal stills (Batches 18 and 21). Summit lake and reeds. Relic art for Leapseed (still a placeholder). A foreground depth layer for the regions other than Terraces. Lighting direction (Q4): per-region tint and a light vignette or stay flat. Also the open items from underground part 1: Heart plinth, E-19 and E-21 unplaced.

### WS12. Quality, release and platform

**Regression:** rerun `MraPlayTest` first. Its last recorded pass was about 13 Oct, which predates the scene consolidation to 8 build scenes, so expect failures from stale paths. One full-route bot in CI style.

**Playtests:** you at each gate; outsiders from 80%. Tracking: an A/B/C bug list in the repo. **Pacing and difficulty:** one full timed run per gate, heatmap of deaths.

**Release:** Steam page and Steamworks, cloud saves, achievements, credits, trailer, company and legal. **Check the AI-content rules early:** Steam requires a disclosure for AI-generated content, and the Ludo, Codex and ElevenLabs terms should be read for commercial use before the store page, not after. Each console's policies need the same check inside its portal.

**Consoles:** developer programmes (status needs confirming with you, they were step 2 of the 10 Oct roadmap), then SDKs, kits and certification. Unity stays at 6000.3.19f1 until each SDK is confirmed compatible.

---

## 4. Gate exit criteria

### Gate 70%: Honest

**Before it starts:** the current underground art thread closes, and the 17 commits are pushed (your OK needed).

| WS | Exit check |
|---|---|
| WS1 | The menu, hint and keys are gone, and no test or prompt mentions them. The staff is the only weapon in a fresh save. Sentinels are beatable. |
| WS2 | Overlay works in every scene. Qori's hits come from per-frame shapes, not the rig, for every staff move. Bodies and strike zones reviewed for every enemy kind, with a saved capture of each. Telegraph test approved by you. Fairness table filled in. Each guardian beaten by you with a real pad. |
| WS3 | Access map approved by you. One data source, and the old prerequisites retired. Checker proves each door in both directions. D3 and D4 decided. |
| WS4 | Minimap v1 in all 89 rooms: outlines, Qori, doors, rest points, reveal on entry, saved. Works on pad. Map-versus-scene check passes. |
| WS5 | Filmed census done. Ambient target list by region approved. Kit proven on one surface region and one route. |
| WS6 | Hurt flash on frames, a hurt beat and a death beat (retimed existing frames are fine here). Run retimed and judged by you. Facing flip without a moonwalk. Size jumps measured and fixed in data. |
| WS7 | Ribwood 4K stack in. Climb capture tool exists, with before captures of all 7 regions. Briefs written for Heights and Summit. |
| WS8 | Fresh Windows build and memory baseline. Frame atlas pipeline proven on Qori. Peak memory per scene written down. |
| WS12 | Regression tests repaired and green. Full route played start to ending by you. A bugs at 0 for the route. |

### Gate 80%: Alive

| WS | Exit check |
|---|---|
| WS2 | Every enemy and guardian hand-tuned, with hit-stun in. Danger zones on all big attacks. Assist option works. |
| WS3 | All Teach, Test, Twist rows filled. Waymark travel checked for every return trip. Lesson moments reachable when they teach. |
| WS4 | Rewards, boss and gate markers. Chart and minimap agree. Sizes and toggle remembered. |
| WS5 | All 7 regions and all 4 routes pass the ambient list on your review. Every thorn, water and gust cue readable. Budgets met. |
| WS6 | All "missing" clips in the game and approved in play. Feet don't visibly skate at pace. Hidden rig removed. |
| WS7 | Heights and Summit in 4K with own skies. All regions signed off on the climb capture and in play. Underground depth layer in. |
| WS8 | All frame sets on atlases, loaded and released by region. Stable 60 at 4K in a Windows build on your PC in the dense scenes, frame times saved. |
| WS11 | Guardians at density. Outer terrain repainted. Lighting decision applied. |
| WS12 | First outside players (3-5). Their findings triaged. Difficulty curve reviewed. |

### Gate 90%: Shippable on PC

| WS | Exit check |
|---|---|
| WS1, WS6 | Dormant weapon code and the legacy rig stack deleted. |
| WS8 | Named quality levels. Stable 60 on a *target-spec* PC (spec decided, not your i9), load times measured. Every 216 px/u exception recorded. No IMGUI left. |
| WS9 | Title, Continue, New Game, slots, settings, remapping, glyphs, safe area, accessibility and credits. Mouse-free play from boot to ending. Localisation-ready text. |
| WS10 | New sound set requested, delivered and approved by you. All cues in, hit feel re-reviewed with sound, music decision made and mixed. |
| WS11 | No placeholder art. Reclining Chart and stills in. |
| WS12 | 10+ outside playthroughs. A bugs 0, B bugs under 10. Saves survive a crash and migrate from older versions. Legal and AI-disclosure review done. Steam page draft ready. |

### Gate 100%: Released

| Part | Exit check |
|---|---|
| **100-PC** | A and B bugs 0. Steamworks, achievements and cloud saves tested. Final builds on target PC and Steam Deck if wanted. Store review passed. |
| **100-Console** | For each of PS5, Series X, **Series S**, **Switch 2 handheld**, **Switch 2 docked**: real build, measured frame time and peak memory, platform save, user and suspend behaviour tested, certification passed. Anything unavailable is logged **not tested**, never passed by inference. |

---

## 5. Threads, and the order to run them

**The limit that matters:** one Unity editor per checkout, and batch tools need Unity closed. Other threads and Codex share this folder. So it isn't scenes alone that collide, but Unity itself. My recommendation is **two lanes**, never more:

- **Unity lane:** one thread at a time holds the editor and the scenes.
- **Desk lane:** one thread at a time that needs no Unity: Codex and Ludo briefs, reviews of deliveries, Python tools, design docs.

Separate worktrees would allow more, but this project has 2 GB of art to reimport per worktree, so I'd only use one for a long isolated job, and only by starting that thread with its Worktree option on.

| Order | Thread | Lane | Touches | Waits for |
|---|---|---|---|---|
| 0 | Underground art, part 2 (running) | Unity | `Qolossal_Underground`, `layouts.py`, builder | your verdict |
| 1 | **Remove weapon menu** (WS1) | Unity, small | `QoriArmory`, tests, `Player.prefab` | 0 |
| 2 | **4K baseline and frame pipeline** (WS8) | Unity | Frame prefabs, importer | 1 |
| 3 | **Combat truth** (WS2) | Unity | `PlayerCombat`, creature scripts, frame data | 1, ideally 2 |
| 4 | **Ability access** (WS3) | Desk, then Unity | `mra_world.json`, `layouts.py`, surface scenes | 0 (shares `layouts.py`) |
| 5 | **Cave minimap** (WS4) | Unity | New UI code, save. No scene edits | 4's data, for correct doors |
| 6 | **Qori animation** (WS6) | Desk for the prompts, then Unity | `LudoQoriFrames.prefab`, frame player | 3 (shared hit data) |
| 7 | **Backgrounds and parallax** (WS7) | Desk for the briefs, then Unity | MR05, MR06, MR07 | 0 |
| 8 | **Ambient life** (WS5) | Unity | All scenes | 7, on the surface, and 0 underground |
| 9 | **Game shell and input** (WS9) | Unity | UI code, boot scene | 70% closed |
| 10 | **Art completion** (WS11) | Desk | Codex and Ludo briefs | runs alongside |
| 11 | **New sound set** (WS10) | Desk | `Sfx`, new library | 80% closed |
| 12 | **Release** (WS12) | Desk | | 90% |

**Why this order.** The menu goes first because it's small and every later combat test benefits. Memory goes early because it may change how all frames load, before we spend more credits. Combat comes before Qori's new clips since hit data is authored per clip. Parallax comes before ambient because both edit the same surface scenes, and ambient looks wrong over planes about to be replaced. Access comes before the minimap so the map shows correct doors from day one.

**Regression tests and gate reviews** stay with the coordinator, not a thread. Each thread updates `QOLOSSAL_SESSION_HANDOFF.md` and this plan's scorecard.

---

## 6. Decisions (all recommendations approved by the user, 2 Nov 2026)

| # | Decision | My recommendation |
|---|---|---|
| D1 | Removing the menu makes Qori **staff only**. Keep later weapons in the design or drop them? | Staff only. Depth through moves, relics and upgrades. All frames are drawn with the staff, so other weapons would need the whole clip set again. |
| D2 | Telegraphs: which level? | Big attacks by default, full option in Settings, after you see the test (WS2). |
| D3 | MR01-MR03 cave networks need the Glidecap. | Keep them as late return trips using the existing Waymark travel, and move the lessons to the pockets and shrine areas. Opening them earlier means re-carving terrain. |
| D4 | Bloomfall and Wind Leaf are never needed on the main route. | Add one required beat for each on the surface, by data, so no player misses them. Otherwise they're optional relics. |
| D5 | Sound: all SFX off since 16 Oct. When does it return? | **Decided 3 Nov:** a totally new sound set at a later stage, after 80%. |
| D6 | Minimap reveal: on entry, or all shown? | On entry. Exploration is part of the caves. |
| D7 | Qori walk and run: retime first, redraw only if that fails? | Yes. Costs nothing, and we learn the true target for any new clip. |
| D8 | Push the 17 local commits to GitHub once the underground art thread closes? | Yes, as soon as possible. Large files need checking against LFS first. |
| Old | Q2 controls, Q3 death cost, Q4 lighting, Q9 camera (`POLISH_PLAN.md` section 5) | Still open. Answer when their workstream starts. |

---

## 7. Risks

- **Credit and AI-frame drift.** New Qori clips may not match each other. Mitigation: test one clip in game first, and do cheap tests before batches.
- **Polish expanding forever.** Each gate has a fixed list. New ideas go in the next gate unless they're A bugs.
- **Regression from shared files.** `PlayerMovement`, `PlayerCombat` and `QoriWalkFramesOnPlayer` get touched by several workstreams, hence one Unity lane and the order above.
- **The map going stale.** `AtlasData.json` is patched by `update_map.py`, and the MainWorldLayout pipeline can overwrite it. WS4's check catches it.
- **Generated scenes.** Builder-made content is lost on rebuild when edited by hand. Fixes go in `layouts.py`, `MraRegion4K` or the builders.
- **Console access.** Kits take the longest and only you can apply. Every month of delay shortens the time 100-Console has.

---

## 8. What changes in the older plans

**`POLISH_PLAN.md`** stays as the backlog of detail. Batches 0 to 4 and the tuning assets are done. **Superseded:** ANIM-01 to ANIM-11, because they target the rig, which is no longer shown. Their ideas move to WS6 as clips. Hit feel from AI-02 and the staff retime from FEEL-07 are done. **Still open and carried over:** FEEL-03, FEEL-04, FEEL-06, PHYS-01 to 03, VIS-01, UI-01 to UI-10, PERF-01, 03, 04, 05, 06, ARCH-01 to 03, SAVE-01 to 05, BUILD-02 to 05.

**`QOLOSSAL_ROADMAP.md`** remains the release outline, but its "where the game stands" is old (it describes 8 regions plus many scenes; the build list is now 7 regions plus one underground scene, and its numbers predate the Ludo work). Rewrite it at 70%.

---

## Appendix A: ability access map

Built from `AtlasData.json`, `mra_world.json`, `layouts.py` and the scene shrines. **Doors are data; room interiors were read from layout notes, not played.**

### Order of relics

| # | Relic | Gives | Found | Have it by |
|---|---|---|---|---|
| 1 | **Leapseed** (name provisional) | Double jump | MR01 Cradle, shelter hut (44, 21) | MR01 |
| 2 | **Climbing Moss** | Wall slide and wall jump | MR01 N06, Moss shrine | MR01 |
| 3 | **Living Thread** | Grapple and swing, 4 u range | MR02 N06 | MR02 |
| 4 | **Bloomfall** | Downward strike and pod bounce | MR03 N06 | MR03 |
| - | *(MR04 Qvale: no relic; the town)* | | | |
| 5 | **Wind Leaf** | Dash and air dash | MR05 N06 | MR05 |
| 6 | **Glidecap** | Glide | MR06 N06 | MR06 |

Without any relic Qori still has jump, ledge grab, evade, sprint and attacks.

### What each area needs

| Area | You arrive with | Main-route gate | Doors you can open then | Doors that wait |
|---|---|---|---|---|
| **MR01 Cradle** | Nothing | Edge E08: `knot:grip` | PC01, PC02, PC03 | MR01 network: **Glidecap** |
| **MR02 Causeway** | Leapseed, Moss | Edge E07: **Living Thread** | PC04, PC05, PC06 | MR02 network: **Glidecap** |
| **MR03 Terraces** | + Thread | none | PC07, PC08, PC09 | MR03 network: **Glidecap** |
| **MR04 Qvale** | + Bloomfall | none | **Route A** | |
| **MR05 Ribwood** | same, Wind Leaf at N06 | none | MR05_C01-C05, PC10, **Route C** (Thread + Bloomfall) | MR05_C06: **Glidecap** |
| **MR06 Heights** | + Wind Leaf, Glidecap at N06 | Edge E06: **Glidecap** | MR06_C01-C06 (usable on the way back from N06), PC11, **Route D** (Moss + Wind Leaf + Glidecap) | |
| **MR07 Summit** | All | none | MR07_C01-C06, PC12 | **Route E**: only after the reveal |

### By relic: what it opens

| Relic | Cave doors that need it | Used inside rooms | Main route | Gap |
|---|---|---|---|---|
| **Leapseed** | None (PC01-03 need it only inside) | Everywhere; Route A | Cradle's first rise over 2 u at x 48.5 | Not in any requirement data |
| **Climbing Moss** | MR05_C03, MR07_C06, Route D | PC05, PC08, moss walls in C and E | Not a marked gate; climbs to confirm in WS3 | Confirm |
| **Living Thread** | MR05_C02, MR06_C02, MR07_C02, Route C | PC07, MR02 network, C, E | Causeway gate | OK |
| **Bloomfall** | MR05_C05, Route C | MR05_C04, MR03_C01, MR03_C06, PC10, C, E | **Never required** | Test comes late, 1 door only |
| **Wind Leaf** | MR06_C03, Route D | MR06_C01, PC11, PC12, D09 | **Never required** | Thin: found MR05, first needed MR06 |
| **Glidecap** | MR01_C06, MR02_C06, MR03_C06 (18 rooms), MR05_C06, MR06_C05, MR07_C04, Route D | D, E, MR07_C05 | Heights gate | Biggest key by far; the way back is Waymark travel |
| **Reveal flag** | Route E | | Summit ending | Intended |

### Teach, Test, Twist, as a proposal for your approval

| Relic | Teach (safe, near shrine) | Test (with risk) | Twist (later combo) |
|---|---|---|---|
| Leapseed | Cradle x 48.5 (exists), PC01 | PC02, PC03 | Route A: moving pumps |
| Moss | Beside the shrine, **to add** | PC05, PC08 | Route D chimneys; moss + Thread in Route C |
| Thread | Beside the shrine, **to add** | Causeway gate; PC07 | Route C chains; MR05_C02 |
| Bloomfall | **To add** in MR03, then a required beat (D4) | PC10, MR05_C04 | Route C pods; E05 |
| Wind Leaf | **To add** in MR05, then a required beat (D4) | MR06_C01, PC11 | D09 glide + dash; gust calms |
| Glidecap | Heights gate | MR06_C05, MR07_C04 | D03, D06; return trips in MR01-03 |

### Problems to fix

1. **Lesson caves behind the last relic** (Moss Pocket MR01_C02, Echo Vault MR02_C03, Grain Store MR03_C02). See D3.
2. **Return trips:** 18 rooms need a trip back from MR06. Waymark travel in the Chart (`MraChart.TryTravel`) covers it from a lit Waymark; check that each return has one and that players can find it.
3. **Route A teaches a single jump and double jump** to a player who has four relics by then. Worth a redesign or a good reason. It could use the tools the player already has.
4. **Conflicting data.** `mra_world.json` has old chamber prerequisites (for example MR01_C04 Thread, MR01_C05 Moss, MR02_C04 Thread, MR03_C05 Wind Leaf) that disagree with the atlas's single Glidecap door.
5. *(PC04 having no way in was a false alarm from an extractor bug, corrected 3 Nov.)*
6. **The map's network.** The atlas joins A, C, D, E and the Heart with passages, while grilles seal each sector. The Chart and minimap must not suggest a route that doesn't exist.
7. **Not yet proven:** whether a door's atlas requirement is enforced in the scene or only described, and whether dash or double jump can break into a Glidecap room. `validate.py` proves what's reachable with a skill set, not what's blocked without one. WS3's checker adds this.

---

## Appendix B: art density against the 216 px/u rule

*Measured from frame data. Qori's height of 1.73 u is derived from the logged cycle rates.*

| Art | px/u | Verdict |
|---|---|---|
| Terrain (240 and 360 PPU sets) | 240-360 | Pass. Remaining flagged pieces are listed in Review 100 |
| 4K background stacks MR01-MR05 | 216 | Pass |
| Heights and Summit old planes | 144 (measured 3 Nov) | **Fail**, Batches 38 and 39 |
| Qori: most clips | 224-335 | Pass |
| Qori: double jump 213, up attack 202, sprint attack 189 | 189-213 | Slightly under; scale data may also be wrong for these poses, so check before redrawing |
| Regular underground creatures | 188-375 | Mostly pass. E-12 188, E-18 189, E-20 194, E-14 202 slightly under |
| Mini-bosses M-01, M-02, M-03 | 77, 116, 96 | **Fail**, about 2-3x |
| Guardians G-03, G-04, G-05 | 50, 54, 73 | **Fail**, about 3-4x |
| Surface enemies | Not measured here | WS8 |

Guardian fix options, for WS11: re-generate with Ludo at a much larger canvas (if it allows; check before spending), or paint keyframes natively and accept fewer frames. Decide on one guardian first.

---

## Appendix C: parallax by region

*Measured from the scene files.*

| Region | Planes | Art | Climb range | Vertical setup | Action |
|---|---|---|---|---|---|
| MR01 Cradle | 5 | 4K | 28 u | Tuned (near 1, mid .35, far .5, cap 6) | Check in play |
| MR02 Causeway | 5 | 4K | 158 u | Tuned; near plane 34.9 u tall | Check top end |
| MR03 Terraces | 5 + 13 foreground | 4K | 146 u | Tuned, reference region | Reference |
| MR04 Qvale | 5 + vista | 4K | 22 u | Tuned | Check in play |
| MR05 Ribwood | 5 | 4K | 180 u | As the Terraces; own sky | Done 3 Nov, check in play |
| MR06 Heights | 3, no mid | **Old**, 144 px/u, no 4K art | 216 u | Gentle movement, art first (Far 3 u, Near 4 u); Batch 38 rejected 3 Nov, no long columns; Terraces sky | **Batch 38**, then tune |
| MR07 Summit | 4 | **Old**, 144 px/u, no 4K art | 154 u | Gentle movement, art first (Far 3 u, Near 4 u); Batch 38 rejected 3 Nov, no long columns; Terraces sky | **Batch 39**, then tune |
| Underground | 2 | Backdrops copy 0.3 and 0.12 of the camera, both axes (`CaveDepth`, 3 Nov) | - | - | Check in play; spores and dust in WS5 |

The default budget of 35 slows sinking a lot on long climbs: a near plane sinks only about 7 u across a whole region, against about 30 u in the tuned regions. That's likely why the upper regions feel flat.
