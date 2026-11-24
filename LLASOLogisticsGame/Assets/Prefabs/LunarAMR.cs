using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MODEL ONLY: add this to any GameObject and press Play (or right-click the header > Preview / Rebuild).
/// It builds a roomba-style warehouse robot (Pudu T300 / Ateago S4P inspired). It does NOT move on its own.
///
/// Ready for driving later:
///   - leftWheel / rightWheel pivots (rotate around local X to spin), lidarHead, statusLED references
///   - real-world numbers: wheelRadius, trackWidth, maxSpeed, maxPayloadKg
///   - optional Rigidbody (tick Add Rigidbody) with the right mass and stable constraints
///   - SpinWheels(forwardSpeed, turnRate, dt) to animate the wheels from whatever movement you add
///   - SetLight(color) for the status LED
/// Pivot is at ground level, centered, robot faces +Z. Units are meters / kg.
/// </summary>
public class LunarAMR : MonoBehaviour
{
    public enum Style { PuduT300Style, AteagoS4PStyle }

    [Header("Look")]
    public Style style = Style.PuduT300Style;
    public Color bodyColor = new Color(0.94f, 0.95f, 0.97f);
    public Color deckColor = new Color(0.45f, 0.18f, 0.72f);
    [Tooltip("Optional. Leave empty to auto-pick the URP/Standard shader. Assign any material if the robot looks pink in a build.")]
    public Material baseMaterial;

    [Header("Options")]
    public bool addRigidbody = false;        // turn on when you start moving it with physics
    public bool spinLidar = true;            // S4P-style lidar puck spins by itself

    // ---- filled in by Build(), use these when you add movement ----
    [Header("Parts (read-only)")]
    public Transform leftWheel;
    public Transform rightWheel;
    public Transform lidarHead;              // null on the T300 style
    public Renderer statusLED;
    public float wheelRadius, trackWidth, maxSpeed, maxPayloadKg, massKg;
    public Vector3 deckTopLocal;             // center of the cargo deck surface (local space)
    public Vector2 deckSize;                 // x = width, y = length

    struct Spec
    {
        public float length, width, deckHeight, groundClearance, cornerRadius;
        public float wheelRadius, wheelWidth, trackWidth, casterRadius;
        public float massKg, maxPayloadKg, maxSpeed, mastHeight;
        public bool hasMast, hasTopLidar;
        public Vector2[] casters;
    }

    static Spec GetSpec(Style st)
    {
        if (st == Style.PuduT300Style)
            return new Spec
            {
                length = 0.835f, width = 0.50f, deckHeight = 0.28f, groundClearance = 0.045f, cornerRadius = 0.17f,
                wheelRadius = 0.075f, wheelWidth = 0.05f, trackWidth = 0.34f, casterRadius = 0.04f,
                massKg = 65f, maxPayloadKg = 300f, maxSpeed = 1.2f,
                hasMast = true, mastHeight = 0.55f, hasTopLidar = false,
                casters = new[] { new Vector2(-0.17f, 0.31f), new Vector2(0.17f, 0.31f), new Vector2(-0.17f, -0.31f), new Vector2(0.17f, -0.31f) }
            };
        return new Spec
        {
            length = 0.704f, width = 0.47f, deckHeight = 0.325f, groundClearance = 0.04f, cornerRadius = 0.15f,
            wheelRadius = 0.0625f, wheelWidth = 0.045f, trackWidth = 0.335f, casterRadius = 0.035f,
            massKg = 45f, maxPayloadKg = 100f, maxSpeed = 1.0f,
            hasMast = false, mastHeight = 0f, hasTopLidar = true,
            casters = new[] { new Vector2(-0.14f, 0.25f), new Vector2(0.14f, 0.25f), new Vector2(-0.14f, -0.25f), new Vector2(0.14f, -0.25f) }
        };
    }

    const string VisualsName = "AMR_Visuals";
    float _spinL, _spinR;

    // ==================================================================
    // Helpers for when you add movement
    // ==================================================================

