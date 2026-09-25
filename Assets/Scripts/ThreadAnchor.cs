using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ThreadAnchor : MonoBehaviour
{
    public static readonly HashSet<ThreadAnchor> Active = new HashSet<ThreadAnchor>();
    private SpriteRenderer sprite;
    private Color restingColor;
    private bool highlighted;
    public bool IsHighlighted => highlighted && isActiveAndEnabled;
    private float flashUntil;
    public bool IsHitFlashing => isActiveAndEnabled && Time.time < flashUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearRegistry() => Active.Clear();

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        restingColor = sprite.color;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable()
    {
        Active.Remove(this);
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        this.highlighted = highlighted;
        if (sprite != null)
            sprite.color = highlighted ? Color.Lerp(restingColor, Color.white, 0.8f) : restingColor;
    }

    public void FlashHit() => flashUntil = Time.time + 0.15f;

    private void LateUpdate()
    {
        // Own both target highlighting and damage feedback to avoid two
        // components overwriting the same sprite color.
        sprite.color = Time.time < flashUntil ? Color.white
            : highlighted ? Color.Lerp(restingColor, Color.white, 0.8f) : restingColor;
    }
}
