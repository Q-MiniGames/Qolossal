using UnityEngine;

// The edges the camera may not look past (the Hollow Knight room edge): the camera follows Qori
// until the edge of its view meets the room's edge, then holds while he walks on toward the side of
// the screen. Along an axis where the room is smaller than the view, the camera stays centred.
//
// A scene may place one CameraBounds to set its room explicitly. Without one, the room is measured
// once from the scene's ground: the furthest left, right and lowest solid Ground colliders. The sky
// is open (no top edge) unless `top` is set. CameraLockZone areas narrow it further while Qori is
// inside them (an arena, a guardian's floor).
[DisallowMultipleComponent]
public sealed class CameraBounds : MonoBehaviour
{
    public Rect room = new Rect(-50f, -10f, 100f, 1000f);
    [Tooltip("Hold the camera below room.yMax; off for open sky.")] public bool top;

    static bool measured; static Rect autoRoom; static bool autoValid;
    static CameraBounds placed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        measured = false; placed = null;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => measured = false;

    void OnEnable() => placed = this;
    void OnDisable() { if (placed == this) placed = null; }

    // Forget the measured room (a new scene, or terrain built at runtime).
    public static void Remeasure() => measured = false;

    // The room's edges: xMin, xMax, yMin, and yMax (infinite when the sky is open).
    public static bool TryGetRoom(out Rect rect, out bool hasTop)
    {
        if (placed != null) { rect = placed.room; hasTop = placed.top; return true; }
        hasTop = false;
        if (!measured) Measure();
        rect = autoRoom;
        return autoValid;
    }

    static void Measure()
    {
        measured = true; autoValid = false;
        int ground = LayerMask.NameToLayer("Ground");
        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue;
        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c.gameObject.layer != ground || c.isTrigger || !c.enabled) continue;
            if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;   // crates, debris
            Bounds b = c.bounds;
            xMin = Mathf.Min(xMin, b.min.x); xMax = Mathf.Max(xMax, b.max.x); yMin = Mathf.Min(yMin, b.min.y);
        }
        if (xMax <= xMin) return;
        autoRoom = Rect.MinMaxRect(xMin, yMin, xMax, float.PositiveInfinity);
        autoValid = true;
    }

    // Keeps a camera centred at `centre`, showing `halfWidth` x `halfHeight`, inside `rect`.
    public static Vector2 Clamp(Vector2 centre, float halfWidth, float halfHeight, Rect rect, bool hasTop)
    {
        centre.x = ClampAxis(centre.x, halfWidth, rect.xMin, rect.xMax);
        float yMax = hasTop ? rect.yMax : float.PositiveInfinity;
        centre.y = ClampAxis(centre.y, halfHeight, rect.yMin, yMax);
        return centre;
    }

    static float ClampAxis(float v, float half, float min, float max)
    {
        if (float.IsInfinity(max)) return Mathf.Max(v, min + half);
        if (max - min <= half * 2f) return (min + max) * .5f;
        return Mathf.Clamp(v, min + half, max - half);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.4f, .9f, 1f, .8f);
        float h = top ? room.height : 60f;
        Gizmos.DrawWireCube(new Vector3(room.center.x, room.yMin + h * .5f, 0f), new Vector3(room.width, h, 0f));
    }
}
