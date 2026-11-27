// LLASO working elevator (v7).  Drop this file anywhere in Assets/ - no setup, no components to add.
// It finds LLASO_Elevator_Car and LLASO_Elevator_Door_L1_A/B, L2_A/B inside the facility on its own.
//
//   Stand in front of a door (inside the cyan box shown in the Scene view)  ->  press O to call the car.
//   Stand inside the car                                                    ->  press O to ride to the other level.
//
// Works with the legacy Input Manager, the new Input System, or both.
// Needs the v7 LLASO_Facility.obj (1.4 m wide door opening). With the old 1.0 m opening a normal
// player capsule cannot fit through the doors.
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LLASO_Elevator : MonoBehaviour
{
    // ---- the key ----
    const KeyCode LegacyKey = KeyCode.O;
#if ENABLE_INPUT_SYSTEM
    const Key NewKey = Key.O;
#endif
    const string KeyLabel = "O";

    // ---- tweak here ----
    public float speed       = 1.8f;   // car speed, metres / second
    public float doorTime    = 0.8f;   // seconds to open or close
    public float useRange    = 3.0f;   // how far in front of a door the call zone reaches (metres)
    public float sideMargin  = 0.8f;   // how far the call zone extends beside the door opening
    public float pocketDepth = 0.3f;   // how deep the door panels tuck into the wall when open
    public float autoClose   = 8f;     // doors close by themselves after this long with nobody near
    public bool  debugHud    = false;  // tick this on the controller object while playing to see live status

    class Panel
    {
        public Transform t; public Vector3 homePos; public Bounds fb;   // fb = bounds in the shaft frame
        public float lo, hi, width, outSign, inner, outer, meshLo; public bool scalable; public BoxCollider col;
    }

    // everything is measured in the "shaft frame" = the local space of the car's parent (the facility)
    Transform car, frame; Vector3 carHomeLocal; Bounds carHomeF;
    Panel[][] panels = new Panel[2][];
    Bounds[] zone = new Bounds[2];
    float[] floorY = new float[2], doorMid = new float[2], openLo = new float[2], openHi = new float[2];
    float levelDy; int fa, sa; float frontSign;     // fa = axis pointing out of the doors, sa = axis the doors slide along
    float[] open = new float[2];                    // 0 closed .. 1 open
    int level = 0; bool busy, ready; float idleTimer, nextSetup, nextFind;
    Transform player; CharacterController cc; Rigidbody prb; Camera cam;
    Vector3[] probes = new Vector3[3]; int np;
    string prompt = "", waitMsg = ""; bool dbgInCar, dbgN0, dbgN1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<LLASO_Elevator>() != null) return;
        new GameObject("LLASO_ElevatorController").AddComponent<LLASO_Elevator>();
    }

    // ------------------------------------------------------------------ frame helpers
    Vector3 ToF(Vector3 w)  { return frame ? frame.InverseTransformPoint(w) : w; }
    Vector3 VToW(Vector3 f) { return frame ? frame.TransformVector(f) : f; }
    Vector3 V(float a, float y, float f) { Vector3 v = Vector3.zero; v[sa] = a; v[fa] = f; v.y = y; return v; }

    Bounds FB(Renderer r)
    {
        Bounds w = r.bounds; Vector3 c = w.center, e = w.extents;
        Bounds b = new Bounds(ToF(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
        for (int i = 1; i < 8; i++)
            b.Encapsulate(ToF(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
        return b;
    }

    static void MakeSolid(GameObject g)
    {
        foreach (var old in g.GetComponents<Collider>()) old.enabled = false;      // replace any mesh collider with simple boxes
        var rb = g.GetComponent<Rigidbody>(); if (!rb) rb = g.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
    }
    static BoxCollider Box(GameObject g, Vector3 center, Vector3 size)
    {
        var b = g.AddComponent<BoxCollider>(); b.center = center; b.size = size; return b;
    }

    // ------------------------------------------------------------------ setup
    Panel MakePanel(Transform t)
    {
        var r = t.GetComponentInChildren<Renderer>(); if (!r) return null;
        var p = new Panel(); p.t = t; p.homePos = t.localPosition; p.fb = FB(r); return p;
    }

    void FinishPanel(Panel p, float outSign)
    {
        p.outSign = outSign; p.lo = p.fb.min[sa]; p.hi = p.fb.max[sa]; p.width = p.hi - p.lo;
        p.inner = outSign > 0 ? p.lo : p.hi; p.outer = outSign > 0 ? p.hi : p.lo;
        p.meshLo = p.lo - p.homePos[sa];
        p.scalable = Quaternion.Angle(p.t.localRotation, Quaternion.identity) < 0.1f
                     && (p.t.localScale - Vector3.one).sqrMagnitude < 1e-6f && p.width > 0.05f;
        MakeSolid(p.t.gameObject);
        Vector3 sz = p.fb.size; sz[fa] = Mathf.Max(sz[fa], 0.15f);                 // thick enough to never be tunnelled
        p.col = Box(p.t.gameObject, p.fb.center - p.homePos, sz);
    }

    bool TrySetup()
    {
        var carGo = GameObject.Find("LLASO_Elevator_Car"); if (!carGo) return false;
        var names = new[] { "LLASO_Elevator_Door_L1_A", "LLASO_Elevator_Door_L1_B", "LLASO_Elevator_Door_L2_A", "LLASO_Elevator_Door_L2_B" };
        var g = new GameObject[4];
        for (int i = 0; i < 4; i++) { g[i] = GameObject.Find(names[i]); if (!g[i]) return false; }

        car = carGo.transform; frame = car.parent; carHomeLocal = car.localPosition;
        var cr = car.GetComponentInChildren<Renderer>();
        if (!cr) { Debug.LogWarning("LLASO_Elevator: car has no renderer."); enabled = false; return false; }
        carHomeF = FB(cr);

        for (int l = 0; l < 2; l++)
        {
            var a = MakePanel(g[l * 2].transform); var b = MakePanel(g[l * 2 + 1].transform);
            if (a == null || b == null) { Debug.LogWarning("LLASO_Elevator: door has no renderer."); enabled = false; return false; }
            panels[l] = new[] { a, b };
            Bounds comb = a.fb; comb.Encapsulate(b.fb);
            if (l == 0)
            {
                fa = comb.size.x < comb.size.z ? 0 : 2; sa = 2 - fa;                       // door panels are thin along their front axis
                frontSign = Mathf.Sign(comb.center[fa] - carHomeF.center[fa]); if (frontSign == 0f) frontSign = 1f;
            }
            floorY[l] = comb.min.y; doorMid[l] = comb.center[fa]; openLo[l] = comb.min[sa]; openHi[l] = comb.max[sa];
            float s = Mathf.Sign(a.fb.center[sa] - b.fb.center[sa]); if (s == 0f) s = -1f;
            FinishPanel(a, s); FinishPanel(b, -s);
        }
        levelDy = floorY[1] - floorY[0];

        for (int l = 0; l < 2; l++)                                                         // call zone in front of each door
        {
            float lo = frontSign > 0 ? doorMid[l] - 0.5f : doorMid[l] - useRange;
            float hi = frontSign > 0 ? doorMid[l] + useRange : doorMid[l] + 0.5f;
            Vector3 mn = Vector3.zero, mx = Vector3.zero;
            mn[sa] = openLo[l] - sideMargin; mx[sa] = openHi[l] + sideMargin;
            mn[fa] = lo;                      mx[fa] = hi;
            mn.y = floorY[l] - 0.4f;          mx.y = floorY[l] + 2.6f;
            Bounds z = new Bounds(); z.SetMinMax(mn, mx); zone[l] = z;
        }

        // car: solid floor + back wall + two side walls (open at the front), built from boxes in car-local space
        MakeSolid(car.gameObject);
        Bounds lb = new Bounds(carHomeF.center - carHomeLocal, carHomeF.size);
        float top = lb.min.y + 0.05f, wallH = lb.size.y - 0.05f, wallY = top + wallH * 0.5f, t2 = 0.1f;
        Box(car.gameObject, new Vector3(lb.center.x, top - 0.15f, lb.center.z), new Vector3(lb.size.x, 0.3f, lb.size.z));
        float backF = frontSign > 0 ? lb.min[fa] + t2 * 0.5f : lb.max[fa] - t2 * 0.5f;
        Box(car.gameObject, V(lb.center[sa], wallY, backF), V(lb.size[sa], wallH, t2));
        Box(car.gameObject, V(lb.min[sa] + t2 * 0.5f, wallY, lb.center[fa]), V(t2, wallH, lb.size[fa]));
        Box(car.gameObject, V(lb.max[sa] - t2 * 0.5f, wallY, lb.center[fa]), V(t2, wallH, lb.size[fa]));

        open[0] = 1f; open[1] = 0f; ApplyDoors(0); ApplyDoors(1);                          // ground doors start open
        Physics.SyncTransforms();
        ready = true;
        Debug.Log("LLASO_Elevator: ready. Call zones are the cyan boxes in the Scene view. Key = " + KeyLabel);
        return true;
    }

    // ------------------------------------------------------------------ player lookup
    Camera Cam()
    {
        if (cam && cam.isActiveAndEnabled) return cam;
        cam = Camera.main;
        if (!cam) foreach (var c2 in Camera.allCameras) { cam = c2; break; }
        return cam;
    }

    void FindPlayer()
    {
        GameObject p = null;
        try { p = GameObject.FindWithTag("Player"); } catch { }
        if (!p) p = GameObject.Find("Player");
        if (!p) { var anyCc = FindFirstObjectByType<CharacterController>(); if (anyCc) p = anyCc.gameObject; }
        if (!p) { var c3 = Cam(); if (c3) p = c3.transform.root.gameObject; }
        if (!p) return;
        player = p.transform; cc = p.GetComponentInChildren<CharacterController>(); prb = p.GetComponentInChildren<Rigidbody>();
        Debug.Log("LLASO_Elevator: using '" + p.name + "' as the player.");
    }

    Vector3 FeetWorld()
    {
        if (cc)
        {
            Transform ct = cc.transform; float h = cc.height * Mathf.Abs(ct.lossyScale.y);
            return ct.TransformPoint(cc.center) + Vector3.down * (h * 0.5f);
        }
        Collider[] cols = player.GetComponentsInChildren<Collider>(); bool any = false; Bounds pb = new Bounds();
        foreach (var col in cols)
        {
            if (col.isTrigger || !col.enabled) continue;
            if (!any) { pb = col.bounds; any = true; } else pb.Encapsulate(col.bounds);
        }
        if (any) return new Vector3(pb.center.x, pb.min.y, pb.center.z);
        return player.position;
    }

    float PlayerRadius()
    {
        if (cc) { Vector3 s = cc.transform.lossyScale; return cc.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)); }
        var cap = player.GetComponentInChildren<CapsuleCollider>();
        if (cap) { Vector3 s2 = cap.transform.lossyScale; return cap.radius * Mathf.Max(Mathf.Abs(s2.x), Mathf.Abs(s2.z)); }
        return 0.4f;
    }

    void BuildProbes()
    {
        np = 0; Vector3 f = FeetWorld();
        probes[np++] = ToF(f + Vector3.up * 0.1f);
        probes[np++] = ToF(f + Vector3.up * 1.0f);
        var c4 = Cam(); if (c4) probes[np++] = ToF(c4.transform.position);
    }

    // ------------------------------------------------------------------ queries
    bool InZone(Bounds z) { for (int i = 0; i < np; i++) if (z.Contains(probes[i])) return true; return false; }

    float CarDy() { return car.localPosition.y - carHomeLocal.y; }

    bool PlayerInCar()
    {
        Bounds b = carHomeF; b.center = b.center + Vector3.up * CarDy();
        for (int i = 0; i < 2 && i < np; i++)
        {
            Vector3 p = probes[i];
            if (p[sa] > b.min[sa] - 0.05f && p[sa] < b.max[sa] + 0.05f && p[fa] > b.min[fa] - 0.05f && p[fa] < b.max[fa] + 0.05f
                && p.y > b.min.y - 0.3f && p.y < b.max.y) return true;
        }
        return false;
    }

    // is the player standing where the door panels travel? (so we never close a door on them)
    bool Occupied(int l)
    {
        if (!player) return false;
        Vector3 f = ToF(FeetWorld()); float r = PlayerRadius();
        if (f.y < floorY[l] - 0.5f || f.y > floorY[l] + 2.3f) return false;
        if (Mathf.Abs(f[fa] - doorMid[l]) > r + 0.1f) return false;
        return f[sa] > openLo[l] - r && f[sa] < openHi[l] + r;
    }

    bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[NewKey].wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyKey)) return true;
