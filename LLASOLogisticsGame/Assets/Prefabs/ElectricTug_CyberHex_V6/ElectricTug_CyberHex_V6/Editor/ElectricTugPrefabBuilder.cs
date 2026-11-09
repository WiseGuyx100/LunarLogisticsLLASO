#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Programmatically generates the CyberHex Glass Cargo Electric Tug (V6).
/// Features connected seamless hexagonal glass container, hexagonal rear hatch door, sleek seat cushion, and independent player scaling.
/// </summary>
public static class ElectricTugPrefabBuilder
{
    [MenuItem("GameObject/Logistics Game/Create CyberHex Glass Cargo Tug V6", false, 17)]
    private static void CreateTug()
    {
        // ---------------------------------------------------------
        // 1. MATERIAL PALETTE
        // ---------------------------------------------------------
        Material whiteBody = MakeMaterial("Tug White Body", new Color(0.94f, 0.96f, 0.99f), 0.2f, 0.7f);
        Material blueAccent = MakeMaterial("Tug Blue Accent", new Color(0.06f, 0.35f, 0.90f), 0.4f, 0.8f);
        Material purpleAccent = MakeMaterial("Tug Purple Accent", new Color(0.50f, 0.15f, 0.80f), 0.3f, 0.6f);
        Material greenGlow = MakeEmissiveMaterial("Tug Green Glow", new Color(0.0f, 0.98f, 0.45f), 3.0f);
        Material darkChassis = MakeMaterial("Tug Carbon Frame", new Color(0.10f, 0.12f, 0.16f), 0.5f, 0.5f);
        Material rubberTire = MakeMaterial("Tug Rubber Tire", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f);
        Material seatLeather = MakeMaterial("Tug Seat Upholstery", new Color(0.08f, 0.09f, 0.12f), 0.0f, 0.3f);
        Material sciFiGlass = MakeGlassMaterial("Tug SeeThrough Glass", new Color(0.22f, 0.62f, 0.88f, 0.22f));
        Material chromeHub = MakeMaterial("Tug Chrome Hub", new Color(0.88f, 0.90f, 0.94f), 0.9f, 0.9f);

        // ---------------------------------------------------------
        // 2. ROOT & RIGIDBODY
        // ---------------------------------------------------------
        GameObject root = new GameObject("Electric Tug - CyberHex Glass Cargo Edition V6");
        if (SceneView.lastActiveSceneView != null)
            root.transform.position = SceneView.lastActiveSceneView.pivot;
        else
            root.transform.position = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        ConfigurePhysicsBody(rb);
        Transform t = root.transform;

        // ---------------------------------------------------------
        // 3. MAIN CHASSIS & FRONT NOSE (Zero Z-fighting)
        // ---------------------------------------------------------
        AddBoxVisual(t, "Lower Chassis Core", new Vector3(0f, 0.28f, 0f), new Vector3(1.68f, 0.28f, 4.40f), darkChassis);
        AddBoxCollider(t, "Main Chassis Collider", new Vector3(0f, 0.35f, 0f), new Vector3(1.72f, 0.42f, 4.45f));

        AddBoxVisual(t, "Front Nose Cowling", new Vector3(0f, 0.68f, 1.52f), new Vector3(1.62f, 0.50f, 1.25f), whiteBody);
        AddBoxVisual(t, "Front Bumper Bar", new Vector3(0f, 0.30f, 2.18f), new Vector3(1.78f, 0.24f, 0.16f), blueAccent);
        
        AddBoxVisual(t, "Front LED Left", new Vector3(-0.65f, 0.65f, 2.16f), new Vector3(0.30f, 0.03f, 0.02f), greenGlow);
        AddBoxVisual(t, "Front LED Right", new Vector3(0.65f, 0.65f, 2.16f), new Vector3(0.30f, 0.03f, 0.02f), greenGlow);
        AddBoxVisual(t, "Hood Purple Accent Stripe", new Vector3(0f, 0.94f, 1.52f), new Vector3(0.12f, 0.02f, 0.92f), purpleAccent);

        // ---------------------------------------------------------
        // 4. CAB & PANORAMIC CLEAR-VIEW WINDOWS
        // ---------------------------------------------------------
        AddBoxVisual(t, "Cab Floor Deck", new Vector3(0f, 0.44f, 0.10f), new Vector3(1.60f, 0.06f, 1.40f), darkChassis);

        float pY = 1.44f, pH = 1.15f;
        AddBoxVisual(t, "Pillar Front Left", new Vector3(-0.76f, pY, 0.78f), new Vector3(0.05f, pH, 0.05f), blueAccent);
        AddBoxVisual(t, "Pillar Front Right", new Vector3(0.76f, pY, 0.78f), new Vector3(0.05f, pH, 0.05f), blueAccent);
        AddBoxVisual(t, "Pillar Rear Left", new Vector3(-0.76f, pY, -0.45f), new Vector3(0.05f, pH, 0.05f), blueAccent);
        AddBoxVisual(t, "Pillar Rear Right", new Vector3(0.76f, pY, -0.45f), new Vector3(0.05f, pH, 0.05f), blueAccent);

        AddBoxVisual(t, "Windshield Front", new Vector3(0f, 1.44f, 0.78f), new Vector3(1.48f, 1.05f, 0.02f), sciFiGlass);
        AddBoxVisual(t, "Window Left", new Vector3(-0.76f, 1.44f, 0.16f), new Vector3(0.02f, 1.05f, 1.18f), sciFiGlass);
        AddBoxVisual(t, "Window Right", new Vector3(0.76f, 1.44f, 0.16f), new Vector3(0.02f, 1.05f, 1.18f), sciFiGlass);
        AddBoxVisual(t, "Window Rear", new Vector3(0f, 1.44f, -0.45f), new Vector3(1.48f, 1.05f, 0.02f), sciFiGlass);

        AddBoxVisual(t, "Cab Roof Shell", new Vector3(0f, 2.02f, 0.16f), new Vector3(1.66f, 0.10f, 1.50f), whiteBody);
        AddBoxVisual(t, "Roof Purple Stripe", new Vector3(0f, 2.08f, 0.16f), new Vector3(0.48f, 0.02f, 1.00f), purpleAccent);
        AddBoxVisual(t, "Roof Beacon Dome", new Vector3(0f, 2.14f, 0.45f), new Vector3(0.14f, 0.08f, 0.14f), greenGlow);

        // ---------------------------------------------------------
        // 5. DRIVER COCKPIT & SEAT CUSHION (Sleek low-profile cushion)
        // ---------------------------------------------------------
        AddBoxVisual(t, "Driver Seat Cushion", new Vector3(0f, 0.68f, -0.15f), new Vector3(0.56f, 0.14f, 0.54f), seatLeather);
        
        AddBoxVisual(t, "Dashboard Console", new Vector3(0f, 0.86f, 0.40f), new Vector3(0.92f, 0.26f, 0.30f), darkChassis);
        AddBoxVisual(t, "Hologram Dashboard Display", new Vector3(0f, 0.97f, 0.35f), new Vector3(0.50f, 0.12f, 0.02f), greenGlow);

        GameObject steeringCol = AddBoxVisual(t, "Steering Column", new Vector3(0f, 0.95f, 0.20f), new Vector3(0.06f, 0.28f, 0.06f), darkChassis);
        steeringCol.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        GameObject wheelRim = AddCylinderVisual(t, "Steering Wheel Rim", new Vector3(0f, 1.07f, 0.12f), new Vector3(0.36f, 0.024f, 0.36f), rubberTire);
        wheelRim.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // ---------------------------------------------------------
        // 6. STUDIO LOGO SIDE DECALS
        // ---------------------------------------------------------
        AddLogoDecal(t, "Left Logo Decal", new Vector3(-0.82f, 0.82f, -0.40f), true, whiteBody, blueAccent, purpleAccent, greenGlow);
        AddLogoDecal(t, "Right Logo Decal", new Vector3(0.82f, 0.82f, -0.40f), false, whiteBody, blueAccent, purpleAccent, greenGlow);

        // ---------------------------------------------------------
        // 7. CONNECTED SIDEWAYS HEXAGONAL GLASS CARGO CONTAINER (SOLID COLLISION)
        // ---------------------------------------------------------
        const float bedZ = -2.45f;
        const float bedL = 3.90f;
        const float bedW = 1.84f;
        const float floorY = 0.52f;
        const float hexH = 1.15f;
        const float halfW = bedW * 0.5f;

        // Cargo Floor & Collider
        AddBoxVisual(t, "Cargo Deck Floor", new Vector3(0f, floorY, bedZ), new Vector3(bedW, 0.08f, bedL), darkChassis);
        AddBoxCollider(t, "Cargo Deck Collider", new Vector3(0f, floorY + 0.05f, bedZ), new Vector3(bedW - 0.1f, 0.1f, bedL - 0.2f));

        // Connected Hexagonal Glass Profile Panels & Solid Colliders (Edge-to-edge seamless union):
        // 1. Lower-Left Slanted Wall + Collider
        GameObject hexLL = AddBoxVisual(t, "Hex Glass Lower-Left", new Vector3(-halfW + 0.14f, floorY + 0.34f, bedZ), new Vector3(0.04f, 0.62f, bedL), sciFiGlass);
        hexLL.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        AddBoxCollider(t, "Hex LL Collider", new Vector3(-halfW + 0.14f, floorY + 0.34f, bedZ), new Vector3(0.08f, 0.62f, bedL));

        // 2. Upper-Left Slanted Wall + Collider
        GameObject hexUL = AddBoxVisual(t, "Hex Glass Upper-Left", new Vector3(-halfW + 0.36f, floorY + 0.90f, bedZ), new Vector3(0.04f, 0.62f, bedL), sciFiGlass);
        hexUL.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        AddBoxCollider(t, "Hex UL Collider", new Vector3(-halfW + 0.36f, floorY + 0.90f, bedZ), new Vector3(0.08f, 0.62f, bedL));

        // 3. Lower-Right Slanted Wall + Collider
        GameObject hexLR = AddBoxVisual(t, "Hex Glass Lower-Right", new Vector3(halfW - 0.14f, floorY + 0.34f, bedZ), new Vector3(0.04f, 0.62f, bedL), sciFiGlass);
        hexLR.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        AddBoxCollider(t, "Hex LR Collider", new Vector3(halfW - 0.14f, floorY + 0.34f, bedZ), new Vector3(0.08f, 0.62f, bedL));

        // 4. Upper-Right Slanted Wall + Collider
        GameObject hexUR = AddBoxVisual(t, "Hex Glass Upper-Right", new Vector3(halfW - 0.36f, floorY + 0.90f, bedZ), new Vector3(0.04f, 0.62f, bedL), sciFiGlass);
        hexUR.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        AddBoxCollider(t, "Hex UR Collider", new Vector3(halfW - 0.36f, floorY + 0.90f, bedZ), new Vector3(0.08f, 0.62f, bedL));

        // 5. Flat Glass Roof Apex + Collider (Connecting seamlessly to upper walls)
        AddBoxVisual(t, "Hex Glass Roof Apex", new Vector3(0f, floorY + hexH + 0.04f, bedZ), new Vector3(bedW * 0.40f, 0.04f, bedL), sciFiGlass);
        AddBoxCollider(t, "Hex Roof Collider", new Vector3(0f, floorY + hexH + 0.04f, bedZ), new Vector3(bedW * 0.40f, 0.08f, bedL));

        // Front Glass Bulkhead + Collider
        float frontZ = bedZ + bedL * 0.5f;
        AddBoxVisual(t, "Hex Glass Bulkhead Front", new Vector3(0f, floorY + 0.04f + hexH * 0.5f, frontZ), new Vector3(bedW - 0.1f, hexH, 0.04f), sciFiGlass);
        AddBoxCollider(t, "Hex Bulkhead Front Collider", new Vector3(0f, floorY + 0.04f + hexH * 0.5f, frontZ), new Vector3(bedW - 0.1f, hexH, 0.08f));

        // Structural Frame Beams (Zero Z-fighting union seams)
        AddBoxVisual(t, "Frame Beam Apex", new Vector3(0f, floorY + hexH + 0.07f, bedZ), new Vector3(0.10f, 0.06f, bedL), whiteBody);
        AddBoxVisual(t, "Frame Beam Bottom Left", new Vector3(-halfW + 0.06f, floorY + 0.06f, bedZ), new Vector3(0.08f, 0.08f, bedL), blueAccent);
        AddBoxVisual(t, "Frame Beam Bottom Right", new Vector3(halfW - 0.06f, floorY + 0.06f, bedZ), new Vector3(0.08f, 0.08f, bedL), blueAccent);

        // 8. HEXAGONAL REAR TRUNK HATCH (`Z` key animation target) - Matches the exact hexagon profile
        float rearZ = bedZ - bedL * 0.5f;
        Transform hatchPivot = MakeChild(t, "Cargo Hatch Pivot", new Vector3(0f, floorY + hexH + 0.07f, rearZ));
        
        // Hexagonal Hatch Door panels (Glass center + matching frame bars)
        GameObject hatchGlass = AddBoxVisual(hatchPivot, "Hex Hatch Glass Center", new Vector3(0f, -hexH * 0.5f, 0.02f), new Vector3(bedW - 0.2f, hexH - 0.1f, 0.06f), sciFiGlass);
        AddBoxVisual(hatchPivot, "Hex Hatch Top Frame", new Vector3(0f, -0.06f, 0.03f), new Vector3(bedW - 0.1f, 0.08f, 0.08f), whiteBody);
        AddBoxVisual(hatchPivot, "Hex Hatch Bottom Frame", new Vector3(0f, -hexH + 0.06f, 0.03f), new Vector3(bedW - 0.1f, 0.08f, 0.08f), whiteBody);
        AddBoxVisual(hatchPivot, "Hex Hatch Green Glow Bar", new Vector3(0f, -hexH + 0.16f, 0.05f), new Vector3(bedW - 0.5f, 0.04f, 0.02f), greenGlow);
        
        BoxCollider hatchCol = hatchGlass.AddComponent<BoxCollider>();
        hatchCol.center = Vector3.zero;
        hatchCol.size = new Vector3(bedW - 0.2f, hexH - 0.1f, 0.08f);

        // ---------------------------------------------------------
        // 9. HEAVY INDUSTRIAL WHEELS & FENDERS
        // ---------------------------------------------------------
        Vector3[] wheels = {
            new Vector3(-0.95f, 0.36f,  1.35f),
            new Vector3( 0.95f, 0.36f,  1.35f),
            new Vector3(-0.95f, 0.36f, -1.60f),
            new Vector3( 0.95f, 0.36f, -1.60f)
        };

        for (int i = 0; i < wheels.Length; i++)
        {
            AddSciFiWheel(t, "SciFi Wheel " + (i + 1), wheels[i], rubberTire, chromeHub, blueAccent);
        }

        AddBoxVisual(t, "Fender FL", new Vector3(-0.93f, 0.66f, 1.35f), new Vector3(0.28f, 0.14f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender FR", new Vector3(0.93f, 0.66f, 1.35f), new Vector3(0.28f, 0.14f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender RL", new Vector3(-0.93f, 0.66f, -1.60f), new Vector3(0.28f, 0.14f, 0.90f), darkChassis);
        AddBoxVisual(t, "Fender RR", new Vector3(0.93f, 0.66f, -1.60f), new Vector3(0.28f, 0.14f, 0.90f), darkChassis);

        // ---------------------------------------------------------
        // 10. PLAYER MOUNT & ENTRY TRIGGERS
        // ---------------------------------------------------------
        Transform seatMount = MakeChild(t, "Player Seat Mount", new Vector3(0f, 0.72f, -0.15f));
        Transform exitPoint = MakeChild(t, "Player Exit Point", new Vector3(-1.80f, 0.15f, -0.15f));

        AddEntryTrigger(t, "Trigger Left", new Vector3(-1.18f, 0.95f, 0f), new Vector3(1.10f, 2.10f, 2.40f));
        AddEntryTrigger(t, "Trigger Right", new Vector3(1.18f, 0.95f, 0f), new Vector3(1.10f, 2.10f, 2.40f));
        AddEntryTrigger(t, "Trigger Front", new Vector3(0f, 0.95f, 2.20f), new Vector3(1.90f, 2.10f, 1.10f));
        AddEntryTrigger(t, "Trigger Rear", new Vector3(0f, 0.95f, -4.00f), new Vector3(1.90f, 2.10f, 1.20f));

        // ---------------------------------------------------------
        // 11. CONTROLLER ATTACHMENT & PREFAB CREATION
        // ---------------------------------------------------------
        ElectricTugController controller = root.AddComponent<ElectricTugController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("seatMount").objectReferenceValue = seatMount;
        so.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        so.FindProperty("cargoHatchPivot").objectReferenceValue = hatchPivot;
        so.ApplyModifiedPropertiesWithoutUndo();

        const string path = "Assets/ElectricTug_CyberHex_V6";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "ElectricTug_CyberHex_V6");

        SaveMat(whiteBody, path + "/WhiteBody.mat");
        SaveMat(blueAccent, path + "/BlueAccent.mat");
        SaveMat(purpleAccent, path + "/PurpleAccent.mat");
        SaveMat(greenGlow, path + "/GreenGlow.mat");
        SaveMat(darkChassis, path + "/DarkChassis.mat");
        SaveMat(rubberTire, path + "/RubberTire.mat");
        SaveMat(seatLeather, path + "/SeatLeather.mat");
        SaveMat(sciFiGlass, path + "/SciFiGlass.mat");
        SaveMat(chromeHub, path + "/ChromeHub.mat");

        string prefabPath = path + "/ElectricTug_CyberHex_V6.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));

        Debug.Log("CyberHex Glass Cargo Tug V6 created at: " + prefabPath);
    }

