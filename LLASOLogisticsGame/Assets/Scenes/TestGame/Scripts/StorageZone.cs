using UnityEngine;

// An invisible box that marks one storage area (like the "EVA GEAR" shelves)
public class StorageZone : MonoBehaviour
{
    public enum OpenSide { PositiveX, NegativeX, PositiveZ, NegativeZ }
    public enum DockResult { NotNearby, Docked, DoesNotFit }

    [Header("Name on the sign in the hold")]
    public string zoneName = "EVA GEAR";

    [Header("Code from the CSV RequiredZone column (separate several with ;)")]
    public string zoneCode = "";

    [Header("Shelves")]
    [Tooltip("Distance between two shelves (meters). Also the tallest box that fits.")]
    public float shelfHeight = 0.6f;
    [Tooltip("Height of the first shelf board above the bottom of the zone box.")]
    public float firstShelfOffset = 0f;

    [Header("Sliding boxes into the rack")]
    [Tooltip("The side boxes slide in from. The green line in the Scene view points this way.")]
    public OpenSide openSide = OpenSide.PositiveZ;
    [Tooltip("How far in front of the rack still counts as 'in front' (meters).")]
    public float dockDistance = 1.0f;

    // Is this spot inside the zone box?
    public bool Contains(Vector3 worldPoint)
    {
        Vector3 p = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(p.x) <= 0.5f
            && Mathf.Abs(p.y) <= 0.5f
            && Mathf.Abs(p.z) <= 0.5f;
    }

    // Which shelf is the bottom of the box on? 1 = lowest shelf
    public int ShelfNumber(float bottomY)
    {
        float firstShelfY = transform.position.y - transform.lossyScale.y / 2f + firstShelfOffset;
        int n = Mathf.FloorToInt((bottomY - firstShelfY + 0.05f) / shelfHeight) + 1;
        return Mathf.Max(1, n);
    }

    // Does this zone accept an item with this RequiredZone code?
    public bool Accepts(string requiredCode)
    {
        // If either one is blank we can't judge, so don't call it wrong
        if (string.IsNullOrWhiteSpace(zoneCode)) return true;
        if (string.IsNullOrWhiteSpace(requiredCode)) return true;

        foreach (string part in zoneCode.Split(';'))
        {
            if (part.Trim().ToUpper() == requiredCode.Trim().ToUpper()) return true;
        }
        return false;
    }

    int LevelCount()
    {
        return Mathf.Max(1, Mathf.FloorToInt((transform.lossyScale.y - firstShelfOffset) / shelfHeight + 0.01f));
    }

    // World direction that points OUT of the rack
    Vector3 OpenDirection()
    {
        switch (openSide)
        {
            case OpenSide.PositiveX: return transform.right;
            case OpenSide.NegativeX: return -transform.right;
            case OpenSide.PositiveZ: return transform.forward;
            default: return -transform.forward;
        }
    }

    // Tries to slide a box into this rack.
    // NotNearby = the box is not in front of this rack. Do nothing.
    // Docked    = it fits. newCenter is where the middle of the box should end up.
    // DoesNotFit = it is in front of the rack but is too big or there is no room.
    public DockResult TryDock(Bounds box, Rigidbody self, out Vector3 newCenter)
    {
        newCenter = box.center;

        Vector3 scale = transform.lossyScale;
        bool xAxis = (openSide == OpenSide.PositiveX || openSide == OpenSide.NegativeX);
        float along = xAxis ? scale.x : scale.z;    // how deep the rack is
        float across = xAxis ? scale.z : scale.x;   // how wide the rack is

        Vector3 d = OpenDirection();                 // out of the rack
        Vector3 side = Vector3.Cross(Vector3.up, d); // across the rack

        // How big is the box in each direction?
        Vector3 size = box.size;
        float boxAlong = Mathf.Abs(d.x) * size.x + Mathf.Abs(d.z) * size.z;
        float boxAcross = Mathf.Abs(side.x) * size.x + Mathf.Abs(side.z) * size.z;
        float boxHeight = size.y;

        // Where is the box compared to the middle of the rack?
        Vector3 off = box.center - transform.position;
        float s = Vector3.Dot(off, d);      // positive = out in front of the rack
        float lat = Vector3.Dot(off, side); // left / right

        // Is it in front of the rack (or already partly in it)?
        float face = along / 2f;
        if (s > face + dockDistance || s < -face) return DockResult.NotNearby;
        if (Mathf.Abs(lat) > across / 2f) return DockResult.NotNearby;

        // Which shelf is it lined up with?
        float firstShelfY = transform.position.y - scale.y / 2f + firstShelfOffset;
        int level = Mathf.FloorToInt((box.center.y - firstShelfY) / shelfHeight);
        if (level < 0 || level >= LevelCount()) return DockResult.NotNearby;

        // Is it small enough?
        if (boxAlong > along + 0.001f || boxAcross > across + 0.001f || boxHeight > shelfHeight)
        {
            return DockResult.DoesNotFit;
        }

        // Keep it inside the sides of the rack
        float maxLat = (across - boxAcross) / 2f;
        float latFinal = Mathf.Clamp(lat, -maxLat, maxLat);

        // Sit the box on the shelf board
        float surfaceY = firstShelfY + level * shelfHeight;
        float centerY = surfaceY + boxHeight / 2f + 0.01f;

        // Start just outside the opening and slide toward the back
        float sStart = face + boxAlong / 2f + 0.02f;
        float sBack = -face + boxAlong / 2f;
        float travel = sStart - sBack;

        Vector3 start = transform.position + d * sStart + side * latFinal;
        start.y = centerY;

        // A little smaller than the box so touching the shelf board doesn't count
        Vector3 half = size / 2f - new Vector3(0.01f, 0.02f, 0.01f);
        half = Vector3.Max(half, Vector3.one * 0.01f);

        // How far can it slide before it hits a post, another box, or the back?
        float stop = travel;
        RaycastHit[] hits = Physics.BoxCastAll(
            start, half, -d, Quaternion.identity, travel, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit h in hits)
        {
            if (self != null && h.collider.attachedRigidbody == self) continue;
            if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (h.distance < stop) stop = h.distance;
        }

        stop = Mathf.Max(0f, stop - 0.01f);
        float finalS = sStart - stop;

        // It has to end up (almost) fully inside the rack
        if (finalS + boxAlong / 2f > face + 0.05f) return DockResult.DoesNotFit;

        newCenter = transform.position + d * finalS + side * latFinal;
        newCenter.y = centerY;
        return DockResult.Docked;
    }

    // Draws the zone in the Scene view so you can place it
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        // The zone box
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // Green line = the open side (where boxes slide in from)
        Vector3 localDir = Vector3.forward;
        switch (openSide)
        {
            case OpenSide.PositiveX: localDir = Vector3.right; break;
            case OpenSide.NegativeX: localDir = Vector3.left; break;
            case OpenSide.PositiveZ: localDir = Vector3.forward; break;
            case OpenSide.NegativeZ: localDir = Vector3.back; break;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, localDir * 0.8f);

        // Cyan sheets = where the shelf boards are (line these up with your real shelves)
        Gizmos.color = Color.cyan;
        float sy = transform.lossyScale.y;
        if (sy > 0.001f)
        {
            int levels = LevelCount();
            for (int i = 0; i < levels; i++)
            {
                float yLocal = -0.5f + (firstShelfOffset + i * shelfHeight) / sy;
                Gizmos.DrawWireCube(new Vector3(0f, yLocal, 0f), new Vector3(1f, 0.001f, 1f));
            }
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, zoneName + "  [" + zoneCode + "]");
#endif
    }
}