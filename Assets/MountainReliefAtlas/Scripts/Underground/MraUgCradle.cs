using UnityEngine;

// The Seed Cradle's counterweights: two thread weights on a balance. Catching the left weight with
// the Living Thread (from the dry floor) drops the left arm and lifts the right; catching the right
// one does the opposite. With the left arm down the cradle settles into its safe detent, saved for
// good (`detentFlag`); then a Bloomfall bounce on the latch pod beside it saves `solvedFlag`,
// which opens the reward grate. Nothing here needs any ability but the route's own two.
public sealed class MraUgCradle : MonoBehaviour
{
    public ThreadAnchor left, right;
    public MraPod latchPod;
    public string detentFlag, solvedFlag;
    public Transform beam;
    public bool LeftDown { get; private set; }
    public bool Detent => GameSave.HasFlag(detentFlag);
    PlayerThread thread; int podBounces;

    void Start() { LeftDown = Detent; podBounces = latchPod != null ? latchPod.Bounces : 0; Tilt(); }

    void Update()
    {
        if (thread == null) { var q = FindAnyObjectByType<PlayerThread>(); if (q == null) return; thread = q; }
        if (thread.IsAttached && !Detent)
        {
            if (left != null && (thread.AnchorPosition - (Vector2)left.transform.position).sqrMagnitude < .01f) Set(true);
            else if (right != null && (thread.AnchorPosition - (Vector2)right.transform.position).sqrMagnitude < .01f) Set(false);
        }
        if (latchPod != null && latchPod.Bounces != podBounces)
        {
            podBounces = latchPod.Bounces;
            if (Detent && GameSave.SetFlag(solvedFlag)) { TownHud.Toast("The latch springs free. The grate lifts."); Sfx.Play("Gate_Open", transform.position); }
            else if (!Detent) TownHud.Toast("The latch won't catch while the cradle swings.");
        }
    }

    // A weight caught (the thread calls it through Update; so can tests): true for the left one.
    public void Set(bool leftDown)
    {
        if (Detent || LeftDown == leftDown && leftDown) return;
        LeftDown = leftDown; Tilt();
        Sfx.Play("Wheel_Turn", transform.position);
        if (leftDown && GameSave.SetFlag(detentFlag)) TownHud.Toast("The cradle settles into its detent with a heavy clunk.");
    }

    void Tilt() { if (beam != null) beam.localRotation = Quaternion.Euler(0f, 0f, LeftDown ? 8f : -8f); }
}
