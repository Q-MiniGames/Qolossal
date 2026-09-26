using UnityEngine;

// Chains from a fixed ceiling height down to a hanging platform (Platform_Moving_A1), drawn
// with the repeating chain tile so they lengthen and shorten as the platform moves.
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class HangingChains : MonoBehaviour
{
    public Sprite chain;
    [Tooltip("World y where the chains are fixed.")] public float ceilingY;
    [Tooltip("Chain x offsets from the platform, and the height (above it) where the painted chains end.")]
    public float leftX = -1.3f, rightX = 1.3f, artTop = 1.62f;
    public int sortingOrder = -16;

    SpriteRenderer left, right;

    void Awake() { left = Make("Chain L"); right = Make("Chain R"); Place(); }

    SpriteRenderer Make(string label)
    {
        var r = new GameObject(label).AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = chain; r.drawMode = SpriteDrawMode.Tiled; r.sortingOrder = sortingOrder;
        return r;
    }

    void LateUpdate() => Place();

    void Place()
    {
        if (chain == null) return;
        float top = transform.position.y + artTop, length = Mathf.Max(0f, ceilingY - top);
        float w = chain.rect.width / chain.pixelsPerUnit;
        foreach (var (r, x) in new[] { (left, leftX), (right, rightX) })
        {
            r.enabled = length > .01f;
            r.size = new Vector2(w, length);
            r.transform.localPosition = new Vector3(x, artTop + length * .5f, 0f);
        }
    }
}
