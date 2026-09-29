using UnityEngine;

// A body-part gate that opens when the titan stirs (Codex's Stir_Gate_A0 … A6, Batch 5 M-09).
// Closed and solid while its knot sleeps; open and passable once it wakes. If the knot wakes
// with the gate in view (during the stir), it shakes and swaps with a burst of dust and leaves.
[DisallowMultipleComponent]
public sealed class StirGate : MonoBehaviour
{
    [Tooltip("A Knots id, e.g. grip.")] public string knot = Knots.Grip;
    public SpriteRenderer image;
    public Sprite closed, open;
    public Collider2D solid;

    bool isOpen; float openedAt = float.NegativeInfinity; Vector3 rest;

    public bool IsOpen => isOpen;

    void Awake() { rest = image.transform.localPosition; Apply(false); GameSave.WorldChanged += Changed; }
    void OnDestroy() => GameSave.WorldChanged -= Changed;

    void Changed() { if (this != null) Apply(true); }

    void Apply(bool animate)
    {
        bool wake = GameSave.IsKnotAwake(knot);
        if (wake == isOpen && animate) return;
        isOpen = wake;
        image.sprite = isOpen ? open : closed;
        if (solid != null) solid.enabled = !isOpen;
        if (!animate || !isOpen) return;
        openedAt = Time.time;
        var lib = Fx.Library;
        if (lib != null) Fx.Leaves(image.bounds.center, 10, 1.6f);
    }

    void Update()
    {
        float k = (Time.time - openedAt) / .6f;
        image.transform.localPosition = k >= 0f && k < 1f
            ? rest + new Vector3(Mathf.Sin(Time.time * 60f) * .05f * (1f - k), 0f, 0f)
            : rest;
    }
}