#endif
        return false;
    }

    string PromptFor(int l)
    {
        if (level != l) return "[" + KeyLabel + "] Call elevator";
        return open[l] < 0.99f ? "[" + KeyLabel + "] Open elevator doors" : "";
    }

    // ------------------------------------------------------------------ main loop
    void Update()
    {
        if (!ready)
        {
            if (Time.unscaledTime >= nextSetup) { nextSetup = Time.unscaledTime + 0.5f; TrySetup(); }
            return;
        }
        if (!player)
        {
            if (Time.unscaledTime >= nextFind) { nextFind = Time.unscaledTime + 0.5f; FindPlayer(); }
            prompt = ""; return;
        }

        BuildProbes();
        bool inCar = PlayerInCar(), n0 = InZone(zone[0]), n1 = InZone(zone[1]);
        dbgInCar = inCar; dbgN0 = n0; dbgN1 = n1;

        for (int l = 0; l < 2; l++)                                                         // door panels are solid only when fully closed and nobody is in them
        {
            bool solid = open[l] < 0.001f && !Occupied(l);
            foreach (var p in panels[l]) p.col.enabled = solid;
        }

        prompt = busy ? waitMsg : "";
        if (busy) return;

        if (inCar) prompt = "[" + KeyLabel + "] Go to level " + (level == 0 ? 2 : 1);
        else if (n0) prompt = PromptFor(0);
        else if (n1) prompt = PromptFor(1);

        if (Pressed())
        {
            if (inCar) StartCoroutine(Go(1 - level));
            else if (n0) StartCoroutine(Go(0));
            else if (n1) StartCoroutine(Go(1));
        }

        idleTimer = (inCar || n0 || n1) ? 0f : idleTimer + Time.deltaTime;
        if (idleTimer > autoClose && open[level] > 0.001f) StartCoroutine(CloseOnly());
    }

    IEnumerator CloseOnly() { busy = true; yield return Slide(level, 0f); busy = false; }

    IEnumerator Go(int target)
    {
        busy = true; waitMsg = "";
        if (target == level) { yield return Slide(level, 1f); busy = false; yield break; }

        yield return Slide(level, 0f);                                                      // close
        while (Occupied(level)) { waitMsg = "Step away from the doors"; yield return null; }
        waitMsg = "";

        BuildProbes();
        bool riding = PlayerInCar();
        float rel = riding ? ToF(FeetWorld()).y - CarDy() : 0f;
        float from = level == 0 ? 0f : levelDy, to = target == 0 ? 0f : levelDy;
        float dist = Mathf.Max(0.01f, Mathf.Abs(to - from)), t = 0f;
        while (t < 1f)                                                                      // travel, eased
        {
            t += Time.deltaTime * speed / dist;
            SetCarY(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
            if (riding) Glue(rel);
            Physics.SyncTransforms();
            yield return null;
        }
        SetCarY(to); if (riding) Glue(rel); Physics.SyncTransforms();

        level = target;
        yield return Slide(level, 1f);                                                      // open
        busy = false;
    }

    void SetCarY(float off) { Vector3 lp = carHomeLocal; lp.y += off; car.localPosition = lp; }

    // keep the rider's feet at the same height above the car floor, whatever their controller does
    void Glue(float rel)
    {
        float dy = (rel + CarDy()) - ToF(FeetWorld()).y;
        if (Mathf.Abs(dy) > 0.0005f) Shift(VToW(Vector3.up * dy));
    }

    void Shift(Vector3 w)
    {
        if (cc) { bool was = cc.enabled; cc.enabled = false; player.position += w; cc.enabled = was; }
        else if (prb)
        {
            prb.position += w;
#if UNITY_6000_0_OR_NEWER
            Vector3 v = prb.linearVelocity; v.y = 0f; prb.linearVelocity = v;
#else
            Vector3 v = prb.velocity; v.y = 0f; prb.velocity = v;
#endif
        }
        else player.position += w;
    }

    IEnumerator Slide(int l, float target)
    {
        float start = open[l];
        if (Mathf.Approximately(start, target)) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / doorTime;
            open[l] = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            ApplyDoors(l); yield return null;
        }
        open[l] = target; ApplyDoors(l);
    }

    // the panels slide outward AND shrink so they tuck into the wall beside the opening (the opening is wider than the wall pocket)
    void ApplyDoors(int l)
    {
        float t = open[l];
        foreach (var p in panels[l])
        {
            Vector3 lp = p.homePos, ls = p.t.localScale;
            if (p.scalable)
            {
                float pocket = Mathf.Min(pocketDepth, p.width * 0.5f);
                float inn  = Mathf.Lerp(p.inner, p.outer, t);
                float outE = Mathf.Lerp(p.outer, p.outer + p.outSign * pocket, t);
                float lo = Mathf.Min(inn, outE), hi = Mathf.Max(inn, outE);
                float s = Mathf.Max((hi - lo) / p.width, 0.01f);
                ls = Vector3.one; ls[sa] = s; lp[sa] = lo - s * p.meshLo;
            }
            else lp[sa] = p.homePos[sa] + p.outSign * p.width * 0.95f * t;                  // fallback: plain slide
            p.t.localPosition = lp; p.t.localScale = ls;
        }
    }

    // ------------------------------------------------------------------ UI / debug
    void OnGUI()
    {
        if (debugHud && ready)
            GUI.Label(new Rect(10, 10, 700, 24), "Elevator  player=" + (player ? player.name : "none") + "  inCar=" + dbgInCar
                      + "  nearL1=" + dbgN0 + "  nearL2=" + dbgN1 + "  level=" + (level + 1) + "  busy=" + busy);
        if (string.IsNullOrEmpty(prompt)) return;
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 36), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        st.normal.textColor = Color.white;
        var r = new Rect(Screen.width / 2f - 220, Screen.height * 0.72f, 440, Mathf.Max(40, Screen.height / 22));
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.Box(r, GUIContent.none); GUI.color = Color.white;
        GUI.Label(r, prompt, st);
    }

    void OnDrawGizmos()
    {
        if (!ready) return;
        Gizmos.matrix = frame ? frame.localToWorldMatrix : Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        for (int l = 0; l < 2; l++) Gizmos.DrawWireCube(zone[l].center, zone[l].size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(carHomeF.center + Vector3.up * CarDy(), carHomeF.size);
    }
}
