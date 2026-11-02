using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Test-only. Builds Assets/Characters/FrameAnimation/QoriWalkTest.unity: the Qori movement lab with the Young Adventurer walk
// (Codex Batch 27b, 16 painted frames in Assets/Characters/FrameAnimation/Art, copied hash-checked from the delivery) worn by the
// real Player, plus a demo walker at the drawn speed on the flat lane.
//   Menu: Qolossal > Qori Walk Test > Build Scene, then open the scene and press Play.
//   Batch: -executeMethod QoriWalkTestBuilder.Run -captureDir <folder>   (builds, plays a scripted capture, quits)
[InitializeOnLoad]
public static class QoriWalkTestBuilder
{
    const string Root = "Assets/Characters/FrameAnimation";
    const string ArtFolder = Root + "/Art";
    const string ScenePath = "Assets/Development/Animation/QoriWalkTest.unity";
    const string LabScene = "Assets/Development/LegacyScenes/QoriMovementLab.unity";
    const string Key = "QoriWalkTest.Running";
    // Canvas, ground line and pivot column of the frames (Batch 27 registration).
    const float CanvasPx = 1400f, FootLineRow = 1340f, PivotColumn = 760f;

    static QoriWalkTestBuilder()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            QoriWalkTestDriver.OutDir = SessionState.GetString(Key + ".Dir", "Temp/QoriWalkTest");
            new GameObject("Qori walk test driver").AddComponent<QoriWalkTestDriver>();
            EditorApplication.update += WaitForDone;
        };
    }

    static void WaitForDone()
    {
        if (!QoriWalkTestDriver.Done) return;
        EditorApplication.update -= WaitForDone;
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(0);
    }

    [System.Serializable] class HandsInfo { public float[] handsPx; }
    [System.Serializable] class ClipInfo { public int frameCount; public int[] canvas; public int groundRow; public int pivotColumn; public float figureHeightPx; public float figureWidthPx; public float strideCanvasPx; public float[] heartPx; }

    // A cleaned Ludo.ai clip (Tools/ArtRequests/LudoRun/clean_ludo_frames.py): Qori_<name>_NN.png and Qori_<name>_info.json.
    static Sprite[] ImportClip(string folder, string name, out ClipInfo info, string prefix = "Qori_")
    {
        info = null;
        string infoPath = folder + "/" + prefix + name + "_info.json";
        if (!File.Exists(infoPath)) return null;
        info = JsonUtility.FromJson<ClipInfo>(File.ReadAllText(infoPath));
        var sprites = new Sprite[info.frameCount];
        for (int k = 1; k <= info.frameCount; k++)
        {
            string path = folder + "/" + prefix + name + "_" + k.ToString("00") + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Debug.LogError("[QoriWalkTest] missing " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.spritePivot = new Vector2(info.pivotColumn / (float)info.canvas[0], (info.canvas[1] - info.groundRow) / (float)info.canvas[1]);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Uncompressed;
            // 2048: the attack canvases reach 1216 px, and at 1024 Unity shrank them (and Qori with them)
            imp.SaveAndReimport();
            sprites[k - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprites;
    }

    static Sprite[] ImportFrames(out float figureHeightPx)
    {
        var sprites = new Sprite[16];
        figureHeightPx = 0f;
        for (int k = 1; k <= 16; k++)
        {
            string path = ArtFolder + "/Qori_Walk_" + k.ToString("00") + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Debug.LogError("[QoriWalkTest] missing " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.spritePivot = new Vector2(PivotColumn / CanvasPx, (CanvasPx - FootLineRow) / CanvasPx);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;   // test only: no import profile decided yet
            imp.SaveAndReimport();
            sprites[k - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (k == 5)
            {
                // figure height of frame 5, ears to soles, from the PNG itself
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(path));
                var px = tex.GetPixels32();
                int top = -1, bottom = -1;
                for (int y = tex.height - 1; y >= 0 && top < 0; y--)
                    for (int x = 0; x < tex.width; x++) if (px[y * tex.width + x].a > 128) { top = y; break; }
                for (int y = 0; y < tex.height && bottom < 0; y++)
                    for (int x = 0; x < tex.width; x++) if (px[y * tex.width + x].a > 128) { bottom = y; break; }
                figureHeightPx = top - bottom + 1;
                Object.DestroyImmediate(tex);
            }
        }
        return sprites;
    }

    [MenuItem("Qolossal/Qori Walk Test/Build Scene")]
    public static void BuildScene()
    {
        var frames = ImportFrames(out float figure);
        if (frames == null) return;
        EditorSceneManager.OpenScene(LabScene, OpenSceneMode.Single);
        var labComp = Object.FindAnyObjectByType<QoriMovementLab>();
        var so = new SerializedObject(labComp);
        var player = so.FindProperty("playerPrefab").objectReferenceValue;
        var anchor = so.FindProperty("anchorPrefab").objectReferenceValue;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var labGo = new GameObject("Qori movement lab (Young Adventurer walk test)");
        var newLab = labGo.AddComponent<QoriMovementLab>();
        var nso = new SerializedObject(newLab);
        nso.FindProperty("playerPrefab").objectReferenceValue = player;
        nso.FindProperty("anchorPrefab").objectReferenceValue = anchor;
        nso.ApplyModifiedPropertiesWithoutUndo();
        var walk = new GameObject("Young Adventurer walk on the Player").AddComponent<QoriWalkFramesOnPlayer>();
        gameEnemyEntries.Clear();
        gameEnemies = new GameObject("LudoEnemyFrames").AddComponent<LudoEnemyLibrary>();
        // Ludo.ai walk and run; the Codex Batch 27b walk for comparison. Cycle times are chosen for the game's speeds (5 walk,
        // 7.5 run): the generated strides are much shorter than those speeds cover, so locking frames to distance would race.
        walk.codexWalk = new QoriWalkFramesOnPlayer.Clip { label = "Codex walk", frames = frames, figureHeightPx = figure, strideCanvasPx = 491.8f, cycleSeconds = .72f, refSpeed = 1.06f, standFrame = 4 };
        var li = ImportClip(Root + "/ArtLudoIdle", "LudoIdle", out ClipInfo ii);
        if (li != null) walk.idle = new QoriWalkFramesOnPlayer.Clip { label = "Ludo idle", frames = li, figureHeightPx = ii.figureHeightPx, strideCanvasPx = 1f, cycleSeconds = 2.4f, refSpeed = 0f, standFrame = 0 };
        // Ludo clips 1-5 and the slash, cut by export_ludo_segments.py into Assets/Characters/FrameAnimation/ArtLudo/<name>.
        QoriWalkFramesOnPlayer.Clip Seg(string name, float fps, bool facesLeft = false)
        {
            var fr = ImportClip(Root + "/ArtLudo/" + name, name, out ClipInfo si);
            return fr == null ? new QoriWalkFramesOnPlayer.Clip() : new QoriWalkFramesOnPlayer.Clip { label = "Ludo " + name, frames = fr, figureHeightPx = si.figureHeightPx, strideCanvasPx = 1f, cycleSeconds = fr.Length / fps, fps = fps, facesLeft = facesLeft };
        }
        // Ludo's sounds (one 3 s track per source clip, 12 frames a second): each animation gets the stretch of its frames.
        void Snd(QoriWalkFramesOnPlayer.Clip c, string file, int first, int last, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/AudioLudo/" + file + ".mp3");
            if (clip == null || c == null) return;
            c.sound = clip; c.soundStart = first / 12f; c.soundEnd = (last + 1) / 12f; c.soundLoop = loop;
        }
        walk.jumpTakeoff = Seg("JumpTakeoff", 28f); walk.jumpRise = Seg("JumpRise", 12f); walk.jumpFall = Seg("JumpFall", 12f); walk.jumpLand = Seg("JumpLand", 24f);
        walk.dash = Seg("Dash", 30f); walk.glideOpen = Seg("GlideOpen", 24f); walk.glide = Seg("Glide", 12f);
        walk.wallSlide = Seg("WallSlide", 12f); walk.wallJump = Seg("WallJump", 24f, true);
        walk.ledgeHang = Seg("LedgeHang", 6f); walk.ledgeHang.pingPong = true;   // one hand on the edge, body dangling
        walk.ledgeClimb = Seg("LedgeClimb", 26f); walk.slash = Seg("Slash", 40f); walk.strike = Seg("Strike", 30f);
        Snd(walk.jumpTakeoff, "LudoJump", 26, 31, false); Snd(walk.jumpRise, "LudoJump", 32, 35, false); Snd(walk.jumpFall, "LudoJump", 5, 10, false);
        Snd(walk.jumpLand, "LudoJump", 11, 17, false);   // the jump redo's frames
        Snd(walk.dash, "LudoDashGlide", 5, 16, false); Snd(walk.glideOpen, "LudoDashGlide", 17, 23, false);
        Snd(walk.glide, "LudoDashGlide", 24, 35, true); Snd(walk.wallSlide, "LudoWallSlide", 0, 35, true); Snd(walk.wallJump, "LudoWall", 20, 31, false);
        Snd(walk.ledgeClimb, "LudoLedge", 12, 35, false);   // the ledge redo's track
        // the strike's sound: from just before the swing (the raise plays first at 30 fps), to the end of the follow-through
        var strikeSnd = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/AudioLudo/LudoStrike.mp3");
        if (strikeSnd != null) { walk.strike.sound = strikeSnd; walk.strike.soundStart = 19 / 12f - 4 / 30f; walk.strike.soundEnd = 32 / 12f; }
        // the attack set A1-A4: the frame where each hit lands (the attack's phases stretch the frames round it)
        QoriWalkFramesOnPlayer.Clip Hit(string name, int contact) { var c = Seg(name, 15f); c.contactFrame = contact; return c; }
        walk.combo1 = File.Exists(Root + "/ArtLudo/SwingTrail/Qori_SwingTrail_info.json") ? Hit("SwingTrail", 4)   // the single attack with the leaf trail
            : File.Exists(Root + "/ArtLudo/Swing/Qori_Swing_info.json") ? Hit("Swing", 4) : Hit("Combo1", 4);
        if (walk.combo1.frames != null && walk.combo1.frames.Length > 0 && walk.combo1.label.Contains("Swing")) Snd(walk.combo1, "LudoSwing", 4, 34, false); walk.combo2 = Hit("Combo2", 1); walk.combo3 = Hit("Combo3", 3); walk.upAttack = File.Exists(Root + "/ArtLudo/UpAttackTrail/Qori_UpAttackTrail_info.json") ? Hit("UpAttackTrail", 3) : Hit("UpAttack", 2); Snd(walk.upAttack, "LudoUpper", 12, 25, false);
        walk.sprintAttack = File.Exists(Root + "/ArtLudo/SprintTrail/Qori_SprintTrail_info.json") ? Hit("SprintTrail", 3) : Hit("SprintAttack", 2); walk.chargeRelease = Hit("ChargeRelease", 1);
        walk.doubleJump = File.Exists(Root + "/ArtLudo/DoubleJump3/Qori_DoubleJump3_info.json") ? Seg("DoubleJump3", 24f)   // take 3: leaf puff and flip (frames 7-22)
            : Seg("DoubleJump", 24f);   // Ludo double jump take 2: the forward flip (frames 10-22), once per double jump
        Snd(walk.doubleJump, "LudoDoubleJump", 10, 22, false);
        walk.ropeHang = Seg("RopeHang", 12f);
        var ropeInfo = File.Exists(Root + "/ArtLudo/RopeHang/Qori_RopeHang_info.json") ? JsonUtility.FromJson<HandsInfo>(File.ReadAllText(Root + "/ArtLudo/RopeHang/Qori_RopeHang_info.json")) : null;
        if (ropeInfo != null && ropeInfo.handsPx != null && ropeInfo.handsPx.Length == 2) walk.ropeHandsPx = new Vector2(ropeInfo.handsPx[0], ropeInfo.handsPx[1]);
        walk.staffShot = Seg("StaffShot", 15f); walk.staffShot.contactFrame = 16;   // the bolt leaves the tip at frame 16
        Snd(walk.staffShot, "LudoStaffShot", 0, 35, false);
        walk.evade = Seg("Evade", 32f);   // the Ludo dodge roll, every other frame 6-22: the whole roll in the quarter-second evade
        Snd(walk.evade, "LudoEvade", 6, 22, false);
        walk.plungeStart = Seg("PlungeStart", 30f); walk.plungeDive = Seg("PlungeDive", 14f); walk.plungeLand = Seg("PlungeLand", 28f);
        Snd(walk.plungeStart, "LudoPlunge", 7, 10, false); Snd(walk.plungeDive, "LudoPlunge", 11, 18, true); Snd(walk.plungeLand, "LudoPlunge", 19, 27, false);
        walk.chargeStart = Seg("ChargeStart", 15f); walk.chargeHold = Seg("ChargeHold", 12f);
        var lw = ImportClip(Root + "/ArtLudoWalk", "LudoWalk", out ClipInfo wi);
        if (lw != null) walk.walk = new QoriWalkFramesOnPlayer.Clip { label = "Ludo walk", frames = lw, figureHeightPx = wi.figureHeightPx, strideCanvasPx = wi.strideCanvasPx, cycleSeconds = .8f, refSpeed = 5f, standFrame = 0 };
        var lr = ImportClip(Root + "/ArtLudoRun", "LudoRun", out ClipInfo ri);
        if (lr != null) walk.run = new QoriWalkFramesOnPlayer.Clip { label = "Ludo run", frames = lr, figureHeightPx = ri.figureHeightPx, strideCanvasPx = ri.strideCanvasPx, cycleSeconds = .46f, refSpeed = 7.5f, standFrame = 0 };
        Snd(walk.run, "LudoRun", 3, 17, true);   // the run loop is frames 3-17 of its clip
        // The Thornheart (the Ludo v4 model, cut by export_thornheart.py), asleep on the lower floor's far side: it wakes
        // when Qori drops down to that floor.
        var thI = ImportClip(Root + "/ArtThornheart/Idle", "Idle", out ClipInfo thInfo, "TH_");
        if (thI != null)
        {
            var th = new GameObject("Thornheart (test)");
            th.transform.position = new Vector3(24f, -4f, 0f);
            th.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var boss = th.AddComponent<ThornheartBoss>();
            boss.idle = thI;
            boss.raise = ImportClip(Root + "/ArtThornheart/Raise", "Raise", out _, "TH_");
            boss.hurt = ImportClip(Root + "/ArtThornheart/Hurt", "Hurt", out _, "TH_");
            boss.death = ImportClip(Root + "/ArtThornheart/Death", "Death", out _, "TH_");
            boss.figureHeightPx = thInfo.figureHeightPx;
            boss.pivotPx = new Vector2(thInfo.pivotColumn, thInfo.groundRow);
            if (thInfo.heartPx != null && thInfo.heartPx.Length == 2) boss.heartPx = new Vector2(thInfo.heartPx[0], thInfo.heartPx[1]);
            // it wakes only once Qori is past the other enemies on its floor (x 15 and on)
            var bso = new SerializedObject(boss); bso.FindProperty("wakeRange").floatValue = 9f; bso.ApplyModifiedPropertiesWithoutUndo();
        }
        // Two Bramble Crawlers (the real GroundCreature) on the lower floor, drawn with the Ludo frames (export_crawler.py).
        var crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GroundCreature01.prefab");
        var crW = ImportClip(Root + "/ArtCrawler/Walk", "Walk", out ClipInfo crInfo, "CR_");
        if (crawlerPrefab != null && crW != null)
        {
            var crWarn = ImportClip(Root + "/ArtCrawler/Warn", "Warn", out _, "CR_");
            var crCharge = ImportClip(Root + "/ArtCrawler/Charge", "Charge", out _, "CR_");
            var crHurt = ImportClip(Root + "/ArtCrawler/Hurt", "Hurt", out _, "CR_");
            var crDeath = ImportClip(Root + "/ArtCrawler/Death", "Death", out _, "CR_");
            foreach (float x in new[] { 5f, 12f })
            {
                var cr = (GameObject)PrefabUtility.InstantiatePrefab(crawlerPrefab);
                cr.name = "Bramble Crawler (Ludo frames, test)";
                cr.transform.position = new Vector3(x, -3.4f, 0f);
                var crFrames = cr.AddComponent<CrawlerFramesOnCreature>();
                crFrames.walk = crW; crFrames.warn = crWarn; crFrames.charge = crCharge; crFrames.hurt = crHurt; crFrames.death = crDeath;
                crFrames.figureWidthPx = crInfo.figureWidthPx;
            }
            gameEnemies.crawlerWalk = crW; gameEnemies.crawlerWarn = crWarn; gameEnemies.crawlerCharge = crCharge;
            gameEnemies.crawlerHurt = crHurt; gameEnemies.crawlerDeath = crDeath; gameEnemies.crawlerFigureWidthPx = crInfo.figureWidthPx;
        }
        // The Thornwing, Pod Spitter, Shellback and Ripple Newt (the real enemies), drawn with the Ludo frames
        // (export_enemies.py). Along the lower floor between the crawlers and the Thornheart; the Thornwing hovers over it.
        PlaceEnemy("Thornwing01", "Thornwing", EnemyFramesOnCreature.Kind.Thornwing, new Vector3(8f, -1.2f, 0f), 1.9f, "Hover", "Warn", "Dive", "Hurt", "Death");
        PlaceEnemy("PodSpitter01", "PodSpitter", EnemyFramesOnCreature.Kind.PodSpitter, new Vector3(15.5f, -4f, 0f), 1.5f, "Idle", "Swell", "Spit", "Hurt", "Death");
        PlaceEnemy("Shellback01", "Shellback", EnemyFramesOnCreature.Kind.Shellback, new Vector3(2.5f, -3.9f, 0f), 1.7f, "Walk", "Crack", "CrackedWalk", "Hurt", "Death");
        PlaceEnemy("Grub01", "Grub", EnemyFramesOnCreature.Kind.Grub, new Vector3(10.5f, -4f, 0f), 1.9f, "Erupt", "Up", "Sink", "Hurt", "Death");
        PlaceEnemy("GustMoth01", "GustMoth", EnemyFramesOnCreature.Kind.GustMoth, new Vector3(13.5f, -2f, 0f), 1.9f, "Hover", "Raise", "Beat", "Death");
        PlaceEnemy("Sentinel01", "Sentinel", EnemyFramesOnCreature.Kind.Sentinel, new Vector3(-0.5f, -3.9f, 0f), 3.05f, "Walk", "Guard", "Draw", "Thrust", "Hurt", "Death");
        PlaceEnemy("Newt01", "Newt", EnemyFramesOnCreature.Kind.Newt, new Vector3(9f, -4f, 0f), 1.9f, "Crouch", "Leap", "Land", "Look", "Spring", "Hurt", "Death");
        SaveGamePrefabs(walk);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[QoriWalkTest] scene " + ScenePath + "; 16 frames imported (PPU 100, pivot on the ground line); frame 5 figure " + figure + " px");
    }

    // One of the enemies drawn with its Ludo frames: the prefab, its frame sets from ArtEnemies/<folder>/<set>, and the
    // width its reference frame is drawn at (world units).
    static void PlaceEnemy(string prefab, string folder, EnemyFramesOnCreature.Kind kind, Vector3 at, float worldWidth, params string[] setNames)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab");
        if (source == null) { Debug.LogWarning("[QoriWalkTest] no prefab " + prefab); return; }
        var sets = new System.Collections.Generic.List<EnemyFramesOnCreature.Set>();
        float widthPx = 0f;
        foreach (string n in setNames)
        {
            var fr = ImportClip(Root + "/ArtEnemies/" + folder + "/" + n, folder + "_" + n, out ClipInfo info, "");
            if (fr == null) continue;
            sets.Add(new EnemyFramesOnCreature.Set { name = n, frames = fr });
            widthPx = info.figureWidthPx;
        }
        if (sets.Count == 0) return;
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(source);
        obj.name = folder + " (Ludo frames, test)";
        obj.transform.position = at;
        var f = obj.AddComponent<EnemyFramesOnCreature>();
        f.kind = kind; f.sets = sets.ToArray(); f.worldWidth = worldWidth; f.figureWidthPx = widthPx;
        gameEnemyEntries.Add(new LudoEnemyLibrary.Entry { kind = kind, sets = sets.ToArray(), worldWidth = worldWidth, figureWidthPx = widthPx });
    }

    // The game's copies (LudoFramesInstaller): Qori's frame player (test keys off) and every enemy kind's frame sets,
    // saved as prefabs under Resources/LudoFrames so every scene gets them.
    static readonly System.Collections.Generic.List<LudoEnemyLibrary.Entry> gameEnemyEntries = new System.Collections.Generic.List<LudoEnemyLibrary.Entry>();
    static LudoEnemyLibrary gameEnemies;
    const string GameFramesDir = "Assets/Resources/LudoFrames";

    static void SaveGamePrefabs(QoriWalkFramesOnPlayer walk)
    {
        if (!AssetDatabase.IsValidFolder(GameFramesDir)) AssetDatabase.CreateFolder("Assets/Resources", "LudoFrames");
        var qori = Object.Instantiate(walk.gameObject); qori.name = "LudoQoriFrames";
        var q = qori.GetComponent<QoriWalkFramesOnPlayer>(); q.testControls = false; q.help = false;
        PrefabUtility.SaveAsPrefabAsset(qori, GameFramesDir + "/LudoQoriFrames.prefab"); Object.DestroyImmediate(qori);
        gameEnemies.enemies = gameEnemyEntries.ToArray();
        PrefabUtility.SaveAsPrefabAsset(gameEnemies.gameObject, GameFramesDir + "/LudoEnemyFrames.prefab");
        Object.DestroyImmediate(gameEnemies.gameObject);
        Debug.Log("[QoriWalkTest] game prefabs saved: LudoQoriFrames, LudoEnemyFrames (" + gameEnemyEntries.Count + " enemy kinds and the crawler)");
    }

    public static void Run()
    {
        BuildScene();
        string dir = "Temp/QoriWalkTest";
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-captureDir") dir = args[i + 1];
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        SessionState.SetString(Key + ".Dir", dir);
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }
}
