# Qolossal: polish review and plan (10 Oct 2026)

A review of the project as it is on disk on 10 Oct 2026 (branch `main`, the 10 Oct commits not yet pushed). It covers code, settings, assets and logs. Nothing was run in the editor for this review. Where something can only be judged in Play mode, it says so, and section 6 lists what to check.

This plan doesn't repeat the release milestones in `Tools/Design/QOLOSSAL_ROADMAP.md` or the art and audio queues in `QOLOSSAL_SESSION_HANDOFF.md`. It points to them where they overlap.

**Ground rules this plan keeps:**

- Accepted art stays byte-identical. Every art-related item here changes import settings, code or new rig poses, never accepted pixels.
- Qori v3 (`Assets/QoriRigTest/`) is parked. Nothing here touches it.
- The 53 silent sounds are the user's own picks. This plan lists them as a decision, not a fix.
- The MRA scenes are **generated**. `MraWorldBuilder`, `MraTownBuilder` and `MraDriftBuilder` rebuild them from `mra_world.json`. A hand edit to an MRA scene is lost on the next rebuild, so "editor work" on those scenes means changing the builder code and rebuilding. The same applies to Qori's Animator Controller and clips: `Editor/QoriRigBuilder.cs` and `Tools/QoriRigAuthoring/clips.py` regenerate them.

---

## 1. Snapshot

### 1.1 Tech stack

