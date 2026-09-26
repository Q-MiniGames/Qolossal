using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Real Play-mode check of area travel and the saved game (batch): in A0 Qori takes the Climbing
// Moss relic and loses two hearts, stands in the portal (nothing happens until he presses up),
// uses it to reach A1, touches A1's checkpoint and uses A1's portal back. The player's own save file is set aside first and put back at the end.
// Usage (no -quit): -executeMethod AreaPlayTest.Run
[InitializeOnLoad]
public static class AreaPlayTest
{
    const string Key = "Qolossal.AreaPlayTest";
    static string Backup => GameSave.FilePath + ".areatest";
    static int stage, failures, errors; static float stageAt;

    static AreaPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[AreaTest] runtime error: " + m); } };
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

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[AreaTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static Portal PortalNamed(string id) => Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).FirstOrDefault(p => p.portalId == id);
    static void Place(Vector2 at) { var body = Qori.GetComponent<Rigidbody2D>(); body.position = at; body.linearVelocity = Vector2.zero; }
    static bool Arrived(string scene) => SceneManager.GetActiveScene().name == scene && !AreaTransition.IsTransitioning;

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.realtimeSinceStartup;
        switch (stage)
        {
            case 0 when Time.time > 1f:
            {
                var abilities = Qori.GetComponent<PlayerAbilityController>();
                Expect(!abilities.HasAbility(Relics.ClimbingMoss) && GameArea.InScene != null, "A0 is a game area and Qori starts a new game without relics");
                Object.FindObjectsByType<AbilityShrine>(FindObjectsSortMode.None).First(s => s.ability.abilityId == Relics.ClimbingMoss).Collect(abilities);
                Expect(GameSave.HasRelic(Relics.ClimbingMoss) && File.Exists(GameSave.FilePath), "taking the moss relic writes it to the save file");
                Qori.GetComponent<PlayerHealth>().SetHealth(3);
                Place(PortalNamed("a0-east").transform.position + Vector3.up * 1f);
                stage = 5; stageAt = t;
                break;
            }
            case 5 when t - stageAt > 1f:
            {
                var portal = PortalNamed("a0-east");
                Expect(portal.QoriInside && !AreaTransition.IsTransitioning && SceneManager.GetActiveScene().name == "A0_TestRoom",
                       "standing in the portal doesn't travel by itself");
                Expect(portal.Use(), "pressing up in the portal starts the trip");
                stage = 1; stageAt = t;
                break;
            }
            case 1 when Arrived(A1RoomBuilder.SceneName) && t - stageAt > .5f || stage == 1 && t - stageAt > 8f:
            {
                Expect(Arrived(A1RoomBuilder.SceneName), "the A0 portal takes Qori to A1");
                var arrival = PortalNamed("a1-west");
                Expect(arrival != null && Mathf.Abs(Qori.transform.position.x - arrival.ArrivalPoint.x) < .6f, $"he steps out beside A1's portal (x {Qori.transform.position.x:F1})");
                Expect(Qori.GetComponent<PlayerHealth>().Health == 3, "he keeps the hearts he left with");
                var abilities = Qori.GetComponent<PlayerAbilityController>();
                Expect(abilities.HasAbility(Relics.ClimbingMoss) && !abilities.HasAbility(Relics.LivingThread), "he keeps the relics he found, and only those");
                // The A1 room itself: swing rings, chains, palettes.
                Expect(ThreadAnchor.Active.Count >= 4, $"A1's three swing rings and the Seed Carrier are grapple anchors ({ThreadAnchor.Active.Count})");
                var chain = Object.FindFirstObjectByType<HangingChains>().transform.Find("Chain L").GetComponent<SpriteRenderer>();
                Expect(chain.enabled && chain.size.y > 5f, $"the hanging platform's chains reach the gallery ceiling ({chain.size.y:F1} u)");
                int a1Parts = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.sprite != null && r.sprite.name.EndsWith("_A1") && r.GetComponentInParent<CreatureRig>() != null);
                Expect(a1Parts > 12, $"A1's enemies wear their A1 palettes ({a1Parts} parts)");
                Place(Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).First(c => c.CheckpointId == "a1-entry").SpawnPosition);
                stage = 2; stageAt = t;
                break;
            }
            case 2 when t - stageAt > .6f:
            {
                string a1 = SceneManager.GetActiveScene().path;
                Expect(GameSave.CheckpointIn(a1) == "a1-entry" && GameSave.LastScene == a1, "touching A1's checkpoint saves it as where to continue");
                Place(PortalNamed("a1-west").transform.position + Vector3.up * 1f);
                stage = 6; stageAt = t;
                break;
            }
            case 6 when t - stageAt > .6f:
                Expect(PortalNamed("a1-west").Use(), "pressing up in A1's portal starts the trip back");
                stage = 3; stageAt = t;
                break;
            case 3 when Arrived("A0_TestRoom") && t - stageAt > .5f || stage == 3 && t - stageAt > 8f:
            {
                Expect(Arrived("A0_TestRoom"), "A1's portal takes Qori back to A0");
                var arrival = PortalNamed("a0-east");
                Expect(arrival != null && Mathf.Abs(Qori.transform.position.x - arrival.ArrivalPoint.x) < .6f, $"he steps out beside A0's portal (x {Qori.transform.position.x:F1})");
                Expect(Object.FindObjectsByType<AbilityShrine>(FindObjectsSortMode.None).First(s => s.ability.abilityId == Relics.ClimbingMoss).IsTaken,
                       "the moss shrine he emptied stays empty");
                Qori.Respawn();
                Expect(Mathf.Abs(Qori.transform.position.x - arrival.ArrivalPoint.x) < .6f, "with no checkpoint in A0 yet, a defeat returns him to where he came in");
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[AreaTest] finished with {failures} failure(s)");
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
