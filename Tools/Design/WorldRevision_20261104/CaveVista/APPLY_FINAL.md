# Final native cave composition

All48 generated images have been inspected individually. Each of12 themes has a unique Left, Right, Vault and Extension painting, plus its preserved original central vista. SHA256/native dimensions/source provenance are in native48_manifest.json. The13 original accepted sources remain unchanged (original13_hash_preservation.json).

## Approved code window

1. Leave Play mode and complete any running editor callbacks.
2. Copy Staged/*.png to Assets/Art/Prototypes/WorldRevision_20261104/CaveVista/ (48 files, preserving exact bytes; eight candidate files already exist).
3. Copy Staged/WorldRefreshCaveVista.shader to Assets/Resources/FX/WorldRefreshCaveVista.shader. This only lowers the perimeter contribution from .72 to .5 relative to candidate3.
4. Copy Staged/WorldRevisionCaveVistas.cs to Assets/Editor/World/WorldRevisionCaveVistas.cs. This adds AuditComplete. The component WorldRefreshCaveVista.cs is unchanged from candidate3; its staged copy should match Assets/Scripts/Environment/WorldRefreshCaveVista.cs.
5. Compile and open Assets/Scenes/Game/Qolossal_Underground.unity. Run apply_final.cs. Expected: configured89, rooms89, themes12, failures[]. This saves the scene.
6. Force-import shader if necessary, then capture a representative of each12 themes at camera half-height5. Review foreground separation, continuous composition and all piece overlaps before full89-room Play captures.

## Coordinate/density contract and floor validation

Each source is sampled at216 native pixels per world unit with translation only: Left/Right1024x1536 =>4.740741x7.111111u; Vault1774x887 =>8.212963x4.106481u; Extension1254square =>5.805556squareu. The original1774x887 vista uses the same216PPU. Blending weights vary organically but never distort UV coordinates. Only the subdued peripheral stone uses four hashed upright repeats; there is no mirrored scene or repeated narrative window.

There is **no world-height floor gradient** or floor cutoff in the shader. The composition follows the active camera with bounded room drift, so rooms at different absolute elevations cannot acquire a shifted horizontal fade band. Center is camera minus.75u plus a bounded±.25u vertical room drift. Left/right sit1.4u above this and vault3.3u above it. Per-room validation therefore consists of confirming the actual play camera's grounded framing and that the physical floor hides the lower composition naturally; do not change room origins to align art. Root's final89-room grounded Play captures are the appropriate visual check. All room topology, terrain, checkpoint/arrival positions and Heart gameplay remain untouched by this applicator.
