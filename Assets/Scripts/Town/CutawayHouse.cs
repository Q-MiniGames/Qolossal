using System.Collections.Generic;
using UnityEngine;

// A house you walk into (world redesign proposal v2, section 5.4): while Qori is inside (this
// object's trigger covers the interior), the front wall fades away and shows the room; when he
// leaves, it closes again. The interior is in the same scene, so there's no loading.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class CutawayHouse : MonoBehaviour
{
    public List<BodyShape> facade = new List<BodyShape>();
    public List<SpriteRenderer> facadeSprites = new List<SpriteRenderer>();
    [Range(0f, 1f)] public float openAlpha = .08f;
    [Min(.05f)] public float seconds = .35f;

    public bool QoriInside { get; private set; }
    public float Openness { get; private set; }

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    static bool IsQori(Collider2D other) => other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    void OnTriggerEnter2D(Collider2D other) { if (IsQori(other)) QoriInside = true; }
    void OnTriggerExit2D(Collider2D other) { if (IsQori(other)) QoriInside = false; }

    void Update()
    {
        float before = Openness;
        Openness = Mathf.MoveTowards(Openness, QoriInside ? 1f : 0f, Time.deltaTime / seconds);
        if (Mathf.Approximately(before, Openness) && Time.frameCount > 2) return;
        float a = Mathf.Lerp(1f, openAlpha, Mathf.SmoothStep(0f, 1f, Openness));
        foreach (var f in facade) if (f != null) f.SetAlpha(a);
        foreach (var s in facadeSprites) if (s != null) { var c = s.color; c.a = a; s.color = c; }
    }
}
