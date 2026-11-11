#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the CyberHex PACK MULE (medium vehicle).
///  - Stand-on operator platform at the rear with glass guard panels (no roof, tower sits BEHIND the operator
///    => unobstructed view; the cargo tray is see-through glass).
///  - Low-slung hex glass tray with glowing hex ribs, hex bulkhead and a hex glass LIFT-GATE RAMP (Z key).
///  - Same palette / hexagon language / glow as the large CyberHex tug.
/// Everything is saved to Assets/HexPackMule_Medium so the prefab keeps working.
/// </summary>
public static class HexPackMuleBuilder
{
    private const string AssetFolderParent = "Assets";
    private const string AssetFolderName = "HexPackMule_Medium";
    private const string AssetFolder = AssetFolderParent + "/" + AssetFolderName;

    private const float WR = 0.22f;        // wheel radius
    private const float WHH = 0.09f;       // wheel half width
    private const float FR_BOT = 0.48f;    // frame bottom
    private const float FR_TOP = 0.60f;    // frame top == platform floor == tray floor outer
    private const float FR_HW = 0.62f;     // frame half width
    private const float Z_R = -1.50f;      // rear end of frame
    private const float Z_F = 1.60f;       // front end of frame / tray

    private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

    [MenuItem("GameObject/Logistics Game/Create CyberHex Pack Mule (Medium)", false, 18)]
    private static void CreateMule()
    {
        meshCache.Clear();
        EnsureFolder();

        Material whiteBody = Palette("WhiteBody", MakeMaterial("Mule White Body", new Color(0.94f, 0.96f, 0.99f), 0.2f, 0.7f));
        Material blueAccent = Palette("BlueAccent", MakeMaterial("Mule Blue Accent", new Color(0.06f, 0.35f, 0.90f), 0.4f, 0.8f));
        Material greenGlow = Palette("GreenGlow", MakeEmissiveMaterial("Mule Green Glow", new Color(0.0f, 0.98f, 0.45f), 3.0f));
        Material darkChassis = Palette("DarkChassis", MakeMaterial("Mule Carbon Frame", new Color(0.10f, 0.12f, 0.16f), 0.5f, 0.5f));
        Material rubberTire = Palette("RubberTire", MakeMaterial("Mule Rubber Tire", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f));
        Material sciFiGlass = Palette("SciFiGlass", MakeGlassMaterial("Mule SeeThrough Glass", new Color(0.22f, 0.62f, 0.88f, 0.22f)));
        Material chromeHub = Palette("ChromeHub", MakeMaterial("Mule Chrome Hub", new Color(0.88f, 0.90f, 0.94f), 0.9f, 0.9f));
        Material lampGlow = Palette("LampGlow", MakeEmissiveMaterial("Mule Headlight Glow", new Color(0.85f, 0.95f, 1.0f), 3.5f));
        PhysicsMaterial slick = MakePhysicsMaterial();

        GameObject root = new GameObject("CyberHex Pack Mule (Medium)");
        root.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1800f;
        rb.linearDamping = 2.2f;
        rb.angularDamping = 5.0f;
        rb.centerOfMass = new Vector3(0f, 0.35f, -0.2f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Transform t = root.transform;

        Transform chassisG = MakeChild(t, "Chassis", Vector3.zero);
        Transform platformG = MakeChild(t, "Operator Platform", Vector3.zero);
        Transform trayG = MakeChild(t, "Cargo Tray", Vector3.zero);
        Transform gateG = MakeChild(t, "Ramp Gate", Vector3.zero);
        Transform wheelsG = MakeChild(t, "Wheels", Vector3.zero);
        Transform trimG = MakeChild(t, "Trim", Vector3.zero);
        Transform lightsG = MakeChild(t, "Lights", Vector3.zero);
        Transform collidersG = MakeChild(t, "Colliders", Vector3.zero);
        Transform triggersG = MakeChild(t, "Entry Triggers", Vector3.zero);

        // ---------------- chassis ----------------
        Box(chassisG, "Frame", darkChassis, -FR_HW, FR_HW, FR_BOT, FR_TOP, Z_R, Z_F);
        Box(chassisG, "Battery Skid", darkChassis, -0.40f, 0.40f, 0.44f, FR_BOT, -1.20f, 0.85f);
        Box(chassisG, "Caster Plate", darkChassis, -0.20f, 0.20f, 0.44f, FR_BOT, 1.03f, 1.27f);
        float[,] stripZ = { { -1.45f, -1.35f }, { -0.55f, 1.45f } };
        for (int s = -1; s <= 1; s += 2)
        {
            float x0 = s > 0 ? FR_HW : -FR_HW - 0.01f, x1 = s > 0 ? FR_HW + 0.01f : -FR_HW;
            for (int i = 0; i < 2; i++)
                Box(trimG, "Frame Glow Strip " + (s > 0 ? "R" : "L") + i, greenGlow, x0, x1, 0.50f, 0.54f, stripZ[i, 0], stripZ[i, 1]);
        }
        MeshObj(chassisG, "Front Hex Bumper", Cached("Bumper", () => PrismZ(HexPoly(0f, 0.45f, 0.40f, 0.56f, 0.05f), Z_F, 1.68f, "Bumper")), blueAccent, Vector3.zero, Quaternion.identity);
        Box(chassisG, "Front Bumper Light Bar", lampGlow, -0.30f, 0.30f, 0.43f, 0.46f, 1.68f, 1.69f);
        Mesh lampMesh = Cached("Lamp", () => PrismZ(RegHex(0, 0, 0.06f), 0f, 0.03f, "Lamp"));
        MeshObj(chassisG, "Hex Headlight L", lampMesh, lampGlow, new Vector3(-0.56f, 0.54f, Z_F), Quaternion.identity);
        MeshObj(chassisG, "Hex Headlight R", lampMesh, lampGlow, new Vector3(0.56f, 0.54f, Z_F), Quaternion.identity);

        // ---------------- operator platform (glass guards, tower BEHIND the operator) ----------------
        for (int s = -1; s <= 1; s += 2)
        {
            string sd = s > 0 ? "R" : "L";
            float x0 = s > 0 ? 0.58f : -0.62f, x1 = s > 0 ? 0.62f : -0.58f;
            Box(platformG, "Guard Glass " + sd, sciFiGlass, x0, x1, FR_TOP, 0.95f, -1.46f, -0.50f);
            Box(platformG, "Guard Rail " + sd, greenGlow, x0, x1, 0.95f, 0.97f, -1.46f, -0.50f);
        }
        Box(platformG, "Guard Glass Rear", sciFiGlass, -0.62f, 0.62f, FR_TOP, 0.95f, -1.50f, -1.46f);
        Box(platformG, "Guard Rail Rear", greenGlow, -0.62f, 0.62f, 0.95f, 0.97f, -1.50f, -1.46f);

        Box(platformG, "Control Tower", whiteBody, -0.56f, -0.34f, FR_TOP, 1.50f, -1.46f, -1.30f);
        Vector3 dispPos = new Vector3(-0.45f, 1.20f, -1.30f);
        MeshObj(platformG, "Tower Display Plate", Cached("DispPlate", () => PrismZ(RegHex(0, 0, 0.15f), 0f, 0.015f, "DispPlate")), darkChassis, dispPos, Quaternion.identity);
        Vector2[] dgo = RegHex(0, 0, 0.12f);
        Vector2[] dgi = OffsetPoly(dgo, -0.025f);
        MeshObj(platformG, "Tower Display Ring", Cached("DispRing", () => RingZ(dgo, dgi, 0.015f, 0.03f, "DispRing")), greenGlow, dispPos, Quaternion.identity);
        MeshObj(platformG, "Tower Display Core", Cached("DispCore", () => PrismZ(RegHex(0, 0, 0.04f), 0.015f, 0.025f, "DispCore")), greenGlow, dispPos, Quaternion.identity);

        // compact hex steering yoke on a low post in front of the operator (below the sight line)
        Vector3 yokePos = new Vector3(0f, 1.46f, -0.62f);
        Vector3 yokeN = new Vector3(0f, 0.72f, -0.69f).normalized;
        Transform yokePivot = MakeChild(platformG, "Steering Yoke Pivot", yokePos);
        yokePivot.localRotation = Quaternion.LookRotation(yokeN, Vector3.up);
        Transform yokeSpin = MakeChild(yokePivot, "Steering Yoke Spin", Vector3.zero);
        Vector2[] yo = RegHex(0, 0, 0.09f);
        Vector2[] yi = OffsetPoly(yo, -0.022f);
        MeshObj(yokeSpin, "Yoke Ring", Cached("YokeRing", () => RingZ(yo, yi, -0.014f, 0.014f, "YokeRing")), darkChassis, Vector3.zero, Quaternion.identity);
        MeshObj(yokeSpin, "Yoke Hub", Cached("YokeHub", () => PrismZ(RegHex(0, 0, 0.032f), -0.022f, 0.022f, "YokeHub")), blueAccent, Vector3.zero, Quaternion.identity);
        float rIn = yi[1].x, aIn = -yi[2].y;
        float spC = (0.032f + rIn - 0.004f) * 0.5f;
        LocalBox(yokeSpin, "Yoke Spoke R", darkChassis, new Vector3(spC, 0f, 0f), new Vector3(rIn - 0.036f, 0.014f, 0.011f));
        LocalBox(yokeSpin, "Yoke Spoke L", darkChassis, new Vector3(-spC, 0f, 0f), new Vector3(rIn - 0.036f, 0.014f, 0.011f));
        LocalBox(yokeSpin, "Yoke Spoke Down", darkChassis, new Vector3(0f, -(0.0277f + aIn) * 0.5f, 0f), new Vector3(0.014f, aIn - 0.0277f, 0.011f));
        float stalkT = (yokePos.y - 1.363f) / yokeN.y;
        Vector3 stalkEnd = yokePos - yokeN * stalkT;
        Beam(platformG, "Steering Stalk", darkChassis, yokePos - yokeN * 0.022f, stalkEnd, 0.03f, 0.03f);
        Box(platformG, "Steering Post", darkChassis, -0.0225f, 0.0225f, FR_TOP, 1.35f, stalkEnd.z - 0.0225f, stalkEnd.z + 0.0225f);

        // ---------------- hex glass tray ----------------
        const float HF = 0.44f, HM = 0.64f, HH = 0.33f;
        const float glassIn = -0.04f, ribOut = 0.035f, ribDepth = 0.09f;
        const int ribCount = 4;
        const float trayRearZ = -0.40f, trayFrontZ = Z_F;
        Vector2[] hex = HexPoly(0f, FR_TOP + ribOut + HH, HF, HM, HH);
        Vector2[] glassInner = OffsetPoly(hex, glassIn);
        Vector2[] ribOuter = OffsetPoly(hex, ribOut);
        float segLen = ((trayFrontZ - trayRearZ) - ribCount * ribDepth) / (ribCount - 1);
        float[] ribZ = new float[ribCount];
        for (int i = 0; i < ribCount; i++) ribZ[i] = trayRearZ + ribDepth * 0.5f + i * (segLen + ribDepth);

        for (int j = 0; j < ribCount - 1; j++)
        {
            float zc = trayRearZ + ribDepth + segLen * 0.5f + j * (segLen + ribDepth);
            for (int k = 0; k < 6; k++)
            {
                int kk = k;
                Mesh m = Cached("TrayEdge" + k, () => EdgePrismZ(hex, glassInner, kk, -segLen * 0.5f, segLen * 0.5f, "TrayEdge" + kk));
                MeshObj(trayG, "Hex " + (k == 2 ? "Deck Floor" : "Glass") + " e" + k + " bay" + j, m, k == 2 ? darkChassis : sciFiGlass, new Vector3(0f, 0f, zc), Quaternion.identity);
            }
        }
        Mesh ribA = Cached("RibA", () => RingZ(ribOuter, glassInner, -ribDepth * 0.5f, -0.015f, "RibA"));
        Mesh ribG = Cached("RibG", () => RingZ(ribOuter, glassInner, -0.015f, 0.015f, "RibG"));
        Mesh ribB = Cached("RibB", () => RingZ(ribOuter, glassInner, 0.015f, ribDepth * 0.5f, "RibB"));
        for (int i = 0; i < ribCount; i++)
        {
            Vector3 rp = new Vector3(0f, 0f, ribZ[i]);
            MeshObj(trayG, "Hex Rib " + i + " A", ribA, whiteBody, rp, Quaternion.identity);
            MeshObj(trayG, "Hex Rib " + i + " Glow", ribG, greenGlow, rp, Quaternion.identity);
            MeshObj(trayG, "Hex Rib " + i + " B", ribB, whiteBody, rp, Quaternion.identity);
        }
        MeshObj(trayG, "Hex Glass Bulkhead", Cached("Bulkhead", () => PrismZ(glassInner, -0.0125f, 0.0125f, "Bulkhead")), sciFiGlass, new Vector3(0f, 0f, ribZ[0]), Quaternion.identity);
        Box(trayG, "Roof LED Strip", greenGlow, -0.10f, 0.10f, glassInner[0].y - 0.02f, glassInner[0].y, trayRearZ + 0.14f, trayFrontZ - 0.14f);

        // ---------------- hex glass lift-gate / loading ramp (hinged at the bottom, Z key) ----------------
        float topY = float.MinValue, botY = float.MaxValue;
        for (int i = 0; i < ribOuter.Length; i++) { topY = Mathf.Max(topY, ribOuter[i].y); botY = Mathf.Min(botY, ribOuter[i].y); }
        Vector2[] gateInnerWorld = OffsetPoly(hex, -0.10f);
        Transform rampPivot = MakeChild(gateG, "Ramp Gate Pivot", new Vector3(0f, botY, trayFrontZ));
        Vector2[] gO = ShiftY(ribOuter, -botY);
        Vector2[] gI = ShiftY(gateInnerWorld, -botY);
        Vector2[] gGl = ShiftY(OffsetPoly(hex, -0.13f), -botY);
        MeshObj(rampPivot, "Hex Gate Frame", Cached("GateFrame", () => RingZ(gO, gI, 0f, 0.09f, "GateFrame")), whiteBody, Vector3.zero, Quaternion.identity);
        MeshObj(rampPivot, "Hex Gate Glass", Cached("GateGlass", () => PrismZ(gI, 0.03f, 0.05f, "GateGlass")), sciFiGlass, Vector3.zero, Quaternion.identity);
        MeshObj(rampPivot, "Hex Gate Glow Outline", Cached("GateGlow", () => RingZ(gI, gGl, 0.05f, 0.065f, "GateGlow")), greenGlow, Vector3.zero, Quaternion.identity);
        float latchY = (topY + gateInnerWorld[0].y) * 0.5f - botY;
        MeshObj(rampPivot, "Hex Gate Latch", Cached("GateLatch", () => PrismZ(RegHex(0, 0, 0.055f), 0.09f, 0.115f, "GateLatch")), blueAccent, new Vector3(0f, latchY, 0f), Quaternion.identity);
        MeshObj(rampPivot, "Hex Gate Latch Core", Cached("GateLatchCore", () => PrismZ(RegHex(0, 0, 0.028f), 0.115f, 0.125f, "GateLatchCore")), greenGlow, new Vector3(0f, latchY, 0f), Quaternion.identity);
        {
            GameObject gc = new GameObject("Ramp Collider");
            gc.transform.SetParent(rampPivot, false);
            MeshCollider mc = gc.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("GateCollider", () => PrismZ(gO, 0f, 0.09f, "GateCollider"));
        }

        // ---------------- wheels ----------------
        List<Transform> spinPivots = new List<Transform>();
        List<Transform> steerPivots = new List<Transform>();
        Vector2[] archOuter = RegHex(0, 0, 0.34f);
        Vector2[] archInner = OffsetPoly(archOuter, -0.05f);
        int[] archEdges = { 4, 5, 0 };

        for (int s = -1; s <= 1; s += 2)
        {
            string sd = s > 0 ? "R" : "L";
            // rear drive wheel
            Transform spinR = MakeChild(wheelsG, "Wheel Spin Rear " + sd, new Vector3(s * 0.76f, WR, -0.95f));
            spinPivots.Add(spinR);
            BuildWheel(spinR, "Rear " + sd, s, rubberTire, chromeHub, blueAccent, greenGlow);
            Vector3 archPos = new Vector3(s * FR_HW, WR, -0.95f);
            Quaternion outward = Quaternion.Euler(0f, s * 90f, 0f);
            foreach (int ek in archEdges)
            {
                int e = ek;
                MeshObj(trimG, "Arch Body Rear " + sd + " e" + e, Cached("ArchB" + e, () => EdgePrismZ(archOuter, archInner, e, 0f, 0.26f, "ArchB" + e)), darkChassis, archPos, outward);
                MeshObj(trimG, "Arch Trim Rear " + sd + " e" + e, Cached("ArchT" + e, () => EdgePrismZ(archOuter, archInner, e, 0.26f, 0.30f, "ArchT" + e)), blueAccent, archPos, outward);
            }
        }
        Transform casterSteer = MakeChild(wheelsG, "Caster Swivel", new Vector3(0f, WR, 1.15f));
        steerPivots.Add(casterSteer);
        for (int s = -1; s <= 1; s += 2)
        {
            string sd = s > 0 ? "R" : "L";
            Transform spinF = MakeChild(casterSteer, "Wheel Spin Front " + sd, new Vector3(s * 0.095f, 0f, 0f));
            spinPivots.Add(spinF);
            BuildWheel(spinF, "Front " + sd, s, rubberTire, chromeHub, blueAccent, greenGlow);
        }

        // ---------------- lights ----------------
        List<Light> headlights = new List<Light>();
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject lg = new GameObject("Headlight Spot " + (s > 0 ? "R" : "L"));
            lg.transform.SetParent(lightsG, false);
            lg.transform.localPosition = new Vector3(s * 0.56f, 0.54f, Z_F + 0.06f);
            lg.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Spot; l.color = new Color(0.86f, 0.95f, 1f); l.range = 24f; l.spotAngle = 70f;
            l.intensity = 2.2f; l.shadows = LightShadows.None; l.enabled = false;
            headlights.Add(l);
        }
        {
            GameObject cl = new GameObject("Tray Glow");
            cl.transform.SetParent(lightsG, false);
            cl.transform.localPosition = new Vector3(0f, 1.0f, 0.6f);
            Light l = cl.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.2f, 1f, 0.6f); l.range = 3.5f; l.intensity = 0.7f; l.shadows = LightShadows.None;
        }

