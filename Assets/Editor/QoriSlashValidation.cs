#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QoriSlashValidation
{
    private const string ActiveKey = "Qori.SlashValidation.Active";
    static QoriSlashValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
                new GameObject("Slash validation").AddComponent<QoriSlashValidationRunner>();
        };
        EditorApplication.update += () =>
        {
            if (SessionState.GetBool(ActiveKey, false) &&
                EditorApplication.timeSinceStartup - SessionState.GetFloat(ActiveKey + ".Start", 0f) > 90f)
                Finish(false, "Validation timed out.");
        };
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("This check runs only in batch mode.");
        Sprite windup = Resources.Load<Sprite>("QoriCloakPrototype/Qori_AttackWindupBody_v1");
        if (windup == null || Vector2.Distance(windup.pivot, new Vector2(924f, 525f)) > 1f)
        { Finish(false, "Wind-up sprite missing or pivot incorrect."); return; }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = Vector3.zero;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        GameObject ground = new GameObject("Validation ground");
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.position = new Vector3(0f, -0.8f, 0f);
        ground.AddComponent<BoxCollider2D>().size = new Vector2(10f, 0.4f);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(ActiveKey + ".Start", (float)EditorApplication.timeSinceStartup);
        EditorApplication.EnterPlaymode();
    }

    public static void Finish(bool success, string message)
    {
        SessionState.SetBool(ActiveKey, false);
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-slashReportPath") File.WriteAllText(args[i+1], (success ? "PASS: " : "FAIL: ") + message);
        Debug.Log("Qori slash validation: " + message);
        EditorApplication.Exit(success ? 0 : 1);
    }
}

public sealed class QoriSlashValidationTarget : MonoBehaviour, IReedbladeTarget
{
    public int Hits;
    public void TakeHit() { Hits++; }
}

public sealed class QoriSlashValidationRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Queue(PlayerCombat combat) => typeof(PlayerCombat).GetField("queued", Private).SetValue(combat, true);
    private static bool Check(bool condition, string message)
    {
        if (!condition) QoriSlashValidation.Finish(false, message);
        return condition;
    }
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        PlayerMovement movement = combat.GetComponent<PlayerMovement>();
        QoriLayeredCloak cloak = combat.GetComponentInChildren<QoriLayeredCloak>();
        if (!Check(cloak != null && cloak.CanShow(10), "Layered wind-up did not initialize.")) yield break;
        GameObject targetObject = new GameObject("Slash target");
        targetObject.transform.position = combat.transform.position + Vector3.right * 0.9f;
        QoriSlashValidationTarget target = targetObject.AddComponent<QoriSlashValidationTarget>();
        targetObject.AddComponent<BoxCollider2D>().isTrigger = true;
        // Multiple colliders on one target must still receive one hit.
        targetObject.AddComponent<BoxCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms();
        Queue(combat);
        yield return new WaitForFixedUpdate();
        yield return null;
        if (!Check(combat.IsAttackWindup && target.Hits == 0, "Damage happened before the strike.")) yield break;
        Time.timeScale = 0f;
        yield return null; yield return null;
        if (!Check(combat.IsAttackWindup && target.Hits == 0, "Paused wind-up advanced.")) yield break;
        Time.timeScale = 1f;
        yield return new WaitForSeconds(0.1f);
        if (!Check(!combat.IsAttackWindup && target.Hits == 1, "Strike did not damage the target exactly once.")) yield break;
        yield return new WaitForSeconds(0.3f);
        if (!Check(!combat.IsAttackPoseActive && combat.StrikeProgress < 0f, "Attack did not finish.")) yield break;
        typeof(PlayerMovement).GetField("<FacingDirection>k__BackingField", Private).SetValue(movement, -1f);
        targetObject.transform.position = combat.transform.position + Vector3.left * 0.9f;
        Physics2D.SyncTransforms();
        Queue(combat);
        yield return new WaitForSeconds(0.14f);
        if (!Check(combat.AttackDirection < 0f && target.Hits == 2, "Left-facing attack failed.")) yield break;
        yield return new WaitForSeconds(0.3f);
        Queue(combat);
        yield return new WaitForFixedUpdate(); yield return null;
        combat.CancelAttack();
        yield return new WaitForSeconds(0.12f);
        if (!Check(!combat.IsAttackPoseActive && target.Hits == 2, "Canceled wind-up applied damage.")) yield break;
        QoriSlashValidation.Finish(true, "Sprite import and pivot; layered wind-up; no early damage; pause; one hit across two colliders; recovery; left-facing strike; cancellation.");
    }
}
#endif
