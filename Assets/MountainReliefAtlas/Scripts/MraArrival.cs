using UnityEngine;

// A named landing: where an ordinary exit, a cave's return or a Waymark trip puts Qori in this scene.
public sealed class MraArrival : MonoBehaviour, IArrivalPoint
{
    public string arrivalId;
    [Tooltip("1 faces right, -1 left.")] public float facing = 1f;
    public string ArrivalId => arrivalId;
    public Vector2 ArrivalPosition => transform.position;
    public float ArrivalFacing => facing;
    void OnDrawGizmos() { Gizmos.color = new Color(.4f, 1f, .6f); Gizmos.DrawWireSphere(transform.position, .3f); }
}
