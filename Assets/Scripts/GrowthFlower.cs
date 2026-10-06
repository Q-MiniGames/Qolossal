using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GrowthFlower : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float activationRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float launchSpeed = 17f;
    [SerializeField, Min(0.1f)] private float cooldown = 1f;
    private static readonly HashSet<GrowthFlower> flowers = new HashSet<GrowthFlower>();
    private SpriteRenderer flowerSprite;
    private Color restingColor;
    private float readyAt;
    private float pulseUntil;
    private WorldPropVisual artwork;
    private BoxCollider2D landingSurface;

    private void FixedUpdate()
    {
        if (landingSurface != null) return;
        if (artwork != null && !artwork.GroundSeated) return;
        // All Start methods have run, so the artwork has settled onto the ground.
        GameObject surface = new GameObject("Flower landing surface");
        surface.transform.SetParent(transform, false);
        surface.transform.position = artwork != null ? artwork.Artwork.transform.position : transform.position;
        surface.transform.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        surface.transform.localScale = new Vector3(1f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f);
        surface.layer = LayerMask.NameToLayer("Ground");
        landingSurface = surface.AddComponent<BoxCollider2D>();
        landingSurface.size = new Vector2(1.05f, .14f);
        landingSurface.offset = new Vector2(0f, .43f);
        landingSurface.usedByEffector = true;
        PlatformEffector2D effector = surface.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 160f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearRegistry() => flowers.Clear();

    private void Awake()
    {
        flowerSprite = GetComponent<SpriteRenderer>();
        restingColor = flowerSprite.color;
        artwork = WorldPropVisual.Create(flowerSprite, WorldPropVisual.Kind.GrowthFlower);
    }

    private void OnEnable()
    {
        flowers.Add(this);
        if (landingSurface != null) landingSurface.enabled = true;
        if (artwork != null) artwork.enabled = true;
    }
    private void OnDisable()
    {
        flowers.Remove(this);
        if (landingSurface != null) landingSurface.enabled = false;
        if (artwork != null) artwork.enabled = false;
        if (flowerSprite != null) flowerSprite.color = restingColor;
    }

    private void Update()
    {
        float pulse = Mathf.Clamp01((pulseUntil - Time.time) / 0.3f);
        if (artwork != null) artwork.SetState(Time.time < readyAt ? pulse : 0.3f, pulse);
        flowerSprite.color = Color.Lerp(restingColor, new Color(0.65f, 1f, 0.9f), pulse);
    }

    private void OnDestroy()
    {
        if (landingSurface != null) Destroy(landingSurface.gameObject);
    }

    public static bool TryActivateClosest(PlayerMovement player)
    {
        GrowthFlower closest = null;
        float closestDistance = float.PositiveInfinity;
        Vector2 playerPosition = player.transform.position;
        Collider2D feetCollider = player.GetComponent<Collider2D>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (feetCollider == null || playerBody == null || playerBody.linearVelocity.y > .1f) return false;
        foreach (GrowthFlower flower in flowers)
        {
            if (flower == null || !flower.isActiveAndEnabled || Time.time < flower.readyAt || flower.landingSurface == null)
                continue;
            Bounds surface = flower.landingSurface.bounds;
            Bounds feet = feetCollider.bounds;
            float gap = feet.min.y - surface.max.y;
            if (gap < -.04f || gap > .12f || feet.max.x <= surface.min.x || feet.min.x >= surface.max.x)
                continue;
            Vector2 position = new Vector2(Mathf.Clamp(feet.center.x, surface.min.x, surface.max.x), surface.max.y + .03f);
            float distance = Vector2.Distance(playerPosition, position);
            if (distance > flower.activationRadius)
                continue;
            // Aim above the landing surface so it cannot block its own boost.
            if (Physics2D.Linecast(playerPosition, position,
                LayerMask.GetMask("Ground")).collider != null)
                continue;
            if (distance < closestDistance) { closestDistance = distance; closest = flower; }
        }
        if (closest == null || !player.LaunchUp(closest.launchSpeed)) return false;
        closest.readyAt = Time.time + closest.cooldown;
        closest.pulseUntil = Time.time + 0.3f;
        Sfx.Play("GrowthFlower_Bloom", closest.transform.position);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.9f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
