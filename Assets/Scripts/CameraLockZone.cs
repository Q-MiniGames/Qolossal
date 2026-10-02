using System.Collections.Generic;
using UnityEngine;

// While Qori is inside this trigger, the camera is held to `view` (an arena, a guardian's floor, a
// corridor): it follows him only as far as the view's edges allow, and centres on an axis the view
// is too small for. The camera eases into and out of the lock rather than jumping.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class CameraLockZone : MonoBehaviour
{
    [Tooltip("The area the camera may show while Qori is inside.")] public Rect view = new Rect(-10f, -2f, 20f, 12f);

    static readonly List<CameraLockZone> active = new List<CameraLockZone>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => active.Clear();

    // The most recently entered zone Qori is still in, if any.
    public static CameraLockZone Current => active.Count > 0 ? active[active.Count - 1] : null;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnDisable() => active.Remove(this);

    static bool IsQori(Collider2D other) => other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    void OnTriggerEnter2D(Collider2D other) { if (IsQori(other) && !active.Contains(this)) active.Add(this); }
    void OnTriggerExit2D(Collider2D other) { if (IsQori(other)) active.Remove(this); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .7f, .2f, .9f);
        Gizmos.DrawWireCube(view.center, view.size);
    }
}
