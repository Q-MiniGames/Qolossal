# Cistern layered reference — 7 November 2026

The user accepted the researched cave direction: enclosed rooms use appropriately close rear walls; open connected caverns share a coordinated distant scene; passages explain changes of space; middle-distance landmarks make traversal readable. This establishes an art direction, not approval of every new painting or an all-cave rollout.

The first local reference is the existing A04/A05 Cistern review. It uses a new quiet reservoir panorama and a separate rocky shoreline with ruined waterworks. One common registration and lighting grade spans both rooms and their matching passage ends. The middle landmark is drawn once, not tiled. It does not touch gameplay collision. Existing foreground terrain remains registered to the physical world.

Screen movement relative to terrain is 45% horizontally / 55% vertically for the far view and 88% / 94% for the middle landmark. Neither is pinned to the camera. These are trial values, not settings claimed to come from Ori or Hollow Knight. F8 or left-stick click switches between the new single painting (A) and layered composition (B); F6 returns to the approach. The persistent Editor preview starts in B and survives Play/Stop. This preview does not ship in a player build.

Saved art, generated with the built-in image tool:

- `Assets/Art/Prototypes/CisternLayers_20261107/Reservoir_Far.png`: new 2172×724 panorama with unique landmarks and a broad calm reservoir.
- `Assets/Art/Prototypes/CisternLayers_20261107/Waterworks_Middle_v2.png`: refined 1536×1024 transparent shoreline landmark. The initial floating-looking `Waterworks_Middle.png` is retained but unused.
- Prompt records: `PROMPTS.md`. Accepted source artwork was not overwritten or resized.

At the 3840×2160 camera with half-height 5, density is 216 screen pixels per world unit. The middle asset's 6.8×4.5333-unit placement requires 1469×980 pixels, which its native source meets. The broad 72×24-unit far placement would require 15552×5184 for one source texel per screen pixel. That source is still an atmospheric composition draft and does NOT pass native 4K acceptance. Do not label the trial final production art. Finalization needs higher-resolution coordinated sections, measured filtering and platform budgets; enlarging the bitmap is not an acceptable substitute.

`verify_render.cs` checks all 63 passage materials survive rock refresh, all 252 room/passage sample comparisons match, and materials restore when the trial is removed. `measure.py` measures independent layer motion from rendered images. `playtest.cs` uses an isolated save, actual input and render-end camera measurements through walking, passage traversal, manual look-down and a glide. `QA.json` verifies camera coverage in tested frames and all 13 saved scene hashes. This is a bounded walking/gliding review, not full-height coverage acceptance for the entire A04/A05 network.

Camera framing remains the approved 15% main ground / 40% platform and solid ledge policy. Automatic cave edge look-down stays disabled. Terrain, collisions, scene topology, map data and source manifests are unchanged.

Windows Unity Editor checks only. Standalone builds, physical controller handling, console performance, PS5, Xbox Series X/S and Switch 2 handheld/docked are not tested. No 60 FPS or hardware-readiness claim. Original Switch is excluded.

Verification note: the inherited generic 85% head-height assertion reported a false failure during deliberate manual look-down. Existing `CameraFollow.PreviewScreenLimit` intentionally allows the head at 90% in that mode. Final QA re-evaluates the recorded manual samples against that existing safety limit; the fixture has been corrected for future runs. No camera behavior was changed for this art trial.

Research basis: Moon Studios' continuous-world editing explanation (https://mcvuk.com/development-news/unity-focus-making-ori-and-the-blind-forest/), Team Cherry's place-led environmental design (https://www.acmi.net.au/stories-and-ideas/from-ludum-dare-to-pharloom/), and Motion Twin's background/readability/transition breakdown (https://www.gamedeveloper.com/production/art-design-deep-dive-giving-back-colors-to-cryptic-worlds-in-i-dead-cells-i-).
