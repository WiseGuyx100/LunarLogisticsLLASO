using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// One saved scan
public class TrackedRecord
{
    public ItemInfo item;
    public string id;
    public string itemName;
    public string location;
}

public class ItemTracker : MonoBehaviour
{
    public static ItemTracker Instance;

    public float scanDistance = 4f;

    private Camera playerCamera;
    private ScannerToggle scannerToggle;
    private GridContainer grid;
    private ItemInspector inspector;
    private StorageZone[] zones;

    private Dictionary<ItemInfo, TrackedRecord> records = new Dictionary<ItemInfo, TrackedRecord>();

    // The item the beam is pointing at
    private ItemInfo highlighted;
    private GameObject beam;

    public ItemInfo Highlighted { get { return highlighted; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        scannerToggle = FindFirstObjectByType<ScannerToggle>();
        grid = FindFirstObjectByType<GridContainer>();
        inspector = FindFirstObjectByType<ItemInspector>();
        zones = FindObjectsByType<StorageZone>(FindObjectsSortMode.None);

        if (inspector != null) playerCamera = inspector.playerCamera;
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        if (!GameControls.StoreLocationPressed()) return;

        // Scanner has to be out
        if (scannerToggle != null && !scannerToggle.IsEquipped) return;

        // Find the box in the middle of the screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, scanDistance)) return;

        ItemInfo target = hit.collider.GetComponentInParent<ItemInfo>();
        if (target == null) return;

        Bounds b = GetBounds(target);

        bool inHold;
        string location = LocationOf(b, out inHold);

        // Has to be inside the cargo hold
        if (!inHold)
        {
            if (inspector != null)
            {
                inspector.ShowMessage("NOT STORED\nThis item is not inside the cargo hold.");
            }
            return;
        }

        ItemData data = InventoryDatabase.Instance.Get(target.itemId);

        TrackedRecord r = new TrackedRecord();
        r.item = target;
        r.id = target.itemId;
        r.itemName = data != null ? data.itemName : target.itemId;
        r.location = location;

        bool updated = records.ContainsKey(target);
        records[target] = r;

        if (inspector != null)
        {
            inspector.ShowMessage(
                (updated ? "LOCATION UPDATED" : "LOCATION STORED") + "\n" +
                r.itemName + "  (" + r.id + ")\n" +
                "Location: " + r.location);
        }
    }

    // Keeps the beam on top of the box it is pointing at
    void LateUpdate()
    {
        if (beam == null) return;

        if (highlighted == null)
        {
            beam.SetActive(false);
            return;
        }

        Bounds b = GetBounds(highlighted);
        beam.transform.position = new Vector3(b.center.x, b.max.y + 1.6f, b.center.z);
    }

    // All saved scans, sorted by ID
    public List<TrackedRecord> GetRecords()
    {
        List<TrackedRecord> list = new List<TrackedRecord>();
        foreach (TrackedRecord r in records.Values)
        {
            if (r.item != null) list.Add(r);
        }
        list.Sort((a, b) => string.Compare(a.id, b.id));
        return list;
    }

    // Where the box is right now (used to spot boxes that were moved)
    public string CurrentLocation(TrackedRecord r)
    {
        if (r.item == null) return "Missing";

        bool inHold;
        return LocationOf(GetBounds(r.item), out inHold);
    }

    // Turns the beam on for this item. Calling it again for the same item turns it off.
    public void ToggleHighlight(ItemInfo item)
    {
        if (item == null) return;

        if (highlighted == item)
        {
            ClearHighlight();
            return;
        }

        highlighted = item;
        if (beam == null) beam = MakeBeam();
        beam.SetActive(true);
    }

    public void ClearHighlight()
    {
        highlighted = null;
        if (beam != null) beam.SetActive(false);
    }

    // Picks the zone name + shelf, or falls back to the grid name
    string LocationOf(Bounds b, out bool inHold)
    {
        foreach (StorageZone z in zones)
        {
            if (z != null && z.Contains(b.center))
            {
                inHold = true;
                return z.zoneName + " - Shelf " + z.ShelfNumber(b.min.y);
            }
        }

        if (grid != null && grid.IsInside(b.center))
        {
            inHold = true;
            return grid.GetLocationLabel(b);
        }

        inHold = false;
        return "Outside hold";
    }

    Bounds GetBounds(ItemInfo item)
    {
        Collider[] cols = item.GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return new Bounds(item.transform.position, Vector3.one * 0.1f);

        Bounds b = cols[0].bounds;
        foreach (Collider c in cols) b.Encapsulate(c.bounds);
        return b;
    }

    // Builds a tall yellow beam out of code (no files needed)
    GameObject MakeBeam()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = "FindBeam";
        Destroy(g.GetComponent<Collider>());
        g.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);

        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Color");

        Material m = new Material(s);
        m.color = new Color(1f, 0.9f, 0.1f, 0.6f);

        Renderer r = g.GetComponent<Renderer>();
        r.material = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return g;
    }
}