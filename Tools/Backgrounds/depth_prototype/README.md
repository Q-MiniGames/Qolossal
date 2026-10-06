# Depth parallax prototype: MR03 Terraces (6 Oct 2026)

**Status:** only the Terraces has changed; every other region is exactly as before. Waiting for your play-test, and not committed.

## What changed

- **`ParallaxLayer` gains `depth`.** Depth is the layer's distance from the camera, with the terrain at 1. Depth 0 keeps the old behaviour exactly.
  - **Horizontal speed:** on screen a layer moves at 1/depth of the terrain's speed (follow.x = 1 − 1/depth).
  - **Zoom:** zooming behaves like a camera pulling back. Zoomed out by k, a layer keeps D/(D+k−1) of its on-screen size relative to the terrain. At the lookout (size 5 → 6.8):

    | Layer | Keeps of its on-screen size |
    |---|---|
    | Far | 99% |
    | Mid | 97% |
    | Near | 93% |
    | Terrain | 74% |
    | Foreground | 68% |

  - **`uvRect`** draws part of a texture: a decor sprite from its sheet. The files themselves are untouched.
- **The Terraces (`MraWorldBuilder.DepthLayers` / `DepthExtras`, `DepthRegions = { MR03 }`):**

  | Plane | Art | Height | Depth | Speed (× terrain) | Sinks over the climb |
  |---|---|---|---|---|---|
  | Sky | `BG_Sky_Terraces` | — | camera-locked | — | — |
  | Far | `BG_Terraces_Far` | 10 u (1:1 at 1080p) | 25 | 0.04 | 0.2 u |
  | Mill | `BG_Terraces_Mill_Landmark` | 2.8 u (was 5.5) | 14 | 0.07 | — |
  | Mid | `BG_Terraces_Mid` | 10 u | 10 | 0.10 | 0.5 u |
  | Near | `BG_Terraces_Near` | 10 u | 5 | 0.20 | 1.0 u |
  | Foreground | 11 clumps of accepted Terraces/A5 decor (dry grass, olive fern, hay tuft), 3–4 u tall, darkened | top ~55% shows | 0.75 | 1.33 (passes in front) | — |

  - The far and mid planes now use the accepted Direction A Terraces set; the brown A5 strips and the hazed far ridge are gone from the Terraces.
  - The foreground clumps stay in the bottom fifth of the frame, under the walk line, and skip spots with a big drop nearby.
  - A **lookout zoom** at Mill Ridge (size 7, lift 1.5): the region had none.
  - Jumps and landings still don't move the background; climb sinking stays smoothed over 2.2 s.

## How it was checked

| Check | Result |
|---|---|
| Polish checks (incl. parallax) | 90 passed, 0 failed |
| Full play test | 656 passed, 0 failed |
| Accepted art | all 1132 files byte-identical |
| Devices | **not tested** (consoles, and PC above 1080p) |

## Evidence

All are recorded runs: Qori runs and climbs by keyboard input, at a fixed 30 fps, rendered at 1280×720.

| File | Shows |
|---|---|
| `terraces_compare_run_climb_zoom.webp` | Current against depth on an identical path: a run, a 21 u climb, and the lookout zoom out and back |
| `terraces_depth_foreground.webp` | The foreground plants passing in front (x 290–360) |
| `compare_stills.jpg` | Matched frames from both runs |

## Try it

1. Open `MRAtlas_MR03_Terraces`, then Play.
2. Run past Mill Ridge to feel the lookout zoom.

## Knobs

All are in `MraWorldBuilder`; rebuild the Terraces only with `MraWorldBuilder.BuildRegionsBatch -mraOnly MR03`.

- **Depths:** `FarDepth`, `MillDepth`, `MidDepth`, `NearDepth`, `ForegroundDepth`. A larger depth means slower and calmer.
- **Climb sink:** `ClimbSink`.
- **Foreground:** density (the step of 14–26 u), size (3–4 u) and tint (0.5) in `DepthExtras`.
- **Turning it off:** remove MR03 from `DepthRegions` to go back to the one-speed treatment.
