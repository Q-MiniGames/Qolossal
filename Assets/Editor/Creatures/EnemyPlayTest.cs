using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Real Play-mode check of the step-8 enemies in A0_TestRoom (batch): moves Qori next to each one
// and records how it reacts. Usage (no -quit): -executeMethod EnemyPlayTest.Run
[InitializeOnLoad]
public static class EnemyPlayTest
{
    const string Key = "Qolossal.EnemyPlayTest";
    static int stage, seedsSeen, failures, errors; static float stageAt, thornwingMinY = float.MaxValue, thornwingStartY;
    static bool thornwingDove;

    static EnemyPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[EnemyTest] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[EnemyTest] {(ok ? "PASS" : "FAIL")} {what}"); }

    static void Place(Vector2 at)
    {
        var body = Object.FindFirstObjectByType<PlayerMovement>().GetComponent<Rigidbody2D>();
        body.position = at; body.linearVelocity = Vector2.zero;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;
        var thornwing = Object.FindFirstObjectByType<ThornwingEnemy>();
        if (thornwing != null) thornwingMinY = Mathf.Min(thornwingMinY, thornwing.transform.position.y);
        seedsSeen = Mathf.Max(seedsSeen, Object.FindObjectsByType<SpitterSeed>(FindObjectsSortMode.None).Length);

        switch (stage)
        {
            case 0 when t > 1f:
                // Carrier: rig on, anchor still grappleable.
                var carrier = Object.FindFirstObjectByType<FlyingCreature>();
                Expect(carrier != null && carrier.GetComponentInChildren<CarrierAnimator>() != null
                       && ThreadAnchor.Active.Contains(carrier.GetComponent<ThreadAnchor>()), "Seed Carrier has its rig and is still a grapple anchor");
                // Shellback shell rules.
                var shell = Object.FindFirstObjectByType<ShellbackEnemy>();
                CombatDamage Hit(string id) { var w = ScriptableObject.CreateInstance<WeaponDefinition>(); w.weaponId = id; return new CombatDamage { Weapon = w, Damage = 1, Direction = Vector2.right }; }
                Expect(shell.ReceiveCombatHit(Hit("forest-0")).Disposition == CombatHitDisposition.Blocked && shell.ShellIntact, "sword bounces off the Shellback's shell");
                shell.ReceiveCombatHit(Hit("forest-2"));
                Expect(!shell.ShellIntact && shell.IsAlive, "mace cracks the Shellback's shell");
                Expect(shell.ReceiveCombatHit(Hit("forest-0")).Disposition == CombatHitDisposition.Damaged, "after cracking, the sword hurts it");
                // Spitter: stand in front of it on the plateau.
                Place(new Vector2(38.5f, 9.3f)); seedsSeen = 0; stage = 1; stageAt = t;
                break;
            case 1 when t - stageAt > 2.5f:
                Expect(seedsSeen > 0, $"Pod Spitter spits at Qori ({seedsSeen} seed(s) in flight)");
                // Thornwing: stand on the large floating island below it.
                thornwingStartY = thornwing.transform.position.y; thornwingMinY = thornwingStartY;
                Place(new Vector2(47f, 4.8f)); stage = 2; stageAt = t;
                break;
            case 2 when t - stageAt > 3f:
                thornwingDove = thornwingStartY - thornwingMinY > 1.5f;
                Expect(thornwingDove, $"Thornwing dives at Qori (dropped {thornwingStartY - thornwingMinY:F1} u)");
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[EnemyTest] finished with {failures} failure(s)");
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
                break;
        }
    }
}
