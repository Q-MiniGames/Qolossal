using UnityEngine;

// Qori's movement feel in one asset (Resources/Tuning/MovementTuning), read by PlayerMovement every
// physics step: edit it in the Inspector during Play mode and the change is felt at once, and kept
// after Play mode ends. The defaults are the values the game shipped with before the asset existed.
[CreateAssetMenu(menuName = "Qolossal/Tuning/Movement", fileName = "MovementTuning")]
public sealed class MovementTuning : ScriptableObject
{
    [Header("Running (units/s)")]
    [Min(0f)] public float moveSpeed = 7f;
    [Min(0f)] public float runSpeed = 10.5f;
    [Tooltip("Units/s per second on the ground.")] [Min(0f)] public float groundAcceleration = 65f;
    [Tooltip("Units/s per second in the air, with the stick held.")] [Min(0f)] public float airAcceleration = 35f;
    [Tooltip("Units/s per second in the air with the stick released. Thread-swing and flower-boost momentum is kept until landing.")]
    [Min(0f)] public float airDeceleration = 25f;

    [Header("Jumping")]
    [Min(0f)] public float jumpSpeed = 12f;
    [Min(0f)] public float coyoteTime = .12f;
    [Min(0f)] public float jumpBufferTime = .12f;
    [Tooltip("A second jump in mid-air (the flip), once until Qori lands, grabs a wall, a ledge or the thread. Held, it opens the Glidecap on the way down.")]
    public bool doubleJump = true;
    [Tooltip("The double jump's upward speed (the first jump's is jumpSpeed).")] [Min(0f)] public float doubleJumpSpeed = 11f;
    [Tooltip("Rising speed kept when Jump is released early.")] [Range(.1f, 1f)] public float jumpReleaseMultiplier = .5f;
    [Min(1f)] public float maximumFallSpeed = 22f;
    [Tooltip("Gravity multiplier while falling: a quicker, weightier fall than the rise.")] [Min(1f)] public float fallGravityMultiplier = 1.7f;
    [Tooltip("Gravity multiplier near the top of a jump while Jump is held: a brief hang at the apex.")] [Range(.1f, 1f)] public float apexGravityMultiplier = .55f;
    [Tooltip("Vertical speed (units/s) either side of zero that counts as the apex.")] [Min(0f)] public float apexSpeedBand = 1.5f;
    [Tooltip("Seconds after a jump or boost before the ground can catch Qori again.")] [Min(0f)] public float launchGraceSeconds = .16f;

    [Header("Walls (Climbing Moss)")]
    [Min(.1f)] public float wallSlideSpeed = 2.5f;
    [Min(.1f)] public float wallJumpHorizontalSpeed = 8f;
    [Min(.01f)] public float wallJumpPushSeconds = .12f;

    [Header("Wind Leaf dash")]
    [Min(1f)] public float dashSpeed = 19f;
    [Min(.02f)] public float dashSeconds = .16f;
    [Tooltip("Rest after a dash before the next can start.")] [Min(0f)] public float dashCooldown = .3f;

    [Header("Evade (the dodge: Shift / Y)")]
    [Tooltip("A quick dash along the ground that can't be hurt and passes through enemies (not the Shellback, whose stone shell stops it). Ground only.")]
    [Min(1f)] public float evadeSpeed = 13f;
    [Min(.05f)] public float evadeSeconds = .26f;
    [Tooltip("Rest after an evade before the next can start.")] [Min(0f)] public float evadeCooldown = .45f;
    [Tooltip("Seconds Qori stays untouchable after the evade ends (a little grace for the way out of an enemy).")] [Min(0f)] public float evadeGrace = .08f;

    [Header("Glidecap")]
    [Min(.1f)] public float glideFallSpeed = 1.8f;
    [Min(0f)] public float glideSpeed = 7.5f;
    [Tooltip("How quickly opening the Glidecap brakes a fast fall (units/s per second).")] [Min(1f)] public float glideBrake = 45f;
    [Tooltip("The drift's own gentle gravity (units/s per second).")] [Min(0f)] public float glidePull = 12f;
    [Tooltip("Closer to the ground than this, a Jump press is a buffered jump, not a glide.")] [Min(0f)] public float glideMinHeight = .5f;
    [Tooltip("Closer to a clingable wall than this, a Jump press is for the wall, not a glide.")] [Min(0f)] public float glideWallClearance = .6f;

    [Header("Ground")]
    [Min(.001f)] public float groundCheckDistance = .06f;
    [Tooltip("Downward look-ahead for landing presentation; doesn't extend grounded or coyote detection.")] [Min(0f)] public float landingLookAheadDistance = 1.5f;
    [Tooltip("Qori stays on the surface over kinks and downhill within this distance.")] [Min(0f)] public float groundSnapDistance = .3f;
    [Tooltip("Steepest walkable slope, as rise over run (1.2 is about 50 degrees).")] [Min(0f)] public float maxSlopeRise = 1.2f;

    static MovementTuning current;
    // The asset in Resources/Tuning, or the defaults if it's missing.
    public static MovementTuning Current
    {
        get
        {
            if (current != null) return current;
            current = Resources.Load<MovementTuning>("Tuning/MovementTuning");
            if (current == null) { current = CreateInstance<MovementTuning>(); current.hideFlags = HideFlags.DontSave; }
            return current;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => current = null;
}
