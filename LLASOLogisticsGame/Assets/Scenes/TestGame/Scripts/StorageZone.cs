using UnityEngine;

// An invisible box that marks one storage area (like the "EVA GEAR" shelves)
public class StorageZone : MonoBehaviour
{
    [Header("Type the name exactly like the sign in the hold")]
    public string zoneName = "EVA GEAR";

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

    // Draws the zone in the Scene view so you can place it
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, zoneName);
#endif
    }
}