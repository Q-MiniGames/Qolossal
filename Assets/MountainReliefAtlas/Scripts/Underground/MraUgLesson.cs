using UnityEngine;

// A skill cave's lesson target: saves the cave's lesson flag (and its solve flag) when Qori uses
// the new skill there. Each cave says what counts:
//   Zone   - standing in this trigger (with `needsDash` / `needsGlide`: having dashed or glided
//            since entering the room), e.g. the top of the moss wall, the glide's landing;
//   Anchor - the Living Thread holding `anchor`;
//   Pod    - a Bloomfall bounce on `pod`.
// The cave's reward listens for the lesson flag (MraUgRewardGate). The skill itself is granted
// earlier and on its own, so nothing here can withhold it.
public sealed class MraUgLesson : MonoBehaviour
{
    public enum Kind { Zone, Anchor, Pod }
    public Kind kind;
    public string lessonFlag, solveFlag;
    public bool needsDash, needsGlide;
    public ThreadAnchor anchor;
    public MraPod pod;
    public string doneText = "";
    PlayerMovement inside; PlayerThread thread; int dashAtStart = -1; bool glided;

    public bool Done => GameSave.HasFlag(lessonFlag);

    void OnTriggerEnter2D(Collider2D other) { var q = MraState.QoriOf(other); if (q != null) inside = q; }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) inside = null; }

    void Update()
    {
        var q = FindAnyObjectByType<PlayerMovement>();
        if (q == null || Done) return;
        if (dashAtStart < 0) dashAtStart = q.DashVersion;
        glided |= q.IsGliding;
        switch (kind)
        {
            case Kind.Zone:
                if (inside == null || !inside.IsGrounded) return;
                if (needsDash && q.DashVersion == dashAtStart) return;
                if (needsGlide && !glided) return;
                break;
            case Kind.Anchor:
                if (thread == null) thread = q.GetComponent<PlayerThread>();
                if (anchor == null || thread == null || !thread.IsAttached || (thread.AnchorPosition - (Vector2)anchor.transform.position).sqrMagnitude > .01f) return;
                break;
            case Kind.Pod:
                if (pod == null || pod.Bounces == 0) return;
                break;
        }
        Complete();
    }

    // The lesson, done (Update calls it; so can tests).
    public void Complete()
    {
        if (!GameSave.SetFlag(lessonFlag)) return;
        if (!string.IsNullOrEmpty(solveFlag)) GameSave.SetFlag(solveFlag);
        Sfx.Play("Discovery");
        if (!string.IsNullOrEmpty(doneText)) TownHud.Toast(doneText);
    }
}
