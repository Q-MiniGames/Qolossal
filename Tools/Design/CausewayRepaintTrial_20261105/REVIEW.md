# Causeway painted terrain trial — 5 November 2026

**Rejected and reverted later on 5 November.** The user requested the original rock artwork with only a darker walking edge. The repaint invocation is removed; retained art/code are historical drafts. Current implementation and verification: `Tools/Design/CausewayEdgeTrial_20261105/REVIEW.md`.

The user requested an in-game test of the darker painted concept. A new native bedrock painting now replaces the visible rock body of upper Causeway terrain. The original walking lip, alpha edges, terrain positions, collision and all four cave entrances are preserved. Backgrounds, caves, houses and the other six surface regions receive no new artwork.

The trial is active automatically in `MRAtlas_MR02_Causeway` through `CausewayTerrainTrial`, called by the existing scoped surface-presentation hook. No scene files or original painting assets were overwritten. Removing the single `CausewayTerrainTrial.Apply` call in `SurfaceTerrainContrast` returns Causeway to the previous tint trial. The new artwork is blended beneath the walking rim over 0.06–0.38 world units; existing cave masks return precisely to the original paint around entrances.

Source: `Assets/Resources/Terrain/CausewayRepaintTrial/DarkBedrock.png`, generated with the built-in image tool. Exact prompt: `PROMPT.md`. Native/imported size: **1254×1254**, uncompressed, sRGB, mipmapped, with no NPOT resize or upscaling. Texture coverage is 4.8984375×4.8984375 world units at **256 native pixels/world unit**. At 3840×2160 with camera half-height 5, the required density is **216 px/world unit**; this coverage therefore requires at least **1059×1059** source pixels. The imported painting exceeds that requirement. The existing rim keeps its original native mapping.

Source borders overlap by 20% in the shader, giving a 3.91875-unit repeat period without mirroring the rock shapes. Explicit texture gradients retain correct mip selection at repeat seams. New pixels are painted source detail, not an enlarged concept screenshot.

Verification: `QA.json`, `import.json`, matched static views in `Preview/`, and actual 1080p/4K Play-mode views plus a virtual-gamepad walk in `Play/`. All 13 playable scenes and the camera retain their pre-trial hashes. The original source assets remain intact. This is a reviewable playable trial, not approval to repaint every region.

The user handoff opens Causeway in Play mode at its entry, using a separate test save. Normal save routing and temporary Editor options restore when Play mode stops. No test inputs or invulnerability remain active for the user's session.

Platform targets: Windows PC, PS5, Xbox Series X and Series S, Switch 2 handheld/docked. Tested here: Windows Unity 6000.3.19f1 Editor, URP, 1920×1080 and 3840×2160, simulated gamepad movement. Standalone builds, physical controller coverage, CPU/GPU frame times, total peak memory, and all console modes are not tested. The four texture samples used for overlap require platform profiling; 60 FPS remains a target.
