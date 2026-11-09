#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Builds a clean, mostly-white, heavy-duty electric cargo tug.</summary>
public static class ElectricTugPrefabBuilder
{
    [MenuItem("GameObject/Logistics Game/Create Electric Tug (Heavy Duty Clear-View Edition)", false, 21)]
    private static void CreateTug()
    {
        Material white = MakeMaterial("Tug HD White", new Color(.95f, .96f, .98f));
        Material whiteShade = MakeMaterial("Tug HD Shadow White", new Color(.76f, .80f, .86f));
        Material blue = MakeMaterial("Tug HD Blue", new Color(.06f, .28f, .68f));
        Material purple = MakeMaterial("Tug HD Purple", new Color(.39f, .19f, .68f));
        Material rubber = MakeMaterial("Tug HD Rubber", new Color(.035f, .045f, .06f));
        Material seat = MakeMaterial("Tug HD Seat", new Color(.10f, .12f, .16f));
        Material glass = MakeGlassMaterial("Tug HD Glass", new Color(.22f, .42f, .50f, .20f));
        Material metal = MakeMaterial("Tug HD Metal", new Color(.42f, .48f, .56f));

        GameObject root = new GameObject("Electric Tug - Heavy Duty Edition");
        root.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        ConfigureBody(rb);

        // FRONT (+Z). Every body panel is separated by a visible gap; no stacked hood layers.
        AddBoxVisual(root.transform, "Lower front chassis", new Vector3(0, .31f, -.39f), new Vector3(1.42f, .46f, 2.34f), whiteShade);
        AddBoxCollider(root.transform, "Main chassis collider", new Vector3(0, .31f, -.39f), new Vector3(1.42f, .46f, 2.34f));
        AddBoxVisual(root.transform, "Single-piece hood", new Vector3(0, .99f, 1.13f), new Vector3(1.38f, .48f, 1.00f), white);
        AddBoxVisual(root.transform, "Front bumper", new Vector3(0, .69f, 1.70f), new Vector3(1.48f, .18f, .14f), blue);
        AddBoxVisual(root.transform, "Grille", new Vector3(0, .91f, 1.642f), new Vector3(.52f, .10f, .018f), metal);
        AddBoxVisual(root.transform, "Left headlamp", new Vector3(-.52f, 1.06f, 1.642f), new Vector3(.22f, .13f, .018f), metal);
        AddBoxVisual(root.transform, "Right headlamp", new Vector3(.52f, 1.06f, 1.642f), new Vector3(.22f, .13f, .018f), metal);
        AddBoxVisual(root.transform, "Blue hood center stripe", new Vector3(0, 1.235f, 1.13f), new Vector3(.10f, .012f, .72f), blue);

        // Driver station: keep the forward sightline clear. Transparent panes sit in a
        // separated frame; none of the glass or trim has a collider.
        AddBoxVisual(root.transform, "Seat pedestal", new Vector3(0, 1.22f, -.48f), new Vector3(.34f, .40f, .36f), whiteShade);
        AddBoxVisual(root.transform, "Raised seat cushion", new Vector3(0, 1.54f, -.48f), new Vector3(.70f, .16f, .66f), seat);
        AddBoxVisual(root.transform, "Seat backrest", new Vector3(0, 1.91f, -.78f), new Vector3(.70f, .58f, .16f), seat);
        AddBoxVisual(root.transform, "Seat headrest", new Vector3(0, 2.28f, -.78f), new Vector3(.36f, .16f, .16f), whiteShade);
        AddBoxVisual(root.transform, "Seat blue piping", new Vector3(0, 1.625f, -.48f), new Vector3(.56f, .012f, .52f), blue);

        // Four-corner cab cage with clear glass. Front glass is forward of the driver's
        // eye line; side/rear panes make the frame feel integrated without blocking view.
        AddBoxVisual(root.transform, "Front panoramic windshield", new Vector3(0, 2.36f, .78f), new Vector3(1.24f, .94f, .025f), glass);
        AddBoxVisual(root.transform, "Left side window", new Vector3(-.70f, 2.34f, .02f), new Vector3(.022f, .84f, 1.20f), glass);
        AddBoxVisual(root.transform, "Right side window", new Vector3(.70f, 2.34f, .02f), new Vector3(.022f, .84f, 1.20f), glass);
        AddBoxVisual(root.transform, "Rear cab window", new Vector3(0, 2.34f, -.56f), new Vector3(1.22f, .84f, .022f), glass);
        AddBoxVisual(root.transform, "Front left cab pillar", new Vector3(-.70f, 2.34f, .78f), new Vector3(.075f, 1.04f, .075f), whiteShade);
        AddBoxVisual(root.transform, "Front right cab pillar", new Vector3(.70f, 2.34f, .78f), new Vector3(.075f, 1.04f, .075f), whiteShade);
        AddBoxVisual(root.transform, "Rear left cab pillar", new Vector3(-.70f, 2.34f, -.66f), new Vector3(.075f, 1.04f, .075f), whiteShade);
        AddBoxVisual(root.transform, "Rear right cab pillar", new Vector3(.70f, 2.34f, -.66f), new Vector3(.075f, 1.04f, .075f), whiteShade);
        AddBoxVisual(root.transform, "Cab roof", new Vector3(0, 2.91f, .06f), new Vector3(1.56f, .14f, 1.62f), white);
        AddBoxVisual(root.transform, "Roof blue panel", new Vector3(0, 2.985f, .06f), new Vector3(.48f, .012f, 1.18f), blue);

        // Controls are deliberately below and forward of the seated eye position.
        AddBoxVisual(root.transform, "Steering column", new Vector3(0, 1.78f, .25f), new Vector3(.07f, .32f, .07f), rubber);
        AddSteeringWheel(root.transform, new Vector3(0, 1.96f, .28f), rubber);
        AddBoxVisual(root.transform, "Left side step", new Vector3(-.82f, .48f, -.12f), new Vector3(.22f, .10f, .66f), metal);
        AddBoxVisual(root.transform, "Right side step", new Vector3(.82f, .48f, -.12f), new Vector3(.22f, .10f, .66f), metal);

        // Rear cargo platform. All wall colliders start exactly at the top of the floor collider.
        const float bedZ = -2.62f, bedLength = 2.78f, bedWidth = 1.68f;
        const float floorY = .63f, floorThickness = .12f, wallHeight = .78f, wallThickness = .10f;
        AddBoxVisual(root.transform, "Cargo deck", new Vector3(0, floorY, bedZ), new Vector3(bedWidth, floorThickness, bedLength), white);
        AddBoxCollider(root.transform, "Cargo deck collider", new Vector3(0, floorY, bedZ), new Vector3(bedWidth, floorThickness, bedLength));
        float sideX = bedWidth * .5f - wallThickness * .5f;
        float wallCenterY = floorY + floorThickness * .5f + wallHeight * .5f;
        AddWall(root.transform, "Left cargo side wall", new Vector3(-sideX, wallCenterY, bedZ), new Vector3(wallThickness, wallHeight, bedLength), white, blue);
        AddWall(root.transform, "Right cargo side wall", new Vector3(sideX, wallCenterY, bedZ), new Vector3(wallThickness, wallHeight, bedLength), white, blue);
        float frontWallZ = bedZ + bedLength * .5f - wallThickness * .5f;
        AddWall(root.transform, "Cargo front wall", new Vector3(0, wallCenterY, frontWallZ), new Vector3(bedWidth, wallHeight, wallThickness), white, blue);
        float rearWallZ = bedZ - bedLength * .5f + wallThickness * .5f;
        AddWall(root.transform, "Cargo rear gate", new Vector3(0, wallCenterY, rearWallZ), new Vector3(bedWidth, wallHeight, wallThickness), white, purple);

        // Distinct wheel wells/fenders are positioned outside the chassis envelope, not buried in it.
        Vector3[] wheelPositions = {
            new Vector3(-.91f, .34f, .91f), new Vector3(.91f, .34f, .91f),
            new Vector3(-.91f, .34f, -1.18f), new Vector3(.91f, .34f, -1.18f)
        };
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            AddWheel(root.transform, "Tire " + (i + 1), wheelPositions[i], rubber, metal, blue);
        }
        AddBoxVisual(root.transform, "Left front fender", new Vector3(-.89f, .72f, .91f), new Vector3(.12f, .12f, .84f), whiteShade);
        AddBoxVisual(root.transform, "Right front fender", new Vector3(.89f, .72f, .91f), new Vector3(.12f, .12f, .84f), whiteShade);
        AddBoxVisual(root.transform, "Left rear fender", new Vector3(-.89f, .72f, -1.18f), new Vector3(.12f, .12f, .84f), whiteShade);
        AddBoxVisual(root.transform, "Right rear fender", new Vector3(.89f, .72f, -1.18f), new Vector3(.12f, .12f, .84f), whiteShade);