        // ---------------- colliders ----------------
        AddSphereCollider(collidersG, "Wheel Collider RL", new Vector3(-0.76f, WR, -0.95f), WR, slick);
        AddSphereCollider(collidersG, "Wheel Collider RR", new Vector3(0.76f, WR, -0.95f), WR, slick);
        AddSphereCollider(collidersG, "Wheel Collider FL", new Vector3(-0.095f, WR, 1.15f), WR, slick);
        AddSphereCollider(collidersG, "Wheel Collider FR", new Vector3(0.095f, WR, 1.15f), WR, slick);
        AddBoxCollider(collidersG, "Frame Collider", new Vector3(0f, (FR_BOT + FR_TOP) * 0.5f, (Z_R + Z_F) * 0.5f), new Vector3(FR_HW * 2f, FR_TOP - FR_BOT, Z_F - Z_R));
        AddBoxCollider(collidersG, "Tower Collider", new Vector3(-0.45f, (FR_TOP + 1.50f) * 0.5f, -1.38f), new Vector3(0.22f, 1.50f - FR_TOP, 0.16f));
        Vector2[] colOuter = OffsetPoly(hex, 0.04f);
        Vector2[] colInner = OffsetPoly(hex, -0.04f);
        float trayLen = trayFrontZ - trayRearZ, trayMid = (trayFrontZ + trayRearZ) * 0.5f;
        for (int k = 0; k < 6; k++)
        {
            int kk = k;
            GameObject wall = new GameObject("Hex Wall Collider e" + k);
            wall.transform.SetParent(collidersG, false);
            wall.transform.localPosition = new Vector3(0f, 0f, trayMid);
            MeshCollider mc = wall.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("TrayCollider" + k, () => EdgePrismZ(colOuter, colInner, kk, -trayLen * 0.5f, trayLen * 0.5f, "TrayCollider" + kk));
        }
        {
            GameObject bh = new GameObject("Hex Bulkhead Collider");
            bh.transform.SetParent(collidersG, false);
            bh.transform.localPosition = new Vector3(0f, 0f, ribZ[0]);
            MeshCollider mc = bh.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("BulkheadCollider", () => PrismZ(glassInner, -ribDepth * 0.5f, ribDepth * 0.5f, "BulkheadCollider"));
        }

