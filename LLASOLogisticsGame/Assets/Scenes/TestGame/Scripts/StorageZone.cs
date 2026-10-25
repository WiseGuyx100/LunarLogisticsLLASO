using UnityEngine;

// An invisible box that marks one rack or storage area (like the "EVA GEAR" shelves)
public class StorageZone : MonoBehaviour
{
    public enum DockResult { NotNearby, Docked, DoesNotFit }

    [Header("Name on the sign in the hold")]
    public string zoneName = "EVA GEAR";

    [Header("Code from the CSV RequiredZone column (separate several with ;)")]
    public string zoneCode = "";

    [Header("Sliding boxes in (you can leave these alone)")]
    [Tooltip("How far in front of the rack still counts as 'in front' (meters).")]
    public float dockDistance = 1.0f;
    [Tooltip("How far up or down (meters) the game looks for a gap if the box is lined up with a shelf board.")]
    public float verticalAssist = 0.35f;

    // Is this spot inside the zone box?
    public bool Contains(Vector3 worldPoint)
    {
        Vector3 p = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(p.x) <= 0.5f
            && Mathf.Abs(p.y) <= 0.5f
            && Mathf.Abs(p.z) <= 0.5f;
    }

    // Counts the shelf boards under the box: "Shelf 2", or "Floor level"
    public string ShelfLabel(Bounds box)
    {
        float zoneBottom = transform.position.y - Mathf.Abs(transform.lossyScale.y) / 2f;

        // Look straight down from inside the box
        Vector3 origin = new Vector3(box.center.x, box.min.y + 0.03f, box.center.z);
        float distance = origin.y - zoneBottom;

        int boards = 0;
        if (distance > 0f)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit h in hits)
            {
                // Other boxes and the player are not shelf boards
                if (h.collider.GetComponentInParent<ItemInfo>() != null) continue;
                if (h.collider.GetComponentInParent<CharacterController>() != null) continue;

                // The floor is not a shelf board
                if (h.point.y < zoneBottom + 0.02f) continue;

                boards++;
            }
        }

        if (boards == 0) return "Floor level";
        return "Shelf " + boards;
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

    // Tries to slide a box into this rack.
    // NotNearby  = the box is not in front of this rack. Do nothing.
    // Docked     = it fits. newCenter is where the middle of the box should end up.
    // DoesNotFit = it is in front of the rack but is too big, or there is no room.
    public DockResult TryDock(Bounds box, Rigidbody self, out Vector3 newCenter)
    {
        newCenter = box.center;

        Vector3 ls = transform.lossyScale;
        Vector3 scale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        Vector3 local = transform.InverseTransformPoint(box.center);

        // The middle of the box must be at the height of the rack
        if (Mathf.Abs(local.y) > 0.5f) return DockResult.NotNearby;

        // The box must be outside the rack on exactly one side. That side is the front.
        bool outX = Mathf.Abs(local.x) > 0.5f;
        bool outZ = Mathf.Abs(local.z) > 0.5f;
        if (outX == outZ) return DockResult.NotNearby;

        Vector3 outDir;    // points out of the rack toward the box
        Vector3 sideDir;   // across the rack
        float along;       // how deep the rack is
        float across;      // how wide the rack is

        if (outX)
        {
            outDir = transform.right * Mathf.Sign(local.x);
            sideDir = transform.forward;
            along = scale.x;
            across = scale.z;
        }
        else
        {
            outDir = transform.forward * Mathf.Sign(local.z);
            sideDir = transform.right;
            along = scale.z;
            across = scale.x;
        }

        // How big is the box in each direction?
        Vector3 size = box.size;
        float boxAlong = Mathf.Abs(outDir.x) * size.x + Mathf.Abs(outDir.z) * size.z;
        float boxAcross = Mathf.Abs(sideDir.x) * size.x + Mathf.Abs(sideDir.z) * size.z;
        float boxHeight = size.y;

        // Where is the box compared to the middle of the rack?
        Vector3 off = box.center - transform.position;
        float s = Vector3.Dot(off, outDir);     // how far out in front
        float lat = Vector3.Dot(off, sideDir);  // left / right

        float face = along / 2f;
        if (s > face + dockDistance) return DockResult.NotNearby;
        if (Mathf.Abs(lat) > across / 2f) return DockResult.NotNearby;

        // Bigger than the whole rack? Then it can never fit.
        if (boxAlong > along + 0.001f || boxAcross > across + 0.001f || boxHeight > scale.y)
        {
            return DockResult.DoesNotFit;
        }

        // Keep it away from the side walls of the rack
        float maxLat = (across - boxAcross) / 2f;
        float latFinal = Mathf.Clamp(lat, -maxLat, maxLat);

        // How far could the middle of the box go before its back touches the back of the rack?
        float sBack = -face + boxAlong / 2f;
        float travel = s - sBack;

        // A little smaller than the box so touching a shelf board doesn't count as a hit
        Vector3 half = size / 2f - new Vector3(0.01f, 0.02f, 0.01f);
        half = Vector3.Max(half, Vector3.one * 0.01f);

        // Try the height the box is at first, then nudge up and down to find a gap
        int steps = Mathf.CeilToInt(verticalAssist / 0.05f);
        for (int i = 0; i <= steps; i++)
        {
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                if (i == 0 && sign == -1) continue;   // test the starting height only once

                float yOff = i * 0.05f * sign;

                Vector3 start = box.center + sideDir * (latFinal - lat);
                start.y += yOff;

                float stop = SlideDistance(start, half, -outDir, travel, self);
                float finalS = s - stop;

                // Is the box fully inside the rack where it stops?
                if (finalS + boxAlong / 2f <= face + 0.05f)
                {
                    newCenter = start - outDir * stop;
                    return DockResult.Docked;
                }
            }
        }

        return DockResult.DoesNotFit;
    }

    // How far the box can slide before it bumps into something
    float SlideDistance(Vector3 start, Vector3 half, Vector3 direction, float travel, Rigidbody self)
    {
        if (travel <= 0.001f) return 0f;

        float stop = travel;

        RaycastHit[] hits = Physics.BoxCastAll(
            start, half, direction, Quaternion.identity, travel, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit h in hits)
        {
            // Ignore the box itself and the player
            if (self != null && h.collider.attachedRigidbody == self) continue;
            if (h.collider.GetComponentInParent<CharacterController>() != null) continue;

            if (h.distance < stop) stop = h.distance;
        }

        return Mathf.Max(0f, stop - 0.01f);
    }

    // Draws the zone in the Scene view so you can place it
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, zoneName + "  [" + zoneCode + "]");
#endif
    }
}