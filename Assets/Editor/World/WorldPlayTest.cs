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
                var finger = Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Clenched Finger");
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
