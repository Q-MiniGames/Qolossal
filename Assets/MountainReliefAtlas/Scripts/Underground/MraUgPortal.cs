using UnityEngine;

// An underground doorway: Up takes Qori to `arrivalId` in `targetScene` (AreaTransition carries his
// hearts and weapon). A doorway can need saved predicates (a solved sluice, the Living Thread) and
// says so when it is closed. A return shortcut (`unlockFlag`) first has to be opened from this side:
// the first press saves the flag, later presses travel. Every doorway leads to one recorded place,
// never to "the last door used".
public sealed class MraUgPortal : MraUgInteract
{
    public string targetScene, arrivalId;
    [Tooltip("Shown with the Up key, e.g. \"Into the drain\".")] public string label = "Go through";
    [Tooltip("Saved predicates (flags, relic:<id>) needed to pass.")] public string[] requires = new string[0];
    [Tooltip("Said when a requirement is missing (a relic's own name is added when one is missing).")] public string closedText = "It won't open from here.";
    [Tooltip("A return shortcut: saved by the first press here, after which the doorway travels.")] public string unlockFlag = "";
    public string unlockLabel = "Open the way back", unlockText = "";
    [Tooltip("Discovered on approach (a surface entrance), shown once.")] public string discoveredFlag = "";
    public string discoveredText = "";
    public string sound = "Cave_Enter";
    [Tooltip("Opt-in side passage: crossing inward while grounded travels without Up.")] public bool walkThroughEntrance;
    public float walkThroughDirection = -1f;

    public bool Unlocked => string.IsNullOrEmpty(unlockFlag) || GameSave.HasFlag(unlockFlag);
    public bool Open => MraState.Holds(requires);

    protected override void Update()
    {
        base.Update();
        if (!walkThroughEntrance || !near || LockedOut || MraState.Busy || GamePauseMenu.BlocksGameplayInput || !Unlocked || !Open) return;
        var player = Qori;
        float direction = walkThroughDirection < 0 ? -1f : 1f;
        if (player != null && player.HasGroundContact &&
            (player.transform.position.x - transform.position.x) * direction > .05f &&
            player.ObservedVelocity.x * direction > .1f) Use();
    }

    protected override void Entered()
    {
        if (!string.IsNullOrEmpty(discoveredFlag) && GameSave.SetFlag(discoveredFlag) && !string.IsNullOrEmpty(discoveredText))
        { TownHud.Toast(discoveredText); Sfx.Play("Discovery"); }
    }

    public override bool Use()
    {
        if (AreaTransition.IsTransitioning) return false;
        if (!Unlocked)
        {
            GameSave.SetFlag(unlockFlag);
            Sfx.Play("Lever_Pull", transform.position);
            if (!string.IsNullOrEmpty(unlockText)) TownHud.Toast(unlockText);
            return true;
        }
        if (!Open)
        {
            string missing = System.Array.Find(requires, r => !MraState.Holds(r));
            TownHud.Toast(missing != null && missing.StartsWith("relic:") ? $"{closedText} You'll need the {MraState.Describe(missing)}." : closedText);
            return false;
        }
        if (!AreaTransition.CanTravelTo(targetScene)) { Debug.LogWarning($"[MraUgPortal] {name}: {targetScene} isn't in the build"); return false; }
        if (!string.IsNullOrEmpty(sound)) Sfx.Play(sound);
        AreaTransition.Travel(targetScene, arrivalId, Qori, "", GetComponent<CaveThresholdPresentation>());
        return true;
    }

    protected override string PromptText => walkThroughEntrance && Unlocked && Open ? "" : !Unlocked ? "▲ " + unlockLabel : "▲ " + label;
}
