using UnityEngine;

// Qori's own sounds, from gameplay state (PlayerMovement adds this to the player):
//   footsteps (one per full stride, softer at a run) when a foot of the live rig (QoriAnimator) plants, one standard step on
//   every surface; jumps, landings (soft or hard by speed), ledge grabs and climbs, the
//   Wind Leaf dash, the Glidecap opening (no loop while it's open: the user removed it), the thread (flick, snag, swing, release),
//   and the return to the checkpoint. Hurt and death are PlayerHealth's.
// Footfalls: a foot "plants" when it comes down to its lowest point under the body after lifting
// clear; without the rig's feet, a step every stride of ground covered.
[DefaultExecutionOrder(40)]   // after QoriAnimator (30) has posed and grounded the feet
[DisallowMultipleComponent]
public sealed class QoriSounds : MonoBehaviour
{
    // StepSeconds: the recorded steps are 0.3-0.5 s shuffles of several scuffs; only the first footfall
    // of a take is heard, so steps stay single taps instead of running together into a hiss.
    // StepVolume sits the steps under the music (-3 dB).
    const float Lift = .06f, Plant = .02f, MinStride = .3f, FallbackStride = .65f, RunVolume = .7f, StepSeconds = .2f,
                StepVolume = .7f;

    PlayerMovement movement; PlayerThread thread; Rigidbody2D body;
    Transform[] feet; float[] lowest = new float[2]; bool[] lifted = new bool[2];
    int launch, landing, dash, respawn, attached, released; bool ready;
    bool wasHanging, wasClimbing, wasGliding, canopy; float swingVy, sinceStepX;
    Vector2 lastPos;

    /// <summary>Footfalls heard (tests compare it with the sounds started).</summary>
    public int Footfalls { get; private set; }

