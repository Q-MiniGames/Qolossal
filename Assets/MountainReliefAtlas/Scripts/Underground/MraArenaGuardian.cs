using UnityEngine;

// A guardian arena's keeper: once its guardian falls, its flag is saved for good (the STATE_CONTRACT's
// ug:BOSS_<route>:open), which opens the arena's gate onward (MraGate.requires); a beaten guardian stays gone.
public sealed class MraArenaGuardian : MonoBehaviour
{
    public EnemyBase guardian;
    public string flag = "ug:BOSS_A:open";
    [Tooltip("The creature this stand-in will become (the design ID), for the record.")] public string designId = "";
    [Tooltip("Rewards held back until it falls (a mini-boss guards its room's reward).")] public MraReward[] rewards = new MraReward[0];
    public string defeatText = "The way onward is open.";

    void Start()
    {
        if (GameSave.HasFlag(flag)) { if (guardian != null) guardian.gameObject.SetActive(false); return; }
        foreach (var r in rewards) if (r != null) r.gameObject.SetActive(false);
    }

    void Update()
    {
        if (guardian == null || guardian.IsAlive || GameSave.HasFlag(flag)) return;
        GameSave.SetFlag(flag);
        foreach (var r in rewards) if (r != null) r.gameObject.SetActive(true);
        TownHud.Toast(defeatText);
    }
}
