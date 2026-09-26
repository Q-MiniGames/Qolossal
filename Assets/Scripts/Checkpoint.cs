using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class Checkpoint : MonoBehaviour
{
    [Tooltip("Unique, permanent ID within this scene, e.g. lower or upper. Do not change after saving progress.")]
    [SerializeField] private string checkpointId = "";
    public string CheckpointId => checkpointId.Trim();
    public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;
    [Tooltip("World-space offset above the marker for the player's return position.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.2f);
    [SerializeField] private Color activeColor = new Color(0.75f, 1f, 0.9f);
    [Tooltip("An area's own shrine art (e.g. Checkpoint_Shrine_A1), shown instead of the A0 lantern; it brightens when touched.")]
    [SerializeField] private Sprite shrineArt;
    private SpriteRenderer marker;
    private Color restingColor;
    private WorldPropVisual artwork;

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        marker = GetComponent<SpriteRenderer>();
        if (shrineArt != null)
        {
            marker.sprite = shrineArt;
            marker.color = restingColor = new Color(.78f, .8f, .82f);
            return;
        }
        restingColor = marker.color;
        artwork = WorldPropVisual.Create(marker, WorldPropVisual.Kind.Checkpoint);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) return;
        PlayerMovement player = other.attachedRigidbody.GetComponent<PlayerMovement>();
        if (player != null && player.isActiveAndEnabled)
            player.ActivateCheckpoint(this, SpawnPosition);
    }

    public void SetActiveMarker(bool active)
    {
        if (shrineArt != null)
        {
            if (active && marker.color != Color.white && Fx.Library != null && Fx.Library.checkpointMote != null)
                for (int i = 0; i < 4; i++) Fx.Pop(Fx.Library.checkpointMote, (Vector2)transform.position + Random.insideUnitCircle * .4f, .35f, .8f, 30);
            marker.color = active ? Color.white : restingColor;
            return;
        }
        if (marker != null) marker.color = active ? activeColor : restingColor;
        if (artwork != null) artwork.SetState(active ? 1f : 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + (Vector3)spawnOffset, 0.12f);
    }
}
