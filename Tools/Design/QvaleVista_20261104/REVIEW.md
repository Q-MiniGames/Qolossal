# Qvale vista camera correction — 4 November 2026

Qvale's street vista retains its authored 7.6 orthographic half-height and 1.2-second zoom transition. The defect was an additional 3.4-unit upward lift applied by `VistaZone.LateUpdate` after `CameraFollow` had already positioned Qori low in the expanded frame. At x129 the rendered feet reached viewport -0.07226449, leaving only the top of Qori visible.

`CameraFollow.ConstrainVistaLift` limits the final vista offset using the actual collider bounds and final zoom. Grounded framing retains the approved 15% foot position; the existing 8% airborne lower safety margin and upper head margin protect jumps and falls. `VistaZone` removes precisely the applied offset on its next Update, and `CurrentLift` reports that applied value for existing diagnostics. Suspended cinematic cameras and disabled follow cameras retain their existing ownership.

No scene, terrain, cave, map, asset import setting or artwork is changed. `scene_integrity.json` checks all 13 scene hashes against the canonical inventory; map regeneration is unnecessary for this camera-only change.

## Validation

`before/result.json` reproduces the failure. `after/result.json` records the corrected street positions, normal jump, high launch, walking through both ends of the zoom zone in both directions, normal zoom restoration, and the other five authored lookout vistas. Measurements come from actual camera render-end callbacks after the vista's LateUpdate, rather than an Editor update that can observe the temporarily unlifted camera. Tests use virtual gamepad input, isolate saves, suppress enemy knockback and restore Editor settings. The initial east-exit test attempted to walk to x235 beyond a terrain lip; its assertion was corrected to verify crossing the actual x233 vista boundary, without changing terrain.

Native captures: `before/street_129_4K.png` and `after/street_129_4K.png`. The 1080p copies are separately rendered previews. `QA.json` contains the final counts and preservation checks.

Supported targets remain Windows PC, PS5, Xbox Series X/S and Switch 2 handheld/docked. Validation here is Windows Unity Editor rendering at 1920×1080 and 3840×2160 with virtual gamepad input. Player builds, console hardware, CPU/GPU frame time, peak memory and physical-controller coverage are not tested. Texture/audio import profiles are unchanged; no 60 FPS or console-readiness claim is made.
