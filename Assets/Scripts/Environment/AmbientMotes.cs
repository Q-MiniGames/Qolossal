using UnityEngine;

// Drifting glowing motes (fireflies) inside a rectangle: each wanders on its own slow curve and
// pulses in and out, so a patch of shade feels alive. Purely visual.
[DisallowMultipleComponent]
public sealed class AmbientMotes : MonoBehaviour
{
    public Sprite sprite;
    [Tooltip("World rectangle the motes wander in.")] public Rect area = new Rect(0f, 0f, 10f, 4f);
    [Min(0)] public int count = 10;
    [Tooltip("Size of a mote, world units (across the sprite's canvas).")] public float size = .55f;
    public int sortingOrder = 22;

    SpriteRenderer[] motes; Vector2[] anchors; float[] seeds;

    void Start()
    {
        if (sprite == null) return;
        motes = new SpriteRenderer[count]; anchors = new Vector2[count]; seeds = new float[count];
        var random = new System.Random(GetInstanceID());
        for (int i = 0; i < count; i++)
        {
            var r = new GameObject("Mote").AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false);
            r.sprite = sprite; r.sortingOrder = sortingOrder;
            float s = size * (.7f + .6f * (float)random.NextDouble()) / Mathf.Max(.0001f, sprite.bounds.size.x);
            r.transform.localScale = new Vector3(s, s, 1f);
            anchors[i] = new Vector2(area.x + (float)random.NextDouble() * area.width, area.y + (float)random.NextDouble() * area.height);
            seeds[i] = (float)random.NextDouble() * 100f;
            motes[i] = r;
        }
    }

    void Update()
    {
        if (motes == null) return;
        float t = Time.time;
        for (int i = 0; i < motes.Length; i++)
        {
            float k = seeds[i];
            Vector2 wander = new Vector2(Mathf.Sin(t * .31f + k) * 1.2f + Mathf.Sin(t * .77f + k * 2f) * .4f,
                                         Mathf.Sin(t * .43f + k * 3f) * .6f + Mathf.Sin(t * 1.1f + k) * .2f);
            Vector2 p = anchors[i] + wander;
            p.x = Mathf.Clamp(p.x, area.xMin, area.xMax); p.y = Mathf.Clamp(p.y, area.yMin, area.yMax);
            motes[i].transform.position = new Vector3(p.x, p.y, 0f);
            float glow = .5f + .5f * Mathf.Sin(t * (1.3f + (k % 1f)) + k);
            motes[i].color = new Color(1f, 1f, 1f, .15f + .85f * glow * glow);
        }
    }
}
