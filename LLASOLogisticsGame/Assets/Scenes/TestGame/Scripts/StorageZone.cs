using UnityEngine;

// An invisible box that marks one storage area (like the "EVA GEAR" shelves)
public class StorageZone : MonoBehaviour
{
    [Header("Name on the sign in the hold")]
    public string zoneName = "EVA GEAR";

    [Header("Code from the CSV RequiredZone column (separate several with ;)")]
    public string zoneCode = "";

    [Header("Distance between two shelves (meters)")]
    public float shelfHeight = 0.6f;

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
        float zoneBottom = transform.position.y - transform.lossyScale.y / 2f;
        int n = Mathf.FloorToInt((bottomY - zoneBottom + 0.05f) / shelfHeight) + 1;
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