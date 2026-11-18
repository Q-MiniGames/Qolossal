# Polish 70%: what to try (work of the night of 2-3 Nov 2026)

Items 1, 2, 4, 5, 8 and 10 passed scripted checks in the real game, and scripts prove logic, not feel. Items 3, 6 and 7 are compiled and ran without errors, but no script tested them. Nothing is committed yet. Open the project in Unity and press Play, or run the Windows build: `C:\UnityProjects\Qolossal\Builds\Polish70\Qolossal.exe` (it uses a test save, only if you start it with `-saveFile polish70_try.json`; otherwise it uses your normal save). The build was made before the danger zones and doesn't have them, so try those in Unity.

**Keys for testing, in the editor and development builds:** F9 shows every hit box. F10 switches danger zones: off, big attacks, all.

## Your verdict needed

| # | Try this | Look for | What changed |
|---|---|---|---|
| 1 | Press Tab, right-stick click, keys 1-4 | Nothing happens; no weapon box on screen | Weapon menu removed |
| 2 | **Swing at enemies (F9 on)** | Does the hit land where you see the staff? **Does the reach feel too short?** | Hits now follow the drawn staff, about 1.5 u from Qori. The old invisible staff tip was about 2.2 u away from the drawn one, and fights were tuned with it |
| 3 | Fight a Sentinel from the front | The charged sweep breaks its shield; normal hits don't | New rule, since the spear is gone |
| 4 | Take a hit | Qori flinches and flashes | He had no visible reaction before |
| 5 | Die | He goes down, a short beat, a fade, then the checkpoint. Does the 1 s feel right? | Was an instant teleport |
| 6 | Run, on flat ground and slopes | Do the feet still skate? Does the run look too frantic? | Run clip plays 1.28x faster. Feet now cover about 63% of the ground, was 49%. `runPace` on the Qori frames prefab tunes it |
| 7 | Reverse direction at full speed | He keeps facing the old way until he slows, instead of running backward | No skid clip yet, this is the stand-in |
| 8 | **Danger zones:** Sluice Warden (A08), Root Snapper (C01), Sluice Crab (A06). F10 to compare | Help or clutter? Colour and strength? | Your telegraph test. Pictures: `zone_*.png`. Default shows guardians and mini-bosses only |
| 9 | Walk through Ribwood | Do the new 4K backgrounds look right? Parallax not tuned yet | Swapped in; pictures in `ribwood/` |
| 10 | Enter the underground in the build | Load is much quicker; creatures look as before | See numbers below |

## Numbers (Windows, RTX 3070 Ti, 3840x2160; says nothing about any console)

| | Before | Now |
|---|---|---|
| Underground, texture memory | 4,612 MiB | 1,683 MiB |
| Underground, worst frame | 19.4 s | 64 ms |
| Surface regions, texture memory | 1.7-2.1 GiB | 1.2-1.7 GiB |
| Qori's frames | about 1.4 GiB | 84 MiB |
| All creature frames | about 3.4 GiB | 675 MiB |
| Build size | 7.3 GB | 4.6 GB |

The "before" column for the underground already had Qori's atlas in it, so the true before was worse. Full table: `memory_baseline.txt`. The source PNGs are unchanged, but about 3,000 `.meta` files changed.

## Issues found, for you to know

