using UnityEngine;
using UnityEngine.InputSystem;

// First movement prototype. This is the sole writer of player velocity.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    public enum LaunchKind { None, Jump, FlowerBoost, WallJump }
    public bool IsWallSliding { get; private set; }
    public float WallDirection { get; private set; }
    [Header("Wall movement")]
    [SerializeField, Min(.1f)] private float wallSlideSpeed=2.5f;
    [SerializeField, Min(.1f)] private float wallJumpHorizontalSpeed=8f;
    [SerializeField, Min(.01f)] private float wallJumpPushSeconds=.12f;
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
    public bool IsRunning { get; private set; }
    public float WalkSpeed => moveSpeed;
    public float RunSpeed => Mathf.Max(moveSpeed,runSpeed);
    public Vector2 ObservedDisplacement { get; private set; }
    public Vector2 ObservedVelocity { get; private set; }
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
    [Header("Running")]
    [SerializeField, Min(0f)] private float moveSpeed = 7f;
    [SerializeField, Min(0f)] private float runSpeed = 10.5f;
    [SerializeField, Min(0f)] private float groundAcceleration = 65f;
    [SerializeField, Min(0f)] private float airAcceleration = 35f;

    [Header("Jumping")]
    [SerializeField, Min(0f)] private float jumpSpeed = 12f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float jumpReleaseMultiplier = 0.5f;
    [SerializeField, Min(1f)] private float maximumFallSpeed = 22f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.06f;
    [Tooltip("Downward look-ahead for landing presentation; does not extend grounded or coyote detection.")]
    [SerializeField, Min(0f)] private float landingLookAheadDistance = 1.5f;

    [Header("Test Room")]
    [Tooltip("Falling below this height returns the player to their latest checkpoint, or the starting position.")]
    [SerializeField] private float resetBelowY = -15f;
    private const float GroundSnapDistance = .3f;
    private const float MaxSlopeRise = 1.2f;   // tan of ~50 degrees, the steepest walkable surface
    private const int LaunchGraceSteps = 8;   // physics steps after a jump/boost before ground can catch Qori
    private int stepsSinceLaunch = LaunchGraceSteps + 1;

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private PhysicsMaterial2D originalMaterial;
    private PhysicsMaterial2D slidingMaterial;
    private PlayerThread thread;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction growthAction;
    private InputAction runAction;
    private bool runHeld;
    private float pendingLaunchSpeed;
    private Vector2 pendingKnockback;
    private bool hasPendingKnockback;
    private Vector2 combatImpulse;
    private float combatBounce;
    private PlayerCombat combatController;
    public void AddCombatImpulse(Vector2 impulse)=>combatImpulse+=impulse;
    public void QueueCombatBounce(float speed)=>combatBounce=Mathf.Max(combatBounce,speed);
    private float hitRecoveryUntil;
    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Vector2 startingPosition;
    private Vector2 respawnPosition;
    private Checkpoint activeCheckpoint;
    private string CheckpointSaveKey => "Qolossal.Checkpoint.v1." + gameObject.scene.path;
    public static void ClearSavedCheckpoint(string scenePath)
    {
        PlayerPrefs.DeleteKey("Qolossal.Checkpoint.v1." + scenePath);
        PlayerPrefs.Save();
    }
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
        runAction.AddBinding("<Keyboard>/leftShift");
        runAction.AddBinding("<Keyboard>/rightShift");
        runAction.AddBinding("<Gamepad>/leftStickPress");
        ledgeVerticalAction=new InputAction("Ledge vertical",InputActionType.Value);
        ledgeVerticalAction.AddCompositeBinding("1DAxis").With("Positive","<Keyboard>/w").With("Positive","<Keyboard>/upArrow").With("Negative","<Keyboard>/s").With("Negative","<Keyboard>/downArrow");
        ledgeVerticalAction.AddBinding("<Gamepad>/leftStick/y").WithProcessor("axisDeadzone");
        ledgeVerticalAction.AddBinding("<Gamepad>/dpad/y");
    }

    private void Start()
    {
        string savedId = PlayerPrefs.GetString(CheckpointSaveKey, "");
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
        ledgeVerticalAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        growthAction.Disable();
        runAction.Disable();
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
        lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        ResetObservation();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
        growthAction?.Dispose();
        runAction?.Dispose();
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
            lastJumpPressedTime = float.NegativeInfinity;
            ledgeVerticalInput=0;
            return;
        }
        moveInput = moveAction.ReadValue<float>();
        runHeld = runAction.IsPressed();
        ledgeVerticalInput=ledgeVerticalAction.ReadValue<float>();
        if (Mathf.Abs(moveInput) > 0.1f && Time.time>=wallSteeringUntil)
            FacingDirection = Mathf.Sign(moveInput);
        jumpHeld = jumpAction.IsPressed();
        if (jumpAction.WasPressedThisFrame())
        {
            // One press chooses a flower boost or a normal buffered jump.
            // LaunchUp queues its impulse for the next physics step.
            if (!GrowthFlower.TryActivateClosest(this))
                lastJumpPressedTime = Time.time;
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
        activeCheckpoint = checkpoint;
        respawnPosition = position;
        checkpoint.SetActiveMarker(true);
        // Checkpoint activation is the only save event; enemies and health
        // reset on a fresh session. The scene object name is never persisted.
        if (!string.IsNullOrEmpty(checkpoint.CheckpointId))
        {
            PlayerPrefs.SetString(CheckpointSaveKey, checkpoint.CheckpointId);
            PlayerPrefs.Save();
        }
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
        lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        canCutJump = false;
    }

    private void FixedUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        // An impulse this step (hit, bounce, knockback, flower boost) must leave the ground freely.
        bool impulse = combatImpulse.y > 0f || combatBounce > 0f || hasPendingKnockback || pendingLaunchSpeed > 0f;
        velocity+=combatImpulse;combatImpulse=Vector2.zero;
        if(combatBounce>0){velocity.y=Mathf.Max(velocity.y,combatBounce);combatBounce=0;canCutJump=false;}
        float groundControl=combatController!=null?combatController.GroundControl:1;
        float airControl=combatController!=null?combatController.AirControl:1;
        if(combatController!=null)velocity+=Physics2D.gravity*body.gravityScale*(combatController.GravityMultiplier-1)*Time.fixedDeltaTime;
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

        bool combatAllowsJump=combatController==null||combatController.CanCancel(CombatCancel.Jump);
        if (combatAllowsJump && Time.time - lastJumpPressedTime <= jumpBufferTime &&
            Time.time - lastGroundedTime <= coyoteTime)
        {
            velocity.y = jumpSpeed;
            lastJumpPressedTime = lastGroundedTime = float.NegativeInfinity;
            canCutJump = true;
            launch = LaunchKind.Jump;
        }
        else if(combatAllowsJump && !recovering && !attached && launch==LaunchKind.None && WallDirection!=0 && Time.time-lastJumpPressedTime<=jumpBufferTime)
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
        if(WallDirection!=0 && launch==LaunchKind.None && velocity.y<0 && moveInput*WallDirection>=-.1f)
        {
            velocity.y=Mathf.Max(velocity.y,-wallSlideSpeed);
            IsWallSliding=true;
            FacingDirection=WallDirection;
        }

        // Cutting once also handles quick taps buffered just before landing.
        if (canCutJump && !jumpHeld && velocity.y > 0f)
        {
            velocity.y *= jumpReleaseMultiplier;
            canCutJump = false;
        }
        if (velocity.y <= 0f)
            canCutJump = false;

        velocity.y = Mathf.Max(velocity.y, -maximumFallSpeed);
        surfaceWalking &= launch == LaunchKind.None && !attached;
        // Cancel this step's gravity while walking on the ground, so he stands still on slopes.
        body.linearVelocity = surfaceWalking ? velocity - Physics2D.gravity * body.gravityScale * Time.fixedDeltaTime : velocity;
        IsGrounded = grounded && !attached && launch == LaunchKind.None && (velocity.y <= 0.1f || surfaceWalking);
        IsRunning = IsGrounded && !recovering && runHeld && Mathf.Abs(velocity.x)>moveSpeed+.1f;
        HasGroundContact = grounded && launch == LaunchKind.None && (velocity.y <= 0.1f || surfaceWalking);
        GroundNormal = HasGroundContact ? groundNormal : Vector2.up;
        GroundPoint = HasGroundContact ? groundPoint : Vector2.zero;
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
            IsLedgeHanging=true;IsLedgeClimbing=false;ledgeAcross=false;
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
        combatImpulse=Vector2.zero;combatBounce=0;
        ReleaseLedge();
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.CancelAttack();
        if (thread != null) thread.Detach();
        body.position = respawnPosition;
        // Teleports must also reset the rendered root before camera/rig LateUpdate.
        transform.position = new Vector3(respawnPosition.x,respawnPosition.y,transform.position.z);
        Physics2D.SyncTransforms();
        body.linearVelocity = Vector2.zero;
        ResetObservation();
        pendingLaunchSpeed = 0f;
        hasPendingKnockback = false;
        hitRecoveryUntil = 0f;
        lastGroundedTime = lastJumpPressedTime = float.NegativeInfinity;
        canCutJump = false;
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.RestoreAfterRespawn();
    }

    private void ObserveMotion(Vector2 velocity)
    {
        Vector2 position = body.position;
        Vector2 displacement = position - observedPosition;
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

