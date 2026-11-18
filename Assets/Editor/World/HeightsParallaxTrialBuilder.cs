using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class HeightsParallaxTrialBuilder
{
    public const string ScenePath = "Assets/Scenes/Game/MRAtlas_MR06_Heights.unity";
    const string Art = "Assets/Art/Prototypes/HeightsParallaxA_20261103/";
    const string CloudArt = "Assets/Art/Prototypes/HeightsCloudsNeutral_20261103/";
    public const string Evidence = "C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/HeightsParallaxA_20261103";
    const float Ppu = 90f, ClimbBottom = 17f;

    [MenuItem("Qolossal/Backgrounds/Heights A test/Install trial")]
    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Install outside Play mode.");
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("An open scene has unsaved edits; installation did not proceed.");
        Directory.CreateDirectory(Evidence);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var file in Directory.GetFiles(Art, "*.png").Concat(Directory.GetFiles(CloudArt, "*.png")))
        {
            var path = file.Replace('\\','/');
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = true; imp.mipmapEnabled = true;
            imp.npotScale = TextureImporterNPOTScale.None; imp.maxTextureSize = path.StartsWith(CloudArt) ? 4096 : 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.isReadable = false; imp.filterMode = FilterMode.Bilinear;
            imp.wrapModeU = Path.GetFileName(path).StartsWith("Cloud") ? TextureWrapMode.Clamp : TextureWrapMode.Mirror;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (Object.FindFirstObjectByType<HeightsParallaxTrial>() != null)
            throw new InvalidOperationException("Trial already installed. Use the A/original switches.");
        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("No main camera in Heights.");
        var originals = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)
            .Where(p => p.sortingOrder < 0 && (p.name.StartsWith("Sky - ") || p.name.StartsWith("Far - ") || p.name.StartsWith("Mid - ") || p.name.StartsWith("Near - ")))
            .Select(p => p.gameObject).Distinct().ToArray();
        if (originals.Length < 2) throw new InvalidOperationException("Could not identify original Heights layers.");
        var host = new GameObject("Heights background | A test (original retained)");
        var trial = host.AddComponent<HeightsParallaxTrial>();
        trial.originalLayers = originals; trial.originalActive = originals.Select(x => x.activeSelf).ToArray();
        trial.targetCamera = camera;
        var root = new GameObject("Option A | valley and passing weather");
        root.transform.SetParent(host.transform, false); trial.optionARoot = root;
        ParallaxLayer Plane(string name, string texture, float depth, float bottom, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            var layer = go.AddComponent<ParallaxLayer>(); layer.enabled = false;
            bool cloud = texture.StartsWith("Cloud_");
            layer.texture = AssetDatabase.LoadAssetAtPath<Texture2D>((cloud ? CloudArt : Art) + texture + ".png");
            // New native cloud paintings retain the prototype's world width without stretching.
            float width = cloud ? AssetDatabase.LoadAssetAtPath<Texture2D>(Art + texture + ".png").width / Ppu : layer.texture.width / Ppu;
            layer.texturePixelsPerUnit = layer.texture.width / width;
            layer.height = layer.texture.height / layer.texturePixelsPerUnit;
            layer.composedForSize = 5; layer.depth = depth; layer.baseY = bottom;
            layer.extendBelow = 0; layer.sortingOrder = order; layer.targetCamera = camera;
            layer.climbBudget = 0; layer.repeat = true; layer.tint = Color.white;
            layer.enabled = true; return layer;
        }
        var sky = Plane("A | warm sky", "Sky", 0, -5.233333f, -110);
        sky.follow = Vector2.one;
        var valley = Plane("A | continuous valley", "Valley", 125, -5.233333f, -105);
        valley.climbProgression = true; valley.climbRange = new Vector2(17,233);
        valley.verticalRate = 1; valley.sinkLimit = 216f * .008f;

        // Register each cloud's X with the actual route at its authored altitude.
        var world = MraWorldBuilder.Prepare();
        var region = world.regions.First(r => r.id.StartsWith("MR06"));
        var nodes = region.nodes.Select(n => MraWorldBuilder.Spawn(n)).OrderBy(p => p.y).ToArray();
        float RouteX(float y)
        {
            if (y <= nodes[0].y) return nodes[0].x;
            for (int i=1;i<nodes.Length;i++)
                if (y <= nodes[i].y) return Mathf.Lerp(nodes[i-1].x,nodes[i].x,Mathf.InverseLerp(nodes[i-1].y,nodes[i].y,y));
            return nodes[nodes.Length-1].x;
        }
        int[] shapes = {0,2,1,3,0,2,1,3};
        float[] heights = {22,50,79,112,139,166,195,222};
        float[] xPixels = {-90,730,-180,20,850,-230,120,900};
        float[] yPixels = {230,100,160,250,70,230,300,140};
        float[] opacity = {.55f,.45f,.46f,.58f,.43f,.53f,.57f,.43f};
        trial.weather = new HeightsParallaxTrial.Weather[shapes.Length];
        trial.windSpeed = .025f;
        for (int i=0;i<shapes.Length;i++)
        {
            float anchor = ClimbBottom + heights[i];
            var cloud = Plane($"A | weather {i+1:00}", "Cloud_"+shapes[i], 1f/.16f, 0, -95+i);
            float originalHeight = cloud.height, originalWidth = cloud.texture.width/cloud.texturePixelsPerUnit;
            cloud.height *= .8f; cloud.texturePixelsPerUnit /= .8f;
            cloud.repeat = false; cloud.climbProgression = false; cloud.follow = new Vector2(.84f,.84f);
            cloud.baseY = anchor*.16f + 5f - yPixels[i]/Ppu - (originalHeight+cloud.height)*.5f;
            cloud.baseX = RouteX(anchor)*.16f - (1600f/Ppu)*.5f + xPixels[i]/Ppu + originalWidth*.5f;
            cloud.tint = new Color(1,1,1,opacity[i]*.9f);
            trial.weather[i] = new HeightsParallaxTrial.Weather {layer=cloud,authoredBaseX=cloud.baseX};
        }
        trial.SetOptionA(true); trial.UpdateWeather(camera,0);
        foreach(var layer in root.GetComponentsInChildren<ParallaxLayer>()) layer.Refresh(camera);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = host;
        return "Installed option A in Heights; originals retained. " + Validate();
    }

    [MenuItem("Qolossal/Backgrounds/Heights A test/Use option A")]
    public static void UseA() { Switch(true); }
    [MenuItem("Qolossal/Backgrounds/Heights A test/Use original background")]
    public static void UseOriginal() { Switch(false); }
    static void Switch(bool useA)
    {
        var trial = Object.FindFirstObjectByType<HeightsParallaxTrial>();
        if (trial == null) throw new InvalidOperationException("Open the Heights scene first.");
        Undo.RecordObject(trial,"Switch Heights background trial"); trial.SetOptionA(useA);
        if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(trial.gameObject.scene);
    }

    public static string Validate()
    {
        var trial = Object.FindFirstObjectByType<HeightsParallaxTrial>();
        if (trial == null || trial.weather.Length != 8) throw new InvalidOperationException("Missing trial/weather.");
        var layers=trial.optionARoot.GetComponentsInChildren<ParallaxLayer>(true);
        var report = new StringBuilder("Heights A prototype: native proof art, no console readiness claim.\n");
        long memory=0;
        foreach(var tex in layers.Select(l=>l.texture).Distinct())
        {
            long bytes=Profiler.GetRuntimeMemorySizeLong(tex); memory+=bytes;
            report.AppendLine($"{tex.name}: {tex.width}x{tex.height}, {tex.format}, mipmaps={tex.mipmapCount}, {bytes} bytes");
        }
        var valley=layers.First(l=>l.name=="A | continuous valley");
        if (Mathf.Abs(valley.Sink(118)-valley.Sink(117)-.008f)>.0001f) throw new Exception("Far rate is incorrect.");
        foreach(var item in trial.weather)
            if(Mathf.Abs((1-item.layer.follow.y)-.16f)>.0001f) throw new Exception("Weather rate is incorrect.");
        if(trial.originalLayers.Any(x=>x.activeSelf)) throw new Exception("Originals overlap option A.");
        report.AppendLine($"Measured imported textures: {memory/1048576.0:F2} MiB (texture objects only; not peak memory).");
        report.AppendLine("Far vertical rate 0.008, cloud vertical rate 0.16; originals hidden and retained.");
        report.AppendLine("PS5, Xbox Series X/Series S, Switch 2 handheld/docked: not tested.");
        File.WriteAllText(Evidence+"/import_validation.txt",report.ToString());
        return report.ToString();
    }
}
