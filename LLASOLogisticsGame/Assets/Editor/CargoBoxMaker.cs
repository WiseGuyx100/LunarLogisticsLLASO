using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CargoBoxMaker
{
    // Real size of each box in METERS: x = width, y = height, z = depth.
    // For round tanks: x = width, y = length, z = width.
    static Dictionary<string, Vector3> sizes = new Dictionary<string, Vector3>
    {
        { "FOOD-01",  new Vector3(0.30f, 0.103f, 0.20f) },
        { "FOOD-02",  new Vector3(0.30f, 0.127f, 0.20f) },
        { "FOOD-03",  new Vector3(0.30f, 0.103f, 0.20f) },
        { "WATER-01", new Vector3(0.40f, 0.41f, 0.40f) },
        { "WATER-02", new Vector3(0.40f, 0.41f, 0.40f) },
        { "GAS-01",   new Vector3(0.55f, 1.24f, 0.55f) },
        { "GAS-02",   new Vector3(0.55f, 1.24f, 0.55f) },
        { "GAS-03",   new Vector3(0.55f, 1.24f, 0.55f) },
        { "CREW-01",  new Vector3(0.40f, 0.28f, 0.30f) },
        { "CREW-02",  new Vector3(0.35f, 0.32f, 0.25f) },
        { "CREW-03",  new Vector3(0.30f, 0.122f, 0.20f) },
        { "CREW-04",  new Vector3(0.40f, 0.266f, 0.30f) },
        { "MED-01",   new Vector3(0.25f, 0.136f, 0.20f) },
        { "WASTE-01", new Vector3(0.35f, 0.258f, 0.25f) },
        { "WASTE-02", new Vector3(0.20f, 0.06f, 0.15f) },
        { "EMER-01",  new Vector3(0.15f, 0.138f, 0.15f) },
        { "EVA-01",   new Vector3(0.60f, 1.10f, 0.48f) },
        { "EVA-02",   new Vector3(0.45f, 0.342f, 0.35f) },
        { "EVA-03",   new Vector3(0.40f, 0.374f, 0.30f) },
        { "EVA-04",   new Vector3(0.30f, 0.24f, 0.25f) },
        { "EVA-05",   new Vector3(0.30f, 0.15f, 0.20f) },
        { "MAINT-01", new Vector3(0.40f, 0.224f, 0.30f) },
        { "MAINT-02", new Vector3(0.30f, 0.24f, 0.25f) },
        { "MAINT-03", new Vector3(0.40f, 0.18f, 0.20f) },
        { "MAINT-04", new Vector3(0.30f, 0.15f, 0.20f) },
        { "POWER-01", new Vector3(0.30f, 0.18f, 0.20f) },
        { "FAB-01",   new Vector3(0.40f, 0.171f, 0.25f) },
        { "SCI-01",   new Vector3(0.40f, 0.215f, 0.25f) },
        { "SCI-02",   new Vector3(0.30f, 0.18f, 0.20f) },
        { "SCI-03",   new Vector3(0.40f, 0.18f, 0.20f) },
        { "SCI-04",   new Vector3(0.30f, 0.18f, 0.30f) },
        { "COLD-01",  new Vector3(0.516f, 0.274f, 0.544f) },
    };

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

    [MenuItem("Tools/Make Cargo Boxes")]
    static void Make()
    {
        if (GameObject.Find("CargoBoxes") != null)
        {
            Debug.LogWarning("There is already a CargoBoxes object in the scene. Delete it first, then run this again.");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>("Data/LunarInventory");
        if (csv == null)
        {
            Debug.LogError("Could not find Assets/Resources/Data/LunarInventory");
            return;
        }

        MakeFolder("Assets", "Prefabs");
        MakeFolder("Assets/Prefabs", "CargoBoxes");
        MakeFolder("Assets/Prefabs/CargoBoxes", "Materials");

        GameObject root = new GameObject("CargoBoxes");
        Undo.RegisterCreatedObjectUndo(root, "Make Cargo Boxes");

        Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        Dictionary<string, Material> mats = new Dictionary<string, Material>();

        string[] lines = csv.text.Split('\n');
        int count = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;

            string[] c = line.Split(',');
            if (c.Length < 9) continue;

            string id = c[0].Trim();
            string zone = c[7].Trim();
            float mass = float.Parse(c[3], CultureInfo.InvariantCulture);

            if (!sizes.ContainsKey(id))
            {
                Debug.LogWarning("No size for " + id + " - skipped.");
                continue;
            }

            Vector3 size = sizes[id];
            bool round = roundOnes.Contains(id);

            // 1. Build the box at its real size
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

            // 2. Color it by zone
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

            // 3. Add your ItemInfo script (with the Id) and real mass
            go.AddComponent<ItemInfo>().itemId = id;
            go.AddComponent<Rigidbody>().mass = mass;

            // 4. Save as a prefab, then remove the loose copy
            string prefabPath = "Assets/Prefabs/CargoBoxes/" + id + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            // 5. Put one copy in the scene, inside its zone group
            if (!groups.ContainsKey(zone))
            {
                GameObject g = new GameObject(zone);
                g.transform.SetParent(root.transform, false);
                groups[zone] = g.transform;
            }

            GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            copy.transform.SetParent(groups[zone], false);
            float x = (count % 8) * 1.5f;
            float z = (count / 8) * 1.5f;
            copy.transform.localPosition = new Vector3(x, size.y / 2f + 0.02f, z);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Made " + count + " cargo boxes. The prefabs are in Assets/Prefabs/CargoBoxes");
    }
}