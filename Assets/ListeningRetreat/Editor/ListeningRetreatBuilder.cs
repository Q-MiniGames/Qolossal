using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Builds the listening retreat (Batch 25a-6, CLAUDE_REVIEW_59) from the reviewed layers in
// Assets/ListeningRetreat/Art (copied and hash-checked by Tools/ListeningRetreat/copy_batch25a6.py):
// sets their import settings, makes the prefab ListeningSpot opens (Resources/ListeningRetreat/...)
// and a preview scene. Layer order, canvases, offsets, pivots and parents come from
// Retreat_OverlookTerrace_Registration.json.
//
// Batch: -executeMethod ListeningRetreatBuilder.BuildAndCapture -captureDir <folder>
public static class ListeningRetreatBuilder
{
    const string Root = "Assets/ListeningRetreat/";
    const string ArtDir = Root + "Art/";
    const string Prefix = "Retreat_OverlookTerrace_";
    const string RegistrationPath = ArtDir + Prefix + "Registration.json";
    const string PrefabPath = Root + "Resources/" + ListeningRetreat.ResourcePath + ".prefab";
    const string ScenePath = Root + "Retreat_Overlook.unity";
    const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    static readonly Vector3 Away = new Vector3(0f, -2000f, 0f);   // far from every level

    [Serializable] class Registration { public LayerEntry[] layers; }
    [Serializable] class LayerEntry
    {
        public string name, file, parent, wrap;
        public int[] canvas; public float[] offset, pivot_px, base_pivot_canvas_px;
        public float period_px;
    }

    [MenuItem("Qolossal/Listening Retreat/Build")]
    public static void Build()
    {
        Import();
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        var root = Make();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var preview = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var r = preview.GetComponent<ListeningRetreat>();
        r.previewOnStart = true; r.view.enabled = true; r.view.tag = "MainCamera";
        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        PrefabUtility.RecordPrefabInstancePropertyModifications(r.view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(r.view.gameObject);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Retreat] built " + PrefabPath + " and " + ScenePath);
    }

    static void Import()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            string n = Path.GetFileNameWithoutExtension(path).Replace(Prefix, "");
            bool mask = n.EndsWith("Mask");
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ListeningRetreat.Ppu;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = !mask;
            ti.alphaSource = mask ? TextureImporterAlphaSource.FromGrayScale : TextureImporterAlphaSource.FromInput;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;   // 2560/2624 px sources stay full size; sizes per platform are measured later
            ti.textureCompression = TextureImporterCompression.Compressed;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.textureShape = TextureImporterShape.Texture2D;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            // Mostly empty layers get tight meshes (less overdraw); full, soft and streaky ones keep a rectangle.
            bool tight = n.StartsWith("Bench") && n != "BenchShadow" || n.StartsWith("Qori_") || n == "Tree" || n == "Lantern";
            s.spriteMeshType = tight ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    static Sprite Art(string file)
    {
        string path = ArtDir + Path.GetFileName(file);
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("[Retreat] no sprite at " + path);
        return s;
    }
    // Canvas px (top-left origin, y down) to local units (origin at the canvas centre, y up).
    static Vector3 Px(float x, float y) => new Vector3((x - 1280f) / ListeningRetreat.Ppu, (720f - y) / ListeningRetreat.Ppu, 0f);

