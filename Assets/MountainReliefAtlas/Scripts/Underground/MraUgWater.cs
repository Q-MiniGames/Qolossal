using UnityEngine;

// Water over an underground basin, at one of two saved levels: high until `drainedFlag` is saved,
// then low for good. Either level is a hazard (no swimming): touching it returns Qori to his last
// checkpoint, the room's dry entry, with every solved flag kept. The art and the hazard always
// change together; a scene loads at the saved level, never part-way. Rects are world
// [left, bottom, width, height].
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgWater : MonoBehaviour
{
    public string drainedFlag;
    public Rect high, low;
    public SpriteRenderer art;
    [Tooltip("Painted water (Batch 29): the surface band hangs from the waterline and the lip sits on it; both follow the level.")]
    public SpriteRenderer surface, lip;
    [Min(.1f)] public float drainSeconds = 1.4f;

    BoxCollider2D box; float level = -1f; float resetAt;   // level: 1 high, 0 low
    public bool Drained => GameSave.HasFlag(drainedFlag);
    public Rect Current => Lerp(level);

    void Awake() { box = GetComponent<BoxCollider2D>(); box.isTrigger = true; }
    void Start() { level = Drained ? 0f : 1f; Apply(); }

    Rect Lerp(float k) => Rect.MinMaxRect(Mathf.Lerp(low.xMin, high.xMin, k), Mathf.Lerp(low.yMin, high.yMin, k), Mathf.Lerp(low.xMax, high.xMax, k), Mathf.Lerp(low.yMax, high.yMax, k));

    void Apply()
    {
        var r = Lerp(level);
        transform.position = r.center;
        box.size = r.size; box.offset = Vector2.zero;
        if (art != null) { art.transform.position = r.center; art.size = r.size; }
        if (surface != null)
        {
            float h = Mathf.Min(r.height, surface.sprite.bounds.size.y);
            surface.transform.position = new Vector3(r.center.x, r.yMax - h * .5f, 0f); surface.size = new Vector2(r.width, h);
        }
        if (lip != null) { lip.transform.position = new Vector3(r.xMin, r.yMax, 0f); lip.size = new Vector2(r.width, lip.sprite.bounds.size.y); }
    }

    void Update()
    {
        float target = Drained ? 0f : 1f;
        if (Mathf.Approximately(level, target)) return;
        level = Mathf.MoveTowards(level, target, Time.deltaTime / drainSeconds);
        Apply();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var q = MraState.QoriOf(other);
        if (q == null || Time.time < resetAt || AreaTransition.IsTransitioning) return;
        resetAt = Time.time + .5f;
        Sfx.Play("Qori_Step_Water", q.transform.position);
        q.Respawn();
    }
}
