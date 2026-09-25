using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GrowthPlatform : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float activationRadius = 1.8f;
    [SerializeField, Min(1.5f)] private float lifetime = 5f;
    [SerializeField] private Vector2 platformOffset = new Vector2(0f, 1.8f);
    [SerializeField] private Vector2 platformSize = new Vector2(2.2f, 0.35f);
    private static readonly HashSet<GrowthPlatform> plants = new HashSet<GrowthPlatform>();
    private GameObject platform;
    private SpriteRenderer platformVisual;
    private SpriteRenderer plantVisual;
    private Color restingColor;
    private float expiresAt;
    private float appearedAt;
    private WorldPropVisual plantArtwork, platformArtwork;
    private ContactFilter2D solidFilter;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearRegistry() => plants.Clear();

    private void Awake()
    {
        plantVisual = GetComponent<SpriteRenderer>();
        restingColor = plantVisual.color;
        plantArtwork = WorldPropVisual.Create(plantVisual, WorldPropVisual.Kind.GrowthPlant);
        if (plantArtwork != null) plantArtwork.SetState(0.2f);
        solidFilter = new ContactFilter2D { useTriggers = false };
        platform = new GameObject("Temporary Growth Platform");
        platform.SetActive(false);
        // Independent world dimensions: the small plant's scale must not
        // shrink its platform. OnDestroy cleans up this runtime object.
        platform.layer = LayerMask.NameToLayer("Ground");
        platformVisual = platform.AddComponent<SpriteRenderer>();
        platformVisual.sprite = plantVisual.sprite;
        platformVisual.sharedMaterial = plantVisual.sharedMaterial;
        platformVisual.color = new Color(0.5f, 0.8f, 0.65f);
        platformVisual.sortingLayerID = plantVisual.sortingLayerID;
        platformVisual.sortingOrder = plantVisual.sortingOrder;
        platform.AddComponent<BoxCollider2D>().size = platformSize;
        platformArtwork = WorldPropVisual.Create(platformVisual, WorldPropVisual.Kind.PlatformLeaf,
            platformSize.x, platformSize.y*0.5f);
        if (platformArtwork != null) platformVisual = platformArtwork.Artwork;
        else
        {
            // Legacy fallback stays visual-only; physics always uses the authored world dimensions.
            platformVisual.drawMode = SpriteDrawMode.Sliced;
            platformVisual.size = platformSize;
        }
    }

    private void OnEnable()
    {
        plants.Add(this);
        if (plantArtwork != null) plantArtwork.enabled = true;
    }
    private void OnDisable()
    {
        plants.Remove(this);
        if (plantArtwork != null) { plantArtwork.SetState(0.2f); plantArtwork.enabled = false; }
        if (platform != null) platform.SetActive(false);
        if (plantVisual != null) plantVisual.color = restingColor;
    }
    private void OnDestroy()
    {
        if (platform != null) Destroy(platform);
    }

    public static bool ActivateClosest(PlayerMovement player, float closerThan)
    {
        GrowthPlatform closest = null;
        Vector2 playerPosition = player.transform.position;
        foreach (GrowthPlatform plant in plants)
        {
            if (plant == null || !plant.isActiveAndEnabled || plant.platform.activeSelf) continue;
            Vector2 position = plant.transform.position;
            float distance = Vector2.Distance(playerPosition, position);
            if (distance > plant.activationRadius || distance >= closerThan || playerPosition.y < position.y - 0.1f)
                continue;
            if (Physics2D.Linecast(playerPosition, position + Vector2.up * 0.25f,
                LayerMask.GetMask("Ground")).collider != null) continue;
            closest = plant;
            closerThan = distance;
        }
        if (closest == null) return false;
        Vector2 destination = (Vector2)closest.transform.position + closest.platformOffset;
        // Include a clearance margin and all solid colliders, including the
        // player and creatures. Never create a platform inside another body.
        if (Physics2D.OverlapBox(destination, closest.platformSize + Vector2.one * 0.1f,
            0f, closest.solidFilter, closest.overlaps) > 0) return true;
        closest.platform.transform.position = new Vector3(destination.x, destination.y, closest.transform.position.z);
        closest.platformVisual.enabled = true;
        closest.platform.SetActive(true);
        closest.appearedAt = Time.time;
        if (closest.platformArtwork != null) closest.platformArtwork.SetUnfold(0f);
        if (closest.plantArtwork != null) closest.plantArtwork.SetState(1f);
        closest.plantVisual.color = new Color(0.65f, 1f, 0.8f);
        closest.expiresAt = Time.time + closest.lifetime;
        return true;
    }

    private void Update()
    {
        if (!platform.activeSelf) return;
        if (platformArtwork != null) platformArtwork.SetUnfold(Mathf.SmoothStep(0f,1f,(Time.time-appearedAt)/0.18f));
        float remaining = expiresAt - Time.time;
        if (remaining <= 0f)
        {
            platform.SetActive(false);
            plantVisual.color = restingColor;
            if (plantArtwork != null) plantArtwork.SetState(0.2f);
        }
        else
        {
            // Only the visual blinks; collision lasts until expiry.
            platformVisual.enabled = remaining > 1f || Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
        }
    }
}
