# Qolossal: independent design review (proposal)

**Status: proposal only.** Nothing in this document is approved. It doesn't change the canonical design (`QOLOSSAL_WORLD_DESIGN.md`), the platform policy (`QOLOSSAL_PLATFORM_DIRECTION.md`), accepted art, or any code or scene. Section 0 lists what is already decided, so the recommendations can't be mistaken for decisions. Written 1 Oct 2026 by Claude (acting as game director, gameplay designer and Unity developer), after reading the design doc, the platform policy, Reviews 12–13, the Batch 6 request, the platform handoff, project memory and the current code.

---

## How to read this review

### Evidence levels
Every finding says how I know it:
- **[Built]** It exists in code or scenes. I cite the file.
- **[Auto-tested]** It is covered by a batch play-mode test that drives the real game (virtual keyboard, physics, scenes). These tests check behaviour, not feel.
- **[User-tested]** You played it and told me the result. So far that is only the Wind Leaf dash and Glidecap glide ("all works", 30 Sep), after three rounds of fixes.
- **[Designed]** It is only in the design documents.
- **[Opinion]** My judgment. No play-test backs it.

**I have never played Qolossal with a controller or keyboard.** All my "testing" is automated scripts and screenshots. That means nothing below about *feel* (combat weight, jump arc, camera, pacing) is a confirmed finding. Where feel matters I say so, and I propose the smallest play-test that would answer it.

### What exists today (the source of truth)
- **Scenes [Built]:**

  | Scene | Size | What it is |
  |---|---|---|
  | `A0_TestRoom` | ~200 u | mechanics gallery and world reach |
  | `A1_Aqueduct` | ~157 u | |
  | `A2_Grove` | ~178 u | |
  | `R1_GripKnot` | ~74 u | placeholder Knot Chamber |
  | `QoriMovementLab` | | sandbox |
  | `OpeningLevel` | | disabled |

  That is about 600 units of real play space. The design calls for 28 spaces of 350–500 u, so **~5% of the planned level length exists**.
- **Movement [Built, Auto-tested]:**
  - `Assets/Scripts/PlayerMovement.cs`: run, jump with coyote time and buffering, wall slide and wall jump (Climbing Moss), ledge hang and climb, Wind Leaf dash, Glidecap glide, knockback.
  - `PlayerThread.cs`: the grapple.
  - Bloomfall pogo, via `PogoAbilityDefinition`.
- **Combat [Built]:**
  - Weapons, set up in `QoriArmoryFactory.cs`:

    | Weapon | Damage | Startup (s) |
    |---|---|---|
    | Leaf Sword | 1.2 | .16 |
    | Seedpod Mace | 2.0 | .34 |
    | Thorn Spear | 1.1 | .17 |
    | Resin Sling (ranged) | .35 | |

  - **All weapons are owned from the start** (`QoriArmory.cs`).
  - Four-swing combos; up and down attacks.
  - Weapon-only barriers: thorns (sword), rubble (mace), weak floors (down-strike), bark shields (spear), seed switches (sling).
  - **No dodge, parry or invulnerable move exists.** Invulnerability exists only for 1 s after being hit (`PlayerHealth.cs`). The dash has no invulnerability.
  - Five hearts, and no death penalty.
- **Enemies [Built]:** 9 kinds (Crawler, Carrier, Thornwing, Pod Spitter, Shellback, Newt, Grub, Gust Moth, Bark Sentinel) in `Assets/Scripts/Creatures/`, with area palette swaps. There is one guardian, the Knucklebramble (`KnucklebrambleGuardian.cs`). It is Auto-tested, **not** User-tested.
- **World systems [Built, Auto-tested by `WorldPlayTest`, `WorldCheck`]:**
  - world state and stir variants;
  - the Chart (`ChartScreen.cs`);
  - Vein Gates that are unknown until used, and Wild Veins;
  - one working stir: Grip clenches, the Crease bridges, the elbow vein grows, the finger gate opens;
  - Waymarks, Lore Stones and heart seeds.
- **Not built [Designed only]:**
  - Old Loam, Scribble and any dialogue system;
  - Sproutling behaviour; the Wilted as a character;
  - the Seer's Lantern;
  - Sap Vials and weapon tiers;
  - every "new puzzle mechanic" in design §3.6 except clench-style stir gates (sluices, tendon pulls, breath currents, sap valves, heavy plates, mill wheels, mirrors, pulse bridges);
  - updrafts; 7 of 8 guardians;
  - localisation, remapping and safe areas;
  - console build profiles; any music integration.
