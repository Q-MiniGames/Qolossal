using UnityEngine;
using UnityEngine.Rendering;

// Drives Qori's cutout rig (Assets/Art/Characters/QoriRig) from gameplay state.
// All poses live in AnimationClips; this component only picks the state,
// feeds speed/attack-time parameters, flips facing and swaps the head drawing.
// It lives on the "QoriRig" child of the Player (created by Qolossal > Qori Rig > Build and Install).
[DefaultExecutionOrder(30)]
[DisallowMultipleComponent]
public sealed class QoriAnimator : MonoBehaviour
{
    [Header("Rig references (filled by the builder)")]
    public Animator animator;
    public Transform facingPivot;
    public Transform handNear, handFar, weaponMount, weaponTip;
    [Tooltip("Grip on the camera-side hand; the blade is carried here.")] public Transform weaponMountFar;
    [Tooltip("Sorting order of the blade while carried in the camera-side hand (between that arm's upper arm and fist).")]
    public int weaponOrderFar = 15;
    public SpriteRenderer head, weapon;
    public Sprite headNeutral, headUp, headDown, headFocus;
    [Tooltip("Neutral head with closed eyes (same outline), flashed briefly for a blink.")] public Sprite headBlink;
    [Tooltip("Neutral head with expression overlays (same outline): squeezed shut after a hit, gritted for effort.")] public Sprite headHurt, headEffort;
    [Tooltip("How long the hurt face shows after taking damage.")] public float hurtFaceSeconds = .45f;
    [Tooltip("The free (camera-side) forearm: open hand normally, fist while hanging.")] public SpriteRenderer freeForearm;
    public Sprite freeFist, freeOpen;
    public SpriteRenderer[] renderers;
    [Tooltip("Normal arm segments, and the long reach arms shown instead while hanging from a ledge.")]
    public SpriteRenderer[] normalArms, reachArms;
    [Tooltip("LedgeClimb progress where the reach arms hand back to the normal arms (hands have let go).")]
    public float reachArmsUntil = .65f;

    [Header("Locomotion")]
    [Tooltip("Below this horizontal speed Qori idles.")] public float idleBelow = .25f;
    [Tooltip("Below this speed the walk cycle is used, above it the run cycle.")] public float runAbove = 2.4f;
    [Tooltip("Speed (world units/s) the walk clip was authored for.")] public float walkClipSpeed = 1.3f;
    [Tooltip("Speed (world units/s) the run clip was authored for.")] public float runClipSpeed = 7f;
    public Vector2 runPlaybackRange = new Vector2(.8f, 1.45f);
    [Tooltip("Landings faster than this play the squash.")] public float landSquashSpeed = 4f;

    [Header("Blending (seconds)")]
    public float locomotionBlend = .12f;
    public float airBlend = .1f;
    public float attackInBlend = .05f;
    public float attackOutBlend = .14f;

    [Header("Leaf cloak spring (adds lag on top of the clips)")]
    public Transform[] capeUpper;
    public Transform[] capeLower;
    [Tooltip("Swing frequency of the cloak panels (Hz).")] public float capeFrequency = 2.6f;
    [Range(0f, 1f)] public float capeDamping = .32f;
    [Tooltip("Degrees of lag per unit of horizontal acceleration.")] public float capeAccelGain = 110f;
    [Tooltip("How far the cloak swings forward when Qori turns around.")] public float capeTurnKick = 34f;
    public float capeLimit = 55f;

    [Header("Leaf ears spring")]
    [Tooltip("Ear bones (upper, lower). They hinge at the base under the leaf hair.")] public Transform[] ears;
    public float earFrequency = 3.1f;
    [Range(0f, 1f)] public float earDamping = .22f;
    [Tooltip("Degrees per unit of vertical speed: falling lifts the ears, rising lets them droop.")] public float earVerticalGain = 1.2f;
    [Tooltip("Degrees per unit of running speed (ears stream back).")] public float earSpeedGain = 1.1f;
    [Tooltip("Degrees of flop per unit of horizontal acceleration.")] public float earAccelGain = .25f;
    public float earLimit = 20f;
    public Vector2 blinkInterval = new Vector2(2.2f, 5.5f);

