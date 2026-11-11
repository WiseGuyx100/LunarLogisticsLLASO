#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the CyberHex GORILLA CART (small, hand-pulled vehicle).
///  - Four big wheels with hex hubs, see-through glass half-hex dump tub with glowing hex ribs,
///    drawbar with a glowing hex grip loop (the "handle" you hold).
///  - F grabs / releases the handle; the cart trails behind you while you walk (your view stays totally clear).
///  - Z tilts the tub and swings the glass gate open so everything slides out (dump cart).
/// Everything is saved to Assets/HexGorillaCart_Small so the prefab keeps working.
/// </summary>
public static class HexGorillaCartBuilder
{
    private const string AssetFolderParent = "Assets";
    private const string AssetFolderName = "HexGorillaCart_Small";
    private const string AssetFolder = AssetFolderParent + "/" + AssetFolderName;

    private const float WR = 0.20f;
    private const float WHH = 0.07f;
    private const float FR_BOT = 0.40f;
    private const float FR_TOP = 0.46f;

    private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

    [MenuItem("GameObject/Logistics Game/Create CyberHex Gorilla Cart (Small)", false, 19)]
    private static void CreateCart()
    {
        meshCache.Clear();
        EnsureFolder();

        Material whiteBody = Palette("WhiteBody", MakeMaterial("Cart White Body", new Color(0.94f, 0.96f, 0.99f), 0.2f, 0.7f));
        Material blueAccent = Palette("BlueAccent", MakeMaterial("Cart Blue Accent", new Color(0.06f, 0.35f, 0.90f), 0.4f, 0.8f));
        Material greenGlow = Palette("GreenGlow", MakeEmissiveMaterial("Cart Green Glow", new Color(0.0f, 0.98f, 0.45f), 3.0f));
        Material darkChassis = Palette("DarkChassis", MakeMaterial("Cart Carbon Frame", new Color(0.10f, 0.12f, 0.16f), 0.5f, 0.5f));
        Material rubberTire = Palette("RubberTire", MakeMaterial("Cart Rubber Tire", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f));
        Material sciFiGlass = Palette("SciFiGlass", MakeGlassMaterial("Cart SeeThrough Glass", new Color(0.22f, 0.62f, 0.88f, 0.22f)));
        Material chromeHub = Palette("ChromeHub", MakeMaterial("Cart Chrome Hub", new Color(0.88f, 0.90f, 0.94f), 0.9f, 0.9f));
        PhysicsMaterial slick = MakePhysicsMaterial();

        GameObject root = new GameObject("CyberHex Gorilla Cart (Small)");
        root.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 90f;
        rb.linearDamping = 2.5f;
        rb.angularDamping = 6.0f;
        rb.centerOfMass = new Vector3(0f, 0.25f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Transform t = root.transform;

        Transform chassisG = MakeChild(t, "Undercarriage", Vector3.zero);
        Transform wheelsG = MakeChild(t, "Wheels", Vector3.zero);
        Transform handleG = MakeChild(t, "Handle", Vector3.zero);
        Transform lightsG = MakeChild(t, "Lights", Vector3.zero);
        Transform collidersG = MakeChild(t, "Colliders", Vector3.zero);
        Transform triggersG = MakeChild(t, "Grab Trigger", Vector3.zero);

        // ---------------- undercarriage ----------------
        Box(chassisG, "Frame", darkChassis, -0.30f, 0.30f, FR_BOT, FR_TOP, -0.62f, 0.62f);
        Box(chassisG, "Hitch Block", darkChassis, -0.05f, 0.05f, FR_BOT, FR_TOP, 0.62f, 0.72f);
        float[] axleZ = { -0.45f, 0.38f };
        foreach (float z in axleZ)
        {
            Box(chassisG, "Axle " + z, darkChassis, -0.45f, 0.45f, 0.17f, 0.23f, z - 0.03f, z + 0.03f);
            Box(chassisG, "Axle Strut " + z, darkChassis, -0.03f, 0.03f, 0.23f, FR_BOT, z - 0.03f, z + 0.03f);
        }

        // ---------------- dump tub (everything inside rotates around the front-bottom hinge) ----------------
        const float HF = 0.30f, HM = 0.46f, HH = 0.17f;
        const float glassIn = -0.04f, ribOut = 0.03f, ribDepth = 0.08f;
        const int ribCount = 3;
        const float tubRearZ = -0.60f, tubFrontZ = 0.60f;
        float bottomY = FR_TOP + ribOut;
        Vector2[] hex = HexPoly(0f, bottomY + HH, HF, HM, HH);
        Vector2[] glassInner = OffsetPoly(hex, glassIn);
        Vector2[] ribOuter = OffsetPoly(hex, ribOut);
        int[] low = { 1, 2, 3 };    // lower three edges of the hexagon = open-top half-hex tub

        Vector3 hinge = new Vector3(0f, FR_TOP, tubFrontZ);
        Transform dumpPivot = MakeChild(t, "Dump Pivot", hinge);
        Transform tubRoot = MakeChild(dumpPivot, "Tub Root (root-space coordinates)", -hinge);

        float segLen = ((tubFrontZ - tubRearZ) - ribCount * ribDepth) / (ribCount - 1);
        float[] ribZ = new float[ribCount];
        for (int i = 0; i < ribCount; i++) ribZ[i] = tubRearZ + ribDepth * 0.5f + i * (segLen + ribDepth);

        for (int j = 0; j < ribCount - 1; j++)
        {
            float zc = tubRearZ + ribDepth + segLen * 0.5f + j * (segLen + ribDepth);
            foreach (int k0 in low)
            {
                int k = k0;
                Mesh m = Cached("TubEdge" + k, () => EdgePrismZ(hex, glassInner, k, -segLen * 0.5f, segLen * 0.5f, "TubEdge" + k));
                MeshObj(tubRoot, "Tub " + (k == 2 ? "Floor" : "Glass Wall") + " e" + k + " bay" + j, m, k == 2 ? darkChassis : sciFiGlass, new Vector3(0f, 0f, zc), Quaternion.identity);
            }
        }
        Mesh ribA = Cached("RibA", () => EdgeSetZ(ribOuter, glassInner, low, -ribDepth * 0.5f, -0.015f, "RibA"));
        Mesh ribG = Cached("RibG", () => EdgeSetZ(ribOuter, glassInner, low, -0.015f, 0.015f, "RibG"));
        Mesh ribB = Cached("RibB", () => EdgeSetZ(ribOuter, glassInner, low, 0.015f, ribDepth * 0.5f, "RibB"));
        for (int i = 0; i < ribCount; i++)
        {
            Vector3 rp = new Vector3(0f, 0f, ribZ[i]);
            MeshObj(tubRoot, "Hex Rib " + i + " A", ribA, whiteBody, rp, Quaternion.identity);
            MeshObj(tubRoot, "Hex Rib " + i + " Glow", ribG, greenGlow, rp, Quaternion.identity);
            MeshObj(tubRoot, "Hex Rib " + i + " B", ribB, whiteBody, rp, Quaternion.identity);
        }

        Vector2[] trapInner = { glassInner[1], glassInner[2], glassInner[3], glassInner[4] };
        MeshObj(tubRoot, "Glass Rear Wall", Cached("RearWall", () => PrismZ(trapInner, -0.0125f, 0.0125f, "RearWall")), sciFiGlass, new Vector3(0f, 0f, ribZ[0]), Quaternion.identity);

        // dump gate: glass trapezoid hinged on the inner floor edge of the front rib
        float gateY = glassInner[2].y;
        Transform gatePivot = MakeChild(tubRoot, "Dump Gate Pivot", new Vector3(0f, gateY, ribZ[ribCount - 1]));
        Vector2[] gateShape = ShiftY(trapInner, -gateY);
        MeshObj(gatePivot, "Glass Dump Gate", Cached("DumpGate", () => PrismZ(gateShape, -0.0125f, 0.0125f, "DumpGate")), sciFiGlass, Vector3.zero, Quaternion.identity);

        // tub colliders (rotate with the tub): exact convex mitered prisms
        Vector2[] colOuter = OffsetPoly(hex, 0.04f);
        Vector2[] colInner = OffsetPoly(hex, -0.04f);
        float tubLen = tubFrontZ - tubRearZ;
        foreach (int k0 in low)
        {
            int k = k0;
            GameObject w = new GameObject("Tub Collider e" + k);
            w.transform.SetParent(tubRoot, false);
            MeshCollider mc = w.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("TubCollider" + k, () => EdgePrismZ(colOuter, colInner, k, -tubLen * 0.5f, tubLen * 0.5f, "TubCollider" + k));
        }
        {
            Vector2[] trapOuter = { colOuter[1], colOuter[2], colOuter[3], colOuter[4] };
            GameObject rw = new GameObject("Rear Wall Collider");
            rw.transform.SetParent(tubRoot, false);
            rw.transform.localPosition = new Vector3(0f, 0f, ribZ[0]);
            MeshCollider mc = rw.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("RearWallCollider", () => PrismZ(trapOuter, -0.03f, 0.03f, "RearWallCollider"));

            GameObject gc = new GameObject("Gate Collider");
            gc.transform.SetParent(gatePivot, false);
            MeshCollider gm = gc.AddComponent<MeshCollider>();
            gm.convex = true;
            gm.sharedMesh = Cached("GateCollider", () => PrismZ(gateShape, -0.03f, 0.03f, "GateCollider"));
        }

        // ---------------- wheels (big pneumatic look + hex hubs) ----------------
        List<Transform> spinPivots = new List<Transform>();
        for (int s = -1; s <= 1; s += 2)
        {
            for (int a = 0; a < axleZ.Length; a++)
            {
                string label = (a == 0 ? "Rear " : "Front ") + (s > 0 ? "R" : "L");
                Transform spin = MakeChild(wheelsG, "Wheel Spin " + label, new Vector3(s * 0.52f, WR, axleZ[a]));
                spinPivots.Add(spin);

                GameObject tr = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tr.name = "Tire " + label;
                tr.transform.SetParent(spin, false);
                tr.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tr.transform.localScale = new Vector3(WR * 2f, WHH, WR * 2f);
                tr.GetComponent<Renderer>().sharedMaterial = rubberTire;
                Object.DestroyImmediate(tr.GetComponent<Collider>());

                Quaternion outward = Quaternion.Euler(0f, s * 90f, 0f);
                MeshObj(spin, "Hub Chrome " + label, Cached("HubC", () => PrismZ(RegHex(0, 0, 0.13f), WHH, WHH + 0.015f, "HubC")), chromeHub, Vector3.zero, outward);
                MeshObj(spin, "Hub Blue " + label, Cached("HubB", () => PrismZ(RegHex(0, 0, 0.07f), WHH + 0.015f, WHH + 0.025f, "HubB")), blueAccent, Vector3.zero, outward);
                MeshObj(spin, "Hub Glow " + label, Cached("HubG", () => PrismZ(RegHex(0, 0, 0.03f), WHH + 0.025f, WHH + 0.035f, "HubG")), greenGlow, Vector3.zero, outward);

                AddSphereCollider(collidersG, "Wheel Collider " + label, new Vector3(s * 0.52f, WR, axleZ[a]), WR, slick);
            }
        }

        // ---------------- drawbar + glowing hex grip loop ----------------
        Vector3 gripCenter = new Vector3(0f, 0.84f, 1.65f);
        const float gripR = 0.10f;
        Beam(handleG, "Drawbar", darkChassis, new Vector3(0f, FR_TOP + 0.025f, 0.70f), new Vector3(0f, gripCenter.y, gripCenter.z - gripR - 0.004f), 0.04f, 0.04f);
        Vector2[] go = RegHex(0, 0, gripR);
        Vector2[] gi = OffsetPoly(go, -0.022f);
        Vector2[] gg = OffsetPoly(go, -0.032f);
        Quaternion gripRot = Quaternion.Euler(0f, 90f, 0f);   // ring lies in the Y-Z plane, hand goes through along X
        MeshObj(handleG, "Hex Grip Loop", Cached("GripRing", () => RingZ(go, gi, -0.02f, 0.02f, "GripRing")), darkChassis, gripCenter, gripRot);
        MeshObj(handleG, "Hex Grip Glow", Cached("GripGlow", () => RingZ(gi, gg, -0.012f, 0.012f, "GripGlow")), greenGlow, gripCenter, gripRot);
        Transform gripPoint = MakeChild(handleG, "Handle Grip Point", gripCenter);

        // ---------------- colliders, light, grab trigger ----------------
        AddBoxCollider(collidersG, "Frame Collider", new Vector3(0f, (FR_BOT + FR_TOP) * 0.5f, 0.05f), new Vector3(0.60f, FR_TOP - FR_BOT, 1.34f));
        {
            GameObject cl = new GameObject("Tub Glow");
            cl.transform.SetParent(lightsG, false);
            cl.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            Light l = cl.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.2f, 1f, 0.6f); l.range = 2.5f; l.intensity = 0.6f; l.shadows = LightShadows.None;
        }
        AddEntryTrigger(triggersG, "Grab Trigger", new Vector3(0f, 0.6f, 0.7f), new Vector3(1.7f, 1.4f, 3.4f));

        // ---------------- controller + prefab ----------------
        HexGorillaCartController controller = root.AddComponent<HexGorillaCartController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("handleGrip").objectReferenceValue = gripPoint;
        so.FindProperty("dumpPivot").objectReferenceValue = dumpPivot;
        so.FindProperty("gatePivot").objectReferenceValue = gatePivot;
        SetObjectArray(so, "wheelSpinPivots", spinPivots);
        so.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = AssetFolder + "/HexGorillaCart_Small.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log("CyberHex Gorilla Cart (Small) created at: " + prefabPath);
    }

    /// <summary>Several edges of a mitered ring merged into one mesh (used for the open half-hex ribs).</summary>
    private static Mesh EdgeSetZ(Vector2[] outer, Vector2[] inner, int[] ks, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        foreach (int k in ks)
        {
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
        }
        return mb.Build(name);
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
