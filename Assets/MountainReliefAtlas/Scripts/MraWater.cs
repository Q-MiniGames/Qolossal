using UnityEngine;

// Deep water over a chamber's lower shelf. While it's high, falling in sends Qori back to the
// chamber's start (no swimming); a lever drains it (SetOpen true) for the rest of the visit.
// The permanent steps beside it stay usable either way.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraWater : MonoBehaviour, IMechanismTarget
{
    public SpriteRenderer[] art = new SpriteRenderer[0];
    [Min(.1f)] public float drainSeconds = 1.2f;
    bool drained; float level = 1f;
    public bool Drained => drained;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    public void SetOpen(bool open) { if (open) drained = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori != null && level > .5f) qori.Respawn();
    }

    void Update()
    {
        if (!drained || level <= 0f) return;
        level = Mathf.MoveTowards(level, 0f, Time.deltaTime / drainSeconds);
        foreach (var a in art) if (a != null) { var c = a.color; c.a = level; a.color = c; }
        if (level <= .5f) GetComponent<BoxCollider2D>().enabled = false;
    }
}
