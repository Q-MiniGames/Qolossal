using System;
using System.Collections.Generic;
using UnityEngine;

// Qvale prototype: the town's state (world redesign proposal v2, section 5): Amber, the quakes the
// town has felt, and flags (conversations had, requests paid for, song shells found). It lives only
// for the play session; the real game will keep it in GameSave.
public static class TownState
{
    public static int Amber { get; private set; }
    public static int Quakes { get; private set; }
    static readonly HashSet<string> flags = new HashSet<string>();
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { Amber = 0; Quakes = 0; flags.Clear(); Changed = null; }

    public static void AddAmber(int amount) { Amber += Mathf.Max(0, amount); Changed?.Invoke(); }
    public static bool Spend(int amount)
    {
        if (amount > Amber) return false;
        Amber -= amount; Changed?.Invoke(); return true;
    }

    public static void Quake() { Quakes++; Changed?.Invoke(); }

    public static bool Has(string flag) => flags.Contains(flag);
    public static void Set(string flag) { if (flags.Add(flag)) Changed?.Invoke(); }

    // A condition used by conversations and town variants: "" (always), "quake" (after a quake),
    // "calm" (before any), "flag:x" or "!flag:x"; several separated by commas must all hold.
    public static bool Check(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        foreach (string raw in condition.Split(','))
        {
            string c = raw.Trim();
            bool ok = c == "quake" ? Quakes > 0
                : c == "calm" ? Quakes == 0
                : c.StartsWith("!flag:") ? !Has(c.Substring(6))
                : c.StartsWith("flag:") ? Has(c.Substring(5))
                : true;
            if (!ok) return false;
        }
        return true;
    }
}
