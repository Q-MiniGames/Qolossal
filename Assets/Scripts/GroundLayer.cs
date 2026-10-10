using UnityEngine;

// The "Ground" layer's mask, looked up once: LayerMask.GetMask allocates on every call, and several
// creatures and effects query the ground every frame.
public static class GroundLayer
{
    static int mask = -1;
    public static int Mask => mask >= 0 ? mask : mask = LayerMask.GetMask("Ground");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => mask = -1;
}
