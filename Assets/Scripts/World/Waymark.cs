using UnityEngine;

// A small root shrine deep in a level: a checkpoint that also charts the level on the titan's
// map. A map leaf floats over it, a "?" until the level is charted and the Waymark's own mark
// after. Touching it the first time charts the level and shows a banner.
[DisallowMultipleComponent, RequireComponent(typeof(Checkpoint))]
public sealed class Waymark : MonoBehaviour
{
    [Tooltip("The level this charts; empty uses the scene's GameArea level id.")] public string levelId = "";
    public SpriteRenderer icon;
    public Sprite unchartedIcon, chartedIcon;
    [Tooltip("Height of the map leaf's centre above the Waymark's position.")] public float iconHeight = 2.1f;

    float chartedAt = -1f; Vector3 iconScale; string levelName;

    public string LevelId => !string.IsNullOrEmpty(levelId) ? levelId : GameArea.InScene != null ? GameArea.InScene.levelId : "";
    public bool IsCharted => GameSave.IsCharted(LevelId);

    void Start()
    {
        var area = GameArea.InScene;
        levelName = area != null && !string.IsNullOrEmpty(area.displayName) ? area.displayName : LevelId;
        if (icon != null) { iconScale = icon.transform.localScale; icon.sprite = IsCharted ? chartedIcon : unchartedIcon; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        if (!GameSave.Chart(LevelId)) return;
        chartedAt = Time.time;
        if (icon != null) icon.sprite = chartedIcon;
        var lib = Fx.Library;
        if (lib != null && icon != null)
        {
            Fx.Pop(lib.telegraphGlint, icon.transform.position, 1.4f, .45f, 45);
            if (lib.checkpointMote != null)
                for (int i = 0; i < 6; i++) Fx.Pop(lib.checkpointMote, (Vector2)icon.transform.position + Random.insideUnitCircle * .5f, .35f, .9f, 44);
        }
    }

    void Update()
    {
        if (icon == null) return;
        float t = Time.time;
        icon.transform.localPosition = new Vector3(0f, iconHeight + .07f * Mathf.Sin(t * 2.1f), 0f);
        // A pop when charted, then a slow breathe.
        float pop = chartedAt < 0f ? 0f : Mathf.Max(0f, 1f - (t - chartedAt) / .5f);
        icon.transform.localScale = iconScale * (1f + .04f * Mathf.Sin(t * 1.7f) + .35f * pop * pop);
    }

    void OnGUI()
    {
        if (chartedAt < 0f || GamePauseMenu.IsPaused || Time.time - chartedAt > 3.5f) return;
        float width = Mathf.Min(460f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, 60f, width, 40f), "Charted: " + levelName);
    }
}
