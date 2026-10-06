# Mountain Relief Atlas — Unity handoff (3 Oct 2026)

> **Updated 4 Oct 2026 by the level polish pass:** designed terraces, caves, backgrounds and new tests replace parts of what's described below (the route generation, doors, backgrounds and the route audit). Read [LEVEL_POLISH_HANDOFF.md](LEVEL_POLISH_HANDOFF.md) first.

A connected, playable prototype of the eight-region climb built from the design package
`DesignReview/MountainReliefAtlas_20261003`. It is a **prototype**: graybox-plus-accepted-art levels,
real mechanics and saving, validated by an automated play test in Unity. It is not a finished game,
and nothing here claims console readiness.

**Result in one line:** 46 scenes (8 regions, 38 side chambers; Qvale's 6 interiors are in place),
walked end to end with the live Qori rig in Play mode by an automated test: **570 checks passed, 0 failed**.

## Where things are

| What | Where |
|---|---|
| Scenes (generated) | `Assets/MountainReliefAtlas/Scenes/` — `MRAtlas_MR01_Cradle` … `MRAtlas_MR08_Descent`, `MRAtlas_MRxx_Cyy` |
| Scene and content index | `Assets/MountainReliefAtlas/SCENE_INDEX.json` |
| Runtime scripts | `Assets/MountainReliefAtlas/Scripts/` (session, exits, chambers, gates, levers, rewards, residents, knots, reveal, ending, terrain, route projection, Chart) |
| Builder (deterministic, idempotent) | `Assets/MountainReliefAtlas/Editor/` — menu **Qolossal ▸ Mountain Relief Atlas ▸ Build All Scenes** |
| Route data (converted) | `Assets/MountainReliefAtlas/Resources/MRA/mra_world.json`, from `Tools/MountainReliefAtlas/convert_spec.py` |
| Source → import traceability | `Assets/MountainReliefAtlas/SOURCE_TRACE.json` (package file hashes, every copied image's source hash) |
| Review-only images | `Resources/MRA/Maps/` (the 8 relief paintings) and `ReviewArt/` (the Batch 11 final-assembly concept) — **not accepted art** |
| Accepted-art receipt and name check | `Assets/MountainReliefAtlas/PRESERVATION_RECEIPT.json` (`Tools/MountainReliefAtlas/verify_preservation.py`) |
| Play test | `Scripts/Testing/MraTestDriver.cs` + `Editor/MraPlayTest.cs`; results `Tools/MountainReliefAtlas/QA/mra_test_results.json` |
| Captures, seam proofs, profile | `Tools/MountainReliefAtlas/QA/` (in-engine 1080/720 captures), `SeamProofs/`, `mra_profile_1080.json` |

Rebuilding: run `python Tools/MountainReliefAtlas/convert_spec.py`, then the menu item. Only each scene's
`Generated (MRA builder)` root is replaced; the Player, camera and GameManager are reused, and anything
added by hand outside the generated root is kept.

## Preservation

- **All 1090 accepted files are byte-identical** in staging and in `Assets/` before and after (receipt above). No accepted
  file, pivot, PPU, slice or the accepted manifest was changed by this work. (The manifest's uncommitted diff is from
  Reviews 21–22, earlier.)
- Global case-insensitive name check: every new name under `Assets/MountainReliefAtlas` against accepted names,
  accepted sub-sprite names and all other Assets names: **0 collisions**.
- The live Player prefab is instantiated unchanged; no Player variant was needed (it already carries
  `PlayerAbilityController`, so relics come from the save, not "all owned").
- Review 22's six redo files are not imported or used. Music is untouched (placeholder tunes at the listening tree).

### Changes to existing (non-art) code — all additive

| File | Change |
|---|---|
| `Progress/GameSave.cs` | Save **version 3**: flags and counters lists, migrated from v2 by adding empty lists; `-saveFile <name>` so tests never touch the player's save. Temp/backup write path unchanged. |
| `Town/TownState.cs` | `BindToSave()`: opt-in persistence (Amber, quakes, town flags). The Qvale prototype stays session-only. |
| `Town/TownShop.cs` | Optional per-item `available` condition (Scribble lists pages only for visited places). |
| `Progress/AreaTransition.cs` | `IArrivalPoint`: ordinary exits and cave returns land at named points (portals and checkpoints work as before). |
| `Progress/GameArea.cs`, `World/ChartScreen.cs`, `GamePauseMenu.cs` | `legacyChart` flag: the old whole-body Chart never exists in these scenes; the pause menu's Chart button opens the regional Chart instead. |
| `PlayerThread.cs` | The thread can also be cast with the gamepad's right trigger (it previously had no gamepad cast). |

## What was validated, and how

Automated Play-mode test in batch Unity (`MraPlayTest.Run -saveFile mra_playtest.json`), driving the live
rig with a virtual keyboard and gamepad. Final run on the delivered scenes: **570 checks passed, 0 failed**. Covered:

- **Main route**: all 8 regions walked start to exit by input (run/jump autopilot), every region exit and arrival,
  every shrine (5 relics granted once, in order), every knot (7 legacy ids woken), Waymarks, the reveal, the ending.
- **Relic gates before/after**: the Cradle quake gate (holds before `knot:grip`, opens and the fallen crossing appears
  after), the Causeway thread lift (impassable without the Living Thread; with it: catch, hoist, step onto the
  ledge), the Terraces sluice (holds until Up at its wheel, then saved open), the Heights glide span (a running jump
  plus a Wind Leaf dash falls short; with the Glidecap Qori lands).
- **Every side chamber (38)**: prerequisite refusal without its relic, entry, its mechanic (plate + crate, 1-3-2
  light sequence with reset, rescue winch, sluice drain, thread anchors, timed thorns, bounce pods), the reward once,
  the return to the safe spot outside, the checkpoint rule, and nothing paid on a revisit.
- **Qvale**: in-place cutaways open while inside; rescued residents at home; a purchase (+1 heart, Amber spent) that
  survives a save reload; Scribble lists exactly the visited regions; found song shells unlock tracks; Waymark
  travel to the Causeway and back.
- **Saving**: every flag, relic and reward read back from disk; quake state on reload; respawn at the saved checkpoint;
  walking backward into the previous region; the descent sends Qori back to the Summit on direct load before the
  reveal and loads normally after; the ending doesn't replay.
- **Chart**: Qori's marker against every beat's authored map position (95 beats measured: mean error 0.0045 UV, max 0.0283, tolerance 0.03; `chart_correspondence` in the results); gamepad-only control with the mouse removed (View opens, stick pans, triggers zoom, shoulders
  switch region, Y centres, B closes), play resumes and focus returns after closing, zoom kept on reopening, a pad
  reconnect, and the final assembly listed only after the reveal (never a whole-world view before it).
- **Static route audit** (`Editor/MraRouteAudit.cs`): no riser over 1.2 u on a walkable piece, no slope over 1.2,
  only the three intended module gaps.

Not validated by the test: combat with the placed encounters (the route test disables them to test traversal),
human feel and difficulty, and the art read from a player's point of view. **A human play-test is the next step.**

## Platform and measurements

| | Status |
|---|---|
| Windows editor (batch) | Play test above, **570 checks passed, 0 failed** |
| Windows player (64-bit development build, `MraWindowsBuild`) | **Built and run**: 0 build errors. Measured 1920×1080 windowed, vsync off, i9-11900K / RTX 3070 Ti. See below. |
| PS5 | **not tested** |
| Xbox Series X | **not tested** |
| Xbox Series S | **not tested** |
| Switch 2 handheld | **not tested** |
| Switch 2 docked | **not tested** |

Windows player profile (`mra_profile_1080.json`; Qori placed at every beat for 3 s; Chart open 3 s in Qvale):

| Scene | Frames | Median ms | p95 ms | p99 ms | Max ms | Frames > 16.7 ms | Load ms |
|---|---|---|---|---|---|---|---|
| MR01 | 2157 | 2.44 | 52.88 | 83.17 | 107.7 | 696 (32.3%) | 1124 |
| MR02 | 16714 | 0.88 | 9.20 | 17.37 | 105.5 | 175 (1.0%) | 341 |
| MR03 | 32273 | 0.67 | 5.36 | 7.68 | 25.1 | 1 (0.0%) | 123 |
| MR04 Chart | 2916 | 0.63 | 5.73 | 6.23 | 6.7 | 0 (0.0%) | — |
| MR04 | 32158 | 0.72 | 5.62 | 6.25 | 16.3 | 0 (0.0%) | 98 |
| MR05 | 21322 | 0.71 | 6.07 | 33.02 | 101.7 | 277 (1.3%) | 113 |
| MR06 | 2056 | 5.02 | 52.14 | 60.08 | 103.6 | 706 (34.3%) | 427 |
| MR07 | 2421 | 4.82 | 52.11 | 76.95 | 104.1 | 654 (27.0%) | 151 |
| MR08 | 2113 | 6.65 | 52.07 | 82.29 | 103.7 | 642 (30.4%) | 101 |
| BASELINE A0 TestRoom | 420 | 4.49 | 51.93 | 52.76 | 103.3 | 99 (23.6%) | — |

The intermittent ~52 ms (and up to ~105 ms) frames appear at the same rate in the **existing, approved
`A0_TestRoom` scene** measured in the same build, and come and go in time rather than by region (Terraces and
Qvale ran 32 000 frames each with none). They are most likely contention on this machine (the Unity editor and a
batch Unity were running at the same time), but that is not proven: **repeat on a clean machine** before drawing
conclusions. Medians are 0.6–6.7 ms. Managed memory ~136 MB, reserved ~270 MB, graphics driver 480–590 MB; region loads
0.1–1.1 s. The development build is 1.26 GB because every `Resources` folder in the project ships (including the
uncompressed review maps); a real build needs measured per-platform import profiles. No compression format is inferred.

## Design decisions and deviations (please review)

1. **The Causeway thread module** (`flat-thread-gap`, 4 u gap) became a **thread lift**: a 1.2 u stone step at the foot
   of a 3.8 u unclimbable wall, with an anchor above the step. A 4 u flat gap is crossable by an ordinary running jump,
   so it wouldn't have needed the thread. The anchor sits over the step because the thread doesn't wrap and won't pull
   Qori into rock. The test's catch happened at 3.97 u, inside the live range of 4 but above the conservative 3.2.
2. **The Heights glide span moved from E06 to E10**, with the exit beat (MR06_N11) moved +15 u to (440, 270): E06 is a
   36 u stair rising 28 u; an 18 u gorge (beyond a running jump plus a Wind Leaf dash, measured) didn't fit it.
3. **Knot beats** are touch-to-wake with an innocent local quake ("The ground shifts."), not guardian fights. No working
   guardian prefab exists for the new route (Knucklebramble is placed as an ordinary encounter, per the schedule).
   The old anatomical stir captions (`Knots.Stir`) are never used.
4. **Pads**: every beat is flattened to its stored safe-pad width. Where a pad cut through a stair it could merge two
   risers into a 2.4 u wall; risers are now split back into ≤1.2 u steps automatically (audited).
5. **Encounters**: the 12 scheduled prefabs are placed on 12 u flat shelves at the scheduled centres (flyers 2.5 u up).
   Their behaviour on these shelves hasn't been tested.
6. **Shop prices**: the heart seed keeps the existing 60 Amber. Proposed, not balanced: weapon "sap edge" upgrades 80,
   sap vial 25, Chart pages 15, chamber Amber 25. Upgrades and the sap vial are **recorded flags only** — there is no
   tier or healing-item system yet.
7. **Rescues**: smith (Underarch Workshop), farmer (Farmer Shelter), lantern keeper (Lost Lantern, lights the street),
   traveller (Weather Cave), last climber (Watcher's Hollow). Names are the Qvale prototype's (Brannick, Marrow, Wick).

## Region notes

**MR01 The Cradle** (A0 + Body surfaces): 12 beats; Climbing Moss shrine; knot `grip`; the quake crossing (5.5 u
root gate under a sealed cap, ravine with catch floor and return steps, the fallen crossing after the quake). 6 caves:
Warm Crack and Moss Pocket (crate on plate), Pale Alcove, Root Gallery (thread), High Crevice (climbing walls),
Hanging Garden (glide). Art gaps: the fallen crossing uses the accepted pillar segment stretched as a slab; crates are
placeholder shapes (no accepted crate art).

**MR02 The Long Causeway** (Causeway kit): Living Thread shrine before the thread lift; knot `reach`. Underarch Workshop
rescues the smith; Silt Cistern drains by lever; Echo Vault is the 1-3-2 light sequence. The aqueduct/root-bridge
props (`Prop_Causeway_*`) are **not yet placed** as set dressing.

**MR03 The Terraces** (Terraces kit, A5 backgrounds, `BG_Sky_Terraces`, the accepted mill as a background landmark):
Bloomfall shrine; the sluice gate and its wheel; knot `spring`. Farmer Shelter rescue; Wind Shelf dash trial (timed
thorns, 1.2 s window). The Review 22 Terraces Far/Mid/Near are not used; A5 Mid/Near stand in.

**MR04 Qvale** (town kit, Qvale backgrounds and decor): six in-place cutaways (smithy, herbalist, Scribble's room,
residents' home, old man's home, musician's loft with a short climb to a song shell). Services and residents follow
saved rescues; lamps light after the Lost Lantern; the square cracks after a quake. No combat.

**MR05 The Ribwood** (A2 legacy kit): Wind Leaf shrine; cosmetic heaving rocks at the swell ledge (static collision);
knot `breath`. Lost Lantern rescue; Stone Drum bounce pods (Bloomfall).

**MR06 The Windward Heights** (A3 legacy kit): Glidecap shrine; the glide span (moved, see above); knot `bloom`; Weather
Cave rescue; Wind Window dash trial.

**MR07 The Summit** (A6 legacy kit; no A6 sky exists, so `BG_Sky_Terraces`): knot `sight`; the reveal on the Reveal Ledge:
the camera eases out over the very terrain Qori crossed (nothing is moved or swapped, Qori stays on the ledge), then
the accepted `Reveal_Face_01_Brow` and the Batch 11 final-assembly concept (review-only). Saved as
`mra:reveal-complete`; the descent's exit requires it. Watcher's Hollow rescue.
**Art gap**: the prototype has no lake/reed/waterfall terrain art — the Summit is built from the A6 kit, so the
reveal's "same lake, reeds and falls" continuity exists only in the data, not yet in the art.

**MR08 The Descent — DESIGNER SPOILERS** (A4 legacy kit): post-reveal only (direct loads before the reveal go back to
the Summit). The careful ending at the final chamber (Up at the roots): the accepted `Ending_Careful_01_HeadLift` and
`_02_Lap` stills, saved `mra:careful-ending`, healed knot afterwards. Two quiet alcoves. No combat.

## The Chart

Regional only: one unlocked region's relief painting at a time, with separate UI overlays — walked paths (or a bought
page's), Qori's marker (projected along the current route edge by arc length, updated as he moves; pinned to the
entrance inside a chamber), the lit Waymark, shrines whose relic is held, and side passages only once discovered.
Zoom is clamped so the drawing always fills the frame. Waymark travel works from a lit Waymark to another region's.
After the reveal the final assembly joins the list. Control hints switch between keyboard and pad wording; they are
text, not platform glyph art (**follow-up**: per-platform button glyphs). Text uses the legacy built-in font, which
lacks most symbols on consoles — localizable, scalable UI text and a shipping font are needed.

## Remaining production gaps

- **Guardians**: no working boss behaviour exists for the new route; knot beats are touch-to-wake.
- **Review 22 redos** still open: Terraces Far/Mid/Near, Reveal 02/03, Ending 03 (none used here).
- **Final-assembly art**: the reveal uses the Batch 11 Chart concept (review-only); a production final-assembly painting
  matching the approved seated model is needed, plus Summit lake/reeds/falls terrain art for reveal continuity.
- **Placeholder art**: crates (shapes), levers and winches (`Mill_Wheel`), sequence lights (`GlowPod_Light_Off`),
  bounce pods (`Carrier_SeedPod_A2`); no accepted art exists for these controls.
- **Set dressing**: the Causeway's aqueduct, root bridge and spring props; landmark placement per the paintings.
- **Systems**: weapon tiers and healing items behind the shop flags; balance pass for Amber and prices.
- **Music**: held for audition; placeholder tunes only.
- **Platforms**: every console target and mode is untested; validate Series S and both Switch 2 modes early.