    [Header("Weapon animation sets")]
    [Tooltip("weaponId values that use the Mace_* clips (heavy smash).")] public string[] maceIds = { "forest-2" };
    [Tooltip("weaponId values that use the Spear_* clips (thrusts).")] public string[] spearIds = { "forest-3" };
    // Sling shots use Sling_*; everything else uses the sword clips.

    [Header("Rope")]
    public float ropeTiltLimit = 45f;
    public float ropeTiltSmoothing = .08f;

    [Header("Wind Leaf dash and Glidecap glide")]
    [Tooltip("Degrees Qori leans forward into a dash.")] public float dashLean = 16f;
    [Tooltip("Most the rig tilts with the drift while gliding.")] public float glideTiltLimit = 10f;
    [Tooltip("Seconds the canopy takes to spring open.")] public float glidecapOpenSeconds = .15f;
    [Tooltip("Direction of the free upper arm while gliding (degrees; 0 = forward, 90 = up). Codex's held proof: forward, grip at the chest.")] public float glideUpperArmAngle = 20f;
    [Tooltip("Direction of the free forearm while gliding (degrees).")] public float glideForearmAngle = 70f;
    [Tooltip("Sorting order of the free arm while gliding: in front of the torso (10), behind the sword arm (15).")] public int glideArmOrder = 11;

    static readonly int IdleState = Animator.StringToHash("Idle");
    static readonly int WalkState = Animator.StringToHash("Walk");
    static readonly int RunState = Animator.StringToHash("Run");
    static readonly int RiseState = Animator.StringToHash("Rise");
    static readonly int FallState = Animator.StringToHash("Fall");
    static readonly int LandState = Animator.StringToHash("Land");
    static readonly int HangState = Animator.StringToHash("Hang");
    static readonly int WallSlideState = Animator.StringToHash("WallSlide");
    static readonly int WallJumpUpState = Animator.StringToHash("WallJumpUp");
    static readonly int WallJumpOffState = Animator.StringToHash("WallJumpOff");
    static readonly int LedgeHangState = Animator.StringToHash("LedgeHang");
    static readonly int LedgeClimbState = Animator.StringToHash("LedgeClimb");
    static readonly int AttackFrontState = Animator.StringToHash("AttackFront");
    static readonly int AttackUpState = Animator.StringToHash("AttackUp");
    static readonly int AttackDownState = Animator.StringToHash("AttackDown");
    static readonly int AttackAirFrontState = Animator.StringToHash("AttackAirFront");
    static readonly int AttackAirUpState = Animator.StringToHash("AttackAirUp");
    // Combo swings 1-4: opener, rising backhand, return cut, lunging thrust.
    static readonly int[] ComboGround = { AttackFrontState, Animator.StringToHash("AttackFront2"), Animator.StringToHash("AttackFront3"), Animator.StringToHash("AttackFront4") };
    static readonly int[] ComboAir = { AttackAirFrontState, Animator.StringToHash("AttackAirFront2"), Animator.StringToHash("AttackAirFront3"), Animator.StringToHash("AttackAirFront4") };
    static readonly int MoveSpeedParam = Animator.StringToHash("MoveSpeed");
    static readonly int AttackTimeParam = Animator.StringToHash("AttackTime");
    const float LandSeconds = .26f;
    const float WallJumpUpSeconds = .42f, WallJumpOffSeconds = .45f;

    PlayerMovement movement; PlayerCombat combat; PlayerThread thread; PlayerHealth health;
    Color[] baseColors;
    int current, landingVersion, resetVersion;
    float facing = 1f, facingScale = 1f, landUntil, lastGroundedTime = -1f, ropeTilt, ropeTiltVelocity;
    bool wasAttacking;
    int cachedAttackId = -1, cachedAttackState; bool cachedGrounded;
    readonly System.Collections.Generic.Dictionary<string, int> stateHashes = new System.Collections.Generic.Dictionary<string, int>();
    float[] capeAngle, capeVelocity, capeLowerAngle, capeLowerVelocity;
    Quaternion[] capeBase, capeSet, capeLowerBase, capeLowerSet;
    Vector2 lastVelocity, smoothedAcceleration; bool hasLastVelocity;
    WeaponDefinition shownWeapon; bool weaponShown;
    float[] earAngle, earVelocity; Quaternion[] earBase, earSet;
    float nextBlink, blinkUntil;
    QoriFootGrounding feet;
    int launchVersion, playedLaunch = -1; float wallJumpUntil, lastWallDirection; bool wallJumpAway;

