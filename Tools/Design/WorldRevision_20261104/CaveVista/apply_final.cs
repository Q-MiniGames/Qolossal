// Run only after the approved file-copy window, editor compilation and synchronous imports.
// This does not open a scene or enter Play mode. Root opens the canonical underground first.
UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/FX/WorldRefreshCaveVista.shader", UnityEditor.ImportAssetOptions.ForceUpdate | UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var applied = WorldRevisionCaveVistas.ApplyPaintedExtensions(true);
var audited = WorldRevisionCaveVistas.AuditComplete();
return new { applied, audited };
