using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Follows the rendered player position after movement and physics interpolation.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
    [Header("Movement framing")]
    [SerializeField, Min(0f)] private float horizontalLookAhead = 1.9f;
    [SerializeField, Min(0f)] private float velocityLeadSeconds = .32f;
    [SerializeField, Min(.01f)] private float lookAheadSmoothTime = .22f;
    [Tooltip("Extra downward lead while falling fast, so Qori can see where he lands.")]
    [SerializeField, Min(0f)] private float fallLookAhead = 2.2f;
    [Tooltip("Upper airborne safety margin, expressed as a share of the half-height above centre.")]
    [SerializeField, Range(.2f, 1f)] private float verticalScreenLimit = .55f;
    [Tooltip("Grounded collider feet sit this far above the bottom of the view, unless room/arena bounds prevent it.")]
    [SerializeField, Range(.1f, .25f)] private float groundedFootViewport = .15f;
    [Tooltip("Platform landings use a higher composition so lower platforms remain visible.")]
    [SerializeField, Range(.3f, .5f)] private float platformFootViewport = .4f;
    [Tooltip("An airborne player can rise to this viewport height before the camera follows upward.")]
    [SerializeField, Range(.45f, .65f)] private float airborneCentreViewport = .55f;
    [SerializeField, Min(.01f)] private float groundVerticalSmoothTime = .24f;
    [Header("Ground preview")]
    [SerializeField, Min(1f)] private float downhillProbeDistance = 6f;
    [SerializeField, Min(.1f)] private float downhillMinimumDrop = .9f;
    [SerializeField, Range(.2f,.6f)] private float maximumDownhillPreview = .58f;
    [SerializeField, Range(.2f,.6f)] private float manualLookDown = .58f;
    [SerializeField, Min(.1f)] private float previewSmoothTime = .28f;
    private InputAction lookDownAction;
    private ContactFilter2D previewGroundFilter;
    private readonly RaycastHit2D[] previewHits = new RaycastHit2D[12];
    private float downwardPreview, previewVelocity, lookDownHeldFor;
    private Vector2 previewLanding;
    private bool hasPreviewLanding;
    public float DownwardPreview => downwardPreview;
    private Camera view;
    private Vector2 lookAhead,lookVelocity;
    private float retainedHorizontalLead;
    private Transform cachedTarget;
    private PlayerMovement movement;
    private PlayerThread thread;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private float verticalAnchor;
    private int resetVersion=-1;
    private float normalSize=-1f;
    public void Configure(Transform followTarget){target=followTarget;needsSnap=true;}

    private Vector3 smoothingVelocity;
    private float verticalSmoothingVelocity;
    private float cameraZ;
    private bool needsSnap;
    private Collider2D framingSupport;
    private bool platformSupport;
    private float retainedFootViewport = .15f;
    public bool PlatformFraming => platformSupport;

    // VistaZone changes zoom after this component has followed Qori. This follow
    // already frames the enlarged view; adding an authored lift would override
    // the takeoff anchor and chase every jump. Only enforce the safety band at
    // the final zoom, with the same margin on both sides of takeoff/landing.
    public float ConstrainVistaLift(float requestedLift)
    {
        if (!isActiveAndEnabled || target == null || playerCollider == null)
            return requestedLift;
        if (view == null) view = GetComponent<Camera>();
        Bounds bounds = playerCollider.bounds;
        float highest = bounds.min.y + view.orthographicSize * (1f - 2f * .08f);
        float lowest = bounds.max.y - view.orthographicSize * PreviewScreenLimit(view.orthographicSize*2f);
        float y = Mathf.Clamp(transform.position.y, lowest, Mathf.Max(lowest, highest));
        return y - transform.position.y;
    }

    private void OnEnable()
    {
        if(lookDownAction==null)
        {
            lookDownAction=new InputAction("Look down",InputActionType.Button);
            lookDownAction.AddBinding("<Keyboard>/s");
            lookDownAction.AddBinding("<Keyboard>/downArrow");
            lookDownAction.AddBinding("<Gamepad>/dpad/down");
            lookDownAction.AddBinding("<Gamepad>/rightStick/down");
        }
        lookDownAction.Enable();
        previewGroundFilter=new ContactFilter2D();
        previewGroundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
        previewGroundFilter.useTriggers=false;
        ResetPreview();
        cameraZ = transform.position.z;
        smoothingVelocity = Vector3.zero;
        verticalSmoothingVelocity=0f;
        needsSnap = true;
        lookAhead=lookVelocity=Vector2.zero;
        retainedHorizontalLead=0f;
        framingSupport=null;
        platformSupport=false;
        retainedFootViewport=groundedFootViewport;
    }

    private void OnDisable(){lookDownAction?.Disable();ResetPreview();}
    private void OnDestroy()=>lookDownAction?.Dispose();
    private void ResetPreview(){downwardPreview=previewVelocity=lookDownHeldFor=0;hasPreviewLanding=false;}

    // A deliberate ground preview can place Qori higher while the landing enters
    // view. Ease the upper safety margin with that preview, never switch on takeoff.
    private float PreviewScreenLimit(float height)=>Mathf.Lerp(verticalScreenLimit,Mathf.Max(verticalScreenLimit,.8f),
        Mathf.Clamp01(downwardPreview/(height*.35f)));

    private float GroundPreview(Bounds bounds,Vector2 velocity,bool grounded,float height)
    {
        if(needsSnap){ResetPreview();return 0;}
        if(Time.deltaTime<=0)return downwardPreview;
        bool available=movement!=null&&movement.isActiveAndEnabled&&!GamePauseMenu.BlocksGameplayInput&&
            !VistaZone.Suspended&&CameraLockZone.Current==null&&!QoriArmory.BlocksAttackInput&&
            !movement.IsLedgeHanging&&!movement.IsLedgeClimbing&&(thread==null||!thread.IsAttached);
        float wanted=0;
        if(available)
        {
            // Cave edges and raised platforms/ledges retain their traversal frame.
            // Keep deliberate look-down and main surface-ground anticipation.
            bool automatic=!platformSupport && gameObject.scene.name!=RegionalSceneCatalog.Underground;
            if(!automatic)hasPreviewLanding=false;
            else if(grounded)hasPreviewLanding=FindLowerLanding(bounds,height,out previewLanding);
            // Carry an already-visible landing into a step off the edge. A jump
            // cancels that preview; this does not create a new airborne framing mode.
            else if(velocity.y>1f)hasPreviewLanding=false;
            if(hasPreviewLanding)
            {
                float landingCameraY=previewLanding.y+height*(.5f-.13f);
                wanted=Mathf.Clamp(verticalAnchor-landingCameraY,0,height*maximumDownhillPreview);
            }
            bool standing=grounded&&Mathf.Abs(movement.MoveInput)<.1f&&Mathf.Abs(velocity.x)<.35f;
            lookDownHeldFor=standing&&lookDownAction.IsPressed()?lookDownHeldFor+Time.deltaTime:0;
            if(lookDownHeldFor>=.18f)wanted=Mathf.Max(wanted,height*manualLookDown);
        }
        else{hasPreviewLanding=false;lookDownHeldFor=0;}
        downwardPreview=Mathf.SmoothDamp(downwardPreview,wanted,ref previewVelocity,previewSmoothTime,Mathf.Infinity,Time.deltaTime);
        return downwardPreview;
    }

    private bool FindLowerLanding(Bounds bounds,float height,out Vector2 landing)
    {
        landing=default;
        float direction=Mathf.Abs(movement.MoveInput)>.1f?Mathf.Sign(movement.MoveInput):movement.FacingDirection;
        Vector2 origin=new Vector2(bounds.center.x,bounds.min.y+.4f);
        // Faster approaches need more notice for both damping stages to finish.
        // At sprint speed look slightly beyond the current view, so the vertical
        // reveal is ready when that landing enters the forward-framed view.
        float speed=body!=null?Mathf.Abs(body.linearVelocity.x):0;
        float reach=Mathf.Min(downhillProbeDistance+speed*.6f,
            Mathf.Max(downhillProbeDistance,view.orthographicSize*view.aspect*1.6f));
        // Do not look through an uphill face or wall at an unrelated floor.
        int blockers=Physics2D.Raycast(origin,Vector2.right*direction,previewGroundFilter,previewHits,reach);
        for(int i=0;i<blockers;i++)
            if(ValidPreviewHit(previewHits[i])&&previewHits[i].normal.x*direction<-.2f)
                reach=Mathf.Min(reach,previewHits[i].distance-.1f);
        for(float distance=.75f;distance<=reach+.01f;distance+=.75f)
        {
            var at=origin+Vector2.right*(distance*direction);
            int count=Physics2D.Raycast(at,Vector2.down,previewGroundFilter,previewHits,height*1.2f);
            float nearest=float.PositiveInfinity;Vector2 point=default;
            for(int i=0;i<count;i++)
                if(ValidPreviewHit(previewHits[i])&&previewHits[i].normal.y>.65f&&previewHits[i].distance<nearest)
                {nearest=previewHits[i].distance;point=previewHits[i].point;}
            if(nearest<float.PositiveInfinity&&bounds.min.y-point.y>=downhillMinimumDrop)
            {landing=point;return true;}
        }
        return false;
    }

    private bool ValidPreviewHit(RaycastHit2D hit)=>hit.collider!=null&&hit.collider!=playerCollider&&
        hit.distance>.001f&&(hit.rigidbody==null||hit.rigidbody.bodyType!=RigidbodyType2D.Dynamic);

    private void LateUpdate()
    {
        if (target == null)
            return;
        if(cachedTarget!=target)
        {
            cachedTarget=target;movement=target.GetComponentInParent<PlayerMovement>();
            body=target.GetComponentInParent<Rigidbody2D>();thread=target.GetComponentInParent<PlayerThread>();
            playerCollider=movement!=null?movement.GetComponent<BoxCollider2D>():target.GetComponent<Collider2D>();
            needsSnap=true;
        }
        if(movement!=null && resetVersion!=movement.ResetVersion){resetVersion=movement.ResetVersion;needsSnap=true;}
        Vector2 velocity=body!=null?body.linearVelocity:Vector2.zero;
        // Retain forward framing through braking and idle. A small velocity
        // threshold ignores contact jitter; genuine reversal changes the target,
        // while the existing damping eases the camera across to the other side.
        if(Time.deltaTime>0 && Mathf.Abs(velocity.x)>.15f)
        {
            float requested=Mathf.Clamp(velocity.x*velocityLeadSeconds,-horizontalLookAhead,horizontalLookAhead);
            if(Mathf.Sign(requested)!=Mathf.Sign(retainedHorizontalLead))retainedHorizontalLead=requested;
            else if(Mathf.Abs(requested)>Mathf.Abs(retainedHorizontalLead))retainedHorizontalLead=requested;
        }
        if(view==null)view=GetComponent<Camera>();
        float height=view.orthographicSize*2f;
        Bounds playerBounds=playerCollider!=null?playerCollider.bounds:new Bounds(target.position,Vector3.zero);
        float feet=playerBounds.min.y;
        bool grounded=movement==null || movement.IsGrounded;
        if(needsSnap)retainedFootViewport=groundedFootViewport;
        if(grounded)
        {
            var support=movement!=null?movement.GroundCollider:null;
            if(support!=framingSupport || needsSnap)
            {
                framingSupport=support;
                platformSupport=CameraPlatformSurface.IsPlatform(support);
            }
            retainedFootViewport=platformSupport?platformFootViewport:groundedFootViewport;
        }
        // Retain the takeoff surface's framing in the air. Losing ground contact
        // must not switch a platform jump back to the terrain's 15% baseline.
        // The existing scene offset Y=1 is the common baseline. Retain any authored
        // adjustment relative to it while measuring the new baseline from collider feet.
        float groundedY=feet+height*(.5f-retainedFootViewport)+(offset.y-1f);
        if(needsSnap)verticalAnchor=groundedY;
        else if(grounded)
        {
            // Ignore tiny contact corrections at rest. Real slopes still drive the
            // normal damped follow; velocity-Y is not added on top of their height.
            if(Mathf.Abs(groundedY-verticalAnchor)>.025f)verticalAnchor=groundedY;
        }
        // Look down during a real descent below the retained takeoff view. Speed
        // alone also triggers on an ordinary jump's return, dipping the camera
        // just before landing. Limit the lead by the drop below that view.
        float dropBelowAnchor=Mathf.Max(0f,verticalAnchor-groundedY);
        float fallLead=velocity.y<-6f?-Mathf.Min(Mathf.Min(fallLookAhead,(-velocity.y-6f)*.16f),dropBelowAnchor):0f;
        Vector2 lead=new Vector2(retainedHorizontalLead,0f);
        if(thread!=null && thread.IsAttached)
        {
            Vector2 anchor=thread.AnchorPosition-(Vector2)target.position;
            lead+=Vector2.ClampMagnitude(anchor*.13f,.65f);
        }
        if(Time.deltaTime>0)lookAhead=Vector2.SmoothDamp(lookAhead,lead,ref lookVelocity,lookAheadSmoothTime,Mathf.Infinity,Time.deltaTime);

        if(!grounded && !needsSnap && Time.deltaTime>0)
        {
            // Keep the takeoff view while a normal jump travels through it. Longer
            // climbs move that anchor only when Qori reaches the upper tracking band.
            // The anchor then stays at that height through the apex, avoiding a
            // camera reversal with every change of vertical velocity.
            float lowest=playerBounds.center.y+height*(.5f-airborneCentreViewport)+Mathf.Max(0f,lookAhead.y);
            float highest=groundedY+fallLead;
            verticalAnchor=Mathf.Clamp(verticalAnchor,lowest,Mathf.Max(lowest,highest));
        }

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x + lookAhead.x,
            verticalAnchor-GroundPreview(playerBounds,velocity,grounded,height),
            cameraZ);

        desiredPosition=Bounded(desiredPosition,true);

        if (needsSnap)
        {
            lookAhead=lookVelocity=Vector2.zero;
            retainedHorizontalLead=0f;
            smoothingVelocity=Vector3.zero;
            verticalSmoothingVelocity=0f;
            desiredPosition=new Vector3(target.position.x+offset.x,verticalAnchor,cameraZ);
            if(body!=null && movement!=null && target==movement.transform)
                desiredPosition=new Vector3(body.position.x+offset.x,verticalAnchor+body.position.y-target.position.y,cameraZ);
            transform.position = Bounded(desiredPosition,true);
            verticalAnchor=transform.position.y;
            needsSnap = false;
            return;
        }
        if(Time.deltaTime<=0)return;

        Vector3 next = new Vector3(
            Mathf.SmoothDamp(transform.position.x,desiredPosition.x,ref smoothingVelocity.x,
                smoothTime,Mathf.Infinity,Time.deltaTime),
            transform.position.y,cameraZ);
        // A slightly slower grounded vertical response smooths slopes and landing
        // recovery independently of responsive horizontal look-ahead.
        float yVelocity=verticalSmoothingVelocity;
        next.y=Mathf.SmoothDamp(transform.position.y,desiredPosition.y,ref yVelocity,
            grounded?groundVerticalSmoothTime:smoothTime,Mathf.Infinity,Time.deltaTime);
        verticalSmoothingVelocity=yVelocity;
        // An asymmetric safety band permits low grounded composition, but keeps the
        // actual collider inside the frame during fast falls and high launches.
        float highestCamera=feet+height*(.5f-.08f);
        float lowestCamera=playerBounds.max.y-view.orthographicSize*PreviewScreenLimit(height);
        if(CameraLockZone.Current!=null){}   // a lock zone frames the space itself
        else if(next.y>highestCamera){next.y=highestCamera;verticalSmoothingVelocity=Mathf.Min(verticalSmoothingVelocity,velocity.y);}
        else if(next.y<lowestCamera){next.y=lowestCamera;verticalSmoothingVelocity=Mathf.Max(verticalSmoothingVelocity,velocity.y);}
        // The room's edges always win (a lock zone is eased into through the desired position above).
        Vector3 held=Bounded(next,false);
        if(held.x!=next.x)smoothingVelocity.x=0f;
        if(held.y!=next.y)verticalSmoothingVelocity=0f;
        transform.position = held;
    }

    // Keeps the view inside the room (CameraBounds) and, with `withLock`, inside the lock zone Qori
    // is standing in. Measured at the normal zoom: when a vista pulls the view out, it may show past
    // the edges. An axis is left free if Qori is far outside the room (a test or debug teleport).
    Vector3 Bounded(Vector3 p,bool withLock)
    {
        if(view==null)view=GetComponent<Camera>();
        if(normalSize<0f)normalSize=view.orthographicSize;
        float halfH=Mathf.Min(view.orthographicSize,normalSize),halfW=halfH*view.aspect;
        Vector2 c=p;
        var zone=withLock?CameraLockZone.Current:null;
        if(zone!=null)c=CameraBounds.Clamp(c,halfW,halfH,zone.view,true);
        if(CameraBounds.TryGetRoom(out Rect room,out bool hasTop))
        {
            Vector2 at=target.position;
            Vector2 bounded=CameraBounds.Clamp(c,halfW,halfH,room,hasTop);
            if(at.x>room.xMin-3f&&at.x<room.xMax+3f)c.x=bounded.x;
            if(at.y>room.yMin-3f)c.y=bounded.y;
        }
        return new Vector3(c.x,c.y,p.z);
    }
}
