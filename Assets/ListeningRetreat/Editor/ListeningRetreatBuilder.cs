using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Builds the listening-retreat places (the overlook terrace, Batch 25a-11, and the six Batch 26 scenes;
// Claude reviews 64-67) from the reviewed layers in Assets/ListeningRetreat/Art (copied and
// hash-checked by Tools/ListeningRetreat/copy_retreat_batch.py): sets their import settings, makes one
// prefab per place under Resources/ListeningRetreat (ListeningRetreat_<id>, which the director loads)
// and a preview scene showing the first. Layer order, canvases, offsets, pivots, parents, wrapping,
// motion ranges and lamp opacities all come from each place's Retreat_<id>_Registration.json.
// Places listed in UseV2 take the new listening-bench scenery from Art/v2/<id>/ instead (copy_retreat_v2.py).
//
// Batch: -executeMethod ListeningRetreatBuilder.BuildAndCapture -captureDir <folder>
public static class ListeningRetreatBuilder
{
    const string Root = "Assets/ListeningRetreat/";
    const string ArtDir = Root + "Art/";
    const string ResourcesDir = Root + "Resources/ListeningRetreat/";
    const string ScenePath = Root + "Retreat_Overlook.unity";
    const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    static readonly Vector3 Away = new Vector3(0f, -2000f, 0f);   // far from every level
    // Off while the 26h eye is repainted (it read as a crack; the heads are restored without it by
    // Tools/ListeningRetreat/restore_heads_no_eye.py): the Qori_Blink layer is left out.
    const bool UseEye = false;
    // Ludo's animated seated Qori (back view; Tools/IncomingArt/Ludo/Qori/Ludo-Qori-Bench frames 27-35, the bench
    // erased) replaces the painted Qori parts in every place, sized to the painted Qori's height and centred on its body.
    // False: the painted parts as before.
    const bool UseLudoSeated = true;
    const string SeatedDir = ArtDir + "LudoSeated/";
    [Serializable] class SeatedInfo { public int frameCount; public int[] canvas, clipStarts; public int clipLength, figureTopPx, figureBottomPx; public float cloakCentrePx; }

    // The seated frames, imported with their pivot at the body's centre on the seat (null when missing or switched off).
    static Sprite[] SeatedFrames(out SeatedInfo info)
    {
        info = null;
        string infoPath = SeatedDir + "Retreat_QoriSeated_info.json";
        if (!UseLudoSeated || !File.Exists(infoPath)) return null;
        info = JsonUtility.FromJson<SeatedInfo>(File.ReadAllText(infoPath));
        var frames = new Sprite[info.frameCount];
        for (int k = 0; k < info.frameCount; k++)
        {
            string path = SeatedDir + "Retreat_QoriSeated_" + (k + 1).ToString("000") + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return null;
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.spritePixelsPerUnit = 100f;
            var st = new TextureImporterSettings(); imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom; st.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(st);
            imp.spritePivot = new Vector2(info.cloakCentrePx / info.canvas[0], 1f - info.figureBottomPx / (float)info.canvas[1]);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            frames[k] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return frames;
    }

    [Serializable] class Registration { public LayerEntry[] layers; public int[] view_canvas; public QoriSeat qori_seat; }
    [Serializable] class LayerEntry
    {
        public string name, file, parent, wrap;
        public int[] canvas; public float[] offset, pivot_px, base_pivot_canvas_px, motion_degrees, opacity_range;
        public float period_px, import_max_px;
    }
    // Where the Ludo seated Qori goes in a place with no painted Qori (the v2 scenery): the frame pivot (cloak centre,
    // figure bottom) in canvas px, his height in canvas px, and the layer he is drawn just behind.
    [Serializable] class QoriSeat { public float[] anchor_px; public float figure_height_px; public string insert_before; }

    // The new listening-bench scenery (Codex Retreat v01 + Fixes v02/v02b; Claude reviews 79, 81, 83), copied by
    // Tools/ListeningRetreat/copy_retreat_v2.py into Art/v2/<id>/. Places are switched over one at a time; the rest
    // keep their earlier art, which stays in place.
    static readonly HashSet<string> UseV2 = new HashSet<string> { "OverlookTerrace", "OverlookNight", "CradleMeadow", "TerracesRain", "CausewayDusk", "RibwoodGlade", "HeightsClouds" };
    const string V2Dir = ArtDir + "v2/";

    // The art folder of a place: v2 when switched on and copied; else the overlook terrace sits in Art/ itself,
    // the others in Art/<id>/.
    static string Folder(string id) =>
        UseV2.Contains(id) && File.Exists(V2Dir + id + "/Retreat_" + id + "_Registration.json") ? V2Dir + id + "/"
        : id == "OverlookTerrace" ? ArtDir : ArtDir + id + "/";
    static string Prefab(string id) => ResourcesDir + "ListeningRetreat_" + id + ".prefab";

    [MenuItem("Qolossal/Listening Retreat/Build")]
    public static void Build()
    {
        Import();
        Directory.CreateDirectory(ResourcesDir);
        foreach (var stale in Directory.GetFiles(ResourcesDir, "*.prefab"))
            if (!ListeningRetreat.Scenes.Any(id => Path.GetFileName(stale) == "ListeningRetreat_" + id + ".prefab")) AssetDatabase.DeleteAsset(stale.Replace('\\', '/'));
        foreach (var id in ListeningRetreat.Scenes)
        {
            var root = Make(id);
            PrefabUtility.SaveAsPrefabAsset(root, Prefab(id));
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var preview = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(ListeningRetreat.Scenes[0])));
        var r = preview.GetComponent<ListeningRetreat>();
        r.previewOnStart = true; r.view.enabled = true; r.view.tag = "MainCamera";
        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        PrefabUtility.RecordPrefabInstancePropertyModifications(r.view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(r.view.gameObject);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Retreat] built " + ListeningRetreat.Scenes.Length + " places and " + ScenePath);
    }

    static string LayerName(string path)
    {
        string n = Path.GetFileNameWithoutExtension(path);   // Retreat_<id>_<Layer>
        int a = n.IndexOf('_'), b = a >= 0 ? n.IndexOf('_', a + 1) : -1;
        return b >= 0 ? n.Substring(b + 1) : n;
    }

    static void Import()
    {
        // Per-layer import size caps from the v2 registrations (layers enlarged from ~2000 px generations: 2048).
        var caps = new Dictionary<string, int>();
        if (Directory.Exists(V2Dir))
            foreach (var regPath in Directory.GetFiles(V2Dir, "Retreat_*_Registration.json", SearchOption.AllDirectories))
                foreach (var e in JsonUtility.FromJson<Registration>(File.ReadAllText(regPath)).layers)
                    if (e.import_max_px > 0f) caps[(Path.GetDirectoryName(regPath).Replace('\\', '/') + "/" + Path.GetFileName(e.file))] = (int)e.import_max_px;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            string n = LayerName(path);
            bool mask = n.EndsWith("Mask");
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            // Masks are stored at a quarter of the view's size (Tools/ListeningRetreat/copy_retreat_batch.py).
            ti.spritePixelsPerUnit = ListeningRetreat.Ppu * PixelScale(Path.GetDirectoryName(path).Replace('\\', '/') + "/") * (mask ? .25f : 1f);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = !mask;
            ti.alphaSource = mask ? TextureImporterAlphaSource.FromGrayScale : TextureImporterAlphaSource.FromInput;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = caps.TryGetValue(path, out int cap) ? cap : 4096;   // 2560/2624 px sources stay full size; sizes per platform are measured later
            ti.textureCompression = TextureImporterCompression.Compressed;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.textureShape = TextureImporterShape.Texture2D;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            // Mostly empty layers get tight meshes (less overdraw); full, soft and streaky ones keep a rectangle.
            bool tight = n.StartsWith("Bench") && n != "BenchShadow" || n.StartsWith("Qori_") || n == "Tree" || n == "Lantern" || n.StartsWith("Prop_");
            s.spriteMeshType = tight ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    static Sprite Art(string folder, string file)
    {
        string path = folder + Path.GetFileName(file);
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("[Retreat] no sprite at " + path);
        return s;
    }
    // Canvas px (top-left origin, y down) to local units (origin at the canvas centre, y up).
    // A place painted at a higher resolution (the 4K test: 3840 x 2160) keeps the same size on screen:
    // its pixels are finer, so sprites import at Ppu x scale and canvas pixels map to units / scale.
    static float scale = 1f;
    static float PixelScale(string folder)
    {
        var reg = Directory.GetFiles(folder, "Retreat_*_Registration.json");
        if (reg.Length == 0) return 1f;
        var r = JsonUtility.FromJson<Registration>(File.ReadAllText(reg[0]));
        return r.view_canvas != null && r.view_canvas.Length == 2 ? r.view_canvas[0] / 2560f : 1f;
    }
    static Vector3 Px(float x, float y) => new Vector3((x / scale - 1280f) / ListeningRetreat.Ppu, (720f - y / scale) / ListeningRetreat.Ppu, 0f);
    static float Range(float[] r, float fallback) => r != null && r.Length == 2 ? Mathf.Max(Mathf.Abs(r[0]), Mathf.Abs(r[1])) : fallback;

    static GameObject Make(string id)
    {
        string folder = Folder(id), prefix = "Retreat_" + id + "_";
        var reg = JsonUtility.FromJson<Registration>(File.ReadAllText(folder + prefix + "Registration.json"));
        scale = reg.view_canvas != null && reg.view_canvas.Length == 2 ? reg.view_canvas[0] / 2560f : 1f;
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
        var root = new GameObject("ListeningRetreat_" + id);
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
            sr.sprite = Art(folder, e.file); sr.sortingOrder = order; sr.maskInteraction = mask;
            if (unlit != null) sr.sharedMaterial = unlit;   // no 2D lights reach this far; unlit shows the painted values
            return sr;
        }
        var pivots = new Dictionary<string, (Transform t, Vector2 px)>();
        var drifters = new List<Transform>(); var drift = new List<float>();
        var swayers = new List<Transform>(); var sway = new List<float>();
        var glows = new List<SpriteRenderer>(); var glowRange = new List<Vector2>();
        int order = 0;
        var seatedFrames = SeatedFrames(out SeatedInfo seatedInfo);
        // The painted Qori's extent in this place (its body, head and ears), where the Ludo Qori goes instead.
        float qx0 = float.MaxValue, qy0 = float.MaxValue, qx1 = float.MinValue, qy1 = float.MinValue, bodyCx = 0f;
        foreach (var e in reg.layers)
        {
            if (!e.name.StartsWith("Qori_") || e.name == "Qori_Blink" || e.canvas == null) continue;
            Vector2 o = e.offset != null && e.offset.Length == 2 ? new Vector2(e.offset[0], e.offset[1]) : Vector2.zero;
            qx0 = Mathf.Min(qx0, o.x); qy0 = Mathf.Min(qy0, o.y); qx1 = Mathf.Max(qx1, o.x + e.canvas[0]); qy1 = Mathf.Max(qy1, o.y + e.canvas[1]);
            if (e.name == "Qori_Body") bodyCx = o.x + e.canvas[0] * .5f;
        }
        // Feet point, height and the layer he sits just behind: from the painted Qori, or the registration's qori_seat.
        Vector2 seatAt = new Vector2(bodyCx, qy1); float seatH = qy1 - qy0; string seatBefore = "BenchFront";
        var qs = reg.qori_seat;
        if (qs != null && qs.anchor_px != null && qs.anchor_px.Length == 2 && qs.figure_height_px > 0f)
        {
            seatAt = new Vector2(qs.anchor_px[0], qs.anchor_px[1]); seatH = qs.figure_height_px;
            seatBefore = string.IsNullOrEmpty(qs.insert_before) ? "Bench" : qs.insert_before;
        }
        bool ludoSeated = seatedFrames != null && seatH > 0f;
        foreach (var e in reg.layers)
        {
            if (e.name == "Qori_Blink" && !UseEye) continue;
            if (ludoSeated && e.name.StartsWith("Qori_")) continue;   // the Ludo Qori takes their place
            if (ludoSeated && e.name == seatBefore && r.seatedView == null)
            {
                // just behind the bench's front (or the whole v2 bench), as the painted Qori was
                order += 2;
                var go = new GameObject("Qori seated (Ludo)"); go.transform.SetParent(root.transform, false);
                go.transform.localPosition = Px(seatAt.x, seatAt.y);
                float k = seatH / Mathf.Max(1f, seatedInfo.figureBottomPx - seatedInfo.figureTopPx);   // place pixels per frame pixel
                go.transform.localScale = Vector3.one * (k / scale);
                var sv = go.AddComponent<SpriteRenderer>(); sv.sprite = seatedFrames[0]; sv.sortingOrder = order;
                if (unlit != null) sv.sharedMaterial = unlit;
                r.seatedView = sv; r.seated = seatedFrames;
                r.seatedClipStarts = seatedInfo.clipStarts ?? new int[0]; r.seatedClipLength = seatedInfo.clipLength > 0 ? seatedInfo.clipLength : seatedFrames.Length;
                var blendGo = new GameObject("Qori seated (Ludo) blend"); blendGo.transform.SetParent(go.transform, false);
                var bv = blendGo.AddComponent<SpriteRenderer>(); bv.sprite = seatedFrames[0]; bv.sortingOrder = order + 1; bv.color = new Color(1f, 1f, 1f, 0f);
                if (unlit != null) bv.sharedMaterial = unlit;
                r.seatedBlend = bv; order += 1;
            }
            order += 2;
            Vector2 size = new Vector2(e.canvas[0], e.canvas[1]);
            Vector2 offset = e.offset != null && e.offset.Length == 2 ? new Vector2(e.offset[0], e.offset[1]) : Vector2.zero;
            Vector2 centre = offset + size * .5f;
            Transform parent = root.transform; Vector2 parentPx = new Vector2(1280f, 720f) * scale;   // the canvas centre, in this place's pixels
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
                var m = maskGo.AddComponent<SpriteMask>(); m.sprite = Art(folder, prefix + e.name + "Mask.png"); m.alphaCutoff = .5f;
                var g = Group(e.name, root.transform, Vector3.zero);
                for (int i = -1; i <= (int)(1440 / e.period_px); i++)
                    Sprite(g, e, Px(centre.x, centre.y + i * e.period_px), order, e.name + " " + i, SpriteMaskInteraction.VisibleInsideMask);
                r.weather = g; r.weatherPeriod = e.period_px / scale / ListeningRetreat.Ppu;
                continue;
            }
            if (e.name == "LanternGlow")
            {
                // The flame and its glow ride on their lamp; only their opacity changes.
                var sr = Sprite(parent, e, Px(centre.x, centre.y) - Px(parentPx.x, parentPx.y), order, e.name);
                glows.Add(sr);
                glowRange.Add(e.opacity_range != null && e.opacity_range.Length == 2 ? new Vector2(e.opacity_range[0], e.opacity_range[1]) : new Vector2(.65f, 1f));
                continue;
            }
            Vector2? pivot = e.pivot_px != null && e.pivot_px.Length == 2 ? new Vector2(e.pivot_px[0], e.pivot_px[1])
                : e.base_pivot_canvas_px != null && e.base_pivot_canvas_px.Length == 2 ? new Vector2(e.base_pivot_canvas_px[0], e.base_pivot_canvas_px[1]) : (Vector2?)null;
            if (pivot.HasValue)
            {
                // A pivot group at the registered point, carrying the sprite (and any children).
                var g = Group(e.name + " pivot", parent, Px(pivot.Value.x, pivot.Value.y) - Px(parentPx.x, parentPx.y));
                var sr = Sprite(g, e, Px(centre.x, centre.y) - Px(pivot.Value.x, pivot.Value.y), order, e.name);
                pivots[e.name] = (g, pivot.Value);
                switch (e.name)
                {
                    case "Qori_Blink": r.blink = sr; sr.enabled = false; break;   // rides on the head; shown for a blink
                    case "Qori_Head": r.head = g; break;
                    case "Qori_EarFar": r.earFar = g; break;
                    case "Qori_EarNear": r.earNear = g; break;
                    default:
                        // Props and foreground sway at about two thirds of their registered range.
                        float deg = Range(e.motion_degrees, e.name == "Tree" ? .3f : e.name == "Lantern" ? 4f : e.name.StartsWith("Foreground") ? 3f : 0f);
                        if (deg > 0f) { swayers.Add(g); sway.Add(deg * .65f); }
                        break;
                }
                continue;
            }
            if (e.name == "Far" || e.name == "Mid")
            {
                var g = Group(e.name, parent, Vector3.zero);
                Sprite(g, e, Px(centre.x, centre.y), order, e.name);
                drifters.Add(g); drift.Add(e.name == "Far" ? 8f : 14f);   // of the ±32 px painted margin
                continue;
            }
            var plain = Sprite(parent, e, Px(centre.x, centre.y) - Px(parentPx.x, parentPx.y), order, e.name);
            if (e.name == "Qori_Blink") { r.blink = plain; plain.enabled = false; }   // rides on the head; shown for a blink
        }
        r.drifters = drifters.ToArray(); r.driftPx = drift.ToArray();
        r.swayers = swayers.ToArray(); r.swayDeg = sway.ToArray();
        r.glows = glows.ToArray(); r.glowRange = glowRange.ToArray();
        r.playerBar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/UI/UI_ListeningSpot_TrackRow_Playing.png");
        return root;
    }

    static Transform Group(string n, Transform parent, Vector3 local)
    {
        var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.localPosition = local; return go.transform;
    }

    // Builds, then renders every place (and the first with the player) into -captureDir/retreat.
    public static void BuildAndCapture()
    {
        Build();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // not the preview: every place sits at the same spot
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "retreat");
        Directory.CreateDirectory(folder);
        for (int k = 0; k < ListeningRetreat.Scenes.Length; k++)
        {
            string id = ListeningRetreat.Scenes[k];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(id)));
            var r = go.GetComponent<ListeningRetreat>();
            r.view.enabled = true;
            Shot(r, folder, id + "_16x9", 1920, 1080, 2.3f);
            Shot(r, folder, id + "_4x3", 1024, 768, 2.3f);
            if (k == 0) Shot(r, folder, id + "_21x9", 2560, 1080, 0f);
            if (id.StartsWith("OverlookTerrace")) Shot(r, folder, id + "_3840", 3840, 2160, 2.3f);   // the 4K comparison
            // Texture memory of this place as imported (what the game loads when it shows it).
            var textures = new HashSet<Texture2D>();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) if (sr.sprite != null) textures.Add(sr.sprite.texture);
            foreach (var m in go.GetComponentsInChildren<SpriteMask>(true)) if (m.sprite != null) textures.Add(m.sprite.texture);
            long bytes = 0, raw = 0;
            foreach (var tex in textures) { bytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex); raw += (long)tex.width * tex.height * 4; }
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[Retreat] {0}: {1} textures, {2:F1} MiB as imported in the editor ({3:F1} MiB if raw RGBA)", id, textures.Count, bytes / 1048576.0, raw / 1048576.0));
            // With the player as the Qvale bench shows it: 14 tracks, the first known and playing.
            ListeningRetreatDirector.Current = k;
            var spotGo = new GameObject("Listening Spot (capture)"); spotGo.AddComponent<BoxCollider2D>();
            var spot = spotGo.AddComponent<ListeningSpot>();
            spot.hasSeat = true;
            for (int i = 0; i < 14; i++) spot.tracks.Add(new ListeningSpot.Track { title = i == 0 ? "Qvale at Dusk" : "Track " + i, shell = i == 0 ? "" : "capture-" + i });
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(ListeningSpot).GetField("barSprite", flags).SetValue(spot, r.playerBar);
            typeof(ListeningSpot).GetMethod("Build", flags).Invoke(spot, null);
            typeof(ListeningSpot).GetField("playing", flags).SetValue(spot, 0);
            typeof(ListeningSpot).GetMethod("Refresh", flags).Invoke(spot, null);
            var ui = spotGo.GetComponentInChildren<Canvas>(true);
            ui.enabled = true; ui.renderMode = RenderMode.ScreenSpaceCamera; ui.worldCamera = r.view; ui.planeDistance = 1f;
            Shot(r, folder, id + "_16x9_player", 1920, 1080, 2.3f, ui);
            Object.DestroyImmediate(spotGo);
            // Motion, measured on the posed transforms over two minutes, against each registered range.
            float head = 0f, ear = 0f, swayRatio = 0f, drift = 0f, glowLo = 1f, glowHi = 0f;
            float Z(Transform t) => t == null ? 0f : Mathf.Abs(Mathf.DeltaAngle(0f, t.localEulerAngles.z));
            for (float s = 0f; s < 120f; s += .05f)
            {
                r.Tick(s);
                head = Mathf.Max(head, Z(r.head)); ear = Mathf.Max(ear, Z(r.earFar), Z(r.earNear));
                for (int i = 0; i < r.swayers.Length; i++) swayRatio = Mathf.Max(swayRatio, Z(r.swayers[i]) / (r.swayDeg[i] / .65f));
                foreach (var d in r.drifters) drift = Mathf.Max(drift, Mathf.Abs(d.localPosition.x) * ListeningRetreat.Ppu);
                for (int i = 0; i < r.glows.Length; i++) { float a = r.glows[i].color.a; glowLo = Mathf.Min(glowLo, a - r.glowRange[i].x); glowHi = Mathf.Max(glowHi, a - r.glowRange[i].y); }
            }
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[Retreat] {0}: head {1:F2} deg (limit 3), ears {2:F2} (8), props and foreground at most {3:P0} of their registered range ({4} pieces), drift {5:F1} px (32), lamps {6}",
                id, head, ear, swayRatio, r.swayers.Length, drift, r.glows.Length == 0 ? "none" : glowLo >= -.001f && glowHi <= .001f ? "inside their opacity range" : "OUTSIDE their opacity range"));
            Object.DestroyImmediate(go);
        }
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
