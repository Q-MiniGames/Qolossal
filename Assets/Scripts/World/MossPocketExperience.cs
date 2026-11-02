using UnityEngine;

// Local presentation for MR01_C02. Progress belongs to the existing puzzle/reward save IDs.
public sealed class MossPocketExperience : MonoBehaviour
{
    public CradleSluicePuzzle puzzle;
    public MraReward reward;
    public MraUgClue entranceClue;
    public SpriteRenderer rewardGlow;
    public Transform[] glints = new Transform[0];
    PlayerMovement player;
    bool welcomed;
    Vector3[] rest;

    void Awake()
    {
        rest = new Vector3[glints.Length];
        for (int i = 0; i < glints.Length; i++) if (glints[i]) rest[i] = glints[i].localPosition;
    }

    void Update()
    {
        if (!puzzle || !reward) return;
        if (!player) player = FindAnyObjectByType<PlayerMovement>();
        bool inside = player && player.transform.position.x > 115 && player.transform.position.x < 159
            && player.transform.position.y > -187 && player.transform.position.y < -162;
        if (inside && !welcomed && !AreaTransition.IsTransitioning && !GamePauseMenu.BlocksGameplayInput)
        {
            welcomed = true;
            TownHud.Toast(reward.Taken ? "Moss Pocket — the pool still holds." : puzzle.Solved
                ? "Moss Pocket — the Heart Seed waits on the highest ledge."
                : "Moss Pocket — follow the water. The old carving beside the pool may help.", 5);
        }
        if (entranceClue)
            entranceClue.text = reward.Taken ? "The pool holds again. Your Heart Seed is safe. Return through the connected rooms to the Hanging Garden (MR01-C06), the only exit to the Cradle."
                : puzzle.Solved ? "The water holds. Climb the mossy ledges to claim the Heart Seed, then return through the connected rooms to the Hanging Garden (MR01-C06)."
                : "Open the floor inlet. Close the spillway above. Fill the pool to awaken the Heart Seed on the highest shelf. The water is safe.";
        if (rewardGlow)
        {
            rewardGlow.enabled = !reward.Taken;
            rewardGlow.color = new Color(.65f, 1f, .72f, (puzzle.Solved ? .5f : .12f) + Mathf.Sin(Time.time * 1.8f) * .04f);
        }
        if (!inside) return;
        for (int i = 0; i < glints.Length; i++) if (glints[i])
            glints[i].localPosition = rest[i] + new Vector3(Mathf.Sin(Time.time * .4f + i) * .12f, Mathf.Sin(Time.time * .65f + i * 2) * .13f, 0);
    }
}