| | |
|---|---|
| Engine | Unity **6000.3.19f1** (`ProjectSettings/ProjectVersion.txt`) |
| Render pipeline | URP 17.3 with the **2D Renderer** (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`). HDR on, MSAA off, SRP Batcher on. Every quality level points at the one URP asset; the Graphics default pipeline slot is empty. |
| Input | **Input System 1.19** only (`activeInputHandler: 1`). Bindings are built in code; the template `InputSystem_Actions.inputactions` is unused. |
| Key packages | 2D Animation 13, 2D SpriteShape, Tilemap (+extras), PSD and Aseprite importers, Timeline, uGUI 2, Test Framework, Visual Scripting (unused) |
| Physics | Physics 2D, fixed step 0.02 s (50 Hz), gravity −9.81 with Qori's gravity scale 3 |
| Code | 248 scripts, about 30,000 lines, all in `Assembly-CSharp` (no assembly definitions) |
| Content | 60 scenes in the build list (59 enabled), 26 prefabs, 45 Qori clips, 1,291 PNGs, 544 SFX takes (86 in use), 12 music tracks |
| Targets | Windows PC, PS5, Xbox Series X\|S, Switch 2 (`Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`). No console module, SDK or build exists yet. |
| Measured performance | Windows development build at 1080p on an i9-11900K / RTX 3070 Ti: median 0.56–0.64 ms per frame, 0 frames over 16.7 ms, region loads 21–748 ms (`Assets/MountainReliefAtlas/LEVEL_POLISH_HANDOFF.md:169`). Consoles untested. |

### 1.2 The game as it exists in the project

Please confirm or correct this.

Qolossal is a side-on 2D action-platformer. You play Qori, a small leaf-cloaked creature, climbing a route of eight regions (MR01 Cradle to MR08 Descent) that is secretly the body of a reclining titan. The secret is only revealed on the Summit.

- **Movement** (`PlayerMovement`): walk at 7 u/s and run at 10.5 u/s while a Run button is held. Jumping has coyote time, a jump buffer and variable height (releasing Jump halves the rise). Qori can also grab and climb up ledges.
- **Relic abilities** (`Relics`), found at shrines: the Living Thread (a grapple and swing), Climbing Moss (wall slide and wall jump), Bloomfall (a downward strike that bounces), the Wind Leaf (a dash, with one air dash) and the Glidecap (a glide while Jump is held).
- **Combat** (`PlayerCombat`, `QoriArmoryFactory`): Qori starts with the Leaf Staff. The Leaf Sword (a 4-hit combo), Seedpod Mace, Thorn Spear and Resin Sling also exist. Every weapon has forward, up and (in the air) down attacks. Hits have hit-stop, camera shake and pad rumble.
- **Health:** 5 hearts, shown as leaves, plus heart seeds. Sap orbs from enemies heal. Death or a fall returns Qori to the last checkpoint with full hearts.
- **World:** the 8 regions, 38 side chambers, the town of Qvale (Amber currency, shops, NPCs, five enterable homes and a listening bench) and 4 optional drift levels.
  - Each region has a knot to wake, Waymarks (checkpoints), gates opened by saved flags, and residents to rescue in chambers.
  - The Chart (regional map) and a route bar at the top of the screen show progress.
  - The Summit holds the reveal (a camera look-back, then a pull-out), and MR08 holds the careful ending.
- **Enemies:** 12 placed instances across all regions: Grub, two Gust Moths, two Sentinels, two Shellbacks, two Thornwings, a Pod Spitter, a Newt, and the Knucklebramble guardian in MR05.
- **Audio:** the music crossfades per scene. Sound effects come from a library with variation, cooldowns and voice limits. The user's selection has left 53 sounds silent.
- **Menus:** the only menu is the pause menu: Resume, Chart, Hit pause on/off, Sound (four volumes), New Game, Quit. There is no title screen.

The build list still contains the older A0–A2 and R1 game (Portals, veins, `ChartScreen`, `TitanKnot`), and the first enabled scene is `A0_TestRoom`.

### 1.3 System map

```
Player.prefab (root scaled 0.8 x 1.2; BoxCollider2D 1x1; Rigidbody2D)
├─ PlayerMovement      sole writer of velocity; input built in code; adds QoriSounds
├─ PlayerThread        Living Thread (rope joint); reads PlayerMovement
├─ PlayerCombat        attack phases, combo, hit detection (CombatHitDetector)
│   ├─ PlayerCombatInput   attack button + aim
│   └─ PlayerCombatFeedback  sounds, hit-stop request, shake (CombatCameraShake), rumble
├─ PlayerHealth        hearts, contact damage, knockback, HitStop, respawn
├─ QoriVisual (inactive)  legacy procedural rig: QoriBodyRig + 16 helpers (~3,200 lines, dead)
└─ QoriRig (nested prefab, live)
    ├─ Animator  QoriRig.controller: 45 states, 0 transitions, no blend trees
    └─ QoriAnimator  picks the state from gameplay, CrossFadeInFixedTime; cloak/ear springs,
                     QoriFootGrounding (2-bone IK on slopes), glide arm, head swaps

Scene (generated by MraWorldBuilder from Resources/MRA/mra_world.json)
├─ MraSession (-2000)  binds the save, the town state and the Chart; blocks the descent before the reveal
├─ MraTerrain x N      builds terrain meshes and PolygonCollider2D at OnEnable
├─ ParallaxLayer x N   background depth layers; rebuilds its quad every LateUpdate
├─ Camera: CameraFollow + CameraBounds + CameraLockZone (+ CombatCameraShake added at run time)
├─ MraExit / MraChamberDoor / MraChamberReturn / MraArrival → AreaTransition (fade, async load)
├─ MraKnot, MraGate, MraVariant, MraLever, MraReward, MraResident, Waymark/Checkpoint, AbilityShrine
├─ MraRouteTracker → MraRouteBar (HUD);  MraChart (pause menu page)
└─ MraMusic → persistent MraMusicPlayer

Persistent / static
├─ GameSave (JSON in persistentDataPath, temp + backup) ← flags, counters, relics, checkpoints
├─ Sfx / SfxPlayer (24-voice pool, mixer buses) ← SfxLibrary.asset
├─ HitStop (timeScale 0.0001, real-time countdown)
├─ GameHud (UGUI 1920x1080 scaler), TownHud, GamePauseMenu (UGUI + IMGUI fallback)
└─ MraState.Prompt and ~25 OnGUI methods for in-world prompts
```

### 1.4 The biggest issues

1. **A fresh launch and New Game drop the player into a test room.** `A0_TestRoom` is the first enabled build scene, and New Game loads build index 0 (`Scripts/GamePauseMenu.cs:84`). There is no front end yet.
2. **Qori's locomotion animation is mostly one clip.** At normal speed (7 u/s) the Run clip plays, because `runAbove` is 2.4 (`Scripts/QoriRig/QoriAnimator.cs:35`). The Walk clip only flickers during the 40 ms of acceleration, which adds a crossfade at every start and stop. On top of that:
   - air poses are near-static loops with a hard switch at the apex;
   - landings don't play while moving;
   - turning has no skid;
   - every clip is pose-to-pose with no overlap.

   This is the main source of the stiffness.
3. **The jump and camera feel floaty and busy.** The jump arc is symmetric (no extra fall gravity, no apex hang), there's no air drag when the stick is released, and the camera follows every jump vertically. Death is an instant teleport with no beat.
4. **Input and UI aren't ready for gamepad-first platforms.**
   - Bindings are hard-coded in 3 scripts, and about 12 more poll `Keyboard.current` and `Gamepad.current` directly.
   - Attack is only on the mouse or LB, and keyboard players have no attack key.
   - In-world prompts are IMGUI with the built-in Arial font, and there are no glyphs, remapping or safe area.
5. **Two of everything.**
   - A disabled legacy Qori rig (~3,200 lines) is still referenced as a fallback.
   - The old A0–A2/R1 world systems still ship next to MRA.
   - The editor runs at the "Very Low" quality level, which halves every texture, so Play-mode reviews show half-resolution art.

---

## 2. Blockers (fix before polish makes sense)

| ID | Problem | Fix | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| BLK-01 | The first enabled build scene is `A0_TestRoom` (`OpeningLevel` is disabled). A launch with no save starts there. `GameSave.Continue` (`Scripts/Progress/GameSave.cs:231`) loads index 0 and then switches, a double load. **New Game** loads index 0 (`GamePauseMenu.cs:84`), so it also lands in the test room. | Until the title screen exists, put `MRAtlas_MR01_Cradle` at index 0 and untick A0–A2/R1 in the build (the roadmap's milestone 2 already plans to retire them). New Game should load MR01 by name, not index 0. Later, a boot scene with the title replaces this (UI-01). | High / S | Editor (Build Profiles scene list) + code | `ProjectSettings/EditorBuildSettings.asset`, `GamePauseMenu.cs`, `GameSave.cs` |
| BLK-02 | **46 MRA scenes have a missing script**: a MonoBehaviour whose GUID (`e3e045b4…`) no longer exists, labelled `MraSession`, on the same object as the real `MraSession`. Each scene load logs "The referenced script (Unknown) on this Behaviour is missing!" (92 times in the current `Editor.log`). | In the builders, call `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` on each generated root, then rebuild the scenes. Add a check to the polish test that fails on missing scripts. | Med / S | Code (builder) + rebuild | `MountainReliefAtlas/Editor/MraWorldBuilder.cs`, 46 scenes |
| BLK-03 | **The editor plays at quality level 0, "Very Low"**: `globalTextureMipmapLimit: 1` (every texture at half resolution), 1 skin weight, vsync off. Standalone builds default to "Ultra". Art and feel judged in Play mode don't match the build. | Set the editor's active quality level to the one the PC build uses (see PERF-06). A quality level is a named preset of rendering settings; the editor uses whichever is ticked in Project Settings > Quality. | High / S | Editor (Project Settings) | `ProjectSettings/QualitySettings.asset` |
| BLK-04 | Lock the current build before changing feel: the 10 Oct commits are unpushed and unplayed. The audio tests haven't been run since the 10 Oct SFX selection, and the full play test hasn't been run since the reclining route (handoff 4.1, 4.7). | Run `MraPlayTest.Run "-mraAudioOnly"` and the full play test on the scratch copy, then the user's play-through and "push" (roadmap step 1). | High / S–M | Tests + user | — |
| BLK-05 | `companyName: DefaultCompany`. On Windows, `Application.persistentDataPath` (where the save lives) includes the company name, so changing it after players have saves orphans them. | Set the final company/publisher name now (open question Q6). | Med / S | Editor (Player settings) | `ProjectSettings/ProjectSettings.asset` |

---

## 3. Polish backlog

Impact: H / M / L. Effort: S (under 2 hours), M (half a day to a day), L (several days). "Work" says whether it needs the editor, code only, or new authored rig poses.

### 3.1 Animation (Qori)

How it works today: `QoriAnimator` picks one of 45 states each frame and calls `CrossFadeInFixedTime`. The controller has no transitions and no blend trees. The clips are baked at 30 fps from `Tools/QoriRigAuthoring/clips.py` with smooth (Catmull-Rom) tangents. The gaits are procedural; everything else is keyposes joined by monotone cubics (`keyed()`). Springs on the cloak and ears add lag on top. The baking itself is clean; the stiffness comes from **state selection, missing states and pose-to-pose timing**.

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| ANIM-01 | **The Walk clip almost never shows.** `runAbove = 2.4` while Qori's normal speed is 7 and his run is 10.5, so normal movement plays Run. Walk (authored for 1.3 u/s, playback clamped 0.6–1.9×) appears only for the ~37 ms of acceleration (65 u/s² from 0 to 2.4). Each start and stop is then Idle → Walk → Run, two crossfades in three frames. | Replace the Walk and Run states with one **1D blend tree** "Locomotion" on a `Speed` parameter. A blend tree mixes clips by a parameter instead of switching between them, and keeps their cycles in step. Suggested thresholds: Walk at 2.5 (re-time the walk so 0.62 s matches about 2.5 u/s, or raise `walkClipSpeed`), Run at 7, Run at 10.5 with ×1.45 speed (or a new Sprint clip). Idle → Locomotion stays a code crossfade of 0.15 s. Do it in `QoriRigBuilder` so a rebuild keeps it. | **H / M** | Code (builder) + rebuild | `Scripts/QoriRig/QoriAnimator.cs:33-37,242-252`, `Editor/QoriRigBuilder.cs` |
| ANIM-02 | Every state change restarts the clip at phase 0 (`CrossFadeInFixedTime(state, blend, 0, 0f)`, `QoriAnimator.cs:259`), so a gait that is re-entered pops the feet. | With ANIM-01 most of this disappears. For the remaining gait-to-gait switches, pass the current normalized time so the cycle continues. | M / S | Code | `QoriAnimator.cs:257-262` |
| ANIM-03 | **The air poses are held and snap at the apex.** Rise and Fall are looping clips that barely move (keys differ by 2–8 rig units). The switch happens at vy = 0.6 with a 0.1 s blend (`QoriAnimator.cs:232`). | A 1D blend tree on vertical speed: Rise (+8), a new **Apex** pose (knees tucked, cape floating) at 0, Fall (−8), and a new **FastFall** (cape streaming, arms up) at −18. Feed it with `SetFloat(id, vy, 0.06f, dt)` (the damping argument smooths the parameter over 0.06 s). Author Apex and FastFall in `clips.py` from the existing rise/fall keys. | **H / M** | Rig poses + code (builder) | `Tools/QoriRigAuthoring/clips.py`, `QoriAnimator.cs`, `QoriRigBuilder.cs` |
| ANIM-04 | **No landing while moving.** Land plays only if the landing speed is ≥ 4 **and** the horizontal speed is < 2.4 (`QoriAnimator.cs:239-241`). At walking speed (7) every landing goes straight into Run. | Add a second Animator layer, **additive**, with a 0.15–0.2 s landing compression (body down, knees bent, cape kick). An additive layer adds its motion on top of whatever the base layer plays. Weight it from code by `LastLandingSpeed` (0 below 4 u/s, 1 at 16 u/s). It needs Write Defaults off on that layer. Keep the full Land state for standing landings. | **H / M** | Rig poses + code (builder) | `clips.py`, `QoriAnimator.cs`, `QoriRigBuilder.cs` |
| ANIM-05 | **No turn or skid.** Facing flips the moment the input reverses (`PlayerMovement.cs:338-339`), while the velocity takes 0.1–0.2 s to reverse, so Qori runs backwards ("moonwalks") for several frames. | When the input opposes the velocity and \|vx\| > 3, play a short **Skid** (0.12–0.15 s: lean back, feet planted, dust from `QoriFx`), and flip the facing when vx crosses 0. Keep the instant flip from standstill. | **H / M** | Rig poses + code | `PlayerMovement.cs`, `QoriAnimator.UpdateFacing`, `clips.py`, `Effects/QoriFx.cs` |
| ANIM-06 | The playback speed is set without smoothing, from `velocity.x` only (`QoriAnimator.cs:246,251`). On a slope Qori moves along the surface faster than \|vx\| says (vx is held constant and vy follows the slope), so the cycle runs too slowly for the ground he covers: foot sliding uphill and downhill. `ObservedVelocity` updates at 50 Hz but is read every render frame. | Use the speed along the path (`ObservedVelocity.magnitude` while grounded) and the damped `SetFloat(id, v, 0.08f, dt)`. | M / S | Code | `QoriAnimator.cs` |
| ANIM-07 | **Pose-to-pose timing.** In `keyed()` every control reaches its key at the same moment, so the head, cloak, skirt and arms start and stop together, which reads stiff. The springs only add lag on the cloak and ears. | Add per-channel delays to `keyed()`. Sample follower channels at `t - lag`: head 0.03 s, skirt 0.04, `cape_u` 0.05, `cape_l` 0.08, the free hand 0.03. For attacks, add 4–8° overshoot on the weapon angle in the first 0.06 s of recovery. Then re-bake and rebuild. | **H / M** | Rig poses (Python) + rebuild | `Tools/QoriRigAuthoring/clips.py`, `anim.py` |
| ANIM-08 | **The glide has no pose.** Gliding plays Idle with the near arm raised procedurally (`QoriAnimator.cs:229,316-336`), so Qori stands straight under the canopy. | Author a Glide loop: legs trailing back, cape lifted, a slight sway, about 1.2 s. Keep the procedural arm raise or bake it in. | M / M | Rig poses + builder | `clips.py`, `QoriAnimator.cs` |
| ANIM-09 | **No hurt or death poses.** A hit only swaps the face and flashes alpha. Death has no animation at all (`PlayerHealth.cs:55-59`). | A 0.25 s Hurt recoil, played during `hitRecoveryUntil`, and a Death beat (see FEEL-03). | M / M | Rig poses + code | `clips.py`, `QoriAnimator.cs`, `PlayerHealth.cs` |
| ANIM-10 | **No dash pose.** The dash keeps the current state and leans 16° (`QoriAnimator.cs:506`). | A 0.16 s Dash clip: stretched, legs tucked, cape trailing. | M / S–M | Rig poses + builder | `clips.py`, `QoriAnimator.cs` |
| ANIM-11 | The controller has Write Defaults on for all 45 states and is driven only from code. That's fine now, but it blocks additive layers. | Write Defaults off for every new layer (ANIM-04), and document that the controller is generated. | L / S | Builder | `QoriRigBuilder.cs` |

What is already good and should stay: the clips scrub with `AttackTime`, so the drawn blade is the one that hits; `QoriFootGrounding` does two-bone IK on slopes; the cloak and ear springs work on top of the clips without accumulating error; the blink and expression swaps; the Animator never applies root motion, so it can't fight the physics.

### 3.2 Game feel

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| FEEL-01 | **The jump arc is symmetric.** Gravity scale 3 and jump speed 12 give an apex of 2.45 u after 0.41 s, and the fall uses the same gravity. Releasing Jump halves vy in one step (`PlayerMovement.cs:612-616`). | Add `fallGravityMultiplier` (1.7) when vy < 0, `apexGravityMultiplier` (0.55) while \|vy\| < 1.5 with Jump held, and a softer cut (`jumpReleaseMultiplier` 0.5 → 0.6, applied once). Re-tune `jumpSpeed` to keep the apex near 2.4 u (~12.5). Keep `maximumFallSpeed` at 22. | **H / S** | Code | `Scripts/PlayerMovement.cs` |
| FEEL-02 | **No air drag.** In the air, letting go of the stick keeps vx (`PlayerMovement.cs:528-536`), so Qori drifts on. | Add `airDeceleration` (about 25 u/s²) when the input is 0, skipped during wall-jump push, flower launches and knockback. | **H / S** | Code | `PlayerMovement.cs` |
| FEEL-03 | **Death and respawn have no beat.** On the last heart, `movement.Respawn()` teleports at once with full hearts. There's no hit-stop on death, `Qori_Death` is silent, and the camera snaps. Falls into water (`MraWater`) work the same way. | A 0.8 s death sequence: HitStop 0.15, collapse pose (ANIM-09), fade to black 0.25 s (reuse the `AreaTransition` curtain), place, fade in 0.25 s, 1 s of invulnerability (already there). | **H / M** | Code | `PlayerHealth.cs`, `PlayerMovement.Respawn`, `Progress/AreaTransition.cs` |
| FEEL-04 | **The camera follows every jump vertically.** `CameraFollow` smooths towards the target's y every frame (smoothTime 0.18), so ordinary jumps bob the view. | Platform snapping: the vertical target is the last grounded height. Follow y only when Qori leaves a band (±35% of the half-height), falls past the ground he left, or is on a thread or wall. Keep the existing fall look-ahead and room bounds. | **H / M** | Code | `Scripts/CameraFollow.cs` |
| FEEL-05 | **The shake is tiny and always the same.** It's clamped to 0.08 u, lasts a fixed 0.09 s, and is a sine at 150 rad/s (`CombatCameraShake.cs`). Guardian slams feel like sword hits. | A trauma-based shake: `AddTrauma(0..1)`, offset = trauma² × max (0.25 u) × Perlin noise, decay 1.6/s, plus an optional kick along the hit direction. Add a Screen shake setting (0–100%) for accessibility. | M / S–M | Code | `CombatCameraShake.cs`, `PlayerCombatFeedback.cs`, `PlayerHealth.cs:65`, `KnucklebrambleGuardian.cs:95,131` |
| FEEL-06 | **Rumble only on hits** (`PlayerCombatFeedback.cs:56`). Nothing for taking damage, a hard landing, the guardian's slam, a knot waking or the dash. | A small `Rumble` service (low/high motor, duration, curve) with a settings toggle. Fire it on hurt (0.4/0.2 for 0.12 s), hard landings over 14 u/s (0.2 for 0.06 s), slams and knot stirs. Console haptics (DualSense) need the platform SDKs later. | M / S | Code | new `Scripts/Feedback/Rumble.cs`, callers |
| FEEL-07 | **The starting weapon is slow.** The Leaf Staff has 0.20 s startup, 0.20 s active and 0.26 s recovery, with no combo (`QoriArmoryFactory.cs:22`, `followUps` cleared). `AttackTime` maps each phase linearly (`QoriAnimator.cs:479-490`), so there's no snap at contact. | Staff: startup 0.09, active 0.10, recovery 0.20, plus a 2-hit follow-up. Map the active phase with an ease-out (p^0.6) so the blade whips through contact. Decide the targets in Play mode (open question Q10). | **H / M** | Code (data) | `QoriArmoryFactory.cs`, `QoriAnimator.AttackTime` |
| FEEL-08 | The fixed timestep is 0.02 s (50 Hz) against a 60 FPS target, which adds up to 20 ms of input delay and a 50/60 beat that interpolation hides only partly. | Set Fixed Timestep to 0.0166667 (Project Settings > Time). `LaunchGraceSteps` (8 steps) becomes 0.133 s instead of 0.16 s, so re-check the slope launches with `SlopeMovementCheck`. | M / S | Editor | `ProjectSettings/TimeManager.asset`, `PlayerMovement.cs:146` |
| FEEL-09 | **Running is L3-hold on a gamepad** (`PlayerMovement.cs:238`), which is tiring over a long climb. | See open question Q2. Options: full stick tilt runs, a run toggle, or always run with a walk modifier. | M / S | Code | `PlayerMovement.cs` |

### 3.3 Physics and collision

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| PHYS-01 | **The Player root is scaled non-uniformly** (0.8, 1.2) to shape a 1×1 BoxCollider2D. Every visual child has to undo it: `QoriAnimator.CancelParentScale`, and the QoriRig child is overridden to 1.25 × 0.833. | Set the root scale to 1 and the collider size to (0.8, 1.2) (offset adjusted). Remove `CancelParentScale` and the child override. Re-run the polish test (the ledge and slope checks). | M / M | Editor (prefab) + code | `Prefabs/Player.prefab`, `QoriAnimator.cs:165-173` |
| PHYS-02 | **Square collider corners on polygon terrain.** A box can catch on PolygonCollider2D seams and step lips. The frictionless material and the 0.3 u ground snap hide most of it. | Try a `CapsuleCollider2D` (vertical, 0.8 × 1.2), or keep the box with `edgeRadius` 0.05 and the size shrunk by the same amount. Compare in `SlopeMovementCheck` and the Qvale steps before choosing. | M / M | Editor + tests | `Player.prefab`, `PlayerMovement.cs` (it requires a BoxCollider2D, line 6) |
| PHYS-03 | **Only one custom layer ("Ground").** Player, enemies, hazards, pickups and triggers are all on Default, with every pair of layers colliding and `queriesHitTriggers: 1`. | Add the layers Player, Enemy, Hazard, Trigger and Projectile and prune the collision matrix. The matrix is the table in Project Settings > Physics 2D that says which layers can touch, and pruning it cuts contact pairs and accidental pushes. Set the layers in the builders. | M / M | Editor + builder code | `ProjectSettings/TagManager.asset`, `Physics2DSettings.asset`, builders |
| PHYS-04 | `LayerMask.GetMask("Ground")` is called inside per-frame code in 23 places, for example `EnemyBase.CanSee` and Thornwing's `FixedUpdate`. Each call allocates. | Cache it in `static readonly int`. | L / S | Code | `Creatures/*.cs`, `GroundCreature.cs`, `FallingRock.cs` |
| PHYS-05 | `Respawn()` always restores full hearts, so a fall into the void or deep water is a free heal. `resetBelowY = -15` is a single global value, but the MRA regions sit far higher, so in practice only water and the drift levels catch falls. | Decide the fall and death rules (Q3). If falls should cost a heart, add `TakeDamage` before respawning for falls. | M / S | Code | `PlayerMovement.cs:740-747`, `MraWater.cs`, `MraDriftRest.cs` |

What's right already: input is read in `Update` and applied in `FixedUpdate`; interpolation and continuous collision are forced on in `Awake` (the prefab says None, the code overrides it); the ground cast, slope following and platform riding are careful work.

### 3.4 AI and other creatures

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| AI-01 | **Only 12 enemies on the whole route** (1 to 2 per region, MR04 and MR08 have none). LEVEL_POLISH_HANDOFF §5 notes that combat on the shelves hasn't been played by a human. | A design decision (Q8). If more are wanted, add encounter slots to `mra_world.json` rather than placing them by hand. | M / M | Data + builder | `mra_world.json`, `MraWorldBuilder.cs` |
| AI-02 | **Hits and deaths are thin.** A hit only flashes. On death the enemy turns grey, stops colliding and vanishes after 0.25 s (`EnemyBase.cs:56-71`). | On a hit: a 0.1 s recoil (the rig body pushed 0.15 u along the hit direction, a squash, a small knockback for the light enemies). On death: a 0.4 s pop (scale 1.1 → 0) with the death puff and a sap orb arc. | M / M | Code | `Creatures/EnemyBase.cs`, `CreatureRig.cs` |
| AI-03 | The state machines are readable (Thornwing: Hover → Telegraph 0.55 s + glint → Dive → Return). They should be checked by hand for fairness. | Play-mode check only (section 6). | L / S | Play test | `Creatures/*.cs` |
| AI-04 | Each creature finds the player with `FindFirstObjectByType` (cached after the first call). | A static `PlayerRef.Current`, set by `PlayerMovement.OnEnable`. | L / S | Code | `EnemyBase.cs`, `GroundCreature.cs`, `SapOrb.cs`, Mra* scripts |

### 3.5 Visual polish

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| VIS-01 | **The lighting is flat.** Each MRA scene has one global Light2D (white, intensity 1) and no post-processing Volume, and the camera's post-processing is off. Lit sprites pay for a lighting pass that changes nothing. | Decide the direction (Q4). The cheapest step that keeps the approved art: a per-region global light tint and, if wanted, a URP Volume with Color Adjustments and a light Vignette. Local lights could go on knots and lanterns. Otherwise switch to Sprite-Unlit and save the pass. | M / M | Editor + builder | `MraWorldBuilder.cs`, `Settings/Renderer2D.asset` |
| VIS-02 | **Mixed shaders.** 14 scripts create materials at run time from `Shader.Find("Sprites/Default")` (unlit): the thread, slash trail, resin shot and others. Terrain and props use Sprite-Lit. The effects won't match if lighting is added, and `Shader.Find` depends on the shader being in the build. | Two shared material assets (Lit and Unlit) referenced from one ScriptableObject in Resources. | L / S | Code + 2 assets | `Environment/TerrainMaterial.cs`, `PlayerThread.cs:103`, `QoriSlashTrail.cs`, `QoriResinShot.cs`, others |
| VIS-03 | **Backgrounds have no mipmaps** (640 of the 1,291 PNGs, the backgrounds among them, import with `enableMipMap: 0`). When the camera pulls out (the reveal's look-back size 22, vistas), they shimmer. The handoff lists this under Option A. | Turn on mipmaps for the parallax layers and stills that are ever shown below native size. This is an import setting, so the source pixels stay byte-identical. | M / S | Editor (importer rule) | `Editor/ArtImport/CodexArtImporter.cs` (`entry.mipmaps`), the manifest |
| VIS-04 | Known art gaps (the Summit lake and reeds terrain, the Chart and stills for the reclining pose, the drift-level art, glyphs). | Already tracked in the roadmap and handoff. Not repeated here. | — | — | — |

### 3.6 Audio

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| AUD-01 | **53 sounds are silent** after the user's selection (`Tools/Audio/sfx_selection_2026-10-10.json`). They include core feedback: jump, footsteps, hurt, death, respawn, ledge grab, thread attach and release, the **Staff swing** (the starting weapon), pause and unpause, and every ambience bed. The game reads quieter than its art. | The user's call (Q5). Suggested regeneration order if wanted: Staff swing, jump, hurt, death, footstep, ambience beds. | **H / —** | User | — |
| AUD-02 | `PlayerCombatFeedback` makes its own AudioSource with no mixer group (`PlayerCombatFeedback.cs:10`), so an attack's `startSound`/`hitSound` would skip the Effects volume and pause. It's latent, because all those fields are null today. | Route it through `Sfx` or give it the Effects group. | L / S | Code | `PlayerCombatFeedback.cs` |
| AUD-03 | **Volume buttons wrap.** Each press lowers by 20%, and from 0% it jumps back to **100%** (`SfxSettings.Step`, `Audio/Sfx.cs`). | Left and right to adjust in 10% steps without wrapping (a slider on PC). | M / S | Code | `Sfx.cs`, `GamePauseMenu.cs:180-190` |
| AUD-04 | Test diagnostics run in shipping builds: every sound formats a history string (`Sfx.cs:254`) and updates count dictionaries. | Wrap them in `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`. | L / S | Code | `Sfx.cs` |
| AUD-05 | The music is streamed Vorbis (12 WAV sources). The platform direction asks for eight-stream decode profiling on hardware. | Not testable yet. Mark it "not tested" in QA. | — | — | — |

### 3.7 UI and UX

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| UI-01 | No title screen, no Continue/New Game choice, no save slots. | Roadmap milestone 3. Needs BLK-01 first. | H / L | Code + scene | new boot scene |
| UI-02 | **In-world prompts are IMGUI.** About 25 `OnGUI` methods (doors, exits, levers, residents, Waymarks, Lore Stones, shrines, NPCs, the listening bench, portals, heart seeds). `MraState.Prompt` creates two `GUIStyle`s per call (`MraState.cs:60-62`), and OnGUI runs several times a frame. The prompts don't scale with the 1920×1080 canvas, can't show glyph art and allocate every frame. | One pooled UGUI `PromptView` (a world-to-screen label with a glyph slot) shown for the nearest interactable. Interactables expose `PromptText` and `PromptAnchor` instead of drawing. | **H / M–L** | Code | `MraState.cs`, every OnGUI caller |
| UI-03 | **Every label uses the built-in `LegacyRuntime.ttf`** (Arial; `UiSkin.cs:49`) through legacy `Text`. It has no character, its glyph coverage is limited (the handoff wants ▲ and symbols), and it's soft at 4K and in handheld mode. | TextMeshPro with one shipping font (an SDF atlas), plus a fallback font for localisation. | M / M | Code + font asset | `UI/UiSkin.cs`, `Town/TownUi.cs`, `GamePauseMenu.cs` |
| UI-04 | Settings has only four volumes and Hit pause. | Display (PC: window mode, resolution, vsync), Controls (remap), Accessibility (screen shake, rumble, hold vs toggle, text size), Language. Roadmap milestone 3. | H / L | Code | `GamePauseMenu.cs` |
| UI-05 | Glyphs: the prompts draw text key caps. The accepted Batch 13 input glyphs aren't wired (handoff §5). The active device isn't tracked. | After UI-08: an `InputGlyphs` service that switches on the last device used (keyboard, Xbox, PlayStation, Switch). | M / M | Code | `MraState.Prompt`, new service |
| UI-06 | **Quit** in the pause menu calls `Application.Quit()` (`GamePauseMenu.cs:112`). Console certification generally doesn't allow a game to quit itself. | Show Quit only on PC (`#if UNITY_STANDALONE`). | M / S | Code | `GamePauseMenu.cs` |
| UI-07 | No safe-area handling (`Screen.safeArea` isn't used). The hearts and route bar sit near the top edges. | A `SafeAreaFitter` on the HUD root, plus a TV safe-zone margin option. | M / S | Code | `UI/GameHud.cs`, `MraRouteBar.cs` |
| UI-08 | **Input is scattered.** Bindings are built with `new InputAction` in `PlayerMovement`, `PlayerCombatInput` and `PlayerThread`, and about 12 more scripts poll `Keyboard.current` and `Gamepad.current` directly (the pause menu, Chart, town, portals, prompts). That rules out remapping and consistent glyphs. | One `InputActionAsset` with Gameplay and UI maps (replace the unused template), a small `GameInput` wrapper, and the polling callers moved to actions. | **H / L** | Code + asset | `PlayerMovement.cs:221-245`, `PlayerCombatInput.cs:14`, `PlayerThread.cs:73-74,142-145`, `GamePauseMenu.cs:35-36`, `Town/TownInput.cs`, `MraChart.cs:59-73`, `MraState.cs:104`, `Mechanics/Portal.cs:126` |
| UI-09 | **The control layout.** Attack is the mouse's left button or the gamepad's **LB**. Keyboard-only players have no attack or thread key. The thread is the right mouse button or RT, with LT to pull. Dash is C or B, Jump is Space or A, Run is Shift or L3, the Chart is M or Select, and interact is Up. The X/Square and Y/Triangle face buttons are unused. | Q2. Recommended: Attack on X/Square (West) and J on the keyboard, Dash on RB or B, the thread on RT or Y, Run as a toggle or always on. | H / S (once decided) | Code | as UI-08 |
| UI-10 | The route bar sets its text every frame (`MraRouteBar.cs:224`), which allocates a string and rebuilds the canvas each frame. | Update it only when the whole-metre value changes. | L / S | Code | `MraRouteBar.cs` |

### 3.8 Performance

PC measurements are healthy (section 1.1), so these items are about hygiene and the console budgets, not a current frame-rate problem.

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| PERF-01 | **`ParallaxLayer.Refresh` rebuilds both meshes every LateUpdate** with new vertex, UV, colour and index arrays (`ParallaxLayer.cs:157-170`). That's about 8 allocations per layer per frame across 4–8 layers, plus a mesh upload each time. | Build the quad once. Each frame update only the transform and a UV offset, through reused arrays with `SetVertices` and `SetUVs`, or through a material property block (`_MainTex_ST`). | M / S–M | Code | `Environment/ParallaxLayer.cs` |
| PERF-02 | IMGUI prompts allocate per frame (UI-02). | See UI-02. | M | Code | — |
| PERF-03 | **Saving writes synchronously on the main thread with a disk flush on every change**: each flag, Amber change, pickup and checkpoint (`GameSave.Write`, `stream.Flush(true)`). | Mark the save dirty and write at checkpoints, scene changes, pause and quit. Write off the main thread on PC. Consoles will need their platform save APIs anyway (platform direction rule 10). | M / M | Code | `Progress/GameSave.cs` |
| PERF-04 | `MraTerrain` builds its meshes and polygon colliders at `OnEnable` (runtime). Region loads took 21–748 ms on a fast PC, and consoles will be slower. | Measure on the slow targets first. If needed, bake the generated meshes into scene assets when building. | M / M–L | Code (builder) | `MountainReliefAtlas/Scripts/MraTerrain.cs` |
| PERF-05 | **The quality levels are Unity's template:** six levels (Very Low … Ultra), all on one URP asset. Standalone defaults to Ultra (`skinWeights: 255`, vsync 1). The editor sits on Very Low (BLK-03). | Reduce to named levels that mean something, such as "PC" and "Handheld" (and a "Console" level later). Set vsync and `Application.targetFrameRate` explicitly, with per-platform defaults. Keep at least 2 skin weights (the Qori v3 notes need them if v3 resumes). | M / S–M | Editor | `ProjectSettings/QualitySettings.asset` |
| PERF-06 | No Sprite Atlases (0 in the project). Each sprite is its own texture, so the SRP Batcher works per texture. | Look at the Frame Debugger on MR04 and MR05 before acting. If batches are high on the slow targets, atlas the Qori rig parts and the per-region props. Atlases repack imported textures only; source PNGs are unchanged. | L / M | Editor | — |
| PERF-07 | Per-call `LayerMask.GetMask` (PHYS-04), the Sfx diagnostics (AUD-04) and the route-bar text (UI-10). | Bundled in the quick-win batch. | L / S | Code | — |

### 3.9 Architecture and code health

Refactors listed only where they unblock polish or future work.

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| ARCH-01 | **A dead Qori rig.** The `QoriVisual` stack is about 3,200 lines in 17 files (`QoriBodyRig` alone is 1,330), and its GameObject is inactive in `Player.prefab`. It still compiles, and live code checks it as a fallback: `PlayerThread.cs:70`, `QoriSlashTrail.cs:59-60`, `PlayerCombat.cs:11`, `QoriPoseLookup`, and `PlayerCombatInput.cs:29`, which searches for a `QoriBodyRig` on every attack press (always null). | Confirm that `Assets/QoriRigTest` (parked) doesn't depend on it, then delete the stack and the fallbacks. | M / M | Code + prefab | `Scripts/Qori*.cs` (legacy), `Player.prefab` |
| ARCH-02 | **Two game generations.** The A0–A2/R1 systems (`ChartScreen`, `StirSequence`, `TitanKnot`, `Knots`, `WorldAtlas`, `Portal` veins, `TerrainBlock`/`TerrainKit`, `LoreStone`, the Town and Palm prototypes) ship next to MRA, and `GamePauseMenu` still has an IMGUI fallback for them. | After Q1: drop the old scenes from the build, then move their code to an archive folder that isn't compiled into the player (or delete it). | M / L | Code | `Scripts/World`, `Scripts/Prototype`, `Scripts/Terrain`, old scenes |
| ARCH-03 | **No assembly definitions.** All 248 scripts recompile on every change, including the 1,128-line test driver. | Add asmdefs: `Qolossal.Runtime`, `Qolossal.MRA`, `Qolossal.Editor`, `Qolossal.Tests`. An asmdef splits code into separately compiled assemblies, so a change recompiles only its own part. | M / M | Code | new `.asmdef` files |
| ARCH-04 | **Tuning lives in code.** Weapon timings are arrays in `QoriArmoryFactory.cs:22`, the hit-stop durations are constants in `HitStop.cs`, `PlayerMovement` has private constants (`GlidePull`, `MaxSlopeRise`, `GroundSnapDistance`), and the gait thresholds are in `QoriAnimator`. Changing feel means editing code. | `MovementTuning`, `WeaponTuning` and `FeedbackTuning` ScriptableObjects (data assets you can edit in the Inspector and tune during Play mode). | **H / M** | Code + assets | listed files |
| ARCH-05 | Obsolete API: `ContactFilter2D.NoFilter()` (`Mechanics/PressurePlate.cs:18`, warning CS0618 on every compile). | Use the replacement the warning suggests. | L / S | Code | `PressurePlate.cs` |
| ARCH-06 | Leftovers: the unused `InputSystem_Actions.inputactions` template, `Assets/_Recovery/*.unity` crash backups (gitignored), and the dense one-line style in `PlayerMovement` and `PlayerCombat`, which makes reviews harder. | Clean these up as each file is touched. | L / S | — | — |

### 3.10 Save and progression

What's saved (`GameSave`, JSON in `persistentDataPath`): relics, knots woken, charted levels, veins, sproutlings, Lore Stones, pickups, the checkpoint per scene, the last scene, and a list of string flags and counters (Amber, rescues, rewards, the reveal). Health isn't saved by design. Writes are safe: a temp file is swapped in with `File.Replace`, and the old save is kept as a backup and read if the save is corrupt.

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| SAVE-01 | One slot, no title choice, and Continue happens inside a `RuntimeInitializeOnLoadMethod` after loading scene 0 (BLK-01). | A boot scene, a title and slots (roadmap milestone 3). | H / L | Code | `GameSave.cs` |
| SAVE-02 | It writes too often (PERF-03). | See PERF-03. | M / M | Code | — |
| SAVE-03 | Versioning only adds missing lists. A renamed flag or a removed region id would silently strand progress. | A migration step per version, and a test that loads saves from older versions. | M / S | Code + test | `GameSave.cs` |
| SAVE-04 | Settings use `PlayerPrefs` (the registry on Windows). That's fine for PC; consoles need platform storage. | Later, with the platform layer. | L | — | `Sfx.cs`, `HitStop.cs` |
| SAVE-05 | Not checked: whether the equipped weapon survives a relaunch. `AreaTransition` carries it between scenes, but the save holds no weapon field. | Play-mode check (section 6). | ? | — | `AreaTransition.cs`, `PlayerCombat.cs` |

### 3.11 Build readiness

| ID | Problem | Proposed change | Impact / Effort | Work | Files |
|---|---|---|---|---|---|
| BUILD-01 | The build list (BLK-01) and missing scripts (BLK-02). | See the blockers. | H | — | — |
| BUILD-02 | **Player settings are template values.** `companyName: DefaultCompany` (BLK-05), version 1.0, no icons (`m_BuildTargetIcons: []`), the Unity splash with no logos, `resizableWindow: 0`, and no scripting backend set (Mono for Windows). | The final company and product name, versioning, icons and splash. IL2CPP for release builds; consoles need it anyway. | M / S | Editor | `ProjectSettings/ProjectSettings.asset` |
| BUILD-03 | The Graphics default render pipeline is empty; URP is assigned only per quality level. | Assign `UniversalRP.asset` as the default too, so no level can ever fall back to the Built-in pipeline. | M / S | Editor | `ProjectSettings/GraphicsSettings.asset` |
| BUILD-04 | Quality levels per platform (PERF-05). | — | M | — | — |
| BUILD-05 | No console modules or SDKs, no Steamworks. | Roadmap milestones 6 and 7. Every console item is **not tested**. | — | — | — |

---

## 4. Phased roadmap

Each batch is sized to finish and test in one session. Every batch ends with the polish test (`MraPlayTest.Run "-mraPolishOnly"`), plus the audio test where sound is touched, on the scratch copy. Nothing is committed until the user has played it.

| Batch | Contents | Why this order | Needs from the user |
|---|---|---|---|
| **0. Unblock** | BLK-01 (MR01 first, test scenes unticked, New Game by name), BLK-02 (missing scripts), BLK-03 (editor quality), ARCH-05, BLK-04 (audio test and full play test) | Every later judgement depends on a sane start scene and full-resolution Play mode | Q1, Q6; then the play-through and "push" |
| **1. Feel quick wins** | FEEL-01 (fall and apex gravity), FEEL-02 (air drag), FEEL-08 (60 Hz physics), FEEL-05 (trauma shake), PHYS-04 / PERF-07 caching | Small code changes with the biggest change in feel | Play and say what's better or worse |
| **2. Tuning assets** | ARCH-04 (movement, weapon and feedback ScriptableObjects) | Lets the later feel batches be tuned live in Play mode | — |
| **3. Locomotion animation** | ANIM-01 (blend tree), ANIM-02, ANIM-06 | The main stiffness source | Watch walk, run and start/stop on flat ground and slopes |
| **4. Air and landing** | ANIM-03 (Apex and FastFall), ANIM-04 (additive landing layer) | The second most visible stiffness | — |
| **5. Turn, follow-through, special poses** | ANIM-05 (skid), ANIM-07 (per-channel lag), ANIM-08 (glide), ANIM-10 (dash) | Builds on 3 and 4 | Judge the clip sheet and in-game captures |
| **6. Hurt, death and combat feel** | ANIM-09, FEEL-03, FEEL-07, FEEL-06, AI-02 | Combat reads properly once Qori's base motion is fixed | Q3, Q10 |
| **7. Camera** | FEEL-04 (vertical platform snap) and look-ahead tuning | Easier to judge once the jump arc is final | Q9 |
| **8. Performance hygiene** | PERF-01, UI-10, AUD-04, PERF-03 | Independent; good filler | — |
| **9. Input foundation** | UI-08 (action asset), UI-09 (layout), UI-06 (no console Quit), AUD-03 | Unblocks remapping, glyphs and console work | Q2 |
| **10. Prompts and text** | UI-02 (UGUI prompts), UI-05 (glyphs), UI-03 (TextMeshPro font), UI-07 (safe area) | Depends on batch 9 | Choose the font |
| **11. Cleanup** | ARCH-01 (legacy rig), ARCH-03 (asmdefs), VIS-02, VIS-03 | Removing the dead code is safer after the animation work proves the live rig | Q7 |
| **Later** | PHYS-01, PHYS-02, PHYS-03, VIS-01, AI-01, ARCH-02, PERF-04/05/06, BUILD-02/03, the title screen, settings and slots (roadmap milestone 3) | Bigger or decision-dependent | Q4, Q8 |

---

## 5. Open questions (yours to decide)

1. **Q1, the build's entry point.** Until there's a title screen, should a launch and New Game start in MR01 Cradle? Can A0–A2 and R1 leave the build list now? **Answered 10 Oct: yes.**
2. **Q2, the controls.** Recommended: Attack on X/Square and J, Dash on RB or B, the thread on RT, and Run as a toggle or always on, instead of Attack on LB and Run held on L3. Do you want that, or something else?
3. **Q3, death and falls.** Today both are free: full hearts back at the checkpoint. Keep that, or should a fall cost a heart and a death cost something, like a share of Amber?
4. **Q4, lighting.** Allow a per-region light tint and gentle colour grading (it changes how the approved art reads on screen, not the files), or keep it exactly flat and switch to unlit to save the cost?
5. **Q5, the silent sounds.** Which of the 53 should be regenerated? The most-felt ones are the Staff swing, jump, hurt, death, footstep and the ambience beds.
6. **Q6, the company/publisher name** for the Player settings. It sets the save folder, so it's better decided before anyone outside plays. **Answered 10 Oct: "Q Studio" for now.**
7. **Q7, legacy code.** Can the disabled procedural Qori rig (~3,200 lines) and, after Q1, the old A-scene systems be deleted, or should they be archived?
8. **Q8, enemy density.** Are 12 enemies along the whole route intended (a calm climb), or should each region get more encounters?
9. **Q9, the camera.** Do you want the Hollow Knight-style camera that holds height until Qori lands or leaves a band, or the current smooth follow?
10. **Q10, weapon speed.** Should the Leaf Staff get faster (about 0.4 s instead of 0.66 s per swing) and a 2-hit follow-up?

---

## 6. Play-mode checklist (what this review couldn't verify)

Set the editor quality level per BLK-03 first, and use a gamepad as well as the keyboard.

**Animation**
- [ ] Walk on flat ground from a standstill and stop: is there a visible pop in the first 2–3 frames (Walk flicker, ANIM-01)?
- [ ] Hold a partial stick tilt: does Qori ever settle into the Walk clip, and do the feet slide?
- [ ] Run up and down the Qvale steps and an MR03 slope: do the feet slide against the ground (ANIM-06)?
- [ ] Jump on the spot and while running: watch the apex for a snap (ANIM-03) and the landing for a missing settle (ANIM-04).
- [ ] Reverse direction at full speed: count the frames Qori moves backwards while facing forwards (ANIM-05).
- [ ] Glide, dash, take a hit, die: does each have a readable pose?
- [ ] The cloak and ears on a sudden stop and a turn: do they lag naturally, or overshoot and jitter at 30 and 144 FPS?

**Feel and camera**
- [ ] A series of small hops on flat ground: does the camera bob?
- [ ] A long fall into a pit: can you see the landing in time?
- [ ] Let go of the stick mid-jump: how far does Qori drift?
- [ ] Hit an enemy with the Staff, then the Mace: do the hit-stop, shake and rumble feel distinct?
- [ ] Die to an enemy and fall in deep water: is the respawn readable or jarring?

**Collision**
- [ ] Walk the whole Qvale street and every MR03 step at walking and running speed: any catches or hitches?
- [ ] Ledge grabs on the 1.9–3.2 u walls; wall jumps on the tall moss walls.

**Enemies**
- [ ] For each enemy type: is the telegraph readable before it hurts, and does Qori get knocked into another hazard?

**UI**
- [ ] Prompts at 1280×720 and 3840×2160: size and sharpness.
- [ ] The pause menu with only a gamepad: can every page be reached and left?
- [ ] An ultrawide (21:9) and a 16:10 window: the route bar, hearts and Chart.

**Save**
- [ ] Equip the Sword, quit, relaunch: which weapon is in hand (SAVE-05)?
- [ ] Collect Amber quickly: any hitch when the save writes (PERF-03)?

**Audio**
- [ ] A full region with the 10 Oct selection: which silences are noticeable?
- [ ] The Sound page: lower Effects past 0% and see whether it jumps back to 100% (AUD-03).
