# Obsolete prop and Heart mechanic retirement

The user's screenshots identify the pale, non-colliding cliff-kit ledges and the large Wakeknot mechanism in Causeway. The final request also explicitly retires the Heart checkpoint and activation mechanics, superseding the initial proposal to hide their art while retaining those mechanics.

- 18 decorative cliff ledges: Cradle 1, Causeway 7, Terraces 10. They had no gameplay components or collision.
- Six surface wake knots: Grip, Reach, Spring, Breath, Bloom and Sight. Their entire objects, triggers and ambient knot audio are inactive.
- Seventh/Heart knot: old central ending assembly and healed duplicate retired, together with all four quarter activation controls and their four overlay effects. The underground runtime no longer references or enables that ending.
- Heart checkpoint retired. Older saves resolve `regional:HEART` to `regional:A08`, the existing Cistern approach connected to the hall. This read-time alias does not write or erase player saves.
- Cradle's obsolete knot-controlled gate is inactive. Its already-authored fallen-stone crossing is permanently active for fresh and older saves; world and atlas requirements no longer demand the removed Grip knot.

The assemblies are disabled in the saved playable scenes rather than destroying accepted artwork or recovery information. No images, texture imports, terrain vertices, background/cave art, cave connections, skill rewards or camera settings were changed. The 13-scene catalog remains intact. Future builders must respect these retirements; do not regenerate the legacy props from old concept data.

`Before/Scenes` and `Before/Map` contain the pre-task backups. `inventory.json` is the original 13-scene inspection. `surface_changes.json` and `heart_retired.json` record the final scope. `cleanup_heart.cs` and `heart_changes.json` are superseded intermediate work; only `retire_heart.cs` describes the final Heart disposition. The first Play receipt, `Play/superseded_partial_result.json`, passed surface checks but failed the earlier hidden-shrine approach; it is not final validation.

Final evidence is recorded in `Play/result.json`, `Map/source_refresh.json`, `Map/ui_tests.json` and `QA.json`. Native 3840 × 2160 Play captures are in `Play/`. The map refresh checks all 84 surface areas, 89 rooms, 37 cave pairs and the approved cartographic transforms. Only the old Cradle gate collider is removed and the existing crossing enabled; no terrain vertices are moved.

Platform scope: Windows Unity Editor, virtual gamepad and keyboard/mouse fixture coverage only. Windows standalone, PS5, Xbox Series X/S, and Switch 2 handheld/docked builds and hardware performance are not tested by this cleanup. No 60-FPS or console-readiness claim follows from these checks.