    /// <summary>Rolls the wheels for a given forward speed (m/s) and turn rate (rad/s, right = +). Call every frame with Time.deltaTime.</summary>
    public void SpinWheels(float forwardSpeed, float turnRate, float dt)
    {
        float vL = forwardSpeed + turnRate * trackWidth * 0.5f;
        float vR = forwardSpeed - turnRate * trackWidth * 0.5f;
        float k = Mathf.Rad2Deg / Mathf.Max(0.001f, wheelRadius) * dt;
        _spinL = Mathf.Repeat(_spinL + vL * k, 360f);
        _spinR = Mathf.Repeat(_spinR + vR * k, 360f);
        if (leftWheel != null) leftWheel.localRotation = Quaternion.Euler(_spinL, 0f, 0f);
        if (rightWheel != null) rightWheel.localRotation = Quaternion.Euler(_spinR, 0f, 0f);
    }

    /// <summary>Sets the glowing status band color (e.g. green idle, blue moving, red stopped).</summary>
    public void SetLight(Color c)
    {
        if (statusLED == null) return;
        Material m = statusLED.material;
        m.color = c;
        m.SetColor("_EmissionColor", c * 2f);
    }

    void Awake() { Build(); }

    void Update()
    {
        if (spinLidar && lidarHead != null) lidarHead.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
    }

    // ==================================================================
    // Build
    // ==================================================================
    [ContextMenu("Preview / Rebuild")]
    public void Build()
    {
        Spec s = GetSpec(style);

        // remove any previous build (deactivate first so old colliders disappear immediately)
        Transform old;
        while ((old = transform.Find(VisualsName)) != null)
        {
            old.name = VisualsName + "_old";
            old.gameObject.SetActive(false);
            SafeDestroy(old.gameObject);
        }
        var root = new GameObject(VisualsName).transform;
        root.SetParent(transform, false);

        wheelRadius = s.wheelRadius;
        trackWidth = s.trackWidth;
        maxSpeed = s.maxSpeed;
        maxPayloadKg = s.maxPayloadKg;
        massKg = s.massKg;

        float halfL = s.length * 0.5f;
        float chassisH = s.deckHeight - s.groundClearance;
        const float plateH = 0.012f;
        float deckTop = s.deckHeight + plateH;
        float frontZ = halfL + 0.007f;
        float rearZ = -frontZ;
        deckTopLocal = new Vector3(0, deckTop, 0);
        deckSize = new Vector2(s.width - 0.06f, s.length - 0.06f);

        if (addRigidbody)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = s.massKg;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.centerOfMass = new Vector3(0f, s.wheelRadius, 0f);
        }

        // materials
        var matBody = MakeMat("AMR_Body", bodyColor, 0.55f, 0.1f);
        var matDeck = MakeMat("AMR_DeckMat", deckColor, 0.35f, 0f);
        var matDark = MakeMat("AMR_Bumper", new Color(0.10f, 0.10f, 0.12f), 0.3f, 0f);
        var matTire = MakeMat("AMR_Tire", new Color(0.04f, 0.04f, 0.04f), 0.1f, 0f);
        var matHub = MakeMat("AMR_Hub", new Color(0.65f, 0.67f, 0.70f), 0.6f, 0.8f);
        var matGlass = MakeMat("AMR_Glass", new Color(0.02f, 0.03f, 0.05f), 0.9f, 0f);
        var matScreen = MakeMat("AMR_Screen", new Color(0.02f, 0.04f, 0.08f), 0.9f, 0f, new Color(0.05f, 0.25f, 0.65f));
        var matLed = MakeMat("AMR_StatusLED", new Color(0.1f, 0.9f, 0.3f), 0.5f, 0f, new Color(0.1f, 0.9f, 0.3f) * 2f);
        var matGold = MakeMat("AMR_Contacts", new Color(0.85f, 0.68f, 0.2f), 0.8f, 1f);

        var slick = new PhysicsMaterial("AMR_Frictionless")
        {
            dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
        };

        // chassis + collider (default friction so cargo grips the deck)
        var chassis = MeshPart("Chassis", root, new Vector3(0, s.groundClearance, 0),
            RoundedSlab("ChassisMesh", s.width, s.length, chassisH, s.cornerRadius, 0.03f), matBody);
        var box = chassis.AddComponent<BoxCollider>();
        box.center = new Vector3(0, (chassisH + plateH) * 0.5f, 0);
        box.size = new Vector3(s.width, chassisH + plateH, s.length);

