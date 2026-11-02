QOLOSSAL LAYOUT FLIGHT - 22 OCTOBER 2026

Open Assets/LevelDesign/WorldLayout_20261022/Qolossal_WorldLayout_Flight.unity.
Press Unity Play, then click the Game view so keyboard and mouse input reach it.
The white diamond is a review cursor. It ignores gravity, room walls and terrain.
This is inspection movement only; it does not change Qori's production abilities or saves.

WASD / arrow keys: fly in any direction
Shift: fast (48 u/s at normal zoom)
Ctrl: precise (2.4 u/s at normal zoom)
Mouse wheel or +/-: zoom
R: restore normal camera (17.78 x 10 u at 16:9)
M: toggle whole-map view; click a location on that map to travel there
H: jump to and frame the heart hall
Home: return to the first early cave at normal zoom
Tab / Shift+Tab: next / previous room (all 99 rooms)
F1: hide/show the control guide

Gamepad: left stick fly, RT fast, LT precise, right stick up/down zoom,
Start whole map, D-pad left/right rooms, D-pad up normal zoom,
north face button heart, west face button start, Select/Back help.
The runtime guide uses common Y/X labels; actual platform glyphs are not implemented in this review tool.

Normal flight is 12 u/s. Speed scales up when zoomed out, capped at 2000 u/s.
Zoom half-height ranges 2.5-900 u; normal is 5. The whole-map mode preserves cursor
position and shows its location on the fixed overview. World padding bounds prevent getting lost.
No runtime cave teleport interactions are needed: inspection flight passes through all borders.

Validated in Windows Unity Editor Play mode, 3840 x 2160 Game view. Fifteen checks passed,
including keyboard/mouse/gamepad Input System events, wall crossing, release-to-stop,
speed modifiers, map-click travel, heart/Home/room shortcuts, zoom bounds, diagonal
normalization, frame-rate independence and visits to all 99 rooms. Input simulation used
a temporary cloned input-settings object to permit keyboard/mouse tests while unfocused;
the original input settings were restored. A physical controller was not tested.

Original borders scene, layout geometry, art, production scene files and build list preserved.
Supported production targets remain Windows PC, PS5, Xbox Series X|S and Switch 2.
Console builds/hardware, physical controller coverage, 60 FPS performance, CPU/GPU budgets,
peak memory, handheld/docked behavior and runtime import budgets: NOT TESTED in this pass.
No imported art or audio settings changed. This is a local spatial review tool.
