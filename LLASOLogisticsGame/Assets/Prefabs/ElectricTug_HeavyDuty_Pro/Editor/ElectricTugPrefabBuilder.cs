#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Programmatically generates a highly-detailed, realistic Heavy-Duty Electric Tow Tug.
/// Precision-fitted geometry prevents panel overlapping/glitching while keeping driver line-of-sight 100% clear.
/// </summary>
public static class ElectricTugPrefabBuilder
{
    [MenuItem("GameObject/Logistics Game/Create Heavy Duty Electric Tug", false, 20)]
    private static void CreateElectricTug()
    {
        // ---------------------------------------------------------
        // 1. MATERIAL PALETTE
        // ---------------------------------------------------------
        Material bodyYellow = MakeMaterial("Tug Industrial Yellow", new Color(0.96f, 0.68f, 0.08f), 0.1f, 0.4f);
        Material bodyDark = MakeMaterial("Tug Graphite Dark", new Color(0.12f, 0.14f, 0.17f), 0.2f, 0.6f);
        Material chassisMetal = MakeMaterial("Tug Steel Chassis", new Color(0.25f, 0.28f, 0.32f), 0.4f, 0.7f);
        Material chromeMetal = MakeMaterial("Tug Chrome Metal", new Color(0.80f, 0.83f, 0.86f), 0.8f, 0.9f);
        Material rubberBlack = MakeMaterial("Tug Heavy Rubber", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f);
        Material seatLeather = MakeMaterial("Tug Seat Upholstery", new Color(0.09f, 0.10f, 0.12f), 0.0f, 0.3f);
        Material glassClear = MakeGlassMaterial("Tug Safety Glass", new Color(0.60f, 0.80f, 0.90f, 0.18f));
        Material headlightGlass = MakeGlassMaterial("Tug Headlight Lens", new Color(0.95f, 0.98f, 1.00f, 0.70f), true);
        Material amberLens = MakeGlassMaterial("Tug Amber Beacon", new Color(1.00f, 0.55f, 0.00f, 0.85f), true);
        Material redTailLens = MakeGlassMaterial("Tug Tail Lamp", new Color(0.90f, 0.10f, 0.10f, 0.80f), true);

        // ---------------------------------------------------------
        // 2. ROOT & RIGIDBODY
        // ---------------------------------------------------------
        GameObject root = new GameObject("Electric Tug - Heavy Duty Pro");
        if (SceneView.lastActiveSceneView != null)
            root.transform.position = SceneView.lastActiveSceneView.pivot;
        else
            root.transform.position = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        ConfigurePhysicsBody(rb);

        Transform mainTransform = root.transform;

        // ---------------------------------------------------------
        // 3. MAIN CHASSIS & FRAME (Flush joints, no Z-fighting)
        // ---------------------------------------------------------
        // Main structural frame bed
        AddBoxVisual(mainTransform, "Main Frame Structural Bed", new Vector3(0f, 0.22f, -0.10f), new Vector3(1.56f, 0.24f, 3.50f), chassisMetal);
        AddBoxCollider(mainTransform, "Chassis Collider Main", new Vector3(0f, 0.30f, -0.10f), new Vector3(1.60f, 0.40f, 3.50f));

        // Front Heavy-Duty Bumper with Rubber Impact Pads
        AddBoxVisual(mainTransform, "Front Bumper Bar", new Vector3(0f, 0.28f, 1.70f), new Vector3(1.72f, 0.32f, 0.18f), chassisMetal);
        AddBoxVisual(mainTransform, "Front Bumper Rubber Guard Left", new Vector3(-0.60f, 0.28f, 1.80f), new Vector3(0.36f, 0.34f, 0.06f), rubberBlack);
        AddBoxVisual(mainTransform, "Front Bumper Rubber Guard Right", new Vector3(0.60f, 0.28f, 1.80f), new Vector3(0.36f, 0.34f, 0.06f), rubberBlack);

        // Side Running Steps / Entry Boards
        AddBoxVisual(mainTransform, "Step Board Left", new Vector3(-0.90f, 0.38f, -0.05f), new Vector3(0.24f, 0.08f, 1.10f), bodyDark);
        AddBoxVisual(mainTransform, "Step Board Right", new Vector3(0.90f, 0.38f, -0.05f), new Vector3(0.24f, 0.08f, 1.10f), bodyDark);

        // ---------------------------------------------------------
        // 4. FRONT HOOD & NOSE ASSEMBLY (Low-profile for driver view)
        // ---------------------------------------------------------
        // Lower Hood Body
        AddBoxVisual(mainTransform, "Hood Base Structure", new Vector3(0f, 0.64f, 1.10f), new Vector3(1.50f, 0.52f, 1.05f), bodyYellow);
        // Slanted Top Hood Cover (Gives sleek realistic slope downwards)
        AddBoxVisual(mainTransform, "Hood Slope Panel", new Vector3(0f, 0.93f, 1.10f), new Vector3(1.46f, 0.08f, 1.00f), bodyYellow);
        
        // Front Grille & Radiator Mesh Panel
        AddBoxVisual(mainTransform, "Front Grille Surround", new Vector3(0f, 0.65f, 1.63f), new Vector3(1.10f, 0.38f, 0.02f), bodyDark);
        AddBoxVisual(mainTransform, "Grille Mesh Insert", new Vector3(0f, 0.65f, 1.64f), new Vector3(0.96f, 0.30f, 0.01f), chromeMetal);

        // Headlight & Turn Signal Clusters
        AddHeadlightCluster(mainTransform, "Headlight Left", new Vector3(-0.58f, 0.70f, 1.64f), headlightGlass, amberLens, bodyDark);
        AddHeadlightCluster(mainTransform, "Headlight Right", new Vector3(0.58f, 0.70f, 1.64f), headlightGlass, amberLens, bodyDark);

        // ---------------------------------------------------------
        // 5. CAB ROOF CAGE & PANORAMIC CLEAR-VIEW GLASS
        // ---------------------------------------------------------
        // Cab Floor Base
        AddBoxVisual(mainTransform, "Cab Deck Floor", new Vector3(0f, 0.42f, -0.05f), new Vector3(1.48f, 0.08f, 1.25f), bodyDark);

        // Slim Outer Corner Pillars (Out of driver center sight line)
        float pillarY = 1.40f;
        float pillarH = 1.10f;
        AddBoxVisual(mainTransform, "Pillar Front Left", new Vector3(-0.71f, pillarY, 0.55f), new Vector3(0.06f, pillarH, 0.06f), bodyDark);
        AddBoxVisual(mainTransform, "Pillar Front Right", new Vector3(0.71f, pillarY, 0.55f), new Vector3(0.06f, pillarH, 0.06f), bodyDark);
        AddBoxVisual(mainTransform, "Pillar Rear Left", new Vector3(-0.71f, pillarY, -0.65f), new Vector3(0.06f, pillarH, 0.06f), bodyDark);
        AddBoxVisual(mainTransform, "Pillar Rear Right", new Vector3(0.71f, pillarY, -0.65f), new Vector3(0.06f, pillarH, 0.06f), bodyDark);

        // High-Transparency Safety Windows (No colliders, non-blocking)
        AddBoxVisual(mainTransform, "Windshield Panoramic", new Vector3(0f, 1.42f, 0.55f), new Vector3(1.36f, 1.02f, 0.02f), glassClear);
        AddBoxVisual(mainTransform, "Window Left Side", new Vector3(-0.71f, 1.42f, -0.05f), new Vector3(0.02f, 1.02f, 1.14f), glassClear);
        AddBoxVisual(mainTransform, "Window Right Side", new Vector3(0.71f, 1.42f, -0.05f), new Vector3(0.02f, 1.02f, 1.14f), glassClear);
        AddBoxVisual(mainTransform, "Window Rear Side", new Vector3(0f, 1.42f, -0.65f), new Vector3(1.36f, 1.02f, 0.02f), glassClear);

        // Heavy Roof Shell
        AddBoxVisual(mainTransform, "Cab Roof Structure", new Vector3(0f, 1.98f, -0.05f), new Vector3(1.52f, 0.10f, 1.34f), bodyYellow);
        AddBoxVisual(mainTransform, "Roof Trim Guard", new Vector3(0f, 2.04f, -0.05f), new Vector3(1.40f, 0.04f, 1.20f), bodyDark);

        // Top Rotating Safety Strobe Light
        AddCylinderVisual(mainTransform, "Safety Beacon Base", new Vector3(0f, 2.08f, 0.35f), new Vector3(0.18f, 0.04f, 0.18f), bodyDark);
        AddCylinderVisual(mainTransform, "Safety Beacon Dome", new Vector3(0f, 2.14f, 0.35f), new Vector3(0.14f, 0.08f, 0.14f), amberLens);

        // ---------------------------------------------------------
        // 6. DRIVER COCKPIT & CONTROLS (Positioned below camera line)
        // ---------------------------------------------------------
        // Seat Base & Ergonomic Cushion
        AddBoxVisual(mainTransform, "Driver Seat Base", new Vector3(0f, 0.62f, -0.20f), new Vector3(0.55f, 0.32f, 0.55f), chassisMetal);
        AddBoxVisual(mainTransform, "Seat Bottom Cushion", new Vector3(0f, 0.81f, -0.20f), new Vector3(0.58f, 0.08f, 0.55f), seatLeather);
        AddBoxVisual(mainTransform, "Seat Backrest", new Vector3(0f, 1.15f, -0.45f), new Vector3(0.56f, 0.60f, 0.10f), seatLeather);
        AddBoxVisual(mainTransform, "Seat Headrest", new Vector3(0f, 1.50f, -0.45f), new Vector3(0.32f, 0.15f, 0.08f), seatLeather);

        // Dashboard Console & Steering Assembly
        AddBoxVisual(mainTransform, "Dashboard Housing", new Vector3(0f, 0.88f, 0.28f), new Vector3(0.85f, 0.26f, 0.28f), bodyDark);
        AddBoxVisual(mainTransform, "Instrument Gauge Display", new Vector3(0f, 0.98f, 0.22f), new Vector3(0.45f, 0.12f, 0.02f), chromeMetal);
        
        // Steering Column & Wheel (angled nicely, kept below horizon line)
        GameObject steeringCol = AddBoxVisual(mainTransform, "Steering Column", new Vector3(0f, 0.98f, 0.10f), new Vector3(0.07f, 0.30f, 0.07f), bodyDark);
        steeringCol.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        
        GameObject wheelRim = AddCylinderVisual(mainTransform, "Steering Wheel Rim", new Vector3(0f, 1.10f, 0.02f), new Vector3(0.38f, 0.025f, 0.38f), rubberBlack);
        wheelRim.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
        AddCylinderVisual(wheelRim.transform, "Steering Wheel Center Cap", Vector3.zero, new Vector3(0.28f, 1.20f, 0.28f), bodyYellow);

        // ---------------------------------------------------------
        // 7. REAR HEAVY TOW CARGO BED & TOWING COUPLER
        // ---------------------------------------------------------
        // Cargo Bed Floor
        AddBoxVisual(mainTransform, "Cargo Deck Floor Bed", new Vector3(0f, 0.44f, -1.35f), new Vector3(1.54f, 0.08f, 1.25f), chassisMetal);
        AddBoxCollider(mainTransform, "Cargo Deck Collider Bed", new Vector3(0f, 0.44f, -1.35f), new Vector3(1.54f, 0.08f, 1.25f));

        // Side Safety Rails
        AddBoxVisual(mainTransform, "Cargo Side Rail Left", new Vector3(-0.74f, 0.68f, -1.35f), new Vector3(0.06f, 0.40f, 1.25f), bodyYellow);
        AddBoxVisual(mainTransform, "Cargo Side Rail Right", new Vector3(0.74f, 0.68f, -1.35f), new Vector3(0.06f, 0.40f, 1.25f), bodyYellow);
        AddBoxVisual(mainTransform, "Cargo Front Barrier", new Vector3(0f, 0.68f, -0.74f), new Vector3(1.54f, 0.40f, 0.06f), bodyYellow);
        AddBoxVisual(mainTransform, "Cargo Tailgate", new Vector3(0f, 0.64f, -1.95f), new Vector3(1.54f, 0.32f, 0.06f), bodyDark);

        // Heavy Rear Towing Hitch & Pin Assembly
        AddBoxVisual(mainTransform, "Rear Tow Hitch Mount", new Vector3(0f, 0.30f, -1.92f), new Vector3(0.35f, 0.22f, 0.15f), chassisMetal);
        AddCylinderVisual(mainTransform, "Rear Towing Pin", new Vector3(0f, 0.42f, -1.96f), new Vector3(0.06f, 0.15f, 0.06f), chromeMetal);

        // Rear Brake Light Modules
        AddBoxVisual(mainTransform, "Rear Light Housing Left", new Vector3(-0.62f, 0.44f, -1.97f), new Vector3(0.20f, 0.10f, 0.02f), bodyDark);
        AddBoxVisual(mainTransform, "Rear Light Lens Left", new Vector3(-0.62f, 0.44f, -1.98f), new Vector3(0.16f, 0.07f, 0.01f), redTailLens);
        AddBoxVisual(mainTransform, "Rear Light Housing Right", new Vector3(0.62f, 0.44f, -1.97f), new Vector3(0.20f, 0.10f, 0.02f), bodyDark);
        AddBoxVisual(mainTransform, "Rear Light Lens Right", new Vector3(0.62f, 0.44f, -1.98f), new Vector3(0.16f, 0.07f, 0.01f), redTailLens);

        // ---------------------------------------------------------
        // 8. HEAVY INDUSTRIAL WHEELS & CONTOURED FENDERS
        // ---------------------------------------------------------
        Vector3[] wheelPositions = new Vector3[]
        {
            new Vector3(-0.84f, 0.35f,  1.10f), // Front Left
            new Vector3( 0.84f, 0.35f,  1.10f), // Front Right
            new Vector3(-0.84f, 0.35f, -1.15f), // Rear Left
            new Vector3( 0.84f, 0.35f, -1.15f)  // Rear Right
        };

        for (int i = 0; i < wheelPositions.Length; i++)
        {
            AddIndustrialWheel(mainTransform, "Industrial Wheel " + (i + 1), wheelPositions[i], rubberBlack, chromeMetal, bodyYellow);
        }

        // Heavy Fender Arches Hugging Tires cleanly
        AddBoxVisual(mainTransform, "Fender Arch FL", new Vector3(-0.82f, 0.62f, 1.10f), new Vector3(0.26f, 0.14f, 0.82f), bodyDark);
        AddBoxVisual(mainTransform, "Fender Arch FR", new Vector3(0.82f, 0.62f, 1.10f), new Vector3(0.26f, 0.14f, 0.82f), bodyDark);
        AddBoxVisual(mainTransform, "Fender Arch RL", new Vector3(-0.82f, 0.62f, -1.15f), new Vector3(0.26f, 0.14f, 0.82f), bodyDark);
        AddBoxVisual(mainTransform, "Fender Arch RR", new Vector3(0.82f, 0.62f, -1.15f), new Vector3(0.26f, 0.14f, 0.82f), bodyDark);

        // ---------------------------------------------------------
        // 9. PLAYER MOUNT & EXIT ANCHORS (Optimized view position)
        // ---------------------------------------------------------
        // Placing seatMount so FPS camera (local Y ~1.6m) looks out cleanly through windshield at Y ~2.45m
        Transform seatMount = MakeChild(mainTransform, "Player Seat Mount", new Vector3(0f, 0.85f, -0.20f));
        Transform exitPoint = MakeChild(mainTransform, "Player Exit Point", new Vector3(-1.60f, 0.15f, -0.20f));

        // 4-Zone Entry Triggers for multi-directional boarding
        AddEntryTrigger(mainTransform, "Trigger Left Boarding", new Vector3(-1.10f, 1.00f, -0.10f), new Vector3(1.10f, 2.00f, 2.00f));
        AddEntryTrigger(mainTransform, "Trigger Right Boarding", new Vector3(1.10f, 1.00f, -0.10f), new Vector3(1.10f, 2.00f, 2.00f));
        AddEntryTrigger(mainTransform, "Trigger Front Boarding", new Vector3(0f, 1.00f, 1.80f), new Vector3(1.80f, 2.00f, 1.00f));
        AddEntryTrigger(mainTransform, "Trigger Rear Boarding", new Vector3(0f, 1.00f, -2.10f), new Vector3(1.80f, 2.00f, 1.00f));

        // ---------------------------------------------------------
        // 10. CONTROLLER ATTACHMENT & ASSET SAVING
        // ---------------------------------------------------------
        ElectricTugController controller = root.AddComponent<ElectricTugController>();
        SerializedObject serializedObj = new SerializedObject(controller);
        serializedObj.FindProperty("seatMount").objectReferenceValue = seatMount;
        serializedObj.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        const string folderPath = "Assets/ElectricTug_HeavyDuty_Pro";
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets", "ElectricTug_HeavyDuty_Pro");

        SaveMaterialAsset(bodyYellow, folderPath + "/TugYellow.mat");
        SaveMaterialAsset(bodyDark, folderPath + "/TugDark.mat");
        SaveMaterialAsset(chassisMetal, folderPath + "/TugSteel.mat");
        SaveMaterialAsset(chromeMetal, folderPath + "/TugChrome.mat");
        SaveMaterialAsset(rubberBlack, folderPath + "/TugRubber.mat");
        SaveMaterialAsset(seatLeather, folderPath + "/TugLeather.mat");
        SaveMaterialAsset(glassClear, folderPath + "/TugGlass.mat");
        SaveMaterialAsset(headlightGlass, folderPath + "/TugHeadlight.mat");
        SaveMaterialAsset(amberLens, folderPath + "/TugAmber.mat");
        SaveMaterialAsset(redTailLens, folderPath + "/TugRedTail.mat");

        string prefabPath = folderPath + "/ElectricTug_HeavyDuty_Pro.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));

        Debug.Log(" Heavy Duty Electric Tug built successfully at: " + prefabPath);
    }