        MeshPart("Bumper", root, new Vector3(0, s.groundClearance + 0.01f, 0),
            RoundedSlab("BumperMesh", s.width + 0.014f, s.length + 0.014f, 0.055f, s.cornerRadius + 0.007f, 0.012f), matDark);

        var led = MeshPart("StatusLED", root, new Vector3(0, s.deckHeight - 0.045f, 0),
            RoundedSlab("LedMesh", s.width + 0.006f, s.length + 0.006f, 0.014f, s.cornerRadius + 0.003f, 0.004f), matLed);
        statusLED = led.GetComponent<Renderer>();

        MeshPart("DeckMat", root, new Vector3(0, s.deckHeight, 0),
            RoundedSlab("DeckMesh", s.width - 0.07f, s.length - 0.07f, plateH, s.cornerRadius - 0.035f, 0.003f), matDeck);

        // wheels + casters
        leftWheel = BuildWheel(root, "Wheel_L", -s.trackWidth * 0.5f, s, matTire, matHub, matDeck, slick);
        rightWheel = BuildWheel(root, "Wheel_R", s.trackWidth * 0.5f, s, matTire, matHub, matDeck, slick);
        for (int i = 0; i < s.casters.Length; i++)
        {
            var c = new GameObject("Caster_" + i).transform;
            c.SetParent(root, false);
            c.localPosition = new Vector3(s.casters[i].x, s.casterRadius, s.casters[i].y);
            Prim(PrimitiveType.Sphere, "Ball", c, Vector3.zero, Vector3.one * (s.casterRadius * 2f), matTire);
            var sc = c.gameObject.AddComponent<SphereCollider>();
            sc.radius = s.casterRadius;
            sc.sharedMaterial = slick;
        }

        // sensors & details
        Prim(PrimitiveType.Cube, "FrontLidarWindow", root, new Vector3(0, s.groundClearance + 0.038f, frontZ), new Vector3(0.16f, 0.022f, 0.006f), matGlass);
        float camX = s.hasTopLidar ? 0.06f : 0.045f;
        float camW = s.hasTopLidar ? 0.055f : 0.04f;
        for (int sg = -1; sg <= 1; sg += 2)
        {
            Prim(PrimitiveType.Cube, "DepthCam", root, new Vector3(sg * camX, s.deckHeight - 0.09f, halfL + 0.004f), new Vector3(camW, 0.03f, 0.02f), matGlass);
            Prim(PrimitiveType.Cube, "ChargeContact", root, new Vector3(sg * 0.04f, s.groundClearance + 0.035f, rearZ - 0.002f), new Vector3(0.03f, 0.02f, 0.008f), matGold);
        }

        if (s.hasMast)
        {
            float mastZ = -halfL + 0.075f;
            var mast = MeshPart("Mast", root, new Vector3(0, deckTop, mastZ),
                RoundedSlab("MastMesh", 0.10f, 0.05f, s.mastHeight, 0.02f, 0.006f), matDark);
            var mc = mast.AddComponent<BoxCollider>();
            mc.center = new Vector3(0, s.mastHeight * 0.5f, 0);
            mc.size = new Vector3(0.10f, s.mastHeight, 0.05f);
            float top = deckTop + s.mastHeight;
            var screen = Prim(PrimitiveType.Cube, "Touchscreen", root, new Vector3(0, top - 0.09f, mastZ - 0.028f), new Vector3(0.14f, 0.09f, 0.01f), matScreen);
            screen.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            var bar = Prim(PrimitiveType.Cylinder, "PushHandle", root, new Vector3(0, top, mastZ - 0.04f), new Vector3(0.028f, 0.20f, 0.028f), matHub);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        lidarHead = null;
        if (s.hasTopLidar)
        {
            float puckZ = halfL - 0.075f;
            Prim(PrimitiveType.Cylinder, "LidarBase", root, new Vector3(0, deckTop + 0.015f, puckZ), new Vector3(0.10f, 0.015f, 0.10f), matDark);
            lidarHead = new GameObject("LidarHead").transform;
            lidarHead.SetParent(root, false);
            lidarHead.localPosition = new Vector3(0, deckTop + 0.045f, puckZ);
            Prim(PrimitiveType.Cylinder, "Head", lidarHead, Vector3.zero, new Vector3(0.09f, 0.015f, 0.09f), matGlass);
            Prim(PrimitiveType.Cube, "Window", lidarHead, new Vector3(0, 0, 0.044f), new Vector3(0.035f, 0.012f, 0.012f), matScreen);
        }
    }