- **Production method [Built]:** every level is written as C# coordinates in a builder (`A0TestRoomBuilder.cs`, `A1RoomBuilder.cs` and so on). There are 26 editor builders and tests. **This matters a great deal for scale** (see R1 below).
- **Presentation [Built]:**
  - Qori is a cutout Animator rig (`Assets/Scripts/QoriRig/QoriAnimator.cs`); the old `QoriVisual` is disabled.
  - On-screen text uses IMGUI in 13 scripts.
  - Input is hard-coded `InputAction`s per script. There is no remapping, and the Chart's direct toggle is keyboard-only.
- **Music:** the Codex procedural auditions were rejected, and the music is moving to Suno. No music is integrated. The design's "soundtrack wakes with the titan" (stacked, synchronised stems) is **not deliverable as written** with finished Suno tracks (see R9).

---

## 0. Approved decisions this review does not reopen
These are recorded in design doc §10 and §11, the platform policy and review history:
- The titan's design, including the sheltering arm and the crown-mask with no human face.
- Codex's twelve art-direction proposals.
- Act II in either order.
- Wild Veins random until settled.
- The Wilted healed at the end.
- No death cost.
- No post-game; the Risen world is saved for a sequel.
- Target platforms: Windows PC, PS5, Xbox Series X|S and Switch 2 (not the original Switch). 60 FPS is a target, not a result.
- Five guardian concepts approved (B-03 to B-07); Batch 7 parts pending.
- The Vine Whip cut.
- Music moving to Suno.

Where I recommend changing one of these, I mark it **"reopens a decision"**, and it appears again in section H.

---

## A. Overall verdict

**The strongest foundation.** The world is literally one sleeping body, and **waking it changes places you have already been.** That is a distinctive metroidvania promise: the map is an anatomy you slowly recognise, and progress is the landscape itself moving. The best pieces serve it well:
- The mint-means-alive rule makes "alive" readable at a glance.
- The Chart as "recognition" (hills that turn out to be a hand) makes the map part of the story.
- Guardians as local organs of the body.
- The palm-and-shelter image makes the story's heart visible.

The Crease bridge proves the loop in miniature: a pit you couldn't cross becomes a clenched finger, and the stir itself shows you why. **[Built, Auto-tested]**

**The biggest threats, in order:**
1. **Scope against production method.** At the current rate, the level plan (28 spaces × 350–500 u, 8 bosses, 10 new puzzle mechanics, NPCs, cinematics) is years of work. Its C#-coordinate level building is the slowest possible way to make 10,000+ units of hand-crafted metroidvania space. **[Built + Opinion; high confidence on the arithmetic]**
2. **The world is cut into portal-linked scenes.** That undermines pillar 1, "one body, one maze". A body you cross by teleporting between separate rooms reads as a level select, not a creature. Random Wild Veins make it worse. **[Designed + Opinion; medium confidence, needs play-test]**
3. **The move set is the genre's standard kit, renamed.** Wall jump, grapple, dash, pogo, glide and light are the Hollow Knight and Ori set. Only the stirs and the Chart are truly the titan's own. Players will recognise the abilities before they recognise the body. **[Opinion; medium-high confidence]**
4. **Combat has no defensive verb.** You can't avoid damage except by walking away. The first defensive tool, the dash, arrives in Region 3, and even then without invulnerability. With telegraphed enemies and weapon-key barriers, combat risks being "trade hits and swap weapons". **[Built; the feel is untested]**
5. **The global stirs multiply content.** "Wind in *all* regions", "updrafts in *all* outdoor regions" and "light shafts in *all* regions" mean every level needs up to six before/after variants that affect traversal. That is the most expensive promise in the doc, and the one most likely to be quietly dropped. Dropping it would leave stirs as spectacle. **[Designed + Opinion; high confidence on cost]**

**The recommendation in one line:** make the game **smaller, contiguous and stir-centred**. Fewer, denser regions stitched into a continuous body. Fewer, titan-specific abilities. Every stir makes one memorable, *usable* change to a place you know. Prove it with one region before building more.

---

## B. Keep / Improve / Replace / Cut / Test