        // FPSMovement camera is normally ~1.6 m above the player root. This mount
        // places the seated eye around 2.4 m while keeping the steering wheel below it.
        Transform seatMount = MakeChild(root.transform, "Player Seat Mount", new Vector3(0, .78f, -.40f));
        Transform exitPoint = MakeChild(root.transform, "Player Exit Point", new Vector3(-2.35f, .12f, -.45f));

        // Four independent approach zones: approach from either side, front, or rear.
        AddEntryTrigger(root.transform, "Entry Trigger Left", new Vector3(-1.12f, 1.18f, -.12f), new Vector3(1.05f, 2.25f, 2.10f));
        AddEntryTrigger(root.transform, "Entry Trigger Right", new Vector3(1.12f, 1.18f, -.12f), new Vector3(1.05f, 2.25f, 2.10f));
        AddEntryTrigger(root.transform, "Entry Trigger Front", new Vector3(0, 1.18f, 1.70f), new Vector3(1.90f, 2.25f, 1.10f));
        AddEntryTrigger(root.transform, "Entry Trigger Rear", new Vector3(0, 1.18f, -2.05f), new Vector3(1.90f, 2.25f, 1.10f));

        ElectricTugController controller = root.AddComponent<ElectricTugController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("seatMount").objectReferenceValue = seatMount;
        serialized.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        const string folder = "Assets/ElectricTug_HeavyDuty_ClearView";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "ElectricTug_HeavyDuty_ClearView");
        SaveMaterial(white, folder + "/TugWhite.mat");
        SaveMaterial(whiteShade, folder + "/TugWhiteShade.mat");
        SaveMaterial(blue, folder + "/TugBlue.mat");
        SaveMaterial(purple, folder + "/TugPurple.mat");
        SaveMaterial(rubber, folder + "/TugRubber.mat");
        SaveMaterial(seat, folder + "/TugSeat.mat");
        SaveMaterial(glass, folder + "/TugGlass.mat");
        SaveMaterial(metal, folder + "/TugMetal.mat");

        PrefabUtility.SaveAsPrefabAsset(root, folder + "/ElectricTug_HeavyDuty_ClearView.prefab");
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/ElectricTug_HeavyDuty_ClearView.prefab"));
        Debug.Log("Electric Tug Heavy Duty Edition created at " + folder + "/ElectricTug_HeavyDuty_ClearView.prefab");
    }

    private static void AddEntryTrigger(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject triggerObject = new GameObject(name);
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.localPosition = position;
        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = size;
    }

    private static Material MakeGlassMaterial(string name, Color color)
    {
        Material material = MakeMaterial(name, color);
        material.color = color;
        if (material.shader != null && material.shader.name.Contains("Universal Render Pipeline"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        return material;
    }

    private static void ConfigureBody(Rigidbody body)
    {
        body.mass = 4200f;
        body.linearDamping = 2.4f;
        body.angularDamping = 6.0f;
        body.centerOfMass = new Vector3(0, -.12f, -.10f);
        body.maxAngularVelocity = 1.0f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private static void AddWall(Transform parent, string name, Vector3 position, Vector3 size, Material face, Material top)
    {
        AddBoxVisual(parent, name, position, size, face);
        AddBoxCollider(parent, name + " collider", position, size);
        // The accent is a narrow inlaid stripe on the outward face, not a cap embedded in the wall.
        bool sideWall = size.x < size.z;
        Vector3 stripePosition = position + (sideWall ? new Vector3(Mathf.Sign(position.x) * (size.x * .5f + .009f), 0, 0) : new Vector3(0, 0, -size.z * .5f - .009f));
        Vector3 stripeSize = sideWall ? new Vector3(.012f, .045f, size.z - .12f) : new Vector3(size.x - .12f, .045f, .012f);
        AddBoxVisual(parent, name + " accent stripe", stripePosition, stripeSize, top);
    }

    private static void AddWheel(Transform parent, string name, Vector3 position, Material tire, Material hub, Material accent)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = position;
        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
        wheel.transform.localScale = new Vector3(.62f, .13f, .62f);
        wheel.GetComponent<Renderer>().sharedMaterial = tire;
        Object.DestroyImmediate(wheel.GetComponent<Collider>());

        GameObject hubObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hubObject.name = name + " hub";
        hubObject.transform.SetParent(parent, false);
        hubObject.transform.localPosition = position + new Vector3(Mathf.Sign(position.x) * .137f, 0, 0);
        hubObject.transform.localRotation = Quaternion.Euler(0, 0, 90);
        hubObject.transform.localScale = new Vector3(.25f, .012f, .25f);
        hubObject.GetComponent<Renderer>().sharedMaterial = hub;
        Object.DestroyImmediate(hubObject.GetComponent<Collider>());
    }

    private static void AddSteeringWheel(Transform parent, Vector3 position, Material material)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "Steering wheel rim";
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = position;
        wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        wheel.transform.localScale = new Vector3(.38f, .025f, .38f);
        wheel.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
    }

    private static GameObject AddBoxVisual(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static BoxCollider AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = center;
        BoxCollider collider = holder.AddComponent<BoxCollider>();
        collider.size = size;
        return collider;
    }

    private static Transform MakeChild(Transform parent, string name, Vector3 position)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        return child.transform;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color };
        return material;
    }

    private static void SaveMaterial(Material material, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null) AssetDatabase.CreateAsset(material, path);
        else Object.DestroyImmediate(material);
    }
}
#endif