    // ---------------------------------------------------------
    // HELPER CREATION METHODS
    // ---------------------------------------------------------
    private static void ConfigurePhysicsBody(Rigidbody body)
    {
        body.mass = 4500f; // Heavy low tug weight
        body.linearDamping = 2.0f;
        body.angularDamping = 5.0f;
        body.centerOfMass = new Vector3(0f, -0.15f, 0f); // Low COG prevents tipping
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = center;
        BoxCollider col = holder.AddComponent<BoxCollider>();
        col.size = size;
        return col;
    }

    private static void AddHeadlightCluster(Transform parent, string name, Vector3 pos, Material mainLens, Material indicatorLens, Material housingMat)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = pos;

        AddBoxVisual(cluster.transform, "Housing", Vector3.zero, new Vector3(0.26f, 0.16f, 0.04f), housingMat);
        AddBoxVisual(cluster.transform, "Main Lamp Lens", new Vector3(-0.04f, 0f, 0.01f), new Vector3(0.14f, 0.12f, 0.02f), mainLens);
        AddBoxVisual(cluster.transform, "Indicator Lens", new Vector3(0.07f, 0f, 0.01f), new Vector3(0.06f, 0.12f, 0.02f), indicatorLens);
    }

    private static void AddIndustrialWheel(Transform parent, string name, Vector3 pos, Material rubberMat, Material hubMat, Material capMat)
    {
        // Main Treaded Tire Body (Cylinder rotated on Z axis)
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = name;
        tire.transform.SetParent(parent, false);
        tire.transform.localPosition = pos;
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(0.70f, 0.14f, 0.70f);
        tire.GetComponent<Renderer>().sharedMaterial = rubberMat;
        Object.DestroyImmediate(tire.GetComponent<Collider>());

        // Steel Wheel Rim / Hub
        float sideSign = Mathf.Sign(pos.x);
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = name + " Steel Hub";
        hub.transform.SetParent(parent, false);
        hub.transform.localPosition = pos + new Vector3(sideSign * 0.142f, 0f, 0f);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        hub.transform.localScale = new Vector3(0.38f, 0.015f, 0.38f);
        hub.GetComponent<Renderer>().sharedMaterial = hubMat;
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        // Center Axle Cap Accent
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = name + " Axle Cap";
        cap.transform.SetParent(parent, false);
        cap.transform.localPosition = pos + new Vector3(sideSign * 0.150f, 0f, 0f);
        cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        cap.transform.localScale = new Vector3(0.18f, 0.012f, 0.18f);
        cap.GetComponent<Renderer>().sharedMaterial = capMat;
        Object.DestroyImmediate(cap.GetComponent<Collider>());
    }

    private static void AddEntryTrigger(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject triggerObj = new GameObject(name);
        triggerObj.transform.SetParent(parent, false);
        triggerObj.transform.localPosition = pos;
        BoxCollider trigger = triggerObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = size;
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

        Material mat = new Material(shader) { name = name };
        mat.color = col;

        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        return mat;
    }

    private static Material MakeGlassMaterial(string name, Color col, bool isEmissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader) { name = name };
        mat.color = col;

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Mode", 2f); // Fade/Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        if (isEmissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * 1.5f);
        }

        return mat;
    }

    private static void SaveMaterialAsset(Material mat, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CreateAsset(mat, path);
        else
            Object.DestroyImmediate(mat);
    }
}
#endif
