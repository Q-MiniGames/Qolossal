using UnityEngine;
using UnityEngine.InputSystem;

// Owns a rope joint; PlayerMovement remains the only script writing velocity.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement))]
public sealed class PlayerThread : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float attachRange = 9f;
    [SerializeField, Min(0.1f)] private float pullSpeed = 4f;
    [SerializeField, Min(0.1f)] private float minimumLength = 0.8f;
    [SerializeField, Min(0f)] private float swingAcceleration = 14f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Rope Attachment Artwork")]
    [SerializeField] private Sprite claspSprite;
    [SerializeField, Min(0.001f)] private float claspScale = 0.055f;
    private SpriteRenderer clasp;
    private Vector3 animatedTip;
    private QoriVisual qoriVisual;
    private QoriAnimator qoriRig;

    [Header("Thread Animation")]
    [SerializeField] private Material ropeMaterial;
    [SerializeField, Min(0.01f)] private float artworkWidth = 0.065f;
    [SerializeField, Min(0.01f)] private float artworkRepeatLength = 1.6f;
    [SerializeField, Min(0.01f)] private float shootSeconds = 0.1f;
    [SerializeField, Min(0.01f)] private float retractSeconds = 0.16f;
    [SerializeField, Min(0f)] private float maximumSag = 0.55f;
    private float attachedAt;
    private float releasedAt = float.NegativeInfinity;
    private Vector3 releasedTip;
    private const int ThreadPoints = 25;
    private readonly Vector3[] threadPoints = new Vector3[ThreadPoints];

    public bool IsAttached => joint != null && joint.enabled && attachedAnchor != null
        && attachedAnchor.isActiveAndEnabled;
    public float SwingAcceleration => swingAcceleration;
    public int AttachmentVersion { get; private set; }
    public int ReleaseVersion { get; private set; }
    public float AttachedAt => attachedAt;
    public float ReleasedAt => releasedAt;
    public float CurrentLength => IsAttached ? joint.distance : 0f;
    public float Slack => IsAttached ? Mathf.Max(0f, joint.distance - Vector2.Distance(body.position, joint.connectedAnchor)) : 0f;
    public bool IsTaut => IsAttached && Slack <= 0.08f;
    public Vector2 LastReleaseVelocity { get; private set; }
    public bool HasTarget => selectedAnchor != null && selectedAnchor.isActiveAndEnabled;
    public Vector2 SelectedAnchorPosition => HasTarget ? (Vector2)selectedAnchor.transform.position : (Vector2)transform.position;

    private Rigidbody2D body;
    private DistanceJoint2D joint;
    private LineRenderer line;
    private Material lineMaterial;
    private ThreadAnchor selectedAnchor;
    private ThreadAnchor attachedAnchor;
    public Vector2 AnchorPosition => IsAttached ? (Vector2)attachedAnchor.transform.position : (Vector2)transform.position;
    private bool pulling;
    private bool extending;
    private InputAction hookAction;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[1];
    private readonly RaycastHit2D[] pullHits = new RaycastHit2D[16];
    private const float CollisionSkin = 0.03f;

    private void Reset() => obstacleLayers = LayerMask.GetMask("Ground");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        qoriVisual = GetComponentInChildren<QoriVisual>();
        hookAction = new InputAction("Hook", InputActionType.Button);

        hookAction.AddBinding("<Mouse>/rightButton");
        hookAction.AddBinding("<Gamepad>/rightTrigger");   // casts; the left trigger pulls (below)

        if (obstacleLayers.value == 0)
            obstacleLayers = LayerMask.GetMask("Ground");
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;

        // Runtime-owned components require no manual joint/line setup.
        joint = gameObject.AddComponent<DistanceJoint2D>();
        joint.enabled = false;
        // A world-connected joint otherwise suppresses collisions with ALL
        // colliders that have no Rigidbody2D (including our ground/platforms).
        joint.enableCollision = true;
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;
        joint.anchor = Vector2.zero;
        joint.maxDistanceOnly = true;

        GameObject visual = new GameObject("Living Thread Visual");
        visual.transform.SetParent(transform, false);
        line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = ThreadPoints;
        line.startWidth = line.endWidth = 0.035f;
        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        line.sortingOrder = 10;
        line.startColor = line.endColor = new Color(0.55f, 0.85f, 0.8f);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            lineMaterial = new Material(shader);
            line.sharedMaterial = lineMaterial;
        }
        line.enabled = false;
        GameObject claspObject = new GameObject("Living Thread Clasp");
        // Keep the clasp outside the player's non-uniform scale to prevent shear.
        claspObject.transform.SetParent(null);
        clasp = claspObject.AddComponent<SpriteRenderer>();
        clasp.sprite = claspSprite;
        clasp.sortingOrder = 11;
        clasp.enabled = false;
        if (ropeMaterial != null)
        {
            if (lineMaterial != null) Destroy(lineMaterial);
            lineMaterial = new Material(ropeMaterial);
            line.sharedMaterial = lineMaterial;
            line.startColor = line.endColor = Color.white;
            line.startWidth = line.endWidth = artworkWidth;
            line.textureMode = LineTextureMode.Tile;
            line.textureScale = new Vector2(1f / artworkRepeatLength, 1f);
        }
    }

    private void OnEnable() => hookAction.Enable();

    // The thread is the Living Thread relic's ability.
    private PlayerAbilityController abilities;
    public bool Unlocked => Relics.Has(abilities != null ? abilities : abilities = GetComponent<PlayerAbilityController>(), Relics.LivingThread);

    private void Update()
    {
        if (GamePauseMenu.BlocksGameplayInput)
        {
            pulling = extending = false;
            return;
        }
        Gamepad pad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;
        pulling = (keyboard != null && keyboard.wKey.isPressed) ||
            (pad != null && pad.leftTrigger.isPressed);
        extending = (keyboard != null && keyboard.sKey.isPressed) ||
            (pad != null && pad.dpad.down.isPressed);

        if (attachedAnchor != null && !attachedAnchor.isActiveAndEnabled) Detach();
        if (!Unlocked) { SelectAnchor(null); return; }
        SelectAnchor(FindAnchor());
        if (hookAction.WasPressedThisFrame()) ToggleHook();
    }

    // A future on-screen touch button can invoke the same action.
    public void ToggleHook()
    {
        if (!isActiveAndEnabled || GamePauseMenu.BlocksGameplayInput || !Unlocked) return;
        if (IsAttached)
        {
            Detach();
            SelectAnchor(FindAnchor());
            return;
        }
        SelectAnchor(FindAnchor());
        if (selectedAnchor == null) return;
        var combat=GetComponent<PlayerCombat>();
        if(combat!=null&&!combat.TryCancel(CombatCancel.Grapple))return;
        attachedAnchor = selectedAnchor;
        joint.connectedAnchor = attachedAnchor.transform.position;
        joint.distance = Mathf.Max(minimumLength,
            Vector2.Distance(body.position, joint.connectedAnchor));
        joint.enabled = true;
        attachedAt = Time.time;
        AttachmentVersion++;
        releasedAt = float.NegativeInfinity;
    }

    private ThreadAnchor FindAnchor()
    {
        if (IsAttached) return attachedAnchor;
        ThreadAnchor best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (ThreadAnchor anchor in ThreadAnchor.Active)
        {
            if (anchor == null || !anchor.isActiveAndEnabled) continue;
            float distance = Vector2.Distance(anchor.transform.position, body.position);
            if (distance > attachRange || distance < minimumLength || IsBlocked(anchor)) continue;
            // Deterministic tie-breaking prevents equal-distance targets flickering.
            if (distance < bestDistance ||
                (distance == bestDistance && best != null && anchor.GetInstanceID() < best.GetInstanceID()))
            {
                bestDistance = distance;
                best = anchor;
            }
        }
        return best;
    }

    private bool IsBlocked(ThreadAnchor anchor)
    {
        return Physics2D.Linecast(body.position, anchor.transform.position,
            obstacleFilter, obstacleHits) > 0;
    }

    private void SelectAnchor(ThreadAnchor next)
    {
        if (selectedAnchor == next) return;
        if (selectedAnchor != null) selectedAnchor.SetHighlighted(false);
        selectedAnchor = next;
        if (selectedAnchor != null) selectedAnchor.SetHighlighted(true);
    }

    private void FixedUpdate()
    {
        // Obstacles prevent initial attachment, but do not break an existing
        // thread when the player swings past a platform. Rope wrapping is not
        // implemented in this prototype; visual slack does not affect the joint.
        if (!IsAttached || !attachedAnchor.isActiveAndEnabled)
        {
            Detach();
            return;
        }
        joint.connectedAnchor = attachedAnchor.transform.position;
        float requestedLength = joint.distance;
        // Opposing inputs cancel. Letting rope out adds slack; gravity and
        // momentum move the player rather than pushing through colliders.
        if (pulling && !extending)
            requestedLength = Mathf.MoveTowards(joint.distance, minimumLength,
                pullSpeed * Time.fixedDeltaTime);
        else if (extending && !pulling)
            requestedLength = Mathf.MoveTowards(joint.distance,
                Mathf.Max(joint.distance, attachRange, minimumLength),
                pullSpeed * Time.fixedDeltaTime);

        Vector2 toAnchor = joint.connectedAnchor - body.position;
        float currentDistance = toAnchor.magnitude;
        float correction = Mathf.Max(0f, currentDistance - requestedLength);
        if (correction > 0f && currentDistance > 0.001f)
        {
            // Sweep the whole player, not just the thread. Never shorten the
            // joint to a length that requires the player to enter solid ground.
            // Also relieve an existing constraint if a collision blocks it.
            Vector2 direction = toAnchor / currentDistance;
            float allowedTravel = correction;
            int count = body.Cast(direction, obstacleFilter, pullHits,
                correction + CollisionSkin);
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(pullHits[i].normal, direction) < -0.001f)
                    allowedTravel = Mathf.Min(allowedTravel,
                        Mathf.Max(0f, pullHits[i].distance - CollisionSkin));
            }
            requestedLength = Mathf.Max(requestedLength, currentDistance - allowedTravel);
        }
        joint.distance = requestedLength;
    }

    private void LateUpdate()
    {
        if (qoriRig == null) qoriRig = GetComponentInChildren<QoriAnimator>();
        Vector3 start = qoriRig != null && qoriRig.Ready ? qoriRig.RopeOrigin
            : qoriVisual != null && qoriVisual.isActiveAndEnabled ? qoriVisual.RopeOrigin : transform.position;
        clasp.enabled = false;
        Vector3 end;
        float sag = 0f;
        float ripple = 0f;
        if (IsAttached)
        {
            float progress = Mathf.Clamp01((Time.time - attachedAt) / shootSeconds);
            end = Vector3.Lerp(start, attachedAnchor.transform.position, progress);
            float distance = Vector2.Distance(body.position, joint.connectedAnchor);
            float slack = Mathf.Max(0f, joint.distance - distance - 0.03f);
            sag = Mathf.Min(maximumSag, slack * 0.6f) * progress;
            ripple = (1f - progress) * 0.09f;
        }
        else
        {
            float progress = (Time.time - releasedAt) / retractSeconds;
            if (progress >= 1f)
            {
                line.enabled = false;
                return;
            }
            end = Vector3.Lerp(releasedTip, start, Mathf.SmoothStep(0f, 1f, progress));
            ripple = 0.08f * (1f - progress);
        }
        line.enabled = true;
        Vector3 delta = end - start;
        Vector3 side = new Vector3(-delta.y, delta.x, 0f).normalized;
        for (int i = 0; i < ThreadPoints; i++)
        {
            float t = (float)i / (ThreadPoints - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            threadPoints[i] = Vector3.Lerp(start, end, t) + Vector3.down * (sag * envelope)
                + side * (Mathf.Sin(t * Mathf.PI * 4f - Time.time * 24f) * ripple * envelope);
        }
        // Preserve exact endpoints despite floating-point sine error.
        threadPoints[0] = start;
        threadPoints[ThreadPoints - 1] = end;
        animatedTip = end;
        if (claspSprite != null)
        {
            float size = Mathf.Min(claspScale, Vector3.Distance(start, end) / 8f);
            Vector3 tangent = (end - threadPoints[ThreadPoints - 2]).normalized;
            if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.up;
            Quaternion rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f);
            clasp.enabled = size > 0.001f;
            clasp.transform.localScale = Vector3.one * size;
            clasp.transform.SetPositionAndRotation(end - tangent * (0.8f * size), rotation);
            // The stem bottom sits 5.8 sprite units below the seed socket.
            // Bend the final section into that stem, so the two pieces meet.
            Vector3 stem = end - tangent * (5.8f * size);
            for (int i = 1; i < ThreadPoints; i++)
            {
                float t = (float)i / (ThreadPoints - 1);
                threadPoints[i] += (stem - end) * t * t;
            }
            threadPoints[ThreadPoints - 1] = stem;
        }
        line.SetPositions(threadPoints);
    }

    public void Detach()
    {
        // Repeated physics cleanup must not restart the release animation.
        if (joint != null && joint.enabled)
        {
            LastReleaseVelocity = body != null ? body.linearVelocity : Vector2.zero;
            ReleaseVersion++;
            releasedTip = line != null && line.enabled
                ? animatedTip
                : attachedAnchor != null ? attachedAnchor.transform.position : transform.position;
            releasedAt = Time.time;
        }
        if (joint != null) joint.enabled = false;
        attachedAnchor = null;
        if (clasp != null) clasp.enabled = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            Detach();
            releasedAt = float.NegativeInfinity;
            if (line != null) line.enabled = false;
            if (clasp != null) clasp.enabled = false;
        }
    }

    private void OnDisable()
    {
        hookAction?.Disable();
        pulling = extending = false;
        Detach();
        releasedAt = float.NegativeInfinity;
        if (line != null) line.enabled = false;
        SelectAnchor(null);
    }

    private void OnDestroy()
    {
        hookAction?.Dispose();
        if (joint != null) Destroy(joint);
        if (line != null) Destroy(line.gameObject);
        if (lineMaterial != null) Destroy(lineMaterial);
        if (clasp != null) Destroy(clasp.gameObject);
    }
}
