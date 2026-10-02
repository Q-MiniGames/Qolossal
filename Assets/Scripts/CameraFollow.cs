using UnityEngine;

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
    [Tooltip("The player never drifts further from the screen centre than this share of the half-height.")]
    [SerializeField, Range(.2f, 1f)] private float verticalScreenLimit = .55f;
    private Camera view;
    private Vector2 lookAhead,lookVelocity;
    private float retainedHorizontalLead;
    private Transform cachedTarget;
    private PlayerMovement movement;
    private PlayerThread thread;
    private Rigidbody2D body;
    private int resetVersion=-1;
    private float normalSize=-1f;
    public void Configure(Transform followTarget){target=followTarget;needsSnap=true;}

    private Vector3 smoothingVelocity;
    private float cameraZ;
    private bool needsSnap;

    private void OnEnable()
    {
        cameraZ = transform.position.z;
        smoothingVelocity = Vector3.zero;
        needsSnap = true;
        lookAhead=lookVelocity=Vector2.zero;
        retainedHorizontalLead=0f;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;
        if(cachedTarget!=target)
        {
            cachedTarget=target;movement=target.GetComponentInParent<PlayerMovement>();
            body=target.GetComponentInParent<Rigidbody2D>();thread=target.GetComponentInParent<PlayerThread>();
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
        // Falling: lead further down as speed builds (from ~6 u/s), up to fallLookAhead.
        float fallLead=velocity.y<-6f?-Mathf.Min(fallLookAhead,(-velocity.y-6f)*.16f):0f;
        Vector2 lead=new Vector2(retainedHorizontalLead,Mathf.Clamp(velocity.y*.055f,-.6f,.8f)+fallLead);
        if(thread!=null && thread.IsAttached)
        {
            Vector2 anchor=thread.AnchorPosition-(Vector2)target.position;
            lead+=Vector2.ClampMagnitude(anchor*.13f,.65f);
        }
        if(Time.deltaTime>0)lookAhead=Vector2.SmoothDamp(lookAhead,lead,ref lookVelocity,lookAheadSmoothTime,Mathf.Infinity,Time.deltaTime);

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x + lookAhead.x,
            target.position.y + offset.y + lookAhead.y,
            cameraZ);

        desiredPosition=Bounded(desiredPosition,true);

        if (needsSnap)
        {
            lookAhead=lookVelocity=Vector2.zero;
            retainedHorizontalLead=0f;
            smoothingVelocity=Vector3.zero;
            desiredPosition=new Vector3(target.position.x+offset.x,target.position.y+offset.y,cameraZ);
            if(body!=null && movement!=null && target==movement.transform)
                desiredPosition=new Vector3(body.position.x+offset.x,body.position.y+offset.y,cameraZ);
            transform.position = Bounded(desiredPosition,true);
            needsSnap = false;
            return;
        }
        if(Time.deltaTime<=0)return;

        Vector3 next = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref smoothingVelocity,
            smoothTime, Mathf.Infinity, Time.deltaTime);
        // Smoothing lags a fast fall; never let Qori leave the middle band of the screen.
        if(view==null)view=GetComponent<Camera>();
        float band=view.orthographicSize*verticalScreenLimit;
        float playerY=target.position.y;
        if(CameraLockZone.Current!=null){}   // a lock zone frames the space itself
        else if(next.y-playerY>band){next.y=playerY+band;smoothingVelocity.y=Mathf.Min(smoothingVelocity.y,velocity.y);}
        else if(playerY-next.y>band){next.y=playerY-band;smoothingVelocity.y=Mathf.Max(smoothingVelocity.y,velocity.y);}
        // The room's edges always win (a lock zone is eased into through the desired position above).
        Vector3 held=Bounded(next,false);
        if(held.x!=next.x)smoothingVelocity.x=0f;
        if(held.y!=next.y)smoothingVelocity.y=0f;
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
