# Cave vista revision

Current status: applied and saved across all 89 underground rooms and 12 themes. The final composition uses 48 unique, unresized native paintings (Left, Right, Vault, Extension) with the 12 unchanged central paintings. The additional accepted Heart detail source also remains unchanged. `complete_application.json` reports 89 rooms, 12 themes and no failures.

Visual review is complete for all 12 native 3840 x 2160 Editor theme captures and all 89 grounded runtime room views. After the final terrain edge parity and D01 taper fixes, all eight runtime contact sheets were regenerated and reviewed again, plus native-resolution D01, PC06 and Heart images. The earlier pass with 12 native runtime spot checks is archived in `BeforeFinalEdgeParity`; it is not represented as native inspection of the final PNG bytes. All 25 overhead-collider flags were reviewed: physical platforms, pods, room/side rocks and portal shoulders remain separate from the distant scenery, with no observed player occlusion. No repeated scenic-panel grid or mirrored diamond seams were observed. Exact final capture hashes and inspection scope are recorded in `theme12_visual_qa.json` and `runtime89_visual_qa.json`.

The camera fixture passed all 90 cases (89 underground rooms plus the MR03 surface entry), with no errors. This is grounded placement and framing evidence, not a complete visual traversal of all parallax extremes or a combat test; enemy damage was suppressed for that fixture. Platform hardware and performance validation remain separate.

Final perimeter dimming is50% of the distant grade, replacing candidate3's72%. All texture coordinates retain216 native pixels per world unit, enough for2160 output pixels at the closest camera half-height5. Unique painted pieces use translation only; no narrative mirroring, repeated panels, black grid or source upscaling. The background has no absolute-world-height floor gradient.

Use APPLY_FINAL.md and apply_final.cs for exact application. native48_manifest.json records all48 native sources, sizes, world footprints and SHA256 values. original13_hash_preservation.json verifies all13 accepted sources remain byte-identical. capture_all_themes.cs creates12 saved-scene Editor previews; build_contact_sheets.py creates JPEG review sheets with original capture hashes. Review sheets live in nested ReviewSheets directories and do not add PNGs to either capture set.

The following sections preserve the iteration history; their candidate-specific pending states and older algorithms are superseded by the current status above.

## Historical candidate1

The user rejected the repeated scenic-panel treatment. Previous visual acceptance claims are superseded. This candidate removes the entire row/column cell system rather than hiding its seams.

Each room now displays its native painting exactly once, upright, at216PPU:1774x887 pixels cover8.212963x4.106481 units. Bounded slow parallax makes that single distant view remain readable across a long room. The remaining view is a continuous mineral field colored from the unchanged painting perimeter, with two orthonormal native texture samples to break repeated rock symmetry. There are no narrative copies, mirrored waterfalls, repeated masked windows, or black cell borders. The bitmap is not enlarged to fill4K.

Source backups are inBefore. Native painting bytes, room bounds, terrain, checkpoints, cave pairs and Heart material/awakening behavior are untouched. The runtime component refreshes on enable; the isolated editor helper only refreshes89 existing materials and optionally saves the current scene.

Root application: compile, force-importAssets/Resources/FX/WorldRefreshCaveVista.shader, open the underground scene, callWorldRevisionCaveVistas.Refresh(false). Capture Heart gameplay and Moss/root cave at3840x2160 with the revised camera framing before deciding whether to apply/save globally. Source is a candidate, not a visual pass. If the center still reads as a postage stamp, iterate composition rather than repeat or stretch the painting.

Review: one dominant distant landmark; no visible panel grid while panning; matching perimeter rock color; no rectangular seam; distinct theme palette; foreground silhouette and hazards remain clear; native asset density remains216PPU. Then capture all12 themes and89 room placement samples; verify return/room IDs and bounds unchanged. Root owns all Unity calls.

Supported targets: Windows PC, PS5, Xbox Series X|S, Nintendo Switch2. Actual Windows build/GPU measurements, console builds, SeriesS and Switch2 handheld/docked hardware tests are not part of this Editor visual check and remain untested.


## Second candidate: painted extension

The first single-painting candidate was rejected in actual4K captures: its outer field was nearly flat olive/red. Inspection confirmed the old Backdrop_Far source is nearly black, and the first shader flattened its tiny luminance range. That source cannot supply the required painted depth. The visible faint cracks belong to the older near overlay.

Two new built-in image_gen extension paintings now provide fullRGB stone/root/fossil detail for Moss(MR01) and Heart(H), with no portals, buildings, or narrative openings to duplicate. Both are native1254x1254, unresized, covering5.805556units at216PPU. The shader retains their RGB detail and blends two native orthonormal samples using continuous weights; no row/column mask or palette-only field remains. Mean-color matching joins each extension to the original landmark painting. Source paths, prompts and hashes are recorded alongside this document.

Apply the review candidate only after compilation/import withWorldRevisionCaveVistas.ApplyPaintedExtensions(false) AFTER opening the underground scene. A capture script that reopens it will discard unsaved assignments; call the helper inside that capture or explicitlysave(true). This currently configuresMR01 andH rooms; other theme extensions await the two-view composition review. Source is frozen pending root captures. No code changes during root runtime fixtures.


## Third candidate: connected painted depth

The second capture rejected mirrored diamond seams and over-detailed bright rock competing with platforms. The third candidate adds six unique native painted adjacent areas: left/right portrait reaches and a broad upper vault for each of Moss and Heart. These join the original central painting into approximately16x8world units of painted cave depth. Each piece is sampled once and translated only; normalized irregular overlap weights hide joins without stretching source pixels or masking separate oval windows. The remaining perimeter uses four upright hashed Repeat samples, with no triangle-wave mirroring. Distant imagery is graded to53%brightness and76%saturation; perimeter stone is another28%dimmer. These are rendering settings, not modifications to source images.

Native sourcepieces: portraits1024x1536(4.740741x7.111111units); vault1774x887(8.212963x4.106481units); originalcenter1774x887; native density216PPU throughout. Sixpieces were inspected as images, but connected game composition still requires root capture review. Metadata:depth_generation.json,depth_native_assets.json,candidate3.json. The code/images were staged underStaged then copied intoAssets during root's explicit codewindow. Frozen awaiting two-game-view review; no Heart core material, awakening logic, camera or terrain edits.

