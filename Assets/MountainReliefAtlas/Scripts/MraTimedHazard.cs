using UnityEngine;

// Thorns that rise and sink on a steady rhythm, with a generous safe window between (the Wind
// Shelf's dash trial: 1.2 s). They warn before rising: a shiver, then up. While up they cost a
// heart and bounce Qori back (ThornHazard's rule); while down they're harmless.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraTimedHazard : MonoBehaviour
{
    public float upSeconds = 1.6f, safeSeconds = 1.2f, warnSeconds = .4f;
    public Transform art;
    public Vector2 bounce = new Vector2(0f, 9f);
    Vector3 rest; float phase; bool warning, started;
    public bool IsUp { get; private set; }

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; if (art != null) rest = art.localPosition; }

    void Update()
    {
        float cycle = upSeconds + safeSeconds;
        phase = Mathf.Repeat(Time.time, cycle);
        bool wasUp = IsUp, wasWarning = warning;
        IsUp = phase < upSeconds;
        warning = !IsUp && phase > cycle - warnSeconds;
        if (started)
        {
            if (IsUp && !wasUp) Sfx.Play("Thorns_Rise", transform.position);
            else if (!IsUp && wasUp) Sfx.Play("Thorns_Sink", transform.position);
            else if (warning && !wasWarning) Sfx.Play("Thorns_Warn", transform.position);
        }
        started = true;
        if (art == null) return;
        float warn = !IsUp && phase > cycle - warnSeconds ? .04f * Mathf.Sin(Time.time * 60f) : 0f;
        float sink = IsUp ? 0f : Mathf.Lerp(0f, .55f, Mathf.Clamp01((phase - upSeconds) / .15f)) * (phase > cycle - warnSeconds ? .6f : 1f);
        art.localPosition = rest + new Vector3(warn, -sink, 0f);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsUp || other.attachedRigidbody == null) return;
        var health = other.attachedRigidbody.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(transform.position, bounce);
    }
}
