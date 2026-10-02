using UnityEngine;

// Palm prototype: a line of text shown at the bottom of the screen while Qori is in this trigger.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class PrototypeNote : MonoBehaviour
{
    [TextArea] public string text;
    bool near;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    static bool IsQori(Collider2D other) => other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    void OnTriggerEnter2D(Collider2D other) { if (IsQori(other)) near = true; }
    void OnTriggerExit2D(Collider2D other) { if (IsQori(other)) near = false; }

    void OnGUI()
    {
        if (!near || PalmGripStir.IsPlaying || GamePauseMenu.IsPaused) return;
        float width = Mathf.Min(620f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height - 110f, width, 44f), text);
    }
}
