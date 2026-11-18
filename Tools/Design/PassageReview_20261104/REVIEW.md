# Cave scenery and passage continuity — 4 November 2026

The user identified the repeated scenery and passage join as the problem in the supplied recording. The earlier shader pinned the composition almost entirely to the camera and clamped its tiny drift. Platforms moved past an apparently stationary painting; every passage then cut to the older dark mineral sheet.

The existing paintings now have stable room registration with 35% horizontal and 12% vertical terrain motion. Vertical registration follows each room's authored checkpoint height so tall rooms can retain distant scenery. A room keeps its single central painting and distinct side/vault paintings; these landmarks do not tile. The low-contrast perimeter field remains a texture-based extension. No source image has been repainted or enlarged.

All 63 authored passages now sample the exact adjoining room compositions. Their blend is based on distance outside the two actual room bounds, so each mouth matches its room and the connecting space gradually changes toward the other. Color properties are declared identically in both shaders to preserve linear-color conversion. The older translucent room-only mineral overlay is hidden during this presentation because it otherwise leaves a rectangular tint seam.

`CaveDepth.Create` installs `CavePassagePresentation` at runtime. The generated presentation restores replaced materials and overlay visibility when destroyed. It changes no saved scene, collider, checkpoint, gate, route, discovery ID or map. `build_connections.py` derives the 63 runtime registrations from the inspected backdrop envelopes and canonical connection data, with unique matching and endpoint membership assertions. Regenerate these registrations if future authorized topology work changes the passage envelopes.

Verification is recorded in `QA.json`, `seams.json`, `parallax.json`, and `Play/result.json`. Both ends of all 63 passages are compared against their room shaders at 216 screen pixels per world unit (the game's closest native 4K density). Image correlation measures the scenery displacement during a two-unit camera pan. An isolated-save Play-mode test walks Qori through A04–A05 and back, opening gates through their public signal only in that test. Native 3840×2160 and 1920×1080 captures show the resulting presentation. All 13 scene hashes remain unchanged.

Editor-only preview tools should temporarily call `CavePassagePresentation.Create()` and destroy the returned generated object after capture; runtime initialization does this automatically. This rendering fix does not require a topology/map rebuild.

Supported targets remain Windows, PS5, Xbox Series X/S and Switch 2. Verification here is Windows Unity Editor only. Player builds, PS5, Series X, Series S, Switch 2 handheld/docked, physical controllers, CPU/GPU performance and peak memory remain untested. Texture import profiles are unchanged.
