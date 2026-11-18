using System.Collections.Generic;
using UnityEngine;

// The caves' slow depth layer (polish plan, WS7). The builder's backdrops are tiled boxes fixed to their rooms;
// here they switch to a shader that tiles them from world position and shifts them with the camera, so the far
// one passes slower than the terrain and the near one a little slower. The boxes stay as built, so no edge can
// open up and nothing is added to the scene. One shader value per frame; no per-renderer work.
[DefaultExecutionOrder(10000)]
public sealed class CaveDepth : MonoBehaviour
{
    // Share of the camera's motion each copies. Far also covers the passages and the sheets behind them.
    public const float FarRate = .3f, NearRate = .12f;

    public static CaveDepth Instance { get; private set; }
    public int Converted { get; private set; }
    public int Skipped { get; private set; }

    static readonly int CameraId = Shader.PropertyToID("_CaveCamera"), TileId = Shader.PropertyToID("_Tile"), RateId = Shader.PropertyToID("_Rate");
    Camera view;

    public static CaveDepth Create(Camera camera)
    {
        var depth = new GameObject("Cave depth (generated)").AddComponent<CaveDepth>();
        depth.view = camera;
        depth.Convert();
        Instance = depth;
        Apply(camera != null ? camera : Camera.main);
        return depth;
    }

    // Finds the backdrops by the names the builder gives them.
    void Convert()
    {
        var shader = Shader.Find("Qolossal/CaveDepthBackdrop");
        if (shader == null) { Debug.LogError("[CaveDepth] the shader is missing; the backdrops stay fixed."); return; }
        var materials = new Dictionary<(Sprite, float), Material>();
        foreach (var art in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // The world refresh's scenic band has its own native-density material.
            if (art.TryGetComponent<WorldRefreshCaveVista>(out var vista) && vista.enabled && vista.vista != null) continue;
            float rate = art.name == "UGP backdrop_near" ? NearRate : art.name == "UGP backdrop_far" || art.name == "UGP passage backdrop" ? FarRate : -1f;
            if (rate < 0f) continue;
            var sprite = art.sprite;
            // A tile cut out of a bigger texture can't wrap: it keeps its fixed place.
            if (sprite == null || art.drawMode != SpriteDrawMode.Tiled || sprite.rect.width != sprite.texture.width || sprite.rect.height != sprite.texture.height) { Skipped++; continue; }
            if (!materials.TryGetValue((sprite, rate), out var material))
            {
                material = new Material(shader) { name = "Cave depth " + sprite.name, hideFlags = HideFlags.DontSave };
                material.SetVector(TileId, sprite.rect.size / sprite.pixelsPerUnit);
                material.SetFloat(RateId, rate);
                materials[(sprite, rate)] = material;
            }
            art.sharedMaterial = material;
            Converted++;
        }
    }

    void LateUpdate() => Apply(view != null ? view : Camera.main);

    public static void Apply(Camera camera)
    {
        if (camera != null) Shader.SetGlobalVector(CameraId, camera.transform.position);
    }
}