    static GameObject Make()
    {
        var reg = JsonUtility.FromJson<Registration>(File.ReadAllText(RegistrationPath));
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
        var root = new GameObject("ListeningRetreat_Overlook");
        root.transform.position = Away;
        var r = root.AddComponent<ListeningRetreat>();

        var camGo = new GameObject("Retreat Camera");
        camGo.transform.SetParent(root.transform, false); camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = ListeningRetreat.CanvasH * .5f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.depth = 50; cam.nearClipPlane = .3f; cam.farClipPlane = 50f; cam.enabled = false;
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false; data.renderShadows = false;
        r.view = cam;

        SpriteRenderer Sprite(Transform parent, LayerEntry e, Vector3 local, int order, string name, SpriteMaskInteraction mask = SpriteMaskInteraction.None)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art(e.file); sr.sortingOrder = order; sr.maskInteraction = mask;
            if (unlit != null) sr.sharedMaterial = unlit;   // no 2D lights reach this far; unlit shows the painted values
            return sr;
        }
        var pivots = new Dictionary<string, (Transform t, Vector2 px)>();
        var fore = new List<Transform>();
        int order = 0;
        foreach (var e in reg.layers)
        {
            order += 2;
            Vector2 size = new Vector2(e.canvas[0], e.canvas[1]);
            Vector2 offset = e.offset != null && e.offset.Length == 2 ? new Vector2(e.offset[0], e.offset[1]) : Vector2.zero;
            Vector2 centre = offset + size * .5f;
            Transform parent = root.transform; Vector2 parentPx = new Vector2(1280f, 720f);
            if (!string.IsNullOrEmpty(e.parent) && pivots.TryGetValue(e.parent, out var p)) { parent = p.t; parentPx = p.px; }

            if (e.wrap == "horizontal")
            {
                // Two copies, side by side, drifting and wrapping.
                var g = Group(e.name, parent, Vector3.zero);
                Sprite(g, e, Px(centre.x, centre.y), order, e.name + " A");
                Sprite(g, e, Px(centre.x, centre.y) + new Vector3(-ListeningRetreat.CanvasW, 0f, 0f), order, e.name + " B");
                if (e.name == "Clouds") r.clouds = g; else r.mist = g;
                continue;
            }
            if (e.wrap == "vertical")
            {
                // The repeat tile stacked from the canvas top, scrolling down one period, inside the clip mask.
                var maskGo = new GameObject(e.name + " mask"); maskGo.transform.SetParent(root.transform, false);
                var m = maskGo.AddComponent<SpriteMask>(); m.sprite = Art(Prefix + e.name + "Mask.png"); m.alphaCutoff = .5f;
                var g = Group(e.name, root.transform, Vector3.zero);
                for (int i = -1; i <= (int)(1440 / e.period_px); i++)
                    Sprite(g, e, Px(centre.x, centre.y + i * e.period_px), order, e.name + " " + i, SpriteMaskInteraction.VisibleInsideMask);
                r.weather = g; r.weatherPeriod = e.period_px / ListeningRetreat.Ppu;
                continue;
            }
            Vector2? pivot = e.pivot_px != null && e.pivot_px.Length == 2 ? new Vector2(e.pivot_px[0], e.pivot_px[1])
                : e.base_pivot_canvas_px != null && e.base_pivot_canvas_px.Length == 2 ? new Vector2(e.base_pivot_canvas_px[0], e.base_pivot_canvas_px[1]) : (Vector2?)null;
            if (pivot.HasValue && e.name != "LanternGlow")
            {
                // A pivot group at the registered point, carrying the sprite (and any children).
                var g = Group(e.name + " pivot", parent, Px(pivot.Value.x, pivot.Value.y) - Px(parentPx.x, parentPx.y));
                Sprite(g, e, Px(centre.x, centre.y) - Px(pivot.Value.x, pivot.Value.y), order, e.name);
                pivots[e.name] = (g, pivot.Value);
                switch (e.name)
                {
                    case "Tree": r.tree = g; break;
                    case "Lantern": r.lantern = g; break;
                    case "Qori_Head": r.head = g; break;
                    case "Qori_EarFar": r.earFar = g; break;
                    case "Qori_EarNear": r.earNear = g; break;
                    default: if (e.name.StartsWith("Foreground")) fore.Add(g); break;
                }
                continue;
            }
            if (e.name == "Far" || e.name == "Mid")
            {
                var g = Group(e.name, parent, Vector3.zero);
                Sprite(g, e, Px(centre.x, centre.y), order, e.name);
                if (e.name == "Far") r.far = g; else r.mid = g;
                continue;
            }
            var sr = Sprite(parent, e, Px(centre.x, centre.y) - Px(parentPx.x, parentPx.y), order, e.name);
            if (e.name == "LanternGlow") r.glow = sr;
        }
        r.foreground = fore.ToArray();
        r.playerBar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/UI/UI_ListeningSpot_TrackRow_Playing.png");
        return root;
    }

    static Transform Group(string n, Transform parent, Vector3 local)
    {
        var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.localPosition = local; return go.transform;
    }

    // Builds, then renders the retreat at several screen shapes and moments into -captureDir/retreat.
    public static void BuildAndCapture()
    {
        Build();
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "retreat");
        Directory.CreateDirectory(folder);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var r = go.GetComponent<ListeningRetreat>();
        r.view.enabled = true;
        Shot(r, folder, "16x9_t0", 1920, 1080, 0f);
        Shot(r, folder, "16x9_t3", 1920, 1080, 3.1f);
        Shot(r, folder, "16x9_t7", 1920, 1080, 7.4f);
        Shot(r, folder, "16x10", 1280, 800, 0f);
        Shot(r, folder, "4x3", 1024, 768, 0f);
        Shot(r, folder, "21x9", 2560, 1080, 0f);
        // With the player as the Qvale bench shows it: 14 tracks, the first known and playing.
        var spotGo = new GameObject("Listening Spot (capture)"); spotGo.AddComponent<BoxCollider2D>();
        var spot = spotGo.AddComponent<ListeningSpot>();
        for (int i = 0; i < 14; i++) spot.tracks.Add(new ListeningSpot.Track { title = i == 0 ? "Qvale at Dusk" : "Track " + i, shell = i == 0 ? "" : "capture-" + i });
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(ListeningSpot).GetField("retreat", flags).SetValue(spot, r);
        typeof(ListeningSpot).GetMethod("Build", flags).Invoke(spot, null);
        typeof(ListeningSpot).GetField("playing", flags).SetValue(spot, 0);
        typeof(ListeningSpot).GetMethod("Refresh", flags).Invoke(spot, null);
        var ui = spotGo.GetComponentInChildren<Canvas>(true);
        ui.enabled = true; ui.renderMode = RenderMode.ScreenSpaceCamera; ui.worldCamera = r.view; ui.planeDistance = 1f;
        Shot(r, folder, "16x9_player", 1920, 1080, 0f, ui);
        Shot(r, folder, "4x3_player", 1024, 768, 0f, ui);
        Object.DestroyImmediate(spotGo);
        // Motion limits, measured on the posed transforms over two minutes.
        float head = 0f, ear = 0f, lantern = 0f, tree = 0f, fg = 0f, far = 0f, mid = 0f, glowLo = 1f, glowHi = 0f;
        float Z(Transform t) => t == null ? 0f : Mathf.Abs(Mathf.DeltaAngle(0f, t.localEulerAngles.z));
        for (float s = 0f; s < 120f; s += .05f)
        {
            r.Tick(s);
            head = Mathf.Max(head, Z(r.head)); ear = Mathf.Max(ear, Z(r.earFar), Z(r.earNear));
            lantern = Mathf.Max(lantern, Z(r.lantern)); tree = Mathf.Max(tree, Z(r.tree));
            foreach (var f in r.foreground) fg = Mathf.Max(fg, Z(f));
            far = Mathf.Max(far, Mathf.Abs(r.far.localPosition.x) * ListeningRetreat.Ppu); mid = Mathf.Max(mid, Mathf.Abs(r.mid.localPosition.x) * ListeningRetreat.Ppu);
            glowLo = Mathf.Min(glowLo, r.glow.color.a); glowHi = Mathf.Max(glowHi, r.glow.color.a);
        }
        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "[Retreat] motion: head {0:F2} deg (limit 3), ears {1:F2} (8), lantern {2:F2} (4), tree {3:F2} (0.3), foreground {4:F2} (3), far {5:F1} px (32), mid {6:F1} px (32), glow {7:F2}-{8:F2} (0.65-1.0)",
            head, ear, lantern, tree, fg, far, mid, glowLo, glowHi));
        Debug.Log("[Retreat] captures in " + folder);
    }

    static void Shot(ListeningRetreat r, string folder, string tag, int w, int h, float t, Canvas ui = null)
    {
        var cam = r.view;
        var texture = new RenderTexture(w, h, 24);
        cam.targetTexture = texture; cam.aspect = (float)w / h;
        ListeningRetreat.Fit(cam); r.Tick(t);
        if (ui != null) Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = texture;
        var read = new Texture2D(w, h, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, "retreat_" + tag + ".png"), read.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; cam.ResetAspect();
        Object.DestroyImmediate(texture); Object.DestroyImmediate(read);
    }

    static string Arg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return fallback;
    }
}
