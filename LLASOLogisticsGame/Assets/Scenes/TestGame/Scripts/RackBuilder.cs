using System.Collections.Generic;
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
        public int shelfLevel;     // 0 = bottom
        public Vector3 position;   // inside the CargoGrid object
        public Quaternion rotation = Quaternion.identity;
        public Vector3 size;
    }

    [Header("Your grid container (leave empty and it finds it)")]
    public GridContainer grid;

    [Header("Door")]
    public DoorPosition door = DoorPosition.EndMinusZ;
    public float doorWidth = 1.3f;
    public float doorCenter = 0f;       // only for a door on a long wall (0 = middle)
    public float doorClearance = 0.15f; // empty space kept next to an end door

    [Header("Racks")]
    public float rackDepth = 0.55f;     // meters. Auto-shrinks if the aisle would be narrower than the door
    public bool doubleDeep = true;      // small boxes sit two deep (back row first)
    public int maxLevels = 6;           // shelves per rack (player reach)
    public bool buildFlatFloor = true;
    public float floorY = 0f;
    public bool showZoneSigns = true;
    public bool showSlotPads = true;

    [Header("Fine tuning (you can leave these alone)")]
    public float signSize = 0.008f;
    public float boxGap = 0.03f;
    public float wallGap = 0.005f;
    public float sideInset = 0.08f;   // pulls racks away from the long walls
    public float endInset = 0.15f;    // keeps racks away from the container ends
    public float topGap = 0.15f;

    [Header("Answer key (filled in when you build)")]
    public List<SlotInfo> slots = new List<SlotInfo>();

    const float boardThickness = 0.03f;
    const float firstBoardTop = 0.06f;
    const float shelfClearance = 0.04f;
    const string rootName = "Racks (built)";
    const string testName = "Test Boxes (side tool)";

    static readonly string[] zoneOrder =
    {
        "EMERGENCY", "FOOD", "MEDICAL", "WATER", "CREW", "WASTE",
        "SCIENCE", "EVA", "EVA TALL", "MAINT", "GAS TALL"
    };

    // Sizes in meters (width, height, depth). Only used if a box prefab is not found.
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
        public Vector3 size;
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
    void Awake()
    {
        if (grid == null) grid = FindGrid();
        if (grid == null) return;
        Transform m = grid.transform.Find(rootName + "/Door Marker");
        if (m != null) m.gameObject.SetActive(false);
    }

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
        float d = rackDepth;
        if (DoorAtEnd())
        {
            d = Mathf.Min(d, (grid.containerSize.x - doorWidth - 0.04f) / 2f);
        }
        return d;
    }

    // Draws the door (red) and the aisle (yellow) in the Scene view
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
        Gizmos.DrawWireCube(new Vector3(0f, floorY + 0.02f, 0f), new Vector3(s.x - 2f * d, 0.04f, s.z));
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
        Debug.Log("RackBuilder: rack depth " + depth.ToString("F2") + " m, aisle " + (cs.x - 2f * depth).ToString("F2") + " m.");

        // ---- 1. read the spreadsheet: one entry for every single box ----
        Dictionary<string, ZonePlan> plans = new Dictionary<string, ZonePlan>();
        string[] lines = csv.text.Split('\n');
        int order = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;

            string[] c = line.Split(',');
            if (c.Length < 11) continue;

            string id = c[0].Trim();
            string zone = c[7].Trim();
            string missionDay = c[10].Trim();

            int qty;
            if (!int.TryParse(c[9].Trim(), out qty)) continue;

            Vector3 size;
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

            string planKey = zone;
            if (size.y > 0.7f) planKey = zone + " TALL";

            if (!plans.ContainsKey(planKey))
            {
                ZonePlan np = new ZonePlan();
                np.key = planKey;
                plans[planKey] = np;
            }

            for (int k = 0; k < qty; k++)
            {
                BoxUnit u = new BoxUnit();
                u.id = id;
                u.zone = zone;
                u.size = size;
                u.day = DayForUnit(missionDay, k, qty);
                u.order = order;
                order++;
                plans[planKey].units.Add(u);
            }
        }

        // ---- 2. plan the shelves of every zone ----
        float topLimit = cs.y - topGap;
        foreach (ZonePlan p in plans.Values) PlanZone(p, topLimit, depth);

        // ---- 3. decide which wall each zone goes on ----
        List<WallRun> runs = MakeRuns();

        // biggest racks first, emergency by the door.
        // A rack that fits nowhere is split in two (earlier days stay nearer the door).
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
            ZonePlan p = queue[0];
            queue.RemoveAt(0);

            WallRun r = FindRun(p, runs);
            if (r == null && p.units.Count >= 2)
            {
                int half = p.units.Count / 2;
                ZonePlan first = new ZonePlan();
                ZonePlan second = new ZonePlan();
                first.key = p.key;
                second.key = p.key;
                first.units.AddRange(p.units.GetRange(0, half));
                second.units.AddRange(p.units.GetRange(half, p.units.Count - half));
                PlanZone(first, topLimit, depth);
                PlanZone(second, topLimit, depth);
                queue.Insert(0, second);
                queue.Insert(0, first);
                continue;
            }

            if (r == null)
            {
                r = runs[0];
                foreach (WallRun other in runs)
                {
                    if (other.length - other.used > r.length - r.used) r = other;
                }
                Debug.LogWarning("RackBuilder: " + p.key + " does not fit anywhere. Not enough wall space.");
            }

            p.run = r;
            p.start = r.used;
            r.used += p.length + 0.06f;
            placed.Add(p);
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
        foreach (ZonePlan p in placed)
        {
            BuildRack(p, depth, shelvesRoot.transform);
            slotCount += PlaceSlots(p, depth, slotsRoot.transform);
        }

        foreach (WallRun r in runs)
        {
            float usedLen = Mathf.Max(0f, r.used - 0.06f);
            Debug.Log("RackBuilder: wall run uses " + usedLen.ToString("F2") + " m of " + r.length.ToString("F2") + " m.");
            if (usedLen > r.length + 0.01f)
            {
                Debug.LogWarning("RackBuilder: a wall is too full by " + (usedLen - r.length).ToString("F2") + " m.");
            }
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Build racks");
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("RackBuilder: built the racks and " + slotCount + " slots.");
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

            GameObject prefab = Resources.Load<GameObject>("CargoBoxes/" + s.boxId);
            GameObject box;
            if (prefab != null)
            {
                box = Instantiate(prefab, pos, rot, holder.transform);
                real++;
            }
            else
            {
                // no prefab found: a colored block of the right size stands in
                box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(holder.transform, false);
                box.transform.position = pos;
                box.transform.rotation = rot;
                box.transform.localScale = s.size;
                box.GetComponent<Renderer>().sharedMaterial = PadMaterial(s.zone);
                placeholders++;
            }
            box.name = s.boxId;

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
                r.length = grid.containerSize.z - 0.03f;
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

                // door wall: starts beside the door
                WallRun a = new WallRun();
                a.wallSign = doorSign;
                a.startZ = doorCenter + dir * doorHalf;
                a.dir = dir;
                a.used = 0f;
                a.length = Mathf.Abs(dir * halfL - a.startZ) - endInset;
                list.Add(a);

                // opposite wall: starts in the middle
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

    // Puts a zone on the tightest wall space it still fits in
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
            float x = run.wallSign * (halfW - wallGap - zp.slotLane[i] * zp.laneW - zp.laneW / 2f);
            float y = shelfTop + u.size.y / 2f + 0.002f;

            // flat colored pad (no collider, so it can't block anything)
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyNow(pad.GetComponent<Collider>());
            pad.name = "Slot " + u.id + " (day " + u.day + ")";
            pad.transform.SetParent(group.transform, false);
            pad.transform.localPosition = new Vector3(x, shelfTop + 0.003f, z);
            pad.transform.localRotation = yaw;
            pad.transform.localScale = new Vector3(Mathf.Max(0.03f, u.size.x - 0.04f), 0.006f,
                                                   Mathf.Max(0.03f, u.size.z - 0.04f));

            Renderer rend = pad.GetComponent<Renderer>();
            rend.sharedMaterial = PadMaterial(u.zone);
            rend.enabled = showSlotPads;

            // the answer key
            SlotInfo info = new SlotInfo();
            info.boxId = u.id;
            info.zone = u.zone;
            info.day = u.day;
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

    // Turns the MissionDay text into one day number for box number k of qty
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