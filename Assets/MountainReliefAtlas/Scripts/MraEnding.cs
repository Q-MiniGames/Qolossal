using System.Collections;
using UnityEngine;

// The careful ending, in the descent's final chamber: Qori lays his hand on the last knot (the
// existing heart knot), and the accepted ending stills play. Saved as mra:careful-ending; from then
// on the descent shows its healed state (MraVariant) and the ending doesn't replay. No combat here.
// Art gap (see the handoff): Ending_Careful_03_Shoulder is a Review 22 redo and is not used.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraEnding : MonoBehaviour
{
    public string knot = "heart";
    public Sprite[] stills = new Sprite[0];
    public string[] captions = new string[0];

    public static bool IsPlaying { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsPlaying = false;

    bool near;
    public bool Done => GameSave.HasFlag(MraState.EndingFlag);

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) { if (MraState.IsQori(other)) near = true; }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    void Update()
    {
        if (near && !Done && !IsPlaying && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && TownInput.Up()) Play();
    }

    // Up calls this; so can tests.
    public bool Play()
    {
        if (Done || IsPlaying) return false;
        StartCoroutine(Run());
        return true;
    }

    IEnumerator Run()
    {
        IsPlaying = true; ModalUi.Open();
        Sfx.PlayStory("Story_EyeOpen");
        GameSave.WakeKnot(knot);
        GameSave.SetFlag(MraState.EndingFlag);
        yield return MraStills.Play(stills, captions, 6f);
        Sfx.PlayStory("Story_Ending");
        TownHud.Caption("Carefully.", "Everything it held is still here.");
        IsPlaying = false; ModalUi.Close();
    }

    void OnGUI()
    {
        if (near && !Done && !MraState.Busy) MraState.Prompt(transform.position + Vector3.up * 3f, "▲ Lay a hand on the roots");
    }

    // Cut short (a scene change, a quit): its sounds stop with it.
    void OnDisable() { if (IsPlaying) { IsPlaying = false; ModalUi.Close(); Sfx.Stop("Story_EyeOpen"); Sfx.Stop("Story_Ending"); } }
}
