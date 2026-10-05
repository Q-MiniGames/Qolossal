using System;
using System.Collections.Generic;
using UnityEngine;

// The town's state (world redesign proposal v2, section 5): Amber, the quakes the town has felt,
// and flags (conversations had, requests paid for, song shells found, residents rescued). In the
// Qvale prototype it lives only for the play session. A scene that calls BindToSave (the Mountain
// Relief Atlas route) keeps it in GameSave instead: Amber and quakes as counters, flags as
// "town:<flag>", loaded now and written on every change.
public static class TownState
{
    public static int Amber { get; private set; }
    public static int Quakes { get; private set; }
    static readonly HashSet<string> flags = new HashSet<string>();
    public static event Action Changed;
    public static bool Persistent { get; private set; }

    const string FlagPrefix = "town:", AmberKey = "amber", QuakesKey = "town-quakes";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { Amber = 0; Quakes = 0; flags.Clear(); Changed = null; Persistent = false; }

    // Loads the town from the saved game, and saves every later change.
    public static void BindToSave()
    {
        Persistent = true;
        Amber = GameSave.Count(AmberKey); Quakes = GameSave.Count(QuakesKey);
        flags.Clear();
        foreach (string f in GameSave.Flags) if (f.StartsWith(FlagPrefix)) flags.Add(f.Substring(FlagPrefix.Length));
        Changed?.Invoke();
    }

    public static void AddAmber(int amount) { Amber += Mathf.Max(0, amount); Save(); Changed?.Invoke(); }
    public static bool Spend(int amount)
    {
        if (amount > Amber) return false;
        Amber -= amount; Save(); Changed?.Invoke(); return true;
    }

    public static void Quake() { Quakes++; Save(); Changed?.Invoke(); }

    public static bool Has(string flag) => flags.Contains(flag);
    public static void Set(string flag)
    {
        if (!flags.Add(flag)) return;
        if (Persistent) GameSave.SetFlag(FlagPrefix + flag);
        Changed?.Invoke();
    }

    static void Save()
    {
        if (!Persistent) return;
        GameSave.SetCount(AmberKey, Amber); GameSave.SetCount(QuakesKey, Quakes);
    }

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
