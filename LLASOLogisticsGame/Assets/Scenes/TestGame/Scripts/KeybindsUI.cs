using System.Text;
using TMPro;
using UnityEngine;

public class KeybindsUI : MonoBehaviour
{
    [Header("Drag these in")]
    public TextMeshProUGUI hintText;
    public CanvasGroup panelGroup;
    public TextMeshProUGUI panelText;

    private bool panelOpen = false;

    void Start()
    {
        hintText.text = "Press K (or L + R) for keybinds";
        panelText.text = BuildText();
        SetPanel(false);
    }

    void Update()
    {
        if (GameControls.KeybindsPressed())
        {
            SetPanel(!panelOpen);
        }
    }

    void SetPanel(bool open)
    {
        panelOpen = open;
        panelGroup.alpha = open ? 1f : 0f;
    }

    // The words on the panel. Change a button name here if you ever move a button.
    string BuildText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<size=44><b>KEYBINDS</b></size>");
        sb.AppendLine();
        sb.AppendLine("<b>ACTION<pos=38%>KEYBOARD<pos=68%>CONTROLLER</b>");
        sb.AppendLine();

        AddLine(sb, "Walk", "W A S D", "Left stick");
        AddLine(sb, "Look around", "Mouse", "Right stick");
        AddLine(sb, "Jump", "Space", "B (bottom)");
        AddLine(sb, "Sprint (hold)", "Left Shift", "Press left stick");
        AddLine(sb, "Crouch", "C", "Press right stick");
        AddLine(sb, "Pick up / drop box", "E", "A (right)");
        AddLine(sb, "Rotate held box", "Arrow keys", "D-pad");
        AddLine(sb, "Scanner out / away", "1", "X (top)");
        AddLine(sb, "Scan for item info", "Q", "ZR");
        AddLine(sb, "Store item location", "T", "ZL");
        AddLine(sb, "Open / close manifest", "M", "+ button");
        AddLine(sb, "Switch app", "Tab", "- button");
        AddLine(sb, "Next / previous", "] and [", "R and L");
        AddLine(sb, "Find picked item", "F", "Y (left)");
        AddLine(sb, "Open / close this panel", "K", "L + R together");

        sb.AppendLine();
        sb.AppendLine("<size=22>Press K (or L + R) again to close</size>");

        return sb.ToString();
    }

    void AddLine(StringBuilder sb, string action, string keyboard, string controller)
    {
        sb.AppendLine(action + "<pos=38%>" + keyboard + "<pos=68%>" + controller);
    }
}