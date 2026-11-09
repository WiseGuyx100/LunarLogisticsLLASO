#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Programmatically generates the Futuristic Heavy-Cargo Electric Tug.
/// Features a massive expanded cargo bed, secure walls, and a striking white/blue/purple/green sci-fi aesthetic.
/// </summary>
public static class ElectricTugPrefabBuilder
{
    [MenuItem("GameObject/Logistics Game/Create Futuristic Heavy Cargo Tug", false, 19)]
    private static void CreateTug()
    {
        // ---------------------------------------------------------
        // 1. MATERIAL PALETTE (White, Blue, Purple, Green Sci-Fi)
        // ---------------------------------------------------------
        Material whiteBody = MakeMaterial("Tug SciFi White", new Color(0.92f, 0.95f, 0.98f), 0.2f, 0.7f);
        Material blueAccent = MakeMaterial("Tug Electric Blue", new Color(0.06f, 0.32f, 0.85f), 0.4f, 0.8f);
        Material purpleAccent = MakeMaterial("Tug Cyber Purple", new Color(0.45f, 0.15f, 0.75f), 0.3f, 0.6f);
        Material greenGlow = MakeEmissiveMaterial("Tug Neon Green", new Color(0.0f, 0.95f, 0.40f), 2.5f);
        Material darkChassis = MakeMaterial("Tug Carbon Frame", new Color(0.12f, 0.15f, 0.20f), 0.5f, 0.5f);
        Material rubberTire = MakeMaterial("Tug Rubber Tire", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f);
        Material seatLeather = MakeMaterial("Tug Seat Upholstery", new Color(0.08f, 0.09f, 0.12f), 0.0f, 0.3f);
        Material sciFiGlass = MakeGlassMaterial("Tug SciFi Glass", new Color(0.20f, 0.60f, 0.85f, 0.25f));
        Material chromeHub = MakeMaterial("Tug Chrome Hub", new Color(0.85f, 0.88f, 0.92f), 0.9f, 0.9f);

        // ---------------------------------------------------------
        // 2. ROOT & RIGIDBODY
        // ---------------------------------------------------------
        GameObject root = new GameObject("Electric Tug - Futuristic Heavy Cargo");
        if (SceneView.lastActiveSceneView != null)
            root.transform.position = SceneView.lastActiveSceneView.pivot;
        else
            root.transform.position = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        ConfigurePhysicsBody(rb);
        Transform t = root.transform;

        // ---------------------------------------------------------
        // 3. MAIN CHASSIS & FRONT NOSE
        // ---------------------------------------------------------
        AddBoxVisual(t, "Lower Chassis Core", new Vector3(0f, 0.30f, 0f), new Vector3(1.60f, 0.30f, 4.20f), darkChassis);
        AddBoxCollider(t, "Main Chassis Collider", new Vector3(0f, 0.35f, 0f), new Vector3(1.65f, 0.45f, 4.25f));

        // Aerodynamic Futuristic Front Nose
        AddBoxVisual(t, "Front Nose Cowling", new Vector3(0f, 0.68f, 1.45f), new Vector3(1.54f, 0.50f, 1.20f), whiteBody);
        AddBoxVisual(t, "Front Bumper Bar", new Vector3(0f, 0.32f, 2.08f), new Vector3(1.70f, 0.26f, 0.16f), blueAccent);
        
        // Neon Green LED Accent Strips on Front
        AddBoxVisual(t, "Front Neon LED Strip Left", new Vector3(-0.62f, 0.65f, 2.06f), new Vector3(0.30f, 0.04f, 0.02f), greenGlow);
        AddBoxVisual(t, "Front Neon LED Strip Right", new Vector3(0.62f, 0.65f, 2.06f), new Vector3(0.30f, 0.04f, 0.02f), greenGlow);
        AddBoxVisual(t, "Cyber Purple Hood Stripe", new Vector3(0f, 0.94f, 1.45f), new Vector3(0.12f, 0.02f, 0.90f), purpleAccent);

        // ---------------------------------------------------------
        // 4. CAB & PANORAMIC CLEAR-VIEW WINDOWS
        // ---------------------------------------------------------
        AddBoxVisual(t, "Cab Floor Deck", new Vector3(0f, 0.46f, 0.10f), new Vector3(1.52f, 0.08f, 1.40f), darkChassis);

        // Corner Pillars (Slim to keep view clear)
        float pY = 1.45f, pH = 1.15f;
        AddBoxVisual(t, "Pillar Front Left", new Vector3(-0.73f, pY, 0.75f), new Vector3(0.06f, pH, 0.06f), blueAccent);
        AddBoxVisual(t, "Pillar Front Right", new Vector3(0.73f, pY, 0.75f), new Vector3(0.06f, pH, 0.06f), blueAccent);
        AddBoxVisual(t, "Pillar Rear Left", new Vector3(-0.73f, pY, -0.45f), new Vector3(0.06f, pH, 0.06f), blueAccent);
        AddBoxVisual(t, "Pillar Rear Right", new Vector3(0.73f, pY, -0.45f), new Vector3(0.06f, pH, 0.06f), blueAccent);

        // Transparent Sci-Fi Glass Panes (Non-blocking, beautiful tint)
        AddBoxVisual(t, "Windshield Front", new Vector3(0f, 1.45f, 0.75f), new Vector3(1.40f, 1.05f, 0.02f), sciFiGlass);
        AddBoxVisual(t, "Window Left", new Vector3(-0.73f, 1.45f, 0.15f), new Vector3(0.02f, 1.05f, 1.18f), sciFiGlass);
        AddBoxVisual(t, "Window Right", new Vector3(0.73f, 1.45f, 0.15f), new Vector3(0.02f, 1.05f, 1.18f), sciFiGlass);
        AddBoxVisual(t, "Window Rear", new Vector3(0f, 1.45f, -0.45f), new Vector3(1.40f, 1.05f, 0.02f), sciFiGlass);

        // Roof Canopy with Purple & Green Accents
        AddBoxVisual(t, "Cab Roof Shell", new Vector3(0f, 2.04f, 0.15f), new Vector3(1.58f, 0.12f, 1.48f), whiteBody);
        AddBoxVisual(t, "Roof Purple Accent Panel", new Vector3(0f, 2.11f, 0.15f), new Vector3(0.50f, 0.02f, 1.00f), purpleAccent);
        AddBoxVisual(t, "Top Beacon Dome", new Vector3(0f, 2.18f, 0.45f), new Vector3(0.16f, 0.10f, 0.16f), greenGlow);

        // ---------------------------------------------------------
        // 5. DRIVER COCKPIT & CONTROLS (Below camera line)
        // ---------------------------------------------------------
        AddBoxVisual(t, "Driver Seat Base", new Vector3(0f, 0.62f, -0.15f), new Vector3(0.55f, 0.32f, 0.55f), darkChassis);
        AddBoxVisual(t, "Seat Cushion", new Vector3(0f, 0.81f, -0.15f), new Vector3(0.58f, 0.08f, 0.55f), seatLeather);
        AddBoxVisual(t, "Seat Backrest", new Vector3(0f, 1.18f, -0.40f), new Vector3(0.56f, 0.64f, 0.10f), seatLeather);
        
        AddBoxVisual(t, "Dashboard Console", new Vector3(0f, 0.88f, 0.40f), new Vector3(0.90f, 0.28f, 0.32f), darkChassis);
        AddBoxVisual(t, "Hologram Dashboard Display", new Vector3(0f, 1.00f, 0.35f), new Vector3(0.50f, 0.14f, 0.02f), greenGlow);

        GameObject steeringCol = AddBoxVisual(t, "Steering Column", new Vector3(0f, 0.98f, 0.20f), new Vector3(0.07f, 0.30f, 0.07f), darkChassis);
        steeringCol.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        GameObject wheelRim = AddCylinderVisual(t, "Steering Wheel Rim", new Vector3(0f, 1.10f, 0.12f), new Vector3(0.38f, 0.025f, 0.38f), rubberTire);
        wheelRim.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // ---------------------------------------------------------
        // 6. MASSIVE EXPANDED REAR CARGO BED (Secured for boxes)
        // ---------------------------------------------------------
        // Massive bed dimensions: Length = 3.8m, Width = 1.85m, Wall Height = 1.0m
        const float bedZ = -2.35f;
        const float bedL = 3.80f;
        const float bedW = 1.85f;
        const float floorY = 0.55f;
        const float wallH = 1.00f;
        const float wallT = 0.12f;

        // Cargo Floor (Visual + Solid Physics Collider)
        AddBoxVisual(t, "Massive Cargo Deck Floor", new Vector3(0f, floorY, bedZ), new Vector3(bedW, 0.10f, bedL), whiteBody);
        AddBoxCollider(t, "Massive Cargo Deck Collider", new Vector3(0f, floorY, bedZ), new Vector3(bedW, 0.10f, bedL));

        // High Containment Walls & Rails (Prevents boxes from falling out or glitching)
        float sideX = bedW * 0.5f - wallT * 0.5f;
        float wallCenterY = floorY + 0.05f + wallH * 0.5f;

        // Left Wall
        AddBoxVisual(t, "Cargo Left Wall", new Vector3(-sideX, wallCenterY, bedZ), new Vector3(wallT, wallH, bedL), whiteBody);
        AddBoxCollider(t, "Cargo Left Wall Collider", new Vector3(-sideX, wallCenterY, bedZ), new Vector3(wallT, wallH, bedL));
        AddBoxVisual(t, "Cargo Left Wall Blue Stripe", new Vector3(-sideX - 0.01f, wallCenterY, bedZ), new Vector3(0.02f, 0.15f, bedL - 0.4f), blueAccent);

        // Right Wall
        AddBoxVisual(t, "Cargo Right Wall", new Vector3(sideX, wallCenterY, bedZ), new Vector3(wallT, wallH, bedL), whiteBody);
        AddBoxCollider(t, "Cargo Right Wall Collider", new Vector3(sideX, wallCenterY, bedZ), new Vector3(wallT, wallH, bedL));
        AddBoxVisual(t, "Cargo Right Wall Blue Stripe", new Vector3(sideX + 0.01f, wallCenterY, bedZ), new Vector3(0.02f, 0.15f, bedL - 0.4f), blueAccent);

        // Front Barrier (Behind cab)
        float frontZ = bedZ + bedL * 0.5f - wallT * 0.5f;
        AddBoxVisual(t, "Cargo Front Barrier", new Vector3(0f, wallCenterY, frontZ), new Vector3(bedW, wallH, wallT), whiteBody);
        AddBoxCollider(t, "Cargo Front Barrier Collider", new Vector3(0f, wallCenterY, frontZ), new Vector3(bedW, wallH, wallT));
        AddBoxVisual(t, "Cargo Front Purple Emblem", new Vector3(0f, wallCenterY, frontZ + 0.02f), new Vector3(0.5f, 0.3f, 0.02f), purpleAccent);

        // Rear Tailgate (High and secure)
        float rearZ = bedZ - bedL * 0.5f + wallT * 0.5f;
        AddBoxVisual(t, "Cargo Rear Tailgate", new Vector3(0f, wallCenterY, rearZ), new Vector3(bedW, wallH, wallT), darkChassis);
        AddBoxCollider(t, "Cargo Rear Tailgate Collider", new Vector3(0f, wallCenterY, rearZ), new Vector3(bedW, wallH, wallT));
        AddBoxVisual(t, "Cargo Rear Green Glow Bar", new Vector3(0f, wallCenterY + 0.35f, rearZ - 0.01f), new Vector3(bedW - 0.4f, 0.06f, 0.02f), greenGlow);

        // Heavy Tow Hitch
        AddBoxVisual(t, "Heavy Rear Tow Hitch", new Vector3(0f, 0.32f, rearZ - 0.15f), new Vector3(0.40f, 0.24f, 0.18f), darkChassis);

        // ---------------------------------------------------------
        // 7. HEAVY INDUSTRIAL WHEELS & FENDERS
        // ---------------------------------------------------------
        Vector3[] wheels = {
            new Vector3(-0.92f, 0.38f,  1.30f), // Front Left
            new Vector3( 0.92f, 0.38f,  1.30f), // Front Right
            new Vector3(-0.92f, 0.38f, -1.55f), // Rear Left
            new Vector3( 0.92f, 0.38f, -1.55f)  // Rear Right
        };

        for (int i = 0; i < wheels.Length; i++)
        {
            AddSciFiWheel(t, "SciFi Wheel " + (i + 1), wheels[i], rubberTire, chromeHub, blueAccent);
        }

        // Fenders
        AddBoxVisual(t, "Fender FL", new Vector3(-0.90f, 0.68f, 1.30f), new Vector3(0.28f, 0.16f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender FR", new Vector3(0.90f, 0.68f, 1.30f), new Vector3(0.28f, 0.16f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender RL", new Vector3(-0.90f, 0.68f, -1.55f), new Vector3(0.28f, 0.16f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender RR", new Vector3(0.90f, 0.68f, -1.55f), new Vector3(0.28f, 0.16f, 0.90f), darkChassis);

        // ---------------------------------------------------------
        // 8. PLAYER MOUNT & ENTRY TRIGGERS
        // ---------------------------------------------------------
        Transform seatMount = MakeChild(t, "Player Seat Mount", new Vector3(0f, 0.85f, -0.15f));
        Transform exitPoint = MakeChild(t, "Player Exit Point", new Vector3(-1.75f, 0.15f, -0.15f));

        AddEntryTrigger(t, "Trigger Left", new Vector3(-1.15f, 1.00f, 0f), new Vector3(1.10f, 2.20f, 2.40f));
        AddEntryTrigger(t, "Trigger Right", new Vector3(1.15f, 1.00f, 0f), new Vector3(1.10f, 2.20f, 2.40f));
        AddEntryTrigger(t, "Trigger Front", new Vector3(0f, 1.00f, 2.10f), new Vector3(1.90f, 2.20f, 1.10f));
        AddEntryTrigger(t, "Trigger Rear", new Vector3(0f, 1.00f, -3.80f), new Vector3(1.90f, 2.20f, 1.20f));

        // ---------------------------------------------------------
        // 9. CONTROLLER & PREFAB CREATION
        // ---------------------------------------------------------
        ElectricTugController controller = root.AddComponent<ElectricTugController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("seatMount").objectReferenceValue = seatMount;
        so.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        so.ApplyModifiedPropertiesWithoutUndo();

        const string path = "Assets/ElectricTug_Futuristic_HeavyCargo";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "ElectricTug_Futuristic_HeavyCargo");

        SaveMat(whiteBody, path + "/WhiteBody.mat");
        SaveMat(blueAccent, path + "/BlueAccent.mat");
        SaveMat(purpleAccent, path + "/PurpleAccent.mat");
        SaveMat(greenGlow, path + "/GreenGlow.mat");
        SaveMat(darkChassis, path + "/DarkChassis.mat");
        SaveMat(rubberTire, path + "/RubberTire.mat");
        SaveMat(seatLeather, path + "/SeatLeather.mat");
        SaveMat(sciFiGlass, path + "/SciFiGlass.mat");
        SaveMat(chromeHub, path + "/ChromeHub.mat");

        string prefabPath = path + "/ElectricTug_Futuristic_HeavyCargo.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));

        Debug.Log("Futuristic Heavy Cargo Tug created at: " + prefabPath);
    }

    private static void ConfigurePhysicsBody(Rigidbody rb)
    {
        rb.mass = 5200f; // Heavy stability for massive cargo
        rb.linearDamping = 2.2f;
        rb.angularDamping = 5.0f;
        rb.centerOfMass = new Vector3(0f, -0.18f, -0.4f); // Shifted slightly back for rear cargo balance
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private static GameObject AddBoxVisual(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = pos;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject AddCylinderVisual(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.SetParent(parent, false);
        cyl.transform.localPosition = pos;
        cyl.transform.localScale = size;
        cyl.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cyl.GetComponent<Collider>());
        return cyl;
    }

    private static BoxCollider AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.size = size;
        return col;
    }

    private static void AddSciFiWheel(Transform parent, string name, Vector3 pos, Material tireMat, Material hubMat, Material accentMat)
    {
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = name;
        tire.transform.SetParent(parent, false);
        tire.transform.localPosition = pos;
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(0.74f, 0.15f, 0.74f);
        tire.GetComponent<Renderer>().sharedMaterial = tireMat;
        Object.DestroyImmediate(tire.GetComponent<Collider>());

        float side = Mathf.Sign(pos.x);
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = name + " Hub";
        hub.transform.SetParent(parent, false);
        hub.transform.localPosition = pos + new Vector3(side * 0.15f, 0f, 0f);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        hub.transform.localScale = new Vector3(0.40f, 0.02f, 0.40f);
        hub.GetComponent<Renderer>().sharedMaterial = hubMat;
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        accent.name = name + " Ring";
        accent.transform.SetParent(parent, false);
        accent.transform.localPosition = pos + new Vector3(side * 0.16f, 0f, 0f);
        accent.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        accent.transform.localScale = new Vector3(0.20f, 0.015f, 0.20f);
        accent.GetComponent<Renderer>().sharedMaterial = accentMat;
        Object.DestroyImmediate(accent.GetComponent<Collider>());
    }

    private static void AddEntryTrigger(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
    }

    private static Transform MakeChild(Transform parent, string name, Vector3 pos)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = pos;
        return child.transform;
    }

    private static Material MakeMaterial(string name, Color col, float metallic = 0.0f, float smoothness = 0.5f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = col };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    private static Material MakeEmissiveMaterial(string name, Color col, float intensity)
    {
        Material mat = MakeMaterial(name, col, 0.2f, 0.8f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * intensity);
        return mat;
    }

    private static Material MakeGlassMaterial(string name, Color col)
    {
        Material mat = MakeMaterial(name, col);
        if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        return mat;
    }

    private static void SaveMat(Material mat, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CreateAsset(mat, path);
        else
            Object.DestroyImmediate(mat);
    }
}
#endif
