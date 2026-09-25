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

        if (needsSnap)
        {
            lookAhead=lookVelocity=Vector2.zero;
            retainedHorizontalLead=0f;
            smoothingVelocity=Vector3.zero;
            desiredPosition=new Vector3(target.position.x+offset.x,target.position.y+offset.y,cameraZ);
            if(body!=null && movement!=null && target==movement.transform)
                desiredPosition=new Vector3(body.position.x+offset.x,body.position.y+offset.y,cameraZ);
            transform.position = desiredPosition;
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
        if(next.y-playerY>band){next.y=playerY+band;smoothingVelocity.y=Mathf.Min(smoothingVelocity.y,velocity.y);}
        else if(playerY-next.y>band){next.y=playerY-band;smoothingVelocity.y=Mathf.Max(smoothingVelocity.y,velocity.y);}
        transform.position = next;
    }
}
