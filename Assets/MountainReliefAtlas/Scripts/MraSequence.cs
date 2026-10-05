using System.Collections.Generic;
using UnityEngine;

// Three lights to work in a set order (the design's 1-3-2). Each light shows its place in the order
// by shape and timing as well as colour: at rest the lights blink their order in turn. A wrong
// light resets only the sequence (the entrance and return stay open); the right order signals the
// targets (the reward niche's gate) and stays solved for the visit.
public sealed class MraSequence : MechanismSwitch
{
    public List<MraLever> lights = new List<MraLever>();
    [Tooltip("The order, as indices into lights (0-based): 1-3-2 is 0, 2, 1.")] public int[] order = { 0, 2, 1 };

    int step; bool solved; float hintAt;
    public bool Solved => solved;

    public void Press(MraLever light)
    {
        if (solved) return;
        int index = lights.IndexOf(light);
        if (index == order[step])
        {
            light.SetLit(true);
            if (++step < order.Length) return;
            solved = true; Signal(true);
            TownHud.Toast("Something opens with a soft click.");
            return;
        }
        // Wrong: everything goes dark again and the hint replays.
        step = 0;
        foreach (var l in lights) l.SetLit(false);
        hintAt = Time.time + .6f;
        TownHud.Toast("The lights go out. Watch their order.");
    }

    void Update()
    {
        if (solved || step > 0 || Time.time < hintAt) return;
        // The hint: each light in turn, a beat apart, then a pause.
        float t = Mathf.Repeat(Time.time - hintAt, order.Length * .7f + 1.6f);
        int beat = Mathf.FloorToInt(t / .7f);
        if (beat < order.Length && t - beat * .7f < .05f) lights[order[beat]].Flash(.35f);
    }
}
