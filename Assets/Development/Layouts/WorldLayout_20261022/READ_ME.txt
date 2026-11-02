QOLOSSAL - ROOM BORDER BLOCKOUT - 22 OCTOBER 2026

Open Assets/LevelDesign/WorldLayout_20261022/Qolossal_WorldLayout_Borders.unity.
This is a dedicated Unity review scene. Existing gameplay scenes and build settings are preserved.

Use Unity's Scene view (2D) to pan and zoom. Menu: Qolossal > Layout Review.
- Whole World / Heart and Four Routes / Heart Hall / Early Cave Networks: Scene view bookmarks.
- Select a named room under '03 Rooms', then Fit Selected Room.
- Normal Camera Scale at Selected Room: puts the Game view at orthographic size 5 (17.78 x 10 u at 16:9).
- Restore Overview Camera: returns the Game view to the whole-map view.
Camera view commands change only this review scene; save it if you want to retain a chosen view.

99 empty room envelopes: 40 heart-route rooms, 18 early-network rooms, 12 small caves,
1 open heart hall, 28 inherited later caves/homes/drift rooms.
63 corridors use 3 or 4 unit clear widths. The room/corridor unions have open joins.
The heart is one 84 x 48 unit hall, with exactly four open entrances. No internal partitions.
Seven regions retain their authored upper-ground surface outlines (17 strips).

Surface doors are scene transitions in the approved design, not continuous shafts.
62 paired surface/room markers are under '05 Paired surface doors'. They are layout references,
not runtime teleport triggers yet. Physical corridors connect the main and early room networks.
Flight and runtime inspection travel will be the next phase; there is no player controller in this scene.
No decorations, internal platforms, enemies, rewards, heart sculpture, locks or gameplay gating.
The five boss allocations are included only through their enclosing room borders.

Collision follows the measured perimeter centerlines exactly. Border strokes become wider in the
whole-world overview for visibility; changing stroke width does not change room/corridor clearance.
Normal-scale rendering uses thin 0.18 u strokes. The disabled normal-scale reference camera is size 5.
All captures in this review folder are actual Unity camera renders at 3840 x 2160.

One inherited atlas overlap was corrected in this review scene only: MR07_C06 (Upper Cleft)
was shifted +8 u horizontally, from x2374 to x2382, leaving a 2 u gap from MR07_C05.
Its size and original surface transition anchor were retained. Source design JSON is unchanged.
Excluded: the legacy alternate MR08 and obsolete E2/E3 attachments; old reserved BOSS_A/BOSS_C
placeholders (superseded by the new route boss-room allocations).

Source: WorldCaves_SmallDiscoveries_20261022/world_design.json
SHA256: 100c41fb794333191a9a27bd9055bc4785a69cdecdca7dd552a136af6ddd7b24
Geometry_QA.json records measured passage clearances and counts.
Unity_Physics_QA.json records collision clearance checks in the actual Unity scene.
Existing_Files_Before.json / Existing_Files_After.json protect the pre-existing scene and camera files.

This scene is an empty spatial review, not a traversal or performance acceptance build.
