using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Qolossal > Audio > SFX Audition: compare a sound's four ElevenLabs takes and record decisions.
//   1-4 play take 1-4 (a loop sound loops until Space), Space stops, F marks the playing/last take
//   Favorite, C Candidate, R Rejected, U back to no decision; Up/Down (or J/K) move between sounds.
// "Game mix" plays a take as the game will (its loudness-matched gain, the sound's volume, through
// its mixer group); "Source" plays the untouched file at full scale. A loop sound offers its seamless
// derivative as well as the source. Decisions save into the library at once; nothing is ever
// marked Favorite or Rejected except by hand, and no file is deleted.
public sealed class SfxAuditionWindow : EditorWindow
{
    SfxLibrary lib;
    Vector2 listScroll, detailScroll;
    string search = "";
    int busFilter = -1;   // -1: all
    bool onlyUndecided, onlyWired;
    int selected = -1, lastTake = -1;
    bool gameMix = true;
    GameObject previewHost; AudioSource preview;
    static GUIStyle wrap;

    [MenuItem("Qolossal/Audio/SFX Audition")]
    public static void Open() => GetWindow<SfxAuditionWindow>("SFX Audition").minSize = new Vector2(820, 520);

    void OnEnable() { lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryBuilder.LibraryPath); wantsMouseMove = false; }
    void OnDisable() { StopPreview(); if (previewHost != null) DestroyImmediate(previewHost); }

    List<int> Visible()
    {
        var list = new List<int>();
        if (lib == null) return list;
        for (int i = 0; i < lib.entries.Count; i++)
        {
            var e = lib.entries[i];
            if (busFilter >= 0 && (int)e.bus != busFilter) continue;
            if (onlyUndecided && e.takes.Any(t => t.state == SfxTakeState.Favorite)) continue;
            if (onlyWired && !e.status.StartsWith("wired")) continue;
            if (search.Length > 0 && e.id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 && (e.gameEvent ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            list.Add(i);
        }
        return list;
    }

    void OnGUI()
    {
        if (wrap == null) wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
        if (lib == null)
        {
            EditorGUILayout.HelpBox("No library at " + SfxLibraryBuilder.LibraryPath + ". Run Qolossal > Audio > Rebuild SFX Library.", MessageType.Warning);
            if (GUILayout.Button("Rebuild SFX Library")) lib = SfxLibraryBuilder.Build(false);
            return;
        }
        var visible = Visible();
        Keys(visible);

        int favs = lib.entries.Count(e => e.takes.Any(t => t.state == SfxTakeState.Favorite));
        int heard = lib.entries.Sum(e => e.takes.Count(t => t.heard)), takes = lib.entries.Sum(e => e.takes.Length);
        int rejected = lib.entries.Sum(e => e.takes.Count(t => t.state == SfxTakeState.Rejected));
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label($"Sounds with a Favorite: {favs}/{lib.entries.Count}   Takes heard: {heard}/{takes}   Rejected: {rejected}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            gameMix = GUILayout.Toggle(gameMix, gameMix ? "Game mix" : "Source", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (GUILayout.Button("Export decisions", EditorStyles.toolbarButton)) Export();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300)))
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                var names = new[] { "All buses" }.Concat(Enum.GetNames(typeof(SfxBus))).ToArray();
                busFilter = EditorGUILayout.Popup(busFilter + 1, names) - 1;
                using (new EditorGUILayout.HorizontalScope())
                {
                    onlyUndecided = GUILayout.Toggle(onlyUndecided, "No Favorite yet", EditorStyles.miniButtonLeft);
                    onlyWired = GUILayout.Toggle(onlyWired, "Wired only", EditorStyles.miniButtonRight);
                }
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (int i in visible)
                {
                    var e = lib.entries[i];
                    bool fav = e.takes.Any(t => t.state == SfxTakeState.Favorite);
                    string mark = fav ? "★ " : e.takes.All(t => t.heard) ? "· " : "  ";
                    var style = i == selected ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button(mark + e.id + (e.status.StartsWith("wired") ? "" : "  (" + e.status + ")"), style)) Select(i);
                }
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.VerticalScope())
            {
                detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                if (selected >= 0 && selected < lib.entries.Count) Detail(lib.entries[selected]);
                else EditorGUILayout.HelpBox("Choose a sound. Keys: 1-4 play, Space stop, F/C/R/U decide, Up/Down next sound.", MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }
        if (preview != null && preview.isPlaying) Repaint();
    }

    void Select(int i) { selected = i; lastTake = -1; StopPreview(); GUI.FocusControl(null); }

    void Detail(SfxEntry e)
    {
        EditorGUILayout.LabelField(e.id, EditorStyles.largeLabel);
        EditorGUILayout.LabelField($"{e.bus} bus · {(e.loop ? "loop" : "one-shot")} · requested {e.requestedSeconds:0.##} s · {e.status}", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Event: " + e.gameEvent, wrap);
        EditorGUILayout.LabelField("Hook: " + e.hook, wrap);
        EditorGUILayout.LabelField("Prompt: " + e.prompt, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        float volume = EditorGUILayout.Slider("Volume", e.volume, 0f, 1f);
        float pitch = EditorGUILayout.Slider("Pitch spread", e.pitchJitter, 0f, .3f);
        float cooldown = EditorGUILayout.Slider("Cooldown (s)", e.cooldown, 0f, 3f);
        int voices = EditorGUILayout.IntSlider("Max voices", e.maxVoices, 1, 8);
        bool positional = EditorGUILayout.Toggle("Positional", e.positional);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(lib, "SFX tuning");
            e.volume = volume; e.pitchJitter = pitch; e.cooldown = cooldown; e.maxVoices = voices; e.positional = positional;
            Save(e);
        }
        bool anyFav = e.takes.Any(t => t.state == SfxTakeState.Favorite);
        EditorGUILayout.HelpBox(anyFav ? "In game: the Favorite take(s) only." : "In game (provisional, not approved): every take that isn't Rejected, in rotation.", MessageType.None);

        for (int i = 0; i < e.takes.Length; i++)
        {
            var t = e.takes[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(t.sourcePath);
                var loopClip = string.IsNullOrEmpty(t.loopPath) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(t.loopPath);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool playing = preview != null && preview.isPlaying && lastTake == i;
                    GUILayout.Label($"Take {i + 1}" + (playing ? "  ▶" : "") + (t.heard ? "" : "  (not heard)"), EditorStyles.boldLabel, GUILayout.Width(150));
                    GUILayout.Label(clip != null ? $"{clip.length:0.00} s, gain {t.gainDb:+0.0;-0.0} dB" : "missing: " + t.sourcePath, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(e.loop ? "▶ Source (loop)" : "▶ Play", GUILayout.Width(110))) Play(e, i, false);
                    if (loopClip != null && GUILayout.Button("▶ Seamless loop", GUILayout.Width(120))) Play(e, i, true);
                    if (GUILayout.Button("■", GUILayout.Width(26))) StopPreview();
                }
                if (clip != null)
                {
                    var tex = AssetPreview.GetAssetPreview(clip);
                    var rect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true));
                    if (tex != null) GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
                    else { EditorGUI.DrawRect(rect, new Color(0, 0, 0, .15f)); Repaint(); }
                    if (preview != null && preview.isPlaying && lastTake == i && preview.clip == clip)
                        EditorGUI.DrawRect(new Rect(rect.x + rect.width * preview.time / clip.length, rect.y, 2, rect.height), Color.yellow);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    var names = new[] { "No decision", "Candidate", "Favorite", "Rejected" };
                    int state = GUILayout.Toolbar((int)t.state, names);
                    if (state != (int)t.state) Decide(e, i, (SfxTakeState)state);
                }
                EditorGUI.BeginChangeCheck();
                string note = EditorGUILayout.TextField("Note", t.note);
                bool useLoop = loopClip != null && e.loop ? EditorGUILayout.ToggleLeft("Game uses the seamless loop derivative (source untouched)", t.useLoopDerivative) : t.useLoopDerivative;
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(lib, "SFX note"); t.note = note; t.useLoopDerivative = useLoop; Save(e); }
                if (!string.IsNullOrEmpty(t.flags)) EditorGUILayout.LabelField("Audit: " + t.flags, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }

    void Decide(SfxEntry e, int take, SfxTakeState state)
    {
        Undo.RecordObject(lib, "SFX decision");
        e.takes[take].state = state;
        Save(e);
    }

    void Save(SfxEntry e)
    {
        SfxLibraryBuilder.RefreshClips(lib, e);
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssetIfDirty(lib);
        Repaint();
    }

    void Play(SfxEntry e, int take, bool loopDerivative)
    {
        var t = e.takes[take];
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(loopDerivative ? t.loopPath : t.sourcePath);
        if (clip == null) return;
        if (previewHost == null)
        {
            previewHost = EditorUtility.CreateGameObjectWithHideFlags("SFX Audition Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
            preview = previewHost.GetComponent<AudioSource>();
            preview.playOnAwake = false; preview.spatialBlend = 0f;
        }
        preview.Stop();
        preview.clip = clip; preview.loop = e.loop; preview.pitch = 1f;
        preview.outputAudioMixerGroup = gameMix ? lib.Group(e.bus) : null;
        preview.volume = gameMix ? Mathf.Clamp01(e.volume * Mathf.Min(1f, Mathf.Pow(10f, (t.gainDb - lib.headroomDb) / 20f))) : 1f;
        preview.Play();
        lastTake = take;
        if (!t.heard) { Undo.RecordObject(lib, "SFX heard"); t.heard = true; EditorUtility.SetDirty(lib); AssetDatabase.SaveAssetIfDirty(lib); }
    }

    void StopPreview() { if (preview != null) preview.Stop(); }

    void Keys(List<int> visible)
    {
        var ev = Event.current;
        if (ev.type != EventType.KeyDown || GUIUtility.keyboardControl != 0) return;
        var e = selected >= 0 && selected < lib.entries.Count ? lib.entries[selected] : null;
        int pos = visible.IndexOf(selected);
        switch (ev.keyCode)
        {
            case KeyCode.Alpha1: case KeyCode.Alpha2: case KeyCode.Alpha3: case KeyCode.Alpha4:
                if (e != null) { int k = ev.keyCode - KeyCode.Alpha1; if (k < e.takes.Length) Play(e, k, e.loop && !string.IsNullOrEmpty(e.takes[k].loopPath) && e.takes[k].useLoopDerivative); }
                break;
            case KeyCode.Space: StopPreview(); break;
            case KeyCode.F: if (e != null && lastTake >= 0) Decide(e, lastTake, SfxTakeState.Favorite); break;
            case KeyCode.C: if (e != null && lastTake >= 0) Decide(e, lastTake, SfxTakeState.Candidate); break;
            case KeyCode.R: if (e != null && lastTake >= 0) Decide(e, lastTake, SfxTakeState.Rejected); break;
            case KeyCode.U: if (e != null && lastTake >= 0) Decide(e, lastTake, SfxTakeState.Unheard); break;
            case KeyCode.DownArrow: case KeyCode.J: if (visible.Count > 0) Select(visible[Mathf.Min(visible.Count - 1, pos + 1)]); break;
            case KeyCode.UpArrow: case KeyCode.K: if (visible.Count > 0) Select(visible[Mathf.Max(0, pos - 1)]); break;
            default: return;
        }
        ev.Use(); Repaint();
    }

    void Export()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "Audio", "SFX_AUDITION_DECISIONS.csv"));
        var sb = new StringBuilder("Sound ID,Take,File,State,Heard,In use,Note\n");
        foreach (var e in lib.entries)
        {
            bool anyFav = e.takes.Any(t => t.state == SfxTakeState.Favorite);
            for (int i = 0; i < e.takes.Length; i++)
            {
                var t = e.takes[i];
                bool inUse = anyFav ? t.state == SfxTakeState.Favorite : t.state != SfxTakeState.Rejected;
                sb.Append($"{e.id},{i + 1},{Path.GetFileName(t.sourcePath)},{t.state},{t.heard},{inUse},\"{(t.note ?? "").Replace("\"", "\"\"")}\"\n");
            }
        }
        File.WriteAllText(path, sb.ToString());
        Debug.Log("[Sfx] audition decisions exported to " + path);
        EditorUtility.RevealInFinder(path);
    }
}