    private static void ConfigurePhysicsBody(Rigidbody rb)
    {
        rb.mass = 5500f;
        rb.linearDamping = 2.2f;
        rb.angularDamping = 5.0f;
        rb.centerOfMass = new Vector3(0f, -0.16f, -0.38f);
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

    private static void AddLogoDecal(Transform parent, string name, Vector3 pos, bool isLeft, Material white, Material blue, Material purple, Material green)
    {
        GameObject decalRoot = new GameObject(name);
        decalRoot.transform.SetParent(parent, false);
        decalRoot.transform.localPosition = pos;

        AddBoxVisual(decalRoot.transform, "Logo Base Plate", Vector3.zero, new Vector3(0.02f, 0.45f, 0.90f), white);
        AddBoxVisual(decalRoot.transform, "Shuttle Body", new Vector3(0.012f, 0.05f, 0.20f), new Vector3(0.015f, 0.12f, 0.35f), white);
        AddBoxVisual(decalRoot.transform, "Blue Ribbon Arc", new Vector3(0.015f, 0.0f, 0f), new Vector3(0.018f, 0.30f, 0.18f), blue);
        AddBoxVisual(decalRoot.transform, "Purple Ribbon Trail", new Vector3(0.015f, 0.12f, -0.28f), new Vector3(0.018f, 0.14f, 0.25f), purple);
        AddBoxVisual(decalRoot.transform, "Green Flame Accent", new Vector3(0.015f, -0.14f, -0.10f), new Vector3(0.018f, 0.08f, 0.20f), green);
    }

    private static void AddSciFiWheel(Transform parent, string name, Vector3 pos, Material tireMat, Material hubMat, Material accentMat)
    {
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = name;
        tire.transform.SetParent(parent, false);
        tire.transform.localPosition = pos;
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(0.74f, 0.14f, 0.74f);
        tire.GetComponent<Renderer>().sharedMaterial = tireMat;
        Object.DestroyImmediate(tire.GetComponent<Collider>());

        float side = Mathf.Sign(pos.x);
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = name + " Hub";
        hub.transform.SetParent(parent, false);
        hub.transform.localPosition = pos + new Vector3(side * 0.142f, 0f, 0f);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        hub.transform.localScale = new Vector3(0.38f, 0.018f, 0.38f);
        hub.GetComponent<Renderer>().sharedMaterial = hubMat;
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        accent.name = name + " Ring";
        accent.transform.SetParent(parent, false);
        accent.transform.localPosition = pos + new Vector3(side * 0.150f, 0f, 0f);
        accent.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        accent.transform.localScale = new Vector3(0.18f, 0.012f, 0.18f);
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
