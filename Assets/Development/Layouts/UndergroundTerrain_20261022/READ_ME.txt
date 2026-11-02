QOLOSSAL UNDERGROUND TERRAIN — FIRST PASS — 22 OCTOBER 2026

Open in Unity:
C:/UnityProjects/Qolossal/Assets/LevelDesign/UndergroundTerrain_20261022/Qolossal_Underground_Terrain_Review.unity

The scene is saved, open, and stopped. Press Play, then click Game view.
WASD/arrows fly; Shift fast; Ctrl precise; wheel/+/- zoom; R normal camera;
H heart; Home first cave; Tab/Shift+Tab next/previous cave; M map, click to travel;
F1 hides the guide. Gamepad bindings are displayed in the guide.

DELIVERED SCOPE
89 underground rooms: 18 early caves, 40 main route rooms, 12 small discovery
caves, 18 later caves, and the shared heart hall. Original room envelopes and
63 connecting passages are retained. 777 draft terrain/platform placements.
Surface homes and sky/drift rooms are excluded. The existing surface profile
only bounds the underground rock mass; no upper-ground artwork was added.
No props, enemies, rewards, heart sculpture, progression gates or music added.
The heart remains one shared hall with four arrival arms and a supported dais.
West/east landings align to their passage floors. Flight ignores collisions.

ART AND 4K
Built-in image_gen produced the rock and ledge painting masters. Exact prompts
and style reference are recorded in Prompts.json. Raw masters are preserved in
Art/Rock_Raw.png (1254x1254) and Art/Ledge_Raw.png (2172x724).
The 4096x4096 v2 rock is a native-pixel quilt assembled from the master, not a
new native 4096 painting or an enlarged image. Horizontal/vertical wrap joins
use minimum-error overlap cuts. The ledge was cropped and its walk line aligned
without enlargement, alpha noise removed, and repeat seam repaired using Python
under the user's explicit permission. Ledge lighting is used upwards only.
Mapping: 256 source pixels per world unit. Normal camera half-height5 at16:9
shows17.78x10u; 3840x2160 output uses216 screen pixels/u (18.5% density margin).
Imported sizes verified4096x4096 and2172x369. Meshes repeat instead of stretching
across the map. Short floating ends have angular taper silhouettes.
These files are REVIEW CANDIDATES. The accepted art manifest is unchanged.

VALIDATION
Windows Unity6000.3.19f1 Editor Play mode; real Game view3840x2160; frames advanced.
15 flight checks passed using synthetic keyboard/mouse/gamepad Input System
events, including visits to all89 rooms. Original input settings restored.
4782 radius0.45u centerline probes across63 connections found no obstruction.
No missing scripts or compilation failure. Current console check found no new
runtime error; the console retains three CLI command-discovery errors from
recompile tooling, not terrain gameplay exceptions. Review snapshots preserved.
All1318 canonical accepted asset hashes and62 protected existing project files
match their previous baseline. The prior flight scene/controller are unchanged.

FIRST-PASS LIMITS / NEXT ART WORK
Independent review accepts v2 for first-pass terrain inspection only. See
Reviewer_v2.json for its exact scope. Room/riser silhouettes still show the
blockout geometry; stone scale/hue transitions and stronger regional materials
need refinement. Native-pixel quilting increases repeat distance, not the amount
of independently painted source detail. Main-route and roof-return jump paths,
full character body/head clearance, drop-through behavior and encounter layouts
have not been gameplay-validated. Passage probes and noclip flight do not prove
those requirements. This scene is not a replacement production level.

PLATFORMS
Supported production targets: WindowsPC, PS5, XboxSeriesX|S, NintendoSwitch2
handheld/docked. OriginalSwitch excluded. Target60FPS/16.67ms remains a target.
No console/devkit build or hardware controller tested. CPU/GPU budgets and peak
memory not profiled. Review uses uncompressed RGB24/RGBA32 with mipmaps and
non-readable textures; production compression/streaming profiles remain to be
measured. Editor runtime-size query: quilt156631935bytes, ledge9121632bytes;
these are editor resource measurements, not console GPU/peak-memory budgets.

REPRODUCIBILITY
prepare_terrain.py -> initial native normalization and geometry draft;
quilt_material.py -> v2 native-pixel rock quilt;
BuildUnderground.cs -> additive scene builder;
RefineUnderground.cs -> v2 refinement with preserved v1 meshes/snapshot;
TerrainQA.cs and FlightInputQA.cs -> recorded validation/captures.
Do not run builders over an existing version. They intentionally refuse to
overwrite existing review scene/mesh folders. Create a new revision for changes.
