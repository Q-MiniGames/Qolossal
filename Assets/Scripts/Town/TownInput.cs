using UnityEngine;
using UnityEngine.InputSystem;

// A dialogue box, shop or listening spot is open: Qori's own controls are off
// (GamePauseMenu.BlocksGameplayInput) and Escape closes the window instead of pausing. Stays
// blocking for a frame after closing, so the key that closed it doesn't also jump.
public static class ModalUi
{
    static int open, closedFrame = -10;
    public static bool IsOpen => open > 0 || Time.frameCount <= closedFrame + 1;
    public static void Open() => open++;
    public static void Close() { open = Mathf.Max(0, open - 1); closedFrame = Time.frameCount; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { open = 0; closedFrame = -10; }
}

// The town's keys. Up (W, the up arrow, the d-pad) talks, sits and shops, like entering a portal;
// in a window, Confirm is Space, Enter or E (south button), Cancel is Escape or Backspace (east
// button), and Up and Down move through lists.
public static class TownInput
{
    static Keyboard Key => Keyboard.current;
    static Gamepad Pad => Gamepad.current;

    public static bool Up() =>
        Key != null && (Key.wKey.wasPressedThisFrame || Key.upArrowKey.wasPressedThisFrame)
        || Pad != null && (Pad.dpad.up.wasPressedThisFrame || Pad.leftStick.up.wasPressedThisFrame);

    public static bool Down() =>
        Key != null && (Key.sKey.wasPressedThisFrame || Key.downArrowKey.wasPressedThisFrame)
        || Pad != null && (Pad.dpad.down.wasPressedThisFrame || Pad.leftStick.down.wasPressedThisFrame);

    public static bool Confirm() =>
        Key != null && (Key.spaceKey.wasPressedThisFrame || Key.enterKey.wasPressedThisFrame || Key.eKey.wasPressedThisFrame)
        || Pad != null && Pad.buttonSouth.wasPressedThisFrame;

    public static bool Cancel() =>
        Key != null && (Key.escapeKey.wasPressedThisFrame || Key.backspaceKey.wasPressedThisFrame)
        || Pad != null && (Pad.buttonEast.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame);
}
