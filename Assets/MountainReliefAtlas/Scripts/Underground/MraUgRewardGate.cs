using UnityEngine;

// Keeps a cave's existing reward (its own reward id, unchanged) out of reach until the cave's
// lesson flag is saved: dimmed and untouchable before, ordinary after. A reward taken before (an
// old save) stays taken: MraReward hides itself.
[RequireComponent(typeof(MraReward))]
public sealed class MraUgRewardGate : MonoBehaviour
{
    public string flag;
    MraReward reward; Collider2D[] colliders;
    public bool Open => GameSave.HasFlag(flag);

    void Awake() { reward = GetComponent<MraReward>(); colliders = GetComponents<Collider2D>(); }
    void OnEnable() { GameSave.FlagsChanged += Refresh; Refresh(); }
    void OnDisable() => GameSave.FlagsChanged -= Refresh;

    void Refresh()
    {
        if (this == null || reward == null || reward.Taken) return;
        foreach (var c in colliders) c.enabled = Open;
        if (reward.image != null) reward.image.color = new Color(1f, 1f, 1f, Open ? 1f : .35f);
    }
}
