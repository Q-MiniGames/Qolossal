using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Real Play-mode check of the world loop (batch), from a new game in A0: the Waymark charts A0;
// the Wild Vein throws Qori somewhere it is allowed to; the Vein Gate to the Grip Knot chamber
// is unknown until used and known after; in the chamber the knot stays sealed until the
// guardian falls, then Qori's touch plays the stir, which gives Climbing Moss, wakes the knot and
// changes the chamber at once (the guardian gone, the elbow vein grown). The new vein leads to
// A1, where the same vein has grown; back in A0 the Crease is bridged, and the Wild Vein has
// settled into the shortcut to A2's Waymark. The player's own save file is set aside and put back.
// Usage (no -quit): -executeMethod WorldPlayTest.Run
[InitializeOnLoad]
public static class WorldPlayTest
{
    const string Key = "Qolossal.WorldPlayTest";
    static string Backup => GameSave.FilePath + ".worldtest";
    static int stage, failures, errors; static float stageAt;

    static WorldPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[WorldTest] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[WorldTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static T Named<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == name);
    static Portal PortalNamed(string id) => Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).FirstOrDefault(p => p.portalId == id);
    static Checkpoint CheckpointNamed(string id) => Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).FirstOrDefault(c => c.CheckpointId == id);
    static void Place(Vector2 at) { var body = Qori.GetComponent<Rigidbody2D>(); body.position = at; body.linearVelocity = Vector2.zero; }
    static bool Arrived(string scene) => SceneManager.GetActiveScene().name == scene && !AreaTransition.IsTransitioning;
    static bool Near(Vector2 at, float within = .7f) => Mathf.Abs(Qori.transform.position.x - at.x) < within;
    static void Next(int s, float t) { stage = s; stageAt = t; }

    // The Chart after the whole loop: A0 and the chamber (R1) and A2 (R3) are charted, A1 (R2)
    // isn't; the Grip Knot is awake; three veins are known. Renders it to <-captureDir>/chart.png.
    static void ChartChecks()
    {
        var chart = ChartScreen.Instance;
        Expect(chart != null, "the area has a Chart");
        if (chart == null) return;
        chart.Open();
        Expect(ChartScreen.IsOpen && Time.timeScale == 0f && GamePauseMenu.BlocksGameplayInput, "opening the Chart pauses the game and takes the controls");
        bool Has(string name) => GameObject.Find(name) != null;
        Expect(Has("Charted R1") && Has("Charted R3") && !Has("Charted R2"), "the mist clears over the charted regions (R1, R3) and not over R2");
        Expect(Has("Stir R1") && Has("Old pose R1") && !Has("Stir R2"), "the Grip stir shows the clenched hand and the old contour");
        int veins = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None).Count(i => i.name == "Vein");
        Expect(veins >= 3, $"the travelled veins are drawn ({veins})");
        Expect(Has("Qori") && Has("Icon a0-mossy-hollow") && Has("Icon r3-ribwood") && !Has("Icon r2-vein-galleries") && !Has("Icon r1-grip-knot"), "marks on the charted places and Qori, none on the uncharted Arm or the chamber he never charted");

        // Batch mode: render the overlay through the camera to look at it.
        var canvas = GameObject.Find("Chart Canvas").GetComponent<Canvas>();
        var camera = Camera.main;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        Canvas.ForceUpdateCanvases();
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/WorldCaptures";
        Directory.CreateDirectory(folder);
        var target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, "chart.png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        chart.Close();
        Expect(!ChartScreen.IsOpen && Time.timeScale == 1f && !GamePauseMenu.BlocksGameplayInput, "closing it gives the game back");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.realtimeSinceStartup;
        switch (stage)
        {
            case 0 when Time.time > 1f:
                Place(CheckpointNamed("a0-waymark").SpawnPosition);
                Next(1, t);
                break;
            case 1 when t - stageAt > .6f:
            {
                Expect(GameSave.IsCharted("a0-mossy-hollow") && GameSave.CheckpointIn(SceneManager.GetActiveScene().path) == "a0-waymark",
                       "touching A0's Waymark charts A0 and makes it the checkpoint");
                Expect(Named<StirVariant>("Crease Bridge (after grip)").transform.GetChild(0).gameObject.activeSelf == false, "a new game starts with the Crease open");
                Place(PortalNamed("a0-wild").transform.position + Vector3.up);
                Next(2, t);
                break;
            }
            case 2 when t - stageAt > .6f:
            {
                var wild = PortalNamed("a0-wild");
                Expect(wild.IsWild && !wild.Known, "the Wild Vein is wild before the Grip Knot wakes");
                Expect(wild.Use(), "the Wild Vein throws Qori somewhere");
                Next(3, t);
                break;
            }
            case 3 when !AreaTransition.IsTransitioning && t - stageAt > 1f || stage == 3 && t - stageAt > 10f:
            {
                var trip = Portal.LastWildTrip;
                Expect(SceneManager.GetActiveScene().name == trip.scene, $"it lands in {trip.scene} at {trip.arrival}");
                var arrivedAt = trip.waymark ? (Vector2?)CheckpointNamed(trip.arrival)?.SpawnPosition : PortalNamed(trip.arrival)?.ArrivalPoint;
                Expect(arrivedAt.HasValue && Near(arrivedAt.Value), $"Qori stands at that arrival (x {Qori.transform.position.x:F1})");
                Expect(trip.arrival != "a0-wild" && trip.arrival != "r1k-elbow" && trip.arrival != "a1-palm", "not at itself, nor at a vein that hasn't grown");
                Expect(GameSave.Veins.Count == 0, "a wild trip charts no vein");
                AreaTransition.Travel("A0_TestRoom", "a0-knot", Qori);   // back to A0, by the gate to the chamber
                Next(4, t);
                break;
            }
            case 4 when Arrived("A0_TestRoom") && t - stageAt > 1f:
                Place(PortalNamed("a0-knot").transform.position + Vector3.up);
                Next(5, t);
                break;
            case 5 when t - stageAt > .6f:
            {
                var gate = PortalNamed("a0-knot");
                Expect(!gate.Known, "the gate to the chamber leads somewhere unknown");
                Expect(gate.Use(), "and Qori enters it");
                Next(6, t);
                break;
            }
            case 6 when Arrived(R1GripKnotBuilder.SceneName) && t - stageAt > 1f || stage == 6 && t - stageAt > 10f:
            {
                Expect(Arrived(R1GripKnotBuilder.SceneName) && Near(PortalNamed("r1k-west").ArrivalPoint), "the vein leads to the Grip Knot chamber, beside its gate");
                Expect(GameSave.KnowsVein("a0-knot", "r1k-west") && PortalNamed("r1k-west").Known, "the vein is now known from both ends");
                var knot = Object.FindFirstObjectByType<TitanKnot>();
                Expect(knot.Current == TitanKnot.State.Sealed && PortalNamed("r1k-elbow") == null && Named<SpriteRenderer>("Dormant Vein Gate") != null,
                       "the knot is sealed, and the elbow vein is only a dormant arch");
                var gate = Object.FindFirstObjectByType<StirGate>();
                Expect(gate != null && !gate.IsOpen && gate.solid.enabled, "the finger-bone stir gate before it is closed and solid");
                Place((Vector2)knot.transform.position + Vector2.up);
                Next(7, t);
                break;
            }
            case 7 when t - stageAt > 1f:
            {
                var knot = Object.FindFirstObjectByType<TitanKnot>();
                Expect(!knot.TryWake() && !StirSequence.IsPlaying, "touching the sealed knot does nothing while the guardian lives");
                var guardian = Object.FindFirstObjectByType<GroundCreature>();
                Expect(guardian != null && guardian.transform.localScale.x > 1.5f, "the guardian (a giant Crawler) is in the chamber");
                for (int i = 0; i < 8; i++) guardian.TakeHit();
                Next(8, t);
                break;
            }
            case 8 when t - stageAt > 1.8f:
            {
                var knot = Object.FindFirstObjectByType<TitanKnot>();
                Expect(knot.Current == TitanKnot.State.Ready || knot.Current == TitanKnot.State.Waking, $"with the guardian down the thorns wither ({knot.Current})");
                if (knot.Current == TitanKnot.State.Ready) knot.TryWake();
                Expect(StirSequence.IsPlaying && GamePauseMenu.BlocksGameplayInput, "Qori's touch starts the stir, and his hands are off the controls");
                Next(9, t);
                break;
            }
            case 9 when !StirSequence.IsPlaying || t - stageAt > 30f:
            {
                var knot = Object.FindFirstObjectByType<TitanKnot>();
                Expect(!StirSequence.IsPlaying && t - stageAt < 30f, $"the stir plays through ({t - stageAt:F1} s)");
                Expect(GameSave.IsKnotAwake(Knots.Grip) && knot.Current == TitanKnot.State.Awake, "the Grip Knot is awake and saved");
                Expect(Qori.GetComponent<PlayerAbilityController>().HasAbility(Relics.ClimbingMoss) && GameSave.HasRelic(Relics.ClimbingMoss), "it gives Climbing Moss");
                Expect(Object.FindFirstObjectByType<GroundCreature>() == null && PortalNamed("r1k-elbow") != null && Named<SpriteRenderer>("Dormant Vein Gate") == null,
                       "the chamber changed at once: the guardian gone, the elbow vein grown");
                Expect(Mathf.Abs(Camera.main.orthographicSize - 5f) < .01f && !GamePauseMenu.BlocksGameplayInput, "the view and the controls are back");
                var gate = Object.FindFirstObjectByType<StirGate>();
                Expect(gate.IsOpen && !gate.solid.enabled && gate.image.sprite == gate.open, "the stir opened the finger-bone gate");
                Place(PortalNamed("r1k-elbow").transform.position + Vector3.up);
                Next(10, t);
                break;
            }
            case 10 when t - stageAt > .6f:
                Expect(PortalNamed("r1k-elbow").Use(), "Qori takes the new vein");
                Next(11, t);
                break;
            case 11 when Arrived(A1RoomBuilder.SceneName) && t - stageAt > 1f || stage == 11 && t - stageAt > 10f:
            {
                Expect(Arrived(A1RoomBuilder.SceneName) && PortalNamed("a1-palm") != null && Near(PortalNamed("a1-palm").ArrivalPoint),
                       "it leads to the Arm (A1), where the same vein has grown");
                Expect(Named<SpriteRenderer>("Dormant Vein Gate") == null, "A1's dormant arch is gone");
                Place(PortalNamed("a1-west").transform.position + Vector3.up);
                Next(12, t);
                break;
            }
            case 12 when t - stageAt > .6f:
                Expect(PortalNamed("a1-west").Use(), "Qori goes back to A0");
                Next(13, t);
                break;
            case 13 when Arrived("A0_TestRoom") && t - stageAt > 1f || stage == 13 && t - stageAt > 10f:
            {
                Expect(Arrived("A0_TestRoom"), "A1's west gate still leads to A0");
                var finger = GameObject.Find("Clenched Finger");
                var hit = Physics2D.Raycast(new Vector2(181f, 20f), Vector2.down, 30f, LayerMask.GetMask("Ground"));
                Expect(finger != null && hit.collider != null && hit.collider.name == "Clenched Finger", "in A0 the clenched finger now bridges the Crease");
                var wild = PortalNamed("a0-wild");
                Expect(!wild.IsWild, "the Wild Vein has settled");
                Place(wild.transform.position + Vector3.up);
                Next(14, t);
                break;
            }
            case 14 when t - stageAt > .6f:
                Expect(PortalNamed("a0-wild").Use(), "Qori takes the settled vein");
                Next(15, t);
                break;
            case 15 when Arrived(A2RoomBuilder.SceneName) && t - stageAt > 1.5f || stage == 15 && t - stageAt > 10f:
            {
                Expect(Arrived(A2RoomBuilder.SceneName) && Near(CheckpointNamed("a2-arena").SpawnPosition), "the settled vein is the shortcut to A2's Waymark");
                Expect(GameSave.IsCharted("r3-ribwood") && GameSave.CheckpointIn(SceneManager.GetActiveScene().path) == "a2-arena", "arriving on the Waymark charts A2 and sets the checkpoint");
                Expect(PortalNamed("a0-wild") == null && GameSave.KnowsVein("a0-wild", "a2-arena"), "and that shortcut is now known");
                ChartChecks();
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[WorldTest] finished with {failures} failure(s)");
                GameSave.Clear();
                if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); }
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                break;
            }
        }
    }
}
