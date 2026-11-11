using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Makes one prefab per box Id (at the exact size of its rack slot) and one copy per slot.
// The boxes now use the CTB_Box_Square / CTB_Box_Long / CTB_Tank models instead of plain cubes and cylinders.
//
// Every box is built from TWO meshes:
//   Body = dark metal, one shared material for every box.
//   Rim  = the glowing bands. It has its own material per zone (Rim_FOOD, Rim_WATER, ...),
//          so changing a rim color is a one-material change and never touches the model.
public class CargoBoxMaker
{
    // ------------------------------------------------------------------
    //  Things you may want to change
    // ------------------------------------------------------------------

    // These are round tanks (they get the tank model)
    static HashSet<string> roundOnes = new HashSet<string> { "GAS-01", "GAS-02", "GAS-03" };

    // Give a single box Id its own rim color (it then gets its own material instead of its zone's).
    // Example:  { "EMER-01", new Color(1f, 0.1f, 0.1f) }
    static Dictionary<string, Color> rimOverrides = new Dictionary<string, Color>
    {
    };

    static readonly Color bodyColor = new Color(0.30f, 0.33f, 0.37f);
    const float rimGlow = 1.6f;           // how bright the rim glows. 0 = no glow
    const float longBoxRatio = 1.35f;     // flat/long slots (longest side / shortest side) use the long box model

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

    const string boxFolder = "Assets/Prefabs/CargoBoxes";
    const string meshFolder = "Assets/Prefabs/CargoBoxes/Meshes";
    const string matFolder = "Assets/Prefabs/CargoBoxes/Materials";

    // ------------------------------------------------------------------
    //  Model description
    // ------------------------------------------------------------------

    // A model keeps its rim, pockets and edges the same thickness at every size. Only the flat wall
    // between "zone lo" and "zone hi" is stretched ("9-slice" for 3D). Zone numbers are in meters,
    // measured from the middle of the model, per axis (x = along shelf, y = up, z = depth).
    class Model
    {
        public string name;
        public bool[] zoned = new bool[3];
        public float[] lo = new float[3];
        public float[] hi = new float[3];
        public Mesh body;
        public Mesh rim;
    }

    static Model MakeModel(string name, bool xz, float xlo, float xhi, bool yz, float ylo, float yhi, bool zz, float zlo, float zhi)
    {
        Model m = new Model();
        m.name = name;
        m.zoned[0] = xz; m.lo[0] = xlo; m.hi[0] = xhi;
        m.zoned[1] = yz; m.lo[1] = ylo; m.hi[1] = yhi;
        m.zoned[2] = zz; m.lo[2] = zlo; m.hi[2] = zhi;
        return m;
    }

