using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CargoBoxMaker
{
    // These are round tanks (cylinders)
    static HashSet<string> roundOnes = new HashSet<string> { "GAS-01", "GAS-02", "GAS-03" };

    static Color ZoneColor(string zone)
    {
        switch (zone)
        {
            case "FOOD": return new Color(0.90f, 0.55f, 0.20f);
            case "WATER": return new Color(0.20f, 0.55f, 0.90f);
            case "GAS": return new Color(0.55f, 0.45f, 0.90f);
            case "CREW": return new Color(0.90f, 0.80f, 0.25f);
            case "MEDICAL": return new Color(0.95f, 0.95f, 0.95f);
            case "WASTE": return new Color(0.45f, 0.40f, 0.30f);
            case "EMERGENCY": return new Color(0.90f, 0.15f, 0.15f);
            case "EVA": return new Color(0.85f, 0.25f, 0.50f);
            case "MAINT": return new Color(0.55f, 0.58f, 0.62f);
            case "SCIENCE": return new Color(0.20f, 0.65f, 0.62f);
        }
        return Color.gray;
    }

    static void MakeFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    // One prefab per box Id, at the exact size of its slot
    static GameObject MakePrefab(string id, string zone, Vector3 size, float itemMass, Dictionary<string, Material> mats)
    {
        bool round = roundOnes.Contains(id);

        // size = (along shelf, height, depth). Round tanks: x and z are the width, y is the length.
        GameObject go = GameObject.CreatePrimitive(round ? PrimitiveType.Cylinder : PrimitiveType.Cube);
        go.name = id;
        go.transform.localScale = round ? new Vector3(size.x, size.y / 2f, size.z) : size;

        if (round)
        {
            // A cylinder is 2 m tall at scale 1, so use a box collider that matches
            Object.DestroyImmediate(go.GetComponent<Collider>());
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.center = Vector3.zero;
            bc.size = new Vector3(1f, 2f, 1f);
        }

        if (!mats.ContainsKey(zone))
        {
            string matPath = "Assets/Prefabs/CargoBoxes/Materials/" + zone + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null)
            {
                m = new Material(go.GetComponent<Renderer>().sharedMaterial);
                m.color = ZoneColor(zone);
                AssetDatabase.CreateAsset(m, matPath);
            }
            mats[zone] = m;
        }
        go.GetComponent<Renderer>().sharedMaterial = mats[zone];

        go.AddComponent<ItemInfo>().itemId = id;
        go.AddComponent<Rigidbody>().mass = itemMass;

        string prefabPath = "Assets/Prefabs/CargoBoxes/" + id + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    [MenuItem("Tools/Make Cargo Boxes")]
    static void Make()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop the game first, then run this again.");
            return;
        }

        RackBuilder rb = Object.FindAnyObjectByType<RackBuilder>();
        if (rb == null || rb.slots.Count == 0)
        {
            Debug.LogError("Build the racks first: click the three dots on RackBuilder, then '1. Build racks and slots'.");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>("Data/LunarInventory");
        if (csv == null)
        {
            Debug.LogError("Could not find Assets/Resources/Data/LunarInventory");
            return;
        }

        // mass of one item, by Id
        Dictionary<string, float> masses = new Dictionary<string, float>();
        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;
            string[] c = line.Split(',');
            if (c.Length < 9) continue;

            float mass;
            float.TryParse(c[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out mass);
            masses[c[0].Trim()] = mass;
        }

        foreach (RackBuilder.SlotInfo check in rb.slots)
        {
            if (check.size == Vector3.zero)
            {
                Debug.LogError("The racks were built with an older RackBuilder. Rebuild them (menu item 1), then run this again.");
                return;
            }
        }

        // start fresh
        GameObject old = GameObject.Find("CargoBoxes");
        if (old != null)
        {
            Debug.Log("Replacing the old CargoBoxes object.");
            Undo.DestroyObjectImmediate(old);
        }

        MakeFolder("Assets", "Prefabs");
        MakeFolder("Assets/Prefabs", "CargoBoxes");
        MakeFolder("Assets/Prefabs/CargoBoxes", "Materials");

        GameObject root = new GameObject("CargoBoxes");
        Undo.RegisterCreatedObjectUndo(root, "Make Cargo Boxes");

        // how many boxes of each Id there are (for the names)
        Dictionary<string, int> totals = new Dictionary<string, int>();
        foreach (RackBuilder.SlotInfo s in rb.slots)
        {
            if (!totals.ContainsKey(s.boxId)) totals[s.boxId] = 0;
            totals[s.boxId]++;
        }

        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        Dictionary<string, Material> mats = new Dictionary<string, Material>();
        Dictionary<string, int> counters = new Dictionary<string, int>();

        for (int i = 0; i < rb.slots.Count; i++)
        {
            RackBuilder.SlotInfo s = rb.slots[i];

            float itemMass = 1f;
            masses.TryGetValue(s.boxId, out itemMass);

            GameObject prefab;
            if (!prefabs.TryGetValue(s.boxId, out prefab))
            {
                prefab = MakePrefab(s.boxId, s.zone, s.size, itemMass, mats);
                prefabs[s.boxId] = prefab;
            }

            if (!groups.ContainsKey(s.zone))
            {
                GameObject g = new GameObject(s.zone);
                g.transform.SetParent(root.transform, false);
                groups[s.zone] = g.transform;
            }

            if (!counters.ContainsKey(s.boxId)) counters[s.boxId] = 0;
            counters[s.boxId]++;

            GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            copy.transform.SetParent(groups[s.zone], false);
            copy.name = s.boxId + " (" + counters[s.boxId] + " of " + totals[s.boxId] + ")";

            float x = (i % 8) * 1.5f;
            float z = (i / 8) * 1.5f;
            copy.transform.localPosition = new Vector3(x, s.size.y / 2f + 0.02f, z);

            ItemInfo info = copy.GetComponent<ItemInfo>();
            info.itemId = s.boxId;
            info.units = s.units;
            info.day = s.day;
            info.slotNumber = i;

            Rigidbody rbody = copy.GetComponent<Rigidbody>();
            if (rbody != null) rbody.mass = Mathf.Max(0.1f, itemMass * s.units);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Made " + rb.slots.Count + " cargo boxes for " + rb.slots.Count + " slots. The prefabs are in Assets/Prefabs/CargoBoxes");
    }
}