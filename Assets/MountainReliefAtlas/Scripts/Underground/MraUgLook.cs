using UnityEngine;

// A place to look from (the heart's inspection steps): while Qori stands here holding Up, the
// camera eases out to `view` (the whole chamber, so the other sectors read as part of one place he
// can see but not reach); letting go or stepping away eases it back. Nothing moves Qori and no
// barrier stops him. The offset and zoom are applied after CameraFollow and taken off before the
// next follow, like VistaZone, so the follow camera itself is untouched.
[DefaultExecutionOrder(1000), RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgLook : MonoBehaviour
{
    [Tooltip("What the camera shows while looking: centre and half-height.")] public Vector2 centre = new Vector2(15f, 10f);
    [Min(1f)] public float halfHeight = 11f;
    [Min(.05f)] public float easeSeconds = .7f;
    public string label = "Hold to look around";
    public float promptHeight = 2.2f;

    bool near; float k; Vector3 shifted; float normalSize = -1f; Camera view;
    public bool Looking => k > .99f;
    public static bool AnyLooking { get; private set; }
    /// <summary>Where the camera was drawn from at the last LateUpdate (the shift is off it between frames; tests read this).</summary>
    public Vector2 RenderedCentre { get; private set; }
    // Tests hold this instead of the key.
    public bool forceHold;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) { if (MraState.IsQori(other)) near = true; }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    static bool UpHeld()
    {
        var key = UnityEngine.InputSystem.Keyboard.current; var pad = UnityEngine.InputSystem.Gamepad.current;
        return key != null && (key.wKey.isPressed || key.upArrowKey.isPressed)
            || pad != null && (pad.dpad.up.isPressed || pad.leftStick.ReadValue().y > .5f);
    }

    void Update()
    {
        if (view != null) view.transform.position -= shifted;
        shifted = Vector3.zero;
    }

    void LateUpdate()
    {
        if (view == null) { view = Camera.main; if (view == null) return; }
        if (normalSize < 0f) normalSize = view.orthographicSize;
        bool hold = near && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && (forceHold || UpHeld());
        float before = k;
        k = Mathf.MoveTowards(k, hold ? 1f : 0f, Time.deltaTime / easeSeconds);
        if (k <= 0f && before <= 0f) { if (AnyLooking && !hold) AnyLooking = false; return; }
        AnyLooking = k > 0f;
        float e = Mathf.SmoothStep(0f, 1f, k);
        view.orthographicSize = Mathf.Lerp(normalSize, halfHeight, e);
        Vector3 at = view.transform.position;
        shifted = new Vector3((centre.x - at.x) * e, (centre.y - at.y) * e, 0f);
        view.transform.position += shifted;
        RenderedCentre = view.transform.position;
    }

    void OnGUI()
    {
        if (near && k < .05f && !MraState.Busy) MraState.Prompt(transform.position + Vector3.up * promptHeight, "▲ " + label);
    }
}
