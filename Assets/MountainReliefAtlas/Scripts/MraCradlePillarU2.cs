using UnityEngine;

// MR01's reviewed after-grip fallen pillar only. Retains the accepted sprite and exact quad.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class MraCradlePillarU2 : MonoBehaviour
{
    SpriteRenderer source;
    GameObject generated;
    Mesh mesh;
    MeshRenderer view;
    Sprite builtSprite;
    bool builtFlipX, builtFlipY, ownsSource, previousForceRenderingOff;
    Color builtColor;
    Color[] colors;
    MaterialPropertyBlock block;

    void OnEnable() => Refresh();
    void LateUpdate() => Refresh();
    void OnDisable() => Release();
    void OnDestroy() => Release();

    // Also callable by the bounded scene integration helper. Inactive conditional parents
    // remain inactive; their transient rendering is constructed when OnEnable runs.
    public bool Refresh()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        if (!Supported()) { Release(); return false; }
        if (!isActiveAndEnabled) { Release(); return true; }
        try
        {
            if (mesh == null || view == null || generated == null || builtSprite != source.sprite ||
                builtFlipX != source.flipX || builtFlipY != source.flipY)
            {
                Release();
                Build();
            }
            Sync();
            return true;
        }
        catch
        {
            // Never leave the original sprite suppressed after an incomplete build.
            Release();
            throw;
        }
    }

    bool Supported()
    {
        if (source == null || source.sprite == null || source.sharedMaterial == null) return false;
        var sprite = source.sprite;
        var texture = sprite.texture;
        var shader = source.sharedMaterial.shader;
        if (texture == null || shader == null || sprite.name != "Pillar_2_Middle" || sprite.packed ||
            texture.width != 1100 || texture.height != 1608 || sprite.pixelsPerUnit != 120f ||
            sprite.rect != new Rect(0, 0, 1100, 1608) || sprite.pivot != new Vector2(0, 804) ||
            texture.wrapModeU != TextureWrapMode.Repeat || texture.wrapModeV != TextureWrapMode.Clamp ||
            source.drawMode != SpriteDrawMode.Simple || source.maskInteraction != SpriteMaskInteraction.None)
            return false;
        return shader.name == "Sprites/Default" || shader.name == "Universal Render Pipeline/2D/Sprite-Lit-Default";
    }

    void Build()
    {
        var sprite = source.sprite;
        var points = sprite.vertices;
        var indices = sprite.triangles;
        var uv = sprite.uv;
        if (points.Length != 4 || indices.Length != 6 || uv.Length != 4)
            throw new System.InvalidOperationException("The reviewed Pillar U2 requires the accepted full quad.");
        var vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            if ((uv[i].x != 0f && uv[i].x != 1f) || (uv[i].y != 0f && uv[i].y != 1f))
                throw new System.InvalidOperationException("The reviewed Pillar U2 requires full 0..1 source UVs.");
            vertices[i] = new Vector3(source.flipX ? -points[i].x : points[i].x, source.flipY ? -points[i].y : points[i].y, 0f);
            uv[i].x *= 2f;
        }
        var triangles = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++) triangles[i] = indices[i];
        mesh = new Mesh { name = "MR01 Pillar U2 exact quad", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        colors = new Color[vertices.Length];
        for (int i = 0; i < colors.Length; i++) colors[i] = source.color;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        var bounds = sprite.bounds;
        var center = bounds.center;
        if (source.flipX) center.x = -center.x;
        if (source.flipY) center.y = -center.y;
        bounds.center = center;
        mesh.bounds = bounds; // Includes original SpriteRenderer's 0.2 Z culling thickness.
        generated = new GameObject("Pillar U2 (generated)") { hideFlags = HideFlags.HideAndDontSave };
        generated.layer = gameObject.layer;
        generated.transform.SetParent(transform, false);
        generated.AddComponent<MeshFilter>().sharedMesh = mesh;
        view = generated.AddComponent<MeshRenderer>();
        view.enabled = false;
        view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        view.receiveShadows = false;
        builtSprite = sprite; builtFlipX = source.flipX; builtFlipY = source.flipY; builtColor = source.color;
        if (block == null) block = new MaterialPropertyBlock();
        previousForceRenderingOff = source.forceRenderingOff;
        ownsSource = true;
    }

    void Sync()
    {
        generated.layer = gameObject.layer;
        view.sharedMaterial = source.sharedMaterial;
        view.sortingLayerID = source.sortingLayerID;
        view.sortingOrder = source.sortingOrder;
        if (builtColor != source.color)
        {
            builtColor = source.color;
            for (int i = 0; i < colors.Length; i++) colors[i] = builtColor;
            mesh.colors = colors;
        }
        source.GetPropertyBlock(block);
        block.SetTexture("_MainTex", source.sprite.texture);
        block.SetColor("_RendererColor", Color.white);
        block.SetVector("_Flip", new Vector4(1, 1, 1, 1));
        view.SetPropertyBlock(block);
        // enabled and sprite remain serialized exactly as before. Only transient rendering
        // ownership changes, so disabling/removing this component restores the legacy view.
        source.forceRenderingOff = true;
        view.enabled = source.enabled && !previousForceRenderingOff;
    }

    void Release()
    {
        if (ownsSource && source != null) source.forceRenderingOff = previousForceRenderingOff;
        ownsSource = false;
        if (view != null) view.enabled = false;
        if (generated != null) { generated.SetActive(false); Dispose(generated); }
        if (mesh != null) Dispose(mesh);
        generated = null; view = null; mesh = null; builtSprite = null; colors = null;
    }

    static void Dispose(Object value)
    {
        if (Application.isPlaying) Object.Destroy(value);
        else Object.DestroyImmediate(value);
    }
}
