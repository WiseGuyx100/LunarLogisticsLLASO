// Put in Assets/Editor/ (next to LLASO_FacilitySetup.cs).
//   LLASO > Apply Warehouse Look      : builds/refreshes the Fac_* + zone materials and remaps them onto LLASO_Warehouse*.obj
//   LLASO > Place Warehouse At Facility : drops the warehouse into the open scene at the same position/rotation/scale
//                                         as LLASO_Facility, and adds ceiling lights under the light bars.
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;

public static class LLASO_WarehouseSetup
{
    static readonly (string name, Color col, bool glass, bool emissive)[] Mats = {
        ("Fac_White",       new Color(0.96f,0.97f,0.98f), false, false),
        ("Fac_Grey",        new Color(0.58f,0.61f,0.67f), false, false),
        ("Fac_DarkBlue",    new Color(0.05f,0.12f,0.33f), false, false),
        ("Fac_Purple",      new Color(0.38f,0.24f,0.66f), false, false),
        ("Fac_LightBlue",   new Color(0.62f,0.83f,0.97f), false, false),
        ("Fac_Green",       new Color(0.18f,0.78f,0.42f), false, false),
        ("Fac_DarkSteel",   new Color(0.30f,0.32f,0.37f), false, false),
        ("Fac_Roof",        new Color(0.88f,0.90f,0.93f), false, false),
        ("Fac_Glass",       new Color(0.72f,0.88f,0.97f,0.28f), true, false),
        ("Fac_Floor",       new Color(0.84f,0.86f,0.90f), false, false),
        ("Fac_Light",       Color.white, false, true),
        // new for the warehouse
        ("Fac_Concrete",    new Color(0.46f,0.48f,0.53f), false, false),
        ("Fac_Yellow",      new Color(0.95f,0.78f,0.10f), false, false),
        ("Zone_Fragile",    new Color(0.87f,0.13f,0.13f), false, false),
        ("Zone_Cold",       new Color(0.10f,0.18f,0.85f), false, false),
        ("Zone_Inspection", new Color(1.00f,0.50f,0.08f), false, false),
    };

    // ceiling light positions (symmetric in X, so it works for both the normal and MIRRORED model)
    static readonly Vector3[] LightPos = {
        new Vector3(-7f, 8.75f,  -4.5f), new Vector3(-7f, 8.75f, -15.5f), new Vector3(-7f, 8.75f, -26.5f),
        new Vector3( 7f, 8.75f,  -4.5f), new Vector3( 7f, 8.75f, -15.5f), new Vector3( 7f, 8.75f, -26.5f),
        new Vector3( 0f, 9.3f, -10.0f), new Vector3( 0f, 9.3f, -21.0f),
    };

    static string[] FindWarehouseModels()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var g in AssetDatabase.FindAssets("LLASO_Warehouse t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith(".obj")) list.Add(p);
        }
        return list.ToArray();
    }

    [MenuItem("LLASO/Apply Warehouse Look")]
    public static void Apply()
    {
        var models = FindWarehouseModels();
        if (models.Length == 0) { Debug.LogError("LLASO_Warehouse*.obj not found in the project."); return; }

        string dir = "Assets/Materials/LLASO_Facility";
        Directory.CreateDirectory(dir);
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader sh = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");

        var built = new System.Collections.Generic.Dictionary<string, Material>();
        foreach (var m in Mats)
        {
            string path = $"{dir}/{m.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, path); }
            else if (!m.name.StartsWith("Zone_") && m.name != "Fac_Concrete" && m.name != "Fac_Yellow")
            { built[m.name] = mat; continue; }          // keep the facility's existing materials untouched
            mat.shader = sh;
            mat.color = m.col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", m.col);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", m.glass ? 0.8f : 0.15f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", m.glass ? 0.8f : 0.15f);
            if (m.emissive) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.white * 1.5f); }
            if (m.glass)
            {
                if (urp) { mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.SetFloat("_ZWrite", 0);
                           mat.SetOverrideTag("RenderType", "Transparent"); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                           mat.renderQueue = (int)RenderQueue.Transparent; }
                else { mat.SetFloat("_Mode", 3); mat.SetInt("_SrcBlend", 5); mat.SetInt("_DstBlend", 10); mat.SetInt("_ZWrite", 0);
                       mat.EnableKeyword("_ALPHABLEND_ON"); mat.renderQueue = 3000; }
            }
            EditorUtility.SetDirty(mat);
            built[m.name] = mat;
        }
        AssetDatabase.SaveAssets();

        foreach (var path in models)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kv in built)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            importer.SaveAndReimport();
        }
        Debug.Log("LLASO warehouse look applied to " + models.Length + " model(s).");
    }

    [MenuItem("LLASO/Place Warehouse At Facility")]
    public static void Place()
    {
        var models = FindWarehouseModels();
        if (models.Length == 0) { Debug.LogError("LLASO_Warehouse*.obj not found."); return; }

        // pick the variant that matches the facility file you use (MIRRORED vs normal)
        string pick = models[0];
        var facGuids = AssetDatabase.FindAssets("LLASO_Facility t:Model");
        bool facMirrored = false;
        foreach (var g in facGuids) if (AssetDatabase.GUIDToAssetPath(g).Contains("MIRRORED")) facMirrored = true;
        foreach (var p in models) if (p.Contains("MIRRORED") == facMirrored) pick = p;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pick);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = "LLASO_Warehouse";
        Undo.RegisterCreatedObjectUndo(inst, "Place LLASO Warehouse");

        GameObject fac = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name.StartsWith("LLASO_Facility")) { fac = go; break; }
        if (fac != null)
        {
            inst.transform.SetPositionAndRotation(fac.transform.position, fac.transform.rotation);
            inst.transform.localScale = fac.transform.lossyScale;
            if (fac.transform.parent != null) inst.transform.SetParent(fac.transform.parent, true);
        }
        else Debug.LogWarning("No LLASO_Facility object found in the scene - placed at the origin. Move it to the same transform as the facility.");

        var lights = new GameObject("Warehouse_Lights");
        lights.transform.SetParent(inst.transform, false);
        foreach (var lp in LightPos)
        {
            var go = new GameObject("WarehouseLight");
            go.transform.SetParent(lights.transform, false);
            go.transform.localPosition = lp;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.range = 16f; l.intensity = 1.4f; l.color = new Color(0.95f, 0.97f, 1f);
            l.shadows = LightShadows.None;
        }
        Selection.activeGameObject = inst;
        Debug.Log("Placed " + pick + ". Disable or delete the old Warehouse_Open (1) object.");
    }
}
