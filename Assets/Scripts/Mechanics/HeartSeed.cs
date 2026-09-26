using UnityEngine;

// A heart-shaped seed hidden somewhere in an area. Collecting it adds a heart for good (it's
// saved), refills Qori's hearts, and shows a banner. Once collected it doesn't come back.
[DisallowMultipleComponent]
public sealed class HeartSeed : MonoBehaviour
{
    [Tooltip("Unique, permanent id, e.g. a1-heartseed. Do not change after saving progress.")] public string seedId = "";
    public SpriteRenderer image;

    Vector3 rest; float collectedAt = -1f;

    void Start()
    {
        rest = image.transform.localPosition;
        if (GameSave.HasPickup(seedId)) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collectedAt >= 0f || other.attachedRigidbody == null) return;
        var health = other.attachedRigidbody.GetComponent<PlayerHealth>();
        if (health == null) return;
        collectedAt = Time.time;
        GameSave.AddPickup(seedId);
        health.AddMaximum(1);
        var lib = Fx.Library;
        if (lib != null) { Fx.Pop(lib.telegraphGlint, image.transform.position, 1.6f, .45f, 45); Fx.Leaves(image.transform.position, 6, 1f); }
        image.enabled = false;
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
    }

    void Update()
    {
        if (collectedAt < 0f) image.transform.localPosition = rest + Vector3.up * (.08f * Mathf.Sin(Time.time * 2.4f));
    }

    void OnGUI()
    {
        if (collectedAt < 0f || GamePauseMenu.IsPaused || Time.time - collectedAt > 3.5f) return;
        float width = Mathf.Min(420f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, 60f, width, 40f), "Heart Seed: one more heart");
    }
}
