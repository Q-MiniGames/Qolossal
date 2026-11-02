using UnityEngine;

// Authoring metadata only: no runtime callbacks, colliders, or save-state changes.
[DisallowMultipleComponent]
public sealed class UndergroundRoom : MonoBehaviour
{
    public string roomId;
    public string roomName;
    public string region;
    public Bounds localBounds;
    public Transform sharedTerrain;

    public Bounds WorldBounds
    {
        get
        {
            var result = new Bounds(transform.TransformPoint(localBounds.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                result.Encapsulate(transform.TransformPoint(localBounds.center + Vector3.Scale(localBounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
        }
    }
}
