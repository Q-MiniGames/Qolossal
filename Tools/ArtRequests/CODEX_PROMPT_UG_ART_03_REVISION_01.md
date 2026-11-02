# Codex brief: underground art 03, revision 01

Date: 2 November 2026. I checked your delivery. The technical work is good: colliders match your baseline, the hooks all work, and the cave kits, thorns, tall rock, anchors, pods, valves and spills are right. Keep all of those unchanged. Fix the five things below and nothing else.

UG_ART_03's "Before you start", technical rules and "may not change" list still apply. Back the scene up to `Tools/IncomingArt/Codex_v2/UG_Art_03/revision_01/before/`, and audit against it.

## 1. Gust (`Shared/Gust_01.png`)

**The problem:** it's a grid of identical claw marks, clipped hard at the lane's edge. In a narrow lane such as PC12 the cut marks look like white spikes, which reads as a hazard that hurts. Gusts only push Qori back.

- Long, soft, flowing wind lines, thin and tapered, that run off the edges and continue into the next tile. No separate repeating marks, and no sharp tips.
- Must wrap both ways with no visible grid at 1.1 x 12 u and at 16 x 4 u, at all three opacities.

## 2. Shelter (`Shared/Shelter_01.png`)

**The problem:** in PC12 it's a heavy black dome, darker than the room, that looks like something to stand on.

- A thin lip or brow at the top, and a very faint recess. Much less mass and no flat, bright or solid-looking top. Qori and the checkpoint behind it must stay fully visible.

## 3. Backdrops: A, E, MR02, MR03 only

**The problem:** at room scale these read as striped curtains, with strong vertical bars repeating across the room. MR03's red is also too saturated, and competes with its terrain and platforms.

- Repaint `Backdrop_Far` and `Backdrop_Near` for those four sets. Keep the same canvases, `ppu` and luminance limits.
- Replace regular vertical bars with irregular cavern depth: uneven masses, varied widths, and shapes that wander. Average brightness per column must stay nearly flat. Report the standard deviation of column means per file, aiming for 1.0 or less.
- MR03: keep it warm, but a dim, desaturated brown-red.
- Keep C, D, H, MR01, MR05, MR06, MR07 and P as they are.

## 4. Room props: A08, C08, D08, MR01_C02, MR02_C03, MR03_C04, HEART

**The problem:** many props hang in empty space in the middle of the room, such as the wreaths, the skull shape, and the window in A08. They look pasted on.

- Every prop must grow out of a wall, the ceiling or the floor, with its base hidden at that edge. Edit the room's JSON and the scripts that write it.
- Drop any prop that can't be attached, and repaint only if a room would be left with fewer than 3 props.
- Keep them out of jump paths and away from platforms, and low in contrast. Leave door surrounds as they are.

## 5. The big heart (`H/`, `quarterlight`)

**The problem:** the quilted membrane reads as kaleidoscope wallpaper, with visible repeats and a mirrored seam. It doesn't look like a huge living heart, and awake it's a flat gold oval.

- Paint one quarter as a single painting, with no quilting: large muscle forms and veins, darker toward the rim, with a lit centre.
- **You may use the master's native size as the canvas.** It's `fit: true`, so it stretches to 12.5 x 23 u. Keep the aspect ratio close to 1 : 1.84, sides a multiple of 4, and no upscaling beyond what `fit` does. The soft result is fine for this piece.
- It must look right mirrored into four, with no obvious seam where the straight edges meet, both asleep (dim grey) and awake (warm amber). Check both tints, because the game colours it at runtime.

## Deliver in `Tools/IncomingArt/Codex_v2/UG_Art_03/revision_01/`

- `CODEX_REVISION_01.md`: what changed, sizes, formats and memory, anything you couldn't check.
- 3840x2160 room captures, before and after: A04, A08, C08, D08, D05, E03, E05, HEART (asleep and awake), MR01_C02, MR02_C03, MR03_C04, MR06_C01, PC12.
- Gust at 1.1 x 12 and 16 x 4 u at all three opacities, beside Qori. Each repainted backdrop repeated 3 x 3.
- **A scene audit:** only these changes. Every other file in the kits, and all colliders and gameplay, must be unchanged.
- **Platform QA:** as before. Don't claim any console, frame-rate or hardware result you didn't measure.

Install everything, build and run the checks before you stop. Then stop and wait for the user's verdict.
