using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Rendering-only revision. Does not rebuild rooms, move anything, or touch Heart surfaces.
public static class WorldRevisionCaveVistas
{
    public static object ApplyPaintedExtensions(bool save = false)
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != WorldRefreshUnderground.ScenePath)
            throw new InvalidOperationException("Open underground in edit mode.");
        const string root = "Assets/Art/Prototypes/WorldRevision_20261104/CaveVista/";
        var means=new System.Collections.Generic.Dictionary<string,Color>();
        foreach (string path in Directory.GetFiles(root,"*_Extension.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default; importer.sRGBTexture=true;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
            var pixels=new Texture2D(2,2,TextureFormat.RGB24,false);
            ImageConversion.LoadImage(pixels,File.ReadAllBytes(path));
            Color mean=Color.black; var colors=pixels.GetPixels();
            foreach(var color in colors) mean+=color;
            mean/=colors.Length; Object.DestroyImmediate(pixels);
            means[Path.GetFileNameWithoutExtension(path)]=mean;
        }
        int configured=0;
        foreach(var field in Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var room=field.GetComponentInParent<UndergroundRoom>();
            string set=UndergroundArtKit.SetOf(room.roomId);
            string path=root+set+"_Extension.png";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)continue;
            var mean=means[set+"_Extension"];
            var edge=WorldRefreshCaveVista.EdgeColor(field.vista.name);
            field.extension=texture;
            field.extensionTint=new Color(edge.r/Mathf.Max(.01f,mean.r),edge.g/Mathf.Max(.01f,mean.g),edge.b/Mathf.Max(.01f,mean.b));
            field.Refresh(); EditorUtility.SetDirty(field); configured++;
        }
        if(save) { EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); }
        return new {configured,saved=save,refresh=Refresh(false)};
    }
    public static object Refresh(bool save = false)
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != WorldRefreshUnderground.ScenePath)
            throw new InvalidOperationException("Open the underground scene in edit mode first.");
        var scene = SceneManager.GetActiveScene();
        var fields = Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(v => v.gameObject.scene == scene).ToArray();
        if (fields.Length != 89) throw new InvalidOperationException("Expected89 existing room vistas; found " + fields.Length);
        var rows = fields.Select(v =>
        {
            var room = v.GetComponentInParent<UndergroundRoom>();
            var art = v.GetComponent<SpriteRenderer>();
            if (v.vista == null || art.drawMode != SpriteDrawMode.Simple)
                throw new InvalidOperationException("Missing native painting or simple quad: " + v.name);
            var bounds = art.bounds;
            v.Refresh();
            if (art.bounds != bounds) throw new InvalidOperationException("Rendering refresh changed bounds.");
            return new { room = room.roomId, asset = AssetDatabase.GetAssetPath(v.vista),
                copies = 1, width = v.vista.width, height = v.vista.height, ppu = v.pixelsPerUnit,
                worldWidth = v.vista.width / v.pixelsPerUnit, worldHeight = v.vista.height / v.pixelsPerUnit };
        }).OrderBy(r => r.room).ToArray();
        if (save) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        const string dir = "Tools/Design/WorldRevision_20261104/CaveVista";
        Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/application.json", JsonConvert.SerializeObject(rows, Formatting.Indented));
        return new { count = rows.Length, narrativeCopiesPerRoom = 1, saved = save,
            geometryChanged = false, nativePPU = 216, evidence = dir + "/application.json" };
    }
}
