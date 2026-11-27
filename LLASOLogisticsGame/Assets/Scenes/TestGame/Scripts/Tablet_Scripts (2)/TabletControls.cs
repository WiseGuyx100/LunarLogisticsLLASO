using UnityEngine.InputSystem;

// All the tablet buttons live here, so you never have to edit GameControls.
// Tablet-only buttons start with  TabletToggle.IsOut &&  so they do nothing while the tablet is away.
public static class TabletControls
{
    // 2   (keyboard only for now - every controller face button is already used)
    public static bool TogglePressed()
    {
        Keyboard k = Keyboard.current;
        return k != null && k.digit2Key.wasPressedThisFrame;
    }

    // ---------- TABLET ONLY (work only while the tablet is out) ----------
    // To add a new tablet button: copy one and keep the  TabletToggle.IsOut &&  part.

    // Tab  or  - button
    public static bool SwitchAppPressed()
    {
        return TabletToggle.IsOut && GameControls.SwitchAppPressed();
    }

    // ]  or  R
    public static bool NextPressed()
    {
        return TabletToggle.IsOut && GameControls.NextPressed();
    }

    // [  or  L
    public static bool PreviousPressed()
    {
        return TabletToggle.IsOut && GameControls.PreviousPressed();
    }

    // Arrow keys  or  D-pad  (move the highlight between the tablet's buttons)
    public static bool NavLeftPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return TabletToggle.IsOut
            && ((k != null && k.leftArrowKey.wasPressedThisFrame)
            || (p != null && p.dpad.left.wasPressedThisFrame));
    }

    public static bool NavRightPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return TabletToggle.IsOut
            && ((k != null && k.rightArrowKey.wasPressedThisFrame)
            || (p != null && p.dpad.right.wasPressedThisFrame));
    }

    public static bool NavUpPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return TabletToggle.IsOut
            && ((k != null && k.upArrowKey.wasPressedThisFrame)
            || (p != null && p.dpad.up.wasPressedThisFrame));
    }

    public static bool NavDownPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return TabletToggle.IsOut
            && ((k != null && k.downArrowKey.wasPressedThisFrame)
            || (p != null && p.dpad.down.wasPressedThisFrame));
    }
}
