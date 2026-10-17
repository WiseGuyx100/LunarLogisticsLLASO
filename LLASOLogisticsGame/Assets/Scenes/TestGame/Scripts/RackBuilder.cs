using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class RackBuilder : MonoBehaviour
{
    public enum DoorPosition { EndMinusZ, EndPlusZ, WallMinusX, WallPlusX }

    [System.Serializable]
    public class SlotInfo
    {
        public string boxId;
        public string zone;
        public int day;            // 0 = emergency, 15 = any day
        public int units;          // how many items are packed in this box
        public int shelfLevel;     // 0 = bottom
        public Vector3 position;   // inside the CargoGrid object
        public Quaternion rotation = Quaternion.identity;
        public Vector3 size;       // along shelf, height, depth (meters)
    }

    [Header("Grid container (leave empty and it finds it)")]
    public GridContainer grid;

    [Header("Mode")]
    public bool condensed = true;          // ON = items grouped into CTBs (fewer boxes)
    public float packEfficiency = 0.7f;    // how full a CTB gets (by volume)
    public bool fillUsesPrefabs = false;   // fill tool: real prefabs instead of colored blocks

    [Header("Door")]
    public DoorPosition door = DoorPosition.WallPlusX;
    public float doorWidth = 1.3f;
    public float doorCenter = 0f;       // only for a door on a long wall (0 = middle)
    public float doorClearance = 0.15f; // only for a door at an end

    [Header("Racks")]
    public float rackDepth = 0.70f;     // max depth. Auto-capped so the aisle stays at least Min Aisle
    public float minAisle = 0.95f;      // 3 ft = 0.914 m, plus a little extra
    public bool doubleDeep = true;
    public int maxLevels = 6;
    public bool buildFlatFloor = true;
    public float floorY = 0f;
    public bool showZoneSigns = true;
    public bool showSlotPads = true;

    [Header("Fine tuning (you can leave these alone)")]
    public float signSize = 0.008f;
    public float boxGap = 0.03f;
    public float wallGap = 0.005f;
    public float topGap = 0.15f;
    public float sideInset = 0.08f;
    public float endInset = 0.15f;

    [Header("Answer key (filled in when you build)")]
    public List<SlotInfo> slots = new List<SlotInfo>();

    const float boardThickness = 0.03f;
    const float firstBoardTop = 0.06f;
    const float shelfClearance = 0.04f;
    const string rootName = "Racks (built)";
    const string testName = "Test Boxes (side tool)";

    // Container sizes in cm. CTB sizes are from the NASA table (L, W, H).
    // Items marked upright are already (along shelf, height, depth).
    class Spec
    {
        public float a, b, c, volume, mass;
        public int fixedPer;
        public bool upright;
        public Spec(float a, float b, float c, float volume, float mass, int fixedPer, bool upright)
        {
            this.a = a; this.b = b; this.c = c;
            this.volume = volume; this.mass = mass;
            this.fixedPer = fixedPer; this.upright = upright;
        }
    }

    static readonly Dictionary<string, Spec> specs = new Dictionary<string, Spec>
    {
        { "0.5 CTB",   new Spec(42.5f, 24.8f, 23.5f, 0.025f, 7.3f, 0, false) },
        { "1.0 CTB",   new Spec(50.2f, 42.5f, 24.8f, 0.053f, 15.4f, 0, false) },
        { "1U Locker", new Spec(50.2f, 42.5f, 24.8f, 0.053f, 15.4f, 0, false) },
        { "2.0 CTB",   new Spec(50.2f, 42.5f, 50.2f, 0.107f, 31.0f, 0, false) },
        { "2U Locker", new Spec(50.2f, 42.5f, 50.2f, 0.107f, 31.0f, 0, false) },
        { "3.0 CTB",   new Spec(74.9f, 42.5f, 50.2f, 0.160f, 46.4f, 0, false) },
        { "4.0 CTB",   new Spec(89.7f, 50.8f, 53.3f, 0.243f, 70.5f, 0, false) },
        { "6.0 CTB",   new Spec(89.7f, 81.9f, 53.3f, 0.392f, 113.7f, 0, false) },
        { "M01 Bag",   new Spec(55f, 124f, 55f, 0f, 0f, 1, true) },
        { "M02 Bag (3 carriers)", new Spec(75f, 50f, 52f, 0f, 0f, 3, true) },
        { "Suit Rack", new Spec(60f, 110f, 48f, 0f, 0f, 1, true) },
        { "Cold Locker", new Spec(51.6f, 27.4f, 54.4f, 0f, 0f, 1, true) },
        { "Rack Slot (Middeck size)", new Spec(51.6f, 27.4f, 54.4f, 0f, 0f, 1, true) },
    };

    // Sizes in meters (along, height, depth). Used when Condensed is OFF and no prefab is found.
    static readonly Dictionary<string, Vector3> fallbackSizes = new Dictionary<string, Vector3>
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

    class BoxUnit
    {
        public string id;
        public string zone;
        public Vector3 size;     // along shelf, height, depth
        public int units;
        public int day;
        public int order;
    }

    class WallRun
    {
        public float wallSign, startZ, dir, length, used;
    }

    class ZonePlan
    {
        public string key;
        public List<BoxUnit> units = new List<BoxUnit>();
        public float maxH, spacing, length, laneW, start;
        public int levels, lanes;
        public List<int> slotLevel = new List<int>();
        public List<int> slotLane = new List<int>();
        public List<float> slotCenter = new List<float>();
        public WallRun run;
    }

    Material shelfMat;
    Material postMat;
    Dictionary<string, Material> padMats = new Dictionary<string, Material>();

    void Reset()
    {
        grid = FindGrid();
    }

    // hide the red door marker while playing
    void Awake()
    {
        if (grid == null) grid = FindGrid();
        if (grid == null) return;
        Transform m = grid.transform.Find(rootName + "/Door Marker");
        if (m != null) m.gameObject.SetActive(false);
    }

    GridContainer FindGrid()
    {
        GridContainer g = GetComponent<GridContainer>();
        if (g == null) g = FindAnyObjectByType<GridContainer>();
        return g;
    }

    bool DoorAtEnd()
    {
        return door == DoorPosition.EndMinusZ || door == DoorPosition.EndPlusZ;
    }

    float RackDepth()
    {
        // racks on both walls plus the side insets must leave at least minAisle
        float d = Mathf.Min(rackDepth, (grid.containerSize.x - minAisle) / 2f - sideInset);
        if (DoorAtEnd())
        {
            d = Mathf.Min(d, (grid.containerSize.x - doorWidth - 0.04f) / 2f);
        }
        return d;
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        Vector3 s = grid.containerSize;
        Gizmos.matrix = grid.transform.localToWorldMatrix;

        Gizmos.color = Color.red;
        if (door == DoorPosition.EndMinusZ)
            Gizmos.DrawWireCube(new Vector3(0f, floorY + 1.05f, -s.z / 2f), new Vector3(doorWidth, 2.1f, 0.1f));
        else if (door == DoorPosition.EndPlusZ)
            Gizmos.DrawWireCube(new Vector3(0f, floorY + 1.05f, s.z / 2f), new Vector3(doorWidth, 2.1f, 0.1f));
        else if (door == DoorPosition.WallMinusX)
            Gizmos.DrawWireCube(new Vector3(-s.x / 2f, floorY + 1.05f, doorCenter), new Vector3(0.1f, 2.1f, doorWidth));
        else
            Gizmos.DrawWireCube(new Vector3(s.x / 2f, floorY + 1.05f, doorCenter), new Vector3(0.1f, 2.1f, doorWidth));

        Gizmos.color = Color.yellow;
        float d = RackDepth();
        Gizmos.DrawWireCube(new Vector3(0f, floorY + 0.02f, 0f), new Vector3(s.x - 2f * (d + sideInset), 0.04f, s.z));
    }

    // ------------------------------------------------------------------
    //  Menu items (click the three dots at the top of this component)
    // ------------------------------------------------------------------

    [ContextMenu("1. Build racks and slots")]
    public void BuildRacks()
    {
        if (grid == null) grid = FindGrid();
        if (grid == null)
        {
            Debug.LogError("RackBuilder: no GridContainer found in the scene.");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>("Data/LunarInventory");
        if (csv == null)
        {
            Debug.LogError("RackBuilder: could not find Assets/Resources/Data/LunarInventory");
            return;
        }

        RemoveRacks();
        slots.Clear();
        padMats.Clear();

        Vector3 cs = grid.containerSize;
        float depth = RackDepth();
        float aisle = cs.x - 2f * (depth + sideInset);
        Debug.Log("RackBuilder: rack depth " + depth.ToString("F2") + " m, aisle " + aisle.ToString("F2") + " m (" + (aisle / 0.3048f).ToString("F1") + " ft). Needs 3 ft.");
        if (aisle < 0.914f) Debug.LogWarning("RackBuilder: the aisle is narrower than 3 ft!");

        float depthLimit = depth - wallGap;

        // ---- 1. read the spreadsheet ----
        Dictionary<string, ZonePlan> plans = new Dictionary<string, ZonePlan>();
        string[] lines = csv.text.Split('\n');
        int order = 0;
        int itemTotal = 0;
        int boxTotal = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;

            string[] c = line.Split(',');
            if (c.Length < 14) continue;

            string id = c[0].Trim();
            string zone = c[7].Trim();
            string missionDay = c[10].Trim();
            string container = c[13].Trim();

            int qty;
            if (!int.TryParse(c[9].Trim(), out qty)) continue;

            float itemMass;
            float itemVol;
            float.TryParse(c[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out itemMass);
            float.TryParse(c[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out itemVol);

            Vector3 size;
            int per = 1;
            Spec spec;

            if (condensed && specs.TryGetValue(container, out spec))
            {
                size = OrientBox(spec, depthLimit);
                per = UnitsPerBox(spec, itemVol, itemMass);
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>("CargoBoxes/" + id);
                MeshFilter mf = null;
                if (prefab != null) mf = prefab.GetComponent<MeshFilter>();

                if (mf != null && mf.sharedMesh != null)
                {
                    size = Vector3.Scale(mf.sharedMesh.bounds.size, prefab.transform.localScale);
                }
                else if (!fallbackSizes.TryGetValue(id, out size))
                {
                    Debug.LogWarning("RackBuilder: no size for " + id + " - skipped.");
                    continue;
                }
            }

            string planKey = zone;
            if (size.y > 0.7f) planKey = zone + " TALL";

            if (!plans.ContainsKey(planKey))
            {
                ZonePlan np = new ZonePlan();
                np.key = planKey;
                plans[planKey] = np;
            }

            int boxes = Mathf.CeilToInt((float)qty / per);
            for (int j = 0; j < boxes; j++)
            {
                BoxUnit u = new BoxUnit();
                u.id = id;
                u.zone = zone;
                u.size = size;
                u.units = Mathf.Min(per, qty - j * per);
                u.day = DayForUnit(missionDay, j * per, qty);
                u.order = order;
                order++;
                plans[planKey].units.Add(u);
            }
            itemTotal += qty;
            boxTotal += boxes;
        }

        // ---- 2. plan the shelves of every zone ----
        float topLimit = cs.y - topGap;
        foreach (ZonePlan zp in plans.Values) PlanZone(zp, topLimit, depth);

        // ---- 3. decide which wall each zone goes on ----
        List<WallRun> runs = MakeRuns();

        // emergency by the door, then biggest racks first. A rack that fits nowhere is split in two.
        List<ZonePlan> queue = new List<ZonePlan>(plans.Values);
        queue.Sort((x, y) => y.length.CompareTo(x.length));
        List<ZonePlan> placed = new List<ZonePlan>();
        if (plans.ContainsKey("EMERGENCY"))
        {
            ZonePlan em = plans["EMERGENCY"];
            queue.Remove(em);
            queue.Insert(0, em);
        }

        int guard = 0;
        while (queue.Count > 0 && guard < 200)
        {
            guard++;
            ZonePlan cur = queue[0];
            queue.RemoveAt(0);

            WallRun target = FindRun(cur, runs);
            if (target == null && cur.units.Count >= 2)
            {
                int half = cur.units.Count / 2;
                ZonePlan first = new ZonePlan();
                ZonePlan second = new ZonePlan();
                first.key = cur.key;
                second.key = cur.key;
                first.units.AddRange(cur.units.GetRange(0, half));
                second.units.AddRange(cur.units.GetRange(half, cur.units.Count - half));
                PlanZone(first, topLimit, depth);
                PlanZone(second, topLimit, depth);
                queue.Insert(0, second);
                queue.Insert(0, first);
                continue;
            }

            if (target == null)
            {
                target = runs[0];
                foreach (WallRun other in runs)
                {
                    if (other.length - other.used > target.length - target.used) target = other;
                }
                Debug.LogWarning("RackBuilder: " + cur.key + " does not fit anywhere. Not enough wall space.");
            }

            cur.run = target;
            cur.start = target.used;
            target.used += cur.length + 0.06f;
            placed.Add(cur);
        }

        // ---- 4. build ----
        GameObject root = new GameObject(rootName);
        root.transform.SetParent(grid.transform, false);
        GameObject shelvesRoot = new GameObject("Shelves");
        shelvesRoot.transform.SetParent(root.transform, false);
        GameObject slotsRoot = new GameObject("Slots");
        slotsRoot.transform.SetParent(root.transform, false);

        shelfMat = MakeMat(new Color(0.60f, 0.63f, 0.68f));
        postMat = MakeMat(new Color(0.20f, 0.22f, 0.26f));
        BuildDoorMarker(root.transform);

        if (buildFlatFloor)
        {
            Cube("Floor", root.transform, new Vector3(0f, floorY - 0.025f, 0f),
                 new Vector3(cs.x, 0.05f, cs.z), MakeMat(new Color(0.28f, 0.30f, 0.33f)));
        }

        int slotCount = 0;
        foreach (ZonePlan built in placed)
        {
            BuildRack(built, depth, shelvesRoot.transform);
            slotCount += PlaceSlots(built, depth, slotsRoot.transform);
        }

        foreach (WallRun rr in runs)
        {
            float usedLen = Mathf.Max(0f, rr.used - 0.06f);
            Debug.Log("RackBuilder: wall run uses " + usedLen.ToString("F2") + " m of " + rr.length.ToString("F2") + " m.");
            if (usedLen > rr.length + 0.01f)
            {
                Debug.LogWarning("RackBuilder: a wall is too full by " + (usedLen - rr.length).ToString("F2") + " m.");
            }
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Build racks");
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("RackBuilder: built the racks and " + slotCount + " slots for " + itemTotal + " items (" + (condensed ? "condensed" : "one box per item") + ").");
    }

    [ContextMenu("2. Remove racks and slots")]
    public void RemoveRacks()
    {
        if (grid == null) grid = FindGrid();
        if (grid == null) return;

        Transform old = grid.transform.Find(rootName);
        if (old != null)
        {
            old.name = "Removed racks";
            DestroyNow(old.gameObject);
        }
        slots.Clear();
    }

    // SIDE TOOL: puts every box into its slot. Only works while the game is playing.
    [ContextMenu("3. Side tool: fill every slot with its box (Play mode only)")]
    public void FillWithBoxes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("RackBuilder: press Play first, then use this tool.");
            return;
        }

        if (grid == null) grid = FindGrid();
        if (grid == null) return;

        if (slots.Count == 0)
        {
            Debug.LogWarning("RackBuilder: build the racks first (menu item 1).");
            return;
        }

        RemoveTestBoxes();

        GameObject holder = new GameObject(testName);
        holder.transform.SetParent(grid.transform, false);

        int real = 0;
        int placeholders = 0;
        foreach (SlotInfo s in slots)
        {
            Vector3 pos = grid.transform.TransformPoint(s.position);
            Quaternion rot = grid.transform.rotation * s.rotation;

            GameObject box = null;
            if (fillUsesPrefabs)
            {
                GameObject prefab = Resources.Load<GameObject>("CargoBoxes/" + s.boxId);
                if (prefab != null)
                {
                    box = Instantiate(prefab, pos, rot, holder.transform);
                    real++;
                }
            }

            if (box == null)
            {
                // a colored block of the right size stands in
                box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(holder.transform, false);
                box.transform.position = pos;
                box.transform.rotation = rot;
                box.transform.localScale = s.size;
                box.GetComponent<Renderer>().sharedMaterial = PadMaterial(s.zone);
                placeholders++;
            }
            box.name = s.boxId + " x" + s.units;

            Rigidbody rb = box.GetComponent<Rigidbody>();
            if (rb != null) rb.Sleep();
        }

        Debug.Log("RackBuilder: placed " + real + " real boxes and " + placeholders + " placeholder blocks.");
    }

    [ContextMenu("4. Side tool: remove the boxes it placed")]
    public void RemoveTestBoxes()
    {
        if (grid == null) grid = FindGrid();
        if (grid == null) return;

        Transform old = grid.transform.Find(testName);
        if (old != null)
        {
            old.name = "Removed boxes";
            DestroyNow(old.gameObject);
        }
    }

    // ------------------------------------------------------------------
    //  Box sizes
    // ------------------------------------------------------------------

    // Returns (along shelf, height, depth) in meters.
    // The side that fits the rack depth best faces the aisle, the shortest remaining side points up.
    static Vector3 OrientBox(Spec s, float depthLimit)
    {
        if (s.upright) return new Vector3(s.a / 100f, s.b / 100f, s.c / 100f);

        float[] d = { s.a / 100f, s.b / 100f, s.c / 100f };
        System.Array.Sort(d);   // smallest first

        int depthIndex = 0;
        for (int i = 2; i >= 0; i--)
        {
            if (d[i] <= depthLimit)
            {
                depthIndex = i;
                break;
            }
        }

        List<float> rest = new List<float>();
        for (int i = 0; i < 3; i++)
        {
            if (i != depthIndex) rest.Add(d[i]);
        }

        float along = Mathf.Max(rest[0], rest[1]);
        float height = Mathf.Min(rest[0], rest[1]);
        return new Vector3(along, height, d[depthIndex]);
    }

    // How many items fit in one box: limited by volume (at packEfficiency) and by loaded mass
    int UnitsPerBox(Spec s, float itemVol, float itemMass)
    {
        if (s.fixedPer > 0) return s.fixedPer;

        float byVol = itemVol > 0f ? s.volume * packEfficiency / itemVol : 999f;
        float byMass = itemMass > 0f ? s.mass / itemMass : 999f;
        return Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(byVol, byMass)));
    }

    // ------------------------------------------------------------------
    //  Layout
    // ------------------------------------------------------------------

    // Rack runs along the walls. They start at the door and go outward.
    List<WallRun> MakeRuns()
    {
        float halfL = grid.containerSize.z / 2f;
        List<WallRun> list = new List<WallRun>();

        if (DoorAtEnd())
        {
            float endSign = (door == DoorPosition.EndMinusZ) ? -1f : 1f;
            for (int w = 0; w < 2; w++)
            {
                WallRun r = new WallRun();
                r.wallSign = (w == 0) ? -1f : 1f;
                r.startZ = endSign * halfL;
                r.dir = -endSign;
                r.used = doorClearance;
                r.length = grid.containerSize.z - endInset;
                list.Add(r);
            }
        }
        else
        {
            float doorSign = (door == DoorPosition.WallPlusX) ? 1f : -1f;
            float doorHalf = doorWidth / 2f + 0.1f;
            for (int d = 0; d < 2; d++)
            {
                float dir = (d == 0) ? 1f : -1f;

                WallRun a = new WallRun();
                a.wallSign = doorSign;
                a.startZ = doorCenter + dir * doorHalf;
                a.dir = dir;
                a.used = 0f;
                a.length = Mathf.Abs(dir * halfL - a.startZ) - endInset;
                list.Add(a);

                WallRun b = new WallRun();
                b.wallSign = -doorSign;
                b.startZ = dir * 0.05f;
                b.dir = dir;
                b.used = 0f;
                b.length = halfL - 0.05f - endInset;
                list.Add(b);
            }
        }
        return list;
    }

    // Finds the tightest wall space a zone still fits in (null if none)
    WallRun FindRun(ZonePlan zp, List<WallRun> runs)
    {
        WallRun best = null;
        foreach (WallRun r in runs)
        {
            float room = r.length - r.used;
            if (zp.length > room) continue;
            if (best == null || room < best.length - best.used) best = r;
        }
        return best;
    }

    // Shelf spacing, shelf count, lanes and the spot of every box in a zone
    void PlanZone(ZonePlan zp, float topLimit, float depth)
    {
        // earliest day first (nearest the door)
        zp.units.Sort((a, b) => a.day != b.day ? a.day.CompareTo(b.day) : a.order.CompareTo(b.order));

        zp.maxH = 0f;
        float maxD = 0f;
        foreach (BoxUnit u in zp.units)
        {
            if (u.size.y > zp.maxH) zp.maxH = u.size.y;
            if (u.size.z > maxD) maxD = u.size.z;
        }

        zp.spacing = zp.maxH + boardThickness + shelfClearance;
        zp.levels = Mathf.FloorToInt((topLimit - firstBoardTop - zp.maxH) / zp.spacing) + 1;
        zp.levels = Mathf.Clamp(zp.levels, 1, maxLevels);

        float avail = depth - wallGap;
        zp.lanes = 1;
        if (doubleDeep) zp.lanes = Mathf.Clamp(Mathf.FloorToInt(avail / (maxD + 0.02f)), 1, 2);
        zp.laneW = avail / zp.lanes;

        float[] cursor = new float[zp.levels * zp.lanes];
        foreach (BoxUnit u in zp.units)
        {
            int best = 0;
            for (int i = 1; i < cursor.Length; i++)
            {
                if (cursor[i] < cursor[best] - 0.0001f) best = i;
            }

            zp.slotLevel.Add(best / zp.lanes);
            zp.slotLane.Add(best % zp.lanes);
            zp.slotCenter.Add(cursor[best] + u.size.x / 2f);
            cursor[best] += u.size.x + boxGap;
        }

        float need = 0f;
        foreach (float cur in cursor)
        {
            if (cur > need) need = cur;
        }
        zp.length = need + 0.08f;
    }

    // ------------------------------------------------------------------
    //  Building
    // ------------------------------------------------------------------

    void BuildDoorMarker(Transform parent)
    {
        Vector3 cs = grid.containerSize;
        GameObject marker = new GameObject("Door Marker");
        marker.transform.SetParent(parent, false);
        Material red = MakeMat(Color.red);

        Vector3 stripPos;
        Vector3 stripSize;
        Vector3 poleA;
        Vector3 poleB;

        if (DoorAtEnd())
        {
            float zEnd = (door == DoorPosition.EndMinusZ ? -1f : 1f) * (cs.z / 2f - 0.03f);
            stripPos = new Vector3(0f, floorY + 0.025f, zEnd);
            stripSize = new Vector3(doorWidth, 0.05f, 0.6f);
            poleA = new Vector3(-doorWidth / 2f, floorY + 1.05f, zEnd);
            poleB = new Vector3(doorWidth / 2f, floorY + 1.05f, zEnd);
        }
        else
        {
            float xWall = (door == DoorPosition.WallPlusX ? 1f : -1f) * (cs.x / 2f - 0.03f);
            stripPos = new Vector3(xWall, floorY + 0.025f, doorCenter);
            stripSize = new Vector3(0.6f, 0.05f, doorWidth);
            poleA = new Vector3(xWall, floorY + 1.05f, doorCenter - doorWidth / 2f);
            poleB = new Vector3(xWall, floorY + 1.05f, doorCenter + doorWidth / 2f);
        }

        GameObject[] parts = new GameObject[3];
        parts[0] = Cube("Door Strip", marker.transform, stripPos, stripSize, red);
        parts[1] = Cube("Door Pole A", marker.transform, poleA, new Vector3(0.06f, 2.1f, 0.06f), red);
        parts[2] = Cube("Door Pole B", marker.transform, poleB, new Vector3(0.06f, 2.1f, 0.06f), red);
        foreach (GameObject part in parts) DestroyNow(part.GetComponent<Collider>());
    }

    void BuildRack(ZonePlan zp, float depth, Transform parent)
    {
        WallRun run = zp.run;
        Vector3 cs = grid.containerSize;
        float halfW = cs.x / 2f - sideInset;

        float z0 = run.startZ + run.dir * zp.start;
        float z1 = z0 + run.dir * zp.length;
        float cz = (z0 + z1) / 2f;
        float cx = run.wallSign * (halfW - depth / 2f);

        GameObject rack = new GameObject(ZoneName(zp.key) + " rack");
        rack.transform.SetParent(parent, false);

        for (int level = 0; level < zp.levels; level++)
        {
            float top = floorY + firstBoardTop + level * zp.spacing;
            Cube("Shelf " + (level + 1), rack.transform,
                 new Vector3(cx, top - boardThickness / 2f, cz),
                 new Vector3(depth, boardThickness, zp.length), shelfMat);
        }

        float postTop = firstBoardTop + (zp.levels - 1) * zp.spacing + zp.maxH + 0.06f;
        float postX = run.wallSign * (halfW - depth + 0.01f);
        int posts = Mathf.Max(2, Mathf.CeilToInt(zp.length / 1.2f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float pz = z0 + (z1 - z0) * i / (posts - 1);
            Cube("Post", rack.transform, new Vector3(postX, floorY + postTop / 2f, pz),
                 new Vector3(0.04f, postTop, 0.04f), postMat);
        }

        if (showZoneSigns)
        {
            MakeSign(ZoneName(zp.key), rack.transform,
                     new Vector3(run.wallSign * (halfW - depth - 0.05f), floorY + cs.y - 0.12f, cz),
                     run.wallSign);
        }
    }

    int PlaceSlots(ZonePlan zp, float depth, Transform slotsRoot)
    {
        WallRun run = zp.run;
        float halfW = grid.containerSize.x / 2f - sideInset;

        GameObject group = new GameObject(ZoneName(zp.key) + " (" + zp.units.Count + ")");
        group.transform.SetParent(slotsRoot, false);

        Quaternion yaw = Quaternion.Euler(0f, run.wallSign > 0f ? 90f : -90f, 0f);

        for (int i = 0; i < zp.units.Count; i++)
        {
            BoxUnit u = zp.units[i];

            float z = run.startZ + run.dir * (zp.start + 0.04f + zp.slotCenter[i]);
            float shelfTop = floorY + firstBoardTop + zp.slotLevel[i] * zp.spacing;
            // box sits against the back of its lane
            float x = run.wallSign * (halfW - wallGap - zp.slotLane[i] * zp.laneW - u.size.z / 2f);
            float y = shelfTop + u.size.y / 2f + 0.002f;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyNow(pad.GetComponent<Collider>());
            pad.name = "Slot " + u.id + " x" + u.units + " (day " + u.day + ")";
            pad.transform.SetParent(group.transform, false);
            pad.transform.localPosition = new Vector3(x, shelfTop + 0.003f, z);
            pad.transform.localRotation = yaw;
            pad.transform.localScale = new Vector3(Mathf.Max(0.03f, u.size.x - 0.04f), 0.006f,
                                                   Mathf.Max(0.03f, u.size.z - 0.04f));

            Renderer rend = pad.GetComponent<Renderer>();
            rend.sharedMaterial = PadMaterial(u.zone);
            rend.enabled = showSlotPads;

            SlotInfo info = new SlotInfo();
            info.boxId = u.id;
            info.zone = u.zone;
            info.day = u.day;
            info.units = u.units;
            info.shelfLevel = zp.slotLevel[i];
            info.position = new Vector3(x, y, z);
            info.rotation = yaw;
            info.size = u.size;
            slots.Add(info);
        }

        return zp.units.Count;
    }

    // ------------------------------------------------------------------
    //  Small helpers
    // ------------------------------------------------------------------

    GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    Material MakeMat(Color color)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyNow(tmp);
        m.color = color;
        return m;
    }

    Material PadMaterial(string zone)
    {
        if (!padMats.ContainsKey(zone)) padMats[zone] = MakeMat(ZoneColor(zone));
        return padMats[zone];
    }

    void MakeSign(string text, Transform parent, Vector3 pos, float wallSign)
    {
        GameObject go = new GameObject("Sign " + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, wallSign * 90f, 0f);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 100;
        tm.characterSize = signSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
    }

    static void DestroyNow(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

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

    static string ZoneName(string key)
    {
        switch (key)
        {
            case "GAS TALL": return "GAS TANKS";
            case "EVA": return "EVA GEAR";
            case "EVA TALL": return "SPACESUITS";
            case "MAINT": return "REPAIR AND POWER";
        }
        return key;
    }

    // Turns the MissionDay text into one day number for item number k of qty
    static int DayForUnit(string md, int k, int qty)
    {
        md = md.Trim();
        if (md == "ALL") return 0;     // emergency: right by the door
        if (md == "ANY") return 15;    // no set day: toward the back

        int a;
        int b;

        if (md.Contains("-"))
        {
            string[] range = md.Split('-');
            if (range.Length == 2 && int.TryParse(range[0], out a) && int.TryParse(range[1], out b))
            {
                int span = b - a + 1;
                return a + (k * span) / qty;
            }
            return 15;
        }

        if (md.Contains(";"))
        {
            string[] list = md.Split(';');
            int index = (k * list.Length) / qty;
            if (int.TryParse(list[index], out a)) return a;
            return 15;
        }

        if (int.TryParse(md, out a)) return a;
        return 15;
    }
}