using UnityEngine;

// An underground creature's strike: a trigger that costs Qori a heart while the creature's strike is live.
[DisallowMultipleComponent]
public sealed class UndergroundEnemyStrike : MonoBehaviour, IContactHazard
{
    UndergroundEnemy owner;
    public void Set(UndergroundEnemy e) => owner = e;
    public bool HurtsOnContact => owner != null && owner.StrikeLive;
}
