using UnityEngine;

// The area portal: a rooted arch with a swirling membrane. The membrane slowly turns and
// breathes, and brightens when Qori steps through.
[DisallowMultipleComponent]
public sealed class Portal : MonoBehaviour
{
    public SpriteRenderer membrane;
    [Tooltip("Area this portal leads to (shown until area loading exists).")] public string destination = "A1 Aqueduct";

    bool entered; float glow, enteredAt; Vector3 membraneScale;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (entered || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        entered = true; enteredAt = Time.time;
    }

    void Update()
    {
        glow = Mathf.MoveTowards(glow, entered ? 1f : 0f, Time.deltaTime * 2f);
        float breathe = 1f + .03f * Mathf.Sin(Time.time * 1.7f);
        membrane.transform.localScale = new Vector3(membraneScale.x * breathe, membraneScale.y * (2f - breathe), 1f);
        Color c = membrane.color; c.a = .78f + .12f * Mathf.Sin(Time.time * 2.3f) + .2f * glow; membrane.color = c;
    }

    void OnGUI()
    {
        if (!entered || GamePauseMenu.IsPaused || Time.time - enteredAt > 3f) return;
        float width = Mathf.Min(420f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, 60f, width, 40f), $"Portal to {destination} (area not built yet)");
    }
}