| # | Item | Verdict | Why (short) | Evidence |
|---|---|---|---|---|
| 1 | Sleeping-titan world, mint = alive, rot never glows | **Keep** | The core identity, and it's working visually | Built (art) |
| 2 | Stirs change earlier places | **Keep and make central** | The one thing only this game does | Built (1 stir) |
| 3 | The Chart as recognition (moss, mist, old contour) | **Keep** | Turns the map into story | Built, Auto-tested |
| 4 | Guardians as organs, knot as weak point | **Keep, Improve** | Strong concept; the fights must not all be "pattern, then expose knot" | Built (1), Designed (7) |
| 5 | The palm, the shelter and the careful ending | **Keep** | The emotional spine | Designed, art done |
| 6 | Waymarks that chart the level | **Keep** | Clear, cheap, readable | Built |
| 7 | No death cost | **Keep** | Suits the tone; lets levels be harder | Decided |
| 8 | 7 regions × (3 levels + chamber) | **Replace** | Too much content; the same template repeated makes regions blur | Designed |
| 9 | Portal-linked separate scenes as the main connective tissue | **Replace** | Breaks "one body"; hides continuity | Built + Designed |
| 10 | Wild Veins random every use | **Test, then likely Replace** (reopens a decision) | Random teleports frustrate route-planning | Built, Auto-tested |
| 11 | Global per-stir traversal changes in every region | **Replace** | Content multiplication; limit to the local region plus one neighbour | Designed |
| 12 | Six abilities as a generic kit | **Improve** | Tie each to the body; cut overlaps | Built (5), Designed (1) |
| 13 | Seer's Lantern (ability) | **Cut** (fold into the Sight stir) | A secrets tax and the weakest ability; the Sight stir can do its job | Designed |
| 14 | No defensive move | **Replace**: give an early evade | Combat needs a skill verb before R3 | Built |
| 15 | Weapons as keys (thorns, rubble, bark), all owned at start | **Improve** | Swap-to-open friction and no weapon progression | Built |
| 16 | Sap Vials and weapon tiers | **Cut for now** | An economy with nothing to buy yet; T2 art can reappear as found weapons | Designed |
| 17 | 10 new puzzle mechanics (§3.6) | **Cut to 4** | Each costs code, art and teaching; pick the body-specific ones | Designed |
| 18 | Old Loam and Scribble | **Improve** (merge roles, few lines) | Two NPCs doing hint duty; one is enough early | Designed |
| 19 | The Wilted arc | **Keep, Improve** | Best character idea; show its failed attempts in the world | Designed |
| 20 | Sproutlings + Lore Stones + shards + seeds | **Cut to two collectible kinds** | Four collectibles for a small game dilutes all of them | Built (seeds, lore), Designed |
| 21 | "Soundtrack wakes with the titan" as stacked stems | **Replace** (Suno-compatible version) | Suno gives finished tracks, not layers | Designed |
| 22 | Level building as C# coordinates | **Replace** for real levels | Too slow for thousands of units of hand-made space | Built |
| 23 | IMGUI text, hard-coded input | **Replace** before content grows | Console, controller and localisation blockers | Built |
| 24 | Batch play-mode test suite | **Keep** | Unusually strong; protects every change | Built |
| 25 | Knucklebramble fight | **Test** (you haven't played it) | Its timings are my guesses | Auto-tested |
| 26 | Stir cinematics 8–12 s + map redraw | **Improve**: shorter, player-controlled camera | Long unskippable cinematics on each knot tire people | Built (stir) |

---

## C. The ten highest-value changes (ranked by impact against cost)

Each change gives: **Problem → What players get now → Change → What players get then → Benefit and tradeoff → Cost → Reuse → Confidence and smallest test.**

### R1. Build levels in scenes with a tile/kit workflow, not C# coordinates
- **Problem:** `A0TestRoomBuilder.cs` places every block as `Block("Plateau", 24.8f, plateau, 19.2f, …)`. Iterating on a jump means editing numbers, recompiling, rebuilding and screenshotting. It's fine for a 200 u gallery, fatal for 10,000 u of paced, secret-filled space.
- **Now:** each level change costs minutes of round trip, and designers can't sketch.
- **Change:** author real levels directly in scenes, using the existing `TerrainBlock` / `TerrainPiece` components as a snap-to-grid prefab kit. A small editor tool would drag a block's edges and regenerate its art. Keep the builders for test rooms and reproducible checks only.
- **Then:** a level blockout in an hour, tuned by playing it.
- **Benefit:** the single biggest multiplier on everything else.
- **Tradeoff:** less "rebuild from scratch" reproducibility. The play-mode tests remain the safety net.
- **Cost:** low to medium, about a week. The terrain components already exist and build their own art.
- **Reuse:** `TerrainBlock`, `TerrainPiece`, the terrain kits, `AreaRoom` helpers (as editor tools), and every test.
- **Confidence:** high.
- **Smallest test:** block out one 350 u R1 level both ways and time it.

### R2. One contiguous body: connect regions physically; make Vein Gates rare shortcuts (reopens part of design §3.1)
- **Problem:** pillar 1 is "one body, one maze", but travel today is portal → fade → new scene. Players never walk from palm to wrist and *feel* the body continue.
- **Now:** Qori exits A0 through an arch and appears elsewhere. The body is a Chart abstraction, not an experience.
- **Change:**
  - Each region is a set of edge-connected rooms (the classic metroidvania room graph). The room scenes load additively as Qori walks between them.
  - Regions join where the body joins: wrist to palm, elbow to arm, shoulder to chest.
  - Vein Gates become few (one or two per region), discovered, and useful as shortcuts, *not* the main way between places.
  - Keep "unknown until used" for those few.
- **Then:** the player walks up a forearm, sees it curve toward a shoulder, and *arrives* at the chest. The Chart confirms what the body already showed.
- **Benefit:** the titan fantasy is felt, not told, and the portal count drops.
- **Tradeoff:**
  - Additive loading and streaming work.
  - Region joins must be designed.
  - The Wild Vein surprise loses some weight.
- **Cost:** medium (additive scene loading around the existing `AreaTransition.cs`, plus room-edge triggers).
- **Reuse:** `Portal`, `AreaTransition`, the vein records, the Chart and `WorldAtlas`.
- **Confidence:** medium-high on the benefit; the streaming cost needs measuring on Switch 2.
- **Smallest test:** join A0's east edge to R1_GripKnot as a walked room edge (no portal) and have you play it both ways.

### R3. Fewer, denser regions: 5 regions × (2 levels + a chamber), or equivalent (reopens design §1.3 and §5)
- **Problem:** the plan is 7 regions × 4 spaces, each region on the same template (teach, combine, raise stakes, exam, boss). Seven repeats of one template will blur together, and the content cost is the project's biggest risk.
- **Now (planned):** 28 spaces, 6–10 hours; realistically, many will be thin.
- **Change** (one option of several; the exact merge is your call):
  - Merge Palm and Arm into one Act I region: *the Hand and Arm* (Grip + Reach).
  - Merge Crown and Eye into *the Head* (Bloom + Sight, with Sight as a stir only; see R6).
  - Keep the Grove, the Knees and the Heart.
  - Each region has two large interconnected levels plus the chamber.
- **Then:** about 15 spaces, each richer. Every region has room to be itself.
- **Benefit:** feasible scope, less template fatigue, and every area can have a real secret network.
- **Tradeoff:** a shorter game (a target of roughly 4–6 hours), and some approved guardians or areas are merged or cut.
- **Cost:** negative; it saves a great deal.
- **Reuse:** all accepted art, including the A5 and A6 kits.
- **Confidence:** medium (scope is your decision).
- **Smallest test:** plan R1 at the new density on paper (room graph and beats) and compare the build estimate with the current plan.

### R4. Give Qori an early evade, and make the Wind Leaf dash its upgrade
- **Problem:** there is no defensive verb until R3's dash, and the dash has no invulnerability (`PlayerMovement.cs`: `IsDashing` only sets velocity). Enemies telegraph with a glint (the Sentinel, the Knucklebramble), but the player's only answer is to walk away.
- **Now:** in the Knucklebramble fight you watch the arm rise, then step out of range or tank the slam. Skill expression is spacing only.
- **Change:**
  - From the start, a short **seed-hop** backstep with about 0.15 s of invulnerability, ground only, with a cooldown.
  - Wind Leaf later upgrades it into the air dash (keeping the relic moment).
  - Guardians' telegraphs become things to read and *answer*.
- **Then:** fights become dodge-and-punish; the exposed-knot window rewards a well-timed evade.
- **Benefit:** combat depth for little code.
- **Tradeoff:** it slightly softens Wind Leaf's novelty, and enemy damage may need raising.
- **Cost:** low. The dash code already exists; an invulnerability window takes a few lines in `PlayerHealth.TakeDamage`.
- **Reuse:** the dash code and its effects.
- **Confidence:** high that combat needs it; medium on the exact form.
- **Smallest test:** add the evade behind a toggle and play the Knucklebramble fight both ways.

### R5. Local stirs, each changing one remembered landmark into a usable route
- **Problem:** the global stir effects (wind everywhere after Breath, updrafts everywhere after Bloom, light shafts everywhere after Sight) need traversal variants in every level. It will either explode the content or quietly become cosmetic.
- **Now (planned):** 6 stirs × ~20 earlier spaces = a combinatorial variant load.
- **Change:**
  - Each stir makes **one or two traversal changes**: in its own region and in *one* neighbouring region, each at a landmark the player has already looked at as a "locked sight" (design §3.5 beat 4).
  - Global effects become ambient only (grass sways, light warms).
  - Every stir is shown on the Chart with the old contour.
- **Then:** "the arm lifted, and that flooded gallery I saw is now a dry path", every time, clearly. There are no hunts for invisible changes.
- **Benefit:** stirs stay the star at a fraction of the cost.
- **Tradeoff:** fewer "the whole world changed" moments. Compensate with the vista and the Chart.
- **Cost:** it saves a great deal.
- **Reuse:** the `StirVariant` system and builders' `Stir(...)` scopes as they are.
- **Confidence:** high.
- **Smallest test:** the Grip stir already does this (the Crease bridge). Have you play it cold and see whether you notice the bridge without being told.

### R6. Cut the Seer's Lantern ability; let the Sight stir do its job (reopens design §3.4)
- **Problem:** a reveal lantern is the weakest traversal ability in the genre. It gates secrets rather than movement, needs "fake wall" content everywhere, and arrives in the last act, so it's barely used.
- **Now (planned):** a sixth relic, a light radius and hidden-platform content in every level.
- **Change:**
  - The Sight stir itself opens the eye and throws directional light through the body (design §11.12 already describes this), permanently revealing marked secrets.
  - The Heart's darkness uses the glow pods (built).
- **Then:** the final act is about the Heart, not a new gadget.
- **Benefit:** cuts an ability, its art, its code and a secret layer.
- **Tradeoff:** one fewer relic moment.
- **Cost:** saves the most. Its art is partly delivered (FX_Lantern_Light, Relic_SeersLantern); it can be reused as the Sight stir's light.
- **Confidence:** medium-high.
- **Smallest test:** none needed; this is a scope decision.

### R7. Weapons: one found per region; keep one kind of weapon lock per region
- **Problem:** all three weapons exist from the start (`QoriArmoryFactory.Build`), and barriers each need a specific weapon. In practice: meet thorns, open the armory (Tab / View), pick the sword, cut, swap back. That is friction, with no progression.
- **Now:** four weapon-specific barrier types in A0's gallery.
- **Change:**
  - Start with the sword and sling.
  - Find the mace in the Knees (it breaks rubble; its weight suits the terraces), and the spear in the Crown (bark shields; the Brow Sentinel).
  - Each new weapon opens its barriers retroactively, which is great backtracking bait.
  - Make the weapon swap contextual: attacking a barrier with the wrong weapon briefly auto-swaps if you own the right one. **[Opinion]**
- **Then:** new weapons feel like rewards; old barriers become reasons to return.
- **Benefit:** progression and backtracking, with no new systems.
- **Tradeoff:** less weapon variety early. The tier-2 art waits (see Cut 16).
- **Cost:** low (ownership flags plus shrine-like pickups).
- **Reuse:** all weapon art, barriers and the Armory.
- **Confidence:** medium-high.
- **Smallest test:** give only the sword in A0 and play the gallery.

### R8. Four body-specific puzzle mechanics, not ten
- **Problem:** design §3.6 lists ten new mechanics, and each needs code, art, a teaching room and combinations. Several are genre generic (mirrors, mill wheels).
- **Change:** keep the mechanics that only a body could have, one per act:
  - **clench platforms** (a hand that grips, R1);
  - **sluices / water level** (the Arm's channels, and the Matriarch fight);
  - **breath currents** (Grove, local only; see R5);
  - **pulse bridges** (the Heart).

  Cut mill wheels, sap valves, heavy plates, tendon pulls and mirrors, or fold them into those four. (Heavy plates can simply be pressure plates plus a Shellback; they're already built.)
- **Then:** each mechanic is explored deeply rather than shown once.
- **Cost:** saves a great deal.
- **Reuse:** plates, root gates and the stir gates.
- **Confidence:** medium.
- **Smallest test:** prototype clench platforms in the Grip chamber, which already has the gate art.

### R9. Suno-compatible "the music wakes with the titan"
- **Problem:** design §7 wants synchronised layers added per knot. Suno produces finished stereo tracks. Its stem splitter separates instruments of one mix; it doesn't give independently composed layers that stay musical when stacked.
- **Change:**
  - Each region gets **two Suno tracks, asleep and awake**, with the same key and tempo requested in the prompts. Crossfade to the awake track when the region's knot wakes.
  - Keep one in-house element: the **heartbeat**, a simple low drum loop made in a DAW or taken from a licensed sound library. It speeds up with each knot and plays under everything, including the Chart.
  - A short Suno stinger for stirs.
- **Then:** the world audibly changes after each knot, with a cheap and reliable implementation.
- **Tradeoff:** it loses the "one motif grows into an orchestra" arc. Prompt Suno with the same melodic idea to keep some thread.
- **Cost:** low (a small `MusicDirector` with two sources and crossfades).
- **Confidence:** medium (Suno's consistency across prompts is the risk).
- **Smallest test:** generate an asleep/awake pair for R1 and crossfade them at the Grip stir.
- **Licensing:** commercial rights only for songs made on a paid plan, as noted in the session. Record the plan per track.

### R10. Make the UI and input console-ready before more content lands
- **Problem:**
  - IMGUI text in 13 scripts (knot, Waymark and Lore messages, the controls guide, the armory box).
  - Hard-coded `InputAction`s per script; no remapping.
  - A keyboard-only Chart toggle.
  - No safe areas and no localisation.

  Each new level adds more of this to fix later.
- **Change:**
  - One input actions asset (the Input System's `.inputactions`) used by all scripts.
  - A uGUI message/banner system replacing IMGUI.
  - A gamepad Chart button.
  - A device-aware controls page.
  - A text table (English only for now) so strings aren't in code.
- **Then:** controller-first play on TV and Switch 2 handheld, and certification-ready foundations.
- **Cost:** medium, done once.
- **Reuse:** `UiSkin`, the pause menu's uGUI, and the existing bindings (move them into the asset).
- **Confidence:** high (the platform policy requires it anyway).
- **Smallest test:** port just the Waymark and knot banners and the Chart toggle, then play A0 on a gamepad with no keyboard.

---

## D. Bold alternatives (up to three)

### D1. "The titan answers": let Qori *cause* local stirs, not just wait for knots
**The idea:** besides the seven big knot stirs, the body has **nerve nodes**. Qori strikes or grapples one, and the nearest body part *twitches* for a while:
- a finger lifts (a ledge appears);
- a rib flexes (a gap closes);
- an eyelid flutters (light for five seconds).

Stirs become the game's main puzzle verb rather than cutscenes.

- **Why it's distinctive:** no other metroidvania makes the *world* the tool.
- **Drawbacks:**
  - Each twitch is bespoke geometry with animation.
  - It could trivialise gating if overused.
  - It competes with the big stirs for meaning.
- **Recommend?** Yes, but small. Prototype **one** nerve node in R1 (a finger that lifts for 6 s). If it's the best thing in the slice, make it the signature. It reuses the stir-gate art and the `StirVariant` switching.

### D2. No abilities from relics: the stirs themselves are the abilities
**The idea:** cut relic abilities. Waking the Grip knot makes *every hand-like surface* climbable, and waking Breath gives the world timed gusts to ride. Qori's move set stays small; the world gains functions.

- **Why it's distinctive:** it fully expresses pillar 3 ("abilities are the titan's functions").
- **Drawbacks:**
  - Players lose the tactile personal power-up.
  - Designs get harder, because every surface must know its body part.
  - It invalidates built work (dash, glide, thread, pogo) and approved shrine moments.
- **Recommend?** **No.** It's too costly now and throws away working, liked features. Keep it as a lens: every ability should *also* change how Qori interacts with the body (the thread hooks tendons; the dash rides breath).

### D3. A handcrafted continuous "body walk" instead of regions
**The idea:** one continuous world with no regions, just body parts flowing into each other, and knots placed along a spiral route from palm to heart.

- **Drawbacks:**
  - Streaming complexity.
  - No per-region kit budgets.
  - A long build before anything is playable end to end.
- **Recommend?** **No** as a full replacement. R2 (contiguous regions with physical joins) gets most of the benefit at a fraction of the risk.

---

## E. A proposed first 20–30 minutes
The goal: the player understands *"I am on a living body, and I can wake it"* before minute 25, without a text wall.

| Time | Beat | What it teaches | Existing work used |
|---|---|---|---|
| 0:00–1:00 | Intro still: a seed falls into a moss palm; the title card. Qori sprouts. The ground under him **breathes once** (a slow rise and fall of the whole screen) | The ground is alive | Intro stills; the camera |
| 1–5 | **Seedbed:** move, jump and sword on soft moss. Fingers tower as the skyline. A crawler. A Lore Stone ("the hill that holds its hand open") | Controls; the setting as a hand | A0 kit, Lore Stone |
| 5–8 | First **evade** (R4) against a Pod Spitter; a heart seed shard tucked behind a knuckle | Read and answer telegraphs | Spitter; the new evade |
| 8–12 | **Lifeline Crease:** the Crease pit, too wide to cross, with something shining beyond it (the **locked sight**). Old Loam at the Waymark, in two lines: "That's the palm's crease. It closes, when the hand remembers." The Chart shows hills | A promise to return | Crease, Loam, Waymark, Chart |
| 12–15 | Vertical climb up the thumb (no wall jump yet: ledges); the **first distant view of the crown-mask** | Scale; the body continues | Ledge grab; backgrounds |
| 15–20 | **Grip Knot chamber:** a clench-platform puzzle (R8), then the Knucklebramble (evade, then punish the knot) | Mastery of the region's idea | Chamber; guardian |
| 20–23 | Touch the knot: Echo's line; the **stir**: the hand clenches (a short vista); the **Chart redraws**. The hills were fingers; the old contour stays | The central fantasy | StirSequence, Chart, stir vista R1 |
| 23–28 | Walk back **through the palm, changed**: the Crease is now a finger bridge, the shining thing is Climbing Moss (the relic), and the first wall climb leads out of the palm, up the wrist. The music shifts from the asleep to the awake track | The payoff: my action changed the world | Stir variants, Crease bridge, shrine |

Note: this moves Climbing Moss to *after* the stir and places it behind the Crease, so the stir is what opens the ability, not the other way round. **[Opinion]**

---

## F. A minimal vertical slice to test the riskiest assumptions
**Scope:**
- R1 only: **two** real levels plus the Grip chamber, with ~700–900 u total, authored with the R1 workflow.
- The evade (R4).
- The Knucklebramble.
- One nerve-node prototype (D1).
- The Grip stir with one local route change (the Crease) and one neighbour change (the wrist).
- The Chart.
- Suno asleep/awake tracks (R9).
- uGUI banners and a gamepad Chart button (R10).

| Assumption to test | How | Pass looks like |
|---|---|---|
| Players read the world as a body **without being told** | 3–5 first-time players; ask "where are you?" at minute 10 and 25 | Most say "on a hand" by minute 25 |
| The stir feels *meaningful*, not spectacle | Watch whether players go back and use the changed route unprompted | Most find the Crease bridge within 2 minutes after the stir |
| A contiguous body beats portals (R2) | A/B: the same content joined by a walked edge vs a portal | Players prefer walked, or it's neutral, with no loading pain |
| Combat needs the evade (R4) | Knucklebramble with and without it | Fewer "cheap hit" complaints; more knot hits per window |
| Level authoring speed (R1) | Time the second level's blockout | A 350 u blockout in under a day |
| The nerve node (D1) is fun | One node, one puzzle | Players experiment with it unprompted |
| Performance on the weakest target | Profile the densest room (parallax, fog, rig, guardian) on Switch 2 hardware if available; otherwise on a proxy, marked **not tested** on Switch 2 | ≤16.7 ms on hardware; the proxy is only indicative |
| Suno music continuity | Asleep → awake crossfade at the stir | Sounds like the same piece waking, not a change of song |

---

## G. Next development sequence

### Essential (in this order)
1. **Your decisions on section H**, especially scope and structure.
2. **The level-authoring workflow (R1)**, before any more level building.
3. **The input asset and uGUI messages (R10)**, before more UI text is written.
4. **The evade (R4)**, and your play of the Knucklebramble fight with and without it.
5. **The R1 vertical slice (F):** two levels plus the chamber, using the contiguous join (R2) and the local stirs (R5).
6. **Suno R1 asleep/awake (R9)** and a minimal `MusicDirector`.
7. **Play-test the slice** with 3–5 people who have never seen it; measure the checks in F.
8. **Only then:** Batch 7 guardians and the R2 content. Batch 7 can run in parallel with Codex, since the art isn't blocked.

### Optional polish (after the slice passes)
- The nerve node, expanded (D1), if it tests well.
- Contextual weapon swap (R7).
- Chart touches: Scribble's notes; the music-box Chart cue.
- The Wilted's environmental trail (section 5 below).
- Tier-2 weapons, as found upgrades, if an economy is still wanted.

### Story and emotion (section 5 of the brief, in short)
- **Keep:** Echo's two lines per knot, and Old Loam's four-line limit.
- **Add, cheaply:**
  - **The Wilted's trail:** wilted handprints on knots it failed to wake; its withered shrine offerings. This reuses Wilted art as decals.
  - **The cost of stirs:** after the Grip stir, a shrine on the knuckle has tipped over, and Loam notices. Each stir moves people's things, which makes the ending's careful hand pay off.
  - **Quiet moments:** one bench-like spot per region where Qori can sit and the titan breathes (a camera pull-back, no text). It reuses the stir vista view.
- **Merge:** Scribble into Loam (Loam carries the map), unless Scribble has a unique mechanic.
- **Avoid:** new lore without a function. **Every line should either point to a route or pay off a stir.**

### Presentation (in short)
- **Readability:** Review 13 already found the A6 backgrounds too contrasty behind Qori; apply that rule everywhere. The far layers need lower contrast than the gameplay plane.
- **Guardian knots:** must read at 1280×720 (asked for in the Batch 7 platform section).
- **Camera:** give guardian arenas a wider framing (a camera zone), so slams from off-screen arms aren't unfair. **[Opinion; test on the Knucklebramble]**
- **Accessibility:**
  - hold-to-glide vs toggle-to-glide (your "Glide Needs New Press" option is a start);
  - hit-pause and shake intensity sliders;
  - a subtitle size option;
  - colour-safe telegraphs (glint plus shape, not colour only).
- **Handheld:** check the smallest UI at 720p handheld size; IMGUI text can't scale properly (R10).

### Technical (in short)
- **Memory:** 600 px/u enemy and guardian parts, and 2048+ backgrounds × 3 parallax layers per area, are large. Set per-category import caps per platform after measuring. The per-entry `maxSize` added on 29 Sep is the hook.
- **Streaming:** additive room loading (R2) is also the natural way to keep memory flat.
- **Saves:** now atomic with a backup. Console storage and user adapters still need the platform SDKs (per the platform policy).
- **Tests:** keep the batch suite. **Fix the scratch-copy package-cache path issue**, which produces a false "runtime error" in every play-mode test, so real errors can't hide behind it. Shorter scratch paths or `LongPathsEnabled` would do it.

---

## H. Decisions that need you
1. **Scope:** keep 7 regions × 4 spaces, or move to about 5 regions × 3 spaces (R3)? This drives everything else.
2. **Structure:** regions as walked, contiguous bodies with rare Vein Gates (R2), or keep portal-linked scenes? Also: keep **random** Wild Veins (a current decision), or make them "unstable gates that open at the stir"?
3. **Combat:** add an early evade (R4)? Should it have invulnerability frames?
4. **Abilities:** cut the Seer's Lantern into the Sight stir (R6)? Move Climbing Moss to *after* the Grip stir, in the first 30 minutes (E)?
5. **Weapons:** start with the sword and sling only, and find the mace and spear in regions (R7)?
6. **Music:** accept per-region asleep/awake Suno tracks plus an in-house heartbeat (R9) in place of layered stems?
7. **The nerve-node prototype (D1):** worth a one-room experiment in the slice?

Everything else here I can prepare as options once you've answered these.
