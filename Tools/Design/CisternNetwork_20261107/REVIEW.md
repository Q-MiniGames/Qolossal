# Connected Cistern A route — 7 November 2026

The layered Cistern review now covers A01–A08 and the corresponding ends of all eleven touching passages (27 material bindings). The user explicitly confirmed all eight main rooms, A01-A08, in the scope clarification. A09, A10, Heart and the other cave interiors keep their previous paintings. This is a reversible persistent **Editor review**, not final source acceptance or a player-build rollout.

## Composition

Eight distinct far sections form the inlet, drain shaft, lower silt channels, reservoir court, spillway shaft, pump gallery, keeper gallery and guardian sluice. Four separately painted middle-distance landmarks provide readable movement. Each narrative painting is placed once; neither horizontal nor vertical movement repeats a scene. A plain mineral material may repeat, as ordinary surface detail, in enclosed passages and beyond the finite painted vaults.

Every selected room and passage samples the same world registration. Far scenery follows the camera by 55% horizontally / 45% vertically (45% / 55% terrain motion); middle landmarks follow by 12% / 6% (88% / 94% terrain motion). There is no camera-edge clamp, recentering, per-room reset or camera-setting change. Room domains prevent a tall shaft's image bleeding into an adjacent upper gallery. Open connectors retain the view; five enclosed connectors use attached stone rear walls, including both segments of the bent connector. The exterior passage ends blend into the unchanged optional/Heart interiors using the existing mouth registration.

The first full-network placement was rejected during our own visual check: wide paintings were too enlarged, the pump floated, and a Unity global-vector-array length limit dropped the last two panels. The final version reduces those spans, adds three distinct sections, fits the landmarks, and pads the named global arrays to a stable capacity. Historical draft imagery remains as provenance, not a claim of visual acceptance.

## Trying it

The existing menu **Qolossal > Art Review > Continuous Cistern Preview** now installs this network review across Edit/Play/reload. It does not teleport Qori during normal Play or change normal save routing. `start_user_trial.cs` provides the isolated-save trial at the familiar A04 approach. Stop restores normal save routing and prior Editor settings.

- F8 / left-stick click: layered scenery versus far scenery only. Both retain the same far motion; this is a layer comparison.
- F6: return to the A04 approach.
- F7: visit the next main A room at its existing checkpoint arrival. Review shortcut only.

## Evidence and limits

`Views/` contains eighteen Editor composition captures and the extracted room bounds. `Play/` contains actual runtime captures at 1920×1080 and 3840×2160, render-end measurements, walking across A04/A05, a sustained glide, cave edge/manual look checks, eight arrivals, and keyboard/controller comparison input. `render_checks.json` checks 126 passage mouths in two modes, material restoration and unrelated-room preservation. `motion_measurements.json` measures rendered pixel displacement of the far and middle layers independently. `QA.json` records the final results. All thirteen playable scene hashes are compared with the pre-change baseline; no topology/map refresh is required because no geometry or scene placement changes.

`resolution.json` calculates each asset's actual 4K placement at 216 screen pixels/world unit (orthographic half-height 5). The four middle paintings each require **1469×980** and have **1536×1024** native sources. Passage stone requires **1210×1210**, source **1254×1254**. These pass. The eight distant paintings remain **composition drafts and do not meet full native 4K density**. The largest requires 15552×5184; generated 2172×724 panoramas are not represented as native 4K. Runtime texture-array resampling creates no extra source detail. Production needs coordinated higher-detail sections at the measured scales, followed by another visual/import review.

Eleven new assets are in `Assets/Art/Prototypes/CisternNetwork_20261107`; two earlier pilot assets are reused unchanged. All generated with the built-in image tool; no separately billed image API. Exact prompts and returned source paths are in `PROVENANCE.json` and `EXTRA_PROVENANCE.json`. Accepted canonical art bytes are not overwritten.

## Platform readiness

Targets remain Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2 (handheld/docked); original Switch is excluded. Tested here: Windows Unity Editor, 1080p/4K captures, synthetic keyboard/gamepad traversal and comparison. The uncompressed preview texture arrays report 218,472,172 bytes (~208 MiB), excluding source textures and the rest of the game; this is not a production memory budget. Frame-time qualification, peak whole-game memory, builds and all console hardware modes—including Series S and both Switch 2 modes—are **not tested**. The 60 FPS target and console readiness are not certified by this review. Shader compilation has no errors, but the sampling function emits a D3D 'potentially uninitialized variable (CavePainting)' warning; rendered sampling/restoration checks are recorded rather than treating that warning as a pass.
