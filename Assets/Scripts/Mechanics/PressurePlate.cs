using UnityEngine;

// A stone plate that holds its targets open while something stands on it (Qori or a creature).
[DisallowMultipleComponent]
public sealed class PressurePlate : MechanismSwitch
{
    public SpriteRenderer image;
    public Sprite up, down;
    [Tooltip("Trigger area on top of the plate.")] public BoxCollider2D sensor;

    bool pressed;
    readonly Collider2D[] found = new Collider2D[8];

    public bool IsPressed => pressed;

    void FixedUpdate()
    {
        var filter = new ContactFilter2D(); filter.NoFilter();
        int n = sensor.Overlap(filter, found);
        bool now = false;
        for (int i = 0; i < n; i++)
        {
            Rigidbody2D rb = found[i].attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && !found[i].isTrigger) { now = true; break; }
        }
        if (now == pressed) return;
        pressed = now;
        if (pressed) Sfx.Play("PressurePlate_Down", transform.position);
        image.sprite = pressed ? down : up;
        Signal(pressed);
    }
}
