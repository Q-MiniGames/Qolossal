using UnityEngine;

// The Bark Sentinel's spear: costs Qori a heart only while it is thrust out (SentinelEnemy.SpearOut).
[DisallowMultipleComponent]
public sealed class SentinelSpear : MonoBehaviour, IContactHazard
{
    SentinelEnemy sentinel;
    void Awake() => sentinel = GetComponentInParent<SentinelEnemy>();
    public bool HurtsOnContact => sentinel != null && sentinel.SpearOut;
}
