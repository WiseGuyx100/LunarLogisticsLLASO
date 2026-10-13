using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ManifestManager : MonoBehaviour
{
    [Header("Drag these in")]
    public TextMeshProUGUI manifestText;
    public CanvasGroup canvasGroup;

    [Header("Mission")]
    public int totalDays = 14;
    public int currentDay = 1;
    public int maxLines = 12;   // how many items to list before saying "+ more"

    private bool panelOpen = false;

    void Start()
    {
        canvasGroup.alpha = 0f;
        Refresh();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // M opens and closes the manifest
        if (kb.mKey.wasPressedThisFrame)
        {
            panelOpen = !panelOpen;
            canvasGroup.alpha = panelOpen ? 1f : 0f;
            if (panelOpen) Refresh();
        }

        if (!panelOpen) return;

        // [ goes back a day, ] goes forward a day
        if (kb.leftBracketKey.wasPressedThisFrame)
        {
            currentDay = Mathf.Max(1, currentDay - 1);
            Refresh();
        }
        if (kb.rightBracketKey.wasPressedThisFrame)
        {
            currentDay = Mathf.Min(totalDays, currentDay + 1);
            Refresh();
        }
    }

    void Refresh()
    {
        if (InventoryDatabase.Instance == null) return;

        // Sort items into "needed today" and "always on hand"
        List<ItemData> today = new List<ItemData>();
        int alwaysCount = 0;

        foreach (ItemData d in InventoryDatabase.Instance.GetAll())
        {
            if (IsAlways(d.missionDay))
            {
                alwaysCount++;
                continue;
            }

            if (ParseDays(d.missionDay).Contains(currentDay))
            {
                today.Add(d);
            }
        }

        today.Sort((a, b) => string.Compare(a.id, b.id));
        float totalMass = 0f;
        float totalVolume = 0f;
        int astronautCount = 0;
        int robotCount = 0;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>MISSION MANIFEST - DAY " + currentDay + " of " + totalDays + "</b>");
        sb.AppendLine("Press [ and ] to change the day");
        sb.AppendLine();

        int shown = 0;
        foreach (ItemData d in today)
        {
            // The CSV quantity covers all the days the item is needed.
            // Share it evenly across those days.
            int totalQty = d.qty > 0 ? d.qty : 1;
            int daysNeeded = ParseDays(d.missionDay).Count;
            float q = (float)totalQty / daysNeeded;

            totalMass += d.mass * q;
            totalVolume += d.volume * q;

            string who = GetHandler(d);
            if (who == "Astronaut") astronautCount++;
            else robotCount++;

            if (shown < maxLines)
            {
                string name = d.itemName;
                if (name.Length > 30) name = name.Substring(0, 30) + "...";

                sb.AppendLine(d.id + "  |  " + name);

                shown++;
            }
        }

        if (today.Count > maxLines)
        {
            sb.AppendLine("+ " + (today.Count - maxLines) + " more items");
        }

        if (today.Count == 0)
        {
            sb.AppendLine("No items needed this day.");
        }

        sb.AppendLine();
        sb.AppendLine("Items today: " + today.Count +
            "   Mass: " + totalMass.ToString("0.#") + " kg" +
            "   Volume: " + totalVolume.ToString("0.###") + " m3");
        sb.AppendLine("Astronaut: " + astronautCount + "   Robot: " + robotCount);
        sb.AppendLine("Always on hand (reserve / as needed): " + alwaysCount + " items");

        manifestText.text = sb.ToString();
    }

    // "ALL" or "ANY" means the item is not tied to one day
    bool IsAlways(string text)
    {
        string t = text.Trim().ToUpper();
        return t == "ALL" || t == "ANY";
    }

    // Turns "1-14", "3;6;9;12", or "4" into a list of day numbers
    List<int> ParseDays(string text)
    {
        List<int> days = new List<int>();

        foreach (string part in text.Split(';'))
        {
            string p = part.Trim();

            if (p.Contains("-"))
            {
                string[] ends = p.Split('-');
                int a;
                int b;
                if (ends.Length == 2 && int.TryParse(ends[0], out a) && int.TryParse(ends[1], out b))
                {
                    for (int day = a; day <= b; day++) days.Add(day);
                }
            }
            else
            {
                int single;
                if (int.TryParse(p, out single)) days.Add(single);
            }
        }

        return days;
    }

    // Uses the CSV handler if there is one, otherwise a simple rule
    string GetHandler(ItemData d)
    {
        if (d.handler != "-") return d.handler;

        // Fragile things need a careful human
        string f = d.fragility.ToLower();
        if (f == "fragile" || f == "veryfragile") return "Astronaut";

        // Medical and biohazard items need human judgment
        string h = d.hazard.ToLower();
        if (h == "medication" || h == "biohazard") return "Astronaut";

        // Everything else can be moved by a robot
        return "Robot";
    }
}