        // ---------------- operator mount + triggers ----------------
        // Standing operator: root sits ~0.9 above the platform so eyes are ~1.5 above it (move this object to taste).
        Transform seatMount = MakeChild(t, "Player Stand Mount", new Vector3(0f, FR_TOP + 0.90f, -0.95f));
        Transform exitPoint = MakeChild(t, "Player Exit Point", new Vector3(-1.35f, 0.95f, -0.95f));
        AddEntryTrigger(triggersG, "Trigger Left", new Vector3(-1.35f, 0.9f, -0.3f), new Vector3(1.2f, 1.8f, 2.6f));
        AddEntryTrigger(triggersG, "Trigger Right", new Vector3(1.35f, 0.9f, -0.3f), new Vector3(1.2f, 1.8f, 2.6f));
        AddEntryTrigger(triggersG, "Trigger Rear", new Vector3(0f, 0.9f, -2.2f), new Vector3(1.6f, 1.8f, 1.2f));
        AddEntryTrigger(triggersG, "Trigger Front (ramp)", new Vector3(0f, 0.9f, 2.6f), new Vector3(1.6f, 1.8f, 1.8f));

        // ---------------- controller + prefab ----------------
        HexPackMuleController controller = root.AddComponent<HexPackMuleController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("seatMount").objectReferenceValue = seatMount;
        so.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        so.FindProperty("rampPivot").objectReferenceValue = rampPivot;
        so.FindProperty("steeringYoke").objectReferenceValue = yokeSpin;
        SetObjectArray(so, "wheelSpinPivots", spinPivots);
        SetObjectArray(so, "frontSteerPivots", steerPivots);
        SetObjectArray(so, "headlights", headlights);
        so.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = AssetFolder + "/HexPackMule_Medium.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log("CyberHex Pack Mule (Medium) created at: " + prefabPath);
    }

    /// <summary>Tire + three stacked hex hub plates on the outer face of the wheel (axis = local X).</summary>
    private static void BuildWheel(Transform spin, string label, int side, Material tire, Material chrome, Material blue, Material glow)
    {
        GameObject tr = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tr.name = "Tire " + label;
        tr.transform.SetParent(spin, false);
        tr.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tr.transform.localScale = new Vector3(WR * 2f, WHH, WR * 2f);
        tr.GetComponent<Renderer>().sharedMaterial = tire;
        Object.DestroyImmediate(tr.GetComponent<Collider>());
        Quaternion outward = Quaternion.Euler(0f, side * 90f, 0f);
        MeshObj(spin, "Hub Chrome " + label, Cached("HubC", () => PrismZ(RegHex(0, 0, 0.15f), WHH, WHH + 0.015f, "HubC")), chrome, Vector3.zero, outward);
        MeshObj(spin, "Hub Blue " + label, Cached("HubB", () => PrismZ(RegHex(0, 0, 0.08f), WHH + 0.015f, WHH + 0.025f, "HubB")), blue, Vector3.zero, outward);
        MeshObj(spin, "Hub Glow " + label, Cached("HubG", () => PrismZ(RegHex(0, 0, 0.035f), WHH + 0.025f, WHH + 0.035f, "HubG")), glow, Vector3.zero, outward);
    }

    // =====================================================================
    // Shared helpers / procedural mesh toolkit
    // =====================================================================
    /// <summary>Axis-aligned box from min/max coordinates (root space), visual only.</summary>
    private static GameObject Box(Transform parent, string name, Material mat, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        cube.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    /// <summary>Box with a position/size in the parent's local space.</summary>
    private static GameObject LocalBox(Transform parent, string name, Material mat, Vector3 localPos, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    /// <summary>Rectangular beam between two points (length along the beam, w/h cross section).</summary>
    private static GameObject Beam(Transform parent, string name, Material mat, Vector3 a, Vector3 b, float w, float h)
    {
        Vector3 d = b - a;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = (a + b) * 0.5f;
        cube.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        cube.transform.localScale = new Vector3(w, h, d.magnitude);
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject MeshObj(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Quaternion localRot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject PrismX(Transform parent, string name, Material mat, string meshKey, Vector2[] polyZY, float x0, float x1)
    {
        Mesh m = Cached(meshKey, () => PrismXZY(polyZY, x0, x1, meshKey));
        return MeshObj(parent, name, m, mat, Vector3.zero, Quaternion.identity);
    }

    private static void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;
        obj.AddComponent<BoxCollider>().size = size;
    }

    private static void AddSphereCollider(Transform parent, string name, Vector3 center, float radius, PhysicsMaterial mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;
        SphereCollider sc = obj.AddComponent<SphereCollider>();
        sc.radius = radius;
        sc.sharedMaterial = mat;
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

    private static void SetObjectArray<T>(SerializedObject so, string propName, List<T> items) where T : Object
    {
        SerializedProperty prop = so.FindProperty(propName);
        prop.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    // =====================================================================
    // Procedural mesh toolkit (all faces are flat-shaded, closed, outward-wound)
    // =====================================================================
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<int> tris = new List<int>();

        /// <summary>Adds a planar quad, flipping its winding if needed so its normal points along 'hint'.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hint)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hint) < 0f) { Vector3 tmp = b; b = d; d = tmp; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 hint)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hint) < 0f) { Vector3 tmp = b; b = c; c = tmp; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public Mesh Build(string name)
        {
            Mesh m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    private static Vector3 V3(Vector2 p, float z) { return new Vector3(p.x, p.y, z); }

    /// <summary>Hexagon with flat top/bottom: half flat width hf, half max width hm (at mid height), half height hh.</summary>
    private static Vector2[] HexPoly(float cx, float cy, float hf, float hm, float hh)
    {
        return new[]
        {
            new Vector2(cx + hf, cy + hh), new Vector2(cx + hm, cy), new Vector2(cx + hf, cy - hh),
            new Vector2(cx - hf, cy - hh), new Vector2(cx - hm, cy), new Vector2(cx - hf, cy + hh)
        };
    }

    /// <summary>Regular hexagon (flat top/bottom) with circumradius r.</summary>
    private static Vector2[] RegHex(float cx, float cy, float r)
    {
        return HexPoly(cx, cy, r * 0.5f, r, r * 0.8660254f);
    }

    private static Vector2 EdgeNormal(Vector2[] poly, int k)
    {
        int n = poly.Length;
        Vector2 cen = Vector2.zero;
        for (int i = 0; i < n; i++) cen += poly[i];
        cen /= n;
        Vector2 a = poly[k], b = poly[(k + 1) % n];
        Vector2 dir = (b - a).normalized;
        Vector2 nm = new Vector2(dir.y, -dir.x);
        if (Vector2.Dot(nm, (a + b) * 0.5f - cen) < 0f) nm = -nm;
        return nm;
    }

    /// <summary>Offsets a convex polygon (positive = outward) with perfectly mitered corners.</summary>
    private static Vector2[] OffsetPoly(Vector2[] poly, float d)
    {
        int n = poly.Length;
        Vector2[] lp = new Vector2[n];
        Vector2[] ld = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % n];
            ld[i] = (b - a).normalized;
            lp[i] = a + EdgeNormal(poly, i) * d;
        }
        Vector2[] res = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            int h = (i + n - 1) % n;
            Vector2 p1 = lp[h], d1 = ld[h], p2 = lp[i], d2 = ld[i];
            Vector2 r = p2 - p1;
            float det = -d1.x * d2.y + d2.x * d1.y;
            float t1 = (-r.x * d2.y + d2.x * r.y) / det;
            res[i] = p1 + d1 * t1;
        }
        return res;
    }

    private static Vector2[] ShiftY(Vector2[] poly, float dy)
    {
        Vector2[] r = new Vector2[poly.Length];
        for (int i = 0; i < poly.Length; i++) r[i] = new Vector2(poly[i].x, poly[i].y + dy);
        return r;
    }

    /// <summary>Filled convex polygon extruded along Z.</summary>
    private static Mesh PrismZ(Vector2[] poly, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = poly.Length;
        Vector2 c2 = Vector2.zero;
        for (int i = 0; i < n; i++) c2 += poly[i];
        c2 /= n;
        Vector3 cen = new Vector3(c2.x, c2.y, (z0 + z1) * 0.5f);
        for (int k = 0; k < n; k++)
        {
            Vector2 a = poly[k], b = poly[(k + 1) % n];
            Vector3 q0 = V3(a, z0), q1 = V3(b, z0), q2 = V3(b, z1), q3 = V3(a, z1);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(V3(poly[0], z1), V3(poly[k], z1), V3(poly[k + 1], z1), Vector3.forward);
            mb.Tri(V3(poly[0], z0), V3(poly[k], z0), V3(poly[k + 1], z0), Vector3.back);
        }
        return mb.Build(name);
    }

    /// <summary>Hollow polygon ring (outer/inner polygons with matching vertex order) extruded along Z.</summary>
    private static Mesh RingZ(Vector2[] outer, Vector2[] inner, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = outer.Length;
        for (int k = 0; k < n; k++)
        {
            int k2 = (k + 1) % n;
            Vector2 nm = EdgeNormal(outer, k);
            Vector3 h = new Vector3(nm.x, nm.y, 0f);
            mb.Quad(V3(outer[k], z0), V3(outer[k2], z0), V3(outer[k2], z1), V3(outer[k], z1), h);
            mb.Quad(V3(inner[k], z0), V3(inner[k2], z0), V3(inner[k2], z1), V3(inner[k], z1), -h);
            mb.Quad(V3(outer[k], z1), V3(outer[k2], z1), V3(inner[k2], z1), V3(inner[k], z1), Vector3.forward);
            mb.Quad(V3(outer[k], z0), V3(outer[k2], z0), V3(inner[k2], z0), V3(inner[k], z0), Vector3.back);
        }
        return mb.Build(name);
    }

    /// <summary>One side (edge k) of a mitered ring: a closed convex prism. Neighbouring edges share their miter planes exactly.</summary>
    private static Mesh EdgePrismZ(Vector2[] outer, Vector2[] inner, int k, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int k2 = (k + 1) % outer.Length;
        Vector3[] p =
        {
            V3(outer[k], z0), V3(outer[k2], z0), V3(inner[k2], z0), V3(inner[k], z0),
            V3(outer[k], z1), V3(outer[k2], z1), V3(inner[k2], z1), V3(inner[k], z1)
        };
        Vector3 cen = Vector3.zero;
        for (int i = 0; i < 8; i++) cen += p[i];
        cen /= 8f;
        int[][] faces =
        {
            new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 },
            new[] { 3, 2, 6, 7 }, new[] { 0, 3, 7, 4 }, new[] { 1, 2, 6, 5 }
        };
        foreach (int[] f in faces)
        {
            Vector3 a = p[f[0]], b = p[f[1]], c = p[f[2]], d = p[f[3]];
            mb.Quad(a, b, c, d, (a + b + c + d) * 0.25f - cen);
        }
        return mb.Build(name);
    }

    /// <summary>Solid loft between two convex polygons with the same vertex count (use parallel edges to keep faces planar).</summary>
    private static Mesh Loft(Vector2[] a, float za, Vector2[] b, float zb, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = a.Length;
        Vector3 cen = Vector3.zero;
        for (int i = 0; i < n; i++) cen += V3(a[i], za) + V3(b[i], zb);
        cen /= (2f * n);
        for (int k = 0; k < n; k++)
        {
            int k2 = (k + 1) % n;
            Vector3 q0 = V3(a[k], za), q1 = V3(a[k2], za), q2 = V3(b[k2], zb), q3 = V3(b[k], zb);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(V3(a[0], za), V3(a[k], za), V3(a[k + 1], za), Vector3.back);
            mb.Tri(V3(b[0], zb), V3(b[k], zb), V3(b[k + 1], zb), Vector3.forward);
        }
        return mb.Build(name);
    }

    /// <summary>Convex polygon given in (z, y) extruded along X between x0 and x1 (vertices in parent space).</summary>
    private static Mesh PrismXZY(Vector2[] polyZY, float x0, float x1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = polyZY.Length;
        Vector2 c2 = Vector2.zero;
        for (int i = 0; i < n; i++) c2 += polyZY[i];
        c2 /= n;
        Vector3 cen = new Vector3((x0 + x1) * 0.5f, c2.y, c2.x);
        for (int k = 0; k < n; k++)
        {
            Vector2 a = polyZY[k], b = polyZY[(k + 1) % n];
            Vector3 q0 = new Vector3(x0, a.y, a.x), q1 = new Vector3(x0, b.y, b.x);
            Vector3 q2 = new Vector3(x1, b.y, b.x), q3 = new Vector3(x1, a.y, a.x);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(new Vector3(x1, polyZY[0].y, polyZY[0].x), new Vector3(x1, polyZY[k].y, polyZY[k].x), new Vector3(x1, polyZY[k + 1].y, polyZY[k + 1].x), Vector3.right);
            mb.Tri(new Vector3(x0, polyZY[0].y, polyZY[0].x), new Vector3(x0, polyZY[k].y, polyZY[k].x), new Vector3(x0, polyZY[k + 1].y, polyZY[k + 1].x), Vector3.left);
        }
        return mb.Build(name);
    }

    // =====================================================================
    // Asset helpers
    // =====================================================================
    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder(AssetFolderParent, AssetFolderName);
    }

    /// <summary>Builds a mesh once per run and stores it as an asset (re-using/overwriting an existing asset of that name).</summary>
    private static Mesh Cached(string key, System.Func<Mesh> build)
    {
        if (meshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

        Mesh fresh = build();
        string path = AssetFolder + "/Mesh_" + key + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Mesh result;
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            result = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(fresh, path);
            result = fresh;
        }
        meshCache[key] = result;
        return result;
    }

    /// <summary>Saves a material as an asset (or refreshes the existing one) and returns the persistent asset.</summary>
    private static Material Palette(string fileName, Material fresh)
    {
        string path = AssetFolder + "/" + fileName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }
        AssetDatabase.CreateAsset(fresh, path);
        return fresh;
    }

    private static PhysicsMaterial MakePhysicsMaterial()
    {
        string path = AssetFolder + "/TugSlick.asset";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("Tug Slick");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0.05f;
        pm.staticFriction = 0.30f;
        pm.bounciness = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum;
        pm.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(pm);
        return pm;
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
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    private static Material MakeGlassMaterial(string name, Color col)
    {
        Material mat = MakeMaterial(name, col, 0.0f, 0.92f);
        if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
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
}
#endif
