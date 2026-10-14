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
    public int currentDay = 14;
    public int maxLines = 12;   // how many items to list before saying "+ more"

    // The "apps" on the screen. Add more names here later.
    private string[] appNames = { "MANIFEST", "TRACKER" };
    private int appIndex = 0;

    private bool panelOpen = false;
    private float refreshTimer = 0f;
    private int selectedIndex = 0;

    void Start()
    {
        canvasGroup.alpha = 0f;
        currentDay = totalDays;   // start on the last day, because it loads first
        Refresh();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // M opens and closes the screen
        if (kb.mKey.wasPressedThisFrame)
        {
            panelOpen = !panelOpen;
            canvasGroup.alpha = panelOpen ? 1f : 0f;
            if (panelOpen) Refresh();
        }

        if (!panelOpen) return;

        // Tab switches to the next app
        if (kb.tabKey.wasPressedThisFrame)
        {
            appIndex = (appIndex + 1) % appNames.Length;
            Refresh();
        }

        // Day keys only work in the Manifest app
        if (appIndex == 0)
        {
            // ] goes to the NEXT day to load (14, 13, 12 ... 1)
            if (kb.rightBracketKey.wasPressedThisFrame)
            {
                currentDay = Mathf.Max(1, currentDay - 1);
                Refresh();
            }
            // [ goes back to the PREVIOUS day to load
            if (kb.leftBracketKey.wasPressedThisFrame)
            {
                currentDay = Mathf.Min(totalDays, currentDay + 1);
                Refresh();
            }
        }

        // Tracker app: pick an item and find it
        if (appIndex == 1 && ItemTracker.Instance != null)
        {
            List<TrackedRecord> list = ItemTracker.Instance.GetRecords();
            int count = list.Count;

            if (count > 0)
            {
                // ] picks the next item, [ picks the one before
                if (kb.rightBracketKey.wasPressedThisFrame)
                {
                    selectedIndex = (selectedIndex + 1) % count;
                    Refresh();
                }
                if (kb.leftBracketKey.wasPressedThisFrame)
                {
                    selectedIndex = (selectedIndex - 1 + count) % count;
                    Refresh();
                }

                // F turns the beam on or off for the picked item
                if (kb.fKey.wasPressedThisFrame)
                {
                    selectedIndex = Mathf.Clamp(selectedIndex, 0, count - 1);
                    ItemTracker.Instance.ToggleHighlight(list[selectedIndex].item);
                    Refresh();
                }
            }

            // The Tracker app updates itself twice a second
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.5f)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }
    }

    void Refresh()
    {
        if (InventoryDatabase.Instance == null) return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(BuildTabBar());
        sb.AppendLine();

        if (appIndex == 0) BuildManifestPage(sb);
        else BuildTrackerPage(sb);

        manifestText.text = sb.ToString();
    }

    // The row of app names at the top
    string BuildTabBar()
    {
        StringBuilder bar = new StringBuilder();
        for (int i = 0; i < appNames.Length; i++)
        {
            if (i == appIndex) bar.Append("<b>[ " + appNames[i] + " ]</b>");
            else bar.Append(appNames[i]);

            bar.Append("    ");
        }
        bar.Append("(Tab = switch app)");
        return bar.ToString();
    }

    // ---------- APP 1: MANIFEST ----------
    void BuildManifestPage(StringBuilder sb)
    {
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

        sb.AppendLine("<b>MISSION MANIFEST - LOAD STEP " + (totalDays - currentDay + 1) + " of " + totalDays + "</b>");
        sb.AppendLine("<b>DAY " + currentDay + "</b>");
        sb.AppendLine("Press ] for the next load day, [ to go back");
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
                sb.AppendLine(d.id + "  |  " + ShortName(d.itemName));
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
    }

    // ---------- APP 2: TRACKER ----------
    void BuildTrackerPage(StringBuilder sb)
    {
        sb.AppendLine("<b>ITEM TRACKER</b>");

        if (ItemTracker.Instance == null)
        {
            sb.AppendLine("Tracker is not set up in the scene.");
            return;
        }

        List<TrackedRecord> list = ItemTracker.Instance.GetRecords();

        sb.AppendLine("Items tracked: " + list.Count);
        sb.AppendLine("Press ] and [ to pick an item, F to find it");
        sb.AppendLine();

        if (list.Count == 0)
        {
            sb.AppendLine("Nothing scanned yet.");
            sb.AppendLine("Place a box in the cargo hold, take out the scanner,");
            sb.AppendLine("aim at the box and press T.");
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, list.Count - 1);

        // Scroll so the picked item is always on screen
        int start = Mathf.Max(0, selectedIndex - maxLines + 1);
        int end = Mathf.Min(list.Count, start + maxLines);

        for (int i = start; i < end; i++)
        {
            TrackedRecord r = list[i];

            string line = (i == selectedIndex ? "> " : "    ") +
                r.id + "  |  " + ShortName(r.itemName) + "  |  " + r.location;

            // Box was moved after it was scanned?
            if (ItemTracker.Instance.CurrentLocation(r) != r.location)
            {
                line += "  (MOVED - rescan)";
            }

            // Beam is pointing at this one?
            if (ItemTracker.Instance.Highlighted == r.item)
            {
                line += "  (FINDING)";
            }

            sb.AppendLine(line);
        }

        if (end < list.Count)
        {
            sb.AppendLine("+ " + (list.Count - end) + " more below");
        }
    }

    string ShortName(string name)
    {
        if (name.Length > 45) return name.Substring(0, 45) + "...";
        return name;
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

        string f = d.fragility.ToLower();
        if (f == "fragile" || f == "veryfragile") return "Astronaut";

        string h = d.hazard.ToLower();
        if (h == "medication" || h == "biohazard") return "Astronaut";

        return "Robot";
    }
}