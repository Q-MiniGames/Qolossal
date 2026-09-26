using UnityEngine;

// A false wall: painted rock drawn in front of a hidden alcove. It looks solid (with a small crack
// and a wisp of mint glow as the hint), but Qori can walk through it, and while he's inside it
// fades so he can see the space behind.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class SecretWall : MonoBehaviour
{
    public SpriteRenderer overlay;
    [Range(0f, 1f)] public float revealedAlpha = .12f;

    bool inside; float alpha = 1f;
    public bool Revealed => inside;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other) { if (IsQori(other)) inside = true; }
    void OnTriggerExit2D(Collider2D other) { if (IsQori(other)) inside = false; }
    static bool IsQori(Collider2D c) => c.attachedRigidbody != null && c.attachedRigidbody.GetComponent<PlayerMovement>() != null;

    void Update()
    {
        alpha = Mathf.MoveTowards(alpha, inside ? revealedAlpha : 1f, Time.deltaTime * 3f);
        Color c = overlay.color; c.a = alpha; overlay.color = c;
    }
}
