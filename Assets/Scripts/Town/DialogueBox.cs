using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The dialogue box: a panel along the bottom of the screen with the speaker's name and their line,
// typed out. Confirm finishes the line, then moves to the next; Cancel skips to the end. Qori
// can't move while it's open (ModalUi). A line may name its own speaker as "Name|text".
public sealed class DialogueBox : MonoBehaviour
{
    public static DialogueBox Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    const float CharSeconds = .022f;
    Canvas canvas; Text nameText, lineText, arrow;
    readonly List<string> lines = new List<string>();
    string speaker; int index, shown; float typedAt; bool open; int openedFrame; Action done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => Instance = null;

    public static void Show(string speaker, IList<string> lines, Action done = null)
    {
        if (Instance == null) Instance = new GameObject("Dialogue Box").AddComponent<DialogueBox>();
        Instance.Begin(speaker, lines, done);
    }

    void Awake()
    {
        canvas = GameHud.CreateCanvas("Dialogue Canvas", 80);
        canvas.transform.SetParent(transform, false);
        var panel = TownUi.Panel(canvas.transform, "Panel", new Vector2(.5f, 0f), new Vector2(0f, 170f), new Vector2(1320f, 270f));
        TownUi.Panel(canvas.transform, "Name Plate", new Vector2(.5f, 0f), new Vector2(-440f, 330f), new Vector2(430f, 84f));
        nameText = TownUi.Label(canvas.transform, "Name", new Vector2(.5f, 0f), new Vector2(-440f, 330f), new Vector2(360f, 56f), 32, TextAnchor.MiddleCenter, TownUi.PanelAmber, false);
        lineText = TownUi.Label(canvas.transform, "Line", new Vector2(.5f, 0f), new Vector2(0f, 160f), new Vector2(1100f, 150f), 36, TextAnchor.UpperLeft, TownUi.PanelInk, false);
        arrow = TownUi.Label(canvas.transform, "Continue", new Vector2(.5f, 0f), new Vector2(560f, 90f), new Vector2(60f, 50f), 34, TextAnchor.MiddleCenter, TownUi.PanelMint, false);
        arrow.text = "▼";
        panel.raycastTarget = false;
        canvas.enabled = false;
    }

    void Begin(string who, IList<string> newLines, Action whenDone)
    {
        if (!open) ModalUi.Open();
        open = true; openedFrame = Time.frameCount;
        speaker = who; lines.Clear(); lines.AddRange(newLines); done = whenDone;
        index = 0; StartLine();
        canvas.enabled = true;
    }

    void StartLine()
    {
        string line = lines[index];
        int bar = line.IndexOf('|');
        nameText.text = bar > 0 ? line.Substring(0, bar) : speaker;
        shown = 0; typedAt = Time.unscaledTime;
        lineText.text = "";
    }

    string Body => lines[index].Contains("|") ? lines[index].Substring(lines[index].IndexOf('|') + 1) : lines[index];

    void Update()
    {
        if (!open) return;
        string body = Body;
        if (shown < body.Length)
        {
            shown = Mathf.Min(body.Length, Mathf.FloorToInt((Time.unscaledTime - typedAt) / CharSeconds));
            lineText.text = body.Substring(0, shown);
        }
        arrow.enabled = shown >= body.Length && Mathf.Repeat(Time.unscaledTime, .9f) < .6f;
        if (Time.frameCount == openedFrame) return;   // the key that opened it doesn't also skip
        if (TownInput.Cancel()) { Finish(); return; }
        if (!TownInput.Confirm() && !TownInput.Up()) return;
        if (shown < body.Length) { shown = body.Length; lineText.text = body; return; }
        if (++index < lines.Count) StartLine(); else Finish();
    }

    void Finish()
    {
        open = false; canvas.enabled = false;
        ModalUi.Close();
        var then = done; done = null;
        then?.Invoke();
    }

    void OnDestroy() { if (open) ModalUi.Close(); if (Instance == this) Instance = null; }
}
