using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch check of PlayerMovement on the A0 test room's 30 degree slope: steps physics by hand
// (no play mode) with scripted input and logs drift while idle and grounding while walking.
// Usage: -executeMethod SlopeMovementCheck.Run
public static class SlopeMovementCheck
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        GameObject player = GameObject.Find("Player");
        var movement = player.GetComponent<PlayerMovement>();
        var body = player.GetComponent<Rigidbody2D>();
        foreach (var item in Object.FindObjectsByType<TerrainPiece>(FindObjectsSortMode.None)) item.Rebuild();
        foreach (var item in Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) item.Rebuild();

        Physics2D.simulationMode = SimulationMode2D.Script;
        Invoke(movement, "Awake");
        FieldInfo input = typeof(PlayerMovement).GetField("moveInput", Any);
        float dt = Time.fixedDeltaTime;

        // Start just above the middle of the slope (surface ~4.9 at x = 18).
        body.position = new Vector2(18f, 5.6f); body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        void Step(float move, int steps, string label)
        {
            int grounded = 0; Vector2 start = body.position;
            for (int i = 0; i < steps; i++)
            {
                input.SetValue(movement, move);
                Invoke(movement, "FixedUpdate");
                Physics2D.Simulate(dt);
                if (movement.IsGrounded) grounded++;
            }
            Vector2 end = body.position;
            Debug.Log($"[SlopeCheck] {label}: from {start:F3} to {end:F3} (moved {end - start:F3}), grounded {grounded}/{steps} steps");
        }
        Step(0f, 50, "settle");
        Step(0f, 150, "idle 3 s");
        Step(1f, 60, "walk uphill 1.2 s");
        Step(0f, 50, "stop 1 s");
        Step(-1f, 60, "walk downhill 1.2 s");
        Step(0f, 100, "idle 2 s");
    }

    static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, Any)?.Invoke(target, null);
}
