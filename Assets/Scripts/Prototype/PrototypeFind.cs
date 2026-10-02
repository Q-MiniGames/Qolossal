using UnityEngine;

// Palm prototype: something Qori finds in a side chamber (a song shell, say). Touching it hides it
// and shows what it was for a few seconds. Nothing is saved.
[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
public sealed class PrototypeFind : MonoBehaviour
{
    [TextArea] public string message = "You found something.";
    [Min(.5f)] public float showSeconds = 4.5f;
    public bool Found { get; private set; }
    float shownAt = -100f; Vector3 rest;

    void Awake() { GetComponent<Collider2D>().isTrigger = true; rest = transform.position; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Found || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        Found = true; shownAt = Time.time;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        if (Fx.Library != null) Fx.Leaves(transform.position, 10, 1.2f);
    }

    void Update() { if (!Found) transform.position = rest + Vector3.up * (.12f * Mathf.Sin(Time.time * 2.2f)); }

    void OnGUI()
    {
        if (Time.time - shownAt > showSeconds || GamePauseMenu.IsPaused) return;
        float width = Mathf.Min(680f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .22f, width, 52f), message);
    }
}
