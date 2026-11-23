using UnityEngine.InputSystem;

// All the tablet buttons live here, so you never have to edit GameControls.
// Tablet-only buttons start with  TabletToggle.IsOut &&  so they do nothing while the tablet is away.
public static class TabletControls
{
    // 2   (keyboard only for now - every controller button is already used)
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
}
