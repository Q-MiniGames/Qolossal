# Causeway walking-edge trial

User request: revert the new dark bedrock painting, then darken only the walking edge marked green in the screenshot.

The new painting is no longer applied. Causeway's original textures and body brightness are restored. The narrow upper rim receives a neutral linear-light multiplier of 0.22; it fades smoothly to the unchanged body between 0.04 and 0.26 world units below the walkline. At the closest 4K view this is a maximum 56.16-pixel depth, with unchanged native UVs, texture dimensions and alpha silhouettes. Other regions keep their prior presentation.

`CausewayWalkingEdge.shader` uses the original 2D lighting path. Cave protection keeps the original entrance colors, but permits the surface route above the cave roof to receive the rim treatment. No geometry, collider, scene, map, camera, background, cave-owned renderer or source artwork was edited. `CausewayTerrainTrial.Apply` is no longer called; its painting is an unused historical draft.

Evidence: `Preview/` contains native 4K route views, matched cave views and the marked slope at 1080p. `Play/` records a short walk with actual frame advancement and isolated saves. `QA.json` records source-scene hashes and protected entrance/body comparisons. The user handoff resumes Causeway near the screenshot location (x=92.24025), using a separate test save.

Platforms: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld/docked. Tested here: Windows Unity 6000.3.19f1 Editor, 1080p/4K and simulated gamepad movement. Standalone builds, physical controllers, CPU/GPU timing, peak memory and console hardware are not tested. No import profiles or native sampling density changed; 60 FPS remains a target.
