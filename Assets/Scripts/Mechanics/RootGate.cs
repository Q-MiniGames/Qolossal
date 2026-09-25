using UnityEngine;

// Interlocked roots blocking a passage, in two halves that slide apart into the ceiling and
// ground when a switch opens it.
[DisallowMultipleComponent]
public sealed class RootGate : MonoBehaviour, IMechanismTarget
{
    public SpriteRenderer top, bottom;
    public Sprite topClosed, bottomClosed, topOpen, bottomOpen;
    public Collider2D solid;
    [Tooltip("How far each half withdraws (world units); the open art already shows the roots pulled back.")] public float travel = .35f;
    [Min(.05f)] public float seconds = .6f;

    bool open; float t; Vector3 topRest, bottomRest;

    public bool IsOpen => open;

    void Awake() { topRest = top.transform.localPosition; bottomRest = bottom.transform.localPosition; }

    public void SetOpen(bool value) => open = value;

    void Update()
    {
        t = Mathf.MoveTowards(t, open ? 1f : 0f, Time.deltaTime / seconds);
        float e = t * t * (3f - 2f * t);
        top.transform.localPosition = topRest + Vector3.up * travel * e;
        bottom.transform.localPosition = bottomRest + Vector3.down * travel * e;
        top.sprite = t > .5f ? topOpen : topClosed;
        bottom.sprite = t > .5f ? bottomOpen : bottomClosed;
        // Passable once the roots have mostly withdrawn; solid again as soon as they close.
        if (solid != null) solid.enabled = t < .6f;
    }
}
