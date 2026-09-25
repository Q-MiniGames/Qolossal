using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class GamePauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    private static int resumeFrame = -1;
    public static bool BlocksGameplayInput => IsPaused || Time.frameCount <= resumeFrame;
    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private bool ownsPause;
    private bool confirmingNewGame;
    private string restartError;

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
        if (toggle)
        {
            if (ownsPause && confirmingNewGame)
            {
                confirmingNewGame = false;
                restartError = null;
            }
            else if (ownsPause) Resume();
            else if (!IsPaused) Pause();
        }
    }

    private void Pause()
    {
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
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        ownsPause = false;
        confirmingNewGame = false;
        restartError = null;
        IsPaused = false;
        resumeFrame = Time.frameCount;
    }

    private void OnDisable() => Resume();

    private void StartNewGame()
    {
        Scene scene = SceneManager.GetActiveScene();
        // Validate before deleting anything: the level must be reloadable.
        if (scene.buildIndex < 0 || !Application.CanStreamedLevelBeLoaded(scene.buildIndex))
        {
            restartError = "Add this scene to the Build Profiles scene list first.";
            return;
        }
        PlayerMovement.ClearSavedCheckpoint(scene.path);
        Resume();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnGUI()
    {
        if (!ownsPause) return;
        float width = Mathf.Min(360f, Screen.width - 20f);
        Rect panel = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - 260f) * 0.5f, width, 260f);
        if (confirmingNewGame)
        {
            GUI.Box(panel, "Start a new game?");
            GUI.Label(new Rect(panel.x + 20f, panel.y + 35f, width - 40f, 55f),
                "Clear the saved checkpoint for this level\nand restart from the beginning?");
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
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 105f, width - 50f, 45f), "New Game"))
            confirmingNewGame = true;
        if (GUI.Button(new Rect(panel.x + 25f, panel.y + 165f, width - 50f, 45f), "Quit"))
        {
            Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
