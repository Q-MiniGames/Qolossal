using UnityEngine;

// A gate in the route or a chamber, solid while closed. It opens when its saved predicates hold
// (the Cradle crossing: knot:grip), when a switch signals it (a lever, a plate, a light sequence),
// or for good once `latchFlag` is saved (a sluice opened, a chamber's reward taken). Its visuals
// are an existing RootGate (the accepted root-gate states) when there is one.
[DisallowMultipleComponent]
public sealed class MraGate : MonoBehaviour, IMechanismTarget
{
    [Tooltip("Open whenever these saved predicates all hold (empty: only by signal or latch).")] public string[] requires = new string[0];
    [Tooltip("Saved when a signal opens it; open from then on. Empty: a signal holds it open only while on.")] public string latchFlag = "";
    [Tooltip("Also open for good once this saved flag exists (e.g. the chamber's reward).")] public string openAfterFlag = "";
    public Collider2D solid;
    public RootGate visual;
    [Tooltip("Shown instead of a RootGate: hidden while open.")] public SpriteRenderer[] plainArt = new SpriteRenderer[0];
    [Tooltip("Shown only while open (e.g. Sluice_Gate_Open).")] public SpriteRenderer[] openArt = new SpriteRenderer[0];

    bool signalled;
    public bool IsOpen { get; private set; }

    public void SetOpen(bool open)
    {
        signalled = open;
        if (open && !string.IsNullOrEmpty(latchFlag)) GameSave.SetFlag(latchFlag);
        Refresh();
    }

    bool ShouldBeOpen =>
        (requires != null && requires.Length > 0 && MraState.Holds(requires))
        || signalled
        || !string.IsNullOrEmpty(latchFlag) && GameSave.HasFlag(latchFlag)
        || !string.IsNullOrEmpty(openAfterFlag) && GameSave.HasFlag(openAfterFlag);

    void Start() => Refresh();
    void OnEnable() { GameSave.FlagsChanged += Refresh; GameSave.WorldChanged += Refresh; }
    void OnDisable() { GameSave.FlagsChanged -= Refresh; GameSave.WorldChanged -= Refresh; }

    public void Refresh()
    {
        if (this == null) return;
        IsOpen = ShouldBeOpen;
        if (visual != null) visual.SetOpen(IsOpen);
        else if (solid != null) solid.enabled = !IsOpen;
        foreach (var art in plainArt) if (art != null) art.enabled = !IsOpen;
        foreach (var art in openArt) if (art != null) art.enabled = IsOpen;
    }
}
