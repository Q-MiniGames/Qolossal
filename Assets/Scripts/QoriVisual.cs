using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-20)]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class QoriVisual : MonoBehaviour
{
    [Header("Rope Holding Pose")]
    [SerializeField] private Sprite ropeHoldSprite;
    [SerializeField] private Sprite ropeRiseSprite;
    [SerializeField] private Sprite ropeFallSprite;
    [SerializeField, Min(0.01f)] private float ropePoseBlendSeconds = 0.12f;
    [SerializeField, Min(0f)] private float ropePoseConfirmSeconds = 0.09f;
    [Header("Directional Rope Poses")]
    [SerializeField] private Sprite ropeRiseForwardSprite;
    [SerializeField] private Sprite ropeRiseBackwardSprite;
    [SerializeField] private Sprite ropeFallForwardSprite;
    [SerializeField] private Sprite ropeFallBackwardSprite;
    [SerializeField, Range(0f, 45f)] private float maximumRopeLean = 25f;
    [SerializeField, Min(0.01f)] private float ropeLeanSmoothSeconds = 0.18f;
    private bool ropeFacingLeft, movingForward = true;
    private int ropeVerticalPhase;
    private float ropeLean, ropeLeanVelocity;
    private Quaternion restingRotation;
    private Sprite activeRopePose, pendingRopePose;
    private float pendingPoseSince, ropeBlendStarted;
    private SpriteRenderer outgoingRopePose;
    private bool showingRopePose;
    [Header("Continuous Rope Motion Study")]
    [SerializeField] private bool useContinuousRopeMotion = true;
    private QoriRopeMotion ropeMotion;
    private bool visualWasEnabled;
    [Header("Walking Follow Through")]
    [SerializeField] private bool useWalkingFollowThrough = true;
    [Header("Jump And Fall Follow Through")]
    [SerializeField] private bool useAirFollowThrough = true;
    private QoriWalkMotion walkingMotion;
    [Header("Layered Cloak")]
    [SerializeField] private bool useLayeredAirCloak = true;
    [SerializeField] private bool useLayeredWalkingCloak = true;
    [SerializeField] private bool useLayeredRopeCloak = true;
    [SerializeField] private bool useLayeredIdleCloak = true;
    [SerializeField] private bool useLayeredAttackCloak = true;
    private QoriLayeredCloak layeredCloak;
    [Header("Ground Rig")]
    [SerializeField] private bool useAnatomicalRig = true;
    [SerializeField] private QoriBodyRig.GroundTuning locomotion = new QoriBodyRig.GroundTuning();
    [SerializeField] private QoriBodyRig.AirTuning airMotion = new QoriBodyRig.AirTuning();
    private QoriBodyRig bodyRig;
    [SerializeField] private QoriSecondaryMotion.Tuning secondaryTuning = new QoriSecondaryMotion.Tuning();
    private readonly QoriSecondaryMotion secondary = new QoriSecondaryMotion();
    [Header("Fluid Rope Swing")]
    [SerializeField] private QoriSwingAnimation.Tuning swingTuning=new QoriSwingAnimation.Tuning();
    private QoriSwingAnimation swingAnimation;
    [SerializeField] private QoriMovementFeedback.Tuning feedbackTuning = new QoriMovementFeedback.Tuning();
    private QoriMovementFeedback feedback;

    [SerializeField] private Vector2 ropeHoldOffset = new Vector2(-0.07f, -0.06f);
    [SerializeField] private Vector2 ropeHandPoint = new Vector2(2.74f, 5.2f);
    private PlayerThread thread;
    public Vector3 RopeOrigin => bodyRig != null && bodyRig.BodyActive
        ? showingRopePose ? bodyRig.ThreadGrip : bodyRig.GetJoint("RightHand").position
        : visual != null && showingRopePose
        ? transform.TransformPoint(new Vector3(ropeHandPoint.x * (visual.flipX ? -1f : 1f), ropeHandPoint.y, 0f))
        : movement != null ? movement.transform.position : transform.position;

    [Header("Attack Pose")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField, Min(0.1f)] private float attackPoseScale = 1.14f;
    [SerializeField] private Vector2 attackPoseOffset = new Vector2(0.39f, -0.057f);
    private PlayerCombat combat;
    [Header("Airborne Poses")]
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite fallSprite;
    [Header("Walking Animation")]
    [SerializeField] private Sprite[] walkFrames = new Sprite[6];
    [SerializeField, Min(0.1f)] private float framesPerSecondAtFullSpeed = 10f;
    [SerializeField, Min(0.1f)] private float referenceMoveSpeed = 7f;
    private Sprite idleSprite;
    private float walkFrameClock;
    private bool wasWalking;
    [Header("Idle Breathing")]
    [SerializeField] private bool enableIdleBreathing = true;
    [SerializeField, Range(0f, 0.04f)] private float breathingAmount = 0.012f;
    [SerializeField, Min(0.2f)] private float breathingCycleSeconds = 2.5f;
    private PlayerMovement movement;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private int groundMask;
    private PlayerHealth health;
    private SpriteRenderer visual;
    private Color restingColor;
    private Vector3 restingPosition;
    private Vector3 restingScale;
    private float idleBlend;
    private bool originalFlip;

    private void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            body = movement.GetComponent<Rigidbody2D>();
            playerCollider = movement.GetComponent<Collider2D>();
        }
        groundMask = LayerMask.GetMask("Ground");
        health = GetComponentInParent<PlayerHealth>();
        combat = GetComponentInParent<PlayerCombat>();
        thread = GetComponentInParent<PlayerThread>();
        visual = GetComponent<SpriteRenderer>();
        // Keep scenery from sorting between the cloak panels and Qori's body.
        SortingGroup sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null) sortingGroup = gameObject.AddComponent<SortingGroup>();
        sortingGroup.sortingLayerID = visual.sortingLayerID;
        sortingGroup.sortingOrder = visual.sortingOrder;
        sortingGroup.enabled = true;
        visualWasEnabled = visual.enabled;
        layeredCloak = gameObject.AddComponent<QoriLayeredCloak>();
        layeredCloak.Initialize(visual);
        bodyRig = gameObject.AddComponent<QoriBodyRig>();
        swingAnimation=gameObject.AddComponent<QoriSwingAnimation>();
        swingAnimation.tuning=swingTuning;bodyRig.Swing=swingAnimation;
        bodyRig.ground = locomotion;
        bodyRig.air = airMotion;
        secondary.tuning = secondaryTuning;
        bodyRig.Secondary = secondary;
        bodyRig.Initialize(visual, movement);
        feedback=gameObject.AddComponent<QoriMovementFeedback>();feedback.tuning=feedbackTuning;feedback.Initialize(movement,bodyRig);
        ropeMotion = gameObject.AddComponent<QoriRopeMotion>();
        ropeMotion.Initialize(ropeHoldSprite, visual);
        idleSprite = visual.sprite;
        walkingMotion = gameObject.AddComponent<QoriWalkMotion>();
        walkingMotion.Initialize(walkFrames, idleSprite, visual, jumpSprite, fallSprite);
        restingColor = visual.color;
        restingPosition = transform.localPosition;
        restingScale = transform.localScale;
        restingRotation = transform.localRotation;
        originalFlip = visual.flipX;
        GameObject blendObject = new GameObject("Rope Pose Transition");
        blendObject.transform.SetParent(transform, false);
        outgoingRopePose = blendObject.AddComponent<SpriteRenderer>();
        outgoingRopePose.sharedMaterial = visual.sharedMaterial;
        outgoingRopePose.sortingLayerID = visual.sortingLayerID;
        outgoingRopePose.sortingOrder = visual.sortingOrder + 1;
        outgoingRopePose.enabled = false;
        if (movement == null)
            Debug.LogWarning("Qori Visual must be on a child of Player.", this);
    }

    private void LateUpdate()
    {
        // Freeze the complete procedural pose on pause, including damped carry/cloak layers.
        if (Time.deltaTime <= 0f) return;
        bool attacking = combat != null && combat.IsAttackPoseActive && (attackSprite!=null||useAnatomicalRig&&bodyRig.Ready);
        bool facingLeft = attacking ? combat.AttackDirection < 0f :
            movement != null && movement.FacingDirection < 0f;
        bool attached = thread != null && thread.IsAttached && (ropeHoldSprite != null || bodyRig.CanShowThread);
        if (attached && !showingRopePose)
        {
            ropeFacingLeft = facingLeft;
            movingForward = true;
            ropeVerticalPhase = 0;
        }
        if (attached) facingLeft = ropeFacingLeft;
        swingAnimation.Step(facingLeft?-1:1);
        if(!attached&&!attacking&&swingAnimation.PreserveReleaseFacing)facingLeft=swingAnimation.Facing<0;
        secondary.Step(movement,thread,attacking,swingAnimation);
        layeredCloak.Impulse(secondary.CloakKick);
        transform.localRotation = restingRotation;
        visual.flipX = facingLeft;
        // Mirror the authored horizontal offset too: the asymmetrical image
        // is centered on Qori's body, rather than on the cloak and weapon.
        transform.localPosition = new Vector3(facingLeft ? -restingPosition.x : restingPosition.x,
            restingPosition.y, restingPosition.z);
        bool grounded = movement != null && movement.IsGrounded;
        bool idle = enableIdleBreathing && grounded && Mathf.Abs(movement.ObservedVelocity.x) < 0.1f;
        bool walking = grounded && Mathf.Abs(movement.ObservedVelocity.x) > 0.1f && walkFrames.Length > 0;
        if (walking)
        {
            int frame = Mathf.FloorToInt(walkFrameClock) % walkFrames.Length;
            visual.sprite = walkFrames[frame] != null ? walkFrames[frame] : idleSprite;
            walkFrameClock = (walkFrameClock + Time.deltaTime * framesPerSecondAtFullSpeed *
                Mathf.Clamp(Mathf.Abs(body.linearVelocity.x) / referenceMoveSpeed, 0.1f, 2f)) % walkFrames.Length;
        }
        else
        {
            bool rising = body != null && body.linearVelocity.y > 0.1f;
            bool airborne = body != null && !grounded;
            // Keep one authored image through the apex; geometry carries the motion.
            visual.sprite = useAirFollowThrough && airborne && fallSprite != null ? fallSprite :
                rising && jumpSprite != null ? jumpSprite :
                airborne && fallSprite != null ? fallSprite : idleSprite;
        }
        wasWalking = walking;
        idleBlend = Mathf.MoveTowards(idleBlend, idle ? 1f : 0f, Time.deltaTime * 5f);
        float breath = (0.5f - 0.5f * Mathf.Cos(Time.time * 2f * Mathf.PI / breathingCycleSeconds))
            * breathingAmount * idleBlend;
        if (useAnatomicalRig && bodyRig.Ready) breath = 0f;
        transform.localScale = new Vector3(restingScale.x * (1f - breath * 0.35f),
            restingScale.y * (1f + breath), restingScale.z);
        if (playerCollider != null && transform.parent != null)
        {
            // Anchor the visual stretch to the player's soles, not its center.
            Vector3 feetWorld = playerCollider.bounds.center;
            feetWorld.y = playerCollider.bounds.min.y;
            float feetY = transform.parent.InverseTransformPoint(feetWorld).y;
            Vector3 position = transform.localPosition;
            position.y += (restingPosition.y - feetY) * breath;
            transform.localPosition = position;
        }
        Color tint = restingColor;
        if (attacking && !(useAnatomicalRig && bodyRig.Ready && combat.CurrentAttack!=null && combat.CurrentAttack.animation!=null))
        {
            // The attack uses a wider canvas; compensate only the visual child.
            visual.sprite = attackSprite;
            transform.localScale = new Vector3(restingScale.x * attackPoseScale,
                restingScale.y * attackPoseScale, restingScale.z);
            transform.localPosition = new Vector3(
                (restingPosition.x + attackPoseOffset.x) * (facingLeft ? -1f : 1f),
                restingPosition.y + attackPoseOffset.y, restingPosition.z);
            idleBlend = 0f;
        }
        showingRopePose = attached;
        if (showingRopePose)
        {
            if ((useContinuousRopeMotion && ropeMotion.Ready) || (useLayeredRopeCloak && layeredCloak.CanShow(7)))
            {
                activeRopePose = ropeHoldSprite;
                pendingRopePose = null;
                outgoingRopePose.enabled = false;
            }
            else UpdateRopePose();
            visual.sprite = activeRopePose;
            transform.localScale = restingScale;
            transform.localPosition = new Vector3(
                (restingPosition.x + ropeHoldOffset.x) * (facingLeft ? -1f : 1f),
                restingPosition.y + ropeHoldOffset.y, restingPosition.z);
            // The player parent is non-uniformly scaled. Rotating this child
            // would shear the artwork; the anatomical rig rotates its joints.
            if(!(useAnatomicalRig && bodyRig.CanShowThread))ApplyRopeLean();
            idleBlend = 0f;
        }
        // Alpha flashing remains visible on full-color art with a white tint.
        if (health != null && health.IsDamageFlashVisible) tint.a *= 0.3f;
        visual.color = tint;
        if (showingRopePose && outgoingRopePose.enabled)
        {
            float blend = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((Time.time - ropeBlendStarted) / ropePoseBlendSeconds));
            Color incoming = tint; incoming.a *= blend;
            Color outgoing = tint; outgoing.a *= 1f - blend;
            visual.color = incoming;
            outgoingRopePose.color = outgoing;
            outgoingRopePose.flipX = visual.flipX;
            if (blend >= 1f) outgoingRopePose.enabled = false;
        }
        bool continuous = showingRopePose && useContinuousRopeMotion && ropeMotion.Ready;

        bool groundedForArt = grounded;
        bool layeredAttack=attacking && combat.CurrentAttack!=null && combat.CurrentAttack.animation!=null;
        bool riggedGround = bodyRig.Prepare(useAnatomicalRig && grounded && !showingRopePose && (!attacking||layeredAttack) && visualWasEnabled, facingLeft ? -1f : 1f);
        bool riggedAir = bodyRig.PrepareAir(useAnatomicalRig && !grounded && !showingRopePose && (!attacking||layeredAttack) && visualWasEnabled, facingLeft ? -1f : 1f);
        bool riggedThread = bodyRig.PrepareThread(useAnatomicalRig && showingRopePose && visualWasEnabled, thread, facingLeft ? -1f : 1f);
        bool riggedCombat = bodyRig.PrepareCombat(useAnatomicalRig && attacking && !layeredAttack && !showingRopePose && visualWasEnabled,combat,facingLeft?-1f:1f);
        bodyRig.ApplyCombatLayer(layeredAttack&&!showingRopePose,combat);
        bool rigged = riggedGround || riggedAir || riggedThread || riggedCombat;
        if (riggedGround) { facingLeft = bodyRig.VisualFacing < 0f; visual.flipX = facingLeft; }
        int layeredPose = showingRopePose ? 7 : 0;
        bool layeredWanted = showingRopePose ? useLayeredRopeCloak : !groundedForArt && useLayeredAirCloak;
        if (groundedForArt && !showingRopePose)
        {
            layeredWanted = !walking && visual.sprite == idleSprite && useLayeredIdleCloak;
            if (layeredWanted) layeredPose = 8;
            if (walking && useLayeredWalkingCloak)
                for (int i = 0; i < walkFrames.Length && i < 6; i++)
                    if (walkFrames[i] == visual.sprite) { layeredPose = i + 1; layeredWanted = true; break; }
        }
        if (attacking && !showingRopePose)
        {
            layeredPose = combat.IsAttackWindup && !combat.IsUpwardAttack && layeredCloak.CanShow(10) ? 10 : 9;
            layeredWanted = useLayeredAttackCloak;
        }
        if (riggedGround) { layeredPose = 8; layeredWanted = true; }
        if (riggedAir) { layeredPose = 0; layeredWanted = true; }
        if (riggedAir && bodyRig.WallPoseActive) { layeredPose = 7; layeredWanted = true; }
        if (riggedThread) { layeredPose = 7; layeredWanted = true; }
        bool layered = layeredCloak.Show(layeredWanted && body != null &&
            visualWasEnabled,
            body != null ? body.linearVelocity : Vector2.zero, visual.flipX, tint, layeredPose, attacking && !layeredAttack && !showingRopePose ? attackPoseScale : 1f,
            rigged ? bodyRig.StepWave : Mathf.Sin(walkFrameClock * Mathf.PI / 3f), rigged ? bodyRig : null,swingAnimation);
        if (rigged) bodyRig.Render(tint);
        ropeMotion.Show(continuous && !layered, body != null ? body.linearVelocity : Vector2.zero,
            visual.flipX, tint, grounded);
        bool walkFollow = walkingMotion.Show(!layered && !rigged && (groundedForArt ? useWalkingFollowThrough : useAirFollowThrough) &&
            !showingRopePose && !attacking, visual.sprite, body != null ? body.linearVelocity.x : 0f,
            referenceMoveSpeed, framesPerSecondAtFullSpeed, visual.flipX, tint,
            body != null ? body.linearVelocity.y : 0f, groundedForArt);
        visual.enabled = visualWasEnabled && !continuous && !walkFollow && !layered && !rigged;
        if (!showingRopePose)
        {
            activeRopePose = pendingRopePose = null;
            outgoingRopePose.enabled = false;
            ropeLean = ropeLeanVelocity = 0f;
        }
    }

    private void UpdateRopePose()
    {
        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
        float forwardSpeed = velocity.x * (ropeFacingLeft ? -1f : 1f);
        // Keep direction through the zero-speed region at the swing apex.
        if (forwardSpeed > 0.45f) movingForward = true;
        else if (forwardSpeed < -0.45f) movingForward = false;
        if (velocity.y > 0.6f) ropeVerticalPhase = 1;
        else if (velocity.y < -0.6f) ropeVerticalPhase = -1;
        else if (Mathf.Abs(velocity.y) < 0.2f) ropeVerticalPhase = 0;
        Sprite rise = movingForward ? ropeRiseForwardSprite : ropeRiseBackwardSprite;
        Sprite fall = movingForward ? ropeFallForwardSprite : ropeFallBackwardSprite;
        if (rise == null) rise = ropeRiseSprite;
        if (fall == null) fall = ropeFallSprite;
        Sprite desired = ropeVerticalPhase > 0 && rise != null ? rise :
            ropeVerticalPhase < 0 && fall != null ? fall : ropeHoldSprite;
        if (activeRopePose == null)
        {
            activeRopePose = desired;
            pendingRopePose = desired;
            pendingPoseSince = Time.time;
            return;
        }
        if (desired != pendingRopePose)
        {
            pendingRopePose = desired;
            pendingPoseSince = Time.time;
        }
        if (desired == activeRopePose || Time.time - pendingPoseSince < ropePoseConfirmSeconds ||
            outgoingRopePose.enabled) return;
        outgoingRopePose.sprite = activeRopePose;
        outgoingRopePose.enabled = true;
        activeRopePose = desired;
        ropeBlendStarted = Time.time;
    }

    private void ApplyRopeLean()
    {
        Vector3 gripLocal = new Vector3(ropeHandPoint.x * (ropeFacingLeft ? -1f : 1f), ropeHandPoint.y, 0f);
        Vector3 gripWorld = transform.TransformPoint(gripLocal);
        Vector2 toAnchor = thread.AnchorPosition - (Vector2)gripWorld;
        float target = toAnchor.sqrMagnitude > 0.001f
            ? Mathf.Clamp(Vector2.SignedAngle(Vector2.up, toAnchor), -maximumRopeLean, maximumRopeLean) : 0f;
        // A planted character should not lean through the floor.
        if (movement != null && movement.HasGroundContact) target = 0f;
        ropeLean = Mathf.SmoothDampAngle(ropeLean, target, ref ropeLeanVelocity,
            ropeLeanSmoothSeconds, Mathf.Infinity, Time.deltaTime);
        transform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, ropeLean);
        // Rotation moves the torso around the grip without pulling the rope endpoint.
        transform.position += gripWorld - transform.TransformPoint(gripLocal);
    }

    private void OnDisable()
    {
        if (visual == null) return;
        visual.enabled = visualWasEnabled;
        if (walkingMotion != null) walkingMotion.Hide();
        if (bodyRig != null) bodyRig.Hide();
        if (layeredCloak != null) layeredCloak.Hide();
        if (ropeMotion != null) ropeMotion.Show(false, Vector2.zero, false, restingColor, false);
        visual.color = restingColor;
        visual.sprite = idleSprite;
        visual.flipX = originalFlip;
        transform.localPosition = restingPosition;
        transform.localScale = restingScale;
        transform.localRotation = restingRotation;
        ropeLean = ropeLeanVelocity = 0f;
        idleBlend = 0f;
        walkFrameClock = 0f;
        wasWalking = false;
        showingRopePose = false;
        activeRopePose = pendingRopePose = null;
        if (outgoingRopePose != null) outgoingRopePose.enabled = false;
    }
    private void OnDestroy()
    {
        if (outgoingRopePose != null) Destroy(outgoingRopePose.gameObject);
        if (ropeMotion != null) Destroy(ropeMotion);
        if (walkingMotion != null) Destroy(walkingMotion);
        if (layeredCloak != null) Destroy(layeredCloak);
        if (bodyRig != null) Destroy(bodyRig);
        if (feedback != null) Destroy(feedback);
    }
}

