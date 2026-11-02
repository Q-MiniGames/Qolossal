WARM CRACK — NATURAL EDGE V2 — 22 OCTOBER 2026

Scope: one room (MR01_C01), isolated sibling review scene. No wider rollout.
Open live Assets/LevelDesign/RoomNaturalEdges_v2_20261022/Qolossal_Room_NaturalEdges_v2.unity.
Play: A/D or stick + Space/South. F2/Select switches flight, M overview, R gameplay zoom, Home character reset.
Right exit uses five basic jumps; take a normal running start for the final gap. Roof is a drop-in.

Source art: Art/Edge_Raw.png, Rock_Raw.png, CornerCaps_Raw.png. ImageGen prompts retained in Prompts.json and CornerCap_Prompt.txt.
Derived art: quilt_material.py makes the native-pixel 4096 fill, prepare_v2.py builds periodic edge/cap atlas and geometry. No source RGB enlargement.
Geometry: prepare_v2.py is the current generator. UpdateGeometry.cs applies pilot_geometry_v2.json to the already-created v2 scene via Unity CLI in Edit mode. BuildV2.cs and RepairCaps.cs document initial creation and intervening repairs; do not use BuildV2 alone as a final-state rebuild.
Rendering: NaturalRock.shader uses world-space translated fill samples. NaturalEdge.shader blends straight edge paint with world-aligned cap faces. Cap swaps fade before UV switches.
Character prefab/shared movement/tuning unchanged; instance-only resetBelowY=-190 prevents the underground test from respawning immediately.

Verification: RouteQA.cs records continuous actual-player synthetic-gamepad traversal and native4K endpoint images. CharacterQA.cs records keyboard cases. KeyboardRetest.cs resolves the second-step short-hold endpoint failure and checks mode/zoom controls. Setup teleports are explicitly unscored and labelled. Failed historical evidence remains retained.
CapturePilot.cs produces matched static4K before/after views. FinalSceneAudit.cs records colliders/imports/shaders. verify_preservation.py verifies protected files and canonical1318 accepted bytes. build_review.py produces index.html and Final_Verification.json from current evidence.

Limits: prototype uncompressed imports, no hardware/frame-time certification, no whole-map expansion, no exhaustive all-topology or all-camera-motion acceptance. Review Independent_Review_Final.json before any further rollout.

FINAL CONTACT REFINEMENT: scene-only RoomContactTuning.asset changes groundCheckDistance from .06 to .025. All other values remain identical (Contact_Tuning_Diff.json). .008 candidate failed and was rejected. Final current-state evidence: Route_Contact025_QA.json (10/10), Keyboard_Contact025_QA.json (13/13), Mode_Control_Final.json (4 checks). Contact025RouteQA.cs captures after pose LateUpdate. Earlier ContactRefined, PoseSynchronized and original reports remain historical. Independent_Contact025_Final.json is the additive final contact verdict.
