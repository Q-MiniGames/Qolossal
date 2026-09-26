using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Real Play-mode check of A1's 9c content (batch): puts Qori beside each new threat and records
// how it reacts: a Ripple Newt surfacing and leaping, the Burrow Grub erupting, a falling rock
// dropping, the secret wall fading and its heart seed adding a heart, and a Gust Moth (spawned
// here; it lives in A3) pushing Qori back. The player's own save file is set aside and restored.
// Usage (no -quit): -executeMethod A1PlayTest.Run
[InitializeOnLoad]
public static class A1PlayTest
{
    const string Key = "Qolossal.A1PlayTest";
    static string Backup => GameSave.FilePath + ".a1test";
    static int stage, failures, errors, hearts; static float stageAt;
    static bool newtUp, newtLanded, grubUp, rockFell;
    static GustMothEnemy moth; static float qoriX;

    static A1PlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[A1Test] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A1_Aqueduct.unity");
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[A1Test] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static void Place(Vector2 at) { var body = Qori.GetComponent<Rigidbody2D>(); body.position = at; body.linearVelocity = Vector2.zero; }
    // The first ground below `fromY` at x (start below a ceiling to land on the floor under it).
    static float GroundAt(float x, float fromY = 30f) => Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, 60f, LayerMask.GetMask("Ground")).point.y;
    static T Nearest<T>(float x) where T : Component =>
        Object.FindObjectsByType<T>(FindObjectsSortMode.None).OrderBy(c => Mathf.Abs(c.transform.position.x - x)).First();

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;
        var newt = Object.FindObjectsByType<NewtEnemy>(FindObjectsSortMode.None).OrderBy(n => n.transform.position.x).FirstOrDefault();
        if (stage == 1 && newt != null)
        {
            newtUp |= !newt.IsSubmerged;
            newtLanded |= newt.transform.position.x < 14f && !newt.IsSubmerged;
        }
        if (stage == 2) grubUp |= !Nearest<GrubEnemy>(93f).IsBuried;
        if (stage == 3) rockFell |= !Nearest<FallingRock>(110.5f).IsResting;

        switch (stage)
        {
            case 0 when t > 1f:
                Expect(Object.FindObjectsByType<NewtEnemy>(FindObjectsSortMode.None).Length == 2 && newt.IsSubmerged, "two Ripple Newts wait under the streams");
                Place(new Vector2(10.5f, 1f)); stage = 1; stageAt = t;
                break;
            case 1 when t - stageAt > 3.5f:
                Expect(newtUp, "the entry Newt surfaces when Qori comes near");
                Expect(newtLanded, "and leaps out onto the bank toward him");
                Place(new Vector2(97f, GroundAt(97f) + 1f)); stage = 2; stageAt = t;
                break;
            case 2 when t - stageAt > 5f:
                Expect(Nearest<GrubEnemy>(93f).IsAlive && grubUp, "the Burrow Grub tunnels over and erupts next to Qori");
                Place(new Vector2(110.5f, GroundAt(110.5f, 2f) + 1f)); stage = 3; stageAt = t;
                break;
            case 3 when t - stageAt > 2f:
                Expect(rockFell, "a loose ceiling rock drops when Qori walks under it");
                hearts = Qori.GetComponent<PlayerHealth>().MaximumHealth;
                Place(new Vector2(143.5f, GroundAt(143.5f, -2f) + 1f)); stage = 4; stageAt = t;
                break;
            case 4 when t - stageAt > 1.2f:
            {
                var wall = Object.FindFirstObjectByType<SecretWall>();
                Expect(wall.Revealed && wall.overlay.color.a < .5f, $"the false wall fades while Qori is inside it (alpha {wall.overlay.color.a:F2})");
                Place(Object.FindFirstObjectByType<HeartSeed>().transform.position + Vector3.up * .3f); stage = 5; stageAt = t;
                break;
            }
            case 5 when t - stageAt > .8f:
            {
                var health = Qori.GetComponent<PlayerHealth>();
                Expect(health.MaximumHealth == hearts + 1 && health.Health == health.MaximumHealth && GameSave.HasPickup("a1-heartseed"),
                       $"the heart seed adds a heart for good ({hearts} -> {health.MaximumHealth}, saved {GameSave.HasPickup("a1-heartseed")})");
                // A Gust Moth, facing Qori across open floor in the gallery.
                float x = 24f, y = GroundAt(x);   // open entry ground, clear of the other enemies
                Place(new Vector2(x, y + 1f));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GustMoth01.prefab");
                moth = Object.Instantiate(prefab, new Vector3(x + 3f, y + 1.2f, 0f), Quaternion.identity).GetComponent<GustMothEnemy>();
                qoriX = x; stage = 6; stageAt = t;
                break;
            }
            case 6 when t - stageAt > 2f:
            {
                float moved = Qori.transform.position.x - qoriX;
                Expect(moth != null && moved < -.8f, $"the Gust Moth's blast pushes Qori back ({moved:F2} u)");
                Expect(!moth.HurtsOnContact, "the Gust Moth never hurts on contact");
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[A1Test] finished with {failures} failure(s)");
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
