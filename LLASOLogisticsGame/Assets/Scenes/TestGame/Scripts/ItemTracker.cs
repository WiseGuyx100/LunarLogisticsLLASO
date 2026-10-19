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
}

public class ItemTracker : MonoBehaviour
{
    public static ItemTracker Instance;

    public float scanDistance = 4f;

    [Header("Drag XRayMaterial here")]
    public Material highlightMaterial;

    private Camera playerCamera;
    private ScannerToggle scannerToggle;
    private GridContainer grid;
    private ItemInspector inspector;
    private StorageZone[] zones;

    private Dictionary<ItemInfo, TrackedRecord> records = new Dictionary<ItemInfo, TrackedRecord>();

    // The item the beam and glow are pointing at
    private ItemInfo highlighted;
    private GameObject beam;

    // Glow bookkeeping
    private Material glowMat;
    private List<Renderer> glowRenderers = new List<Renderer>();
    private List<Material[]> originalMats = new List<Material[]>();

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

    // Keeps the beam on the box and makes the glow pulse
    // Makes the glow pulse
    void LateUpdate()
    {
        // Nothing picked, or the box is gone
        if (highlighted == null)
        {
            StopGlow();
            return;
        }

        if (glowMat != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            Color c = glowMat.color;
            c.a = 0.25f + 0.4f * pulse;
            glowMat.color = c;
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

    // Turns the beam and glow on for this item. Calling it again for the same item turns them off.
    // Turns the glow on for this item. Calling it again for the same item turns it off.
    public void ToggleHighlight(ItemInfo item)
    {
        if (item == null) return;

        if (highlighted == item)
        {
            ClearHighlight();
            return;
        }

        // Switching from another item? Turn its glow off first.
        StopGlow();

        highlighted = item;
        StartGlow(item);
    }

    public void ClearHighlight()
    {
        highlighted = null;
        StopGlow();
    }

    // Adds the X-ray material on top of the box's own materials
    void StartGlow(ItemInfo item)
    {
        if (highlightMaterial == null) return;

        if (glowMat == null) glowMat = new Material(highlightMaterial);

        foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
        {
            // Skip anything that is not a plain mesh, and skip floating text labels
            if (!(r is MeshRenderer)) continue;
            if (r.GetComponent<TMP_Text>() != null) continue;

            Material[] old = r.sharedMaterials;
            Material[] extended = new Material[old.Length + 1];
            for (int i = 0; i < old.Length; i++) extended[i] = old[i];
            extended[old.Length] = glowMat;

            glowRenderers.Add(r);
            originalMats.Add(old);
            r.sharedMaterials = extended;
        }
    }

    // Puts the box's own materials back
    void StopGlow()
    {
        for (int i = 0; i < glowRenderers.Count; i++)
        {
            if (glowRenderers[i] != null)
            {
                glowRenderers[i].sharedMaterials = originalMats[i];
            }
        }

        glowRenderers.Clear();
        originalMats.Clear();
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

    // Builds a tall beam out of code (no files needed)
    GameObject MakeBeam()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = "FindBeam";
        Destroy(g.GetComponent<Collider>());
        g.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);

        Material m;
        if (highlightMaterial != null)
        {
            // Same see-through material, so shelves can't hide the beam
            m = new Material(highlightMaterial);
            m.color = new Color(1f, 0.9f, 0.1f, 0.8f);
        }
        else
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Color");
            m = new Material(s);
            m.color = new Color(1f, 0.9f, 0.1f, 0.6f);
        }

        Renderer r = g.GetComponent<Renderer>();
        r.material = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return g;
    }
}