- **Combat bot:** 23 wins out of 39 fights, was 22. It still can't beat the Sluice Warden as a skilled player. Two fights got worse because the honest staff is shorter: Hail Sentry (perched, so the bot needs a jump or the staff shot) and Root Matron (weak point 4 u up). The bot doesn't climb the fight ledges, so only your hands can judge these.
- **Sluice Warden's sweep:** the danger zone showed the strike box starts about 1 u past the claw as drawn. To correct in the per-enemy review.
- **Root Matron and floods have no zone yet:** her shockwave and seeds and the Warden's flood aren't strike boxes, so they need their own shapes.
- **Old polish test:** 49 pass, 6 fail. Five are out-of-date checks (the test save's name, the loft, home interiors, the layout name, the drift levels). The reveal check found no reveal object when it looked, although the Summit scene has one, so please watch the reveal once.
- **Staff shot:** nothing on screen explains it now, until the controls page exists.
- **C: drive:** you freed it on 3 Nov (458 GB free), so the cache and builds are back on C:.

## Still open for gate 70%

Per-enemy box review and fairness table, cave minimap, filmed ambient census, climb capture tool and Heights and Summit briefs, repaired regression tests, and your full play from start to ending.

## Cave minimap (added 3 Nov, WS4)

Passed `CaveMinimapCheck` (29 checks, logic only); captures in `minimap/`. Not in the `Polish70` build, so try it in Unity, in `Qolossal_Underground`.

1. Press Tab or right-stick click: small, medium, large, off. Does small feel big enough? Is it in the way at any size? It sits just below the Amber count.
2. Walk from A01 onward: each room appears as you enter it, and every exit shows the beginning of its passage. Passages still unexplored start in amber: do they make you want to go there? Exits to the surface get the same stub, drawn rather than real.
3. A03: the red bar on a shut gate goes when the gate opens. A08: the red diamond goes when the guardian falls.
4. **Open the Chart with a save that has few rooms found.** Undiscovered caves are now hidden there too. This is the plan's rule, but it's a visible change: say if you want the surface cave mouths back as hints.
5. The Heart hall: do the grilles read as closed, so A, C, D and E don't look joined?
6. Rewards are marked as soon as a room is found, including ones you can't reach yet (dimmed if a lesson holds them). Helpful, or too much hand-holding? Easy to turn off.
7. Windows, pause, the Chart, cutscenes: the map should be hidden during all of them.

## Backgrounds and parallax (added 3 Nov, WS7)

Passed `ClimbCapture` (0 faults in 7 regions) and `CaveDepthCheck` (0 failures, 0 runtime errors); renders and numbers only, nobody has played it. Not in the `Polish70` build, so try it in Unity. Pictures: `climb/before/` and `climb/after/`, one sheet per region at 0, 15, 30, 50, 75 and 100% of the climb, top row as played, bottom row the planes alone (the bottom row of `before` still shows terrain, from a bug in the tool's first version). `cavedepth/` has three rooms at two camera places.

1. **Any big cave room, walk and jump (the main new thing):** the far backdrop now passes at 0.7 of the terrain's speed and the near one at 0.88. Does it read as depth, or make you slightly seasick? Too subtle? Both speeds are one line in `CaveDepth`.
2. **Walk through a passage between rooms of the same route:** the far backdrop now carries through with no jump. The colour change between routes is still there. Does the pattern's repetition look worse now it moves? A01 shows it most.
3. **Climb the Heights, then the Summit:** the old sunset paintings are back, unchanged. Codex's Heights art is out (you rejected it). Art comes first, so vertical movement is gentle: the far mountains fall away 3 u over the climb and the near hills stay in the frame until about 85%. The climb check warns about the Near's low speed; that's expected.
4. **Climb the Ribwood:** I changed nothing after the swap. It's placed by the same rules as the Terraces, and its numbers sit next to the Causeway's, which has about the same climb. Say if the top of the region feels empty: the Mid has gone by 100%, leaving a tree's tip and the cloud sea.
5. **Don't send the Heights or Summit prompts:** they're on hold until rewritten with no long columns.

Not checked: any console, frame time, or how the climb feels in motion. `ParallaxLayer` should no longer allocate per frame, from reading the code; I couldn't measure it, because the memory reading in batch mode also shows 0 for a known allocation. Frames in the five regions whose scenes I didn't tune match to 0.02 of 255 at most, so their look is unchanged. The new shader, `CaveDepthBackdrop`, still needs a console build to prove itself.
