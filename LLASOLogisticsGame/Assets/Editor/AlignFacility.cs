// OPTIONAL helper. Put this file in a folder named "Editor" inside Assets (you already have Assets/Editor).
// Then: select the facility in the Hierarchy and click  Tools > Align Facility To Warehouse.
// It measures your warehouse and moves/rotates the facility so the walkway meets the +X end of the warehouse.
using UnityEngine;
using UnityEditor;

public static class AlignFacility
{
    [MenuItem("Tools/Align Facility To Warehouse")]
    private static void Align()
    {
        GameObject facility = Selection.activeGameObject;
        GameObject warehouse = GameObject.Find("Warehouse_Open (1)");

        if (facility == null) { Debug.LogWarning("Select the facility in the Hierarchy first."); return; }
        if (warehouse == null) { Debug.LogWarning("Could not find 'Warehouse_Open (1)' in the scene."); return; }

        Renderer[] renderers = warehouse.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogWarning("The warehouse has no Renderer to measure."); return; }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        Undo.RecordObject(facility.transform, "Align Facility");
        facility.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        // end face = warehouse max X, centred across its width, floor 5 cm above the warehouse's lowest point
        facility.transform.position = new Vector3(b.max.x, b.min.y + 0.05f, b.center.z);

        Debug.Log("Facility placed at " + facility.transform.position +
                  "  (warehouse end face X = " + b.max.x.ToString("F2") + ", centre Z = " + b.center.z.ToString("F2") + ")");
    }
}
