using UnityEngine;

// Palm prototype: a background group (the titan's other fingers, its far silhouette) that copies
// a share of the camera's movement. At `alignedAt` (a camera position) the group sits exactly
// where it was built; elsewhere it has moved `follow` of the camera's offset from there.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ParallaxShape : MonoBehaviour
{
    public Vector2 follow = new Vector2(.5f, .5f);
    [Tooltip("The camera position at which the group sits where it was built.")] public Vector2 alignedAt;
    [Tooltip("Where the group was built (set by the builder).")] public Vector2 home;
    [Tooltip("Slow breathing bob, in units (0 for none).")] public float breathe;
    public Camera targetCamera;

    void LateUpdate() => Refresh(targetCamera != null ? targetCamera : Camera.main);

    public void Refresh(Camera view)
    {
        if (view == null) return;
        Vector2 cam = view.transform.position;
        Vector2 at = home + Vector2.Scale(cam - alignedAt, follow);
        if (breathe > 0f && Application.isPlaying) at.y += breathe * Mathf.Sin(Time.time * (2f * Mathf.PI / 9f));
        transform.position = new Vector3(at.x, at.y, transform.position.z);
    }
}
