# Upper-terrain contrast — 4 November 2026

User scope: upper terrain only. Backgrounds and caves are excluded.

The seven surface regions retain their painted palettes, with darker rock bodies and a lighter walking edge. Ribwood already has dark earth, so its body receives a smaller adjustment and its moss edge a slight lift. The transition extends 0.9 world units below the authored walkline; it does not add an outline or modify the silhouette.

`SurfaceTerrainContrast` applies only to generated MRA route meshes and outdoor traversal floors/overhangs in the exact seven surface scenes. Cave-owned terrain, underground rooms, house interiors, props and all background renderers are excluded. Each region's cave-opening spans protect the existing mouth/rim colors, including overlapping terrain belonging to a different object. Two units of padding remain unchanged, followed by a four-unit smooth transition outside the protected span.

This is rendering only: no scene save, mesh position/index/UV0/alpha change, collision change, map change, camera change, texture resize, source repaint or import-setting change. The existing terrain generator has one scoped rendering hook. Its generated mesh gains an extra two-component UV channel containing shading coordinates. The material preserves URP sprite lighting and the normal/forward passes, using mesh transforms rather than SpriteRenderer-only state.

## Review evidence

- `Before/` and `After/`: 21 matched native 3840×2160 surface views and 37 matched 960×540 cave-mouth checks.
- `comparison.jpg`: same-camera before/after comparison for Cradle, Causeway, Ribwood and Summit.
- `pixel_comparison.json`: cave-protection comparisons.
- `PlayFinal/`: 1080p and native 4K captures in all seven running surface scenes; actual rendered-frame advancement, complete cave-mask presence, errors and save/settings restoration are recorded in `result.json`.
- `scope_audit.json`: renderer scope, additional vertex-buffer bytes and shader diagnostics.
- `accepted_source_hashes.json`: all 1,351 canonical accepted source files.
- `QA.json`: final verification summary and protected scene/camera hashes.

Initial diagnostic captures and `Play/` are historical intermediate runs. The final cave protection also handles terrain rebuilt from OnEnable before its scene is fully loaded. Only `After/`, `PlayFinal/` and final QA describe the delivered state.

## Resolution and platform scope

The 4K review target remains 3840×2160. At the closest camera half-height of 5, the requirement is 216 screen pixels/world unit. Texture dimensions, original UVs, source detail and import profiles are unchanged, so this color treatment adds no texture-resolution requirement or bitmap memory. The prior environment audit's three approved source-density exceptions remain.

Targets remain Windows PC, PS5, Xbox Series X and Series S, and Switch 2 handheld/docked. Verification here is Windows Unity 6000.3.19f1 Editor, URP, at 1920×1080 and 3840×2160. No standalone build, physical controller test, GPU/frame-time qualification, total peak-memory measurement or console hardware test is claimed. The shader uses the existing draw geometry and adds a bounded per-fragment cave-protection loop (up to eight spans in the current regions); hardware performance still requires measurement. The additional UV channel is measured in `scope_audit.json`, not presented as total runtime memory.

No scene-derived map refresh is required because all 13 playable scene files remain byte-identical and no topology or terrain positions changed.
