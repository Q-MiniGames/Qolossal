using UnityEngine;

[DisallowMultipleComponent]
public sealed class SurfaceArea : MonoBehaviour
{
    public string areaId, areaName, regionName;
    public Bounds localBounds;
    public Transform sharedTerrain;
    public GameObject[] relatedEncounters;
    public Bounds WorldBounds => new Bounds(transform.TransformPoint(localBounds.center), Vector3.Scale(localBounds.size, transform.lossyScale));
}
