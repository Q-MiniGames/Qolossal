using UnityEngine;

// A small root shrine deep in a level: a checkpoint that also charts the level on the titan's
// map. Its seed is closed until the level is charted, then opens and glows (Codex's
// Waymark_Dormant / Waymark_Lit, drawn by the checkpoint's own renderer). Touching it the first
// time charts the level and shows a banner. An optional map leaf can float over it.
[DisallowMultipleComponent, RequireComponent(typeof(Checkpoint))]
public sealed class Waymark : MonoBehaviour
{
    [Tooltip("The level this charts; empty uses the scene's GameArea level id.")] public string levelId = "";
    public SpriteRenderer icon;
    public Sprite unchartedIcon, chartedIcon;
    [Tooltip("The shrine's closed and open art, shown by the checkpoint's renderer.")] public Sprite shrineDormant, shrineLit;
    SpriteRenderer shrine;
    [Tooltip("Height of the map leaf's centre above the Waymark's position.")] public float iconHeight = 2.1f;

    float chartedAt = -1f; Vector3 iconScale; string levelName;

    public string LevelId => !string.IsNullOrEmpty(levelId) ? levelId : GameArea.InScene != null ? GameArea.InScene.levelId : "";
    public bool IsCharted => GameSave.IsCharted(LevelId);

    void Start()
    {
        var area = GameArea.InScene;
        levelName = area != null && !string.IsNullOrEmpty(area.displayName) ? area.displayName : LevelId;
        if (icon != null) { iconScale = icon.transform.localScale; icon.sprite = IsCharted ? chartedIcon : unchartedIcon; }
        shrine = GetComponent<SpriteRenderer>();
        if (shrine != null && shrineLit != null) shrine.sprite = IsCharted ? shrineLit : shrineDormant;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        if (!GameSave.Chart(LevelId)) return;
        chartedAt = Time.time;
        Sfx.Play("Waymark_Light");
        if (icon != null) icon.sprite = chartedIcon;
        if (shrine != null && shrineLit != null) shrine.sprite = shrineLit;
        var lib = Fx.Library;
        Vector2 burst = icon != null ? (Vector2)icon.transform.position : shrine != null ? (Vector2)shrine.bounds.center : (Vector2)transform.position;
        if (lib != null)
        {
            Fx.Pop(lib.telegraphGlint, burst, 1.4f, .45f, 45);
            if (lib.checkpointMote != null)
                for (int i = 0; i < 6; i++) Fx.Pop(lib.checkpointMote, burst + Random.insideUnitCircle * .5f, .35f, .9f, 44);
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
