using UnityEngine;

// A carved tablet telling the people's side of the story, one per level. Touching it reads it
// (a banner) and adds it to the saved game's journal; a read stone stays, dimmed.
[DisallowMultipleComponent]
public sealed class LoreStone : MonoBehaviour
{
    [Tooltip("Unique, permanent id, e.g. a0-lore. Do not change after saving progress.")] public string stoneId = "";
    [TextArea] public string text = "";
    public SpriteRenderer image;

    float readAt = -1f;
    const float ReadSeconds = 6f;

    void Start() { if (GameSave.HasLoreStone(stoneId)) image.color = new Color(.75f, .75f, .75f); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        if (readAt >= 0f && Time.time - readAt < ReadSeconds) return;
        bool first = !GameSave.HasLoreStone(stoneId);
        GameSave.AddLoreStone(stoneId);
        readAt = Time.time;
        if (first && Fx.Library != null) Fx.Pop(Fx.Library.telegraphGlint, image.transform.position, 1.2f, .4f, 45);
        image.color = new Color(.75f, .75f, .75f);
    }

    void OnGUI()
    {
        if (readAt < 0f || GamePauseMenu.IsPaused || Time.time - readAt > ReadSeconds) return;
        float width = Mathf.Min(620f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, 60f, width, 76f), "Lore Stone\n" + text);
    }
}
