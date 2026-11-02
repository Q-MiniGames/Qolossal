using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The double jump as an earned relic, the Leapseed, found at the Cradle's shelter hut (the first checkpoint).
// Creates the relic (placeholder art until Codex paints it) and places its shrine, a copy of the Cradle's
// own ability shrine, beside the hut. Re-running replaces the shrine. Menu: Qolossal > Relics > Install Leapseed.
// Batch: -executeMethod DoubleJumpRelicInstaller.InstallBatch
public static class DoubleJumpRelicInstaller
{
    const string AssetPath = "Assets/Resources/Relics/Leapseed.asset";
    const string ArtFolder = "Assets/Art/Placeholder/";
    const string CradlePath = "Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity";
    const string HutCheckpoint = "mra:MR01:cp:01";
    const string ShrineName = "Shrine Leapseed";
    const float AfterHut = 4f;

    [MenuItem("Qolossal/Relics/Install Leapseed (double jump)")]
    public static void InstallMenu() => Install(save: false);

    public static void InstallBatch() => Install(save: true);

    static void Install(bool save)
    {
        var relic = Placeholder("Relic_Leapseed_Placeholder", 160f, Seed);
        var icon = Placeholder("Icon_Ability_DoubleJump_Placeholder", 128f, Seed);
        var ability = AssetDatabase.LoadAssetAtPath<RelicAbilityDefinition>(AssetPath);
        if (ability == null) { ability = ScriptableObject.CreateInstance<RelicAbilityDefinition>(); AssetDatabase.CreateAsset(ability, AssetPath); }
        ability.abilityId = Relics.DoubleJump; ability.displayName = "Leapseed";
        ability.hint = "Press Jump again in mid-air to leap a second time.";
        ability.relic = relic; ability.icon = icon;
        EditorUtility.SetDirty(ability); AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene().path == CradlePath ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(CradlePath, OpenSceneMode.Single);
        foreach (var old in Object.FindObjectsByType<AbilityShrine>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.name == ShrineName))
            Object.DestroyImmediate(old.gameObject);
        var hut = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.CheckpointId == HutCheckpoint);
        var model = Object.FindObjectsByType<AbilityShrine>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s.ability != null);
        if (hut == null || model == null) { Debug.LogError("Leapseed: no shelter-hut checkpoint or model shrine in the Cradle."); return; }

        Physics2D.SyncTransforms();
        Vector2 probe = (Vector2)hut.transform.position + new Vector2(AfterHut, 4f);
        var hit = Physics2D.Raycast(probe, Vector2.down, 12f, LayerMask.GetMask("Ground"));
        if (hit.collider == null) { Debug.LogError("Leapseed: no ground after the shelter hut."); return; }

        // A copy of the Cradle's own shrine keeps its pedestal art, sorting and trigger; its foot sits where the model's does.
        var shrine = Object.Instantiate(model, hut.transform.parent);
        shrine.name = ShrineName;
        float foot = model.GetComponent<Collider2D>() is BoxCollider2D box ? box.bounds.min.y - model.transform.position.y : 0f;
        shrine.transform.position = new Vector3(hit.point.x, hit.point.y - foot, model.transform.position.z);
        shrine.ability = ability;
        shrine.relic.sprite = relic;
        shrine.relic.transform.localScale = Vector3.one;
        shrine.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        if (save) EditorSceneManager.SaveScene(scene);
        Debug.Log($"Leapseed: shrine at {shrine.transform.position} beside {HutCheckpoint}.");
    }

    // A pale seed with two rising chevrons: obviously temporary art.
    static Color Seed(float x, float y)
    {
        float body = new Vector2((x - .5f) / .32f, (y - .4f) / .38f).sqrMagnitude;
        float chevron(float cy) => Mathf.Abs(y - cy - (.18f - Mathf.Abs(x - .5f) * .6f)) < .035f && Mathf.Abs(x - .5f) < .22f ? 1f : 0f;
        if (Mathf.Max(chevron(.62f), chevron(.75f)) > 0f) return new Color(1f, 1f, .85f);
        return body < 1f ? Color.Lerp(new Color(.55f, .85f, .45f), new Color(.3f, .6f, .3f), body) : Color.clear;
    }

    static Sprite Placeholder(string name, float pixelsPerUnit, System.Func<float, float, Color> paint)
    {
        string path = ArtFolder + name + ".png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(ArtFolder);
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) tex.SetPixel(x, y, paint((x + .5f) / size, (y + .5f) / size));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = pixelsPerUnit; importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

// Batch: -executeMethod DoubleJumpRelicCheck.Run -captureDir <dir>. A capture around the shrine and the
// Cradle's ground profile (top-most ground every 0.5 u), to find rises a single jump can't make.
public static class DoubleJumpRelicCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity", OpenSceneMode.Single);
        Physics2D.SyncTransforms();
        var args = System.Environment.GetCommandLineArgs();
        string dir = args[System.Array.IndexOf(args, "-captureDir") + 1];
        Directory.CreateDirectory(dir);
        var lines = new System.Collections.Generic.List<string>();
        int ground = LayerMask.GetMask("Ground");
        for (float x = 0f; x <= 200f; x += .5f)
        {
            var hits = Physics2D.RaycastAll(new Vector2(x, 80f), Vector2.down, 120f, ground);
            lines.Add(x + "\t" + string.Join(" ", hits.Where(h => !h.collider.isTrigger).Select(h => h.point.y.ToString("0.00"))));
        }
        File.WriteAllLines(Path.Combine(dir, "cradle_profile.txt"), lines);
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 7f; cam.transform.position = new Vector3(46f, 23f, -50f);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, "leapseed_shrine.png"), tex.EncodeToPNG());
        RenderTexture.active = null;
    }
}
