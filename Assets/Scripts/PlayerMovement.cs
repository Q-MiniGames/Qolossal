using UnityEngine;
using UnityEngine.InputSystem;

// First movement prototype. This is the sole writer of player velocity.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    public enum LaunchKind { None, Jump, FlowerBoost, WallJump, DoubleJump }
    public bool IsWallSliding { get; private set; }
    public float WallDirection { get; private set; }
    private float wallSlideSpeed => Tuning.wallSlideSpeed;
    private float wallJumpHorizontalSpeed => Tuning.wallJumpHorizontalSpeed;
    private float wallJumpPushSeconds => Tuning.wallJumpPushSeconds;
    private float wallJumpUntil=float.NegativeInfinity;
    private float wallSteeringUntil=float.NegativeInfinity;
    private readonly RaycastHit2D[] wallHits=new RaycastHit2D[8];
    public bool IsLedgeHanging { get; private set; }
    public bool IsLedgeClimbing { get; private set; }
    public Vector2 LedgePoint { get; private set; }
    public float LedgeDirection { get; private set; }
    public float LedgeClimbProgress => !IsLedgeClimbing?0f:!ledgeAcross?
        .65f*Mathf.InverseLerp(ledgeHangPosition.y,ledgeStandPosition.y,body.position.y):
        Mathf.Lerp(.65f,1f,Mathf.InverseLerp(ledgeHangPosition.x,ledgeStandPosition.x,body.position.x));
    private Collider2D ledgeSurface;
    private Vector2 ledgeSurfacePosition,ledgeHangPosition,ledgeStandPosition;
    private float savedLedgeGravity,ledgeRetryAt;
    private bool ledgeGravityOwned,ledgeAcross;
    private InputAction ledgeVerticalAction;
    private float ledgeVerticalInput;
    private readonly Collider2D[] ledgeOverlaps=new Collider2D[8];

    public float FacingDirection { get; private set; } = 1f;
    public bool IsGrounded { get; private set; }
    public bool HasGroundContact { get; private set; }
    public float MoveInput => moveInput;
    public bool RunRequested => runHeld;
    // Running is Qori's normal pace (16 Oct). Areas that want a walk (house interiors, later) set ForceWalk.
    public bool ForceWalk { get; set; }
    // Seconds Qori has been running at full speed (0 when not): the sprint attack needs a run-up.
    public float RunningFor => IsRunning ? Time.time - runningSince : 0f;
    private float runningSince;
    public bool IsRunning { get; private set; }
    public float WalkSpeed => moveSpeed;
    public float RunSpeed => Mathf.Max(moveSpeed,runSpeed);
    public Vector2 ObservedDisplacement { get; private set; }
    public Vector2 ObservedVelocity { get; private set; }
    /// <summary>The collider Qori is standing on (null when airborne).</summary>
    public Collider2D GroundCollider { get; private set; }
    public Vector2 GroundNormal { get; private set; } = Vector2.up;
    public Vector2 GroundPoint { get; private set; }
    public float GroundDistance { get; private set; } = float.PositiveInfinity;
    public float LandingTimeEstimate { get; private set; } = float.PositiveInfinity;
    public int LaunchVersion { get; private set; }
    public LaunchKind LastLaunchKind { get; private set; }
    public float LastLaunchSpeed { get; private set; }
    public float LastLaunchTime { get; private set; } = float.NegativeInfinity;
    public int LandingVersion { get; private set; }
    public float LastLandingSpeed { get; private set; }
    public float LastLandingTime { get; private set; } = float.NegativeInfinity;
    public int ResetVersion { get; private set; }
    /// <summary>Counts returns to the checkpoint (a death, a fall, deep water); a scene's own start counts too.</summary>
    public int RespawnVersion { get; private set; }
    private float moveSpeed => Tuning.moveSpeed;
    private float runSpeed => Tuning.runSpeed;
    private float groundAcceleration => Tuning.groundAcceleration;
    private float airAcceleration => Tuning.airAcceleration;

    private float jumpSpeed => Tuning.jumpSpeed;
    private float coyoteTime => Tuning.coyoteTime;
    private float jumpBufferTime => Tuning.jumpBufferTime;
    private float jumpReleaseMultiplier => Tuning.jumpReleaseMultiplier;
    private float maximumFallSpeed => Tuning.maximumFallSpeed;
    private float fallGravityMultiplier => Tuning.fallGravityMultiplier;
    private float apexGravityMultiplier => Tuning.apexGravityMultiplier;
    private float apexSpeedBand => Tuning.apexSpeedBand;
    private float airDeceleration => Tuning.airDeceleration;

    private float dashSpeed => Tuning.dashSpeed;
    private float dashSeconds => Tuning.dashSeconds;
    private float dashCooldown => Tuning.dashCooldown;

    private float glideFallSpeed => Tuning.glideFallSpeed;
    private float glideSpeed => Tuning.glideSpeed;
    private float glideBrake => Tuning.glideBrake;
    [Tooltip("Off: holding Jump while falling opens the Glidecap, even the jump's own press. " +
             "On: it needs its own press of Jump in mid-air, clear of walls, held from then on.")]
    [Header("Glidecap glide (relic)")]
    [SerializeField] private bool glideNeedsNewPress = false;

    /// <summary>True during a Wind Leaf dash; DashDirection is -1 or 1.</summary>
    public bool IsDashing { get; private set; }
    public float DashDirection { get; private set; } = 1f;
    public int DashVersion { get; private set; }
    public Vector2 DashStart { get; private set; }
    /// <summary>True while the Glidecap is open (jump held while falling).</summary>
    public bool IsGliding { get; private set; }
    /// <summary>True while Jump is held.</summary>
    public bool JumpHeld => jumpHeld;
    /// <summary>True once the air dash is spent; ground, a wall or a thread renews it.</summary>
    public bool AirDashUsed => airDashUsed;
    private float dashUntil, nextDashAt, lastDashPressedTime = float.NegativeInfinity;
    private bool airDashUsed;
    private bool airJumpUsed;   // the double jump, spent until ground, a wall, a ledge or the thread
    public bool AirJumpUsed => airJumpUsed;
    // With glideNeedsNewPress the Glidecap opens only on its own press of Jump in mid-air (one that
    // didn't make a jump or wall jump), held from then on: holding Jump through a jump or up a
    // wall never opens it. Without it, any held Jump opens it once he falls.
    private bool airJumpPress, glideArmed, glideNeedsPressAfterAttack;
    private float GlidePull => Tuning.glidePull;   // units/s per second: the drift's own gentle gravity
    private float GlideMinHeight => Tuning.glideMinHeight;   // closer to the ground than this, a press is a buffered jump
    private float GlideWallClearance => Tuning.glideWallClearance;   // closer to a clingable wall than this, a press is for the wall

    // Ends a dash: an ordinary end keeps walking speed along it; a jump, hit or thread keeps
    // whatever velocity they set.
    private void EndDash(ref Vector2 velocity, bool settle)
    {
        IsDashing = false;
        nextDashAt = Time.time + dashCooldown;
        if (settle) velocity = new Vector2(DashDirection * moveSpeed, 0f);
        else velocity.x = Mathf.Clamp(velocity.x, -RunSpeed, RunSpeed);   // a dash-jump carries running speed, no more
    }

    private void CancelDash() { IsDashing = false; IsGliding = false; airJumpPress = glideArmed = false; }

    // The evade: a quick dodge along the ground. Qori can't be hurt during it (and for evadeGrace after), and he
    // passes through enemies: their colliders stop touching his for the evade. The Shellback's stone shell is the
    // exception: an evade into it stops against it.
    public bool IsEvading { get; private set; }
    public bool Untouchable => IsEvading || Time.time < evadeGraceUntil;
    public float EvadeDirection { get; private set; } = 1f;
    public int EvadeVersion { get; private set; }
    private float evadeUntil, nextEvadeAt, evadeGraceUntil, lastEvadePressedTime = float.NegativeInfinity;
    private readonly System.Collections.Generic.List<Collider2D> passedThrough = new System.Collections.Generic.List<Collider2D>();
    private readonly Collider2D[] evadeOverlaps = new Collider2D[16];
    private readonly RaycastHit2D[] evadeHits = new RaycastHit2D[8];
    private Collider2D[] ownColliders;

    private void StopEvade()
    {
        if (IsEvading) { nextEvadeAt = Time.time + Tuning.evadeCooldown; evadeGraceUntil = Time.time + Tuning.evadeGrace; }
        IsEvading = false;
    }

    // Enemies (anything with a damage receiver that isn't Qori) the evade is passing: their collisions with Qori are
    // switched off, and back on once he is clear of them.
    private void PassThroughEnemies(Vector2 step)
    {
        if (ownColliders == null) ownColliders = GetComponents<Collider2D>();
        var bounds = ownColliders.Length > 0 ? ownColliders[0].bounds : new Bounds(body.position, Vector3.one);
        int n = Physics2D.OverlapBoxNonAlloc((Vector2)bounds.center + step, (Vector2)bounds.size + new Vector2(Mathf.Abs(step.x) + .3f, 0f), 0f, evadeOverlaps);
        for (int i = 0; i < n; i++)
        {
            var c = evadeOverlaps[i];
            if (c == null || c.isTrigger || c.attachedRigidbody == body || passedThrough.Contains(c) || IsShellback(c)) continue;
            if (c.GetComponentInParent<ICombatDamageReceiver>() == null || c.GetComponentInParent<PlayerMovement>() != null) continue;
            foreach (var own in ownColliders) Physics2D.IgnoreCollision(own, c, true);
            passedThrough.Add(c);
        }
    }

    private void RestorePassedThrough(bool force)
    {
        if (passedThrough.Count == 0 || ownColliders == null || ownColliders.Length == 0) return;
        for (int i = passedThrough.Count - 1; i >= 0; i--)
        {
            var c = passedThrough[i];
            bool clear = c == null || !c.isActiveAndEnabled || !ownColliders[0].bounds.Intersects(c.bounds);
            if (!force && !clear) continue;
            if (c != null) foreach (var own in ownColliders) Physics2D.IgnoreCollision(own, c, false);
            passedThrough.RemoveAt(i);
        }
    }

    private static bool IsShellback(Collider2D c) => c != null && c.GetComponentInParent<ShellbackEnemy>() != null;

    // True when the evade's next step would run into a Shellback.
    private bool ShellbackAhead(float distance)
    {
        if (ownColliders == null) ownColliders = GetComponents<Collider2D>();
        if (ownColliders.Length == 0) return false;
        int n = ownColliders[0].Cast(new Vector2(EvadeDirection, 0f), evadeHits, distance + .05f, true);
        for (int i = 0; i < n; i++) if (!evadeHits[i].collider.isTrigger && IsShellback(evadeHits[i].collider)) return true;
        return false;
    }

    // A clingable wall within reach on either side: Jump there is for wall jumps (an early press
    // is buffered into one), so it never opens the Glidecap.
    private bool NearWall()
    {
        if (!Relics.Has(abilities, Relics.ClimbingMoss)) return false;
        for (int side = -1; side <= 1; side += 2)
        {
            int count = body.Cast(Vector2.right * side, groundFilter, wallHits, GlideWallClearance);
            for (int i = 0; i < count; i++)
                if (Mathf.Abs(wallHits[i].normal.y) < .25f && WallSurface.AllowsCling(wallHits[i].collider)) return true;
        }
        return false;
    }

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers;
    private float groundCheckDistance => Tuning.groundCheckDistance;
    private float landingLookAheadDistance => Tuning.landingLookAheadDistance;

    [Header("Test Room")]
    [Tooltip("Falling below this height returns the player to their latest checkpoint, or the starting position.")]
    [SerializeField] private float resetBelowY = -15f;
    private float GroundSnapDistance => Tuning.groundSnapDistance;
    private float MaxSlopeRise => Tuning.maxSlopeRise;   // tan of ~50 degrees, the steepest walkable surface
    private float LaunchGraceSeconds => Tuning.launchGraceSeconds;   // after a jump/boost, before ground can catch Qori
    private int LaunchGraceSteps => Mathf.CeilToInt(LaunchGraceSeconds / Time.fixedDeltaTime - .001f);
    private int stepsSinceLaunch = int.MaxValue / 2;
    // A thread swing's or flower boost's momentum: kept through the air (no air drag) until he lands.
    private bool carryingMomentum;

    // The feel values (speeds, gravity, dash, glide): the shared tuning asset, read every step so edits made
    // in Play mode apply at once. A per-player asset can be set here to try a variant.
    [SerializeField] private MovementTuning tuning;
    private MovementTuning Tuning => tuning != null ? tuning : tuning = MovementTuning.Current;
    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private PhysicsMaterial2D originalMaterial;
    private PhysicsMaterial2D slidingMaterial;
    private PlayerThread thread;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction growthAction;
    private InputAction runAction;
    private InputAction dashAction, evadeAction;
    private bool runHeld;
    private float pendingLaunchSpeed;
    private Vector2 pendingKnockback;
    private bool hasPendingKnockback;
    private Vector2 combatImpulse;
    private float combatBounce;
    private PlayerCombat combatController;
    private PlayerAbilityController abilities;
    public void AddCombatImpulse(Vector2 impulse)=>combatImpulse+=impulse;
    public void QueueCombatBounce(float speed)=>combatBounce=Mathf.Max(combatBounce,speed);
    private float hitRecoveryUntil;
    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Vector2 startingPosition;
    private Vector2 respawnPosition;
    private Checkpoint activeCheckpoint;
    public bool HasCheckpoint => activeCheckpoint != null;
    private float moveInput;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private bool jumpHeld;
    private bool canCutJump;
    private Vector2 observedPosition;
    private float previousFallSpeed;
    private bool observationReady;

    private void Reset()
    {
        groundLayers = LayerMask.GetMask("Ground");
    }

    private void Awake()
    {
        if (GetComponent<QoriSounds>() == null) gameObject.AddComponent<QoriSounds>();
        body = GetComponent<Rigidbody2D>();
        combatController=GetComponent<PlayerCombat>();
        ResetObservation();
        thread = GetComponent<PlayerThread>();
        startingPosition = body.position;
        respawnPosition = startingPosition;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerCollider = GetComponent<BoxCollider2D>();
        originalMaterial = playerCollider.sharedMaterial;
        // Running stops via acceleration above; surface friction must not pin
        // the player against vertical edges when the rope pulls sideways.
        slidingMaterial = new PhysicsMaterial2D("Player Slide (Runtime)")
        {
            friction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine2D.Minimum,
            bounceCombine = PhysicsMaterialCombine2D.Minimum
        };
        playerCollider.sharedMaterial = slidingMaterial;

        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.useTriggers = false;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone");
        moveAction.AddBinding("<Gamepad>/dpad/x");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        growthAction = new InputAction("Growth", InputActionType.Button);
        growthAction.AddBinding("<Keyboard>/e");
        growthAction.AddBinding("<Gamepad>/rightShoulder");
        runAction = new InputAction("Run", InputActionType.Button);
        runAction.AddBinding("<Gamepad>/leftStickPress");
        evadeAction = new InputAction("Evade", InputActionType.Button);
        evadeAction.AddBinding("<Keyboard>/leftShift");
        evadeAction.AddBinding("<Keyboard>/rightShift");
        evadeAction.AddBinding("<Gamepad>/buttonNorth");
        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/c");
        dashAction.AddBinding("<Gamepad>/buttonEast");
        ledgeVerticalAction=new InputAction("Ledge vertical",InputActionType.Value);
        ledgeVerticalAction.AddCompositeBinding("1DAxis").With("Positive","<Keyboard>/w").With("Positive","<Keyboard>/upArrow").With("Negative","<Keyboard>/s").With("Negative","<Keyboard>/downArrow");
        ledgeVerticalAction.AddBinding("<Gamepad>/leftStick/y").WithProcessor("axisDeadzone");
        ledgeVerticalAction.AddBinding("<Gamepad>/dpad/y");
    }

    private void Start()
    {
        string savedId = GameSave.CheckpointIn(gameObject.scene.path);
        if (string.IsNullOrEmpty(savedId)) return;
        Checkpoint match = null;
        foreach (Checkpoint checkpoint in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            if (checkpoint.gameObject.scene != gameObject.scene || !checkpoint.isActiveAndEnabled ||
                checkpoint.CheckpointId != savedId) continue;
            if (match != null)
            {
                Debug.LogWarning("Checkpoint IDs must be unique. Starting at the beginning because ID is duplicated: " + savedId);
                return;
            }
            match = checkpoint;
        }
        if (match == null)
        {
            Debug.LogWarning("Saved checkpoint was not found; starting at the beginning: " + savedId);
            return;
        }
        activeCheckpoint = match;
        respawnPosition = match.SpawnPosition;
        match.SetActiveMarker(true);
        Respawn();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        growthAction.Enable();
        runAction.Enable();
        dashAction.Enable(); evadeAction.Enable();
        ledgeVerticalAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        growthAction.Disable();
        runAction.Disable();
        dashAction.Disable(); evadeAction.Disable();
        ledgeVerticalAction.Disable();
        ReleaseLedge();
        runHeld = false;
        pendingLaunchSpeed = 0f;
        hasPendingKnockback = false;
        combatImpulse=Vector2.zero;combatBounce=0;
        hitRecoveryUntil = 0f;
        moveInput = 0f;
        jumpHeld = false;
        canCutJump = false;
        lastGroundedTime = lastJumpPressedTime = lastDashPressedTime = float.NegativeInfinity;
        ResetObservation();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
        growthAction?.Dispose();
        runAction?.Dispose();
        dashAction?.Dispose();
        ledgeVerticalAction?.Dispose();
        if (playerCollider != null && playerCollider.sharedMaterial == slidingMaterial)
            playerCollider.sharedMaterial = originalMaterial;
        if (slidingMaterial != null)
            Destroy(slidingMaterial);
    }

    private void Update()
    {
        if (GamePauseMenu.BlocksGameplayInput)
        {
            moveInput = 0f;
            runHeld = false;
            IsRunning = false;
            IsWallSliding=false;
            WallDirection=0;
            jumpHeld = false;
            lastJumpPressedTime = lastDashPressedTime = float.NegativeInfinity;
            ledgeVerticalInput=0;
            return;
        }
        if (dashAction.WasPressedThisFrame()) lastDashPressedTime = Time.time;
        if (evadeAction.WasPressedThisFrame()) lastEvadePressedTime = Time.time;
        moveInput = moveAction.ReadValue<float>();
        runHeld = !ForceWalk;
        ledgeVerticalInput=ledgeVerticalAction.ReadValue<float>();
        if (Mathf.Abs(moveInput) > 0.1f && Time.time>=wallSteeringUntil)
            FacingDirection = Mathf.Sign(moveInput);
        jumpHeld = jumpAction.IsPressed();
        if (jumpAction.WasPressedThisFrame())
        {
            // One press chooses a flower boost or a normal buffered jump.
            // LaunchUp queues its impulse for the next physics step.
            if (!GrowthFlower.TryActivateClosest(this))
            {
                lastJumpPressedTime = Time.time;
                // Presses on a wall, a ledge or the thread belong to those moves, never the Glidecap.
                if (!IsGrounded && !IsWallSliding && !IsLedgeHanging && !IsLedgeClimbing && (thread == null || !thread.IsAttached))
                    airJumpPress = true;
            }
        }
        if (growthAction.WasPressedThisFrame())
            GrowthPlatform.ActivateClosest(this, float.PositiveInfinity);
    }

    public bool LaunchUp(float speed)
    {
        if (!isActiveAndEnabled || speed <= 0f || Time.time < hitRecoveryUntil) return false;
        ReleaseLedge();
        pendingLaunchSpeed = speed;
        wallJumpUntil=float.NegativeInfinity;wallSteeringUntil=float.NegativeInfinity;
        lastJumpPressedTime = float.NegativeInfinity;
        // A flower launch releases an existing tether so it cannot cancel
        // the upward impulse. The player can catch an anchor again afterward.
        if (thread != null) thread.Detach();
        return true;
    }

    public void ActivateCheckpoint(Checkpoint checkpoint, Vector2 position)
    {
        if (checkpoint == null) return;
        if (activeCheckpoint != null && activeCheckpoint != checkpoint)
            activeCheckpoint.SetActiveMarker(false);
        // A new checkpoint is heard (a Waymark charting its level plays its own, over this one), but
        // not the one an arrival lands Qori on.
        if (activeCheckpoint != checkpoint && !AreaTransition.IsTransitioning && Time.timeSinceLevelLoad > .5f) Sfx.Play("Checkpoint_Touch");
        activeCheckpoint = checkpoint;
        respawnPosition = position;
        checkpoint.SetActiveMarker(true);
        // Saved by id (never by object name); enemies and health reset on a fresh session.
        if (!string.IsNullOrEmpty(checkpoint.CheckpointId))
            GameSave.SetCheckpoint(gameObject.scene.path, checkpoint.CheckpointId);
    }

    public void ApplyKnockback(Vector2 velocity)
    {
        if(combatController!=null)combatController.CancelAttack();
        ReleaseLedge();
        wallJumpUntil=float.NegativeInfinity;wallSteeringUntil=float.NegativeInfinity;
        if (thread != null) thread.Detach();
        pendingLaunchSpeed = 0f;
        pendingKnockback = velocity;
        hasPendingKnockback = true;
        hitRecoveryUntil = Time.time + 0.2f;
        CancelDash();
        lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        canCutJump = false;
    }

    private void FixedUpdate()
    {
        // Remove last step's ride so controls work on Qori's own velocity; re-added below.
        Vector2 velocity = body.linearVelocity - platformVelocity;
        // An impulse this step (hit, bounce, knockback, flower boost) must leave the ground freely.
        bool impulse = combatImpulse.y > 0f || combatBounce > 0f || hasPendingKnockback || pendingLaunchSpeed > 0f;
        velocity+=combatImpulse;combatImpulse=Vector2.zero;
        if(combatBounce>0){velocity.y=Mathf.Max(velocity.y,combatBounce);combatBounce=0;canCutJump=false;}
        float groundControl=combatController!=null?combatController.GroundControl:1;
        float airControl=combatController!=null?combatController.AirControl:1;
        if(combatController!=null)velocity+=Physics2D.gravity*body.gravityScale*(combatController.GravityMultiplier-1)*Time.fixedDeltaTime;
        // The plunge: held in the air through its wind-up, then straight down at its speed.
        float plunge=combatController!=null?combatController.PlungeSpeed:0f;
        if(plunge>0f){velocity.y=combatController.IsPlungeWindup?0f:-plunge;velocity.x*=.85f;canCutJump=false;}
        ObserveMotion(velocity);
        if(UpdateLedge())return;
        bool wasGrounded = IsGrounded && !impulse;
        LaunchKind launch = LaunchKind.None;
        bool surfaceWalking = false;
        bool recovering = Time.time < hitRecoveryUntil;
        if (hasPendingKnockback)
        {
            velocity = pendingKnockback;
            hasPendingKnockback = false;
        }
        if (pendingLaunchSpeed > 0f)
        {
            velocity.y = Mathf.Max(velocity.y, pendingLaunchSpeed);
            launch = LaunchKind.FlowerBoost;
            pendingLaunchSpeed = 0f;
            canCutJump = false;
            lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        }
        bool grounded = false;
        Vector2 groundNormal = Vector2.up;
        Vector2 groundPoint = Vector2.zero;
        Collider2D groundCollider = null;
        float nearestGround = float.PositiveInfinity;
        float snapDistance = 0f;
        // Walking up a slope moves Qori upward, so keep checking for ground while he was grounded,
        // and also when he rises no faster than a walkable slope allows (e.g. landing on a slope
        // while pressing uphill) - but never in the first steps after a launch, so jumps stay jumps.
        stepsSinceLaunch++;
        bool slopeRise = velocity.y <= Mathf.Abs(velocity.x) * MaxSlopeRise + .1f && stepsSinceLaunch > LaunchGraceSteps
                         && !hasPendingKnockback && Time.time >= hitRecoveryUntil;
        if (velocity.y <= 0.1f || wasGrounded || slopeRise)
        {
            float castDistance = velocity.y < -0.1f
                ? Mathf.Max(groundCheckDistance, landingLookAheadDistance) : groundCheckDistance;
            if (wasGrounded) castDistance = Mathf.Max(castDistance, GroundSnapDistance);
            int count = body.Cast(Vector2.down, groundFilter, groundHits, castDistance);
            for (int i = 0; i < count; i++)
                if (groundHits[i].normal.y > 0.65f)
                {
                    if (groundHits[i].distance <= groundCheckDistance) grounded = true;
                    if (groundHits[i].distance < nearestGround)
                    {
                        nearestGround = groundHits[i].distance;
                        groundNormal = groundHits[i].normal;
                        groundPoint = groundHits[i].point;
                        groundCollider = groundHits[i].collider;
                    }
                }
        }

        // Stay on the surface over slope kinks and downhill instead of hopping off it: pull down
        // to the nearest surface below, only when not already touching one.
        if (!grounded && wasGrounded && nearestGround <= GroundSnapDistance)
        {
            grounded = true;
            snapDistance = Mathf.Max(0f, nearestGround - groundCheckDistance * .5f);
        }
        if (grounded)
            lastGroundedTime = Time.time;

        bool attached = thread != null && thread.IsAttached;
        WallDirection=0;
        IsWallSliding=false;
        if(!grounded && !attached && !recovering && launch==LaunchKind.None && Time.time>=wallJumpUntil)
        {
            float nearestWall=float.PositiveInfinity;
            // Contact, rather than held input, makes a wall available to jump
            // from. Check both sides so neutral input works in either facing.
            for(int side=0;side<2;side++)
            {
                float direction=side==0?FacingDirection:-FacingDirection;
                int wallCount=body.Cast(Vector2.right*direction,groundFilter,wallHits,.045f);
                for(int i=0;i<wallCount;i++)
                    if(wallHits[i].normal.x*direction<-.85f && Mathf.Abs(wallHits[i].normal.y)<.25f && wallHits[i].distance<nearestWall &&
                        WallSurface.AllowsCling(wallHits[i].collider))
                    {WallDirection=direction;nearestWall=wallHits[i].distance;}
            }
        }
        if (grounded && !wasGrounded && !attached && previousFallSpeed > 0.1f)
        {
            LastLandingSpeed = previousFallSpeed;
            LastLandingTime = Time.fixedTime;
            LandingVersion++;
        }
        if(!grounded && !attached && !recovering && launch==LaunchKind.None && WallDirection!=0 && velocity.y<=1f && moveInput*WallDirection>=-.1f && ledgeVerticalInput>=-.1f && Time.time>=ledgeRetryAt && TryGrabLedge(WallDirection))return;
        if (attached || launch == LaunchKind.FlowerBoost) carryingMomentum = true;
        else if (grounded || WallDirection != 0) carryingMomentum = false;
        if (recovering)
        {
            // Let physics resolve the hit before normal steering resumes.
            if (thread != null) thread.Detach();
            lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        }
        else if (attached)
        {
            // Steer the swing without replacing velocity produced by the rope.
            velocity.x += moveInput * thread.SwingAcceleration * Time.fixedDeltaTime;
            canCutJump = false;
            lastGroundedTime = float.NegativeInfinity;
        }
        else if (grounded && (impulse || launch != LaunchKind.None))
        {
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * (runHeld ? RunSpeed : moveSpeed)*groundControl,
                groundAcceleration * Time.fixedDeltaTime);
        }
        else if (grounded)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * (runHeld ? RunSpeed : moveSpeed)*groundControl,
                groundAcceleration * Time.fixedDeltaTime);
            // Move along the surface (the collider is frictionless, so on a slope gravity would
            // otherwise slide him downhill and uphill walking would read as leaving the ground).
            velocity.y = -velocity.x * groundNormal.x / Mathf.Max(.2f, groundNormal.y) - snapDistance / Time.fixedDeltaTime;
            surfaceWalking = true;
            // Standing on a moving (kinematic) platform: move with it in the same physics step.
            Rigidbody2D platform = groundCollider != null ? groundCollider.attachedRigidbody : null;
            ridingVelocity = platform != null && platform.bodyType == RigidbodyType2D.Kinematic ? platform.linearVelocity : Vector2.zero;
        }
        else if (Mathf.Abs(moveInput) > 0.01f && Time.time>=wallSteeringUntil)
        {
            // Retain launch speed; opposite input can brake and redirect it.
            bool alreadyFaster = Mathf.Sign(moveInput) == Mathf.Sign(velocity.x) &&
                Mathf.Abs(velocity.x) >= Mathf.Abs(moveInput * moveSpeed);
            if (!alreadyFaster)
                velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed,
                    airAcceleration * airControl * Time.fixedDeltaTime);
        }
        else if (!carryingMomentum && Time.time >= wallSteeringUntil)
        {
            // Stick released in the air: his own speed fades, so a jump ends about where it's let go.
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, airDeceleration * airControl * Time.fixedDeltaTime);
        }

        bool combatAllowsJump=combatController==null||combatController.CanCancel(CombatCancel.Jump);
        // Wall slides and wall jumps are the Climbing Moss relic's ability; ledge grabs are not.
        if(abilities==null)abilities=GetComponent<PlayerAbilityController>();
        bool wallCling=Relics.Has(abilities,Relics.ClimbingMoss);
        if (combatAllowsJump && Time.time - lastJumpPressedTime <= jumpBufferTime &&
            Time.time - lastGroundedTime <= coyoteTime)
        {
            velocity.y = jumpSpeed;
            lastJumpPressedTime = lastGroundedTime = float.NegativeInfinity;
            canCutJump = true;
            launch = LaunchKind.Jump;
        }
        else if(combatAllowsJump && !recovering && !attached && launch==LaunchKind.None && WallDirection!=0 && wallCling && Time.time-lastJumpPressedTime<=jumpBufferTime)
        {
            bool jumpAway=moveInput*WallDirection<-.1f;
            velocity=new Vector2(jumpAway?-WallDirection*wallJumpHorizontalSpeed:0f,jumpSpeed);
            FacingDirection=jumpAway?-WallDirection:WallDirection;
            wallJumpUntil=Time.time+wallJumpPushSeconds;
            wallSteeringUntil=jumpAway?wallJumpUntil:float.NegativeInfinity;
            lastJumpPressedTime=lastGroundedTime=float.NegativeInfinity;
            canCutJump=true;
            launch=LaunchKind.WallJump;
            WallDirection=0;
        }
        // The double jump: a fresh press in mid-air, once. Close to the ground the press is left to the buffered jump.
        else if(combatAllowsJump && Tuning.doubleJump && !airJumpUsed && !grounded && !recovering && !attached && launch==LaunchKind.None &&
                !IsLedgeHanging && !IsLedgeClimbing && Time.time-lastJumpPressedTime<=jumpBufferTime && !(nearestGround<=GlideMinHeight))
        {
            velocity.y=Tuning.doubleJumpSpeed;
            airJumpUsed=true;
            lastJumpPressedTime=float.NegativeInfinity;
            canCutJump=true;
            launch=LaunchKind.DoubleJump;
        }
        if(WallDirection!=0 && wallCling && launch==LaunchKind.None && velocity.y<0 && moveInput*WallDirection>=-.1f)
        {
            velocity.y=Mathf.Max(velocity.y,-wallSlideSpeed);
            IsWallSliding=true;
            FacingDirection=WallDirection;
        }

        // Wind Leaf: a short, level dash along the input (or facing) direction. On the ground it
        // can be used again after a short rest; in the air once, until ground, a wall or a thread.
        if(grounded || IsWallSliding || attached) airDashUsed=airJumpUsed=false;
        bool dashAllowed=combatController==null||combatController.CanCancel(CombatCancel.Jump);
        if(IsDashing && (launch!=LaunchKind.None || recovering || attached || Time.fixedTime>=dashUntil))
            EndDash(ref velocity, launch==LaunchKind.None && !recovering && !attached);
        else if(!IsDashing && dashAllowed && Relics.Has(abilities,Relics.WindLeaf) && Time.time-lastDashPressedTime<=jumpBufferTime &&
                Time.time>=nextDashAt && !recovering && !attached && launch==LaunchKind.None && (grounded || !airDashUsed))
        {
            DashDirection=Mathf.Abs(moveInput)>.1f ? Mathf.Sign(moveInput) : FacingDirection;
            FacingDirection=DashDirection;
            if(!grounded) airDashUsed=true;
            IsDashing=true; dashUntil=Time.fixedTime+dashSeconds; DashStart=body.position; DashVersion++;
            lastDashPressedTime=float.NegativeInfinity; canCutJump=false;
            wallJumpUntil=wallSteeringUntil=float.NegativeInfinity;
            if(combatController!=null) combatController.CancelAttack();
        }
        if(IsDashing)
        {
            velocity=new Vector2(DashDirection*dashSpeed,0f);
            surfaceWalking=false;
        }

        // Evade (Shift / Y): on the ground, along the input (or facing) direction.
        if(IsEvading && (Time.fixedTime>=evadeUntil || recovering || attached || launch!=LaunchKind.None || IsDashing))
            StopEvade();
        else if(!IsEvading && dashAllowed && grounded && !IsDashing && !recovering && !attached && launch==LaunchKind.None &&
                Time.time-lastEvadePressedTime<=jumpBufferTime && Time.time>=nextEvadeAt)
        {
            EvadeDirection=Mathf.Abs(moveInput)>.1f ? Mathf.Sign(moveInput) : FacingDirection;
            FacingDirection=EvadeDirection;
            IsEvading=true; evadeUntil=Time.fixedTime+Tuning.evadeSeconds; EvadeVersion++;
            lastEvadePressedTime=float.NegativeInfinity;
            if(combatController!=null) combatController.CancelAttack();
        }
        if(IsEvading)
        {
            float step=Tuning.evadeSpeed*Time.fixedDeltaTime;
            if(ShellbackAhead(step)) { StopEvade(); velocity.x=0f; }   // the stone shell stops the dodge
            else { velocity.x=EvadeDirection*Tuning.evadeSpeed; PassThroughEnemies(new Vector2(EvadeDirection*step,0f)); }
        }
        else RestorePassedThrough(false);

        // Glidecap: holding Jump while falling opens it (with glideNeedsNewPress, only a fresh
        // mid-air press), braking the fall to a slow drift that can be steered. Walls, threads,
        // ledges and dashes close it.
        // A press that jumped (coyote or buffered) or wall-jumped is spent; one just before landing
        // is left to the buffered jump.
        // An attack in the air closes the canopy (Qori can't swing and glide at once): it falls, and only a fresh
        // Jump press opens it again.
        bool attackingNow=combatController!=null&&combatController.IsAttackPoseActive;
        if(attackingNow&&!grounded) glideNeedsPressAfterAttack=true;
        if(grounded||attached||IsWallSliding) glideNeedsPressAfterAttack=false;
        if(!jumpHeld || launch!=LaunchKind.None || grounded || attached || IsWallSliding || attackingNow) glideArmed=false;
        else if(glideNeedsPressAfterAttack){ if(airJumpPress && !(nearestGround<=GlideMinHeight) && !NearWall()){ glideArmed=true; glideNeedsPressAfterAttack=false; } }
        else if(!glideNeedsNewPress || airJumpPress && !(nearestGround<=GlideMinHeight) && !NearWall()) glideArmed=true;
        airJumpPress=false;
        IsGliding=glideArmed && !IsDashing && !grounded && !attached && !recovering && !IsWallSliding &&
                  launch==LaunchKind.None && velocity.y<=0f && Relics.Has(abilities,Relics.Glidecap);
        if(IsGliding)
        {
            // The canopy replaces gravity (cancelled below): a soft pull down into the drift speed,
            // or a firm brake from a faster fall.
            velocity.y=velocity.y>-glideFallSpeed ? Mathf.Max(velocity.y-GlidePull*Time.fixedDeltaTime,-glideFallSpeed)
                                                  : Mathf.MoveTowards(velocity.y,-glideFallSpeed,glideBrake*Time.fixedDeltaTime);
            velocity.x=Mathf.MoveTowards(velocity.x,moveInput*glideSpeed,airAcceleration*airControl*Time.fixedDeltaTime);
        }

        // Cutting once also handles quick taps buffered just before landing.
        if (canCutJump && !jumpHeld && velocity.y > 0f)
        {
            velocity.y *= jumpReleaseMultiplier;
            canCutJump = false;
        }
        if (velocity.y <= 0f)
            canCutJump = false;

        // A weightier fall than rise, and a brief hang at the apex while Jump is held. Left alone while
        // something else owns his vertical motion (ground, thread, wall, dash, Glidecap, an air attack's
        // own gravity) and in the step a launch sets his speed.
        bool freeFlight = !grounded && !attached && !IsWallSliding && !IsDashing && !IsGliding && launch == LaunchKind.None &&
                          (combatController == null || Mathf.Approximately(combatController.GravityMultiplier, 1f));
        if (freeFlight)
        {
            float multiplier = jumpHeld && Mathf.Abs(velocity.y) <= apexSpeedBand ? apexGravityMultiplier
                : velocity.y < 0f ? Mathf.Lerp(1f, fallGravityMultiplier, -velocity.y / Mathf.Max(.01f, apexSpeedBand))
                : 1f;
            velocity.y += Physics2D.gravity.y * body.gravityScale * (multiplier - 1f) * Time.fixedDeltaTime;
        }
        velocity.y = Mathf.Max(velocity.y, -Mathf.Max(maximumFallSpeed, plunge));
        surfaceWalking &= launch == LaunchKind.None && !attached;
        // Cancel this step's gravity while walking on the ground, so he stands still on slopes.
        if (!surfaceWalking) ridingVelocity = Vector2.zero;
        platformVelocity = ridingVelocity;
        carriedThisStep = platformVelocity * Time.fixedDeltaTime;   // measured out of ObservedVelocity next step
        // A dash and the Glidecap also hold Qori against gravity: they set his fall speed themselves.
        bool ownGravity = surfaceWalking || IsDashing || IsGliding;
        body.linearVelocity = (ownGravity ? velocity - Physics2D.gravity * body.gravityScale * Time.fixedDeltaTime : velocity) + platformVelocity;
        IsGrounded = grounded && !attached && launch == LaunchKind.None && (velocity.y <= 0.1f || surfaceWalking);
        bool wasRunning = IsRunning;
        IsRunning = IsGrounded && !recovering && runHeld && Mathf.Abs(velocity.x)>moveSpeed+.1f;
        if (IsRunning && !wasRunning) runningSince = Time.time;
        HasGroundContact = grounded && launch == LaunchKind.None && (velocity.y <= 0.1f || surfaceWalking);
        GroundNormal = HasGroundContact ? groundNormal : Vector2.up;
        GroundPoint = HasGroundContact ? groundPoint : Vector2.zero;
        GroundCollider = HasGroundContact ? groundCollider : null;
        bool approachingGround = !attached && launch == LaunchKind.None && velocity.y < -0.1f &&
            !float.IsPositiveInfinity(nearestGround);
        GroundDistance = IsGrounded || approachingGround ? nearestGround : float.PositiveInfinity;
        LandingTimeEstimate = IsGrounded ? 0f : float.PositiveInfinity;
        if (approachingGround && !IsGrounded)
        {
            // Vertical-only prediction. It is a pose hint, never a future collision guarantee.
            float downwardSpeed = -velocity.y;
            float gravity = Mathf.Max(0f, -Physics2D.gravity.y * body.gravityScale);
            LandingTimeEstimate = gravity > 0.001f
                ? 2f * nearestGround / (downwardSpeed + Mathf.Sqrt(downwardSpeed * downwardSpeed + 2f * gravity * nearestGround))
                : nearestGround / downwardSpeed;
        }
        if (launch != LaunchKind.None)
        {
            stepsSinceLaunch = 0;
            LastLaunchKind = launch;
            LastLaunchSpeed = velocity.y;
            LastLaunchTime = Time.fixedTime;
            LaunchVersion++;
        }
        // Contact resolution may zero velocity before the next fixed tick.
        previousFallSpeed = IsGrounded || launch != LaunchKind.None ? 0f : Mathf.Max(0f, -velocity.y);

        if (body.position.y < resetBelowY)
            Respawn();
    }

    bool LedgeSpaceClear(Vector2 position)
    {
        Vector2 offset=(Vector2)playerCollider.bounds.center-body.position;
        return Physics2D.OverlapBox(position+offset,(Vector2)playerCollider.bounds.size*.98f,0,groundFilter,ledgeOverlaps)==0;
    }
    bool TryGrabLedge(float direction)
    {
        Bounds bounds=playerCollider.bounds;
        Vector2 origin=new Vector2(bounds.center.x+direction*(bounds.extents.x+.09f),bounds.max.y+.22f);
        int count=Physics2D.Raycast(origin,Vector2.down,groundFilter,wallHits,.40f);
        for(int i=0;i<count;i++)
        {
            RaycastHit2D hit=wallHits[i];
            if(hit.normal.y<.95f || hit.collider.attachedRigidbody!=null)continue;
            Vector2 offset=(Vector2)bounds.center-body.position;
            Vector2 point=new Vector2(bounds.center.x+direction*(bounds.extents.x+.01f),hit.point.y);
            Vector2 stand=new Vector2(point.x+direction*(bounds.extents.x+.07f),point.y+bounds.extents.y+.035f)-offset;
            Vector2 hang=new Vector2(body.position.x,point.y-bounds.extents.y-.06f)-new Vector2(0,offset.y);
            if(!LedgeSpaceClear(stand) || !LedgeSpaceClear(hang))continue;
            int supports=Physics2D.Raycast(stand+offset,Vector2.down,groundFilter,groundHits,bounds.extents.y+.08f);
            bool supported=false;
            for(int j=0;j<supports;j++)if(groundHits[j].collider==hit.collider && groundHits[j].normal.y>.95f)supported=true;
            if(!supported)continue;
            ledgeSurface=hit.collider;ledgeSurfacePosition=hit.collider.transform.position;
            LedgePoint=point;LedgeDirection=direction;FacingDirection=direction;
            ledgeHangPosition=hang;ledgeStandPosition=stand;
            savedLedgeGravity=body.gravityScale;ledgeGravityOwned=true;body.gravityScale=0;
            IsLedgeHanging=true;IsLedgeClimbing=false;ledgeAcross=false;CancelDash();airDashUsed=airJumpUsed=false;
            IsGrounded=HasGroundContact=IsWallSliding=IsRunning=false;
            GroundDistance=LandingTimeEstimate=float.PositiveInfinity;
            lastJumpPressedTime=lastGroundedTime=float.NegativeInfinity;canCutJump=false;
            body.linearVelocity=Vector2.zero;
            return true;
        }
        return false;
    }
    bool UpdateLedge()
    {
        if(!IsLedgeHanging && !IsLedgeClimbing)return false;
        if(ledgeSurface==null || !ledgeSurface.enabled || !ledgeSurface.gameObject.activeInHierarchy ||
            Vector2.Distance(ledgeSurface.transform.position,ledgeSurfacePosition)>.01f ||
            (thread!=null && thread.IsAttached) || Time.time<hitRecoveryUntil)
        {ReleaseLedge();return false;}
        FacingDirection=LedgeDirection;
        if(ledgeVerticalInput<-.2f || moveInput*LedgeDirection<-.2f)
        {ReleaseLedge();return false;}
        if(IsLedgeHanging && (ledgeVerticalInput>.2f || Time.time-lastJumpPressedTime<=jumpBufferTime))
        {
            if(!LedgeSpaceClear(ledgeStandPosition)){ReleaseLedge();return false;}
            IsLedgeHanging=false;IsLedgeClimbing=true;lastJumpPressedTime=float.NegativeInfinity;
        }
        Vector2 target=IsLedgeHanging?ledgeHangPosition:ledgeAcross?ledgeStandPosition:new Vector2(ledgeHangPosition.x,ledgeStandPosition.y);
        Vector2 delta=target-body.position;
        if(delta.magnitude<.012f)
        {
            body.linearVelocity=Vector2.zero;
            if(IsLedgeClimbing)
            {
                if(!ledgeAcross)ledgeAcross=true;
                else ReleaseLedge();
            }
            return true;
        }
        Vector2 step=Vector2.ClampMagnitude(delta,5f*Time.fixedDeltaTime);
        int count=body.Cast(step.normalized,groundFilter,wallHits,step.magnitude+.01f);
        for(int i=0;i<count;i++)
            if(Vector2.Dot(wallHits[i].normal,step.normalized)<-.2f)
            {ReleaseLedge();body.linearVelocity=Vector2.zero;return true;}
        body.linearVelocity=step/Time.fixedDeltaTime;
        return true;
    }
    void ReleaseLedge()
    {
        if(ledgeGravityOwned && body!=null)body.gravityScale=savedLedgeGravity;
        if(IsLedgeHanging || IsLedgeClimbing)ledgeRetryAt=Time.time+.35f;
        ledgeGravityOwned=false;IsLedgeHanging=IsLedgeClimbing=false;ledgeSurface=null;
    }
    public void Respawn()
    {
        if (!isActiveAndEnabled) return;
        Teleport(respawnPosition);
        RespawnVersion++;
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.RestoreAfterRespawn();
    }

    // A resting place reached in play (a drift level's safe stone): where a fall now returns him.
    public void SetRespawn(Vector2 position) => respawnPosition = position;

    // Arrival in an area: puts Qori at `position` facing `facing`, keeping his hearts. With
    // `asRespawn` it also becomes where he returns after a fall or defeat.
    public void PlaceAt(Vector2 position, float facing, bool asRespawn)
    {
        if (asRespawn) respawnPosition = position;
        if (facing != 0f) FacingDirection = Mathf.Sign(facing);
        Teleport(position);
    }

    private void Teleport(Vector2 position)
    {
        combatImpulse=Vector2.zero;combatBounce=0;
        ReleaseLedge();
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.CancelAttack();
        if (thread != null) thread.Detach();
        body.position = position;
        // Teleports must also reset the rendered root before camera/rig LateUpdate.
        transform.position = new Vector3(position.x,position.y,transform.position.z);
        Physics2D.SyncTransforms();
        body.linearVelocity = Vector2.zero;
        ResetObservation();
        pendingLaunchSpeed = 0f;
        hasPendingKnockback = false;
        hitRecoveryUntil = 0f;
        lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        canCutJump = false;
    }

    // A moving platform's velocity, added to Qori's while he stands on it. The distance it carries
    // him isn't his own motion, so it is taken out of ObservedVelocity (no walk while riding).
    private Vector2 carriedThisStep, platformVelocity, ridingVelocity;

    private void ObserveMotion(Vector2 velocity)
    {
        Vector2 position = body.position;
        Vector2 displacement = position - observedPosition - carriedThisStep;
        // Respawn resets explicitly; this also rejects external scene/test teleports.
        float teleportDistance = Mathf.Max(2f, velocity.magnitude * Time.fixedDeltaTime * 3f);
        if (observationReady && displacement.sqrMagnitude > teleportDistance * teleportDistance)
            ResetObservation();
        else if (!observationReady)
            ObservedDisplacement = ObservedVelocity = Vector2.zero;
        else
        {
            ObservedDisplacement = displacement;
            ObservedVelocity = displacement / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        }
        observedPosition = position;
        observationReady = true;
    }

    private void ResetObservation()
    {
        ReleaseLedge();
        observedPosition = body != null ? body.position : (Vector2)transform.position;
        observationReady = false;
        ObservedDisplacement = ObservedVelocity = Vector2.zero;
        IsGrounded = false;
        IsRunning = false;
        HasGroundContact = false;
        IsWallSliding=false;WallDirection=0;wallJumpUntil=float.NegativeInfinity;wallSteeringUntil=float.NegativeInfinity;
        CancelDash();airDashUsed=airJumpUsed=false;airJumpPress=glideArmed=false;carryingMomentum=false;
        IsEvading=false;RestorePassedThrough(true);
        GroundNormal = Vector2.up;
        GroundPoint = Vector2.zero;
        GroundDistance = LandingTimeEstimate = float.PositiveInfinity;
        previousFallSpeed = 0f;
        LastLaunchKind = LaunchKind.None;
        LastLaunchSpeed = LastLandingSpeed = 0f;
        LastLaunchTime = LastLandingTime = float.NegativeInfinity;
        ResetVersion++;
    }
}

