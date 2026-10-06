using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class GamePauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    private static int resumeFrame = -1;
    public static bool BlocksGameplayInput => IsPaused || Time.frameCount <= resumeFrame || AreaTransition.IsTransitioning || StirSequence.IsPlaying || ChartScreen.IsOpen || ModalUi.IsOpen;
    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private bool ownsPause;
    private bool confirmingNewGame;
    private string restartError;
    // Skinned menu (UiSkin): built on first pause; the IMGUI menu below is the fallback.
    private Canvas menuCanvas;
    private GameObject mainPage, confirmPage, soundPage;
    private bool soundOptions;
    private Text errorText;
    private Button firstMain, firstConfirm, firstSound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsPaused = false;
        resumeFrame = -1;
    }

    private void Update()
    {
        bool toggle = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        toggle |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (toggle && !AreaTransition.IsTransitioning && !ChartScreen.BlocksPause && !ModalUi.IsOpen)   // an open dialogue, shop or listening spot takes Escape itself
        {
            if (ownsPause && (confirmingNewGame || soundOptions))
            {
                confirmingNewGame = soundOptions = false;
                restartError = null;
                Sfx.Play("UI_Back");
                ShowMenu(true);
            }
            else if (ownsPause) Resume();
            else if (!IsPaused) Pause();
        }
    }

    private void Pause()
    {
        Sfx.Play("UI_Pause");
        ShowMenu(true);
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        ownsPause = true;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void Resume()
    {
        if (!ownsPause) return;
        Sfx.Play("UI_Unpause");
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        ownsPause = false;
        confirmingNewGame = soundOptions = false;
        restartError = null;
        IsPaused = false;
        resumeFrame = Time.frameCount;
        ShowMenu(false);
    }

    private void OnDisable() => Resume();

    // Forgets the saved game. In an area, the game restarts in the first area (the first scene in
    // the build list); a sandbox scene just restarts itself.
    private void StartNewGame()
    {
        Scene scene = SceneManager.GetActiveScene();
        int target = GameArea.InScene != null ? 0 : scene.buildIndex;
        // Validate before deleting anything: the level must be loadable.
        if (target < 0 || !Application.CanStreamedLevelBeLoaded(target))
        {
            restartError = "Add this scene to the Build Profiles scene list first.";
            ShowMenu(true);
            return;
        }
        GameSave.Clear();
        Resume();
        SceneManager.LoadScene(target);
    }

    // The map of the titan; only in game areas, where the Chart exists.
    private void OpenChart()
    {
        if (ChartScreen.Instance == null && ChartScreen.OpenReplacement == null) return;
        Resume();
        if (ChartScreen.Instance != null) ChartScreen.Instance.Open();
        else ChartScreen.OpenReplacement();
    }

    private void Quit()
    {
        Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMenu(bool visible)
    {
        if (visible && menuCanvas == null && !BuildMenu()) return;
        if (menuCanvas == null) return;
        menuCanvas.gameObject.SetActive(visible);
        if (!visible) return;
        mainPage.SetActive(!confirmingNewGame && !soundOptions);
        confirmPage.SetActive(confirmingNewGame);
        soundPage.SetActive(soundOptions && !confirmingNewGame);
        errorText.text = restartError ?? "";
        EventSystem.current?.SetSelectedGameObject((confirmingNewGame ? firstConfirm : soundOptions ? firstSound : firstMain).gameObject);
    }

    private bool BuildMenu()
    {
        UiSkin skin = UiSkin.Load();
        if (skin == null) return false;
        menuCanvas = GameHud.CreateCanvas("Pause Menu", 50);
        menuCanvas.transform.SetParent(transform, false);
        var dim = GameHud.AddImage(menuCanvas.transform, "Dim", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000f, 4000f));
        dim.color = new Color(.08f, .1f, .1f, .55f); dim.raycastTarget = true;
        var panel = GameHud.AddImage(menuCanvas.transform, "Panel", skin.panel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(620f, 960f));
        panel.type = Image.Type.Sliced; panel.preserveAspect = false;

        Text Label(Transform parent, string text, float y, int size, float height = 70f)
        {
            var obj = new GameObject("Label", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = (RectTransform)obj.transform;
            rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(500f, height);
            var t = obj.AddComponent<Text>();
            t.font = UiSkin.Font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(.28f, .2f, .12f); t.text = text; t.raycastTarget = false;
            return t;
        }
        Button MakeButton(Transform parent, string text, float y, UnityEngine.Events.UnityAction onClick)
        {
            var image = GameHud.AddImage(parent, text, skin.buttonNormal, new Vector2(.5f, .5f), new Vector2(0f, y), new Vector2(390f, 146f));
            image.preserveAspect = true; image.raycastTarget = true;   // the button art doesn't stretch well
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = skin.buttonHover, selectedSprite = skin.buttonHover, pressedSprite = skin.buttonPressed };
            button.onClick.AddListener(() => Sfx.Play(text == "Cancel" || text == "Back" ? "UI_Back" : "UI_Confirm"));
            if (onClick != null) button.onClick.AddListener(onClick);
            Label(image.transform, text, 2f, 32);
            return button;
        }

        mainPage = new GameObject("Main", typeof(RectTransform)); mainPage.transform.SetParent(panel.transform, false);
        Label(mainPage.transform, "Paused", 385f, 46);
        firstMain = MakeButton(mainPage.transform, "Resume", 275f, Resume);
        MakeButton(mainPage.transform, "Chart", 155f, OpenChart);
        // Freeze frames on big hits (HitStop): some players prefer them off.
        var hitPause = MakeButton(mainPage.transform, "Hit pause", 35f, null);
        var hitPauseText = hitPause.GetComponentInChildren<Text>();
        hitPauseText.text = HitPauseLabel;
        hitPause.onClick.AddListener(() => { HitStop.Enabled = !HitStop.Enabled; hitPauseText.text = HitPauseLabel; });
        MakeButton(mainPage.transform, "Sound", -85f, () => { soundOptions = true; ShowMenu(true); });
        MakeButton(mainPage.transform, "New Game", -205f, () => { confirmingNewGame = true; ShowMenu(true); });
        MakeButton(mainPage.transform, "Quit", -325f, Quit);

        // Sound: each press steps a volume down by a fifth, from silent back to full.
        soundPage = new GameObject("Sound", typeof(RectTransform)); soundPage.transform.SetParent(panel.transform, false);
        Label(soundPage.transform, "Sound", 385f, 46);
        Button Volume(string name, float y, System.Func<float> get, System.Action<float> set)
        {
            var b = MakeButton(soundPage.transform, name, y, null);
            var t = b.GetComponentInChildren<Text>(); t.text = SfxSettings.Label(name, get());
            b.onClick.AddListener(() => { set(SfxSettings.Step(get())); t.text = SfxSettings.Label(name, get()); });
            return b;
        }
        firstSound = Volume("Master", 275f, () => SfxSettings.Master, v => SfxSettings.Master = v);
        Volume("Music", 155f, () => SfxSettings.Music, v => SfxSettings.Music = v);
        Volume("Effects", 35f, () => SfxSettings.Effects, v => SfxSettings.Effects = v);
        Volume("Ambience", -85f, () => SfxSettings.Ambience, v => SfxSettings.Ambience = v);
        MakeButton(soundPage.transform, "Back", -205f, () => { soundOptions = false; ShowMenu(true); });

        confirmPage = new GameObject("Confirm", typeof(RectTransform)); confirmPage.transform.SetParent(panel.transform, false);
        Label(confirmPage.transform, "Start a new game?", 190f, 40);
        Label(confirmPage.transform, "Forget the saved game (relics, checkpoints)\nand start again from the beginning?", 110f, 26, 80f);
        firstConfirm = MakeButton(confirmPage.transform, "Cancel", 0f, () => { confirmingNewGame = false; restartError = null; ShowMenu(true); });
        MakeButton(confirmPage.transform, "Confirm", -110f, StartNewGame);
        errorText = Label(confirmPage.transform, "", -200f, 22);
        errorText.color = new Color(.55f, .15f, .1f);
        return true;
    }

    static string HitPauseLabel => HitStop.Enabled ? "Hit pause: On" : "Hit pause: Off";

    private void OnGUI()
    {
        if (!ownsPause || menuCanvas != null) return;
        float width = Mathf.Min(360f, Screen.width - 20f);
        Rect panel = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - 380f) * 0.5f, width, 380f);
        if (confirmingNewGame)
        {
            GUI.Box(panel, "Start a new game?");
            GUI.Label(new Rect(panel.x + 20f, panel.y + 35f, width - 40f, 55f),
                "Forget the saved game (relics, checkpoints)\nand start again from the beginning?");
            if (GUI.Button(new Rect(panel.x + 25f, panel.y + 100f, width - 50f, 40f), "Cancel"))
            {
                confirmingNewGame = false;
                restartError = null;
            }
            if (GUI.Button(new Rect(panel.x + 25f, panel.y + 155f, width - 50f, 40f), "Confirm New Game"))
                StartNewGame();
            if (!string.IsNullOrEmpty(restartError))
                GUI.Label(new Rect(panel.x + 15f, panel.y + 210f, width - 30f, 45f), restartError);
            return;
        }
        GUI.Box(panel, "Paused");
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 45f, width - 50f, 45f), "Resume"))
            Resume();
        if ((ChartScreen.Instance != null || ChartScreen.OpenReplacement != null) && GUI.Button(new Rect(panel.x + 25f, panel.y + 105f, width - 50f, 45f), "Chart"))
            OpenChart();
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 165f, width - 50f, 45f), "New Game"))
            confirmingNewGame = true;
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 225f, width - 50f, 45f), HitPauseLabel))
            HitStop.Enabled = !HitStop.Enabled;
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 285f, width - 50f, 45f), "Quit"))
            Quit();
    }
}
