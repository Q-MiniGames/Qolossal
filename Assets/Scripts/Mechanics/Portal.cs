using UnityEngine;
using UnityEngine.InputSystem;

// A Vein Gate (the area portal): a rooted arch with a swirling membrane. The membrane slowly
// turns and breathes, and brightens while Qori stands in it. Pressing up (W, the up arrow, or up
// on a gamepad) there takes him to the matching portal (destinationPortal) in the destination
// scene. Where a vein leads is unknown until it has been travelled once (the saved game keeps the
// veins travelled). A Wild Vein flickers, and throws Qori somewhere random each time: a charted
// Waymark or a vein end he hasn't used (WorldAtlas). When its region's knot wakes it settles
// into a fixed shortcut (settledScene and settledArrival, a portal or Waymark id).
[DisallowMultipleComponent]
public sealed class Portal : MonoBehaviour
{
    public SpriteRenderer membrane;
    [Tooltip("This portal's id; a portal elsewhere names it as its destinationPortal.")] public string portalId = "a0-east";
    [Tooltip("Scene name to load, e.g. A1_Aqueduct.")] public string destinationScene = "";
    public string destinationPortal = "";
    [Tooltip("Where it leads, shown in the prompt, e.g. \"A1 Aqueduct Ravine\".")] public string destinationName = "";
    [Tooltip("Side Qori steps out on when he arrives here: -1 left, 1 right.")] public float exitSide = -1f;

    public enum Vein { Fixed, Wild }
    [Header("Vein")] public Vein vein = Vein.Fixed;
    [Tooltip("Wild Veins: the knot (Knots id) that settles it into a fixed shortcut.")] public string settlesWith = "";
    public string settledScene = "", settledArrival = "", settledName = "";
    [Tooltip("Vein art (Codex K-04): the calm and the broken membrane; a Wild Vein shows the broken one until it settles.")]
    public Sprite membraneStable, membraneWild;

    public bool IsWild => vein == Vein.Wild && !GameSave.IsKnotAwake(settlesWith);
    bool Settled => vein == Vein.Wild && !IsWild;
    string Scene => Settled ? settledScene : destinationScene;
    string Arrival => Settled ? settledArrival : destinationPortal;
    string Where => Settled ? settledName : !string.IsNullOrEmpty(destinationName) ? destinationName : destinationScene;
    // Whether Qori has travelled this vein (a Wild Vein never is, until it settles).
    public bool Known => !IsWild && GameSave.KnowsVein(portalId, Arrival);
    // Where the last Wild Vein trip went (for tests).
    public static WorldAtlas.Place LastWildTrip { get; private set; }
    float quietUntil = float.NegativeInfinity;

    PlayerMovement inside;
    bool used; float glow, usedAt; Vector3 membraneScale;

    // Where an arriving Qori appears: beside the arch, clear of its trigger (so he doesn't go
    // straight back), dropping to the ground.
    public Vector2 ArrivalPoint => (Vector2)transform.position + new Vector2(exitSide * 1.8f, 1.2f);

    // Leaves and motes spiralling into the doorway: each follows a smooth inward spiral on a loop,
    // staggered so a steady stream is always drawn in.
    const int SwirlLeaves = 7, SwirlMotes = 5;
    const float SwirlPeriod = 3.2f, SwirlTurns = 1.4f;
    SpriteRenderer[] swirl; Vector3[] swirlLast; float[] swirlSize, swirlSpin;
    Vector2 swirlCenter, swirlRadius;

    void Awake() { membraneScale = membrane.transform.localScale; }

    void Start()
    {
        var lib = Fx.Library; if (lib == null || lib.leaves.Length == 0) return;
        Bounds b = membrane.bounds;
        swirlCenter = transform.InverseTransformPoint(b.center);
        Vector3 scale = transform.lossyScale;   // sizes below are world units, applied in local space
        swirlRadius = new Vector2(b.extents.x * .72f / scale.x, b.extents.y * .62f / scale.y);
        float leaf = Mathf.Clamp(b.size.x * .16f, .16f, .34f) / Mathf.Abs(scale.x);
        int n = SwirlLeaves + SwirlMotes;
        swirl = new SpriteRenderer[n]; swirlLast = new Vector3[n]; swirlSize = new float[n]; swirlSpin = new float[n];
        for (int i = 0; i < n; i++)
        {
            bool mote = i >= SwirlLeaves && lib.checkpointMote != null;
            Sprite s = mote ? lib.checkpointMote : lib.leaves[i % lib.leaves.Length];
            var obj = new GameObject(mote ? "Portal mote" : "Portal leaf");
            obj.transform.SetParent(transform, false);
            var r = swirl[i] = obj.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = membrane.sortingOrder + 3; r.flipX = i % 2 == 1;
            swirlSize[i] = (mote ? leaf * .9f : leaf * Random.Range(.85f, 1.15f)) / Mathf.Max(.0001f, s.bounds.size.x);
            swirlSpin[i] = mote ? 0f : Random.Range(-160f, 160f);
        }
        PlaceSwirl();
        for (int i = 0; i < n; i++) swirlLast[i] = swirl[i].transform.position;
    }