    static int Find(List<int> parent, int x)
    {
        while (parent[x] != x)
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }
        return x;
    }

    // Splits a mesh into its separate closed shells (pieces that share no vertex positions).
    // The biggest shell is the Body, everything else is the Rim bands.
    // This does not depend on how Unity's importer groups or names things.
    static bool SplitShells(Mesh src, out int[] bodyTris, out int[] rimTris)
    {
        int[] tris = src.triangles;
        Vector3[] v = src.vertices;

        Dictionary<Vector3Int, int> ids = new Dictionary<Vector3Int, int>();
        List<int> parent = new List<int>();
        int[] vid = new int[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            Vector3Int key = new Vector3Int(Mathf.RoundToInt(v[i].x * 100000f), Mathf.RoundToInt(v[i].y * 100000f), Mathf.RoundToInt(v[i].z * 100000f));
            int id;
            if (!ids.TryGetValue(key, out id))
            {
                id = parent.Count;
                ids[key] = id;
                parent.Add(id);
            }
            vid[i] = id;
        }

        for (int t = 0; t < tris.Length; t += 3)
        {
            int a = Find(parent, vid[tris[t]]);
            int b = Find(parent, vid[tris[t + 1]]);
            int c = Find(parent, vid[tris[t + 2]]);
            parent[b] = a;
            parent[Find(parent, c)] = Find(parent, a);
        }

        Dictionary<int, int> count = new Dictionary<int, int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int root = Find(parent, vid[tris[t]]);
            int n;
            count.TryGetValue(root, out n);
            count[root] = n + 3;
        }

        int bodyRoot = -1;
        int best = -1;
        foreach (KeyValuePair<int, int> kv in count)
        {
            if (kv.Value > best) { best = kv.Value; bodyRoot = kv.Key; }
        }

        List<int> bl = new List<int>();
        List<int> rl = new List<int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            List<int> target = (Find(parent, vid[tris[t]]) == bodyRoot) ? bl : rl;
            target.Add(tris[t]);
            target.Add(tris[t + 1]);
            target.Add(tris[t + 2]);
        }
        bodyTris = bl.ToArray();
        rimTris = rl.ToArray();
        return bl.Count > 0 && rl.Count > 0;
    }

    // Copies some triangles of a mesh into a new mesh (only the vertices they use)
    static Mesh ExtractTriangles(Mesh src, int[] tris, string meshName)
    {
        Vector3[] sv = src.vertices;
        Vector3[] sn = src.normals;
        Vector2[] su = src.uv;
        bool hasN = sn != null && sn.Length == sv.Length;
        bool hasUV = su != null && su.Length == sv.Length;

        Dictionary<int, int> map = new Dictionary<int, int>();
        List<Vector3> v = new List<Vector3>();
        List<Vector3> n = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        int[] nt = new int[tris.Length];

        for (int i = 0; i < tris.Length; i++)
        {
            int oldIndex = tris[i];
            int newIndex;
            if (!map.TryGetValue(oldIndex, out newIndex))
            {
                newIndex = v.Count;
                map[oldIndex] = newIndex;
                v.Add(sv[oldIndex]);
                if (hasN) n.Add(sn[oldIndex]);
                if (hasUV) uv.Add(su[oldIndex]);
            }
            nt[i] = newIndex;
        }

        Mesh r = new Mesh();
        r.name = meshName;
        if (v.Count > 65000) r.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        r.SetVertices(v);
        if (hasN) r.SetNormals(n);
        if (hasUV) r.SetUVs(0, uv);
        r.SetTriangles(nt, 0);
        r.RecalculateBounds();
        return r;
    }

    // Finds the model file by name anywhere in Assets, makes sure it can be read, loads its Body and Rim meshes
    static bool LoadModel(Model m)
    {
        string path = null;
        foreach (string guid in AssetDatabase.FindAssets(m.name + " t:Model"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == m.name) { path = p; break; }
        }

        if (path == null)
        {
            Debug.LogError("Could not find the model '" + m.name + "'. Put " + m.name + ".obj somewhere under Assets (for example Assets/Models).");
            return false;
        }

        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            bool changed = false;
            if (!importer.isReadable) { importer.isReadable = true; changed = true; }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None) { importer.materialImportMode = ModelImporterMaterialImportMode.None; changed = true; }
            if (importer.importNormals != ModelImporterNormals.Import) { importer.importNormals = ModelImporterNormals.Import; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        m.body = null;
        m.rim = null;
        List<string> found = new List<string>();
        Mesh single = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            Mesh mesh = o as Mesh;
            if (mesh == null) continue;
            found.Add(mesh.name + " (" + mesh.subMeshCount + " submeshes)");
            string n = mesh.name.ToLowerInvariant();
            if (n.Contains("rim")) m.rim = mesh;
            else if (n.Contains("body")) m.body = mesh;
            else single = mesh;
        }

        // Unity's OBJ importer merges everything into ONE mesh called "default",
        // so split it back into the body and the rim bands by shape.
        if ((m.body == null || m.rim == null) && single != null)
        {
            if (!single.isReadable)
            {
                Debug.LogError("The model '" + m.name + "' is not readable. Select it in the Project window, tick Read/Write in the Model tab, click Apply, then run this again.");
                return false;
            }
            int[] bodyTris;
            int[] rimTris;
            if (SplitShells(single, out bodyTris, out rimTris))
            {
                m.body = ExtractTriangles(single, bodyTris, "Body");
                m.rim = ExtractTriangles(single, rimTris, "Rim");
                return true;
            }
        }

        if (m.body == null || m.rim == null)
        {
            Debug.LogError("The model '" + m.name + "' needs a Body part and a Rim part, but it has: " + string.Join(", ", found.ToArray()));
            return false;
        }

        if (!m.body.isReadable || !m.rim.isReadable)
        {
            Debug.LogError("The model '" + m.name + "' is not readable. Select it in the Project window, tick Read/Write in the Model tab, click Apply, then run this again.");
            return false;
        }
        return true;
    }

    // ------------------------------------------------------------------
    //  Fitting a model to a slot size
    // ------------------------------------------------------------------

    static Vector3 RotY90(Vector3 v)
    {
        return new Vector3(v.z, v.y, -v.x);
    }

    static float MapAxis(float c, float mn, float mx, float size, bool zoned, float lo, float hi, float u, float border, out float slope)
    {
        if (!zoned)
        {
            slope = size / (mx - mn);
            return -size / 2f + (c - mn) * slope;
        }
        if (c <= lo)
        {
            slope = u;
            return -size / 2f + (c - mn) * u;
        }
        if (c >= hi)
        {
            slope = u;
            return size / 2f - (mx - c) * u;
        }
        slope = (size - u * border) / (hi - lo);
        return -size / 2f + (lo - mn) * u + (c - lo) * slope;
    }

    static Mesh Remap(Mesh src, Vector3[] verts, Vector3[] normals, Vector3 mn, Vector3 mx, Vector3 size,
                      bool[] zoned, float[] lo, float[] hi, float u, float[] border, string meshName)
    {
        Vector3[] nv = new Vector3[verts.Length];
        Vector3[] nn = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = verts[i];
            Vector3 q = Vector3.zero;
            Vector3 slope = Vector3.one;
            for (int a = 0; a < 3; a++)
            {
                float s;
                q[a] = MapAxis(p[a], mn[a], mx[a], size[a], zoned[a], lo[a], hi[a], u, border[a], out s);
                slope[a] = s;
            }
            nv[i] = q;

            if (normals != null && normals.Length == verts.Length)
            {
                // the normal of a stretched surface tilts by 1 / (stretch on each axis)
                Vector3 n = normals[i];
                n = new Vector3(n.x / slope.x, n.y / slope.y, n.z / slope.z);
                nn[i] = n.normalized;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = nv;
        mesh.triangles = src.triangles;     // a stretch (or a 90 degree turn) never flips the winding
        if (src.uv != null && src.uv.Length == verts.Length) mesh.uv = src.uv;
        if (normals != null && normals.Length == verts.Length) mesh.normals = nn;
        else mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Makes new Body and Rim meshes of exactly 'size' (along shelf, height, depth), centered on the origin
    static void Fit(Model m, Vector3 size, bool rotate, string id, out Mesh body, out Mesh rim)
    {
        Vector3[] bv = m.body.vertices;
        Vector3[] rv = m.rim.vertices;
        Vector3[] bn = m.body.normals;
        Vector3[] rn = m.rim.normals;

        bool[] zoned = (bool[])m.zoned.Clone();
        float[] lo = (float[])m.lo.Clone();
        float[] hi = (float[])m.hi.Clone();

        if (rotate)
        {
            for (int i = 0; i < bv.Length; i++) bv[i] = RotY90(bv[i]);
            for (int i = 0; i < rv.Length; i++) rv[i] = RotY90(rv[i]);
            if (bn != null) for (int i = 0; i < bn.Length; i++) bn[i] = RotY90(bn[i]);
            if (rn != null) for (int i = 0; i < rn.Length; i++) rn[i] = RotY90(rn[i]);

            bool tz = zoned[0]; zoned[0] = zoned[2]; zoned[2] = tz;
            float tl = lo[0]; lo[0] = lo[2]; lo[2] = tl;
            float th = hi[0]; hi[0] = hi[2]; hi[2] = th;
        }

        // size of the model (body + rim together)
        Vector3 mn = bv[0];
        Vector3 mx = bv[0];
        for (int i = 0; i < bv.Length; i++) { mn = Vector3.Min(mn, bv[i]); mx = Vector3.Max(mx, bv[i]); }
        for (int i = 0; i < rv.Length; i++) { mn = Vector3.Min(mn, rv[i]); mx = Vector3.Max(mx, rv[i]); }
        Vector3 centre = (mn + mx) / 2f;

        // zone limits are measured from the middle of the model
        float[] zlo = new float[3];
        float[] zhi = new float[3];
        float[] border = new float[3];
        float u = 1f;     // 1 = the rim is drawn at its real size. Gets smaller only if the box is too small for that.
        for (int a = 0; a < 3; a++)
        {
            zlo[a] = centre[a] + lo[a];
            zhi[a] = centre[a] + hi[a];
            if (!zoned[a])
            {
                u = Mathf.Min(u, size[a] / (mx[a] - mn[a]));
            }
            else
            {
                border[a] = (zlo[a] - mn[a]) + (mx[a] - zhi[a]);
                u = Mathf.Min(u, 0.85f * size[a] / border[a]);
            }
        }

        body = Remap(m.body, bv, bn, mn, mx, size, zoned, zlo, zhi, u, border, id + "_Body");
        rim = Remap(m.rim, rv, rn, mn, mx, size, zoned, zlo, zhi, u, border, id + "_Rim");
    }

    // ------------------------------------------------------------------
    //  Assets
    // ------------------------------------------------------------------

    static void MakeFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    static Mesh SaveMesh(Mesh fresh, string path)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }
        AssetDatabase.CreateAsset(fresh, path);
        return fresh;
    }

    static void SetFloatIfThere(Material m, string prop, float value)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, value);
    }

    // Loads a material, or makes it from the default material (works with the built-in and URP/HDRP shaders)
    static Material GetMaterial(string name, Color color, bool glow, float metallic, float smoothness, Dictionary<string, Material> cache)
    {
        if (cache.ContainsKey(name)) return cache[name];

        string path = matFolder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = (m == null);
        if (isNew)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
            Object.DestroyImmediate(tmp);
            m.name = name;
        }

        m.color = color;
        SetFloatIfThere(m, "_Metallic", metallic);
        SetFloatIfThere(m, "_Glossiness", smoothness);   // built-in render pipeline
        SetFloatIfThere(m, "_Smoothness", smoothness);   // URP / HDRP

        if (glow && rimGlow > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * rimGlow);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (isNew) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);

        cache[name] = m;
        return m;
    }

    // One prefab per box Id, at the exact size of its slot
    static GameObject MakePrefab(string id, string zone, Vector3 size, float itemMass,
                                 Model square, Model longBox, Model tank, Dictionary<string, Material> mats)
    {
        bool round = roundOnes.Contains(id);

        // size = (along shelf, height, depth). Round tanks: x and z are the width, y is the length.
        Model model;
        bool rotate = false;
        if (round)
        {
            model = tank;
        }
        else
        {
            float along = size.x;
            float depth = size.z;
            float ratio = Mathf.Max(along, depth) / Mathf.Max(0.0001f, Mathf.Min(along, depth));
            if (ratio < longBoxRatio)
            {
                model = square;
            }
            else
            {
                model = longBox;
                rotate = depth > along;     // the long side goes along the deeper direction
            }
        }

        Mesh body;
        Mesh rim;
        Fit(model, size, rotate, id, out body, out rim);
        body = SaveMesh(body, meshFolder + "/" + id + "_Body.asset");
        rim = SaveMesh(rim, meshFolder + "/" + id + "_Rim.asset");

        // rim color: this box's own override, or its zone's color
        string rimKey = zone;
        Color rimColor = ZoneColor(zone);
        Color over;
        if (rimOverrides.TryGetValue(id, out over))
        {
            rimKey = id;
            rimColor = over;
        }

        Material bodyMat = GetMaterial("CargoBody", bodyColor, false, 0.45f, 0.55f, mats);
        Material rimMat = GetMaterial("Rim_" + rimKey, rimColor, true, 0.1f, 0.4f, mats);

        GameObject go = new GameObject(id);
        go.AddComponent<MeshFilter>().sharedMesh = body;
        go.AddComponent<MeshRenderer>().sharedMaterial = bodyMat;

        GameObject rimObj = new GameObject("Rim");
        rimObj.transform.SetParent(go.transform, false);
        rimObj.AddComponent<MeshFilter>().sharedMesh = rim;
        rimObj.AddComponent<MeshRenderer>().sharedMaterial = rimMat;

        // one box collider around everything (stable for stacking on shelves and for picking up)
        Bounds b = body.bounds;
        b.Encapsulate(rim.bounds);
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = b.center;
        bc.size = b.size;

        go.AddComponent<ItemInfo>().itemId = id;
        go.AddComponent<Rigidbody>().mass = itemMass;

        string prefabPath = boxFolder + "/" + id + ".prefab";
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

        // the three models (zone numbers are in meters from the middle of each model)
        Model square = MakeModel("CTB_Box_Square", true, -0.100f, 0.100f, true, -0.105f, 0.005f, true, -0.100f, 0.100f);
        Model longBox = MakeModel("CTB_Box_Long", true, -0.325f, 0.325f, true, -0.105f, 0.005f, true, -0.100f, 0.100f);
        Model tank = MakeModel("CTB_Tank", false, 0f, 0f, true, -0.275f, -0.0875f, false, 0f, 0f);
        if (!LoadModel(square) || !LoadModel(longBox) || !LoadModel(tank)) return;

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
        MakeFolder(boxFolder, "Materials");
        MakeFolder(boxFolder, "Meshes");

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
                prefab = MakePrefab(s.boxId, s.zone, s.size, itemMass, square, longBox, tank, mats);
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
        Debug.Log("Made " + rb.slots.Count + " cargo boxes for " + rb.slots.Count + " slots. The prefabs are in " + boxFolder);
    }
}
