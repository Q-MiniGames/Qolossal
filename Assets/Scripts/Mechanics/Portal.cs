using UnityEngine;

// The area portal: a rooted arch with a swirling membrane. The membrane slowly turns and
// breathes, and brightens when Qori steps through.
[DisallowMultipleComponent]
public sealed class Portal : MonoBehaviour
{
    public SpriteRenderer membrane;
    [Tooltip("Area this portal leads to (shown until area loading exists).")] public string destination = "A1 Aqueduct";

    bool entered; float glow, enteredAt; Vector3 membraneScale;

    void Awake() { membraneScale = membrane.transform.localScale; }

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