    void PlaceSwirl()
    {
        int n = swirl.Length;
        for (int i = 0; i < n; i++)
        {
            bool mote = i >= SwirlLeaves;
            // Leaves and motes are interleaved around the loop rather than bunched.
            float offset = mote ? (i - SwirlLeaves + .5f) / SwirlMotes : (float)i / SwirlLeaves;
            float t = Mathf.Repeat(Time.time / SwirlPeriod + offset, 1f);
            float inward = t * t * (3f - 2f * t);                    // eases in and out of the centre
            float radius = Mathf.Lerp(1f, .06f, inward);
            float angle = (offset * 360f + inward * SwirlTurns * 360f) * Mathf.Deg2Rad;
            var tr = swirl[i].transform;
            tr.localPosition = new Vector3(swirlCenter.x + Mathf.Cos(angle) * swirlRadius.x * radius,
                                           swirlCenter.y + Mathf.Sin(angle) * swirlRadius.y * radius, 0f);
            tr.localScale = Vector3.one * swirlSize[i] * Mathf.Lerp(.35f, 1f, radius);
            float alpha = Mathf.Min(Mathf.Clamp01(t / .18f), Mathf.Clamp01((1f - t) / .3f));
            Color c = swirl[i].color; c.a = alpha * (mote ? .9f : 1f); swirl[i].color = c;
        }
    }

    void LateUpdate()
    {
        if (swirl == null) return;
        PlaceSwirl();
        for (int i = 0; i < swirl.Length; i++)
        {
            var tr = swirl[i].transform;
            Vector3 step = tr.position - swirlLast[i]; swirlLast[i] = tr.position;
            if (step.sqrMagnitude < 1e-8f) continue;
            float heading = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg;
            // Motes lead with their bright head (the art's tail points up); leaves follow the path with a gentle flutter.
            if (i >= SwirlLeaves) { tr.rotation = Quaternion.Euler(0f, 0f, heading + 90f); continue; }
            float z = heading + swirlSpin[i] * .25f * Mathf.Sin(Time.time * 2.2f + i);
            tr.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(tr.eulerAngles.z, z, Time.deltaTime * 8f));
        }
    }

    static PlayerMovement PlayerOf(Collider2D other) =>
        other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;

    void OnTriggerEnter2D(Collider2D other) { var p = PlayerOf(other); if (p != null) inside = p; }
    void OnTriggerExit2D(Collider2D other) { if (PlayerOf(other) == inside) inside = null; }

    static bool UpPressed()
    {
        var key = Keyboard.current; var pad = Gamepad.current;
        return key != null && (key.wKey.wasPressedThisFrame || key.upArrowKey.wasPressedThisFrame)
            || pad != null && (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame);
    }

    // Travels if Qori is standing in the portal (the up key calls this; so can tests).
    public bool Use()
    {
        if (inside == null || !inside.isActiveAndEnabled || AreaTransition.IsTransitioning) return false;
        if (IsWild)
        {
            var atlas = WorldAtlas.Load();
            var places = atlas != null ? atlas.WildDestinations(portalId) : new System.Collections.Generic.List<WorldAtlas.Place>();
            if (places.Count == 0) { quietUntil = Time.time + 2.5f; return false; }
            var place = LastWildTrip = places[Random.Range(0, places.Count)];
            used = true; usedAt = Time.time;
            AreaTransition.Travel(place.scene, place.arrival, inside, "The wild vein threw you here");
            return true;
        }
        if (!AreaTransition.CanTravelTo(Scene)) return false;
        used = true; usedAt = Time.time;
        bool first = GameSave.TravelVein(portalId, Arrival);
        AreaTransition.Travel(Scene, Arrival, inside, first ? "New vein charted" : "");
        return true;
    }
    public bool QoriInside => inside != null;

    void Update()
    {
        if (inside != null && !GamePauseMenu.BlocksGameplayInput && UpPressed()) Use();
        glow = Mathf.MoveTowards(glow, used ? 1f : inside != null ? .5f : 0f, Time.deltaTime * 2f);
        float breathe = 1f + .03f * Mathf.Sin(Time.time * 1.7f);
        membrane.transform.localScale = new Vector3(membraneScale.x * breathe, membraneScale.y * (2f - breathe), 1f);
        Color c = membrane.color; c.a = .78f + .12f * Mathf.Sin(Time.time * 2.3f) + .2f * glow;
        if (membraneWild != null) membrane.sprite = IsWild ? membraneWild : membraneStable;
        if (IsWild)
        {
            // A wild vein flickers between mint and a pale bruise-violet, and gutters.
            float n = Mathf.PerlinNoise(Time.time * 3.1f, portalId.Length), flick = Mathf.PerlinNoise(Time.time * 9f, 7.3f);
            Color tint = Color.Lerp(new Color(.8f, 1f, .92f), new Color(.82f, .7f, 1f), n);
            c = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(c.a * Mathf.Lerp(.45f, 1.05f, flick)));
        }
        else c = new Color(1f, 1f, 1f, c.a);
        membrane.color = c;
    }

    // While Qori stands in it: how to travel and where to; a portal with nowhere to go says so.
    void OnGUI()
    {
        if (inside == null || AreaTransition.IsTransitioning || GamePauseMenu.IsPaused) return;
        string text;
        if (IsWild) text = Time.time < quietUntil ? "The wild vein is quiet: there is nowhere it can throw you yet"
            : "A wild vein: it could throw you anywhere. Press W / Up to enter";
        else if (!AreaTransition.CanTravelTo(Scene))
            text = string.IsNullOrEmpty(Scene) ? "This vein leads nowhere yet" : "Vein to " + Scene + " (not in the build's scene list)";
        else if (Known) text = "Press W / Up to travel to " + Where;
        else text = Settled ? "The wild vein has settled. Press W / Up to find where it leads"
            : "Press W / Up to enter the vein. Where it leads is unknown";
        float width = Mathf.Min(460f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height - 110f, width, 40f), text);
    }
}
