# Qvale jump camera correction — 4 November 2026

The prior offscreen correction preserved visibility but broke motion. At takeoff, its final camera clamp switched from a 15% grounded foot margin to 8% airborne, and the authored 3.4-unit lift then chased the collider upward. The rendered-frame reproduction records a 1.255825-unit single-frame displacement. Landing switched the limit back just as abruptly. The previous test verified visibility and that Qori jumped, not smooth camera movement.

`CameraFollow.ConstrainVistaLift` now leaves framing to the existing damped follow and its takeoff anchor. It checks the camera's current position against a constant safety band at the newly applied zoom. It does not add the authored lift when a player follow is active. The authored zoom remains unchanged; cameras without an active follow retain their lift. `VistaZone.CurrentLift` still reports the actual correction applied for rendering.

The first corrected-motion run also exposed a smaller landing dip (`after/landing_dip_check.json`): speed-based falling look-ahead activated while a normal jump returned to its starting level. Falling lead is now limited by the distance below the retained framing anchor. Long drops retain downward lead, while ordinary jumps retain the takeoff view through landing.

`playtest_before.cs` reproduces the failure, and `playtest_after.cs` measures actual rendered frames for repeated jumps at 30/60 FPS caps, a running jump, a high launch, four street boundary crossings, outside-vista framing and all six authored vistas. Ordinary jumps must include real airborne travel while remaining visible, with camera displacement below 0.1 unit per rendered frame and total vertical travel below 0.3 unit. This catches the previous snap and jump chasing. Captures include 1920×1080 and 3840×2160. `QA.json` holds the measured results and scene integrity check.

Tests use an isolated save and restore Editor settings. No scene, terrain, map or art edits are required. Windows Editor verification does not qualify player builds or PS5, Xbox Series X/S, or Switch 2 handheld/docked hardware. Performance, memory and physical controllers are not tested here.
