using UnityEngine.InputSystem;

// Every button choice lives here, so you only change a button in one place.
public static class GameControls
{
    // ---------- ONE PRESS ----------

    // M  or  + button
    public static bool ManifestPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.mKey.wasPressedThisFrame)
            || (p != null && p.startButton.wasPressedThisFrame);
    }

    // Tab  or  - button
    public static bool SwitchAppPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.tabKey.wasPressedThisFrame)
            || (p != null && p.selectButton.wasPressedThisFrame);
    }

    // ]  or  R
    public static bool NextPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.rightBracketKey.wasPressedThisFrame)
            || (p != null && p.rightShoulder.wasPressedThisFrame);
    }

    // [  or  L
    public static bool PreviousPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.leftBracketKey.wasPressedThisFrame)
            || (p != null && p.leftShoulder.wasPressedThisFrame);
    }

    // F  or  Y (left button)
    public static bool FindPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.fKey.wasPressedThisFrame)
            || (p != null && p.buttonWest.wasPressedThisFrame);
    }

    // Q  or  ZR
    public static bool ScanInfoPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.qKey.wasPressedThisFrame)
            || (p != null && p.rightTrigger.wasPressedThisFrame);
    }

    // T  or  ZL
    public static bool StoreLocationPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.tKey.wasPressedThisFrame)
            || (p != null && p.leftTrigger.wasPressedThisFrame);
    }

    // ---------- HELD DOWN (rotating the box) ----------

    public static bool TurnRightHeld()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.rightArrowKey.isPressed)
            || (p != null && p.dpad.right.isPressed);
    }

    public static bool TurnLeftHeld()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.leftArrowKey.isPressed)
            || (p != null && p.dpad.left.isPressed);
    }

    public static bool TipUpHeld()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.upArrowKey.isPressed)
            || (p != null && p.dpad.up.isPressed);
    }

    public static bool TipDownHeld()
    {
        Keyboard k = Keyboard.current;
        Gamepad p = Gamepad.current;
        return (k != null && k.downArrowKey.isPressed)
            || (p != null && p.dpad.down.isPressed);
    }
}