using System.Collections.Generic;
using TMPro;
using UnityEngine;

// One saved scan
public class TrackedRecord
{
    public ItemInfo item;
    public string id;
    public string itemName;
    public string location;
    public string requiredZone;
    public bool wrongZone;
}

public class ItemTracker : MonoBehaviour
{
    public static ItemTracker Instance;

    public float scanDistance = 4f;
    public float flashSeconds = 1.5f;

    [Header("Drag XRayMaterial here")]
    public Material highlightMaterial;

    private Camera playerCamera;
    private ScannerToggle scannerToggle;
    private GridContainer grid;
    private ItemInspector inspector;
    private StorageZone[] zones;

    private Dictionary<ItemInfo, TrackedRecord> records = new Dictionary<ItemInfo, TrackedRecord>();

    // Boxes the player has scanned with Q
    private HashSet<ItemInfo> scanned = new HashSet<ItemInfo>();

    // Yellow find-glow
    private ItemInfo highlighted;
    private Material glowMat;

    // Red wrong-zone flash
    private ItemInfo flashItem;
    private float flashTimeLeft;
    private Material flashMat;

    // Each box's own materials, saved so we can always put them back
    private Dictionary<Renderer, Material[]> baseMats = new Dictionary<Renderer, Material[]>();

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

    // Called by ItemInspector when the player scans a box with Q
    public void MarkScanned(ItemInfo item)
    {
        if (item != null) scanned.Add(item);
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

        // The box must be scanned with Q first
        if (!scanned.Contains(target))
        {
            if (inspector != null)
            {
                inspector.ShowMessage(
                    "<color=#FF5050>CANNOT STORE</color>\n" +
                    "Cannot store item without scanning first!\n" +
                    "Press Q (or ZR) to scan it.");
            }
            return;
        }

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
        StorageZone zone = FindZone(b);

        TrackedRecord r = new TrackedRecord();
        r.item = target;
        r.id = target.itemId;
        r.itemName = data != null ? data.itemName : target.itemId;
        r.location = location;
        r.requiredZone = data != null ? data.requiredZone : "";
        r.wrongZone = zone != null && data != null && !zone.Accepts(data.requiredZone);

        bool updated = records.ContainsKey(target);
        records[target] = r;

        string message =
            (updated ? "LOCATION UPDATED" : "LOCATION STORED") + "\n" +
            r.itemName + "  (" + r.id + ")\n" +
            "Location: " + r.location;

        if (r.wrongZone)
        {
            message += "\n<color=#FF5050>WRONG ZONE! This item belongs in: " + r.requiredZone + "</color>";
            StartFlash(target);
        }

        if (inspector != null) inspector.ShowMessage(message);
    }

    // Makes the glow pulse and runs the red flash
    void LateUpdate()
    {
        // Yellow find-glow pulse
        if (highlighted != null && glowMat != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            Color c = glowMat.color;
            c.a = 0.25f + 0.4f * pulse;
            glowMat.color = c;
        }

        // Red wrong-zone flash
        if (flashItem != null)
        {
            flashTimeLeft -= Time.deltaTime;

            if (flashTimeLeft <= 0f)
            {
                ItemInfo done = flashItem;
                flashItem = null;
                ApplyOverlays(done);
            }
            else if (flashMat != null)
            {
                float blink = Mathf.Abs(Mathf.Sin(flashTimeLeft * Mathf.PI * 2f));
                Color c = flashMat.color;
                c.a = 0.75f * blink;
                flashMat.color = c;
            }
        }
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

    // Turns the yellow glow on for this item. Calling it again for the same item turns it off.
    public void ToggleHighlight(ItemInfo item)
    {
        if (item == null) return;

        ItemInfo previous = highlighted;

        if (highlighted == item) highlighted = null;
        else highlighted = item;

        if (highlighted != null && glowMat == null && highlightMaterial != null)
        {
            glowMat = new Material(highlightMaterial);
            glowMat.color = new Color(1f, 0.9f, 0.1f, 0.5f);
        }

        ApplyOverlays(previous);
        ApplyOverlays(highlighted);
    }

    public void ClearHighlight()
    {
        ItemInfo previous = highlighted;
        highlighted = null;
        ApplyOverlays(previous);
    }

    // Starts a quick red blink on a box
    void StartFlash(ItemInfo item)
    {
        if (highlightMaterial == null) return;

        if (flashMat == null)
        {
            flashMat = new Material(highlightMaterial);
            flashMat.color = new Color(1f, 0.1f, 0.1f, 0.75f);
        }

        ItemInfo previous = flashItem;
        flashItem = item;
        flashTimeLeft = flashSeconds;

        ApplyOverlays(previous);
        ApplyOverlays(item);
    }

    // Puts the right overlays on a box: its own materials + yellow glow and/or red flash
    void ApplyOverlays(ItemInfo item)
    {
        if (item == null) return;

        bool glow = (item == highlighted) && glowMat != null;
        bool flash = (item == flashItem) && flashMat != null;

        foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
        {
            // Skip anything that is not a plain mesh, and skip floating text labels
            if (!(r is MeshRenderer)) continue;
            if (r.GetComponent<TMP_Text>() != null) continue;

            // Save the box's own materials the first time we touch it
            if (!baseMats.ContainsKey(r)) baseMats[r] = r.sharedMaterials;

            List<Material> list = new List<Material>(baseMats[r]);
            if (glow) list.Add(glowMat);
            if (flash) list.Add(flashMat);

            r.sharedMaterials = list.ToArray();
        }
    }

    StorageZone FindZone(Bounds b)
    {
        foreach (StorageZone z in zones)
        {
            if (z != null && z.Contains(b.center)) return z;
        }
        return null;
    }

    // Picks the zone name + shelf, or falls back to the grid name
    string LocationOf(Bounds b, out bool inHold)
    {
        StorageZone z = FindZone(b);
        if (z != null)
        {
            inHold = true;
            return z.zoneName + " - Shelf " + z.ShelfNumber(b.min.y);
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
}