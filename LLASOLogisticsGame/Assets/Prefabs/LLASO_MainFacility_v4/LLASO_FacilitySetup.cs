// Put in Assets/Editor/. Menu: LLASO > Apply Facility Look (one click).
// Rebuilds the facility materials (white / dark blue / light blue / purple, green accents),
// remaps them onto LLASO_Facility.obj (replaces the dark brown floor), and brightens scene lighting.
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;

public static class LLASO_FacilitySetup
{
    static readonly (string name, Color col, bool glass)[] Mats = {
        ("Fac_White",     new Color(0.96f,0.97f,0.98f), false),
        ("Fac_Grey",      new Color(0.58f,0.61f,0.67f), false),
        ("Fac_DarkBlue",  new Color(0.05f,0.12f,0.33f), false),
        ("Fac_Purple",    new Color(0.38f,0.24f,0.66f), false),
        ("Fac_LightBlue", new Color(0.62f,0.83f,0.97f), false),
        ("Fac_Green",     new Color(0.18f,0.78f,0.42f), false),
        ("Fac_DarkSteel", new Color(0.30f,0.32f,0.37f), false),
        ("Fac_Roof",      new Color(0.88f,0.90f,0.93f), false),
        ("Fac_Glass",     new Color(0.72f,0.88f,0.97f,0.28f), true),
        ("Fac_Floor",     new Color(0.84f,0.86f,0.90f), false),
        ("Fac_Light",     Color.white, false),
    };

    [MenuItem("LLASO/Apply Facility Look")]
    public static void Apply()
    {
        string[] guids = AssetDatabase.FindAssets("LLASO_Facility t:Model");
        string objPath = null;
        foreach (var g in guids) { var p = AssetDatabase.GUIDToAssetPath(g); if (p.EndsWith("LLASO_Facility.obj")) objPath = p; }
        if (objPath == null) { Debug.LogError("LLASO_Facility.obj not found."); return; }

        string dir = "Assets/Materials/LLASO_Facility";
        Directory.CreateDirectory(dir);
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader sh = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        var importer = (ModelImporter)AssetImporter.GetAtPath(objPath);

        foreach (var m in Mats)
        {
            string path = $"{dir}/{m.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = sh;
            mat.color = m.col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", m.col);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", m.glass ? 0.8f : 0.15f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", m.glass ? 0.8f : 0.15f);
            if (m.name == "Fac_Light") { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.white * 1.5f); }
            if (m.glass)
            {
                if (urp) { mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.SetFloat("_ZWrite", 0);
                           mat.SetOverrideTag("RenderType", "Transparent"); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                           mat.renderQueue = (int)RenderQueue.Transparent; }
                else { mat.SetFloat("_Mode", 3); mat.SetInt("_SrcBlend", 5); mat.SetInt("_DstBlend", 10); mat.SetInt("_ZWrite", 0);
                       mat.EnableKeyword("_ALPHABLEND_ON"); mat.renderQueue = 3000; }
            }
            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
        }
        AssetDatabase.SaveAssets();
        importer.SaveAndReimport();

        // brighter, neutral lighting so it reads as a clean white facility
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.color = Color.white; l.intensity = Mathf.Max(l.intensity, 1.2f); Undo.RecordObject(l, "LLASO light"); }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.82f, 0.9f);
        Debug.Log("LLASO facility look applied. Save the scene (Ctrl+S).");
    }
}
