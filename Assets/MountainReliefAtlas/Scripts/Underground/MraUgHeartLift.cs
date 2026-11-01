using UnityEngine;

// The heart's lift between two sectors that share a wall (A below B, C below D). Its hatch through
// the dividing rock stays shut, and the cabin can't be worked, until BOTH sectors have been reached
// on their own (each sector's visit flag, saved on a completed arrival there) AND the latch at one
// of its stations has been set (`linkFlag`). A lift never gives first access to a sector.
// The cabin rides between its two stops by velocity (it carries Qori); while it is away from the
// upper stop a guard plate covers the hatch, so the upper sector never has an open drop. Going down
// it stops and returns if Qori is under it, so it never crushes him. A scene loads with the cabin
// at rest at the lower stop. Stations call it (MraUgLiftStation).
public sealed class MraUgHeartLift : MonoBehaviour
{
    public string[] visitFlags = new string[0];
    public string linkFlag;
    public Rigidbody2D cabin;
    [Tooltip("The cabin's centre at its lower and upper stop.")] public Vector2 lower, upper;
    [Min(.1f)] public float speed = 2f;
    [Tooltip("Solid while the lift is sealed: the rock plug in the hatch.")] public Collider2D hatch;
    public SpriteRenderer[] hatchArt = new SpriteRenderer[0];
    [Tooltip("Shown only while the hatch is open (the painted open frame).")] public SpriteRenderer[] hatchOpenArt = new SpriteRenderer[0];
    [Tooltip("The plate over the hatch's top while the cabin isn't there.")] public Collider2D guard;
    public SpriteRenderer guardArt;

    float pos;          // 0 at the lower stop, 1 at the upper
    float target;       // where it's going
    // A plain call lift (a skill cave's lift to its launch) has no flags: never sealed.
    public bool Sealed => !MraState.Holds(visitFlags) || !string.IsNullOrEmpty(linkFlag) && !GameSave.HasFlag(linkFlag);
    public bool BothVisited => MraState.Holds(visitFlags);
    public bool Latched => string.IsNullOrEmpty(linkFlag) || GameSave.HasFlag(linkFlag);
    public bool Moving => !Mathf.Approximately(pos, target);
    public bool AtUpper => !Moving && pos > .5f;
    public bool AtLower => !Moving && pos < .5f;
    public float Position => pos;

    void Start()
    {
        cabin.bodyType = RigidbodyType2D.Kinematic; cabin.interpolation = RigidbodyInterpolation2D.Interpolate;
        pos = target = 0f;
        cabin.position = lower; cabin.transform.position = lower;
        Refresh();
    }

    void OnEnable() { GameSave.FlagsChanged += Refresh; }
    void OnDisable() { GameSave.FlagsChanged -= Refresh; }

    void Refresh()
    {
        if (this == null) return;
        bool closed = Sealed;
        if (hatch != null) hatch.enabled = closed;
        foreach (var a in hatchArt) if (a != null) a.enabled = closed;
        foreach (var a in hatchOpenArt) if (a != null) a.enabled = !closed;
        bool cover = !closed && !AtUpper && !Moving;
        if (guard != null) guard.enabled = cover;
        if (guardArt != null) guardArt.enabled = cover;
    }

    // A station's call: comes to `toUpper`'s stop, or, already there, goes to the other one.
    public bool Call(bool toUpper)
    {
        if (Sealed || Moving) return false;
        float stop = toUpper ? 1f : 0f;
        target = Mathf.Approximately(pos, stop) ? 1f - stop : stop;
        Sfx.Play("Lever_Pull", cabin.position);
        Refresh();
        return true;
    }

    void FixedUpdate()
    {
        if (!Moving) { cabin.linearVelocity = Vector2.zero; return; }
        float travel = Vector2.Distance(lower, upper);
        float step = speed * Time.fixedDeltaTime / Mathf.Max(.01f, travel);
        // Going down: never onto Qori. Someone under the cabin sends it back up.
        if (target < pos && SomeoneBelow()) { target = 1f; Sfx.Play("Gate_Close", cabin.position); }
        pos = Mathf.MoveTowards(pos, target, step);
        if (Mathf.Approximately(pos, target)) pos = target;   // exactly at the stop, never a hair short
        Vector2 next = Vector2.Lerp(lower, upper, pos);
        cabin.linearVelocity = (next - cabin.position) / Time.fixedDeltaTime;
        if (!Moving) { Refresh(); Sfx.Play("Gate_Open", next); }
        else if (guard != null && guard.enabled) Refresh();
    }

    bool SomeoneBelow()
    {
        var q = FindAnyObjectByType<PlayerMovement>(); if (q == null) return false;
        var qBox = q.GetComponent<BoxCollider2D>(); var cBox = cabin.GetComponent<Collider2D>();
        if (qBox == null || cBox == null) return false;
        Bounds c = cBox.bounds, b = qBox.bounds;
        bool underneath = b.max.y <= c.min.y + .05f && b.max.y > c.min.y - 1.2f;   // within the next stretch below the deck
        return underneath && b.max.x > c.min.x + .05f && b.min.x < c.max.x - .05f;
    }
}
