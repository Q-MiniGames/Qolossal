using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class LevelFinish : MonoBehaviour
{
    private bool completed;
    private SpriteRenderer marker;
    private GUIStyle titleStyle;
    private GUIStyle messageStyle;
    private WorldPropVisual artwork;

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        marker = GetComponent<SpriteRenderer>();
        artwork = WorldPropVisual.Create(marker, WorldPropVisual.Kind.LevelFinish);
        if (artwork != null) artwork.SetState(0.25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed || other.attachedRigidbody == null) return;
        PlayerMovement player = other.attachedRigidbody.GetComponent<PlayerMovement>();
        if (player == null || !player.isActiveAndEnabled) return;
        completed = true;
        marker.color = new Color(1f, 0.95f, 0.65f);
        if (artwork != null) artwork.SetState(1f);
    }

    private void OnGUI()
    {
        if (!completed || GamePauseMenu.IsPaused) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            messageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
        float width = Mathf.Min(440f, Screen.width - 24f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 60f, width, 105f);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, width - 24f, 45f),
            "Opening level complete", titleStyle);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 55f, width - 24f, 40f),
            "Keep exploring, or press Escape to open the menu.", messageStyle);
    }
}