    Transform BuildWheel(Transform parent, string name, float x, Spec s, Material tire, Material hub, Material marker, PhysicsMaterial slick)
    {
        float R = s.wheelRadius, w = s.wheelWidth;
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = new Vector3(x, R, 0f);

        var t = Prim(PrimitiveType.Cylinder, "Tire", pivot, Vector3.zero, new Vector3(R * 2f, w * 0.5f, R * 2f), tire);
        t.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var h = Prim(PrimitiveType.Cylinder, "Hub", pivot, Vector3.zero, new Vector3(R * 1.1f, w * 0.52f, R * 1.1f), hub);
        h.transform.localRotation = Quaternion.Euler(0, 0, 90);
        Prim(PrimitiveType.Cube, "Marker", pivot, new Vector3(0, R * 0.72f, 0), new Vector3(w * 1.06f, R * 0.16f, R * 0.16f), marker);

        var sc = pivot.gameObject.AddComponent<SphereCollider>();
        sc.radius = R;
        sc.sharedMaterial = slick;
        return pivot;
    }

    GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        SafeDestroy(go.GetComponent<Collider>());
        return go;
    }

    GameObject MeshPart(string name, Transform parent, Vector3 pos, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    Material MakeMat(string name, Color c, float smooth, float metal, Color? emission = null)
    {
        Material m;
        if (baseMaterial != null) m = new Material(baseMaterial);
        else
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
        }
        m.name = name;
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
        }
        return m;
    }

    static void SafeDestroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    /// <summary>Rounded-rectangle slab (w = X, d = Z, h = Y) with beveled edges, sitting on y = 0.</summary>
    static Mesh RoundedSlab(string name, float w, float d, float h, float radius, float bevel, int seg = 10)
    {
        radius = Mathf.Min(radius, Mathf.Min(w, d) * 0.5f - 0.001f);
        radius = Mathf.Max(radius, 0.003f);
        bevel = Mathf.Min(bevel, radius * 0.9f);
        bevel = Mathf.Min(bevel, h * 0.45f);

        float[] insets = { bevel, 0f, 0f, bevel };
        float[] ys = { 0f, bevel, h - bevel, h };
        int per = 4 * (seg + 1);
        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int k = 0; k < 4; k++)
        {
            float hx = w * 0.5f - insets[k];
            float hz = d * 0.5f - insets[k];
            float r = Mathf.Max(0.002f, radius - insets[k]);
            for (int c = 0; c < 4; c++)
            {
                float cx = (c == 0 || c == 3) ? hx - r : -(hx - r);
                float cz = (c == 0 || c == 1) ? hz - r : -(hz - r);
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + 90f * i / seg) * Mathf.Deg2Rad;
                    verts.Add(new Vector3(cx + r * Mathf.Cos(a), ys[k], cz + r * Mathf.Sin(a)));
                }
            }
        }

        for (int k = 0; k < 3; k++)
            for (int i = 0; i < per; i++)
            {
                int i2 = (i + 1) % per;
                int a0 = k * per + i, a1 = k * per + i2, b0 = (k + 1) * per + i, b1 = (k + 1) * per + i2;
                tris.Add(a0); tris.Add(b0); tris.Add(b1);
                tris.Add(a0); tris.Add(b1); tris.Add(a1);
            }

        int topStart = verts.Count;
        verts.Add(new Vector3(0, h, 0));
        for (int i = 0; i < per; i++) verts.Add(verts[3 * per + i]);
        for (int i = 0; i < per; i++)
        {
            int i2 = (i + 1) % per;
            tris.Add(topStart); tris.Add(topStart + 1 + i2); tris.Add(topStart + 1 + i);
        }

        int botStart = verts.Count;
        verts.Add(Vector3.zero);
        for (int i = 0; i < per; i++) verts.Add(verts[i]);
        for (int i = 0; i < per; i++)
        {
            int i2 = (i + 1) % per;
            tris.Add(botStart); tris.Add(botStart + 1 + i); tris.Add(botStart + 1 + i2);
        }

        var uv = new Vector2[verts.Count];
        for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(verts[i].x / w + 0.5f, verts[i].z / d + 0.5f);

        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
