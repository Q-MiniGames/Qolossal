using UnityEngine;

// Palm prototype: while Qori stands in this trigger the camera eases out to `size`, so the hand
// (and the titan above it) comes into view; leaving it eases the camera back to its normal size.
// The lift is an offset added after CameraFollow and taken off again before the next follow.
[DefaultExecutionOrder(1000), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class VistaZone : MonoBehaviour
{
    [Min(1f)] public float size = 12f;
    [Tooltip("Camera lift while zoomed out, so more of the sky (the titan) shows above Qori.")] public float lift = 4f;
    [Min(.05f)] public float easeSeconds = 1.6f;

    static int inside, drivenFrame = -1;
    static float normalSize = -1f, current;   // current: 0 = normal, 1 = the zone's view
    static VistaZone zone;
    static Vector3 lifted;
    static Camera view;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { inside = 0; drivenFrame = -1; normalSize = -1f; current = 0f; zone = null; lifted = Vector3.zero; view = null; Suspended = false; }

    // Set while a stir moves the camera itself: the zones let go (and drop their lift).
    public static bool Suspended;
    public static float NormalSize => normalSize > 0f ? normalSize : 5f;
    /// <summary>The lift the active zone adds for rendering (it is off the camera between LateUpdate and the next Update; tests read it).</summary>
    public static float CurrentLift => zone != null && !Suspended ? zone.lift * Mathf.SmoothStep(0f, 1f, current) : 0f;

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    static bool IsQori(Collider2D other) => other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    void OnTriggerEnter2D(Collider2D other) { if (IsQori(other)) { inside++; zone = this; } }
    void OnTriggerExit2D(Collider2D other) { if (IsQori(other)) inside = Mathf.Max(0, inside - 1); }

    void Update()
    {
        if (view != null) view.transform.position -= lifted;
        lifted = Vector3.zero;
    }

    void LateUpdate()
    {
        if (drivenFrame == Time.frameCount) return;   // one zone drives the camera each frame
        drivenFrame = Time.frameCount;
        if (view == null) { view = Camera.main; if (view == null) return; }
        if (normalSize < 0f) normalSize = view.orthographicSize;
        if (Suspended) { current = 0f; return; }
        current = Mathf.MoveTowards(current, inside > 0 ? 1f : 0f, Time.deltaTime / easeSeconds);
        if (zone == null) return;
        float k = Mathf.SmoothStep(0f, 1f, current);
        view.orthographicSize = Mathf.Lerp(normalSize, zone.size, k);
        lifted = new Vector3(0f, zone.lift * k, 0f);
        view.transform.position += lifted;
    }
}
