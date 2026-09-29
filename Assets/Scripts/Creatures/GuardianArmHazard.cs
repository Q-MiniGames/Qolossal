using UnityEngine;

// One of the Knucklebramble's thorn-arms: its claws cost Qori a heart only while that arm is
// slamming down (KnucklebrambleGuardian.ArmStriking). Blows that land on the arm go to the
// guardian (it is the nearest damage receiver up the hierarchy).
[DisallowMultipleComponent]
public sealed class GuardianArmHazard : MonoBehaviour, IContactHazard
{
    [Range(1, 3)] public int arm = 1;
    KnucklebrambleGuardian guardian;

    void Awake() => guardian = GetComponentInParent<KnucklebrambleGuardian>();

    public bool HurtsOnContact => guardian != null && guardian.ArmStriking(arm);
}