    public bool Ready => isActiveAndEnabled && animator != null && movement != null;
    public float VisualFacing => facing;
    /// <summary>The grip the blade is currently attached to (hit detection samples from it).</summary>
    public Transform WeaponMount => weapon != null && weapon.transform.parent != null ? weapon.transform.parent : weaponMount;
    public Transform WeaponTip => weaponTip;
    public Transform HandNear => handNear;
    public Transform HandFar => handFar;
    /// <summary>Where the living thread leaves Qori: the near hand (raised while hanging).</summary>
    public Vector3 RopeOrigin
    {
        get
        {
            return handNear != null ? handNear.position : transform.position;
        }
    }

    void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
        if (movement == null) { Debug.LogWarning("QoriAnimator must be a child of the Player.", this); enabled = false; return; }
        combat = movement.GetComponent<PlayerCombat>();
        thread = movement.GetComponent<PlayerThread>();
        health = movement.GetComponent<PlayerHealth>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // hit detection reads the blade even off-screen
        if (animator == null || animator.runtimeAnimatorController == null)
            Debug.LogError("QoriRig: the Animator has no controller, so Qori will stay in his default pose. Run Qolossal > Qori Rig > Build and Install on Player.", this);
        if (renderers == null) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        CancelParentScale();
        if (animator != null) feet = new QoriFootGrounding(animator.transform, movement);
        landingVersion = movement.LandingVersion;
        resetVersion = movement.ResetVersion;
        launchVersion = movement.LaunchVersion;
        facing = facingScale = movement.FacingDirection < 0 ? -1f : 1f;
        current = 0;
    }

    // The Player root is scaled non-uniformly (collider shape). Undo it here so
    // bone rotations never shear the artwork.
    void CancelParentScale()
    {
        Transform parent = transform.parent;
        if (parent == null) return;
        Vector3 s = parent.lossyScale;
        transform.localScale = new Vector3(1f / Mathf.Max(1e-4f, Mathf.Abs(s.x)), 1f / Mathf.Max(1e-4f, Mathf.Abs(s.y)), 1f);
    }

    void OnEnable() => current = 0;

    void Update()
    {
        if (!Ready || Time.deltaTime <= 0f) return;
        if (resetVersion != movement.ResetVersion)
        {
            resetVersion = movement.ResetVersion; landingVersion = movement.LandingVersion; landUntil = 0f; current = 0;
            launchVersion = movement.LaunchVersion; wallJumpUntil = 0f; lastWallDirection = 0f;
            hasLastVelocity = false; smoothedAcceleration = Vector2.zero; capeAngle = null; earAngle = null;
            facing = facingScale = movement.FacingDirection < 0 ? -1f : 1f; ropeTilt = 0f; ropeTiltVelocity = 0f;
        }

        Vector2 velocity = movement.ObservedVelocity;
        bool attacking = combat != null && combat.IsAttackPoseActive && combat.CurrentAttack != null;
        bool attached = thread != null && thread.IsAttached;
        bool hanging = attached || movement.IsLedgeHanging || movement.IsLedgeClimbing;
        if (movement.IsGrounded) lastGroundedTime = Time.time;
        bool grounded = movement.IsGrounded || (Time.time - lastGroundedTime < .08f && velocity.y <= .1f);

        UpdateFacing(attacking);
        // Wall jumps: PlayerMovement faces Qori at the wall for a jump up it (climbing),
        // or away from it when the player pushed off.
        if (movement.WallDirection != 0f) lastWallDirection = movement.WallDirection;
        if (movement.LaunchVersion != launchVersion)
        {
            launchVersion = movement.LaunchVersion;
            if (movement.LastLaunchKind == PlayerMovement.LaunchKind.WallJump)
            {
                wallJumpAway = lastWallDirection != 0f && movement.FacingDirection * lastWallDirection < 0f;
                wallJumpUntil = Time.time + (wallJumpAway ? WallJumpOffSeconds : WallJumpUpSeconds);
            }
            else wallJumpUntil = 0f;
        }
        bool wallJumping = Time.time < wallJumpUntil && !movement.IsGrounded && !attached && !movement.IsWallSliding;
        bool ledge = movement.IsLedgeHanging || movement.IsLedgeClimbing;
        // The Glidecap: the idle pose, with the free near arm raised to its grip (RaiseGlideArm).
        bool gliding = movement.IsGliding && !attacking && !hanging;

        int state; float blend;
        if (attacking)
        {
            state = AttackState(grounded);
            animator.SetFloat(AttackTimeParam, AttackTime());
            blend = attackInBlend;
        }
        else if (movement.IsLedgeClimbing)
        {
            state = LedgeClimbState; blend = .05f;
            animator.SetFloat(AttackTimeParam, Mathf.Clamp(movement.LedgeClimbProgress, 0f, .999f));
        }
        else if (movement.IsLedgeHanging) { state = LedgeHangState; blend = .08f; }
        else if (hanging) { state = HangState; blend = airBlend; }
        else if (gliding) { state = IdleState; blend = airBlend; }   // at rest under the canopy, sword in hand
        else if (wallJumping) { state = wallJumpAway ? WallJumpOffState : WallJumpUpState; blend = .04f; }
        else if (movement.IsWallSliding) { state = WallSlideState; blend = .08f; }
        else if (!grounded) { state = velocity.y > .6f ? RiseState : FallState; blend = airBlend; }
        else
        {
            float speed = Mathf.Abs(velocity.x);
            if (movement.LandingVersion != landingVersion)
            {
                landingVersion = movement.LandingVersion;
                if (movement.LastLandingSpeed >= landSquashSpeed && speed < runAbove) landUntil = Time.time + LandSeconds;
            }
            if (Time.time < landUntil && speed < runAbove) { state = LandState; blend = .03f; }
            else if (speed < idleBelow) { state = IdleState; blend = locomotionBlend; }
            else if (speed < runAbove)
            {
                state = WalkState; blend = locomotionBlend;
                animator.SetFloat(MoveSpeedParam, Mathf.Clamp(speed / walkClipSpeed, .6f, 1.9f));
            }
            else
            {
                state = RunState; blend = locomotionBlend;
                animator.SetFloat(MoveSpeedParam, Mathf.Clamp(speed / runClipSpeed, runPlaybackRange.x, runPlaybackRange.y));
            }
        }
        if (wasAttacking && !attacking) blend = attackOutBlend;
        wasAttacking = attacking;
        bool restart = wallJumping && movement.LaunchVersion != playedLaunch && (state == WallJumpUpState || state == WallJumpOffState);
        if (state != current || restart)
        {
            animator.CrossFadeInFixedTime(state, blend, 0, 0f);
            current = state;
            if (restart || wallJumping) playedLaunch = movement.LaunchVersion;
        }

        UpdateRopeTilt(attached, gliding, velocity);
        // Look up / grip with the fists while hanging, pulling up (early part) or jumping up a wall.
        bool gripping = attached || movement.IsLedgeHanging || (movement.IsLedgeClimbing && movement.LedgeClimbProgress < .45f)
                        || (wallJumping && !wallJumpAway);
        // Strain: a heavy smash winding up or striking, pulling up a ledge, or jumping up a wall.
        bool effort = (attacking && WeaponFamily(combat.CurrentAttack) == "Mace" && combat.Phase != AttackPhase.Recovery)
                      || (movement.IsLedgeClimbing && movement.LedgeClimbProgress < .65f) || (wallJumping && !wallJumpAway);
        UpdateHead(attacking, gripping, grounded, velocity, effort);
        UpdateWeapon(hanging || ledge || movement.IsWallSliding || (wallJumping && !wallJumpAway));
        glidingNow = gliding;
        UpdateWeaponHand(true);   // every weapon clip is authored for the camera-side hand
        UpdateArms(movement.IsLedgeHanging || (movement.IsLedgeClimbing && movement.LedgeClimbProgress < reachArmsUntil));
        UpdateTint();
    }

    void UpdateFacing(bool attacking)
    {
        float wanted = attacking ? (combat.AttackDirection < 0 ? -1f : 1f) : (movement.FacingDirection < 0 ? -1f : 1f);
        if (wanted != facing && capeAngle != null)
            for (int i = 0; i < capeAngle.Length; i++) { capeAngle[i] += capeTurnKick; capeLowerAngle[i] += capeTurnKick * .6f; }
        facing = facingScale = wanted;
        // Instant flip: an animated "paper turn" read as a glitchy sliver in play.
        if (facingPivot != null) facingPivot.localScale = new Vector3(facingScale, 1f, 1f);
    }

    // Runs after the Animator has written this frame's pose, and adds a damped
    // spring on each cloak panel so it lags behind starts, stops, turns and the rope.
    void LateUpdate()
    {
        if (!Ready || Time.deltaTime <= 0f) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Vector2 velocity = movement.ObservedVelocity;
        // Velocity comes from the physics step, so smooth the derived acceleration
        // to avoid spikes on frames where FixedUpdate ran twice or not at all.
        Vector2 raw = hasLastVelocity ? (velocity - lastVelocity) / dt : Vector2.zero;
        lastVelocity = velocity; hasLastVelocity = true;
        smoothedAcceleration = Vector2.Lerp(smoothedAcceleration, raw, 1f - Mathf.Exp(-dt / .06f));
        float forwardAcceleration = Mathf.Clamp(smoothedAcceleration.x * facing, -90f, 90f);
        feet?.Apply(movement.IsGrounded && !movement.IsLedgeHanging && !movement.IsLedgeClimbing && !movement.IsWallSliding, dt);
        UpdateEars(dt, velocity, forwardAcceleration);
        UpdateCape(dt, forwardAcceleration);
        RaiseGlideArm();
        UpdateGlidecap();
    }

    bool glidingNow; float glideSince = -1f; SpriteRenderer glidecap;

    // Over the Animator's pose: the free near arm (the blade is in the far hand) reaches forward and
    // up, holding the Glidecap's grip in front of the chest, and is drawn in front of the torso
    // while it does. Angles are the arm segments' directions in the torso's frame (0 = forward,
    // 90 = up); the rig's bones rest at zero.
    SpriteRenderer[] glideArm; int[] glideArmRestOrder;
    void RaiseGlideArm()
    {
        Transform forearm = handNear != null ? handNear.parent : null, upper = forearm != null ? forearm.parent : null;
        if (upper == null) return;
        if (glideArm == null)
        {
            glideArm = new[] { upper.GetComponent<SpriteRenderer>(), forearm.GetComponent<SpriteRenderer>() };
            glideArmRestOrder = new int[glideArm.Length];
            for (int i = 0; i < glideArm.Length; i++) glideArmRestOrder[i] = glideArm[i] != null ? glideArm[i].sortingOrder : 0;
        }
        for (int i = 0; i < glideArm.Length; i++)
            if (glideArm[i] != null) glideArm[i].sortingOrder = glidingNow ? glideArmOrder + i : glideArmRestOrder[i];
        if (!glidingNow) return;
        if (glideSince < 0f) glideSince = Time.time;
        float raise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - glideSince) / Mathf.Max(.01f, glidecapOpenSeconds)));
        Vector2 upperRest = forearm.localPosition, forearmRest = handNear.localPosition;
        float upperAngle = glideUpperArmAngle - Mathf.Atan2(upperRest.y, upperRest.x) * Mathf.Rad2Deg;
        float forearmAngle = glideForearmAngle - glideUpperArmAngle - Mathf.Atan2(forearmRest.y, forearmRest.x) * Mathf.Rad2Deg + Mathf.Atan2(upperRest.y, upperRest.x) * Mathf.Rad2Deg;
        upper.localRotation = Quaternion.Slerp(upper.localRotation, Quaternion.Euler(0f, 0f, upperAngle), raise);
        forearm.localRotation = Quaternion.Slerp(forearm.localRotation, Quaternion.Euler(0f, 0f, forearmAngle), raise);
    }

    // The opened Glidecap over Qori, its grip (the sprite's pivot) in his raised near hand. It
    // springs open over the first moments and tilts with him; it sorts just behind his body so
    // the hand closes over the grip. Lives beside the Facing pivot, where the parent scale is undone.
    void UpdateGlidecap()
    {
        Sprite art = Fx.Library != null ? Fx.Library.glidecapHeld : null;
        if (!glidingNow || art == null || handNear == null)
        {
            if (glidecap != null && glidecap.enabled) glidecap.enabled = false;
            glideSince = -1f;
            return;
        }
        if (glidecap == null)
        {
            glidecap = new GameObject("Glidecap").AddComponent<SpriteRenderer>();
            glidecap.transform.SetParent(transform, false);
            glidecap.sprite = art;
            var group = animator != null ? animator.GetComponent<SortingGroup>() : null;
            if (group != null) { glidecap.sortingLayerID = group.sortingLayerID; glidecap.sortingOrder = group.sortingOrder - 1; }
        }
        if (glideSince < 0f) glideSince = Time.time;
        float open = Mathf.SmoothStep(.35f, 1f, Mathf.Clamp01((Time.time - glideSince) / Mathf.Max(.01f, glidecapOpenSeconds)));
        glidecap.enabled = true;
        glidecap.color = health != null && health.IsDamageFlashVisible ? new Color(1f, 1f, 1f, .3f) : Color.white;
        glidecap.transform.SetPositionAndRotation(handNear.position, Quaternion.Euler(0f, 0f, ropeTilt));
        glidecap.transform.localScale = new Vector3(open, Mathf.Lerp(.8f, 1f, open), 1f);
    }

    // Leaf ears: a damped spring per ear on top of the animated pose (non-accumulating, like the cloak).
    void UpdateEars(float dt, Vector2 velocity, float forwardAcceleration)
    {
        if (ears == null || ears.Length == 0) return;
        int n = ears.Length;
        if (earAngle == null || earAngle.Length != n)
        {
            earAngle = new float[n]; earVelocity = new float[n]; earBase = new Quaternion[n]; earSet = new Quaternion[n];
            for (int i = 0; i < n; i++) if (ears[i] != null) earBase[i] = earSet[i] = ears[i].localRotation;
        }
        float target = Mathf.Clamp(velocity.y * earVerticalGain, -earLimit, earLimit)
                     + Mathf.Min(Mathf.Abs(velocity.x), 9f) * earSpeedGain;
        for (int i = 0; i < n; i++)
        {
            if (ears[i] == null) continue;
            float sway = Mathf.Sin(Time.time * (1.3f + .4f * i) + i * 1.7f) * 1.6f;
            float w = earFrequency * (1f + .12f * i) * 2f * Mathf.PI;
            float a = -w * w * (earAngle[i] - target - sway) - 2f * earDamping * w * earVelocity[i] + w * w * forwardAcceleration * earAccelGain;
            earVelocity[i] += a * dt;
            earAngle[i] = Mathf.Clamp(earAngle[i] + earVelocity[i] * dt, -earLimit, earLimit);
            earBase[i] = ears[i].localRotation == earSet[i] ? earBase[i] : ears[i].localRotation;
            ears[i].localRotation = earSet[i] = earBase[i] * Quaternion.Euler(0f, 0f, earAngle[i]);
        }
    }

    void UpdateCape(float dt, float forwardAcceleration)
    {
        if (capeUpper == null || capeUpper.Length == 0) return;
        int n = capeUpper.Length;
        if (capeAngle == null || capeAngle.Length != n)
        {
            capeAngle = new float[n]; capeVelocity = new float[n]; capeLowerAngle = new float[n]; capeLowerVelocity = new float[n];
            capeBase = new Quaternion[n]; capeSet = new Quaternion[n]; capeLowerBase = new Quaternion[n]; capeLowerSet = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                if (capeUpper[i] != null) capeBase[i] = capeSet[i] = capeUpper[i].localRotation;
                if (capeLower != null && i < capeLower.Length && capeLower[i] != null) capeLowerBase[i] = capeLowerSet[i] = capeLower[i].localRotation;
            }
        }
        float hangTarget = -ropeTilt * facing * .7f; // keep hanging toward the ground while the body tilts on the rope
        for (int i = 0; i < n; i++)
        {
            if (capeUpper[i] == null) continue;
            float w = capeFrequency * (1f + .17f * i) * 2f * Mathf.PI;
            float a = -w * w * (capeAngle[i] - hangTarget) - 2f * capeDamping * w * capeVelocity[i] - forwardAcceleration * capeAccelGain;
            capeVelocity[i] += a * dt;
            capeAngle[i] = Mathf.Clamp(capeAngle[i] + capeVelocity[i] * dt, -capeLimit, capeLimit);
            float w2 = w * .8f;
            float a2 = -w2 * w2 * (capeLowerAngle[i] - capeAngle[i] * .6f) - 2f * capeDamping * w2 * capeLowerVelocity[i];
            capeLowerVelocity[i] += a2 * dt;
            capeLowerAngle[i] = Mathf.Clamp(capeLowerAngle[i] + capeLowerVelocity[i] * dt, -capeLimit, capeLimit);
            // Add the spring on top of this frame's animated pose. If nothing animated
            // the bone this frame, reuse last frame's base so the offset never accumulates.
            capeBase[i] = capeUpper[i].localRotation == capeSet[i] ? capeBase[i] : capeUpper[i].localRotation;
            capeUpper[i].localRotation = capeSet[i] = capeBase[i] * Quaternion.Euler(0f, 0f, capeAngle[i]);
            if (capeLower != null && i < capeLower.Length && capeLower[i] != null)
            {
                Transform low = capeLower[i];
                capeLowerBase[i] = low.localRotation == capeLowerSet[i] ? capeLowerBase[i] : low.localRotation;
                low.localRotation = capeLowerSet[i] = capeLowerBase[i] * Quaternion.Euler(0f, 0f, capeLowerAngle[i]);
            }
        }
    }

    // Picks the clip for the current attack: weapon family, direction, combo step and air/ground.
    // Falls back to the sword clips when a weapon-specific clip does not exist.
    int AttackState(bool grounded)
    {
        if (combat.ExecutionId == cachedAttackId && grounded == cachedGrounded) return cachedAttackState;
        cachedAttackId = combat.ExecutionId; cachedGrounded = grounded;
        AttackDefinition attack = combat.CurrentAttack;
        AttackAim aim = attack.direction;
        int step = ComboStep(attack);
        string family = WeaponFamily(attack);
        if (family != null)
        {
            string name = family == "Sling" ? (grounded ? "Sling_Throw" : "Sling_AirThrow")
                : aim == AttackAim.Down ? family + "_Down"
                : aim == AttackAim.Up ? family + (grounded ? "_Up" : "_AirUp")
                : family + (grounded ? "_Front" : "_AirFront") + (step + 1);
            if (!stateHashes.TryGetValue(name, out int hash)) { hash = Animator.StringToHash(name); stateHashes[name] = hash; }
            if (animator.HasState(0, hash)) return cachedAttackState = hash;
        }
        if (aim == AttackAim.Down) return cachedAttackState = AttackDownState;
        if (aim == AttackAim.Up) return cachedAttackState = grounded ? AttackUpState : AttackAirUpState;
        return cachedAttackState = grounded ? ComboGround[step] : ComboAir[step];
    }

    string WeaponFamily(AttackDefinition attack)
    {
        if (attack.slingProjectile) return "Sling";
        WeaponDefinition weapon = combat.EquippedWeapon;
        if (weapon == null) return null;
        if (System.Array.IndexOf(maceIds, weapon.weaponId) >= 0) return "Mace";
        if (System.Array.IndexOf(spearIds, weapon.weaponId) >= 0) return "Spear";
        return null;
    }

    // Which swing of the front combo this attack is (0-3), found by walking the
    // weapon's follow-up chain. Anything outside the chain uses the opener.
    int ComboStep(AttackDefinition attack)
    {
        WeaponDefinition weapon = combat.EquippedWeapon;
        if (attack == null || attack.slingProjectile || weapon == null || weapon.moveSet == null) return 0;
        AttackDefinition move = weapon.moveSet.Find(CombatMoveSlot.Front);
        for (int step = 0; move != null && step < ComboGround.Length; step++)
        {
            if (move == attack) return step;
            move = move.followUps != null && move.followUps.Length > 0 ? move.followUps[0] : null;
        }
        return 0;
    }

    float AttackTime()
    {
        // Clip layout: startup 0-0.30, active 0.30-0.55, recovery 0.55-1.00.
        float p = Mathf.Clamp01(combat.PhaseProgress);
        switch (combat.Phase)
        {
            case AttackPhase.Startup: return .30f * p;
            case AttackPhase.Active: return .30f + .25f * p;
            case AttackPhase.Recovery: return .55f + .449f * p;
            default: return 0f;
        }
    }

    // Leans the whole rig: into the rope while swinging, a little with the drift under the Glidecap,
    // and forward into a Wind Leaf dash.
    void UpdateRopeTilt(bool attached, bool gliding, Vector2 velocity)
    {
        float target = 0f;
        if (attached)
        {
            Vector2 toAnchor = thread.AnchorPosition - (Vector2)movement.transform.position;
            // Only lean into the rope when the anchor is above Qori (a real swing).
            // While the thread yanks sideways or downward, stay upright instead of lying flat.
            if (toAnchor.sqrMagnitude > .0001f && toAnchor.y > .35f * toAnchor.magnitude)
                target = Mathf.Clamp(Vector2.SignedAngle(Vector2.up, toAnchor), -ropeTiltLimit, ropeTiltLimit);
        }
        else if (gliding) target = Mathf.Clamp(-velocity.x * 1.4f, -glideTiltLimit, glideTiltLimit);   // grip ahead, feet trailing
        else if (movement.IsDashing) target = -movement.DashDirection * dashLean;
        ropeTilt = Mathf.SmoothDampAngle(ropeTilt, target, ref ropeTiltVelocity, ropeTiltSmoothing, Mathf.Infinity, Time.deltaTime);
        if (facingPivot != null) facingPivot.localRotation = Quaternion.Euler(0f, 0f, ropeTilt);
    }

    void UpdateHead(bool attacking, bool hanging, bool grounded, Vector2 velocity, bool effort)
    {
        if (head == null) return;
        Sprite s = headNeutral;
        // Expressions are drawn on the neutral head, so they take priority over the look-direction heads.
        if (headHurt != null && health != null && Time.time - health.LastHitTime < hurtFaceSeconds) s = headHurt;
        else if (headEffort != null && effort) s = headEffort;
        else if (attacking)
        {
            AttackAim aim = combat.CurrentAttack.direction;
            s = aim == AttackAim.Up ? headUp : aim == AttackAim.Down ? headDown : headFocus;
        }
        else if (hanging) s = headUp;
        else if (!grounded) s = velocity.y < -4f ? headDown : headNeutral;
        else if (Mathf.Abs(velocity.x) >= runAbove) s = headFocus;
        if (s == headNeutral && headBlink != null)
        {
            if (Time.time >= nextBlink)
            {
                blinkUntil = Time.time + .11f;
                // now and then a quick double blink
                nextBlink = Time.time + (Random.value < .15f ? .22f : Random.Range(blinkInterval.x, blinkInterval.y));
            }
            if (Time.time < blinkUntil) s = headBlink;
        }
        if (s != null && head.sprite != s) head.sprite = s;
        if (freeForearm != null && freeFist != null && freeOpen != null)
        {
            Sprite hand = hanging ? freeFist : freeOpen;   // grips the rope / ledge, relaxed otherwise
            if (freeForearm.sprite != hand) freeForearm.sprite = hand;
        }
    }

    // All clips carry the blade in the camera-side hand (hanging and wall grips stow it).
    int weaponOrderNear = int.MinValue;
    void UpdateWeaponHand(bool far)
    {
        if (weapon == null || weaponMountFar == null) return;
        if (weaponOrderNear == int.MinValue) weaponOrderNear = weapon.sortingOrder;
        Transform mount = far ? weaponMountFar : weaponMount;
        if (weapon.transform.parent == mount) return;
        weapon.transform.SetParent(mount, false);
        weapon.sortingOrder = far ? weaponOrderFar : weaponOrderNear;
    }

    // The ledge clips solve both arm sets to the same hands; only one set is drawn.
    void UpdateArms(bool reach)
    {
        if (reachArms == null || reachArms.Length == 0) return;
        foreach (SpriteRenderer arm in normalArms) if (arm != null && arm.enabled == reach) arm.enabled = !reach;
        foreach (SpriteRenderer arm in reachArms) if (arm != null && arm.enabled != reach) arm.enabled = reach;
    }

    void UpdateWeapon(bool hanging)
    {
        if (weapon == null) return;
        WeaponDefinition w = combat != null ? combat.PresentationWeapon : null;
        // The near hand holds the thread while hanging, so the blade is stowed.
        weapon.enabled = !hanging && w != null && w.weaponArtwork != null && w.weaponId != "resin-sling";
        if (w == shownWeapon && weaponShown) return;
        shownWeapon = w; weaponShown = true;
        weapon.sprite = w != null ? w.weaponArtwork : null;
        float scale = w != null ? w.artworkScale : 1f;
        weapon.transform.localScale = new Vector3(scale, scale, 1f);
        if (weaponTip != null) weaponTip.localPosition = w != null ? (Vector3)w.artworkTip : new Vector3(7.3f, 0f, 0f);
    }

    void UpdateTint()
    {
        bool flash = health != null && health.IsDamageFlashVisible;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = baseColors[i];
            if (flash) c.a *= .3f;
            renderers[i].color = c;
        }
    }
}
