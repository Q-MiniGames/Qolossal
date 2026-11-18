# Grounded framing revision — 4 November 2026

The user requested a small visible terrain band with Qori low on the screen, on both the surface and underground. Higher platforming/jumping should let Qori move toward the middle. `Assets/Scripts/CameraFollow.cs` now derives the grounded target from the actual player collider's bottom, not its transform origin.

At orthographic size 5, a 2160-pixel-high view spans 10 world units. The target is feet at 0.15 of viewport height, or 324 pixels above the bottom. The camera centre is therefore `feetY + 3.5`. The accepted validation band is 0.13–0.18 (281–389 pixels at 2160p). This changes composition without changing the closest zoom or 216 px/world-unit density requirement. Steep slopes can still occupy more screen area away from the player's feet; a foot-height measurement does not claim that every terrain pixel is confined to 15% of the image.

Grounded vertical velocity no longer adds a separate bobbing lead. A 0.025-unit contact deadband and 0.24-second vertical damping handle walking slopes and landing recovery. Horizontal response retains the original 0.18-second follow, retained forward lead and thread bias. The shared authored offset Y=1 is the new baseline; deviations from it remain additive adjustments.

On takeoff the camera retains its vertical anchor. Qori rises through the frame, and upward following begins when the collider centre reaches viewport 0.55. The anchor holds through the apex rather than immediately reversing with vertical velocity. Falling retains the existing speed-based downward lead. An asymmetric safety band holds feet above 0.08 and the collider head below the authored upper margin (0.775 at its default). Room and arena constraints retain priority. Explicit configure/respawn resets still snap, ordinary motion does not.

## Authored constraint audit

`camera_constraints.json` records the serialized CameraBounds and CameraLockZone components in all 13 shipping scenes. There are no authored lock zones. The underground gallery has one broad bounds rectangle with `top=false`; it does not prevent low-foot framing or force a ceiling-centred composition. Surface limits are also broad. The five Qvale service houses have a fixed 10-unit-high view and retain their authored room composition. No scene settings or bounds were changed by this code edit. The pre-change CameraFollow source is in `Before/CameraFollow.cs`; HEAD matched the pre-edit file before this revision.

Physical cave ceilings remain a separate composition check. At size 5 with feet at 0.15, the top of the view is 8.5 units above the feet. The fixture records upward-facing-view rays that hit underside Ground surfaces at centre and ±2 units, and flags hits below viewport 0.75 for visual review. Those flags do not establish that rendered art is opaque, nor does a framing-metric pass waive a ceiling-filled screenshot.

## Runtime verification to execute through the coordinating Unity session

Run `camera_playtest.cs` as an Editor C# snippet after saving scene changes and compilation. It uses normal PlayerMovement input and physics, native camera renders, an isolated save, a temporary virtual gamepad, frame-progress deadlines and restoration of normal settings/scenes. Damage shielding applies only to the fixture. It does not save scenes.

- Seven surface entry idle cases require grounded feet 0.13–0.18 and idle variation below 0.008 viewport.
- Seven real gamepad jumps require an airborne interval, visible rise of at least 0.08 viewport, grounded recovery, no respawn and safe head/foot limits.
- Seven higher launches use the existing Flower `LaunchUp(18)` path and additionally require centre height to reach at least 0.45 viewport. This is a controlled camera stress case, not a claim that a flower exists at each entry.
- All 89 actual underground checkpoint transforms receive a settled idle framing check. Twelve representative cave views are captured, along with any ceiling-risk or failed case. Surface idle and airborne apex views are also captured.
- Every sampled frame must preserve camera half-height ≥5. Actual console errors, failed cases and restoration results are recorded. All numeric values are scalar JSON values, not recursive Unity vector objects.

Results are written under `Camera/`: `camera_progress.json`, durable `camera_cases.json`, final `camera_result.json`, and native 3840 × 2160 images. The planned scope is 110 cases, approximately 240 simulation seconds, bounded to 900 wall seconds. A final `passed` flag refers to the measurable camera checks; `ceilingVisualReviewRequired` remains separate and must be resolved by inspecting captures. No runtime pass is claimed before this fixture is executed.

## Recorded Editor result

The coordinating agent executed the fixture. `Camera/camera_result.json` records **110/110 passed**, zero fixture errors and zero console errors. Across the 96 idle cases, measured foot height ranged from 0.1499939 to 0.150005341 viewport; maximum sampled idle variation was 0. Normal jumps and higher launches reached body-centre maxima between 0.446735978 and 0.653666 viewport. Normal save hashes, settings and scenes were restored.

The result also records **25 overhead collision review flags**. Those are pending separate rendered-composition inspection; the camera metric pass does not approve ceiling appearance. Subsequent scene/art mutations require updated visual evidence where they affect framing.

The fixture does not establish full traversal, physical controller behavior, standalone performance, 60 FPS or console readiness. Windows PC, PS5, Xbox Series X/S and Nintendo Switch 2 handheld/docked remain the shipping targets; platform hardware testing remains separate.
