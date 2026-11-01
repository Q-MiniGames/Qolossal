using System.Collections.Generic;
using UnityEngine;

// The Resonator Door's three bowls, worked in the landmarks' order (Bell, Fossil, Root: one ring,
// two spirals, three branches, carved on each bowl and on the landmarks in the maze before it). The
// order reads from the carved shapes; light and sound only add to them. A wrong bowl clears the
// sequence (never the solved state); the replay stone lights the bowls in order again and clears
// it too. The last right bowl saves `solvedFlag` for good, which opens the gate (MraGate.requires).
public sealed class MraUgSequence : MonoBehaviour
{
    public string solvedFlag;
    public List<MraUgBowl> bowls = new List<MraUgBowl>();
    [Tooltip("The order, as indices into bowls.")] public int[] order = { 0, 1, 2 };

    int step; float replayAt = -1f;
    public bool Solved => GameSave.HasFlag(solvedFlag);
    public int Step => step;

    void Start() { if (Solved) foreach (var b in bowls) b.SetLit(true); }

    public void Press(MraUgBowl bowl)
    {
        if (Solved) return;
        replayAt = -1f;
        int index = bowls.IndexOf(bowl);
        if (index == order[step])
        {
            bowl.SetLit(true);
            Sfx.Play("SeedSwitch_Hit", bowl.transform.position, 1f, .9f + .12f * step);
            if (++step < order.Length) return;
            GameSave.SetFlag(solvedFlag);
            TownHud.Toast("The three bowls ring together. The door lifts.");
            return;
        }
        Clear();
        TownHud.Toast("The bowls fall silent. Their carvings show the order.");
    }

    // The replay stone: clear any partial sequence and light the bowls in order once.
    public void Replay()
    {
        if (Solved) return;
        Clear();
        replayAt = Time.time;
    }

    void Clear() { step = 0; foreach (var b in bowls) b.SetLit(false); }

    void Update()
    {
        if (replayAt < 0f || Solved) return;
        float t = Time.time - replayAt;
        int beat = Mathf.FloorToInt(t / .8f);
        if (beat >= order.Length) { replayAt = -1f; return; }
        for (int i = 0; i < bowls.Count; i++) bowls[i].Glow(i == order[beat]);
    }
}
