QOLOSSAL / WARM CRACK / LOCAL ROCK BACKING / 23 OCTOBER 2026

Live scene:
C:/UnityProjects/Qolossal/Assets/LevelDesign/RoomBacking_20261023/Qolossal_Room_LocalBacking.unity
Review:
C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/RoomBacking_20261023/index.html

One-room sibling of the previously reviewed natural-edge v2 pilot. No map-wide
expansion. Reuses the existing painted quilt and rim materials. Adds local rock
behind the room borders, solid undercoats following the existing contours, and
visual passage transitions. Three original colliders are unchanged. No new
texture masters, character changes or camera tuning changes.

Final evidence:
- Independent_Join_Closure.json: PASS for scoped backing, transitions and4K coverage.
- Coverage_QA.json:17/17pass at3840x2160; no missing rock/new blocked-air pixels
  outside the0.03u silhouette tolerance and documented player exclusion.
- Traversal_QA.json:10/10 actual-player synthetic-gamepad route checks pass.
- Runtime_Camera_QA.json:1015 advancing fixed-step samples inside camera contract.
- Scene_Audit.json: three colliders identical, no missing scripts/shader errors.
- Preservation_Assessment.json:11protected files match historical hashes. Current
  accepted manifest has1348assets, all hash-match. Its historical manifest hash
  differs; this backing task did not edit the manifest. Never claim12/12unchanged.
- Initial reviewer report and Before_Join_Refinement evidence are retained.
  Draft_* and *_QA_Fail.png are earlier diagnostics, not final results.

Coverage contract:
16:9, normal camera size5 (17.78x10u), stress size6.5 (23.11x13u).
Camera centers x28.65..77.35, y-179.35..-146.65. Local rock bounds
x16..90, y-188..-138 retain1.09u horizontal/2.15u vertical spare coverage.
Unrestricted flight/size13overview may reveal the local patch boundary; those
views are not included in the gameplay coverage guarantee. This is a bounded
design contract, not a new camera clamp.

Roof physical stub ends y-152; right physical stub ends x71. Backing beyond these
points is visual-only. Roof continuation narrows3.2u->3u and right3.18u->3u over2u;
the existing painted rim fades over1.6u. New corridor art/collision is not claimed.
Crossing_QA_Fixture_ONLY is a separate4u union coverage fixture with no colliders,
saved inactive. It does not add a room or crossing to the playable map.

Implementation order (already applied; builders are historical recipes):
prepare_backing.py / BuildBacking.cs -> RefineBacking.cs ->
prepare_undercoat.py / ApplyUndercoat.cs -> prepare_joins.py / SmoothJoins.cs.
Final backing geometry is backing_geometry_join_v2.json. Do not rerun the older
builders over reviewed final assets; they represent earlier stages. Do not rerun
prepare_backing.py to refresh protected baselines. Final scripts/captures live in
this workspace; Unity assets live only in the new sibling folder.

Controls: Play starts character mode. F2/gamepadSelect switches flight, R restores
gameplay zoom in flight, Home resets character. Test harness uses real movement
code and synthetic controller events, not character teleports between jumps.

Platform readiness:
Windows Editor6000.3.19f1, actual3840x2160GameView and offscreen4K captures tested.
Physical controller, CPU/GPU frame-time, peak memory and console builds not tested.
Windows PC, PS5, Xbox Series X|S, Switch2handheld/docked remain supported targets.
Original Switch excluded.60FPS and4K are targets, not measured hardware claims.
Existing proof texture/import configuration is reused; no new platform profiles.
Grey rock is structural proof; distinct regional themes are still required.

Browser automation could not open the local file URL because its URL policy only
allows HTTP/HTTPS. The HTML and all local evidence remain available via file links.
