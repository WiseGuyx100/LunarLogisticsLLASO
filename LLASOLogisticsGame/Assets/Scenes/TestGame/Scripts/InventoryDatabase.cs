using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ItemData
{
    public string id;
    public string category;
    public string itemName;
    public float mass;
    public float volume;
    public string storage;
    public string temperature;
    public string requiredZone;
    public string access;

    // NEW columns
    public int qty;
    public string missionDay = "-";
    public string fragility = "-";
    public string hazard = "-";
    public string container = "-";
    public float powerW;
}

public class InventoryDatabase : MonoBehaviour
{
    public static InventoryDatabase Instance;

    public TextAsset csvFile;

    private Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();

    void Awake()
    {
        Instance = this;
        LoadCsv();
    }

    void LoadCsv()
    {
        string[] lines = csvFile.text.Split('\n');

        // Start at 1 to skip the header row
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;

            string[] c = line.Split(',');
            if (c.Length < 9) continue;

            ItemData d = new ItemData();
            d.id = c[0].Trim();
            d.category = c[1].Trim();
            d.itemName = c[2].Trim();
            d.mass = float.Parse(c[3], CultureInfo.InvariantCulture);
            d.volume = float.Parse(c[4], CultureInfo.InvariantCulture);
            d.storage = c[5].Trim();
            d.temperature = c[6].Trim();
            d.requiredZone = c[7].Trim();
            d.access = c[8].Trim();

            // The new columns. Only read them if the file has them.
            if (c.Length >= 15)
            {
                int.TryParse(c[9].Trim(), out d.qty);
                d.missionDay = c[10].Trim();
                d.fragility = c[11].Trim();
                d.hazard = c[12].Trim();
                d.container = c[13].Trim();
                float.TryParse(c[14].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out d.powerW);
            }

            items[d.id] = d;
        }
    }

    public ItemData Get(string id)
    {
        if (items.ContainsKey(id)) return items[id];
        return null;
    }
}