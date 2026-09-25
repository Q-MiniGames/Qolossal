using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FlyingCreatureVisual : MonoBehaviour
{
    [SerializeField, Range(0f, 0.15f)] private float wingFlex = 0.06f;
    [SerializeField, Min(0.1f)] private float wingCyclesPerSecond = 2f;
    private SpriteRenderer source;
    private ThreadAnchor anchor;
    private Mesh mesh;
    private Material material;
    private MeshRenderer drawing;
    private Vector3[] rest, vertices;
    private Color[] colors;
    private Vector2[] coordinates;
    private bool sourceWasEnabled;
    private float height;
    private float previousAnchorX;
    private bool facingLeft;

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        anchor = GetComponentInParent<ThreadAnchor>();
        previousAnchorX = anchor != null ? anchor.transform.position.x : transform.position.x;
        sourceWasEnabled = source.enabled;
        Sprite sprite = source.sprite;
        Shader shader = Shader.Find("Sprites/Default");
        if (sprite == null || shader == null)
        {
            Debug.LogWarning("Flying Creature Visual needs a sprite and Sprites/Default shader.", this);
            enabled = false;
            return;
        }
        const int cells = 24;
        int count = (cells + 1) * (cells + 1);
        rest = new Vector3[count]; vertices = new Vector3[count];
        coordinates = new Vector2[count]; colors = new Color[count];
        Vector2[] uv = new Vector2[count];
        Rect rect = sprite.rect;
        height = rect.height / sprite.pixelsPerUnit;
        int[] triangles = new int[cells * cells * 6];
        for (int y = 0; y <= cells; y++)
        for (int x = 0; x <= cells; x++)
        {
            int i = y * (cells + 1) + x;
            Vector2 t = new Vector2((float)x / cells, (float)y / cells);
            coordinates[i] = t;
            rest[i] = new Vector3((t.x * rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                (t.y * rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);
            uv[i] = new Vector2((rect.x + t.x * rect.width) / sprite.texture.width,
                (rect.y + t.y * rect.height) / sprite.texture.height);
            colors[i] = source.color;
            if (x == cells || y == cells) continue;
            int k = (y * cells + x) * 6;
            triangles[k] = i; triangles[k + 1] = i + cells + 1; triangles[k + 2] = i + 1;
            triangles[k + 3] = i + 1; triangles[k + 4] = i + cells + 1;
            triangles[k + 5] = i + cells + 2;
        }
        mesh = new Mesh { name = "Seed carrier flexible wings" };
        mesh.vertices = rest; mesh.uv = uv; mesh.triangles = triangles; mesh.colors = colors;
        mesh.MarkDynamic();
        GameObject display = new GameObject("Animated Creature Artwork");
        display.transform.SetParent(transform, false);
        display.AddComponent<MeshFilter>().sharedMesh = mesh;
        drawing = display.AddComponent<MeshRenderer>();
        material = new Material(shader);
        material.mainTexture = sprite.texture;
        drawing.sharedMaterial = material;
        drawing.sortingLayerID = source.sortingLayerID;
        drawing.sortingOrder = source.sortingOrder;
        source.enabled = false;
    }

    private void OnEnable()
    {
        previousAnchorX = anchor != null ? anchor.transform.position.x : transform.position.x;
        if (drawing == null) return;
        drawing.enabled = true;
        source.enabled = false;
    }

    private void LateUpdate()
    {
        if (drawing == null) return;
        if (anchor != null)
        {
            float currentX = anchor.transform.position.x;
            float travel = currentX - previousAnchorX;
            // Retain facing at the turning point instead of flickering at rest.
            if (Mathf.Abs(travel) > 0.0001f) facingLeft = travel < 0f;
            previousAnchorX = currentX;
            // Reflect around the hook point, not the asymmetrical sprite centre.
            float hookX = transform.InverseTransformPoint(anchor.transform.position).x;
            drawing.transform.localScale = new Vector3(facingLeft ? -1f : 1f, 1f, 1f);
            drawing.transform.localPosition = new Vector3(facingLeft ? 2f * hookX : 0f, 0f, 0f);
        }
        float wave = Mathf.Sin(Time.time * Mathf.PI * 2f * wingCyclesPerSecond);
        Color tint = anchor != null && anchor.IsHitFlashing
            ? new Color(1f, 0.35f, 0.35f, 0.7f) : source.color;
        for (int i = 0; i < rest.Length; i++)
        {
            Vector2 t = coordinates[i];
            // Upper wings flex progressively toward their tips. The lower
            // body, seed, and hook point remain completely undeformed.
            float upper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.9f, t.y));
            vertices[i] = rest[i] + Vector3.down * (upper * height * wingFlex * wave);
            colors[i] = tint;
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

    private void OnDisable()
    {
        if (drawing != null) drawing.enabled = false;
        if (source != null) source.enabled = sourceWasEnabled;
    }

    private void OnDestroy()
    {
        if (drawing != null) Destroy(drawing.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
