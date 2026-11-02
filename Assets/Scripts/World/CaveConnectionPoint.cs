using UnityEngine;

[DisallowMultipleComponent]
public sealed class CaveConnectionPoint : MonoBehaviour
{
    public string connectionId, upperScene, upperRegion, upperAreaId, upperAreaName;
    public string lowerRoomId, lowerRoomName, requirements, activation;
    public bool undergroundSide;
    public Component entrance;
    public Component arrival;
    public string ArrivalId => arrival is MraArrival a ? a.arrivalId : arrival is Checkpoint c ? c.CheckpointId : "";
    public Vector3 ArrivalPosition => arrival is Checkpoint c ? (Vector3)c.SpawnPosition : arrival != null ? arrival.transform.position : transform.position;
    public Vector3 EntrancePosition => entrance != null ? entrance.transform.position : transform.position;
    public Bounds FocusBounds
    {
        get
        {
            var bounds = new Bounds(EntrancePosition + Vector3.up * 2, new Vector3(12, 10, 1));
            if (arrival != null) bounds.Encapsulate(ArrivalPosition);
            return bounds;
        }
    }
}
