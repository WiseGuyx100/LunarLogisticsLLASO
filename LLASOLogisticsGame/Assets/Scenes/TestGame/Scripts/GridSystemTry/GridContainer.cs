using UnityEngine;

public class GridContainer : MonoBehaviour
{
    [Header("Container size in meters")]
    public Vector3 containerSize = new Vector3(4f, 2f, 6f);

    [Header("Size of one grid square")]
    public float cellSize = 0.5f;

    public bool showGrid = true;

    // Is this spot inside the container?
    public bool IsInside(Vector3 worldPos)
    {
        Vector3 p = transform.InverseTransformPoint(worldPos);
        return Mathf.Abs(p.x) <= containerSize.x / 2f
            && Mathf.Abs(p.z) <= containerSize.z / 2f
            && p.y >= -0.1f
            && p.y <= containerSize.y;
    }

    // Snaps a box so its EDGES line up with the grid lines
    public Vector3 Snap(Vector3 worldPos, Vector3 boxSize)
    {
        Vector3 p = transform.InverseTransformPoint(worldPos);

        float halfW = containerSize.x / 2f;
        float halfL = containerSize.z / 2f;
        int cellsX = Mathf.Max(1, Mathf.FloorToInt(containerSize.x / cellSize));
        int cellsZ = Mathf.Max(1, Mathf.FloorToInt(containerSize.z / cellSize));

        // How many squares wide and deep is this box?
        int wide = Mathf.Clamp(Mathf.RoundToInt(boxSize.x / cellSize), 1, cellsX);
        int deep = Mathf.Clamp(Mathf.RoundToInt(boxSize.z / cellSize), 1, cellsZ);

        // Which square does the box's corner start on?
        int cx = Mathf.Clamp(Mathf.RoundToInt((p.x + halfW) / cellSize - wide / 2f), 0, cellsX - wide);
        int cz = Mathf.Clamp(Mathf.RoundToInt((p.z + halfL) / cellSize - deep / 2f), 0, cellsZ - deep);

        p.x = -halfW + (cx + wide / 2f) * cellSize;
        p.z = -halfL + (cz + deep / 2f) * cellSize;

        return transform.TransformPoint(p);
    }

    // Draws the grid in the Scene view
    void OnDrawGizmos()
    {
        if (!showGrid) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0, containerSize.y / 2f, 0), containerSize);

        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        float halfW = containerSize.x / 2f;
        float halfL = containerSize.z / 2f;

        for (float x = -halfW; x <= halfW + 0.001f; x += cellSize)
            Gizmos.DrawLine(new Vector3(x, 0, -halfL), new Vector3(x, 0, halfL));

        for (float z = -halfL; z <= halfL + 0.001f; z += cellSize)
            Gizmos.DrawLine(new Vector3(-halfW, 0, z), new Vector3(halfW, 0, z));
    }
}