#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Explicitly created by the review launcher, never installed in saved scenes or
// player builds. Both sides of each affected passage use the room's same override.
[ExecuteAlways]
public sealed class CaveParallaxComparison : MonoBehaviour
{
    [Serializable] sealed class Connection { public string id, fromRoom, toRoom; }
    [Serializable] sealed class Connections { public Connection[] connections; }
    struct Binding { public Material material; public string prefix; public Vector4 settings, pivot, continuousRect; public Texture continuous; public float continuousEnabled; }
    readonly List<Binding> bindings = new List<Binding>();
    public bool Gentle { get; private set; }
    public int BindingCount => bindings.Count;
    public bool HasContinuousPainting { get; private set; }
    public Vector2 ReferenceCamera { get; private set; }
    public static bool Includes(string id) => id == "A04" || id == "A05";
    // Fixed world coverage for the walking ledge and glide below it; never
    // recenter the painting with the camera to hide its finite authored extent.
    public static Vector4 ContinuousReviewRect => new Vector4(1605,100,108,36);

    public void Configure(Vector2 reference)
    {
        if (bindings.Count != 0) throw new InvalidOperationException("Comparison already configured");
        ReferenceCamera = reference;
        CavePassagePresentation.Create();
        foreach (var v in FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None))
            if (Includes(v.GetComponentInParent<UndergroundRoom>().roomId)) Add(v.GetComponent<SpriteRenderer>().sharedMaterial, "_");
        var connections = JsonUtility.FromJson<Connections>(Resources.Load<TextAsset>("CavePassageConnections").text).connections;
        var lookup = new Dictionary<string, Connection>();
        foreach (var c in connections) lookup["Continuous passage " + c.id] = c;
        foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (r.sharedMaterial == null || !lookup.TryGetValue(r.sharedMaterial.name, out var c)) continue;
            if (Includes(c.fromRoom)) Add(r.sharedMaterial, "_");
            if (Includes(c.toRoom)) Add(r.sharedMaterial, "_B_");
        }
        SetGentle(false, false);
    }
    void Add(Material material, string prefix)
    {
        bindings.Add(new Binding { material = material, prefix = prefix,
            continuous = material.GetTexture(prefix + "Continuous"), continuousRect = material.GetVector(prefix + "ContinuousRect"), continuousEnabled = material.GetFloat(prefix + "ContinuousEnabled"),
            settings = material.GetVector(prefix + "Parallax"), pivot = material.GetVector(prefix + "ParallaxPivot") });
    }
    public void SetContinuousPainting(Texture2D painting, Vector4 sharedRect)
    {
        if (painting == null || sharedRect.z <= 0 || sharedRect.w <= 0) throw new ArgumentException("Painting and positive world size required");
        HasContinuousPainting = true;
        foreach (var b in bindings) if (b.material != null)
        {
            b.material.SetTexture(b.prefix + "Continuous", painting);
            b.material.SetVector(b.prefix + "ContinuousRect", sharedRect);
            b.material.SetFloat(b.prefix + "ContinuousEnabled", 1);
        }
    }
    public void SetGentle(bool gentle, bool announce = true)
    {
        Gentle = gentle;
        foreach (var b in bindings) if (b.material != null)
        {
            b.material.SetVector(b.prefix + "Parallax", new Vector4(gentle ? .30f : 0, 0, 1, 0));
            b.material.SetVector(b.prefix + "ParallaxPivot", new Vector4(ReferenceCamera.x, ReferenceCamera.y, 0, 0));
        }
        if (announce) ShowMode();
    }
    public void ShowMode() => TownHud.Toast(
        (Gentle ? "B: Horizontal depth (30%)" : "A: Scenery anchored to the cave") +
        " — F8 / left stick click: switch. F6: return to the start.", 12f);
    void Update()
    {
        if (!Application.isPlaying) return;
        if (GamePauseMenu.BlocksGameplayInput || MraState.Busy) return;
        var keys = Keyboard.current; var pad = Gamepad.current;
        if ((keys != null && keys.f8Key.wasPressedThisFrame) || (pad != null && pad.leftStickButton.wasPressedThisFrame)) SetGentle(!Gentle);
        if (keys != null && keys.f6Key.wasPressedThisFrame) ReturnToStart();
    }
    public void ReturnToStart()
    {
        var player = FindAnyObjectByType<PlayerMovement>(); if (player == null) return;
        foreach (var hit in Physics2D.RaycastAll(new Vector2(1610,109), Vector2.down, 8, LayerMask.GetMask("Ground")))
            if (hit.collider != null && !hit.collider.isTrigger && hit.normal.y > .6f)
            { player.PlaceAt(new Vector2(1610,hit.point.y+.66f),1,true); Physics2D.SyncTransforms(); ShowMode(); return; }
    }
    void OnDisable() => RestoreOriginal();
    void OnDestroy() => RestoreOriginal();
    void RestoreOriginal()
    {
        foreach (var b in bindings) if (b.material != null)
        {
            b.material.SetVector(b.prefix + "Parallax", b.settings); b.material.SetVector(b.prefix + "ParallaxPivot", b.pivot);
            b.material.SetTexture(b.prefix + "Continuous", b.continuous);
            b.material.SetVector(b.prefix + "ContinuousRect", b.continuousRect);
            b.material.SetFloat(b.prefix + "ContinuousEnabled", b.continuousEnabled);
        }
        bindings.Clear();
        HasContinuousPainting = false;
    }
}
#endif