    void Start()
    {
        movement = GetComponent<PlayerMovement>(); thread = GetComponent<PlayerThread>(); body = GetComponent<Rigidbody2D>();
        var rig = GetComponentInChildren<QoriAnimator>();
        if (rig != null && rig.animator != null)
        {
            Transform near = null, far = null;
            foreach (var t in rig.animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "FootNear") near = t;
                else if (t.name == "FootFar") far = t;
            }
            if (near != null && far != null) feet = new[] { near, far };
        }
        lastPos = transform.position;
    }

    // One footstep everywhere, on every surface: the user chose Qori_Step_Water's Take 3 (its library
    // Favorite) as the game's standard step (8 Oct). The per-surface steps are kept, unused.
    public const string StepId = "Qori_Step_Water";

    void Step()
    {
        if (sinceStepX < MinStride) return;   // one footfall per plant, however the feet are read
        sinceStepX = 0f; Footfalls++;
        // One sound per full stride (every other footfall), and softer at a run (-3 dB at full speed).
        if (Footfalls % 2 == 1) return;
        float run = Mathf.InverseLerp(movement.WalkSpeed, movement.RunSpeed, Mathf.Abs(movement.ObservedVelocity.x));
        Sfx.PlayShort(StepId, StepSeconds, StepVolume * Mathf.Lerp(1f, RunVolume, run));
    }

    void LateUpdate()
    {
        if (movement == null || !movement.isActiveAndEnabled) return;
        if (!ready)
        {
            // The first frame sets the baseline: nothing that happened at load is heard.
            ready = true; Sync(); return;
        }
        // The canopy counts as open from the moment the Glidecap opens for as long as Jump stays held in
        // the air, even through the moments the glide itself lets go (an updraft, a swell's push), so
        // its opening sounds once per glide.
        canopy = (canopy || movement.IsGliding) && movement.JumpHeld && !movement.IsGrounded && !movement.IsLedgeHanging
                 && !movement.IsLedgeClimbing && !movement.IsWallSliding && !movement.IsDashing && (thread == null || !thread.IsAttached);

        if (respawn != movement.RespawnVersion)
        {
            // After a death the return waits for the death's own sound to fall away.
            if (Sfx.SinceLast("Qori_Death") < .3f) Sfx.PlayAfter("Qori_Respawn", .6f); else Sfx.Play("Qori_Respawn");
            Sync(); return;
        }

        if (launch != movement.LaunchVersion) { launch = movement.LaunchVersion; Sfx.Play("Qori_Jump"); }
        if (landing != movement.LandingVersion)
        {
            landing = movement.LandingVersion;
            float strength = Mathf.InverseLerp(2f, 22f, movement.LastLandingSpeed);
            if (strength > .05f) Sfx.Play(strength > .6f ? "Qori_Land_Hard" : "Qori_Land_Soft", .6f + .4f * strength);
            sinceStepX = 0f;   // the landing is the step
        }
        if (dash != movement.DashVersion) { dash = movement.DashVersion; Sfx.Play("Qori_Dash"); }
        if (movement.IsLedgeHanging && !wasHanging) Sfx.Play("Qori_LedgeGrab");
        if (movement.IsLedgeClimbing && !wasClimbing) Sfx.Play("Qori_Climb");
        if (canopy && !wasGliding) Sfx.Play("Qori_Glide_Open");   // once per opening
        wasHanging = movement.IsLedgeHanging; wasClimbing = movement.IsLedgeClimbing; wasGliding = canopy;

        if (thread != null)
        {
            if (attached != thread.AttachmentVersion) { attached = thread.AttachmentVersion; Sfx.Play("Qori_Thread_Shoot"); Sfx.PlayAfter("Qori_Thread_Attach", .08f); }
            if (released != thread.ReleaseVersion) { released = thread.ReleaseVersion; Sfx.Play("Qori_Thread_Release"); }
            if (thread.IsAttached)
            {
                // A swing: through the bottom of the arc, fast (the fall turning into a rise).
                float vy = movement.ObservedVelocity.y;
                if (swingVy < 0f && vy >= 0f && movement.ObservedVelocity.magnitude > 5f) Sfx.Play("Qori_Thread_Swing");
                swingVy = vy;
            }
            else swingVy = 0f;
        }

        // Footsteps.
        Vector2 pos = transform.position;
        bool walking = movement.IsGrounded && !movement.IsLedgeHanging && !movement.IsLedgeClimbing && !movement.IsDashing
                       && Mathf.Abs(movement.ObservedVelocity.x) > .3f;
        if (walking) sinceStepX += Mathf.Abs(pos.x - lastPos.x);
        lastPos = pos;
        if (!walking) { for (int i = 0; i < 2; i++) lifted[i] = false; return; }
        if (feet == null)
        {
            if (sinceStepX >= FallbackStride) Step();
            return;
        }
        float bodyY = movement.transform.position.y;
        for (int i = 0; i < 2; i++)
        {
            if (feet[i] == null) continue;
            float y = feet[i].position.y - bodyY;
            // The lowest the foot has been recently (relaxing upward slowly, for slopes and steps).
            lowest[i] = Mathf.Min(lowest[i] + Time.deltaTime * .5f, y);
            if (y > lowest[i] + Lift) lifted[i] = true;
            else if (lifted[i] && y <= lowest[i] + Plant) { lifted[i] = false; Step(); }
        }
    }

    void Sync()
    {
        launch = movement.LaunchVersion; landing = movement.LandingVersion; dash = movement.DashVersion; respawn = movement.RespawnVersion;
        attached = thread != null ? thread.AttachmentVersion : 0; released = thread != null ? thread.ReleaseVersion : 0;
        wasHanging = movement.IsLedgeHanging; wasClimbing = movement.IsLedgeClimbing; wasGliding = canopy = movement.IsGliding;
        lastPos = transform.position; sinceStepX = 0f;
        if (feet != null) for (int i = 0; i < 2; i++) { lowest[i] = feet[i] != null ? feet[i].position.y - movement.transform.position.y : 0f; lifted[i] = false; }
    }
}
