Qolossal: Codex, we need your eye as an artist. Come up with a better way to make backgrounds feel right during long vertical climbs, then show the user. This is an ideas round: propose and prove, install nothing.

Project (read only for this job): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\VerticalParallaxIdeas\

## The problem
Qolossal is a 2D side-on platformer. The frame is 10 world units tall, and a single region is a climb of up to 216 u (Heights 216, Ribwood 180, Causeway 158, Summit 154, Terraces 146). When Qori climbs, the player should feel the height, with near things passing and far things hardly moving, as they already feel when walking sideways.

Our answer so far is one very tall Near painting (up to 3840x8192) sliding down the frame at 16-20% of the terrain's speed. To fill that canvas with things that stay grounded in every frame, it keeps turning into long columns, spires and diagonal bands.

## What the user has decided
- **The art comes first.** If vertical parallax would ruin the art, the user would rather leave it out.
- **The long columns style is not acceptable any more.** That includes long diagonal slabs and bands.
- Your Batch 38 Heights art was rejected as very bad. It passed every number and still failed as a picture, so a passing number proves nothing here. See `Art\Codex_v2_full\Batch38\QA\unity_motion_strip_12.jpg`, and don't repeat it.
- The Descent's background is dropped.

## The bar
`Assets\Art\Codex\Backgrounds\BG_Heights_Far.png`, `BG_Heights_Near.png` and the sunset sky in `Assets\Art\Codex\Backgrounds\` are what the Heights have now, and the user chose to go back to them. See `Tools\Design\PolishGates\70\climb\after\MR06_climb.jpg`. Any idea must look at least this good in a still frame, and it must also work in motion.

## What to come up with
You choose. **Don't feel limited by the current system, including the idea of one tall painting per plane.** We can write new code and change how planes move, as long as the result is reliable. Think about what a great hand-painted 2D game would do. For a start only, and please beat these:
- Several ordinary frame-sized paintings, one for each altitude band, handing over as the climb goes on, instead of one tall canvas.
- Separate near elements (crags, trees, clouds, birds) placed by height, each passing the frame on its own, instead of a continuous plane.
- Cloud and weather layers that the climb passes through, so height is felt through air rather than rock.
- A distant view and sky that visibly change with altitude: the valley sinking, the horizon widening, the light changing.
- Terrain-side framing, so the sense of climbing comes from the foreground, and the far art can stay nearly still.

## What the user wants to see, for each of your best 2 or 3 ideas
1. The idea in two or three plain sentences, and why it avoids columns.
2. **A real painted proof, in a game frame**, on the Heights: at least three heights on the climb, with Qori and the region's real terrain, from Unity renders or `Tools\Backgrounds\proposal\mattes\MR06_mid_foreground_rgba.png`. Label it clearly as a proof. Don't use grey boxes: the user judges the art.
3. **A motion proof:** a short GIF or video of a section of climb, moving at the speeds the idea would really use. One strip of at least 12 frames as well.
4. What it would take: how much new painting per region, what code would have to change, and roughly how much texture memory compared with today's five-plane stack, about 713 MiB in the Editor. Keep Series S and Switch 2 handheld in mind, and mark device tests "not tested".
5. Its weak points, honestly. For instance: where do things pop, repeat or float? How does it behave when the player falls fast, stands still for a long time, or zooms out at a lookout?

Then recommend one idea and say why. Also say whether it could work in the other regions, and whether it would help the Causeway's tall rock pillars, the Cradle's and Terraces' spires and the Ribwood's tall trunks, in case the user wants those changed. They're undecided, so they aren't a style reference for this round.

## Rules
- **Install nothing.** No writing under `C:\UnityProjects\Qolossal`, no scene saved, no commit. Unity renders only with no editor open on the project, and without saving anything. If Unity is open, work from the existing renders and mattes, or wait.
- Every proof is painted for this job. No upscaling or stretching, and no generator alpha as a mask: draw the silhouettes.
- Nothing floats or ends in a straight cut in any single frame. Look at each frame as if it were the only one.
- Mystery rule: backgrounds show sky, weather and distant land only. Nothing may read as a hand, finger, knee, lap, chest, rib, limb, shoulder, face, brow, eye, lid or lash. Check at full size, at 10% and blurred. The world's secret must stay hidden.
- Read for context only: `Assets\Scripts\Environment\ParallaxLayer.cs`, `Assets\MountainReliefAtlas\Editor\MraRegion4K.cs`, `Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md`, and section 2 of `Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md`.

## Finish
Write `INDEX.html`, one page the user can open to compare the ideas side by side with the motion proofs playing, and `HANDOFF.md`, short and in plain language. Then stop and wait for the user to choose. Nobody builds anything until then.
