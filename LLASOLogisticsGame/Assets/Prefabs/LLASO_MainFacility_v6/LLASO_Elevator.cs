// LLASO working elevator. Drop this file anywhere in Assets/ - no setup, no components to add.
// It finds the objects named LLASO_Elevator_Car / LLASO_Elevator_Door_* inside the facility at scene start.
//   Walk up to a door and press E to call the car.  Inside the car press E to ride to the other floor.
//   The car carries the player (CharacterController, Rigidbody or plain transform).
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LLASO_Elevator : MonoBehaviour
{
    // ---- tweak here ----
    const KeyCode LegacyKey = KeyCode.E;
#if ENABLE_INPUT_SYSTEM
    const Key NewKey = Key.E;
#endif
    public float floorHeight = 4.35f;   // level 1 -> level 2
    public float speed       = 1.8f;    // metres / second
    public float doorTime    = 0.8f;    // seconds to open or close
    public float useRange    = 2.6f;    // how close you must stand to a door
    public float autoClose   = 6f;      // doors close by themselves after this long with nobody near

    Transform car; Vector3 carHome;
    Transform[][] doors = new Transform[2][];       // [level][A,B]
    Vector3[][]  doorHome = new Vector3[2][];
    Vector3[]    doorDir  = new Vector3[2];         // slide direction of panel A (B is the opposite)
    float[]      doorSlide = new float[2];
    float[]      open = new float[2];               // 0 closed .. 1 open
    Bounds carLocal;                                 // car mesh bounds in car-local space
    int level = 0; bool busy; float idleTimer;
    Transform player; CharacterController cc; Rigidbody prb;
    string prompt = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (GameObject.Find("LLASO_Elevator_Car") == null || FindFirstObjectByType<LLASO_Elevator>() != null) return;
        new GameObject("LLASO_ElevatorController").AddComponent<LLASO_Elevator>();
    }

    void Start()
    {
        car = GameObject.Find("LLASO_Elevator_Car").transform; carHome = car.localPosition;
        var mf = car.GetComponent<MeshFilter>(); carLocal = mf ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        for (int l = 0; l < 2; l++)
        {
            string lv = l == 0 ? "L1" : "L2";
            var a = GameObject.Find($"LLASO_Elevator_Door_{lv}_A"); var b = GameObject.Find($"LLASO_Elevator_Door_{lv}_B");
            if (!a || !b) { Debug.LogWarning("LLASO_Elevator: door objects missing."); enabled = false; return; }
            doors[l] = new[] { a.transform, b.transform };
            doorHome[l] = new[] { a.transform.localPosition, b.transform.localPosition };
            var ma = a.GetComponent<MeshFilter>().sharedMesh.bounds; var mb = b.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 d = (a.transform.localPosition + ma.center) - (b.transform.localPosition + mb.center); d.y = 0; d.z = 0;   // A slides away from B along local X
            doorDir[l] = d.x >= 0 ? Vector3.right : Vector3.left;
            doorSlide[l] = ma.size.x * 0.9f;                                   // slides into the wall
            Solidify(a); Solidify(b);
        }
        Solidify(car.gameObject);
        open[0] = 1f; ApplyDoors();                                            // start with the ground doors open
        FindPlayer();
    }

    static void Solidify(GameObject g)
    {
        if (!g.GetComponent<Collider>()) g.AddComponent<MeshCollider>();
        var rb = g.GetComponent<Rigidbody>() ?? g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
    }

    void FindPlayer()
    {
        GameObject p = null;
        try { p = GameObject.FindWithTag("Player"); } catch { }
        if (!p) p = GameObject.Find("Player");
        if (!p) return;
        player = p.transform; cc = p.GetComponentInChildren<CharacterController>(); prb = p.GetComponentInChildren<Rigidbody>();
    }

    Vector3 Feet()
    {
        var col = player.GetComponentInChildren<Collider>();
        return col ? new Vector3(col.bounds.center.x, col.bounds.min.y, col.bounds.center.z) : player.position;
    }
    bool PlayerInCar()
    {
        Vector3 l = car.InverseTransformPoint(Feet()); var b = carLocal;
        return l.x > b.min.x && l.x < b.max.x && l.z > b.min.z && l.z < b.max.z && l.y > b.min.y - 0.3f && l.y < b.max.y;
    }
    bool PlayerAtDoor(int lv)
    {
        Vector3 c = doors[lv][0].GetComponent<Renderer>().bounds.center, f = Feet();
        Vector3 d = f - c; float dy = d.y; d.y = 0;
        return d.magnitude < useRange && Mathf.Abs(dy + 1.1f) < 2.2f;
    }

    bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyKey)) return true;
#endif
        return false;
    }

    void Update()
    {
        if (!player) { FindPlayer(); return; }
        prompt = "";
        bool inCar = PlayerInCar(), near0 = PlayerAtDoor(0), near1 = PlayerAtDoor(1);
        if (!busy)
        {
            if (inCar) prompt = $"[E] Go to level {(level == 0 ? 2 : 1)}";
            else if (near0 && (level != 0 || open[0] < 1f)) prompt = "[E] Call elevator";
            else if (near1 && (level != 1 || open[1] < 1f)) prompt = "[E] Call elevator";
            if (Pressed())
            {
                if (inCar) StartCoroutine(Go(1 - level));
                else if (near0) StartCoroutine(Go(0));
                else if (near1) StartCoroutine(Go(1));
            }
            idleTimer = (inCar || near0 || near1) ? 0f : idleTimer + Time.deltaTime;
            if (idleTimer > autoClose && open[level] > 0f) StartCoroutine(CloseOnly());
        }
        else prompt = "";
    }

    IEnumerator CloseOnly() { busy = true; yield return Slide(level, 0f); busy = false; }

    IEnumerator Go(int target)
    {
        busy = true;
        if (target == level) { yield return Slide(level, 1f); busy = false; yield break; }
        yield return Slide(level, 0f);                                  // close
        float y0 = carHome.y + level * floorHeight, y1 = carHome.y + target * floorHeight;
        float dist = Mathf.Abs(y1 - y0), t = 0f;
        while (t < 1f)                                                  // travel (ease in/out)
        {
            t += Time.deltaTime * speed / dist;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Vector3 before = car.position;
            var lp = car.localPosition; lp.y = Mathf.Lerp(y0, y1, k); car.localPosition = lp;
            Vector3 delta = car.position - before;
            if (player && PlayerInCar()) Carry(delta);
            yield return null;
        }
        level = target;
        yield return Slide(level, 1f);                                  // open
        busy = false;
    }

    void Carry(Vector3 delta)
    {
        if (cc) cc.Move(delta);
        else if (prb) { prb.position += delta; prb.linearVelocity = new Vector3(prb.linearVelocity.x, 0f, prb.linearVelocity.z); }
        else player.position += delta;
    }

    IEnumerator Slide(int lv, float target)
    {
        float start = open[lv];
        if (Mathf.Approximately(start, target)) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / doorTime;
            open[lv] = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            ApplyDoors(); yield return null;
        }
        open[lv] = target; ApplyDoors();
    }

    void ApplyDoors()
    {
        for (int l = 0; l < 2; l++)
        {
            if (doors[l] == null) continue;
            Vector3 off = doorDir[l] * doorSlide[l] * open[l];
            doors[l][0].localPosition = doorHome[l][0] + off;
            doors[l][1].localPosition = doorHome[l][1] - off;
        }
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(prompt)) return;
        var st = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        st.normal.textColor = Color.white;
        var r = new Rect(Screen.width / 2f - 200, Screen.height * 0.72f, 400, 40);
        GUI.color = new Color(0, 0, 0, 0.55f); GUI.Box(r, GUIContent.none); GUI.color = Color.white;
        GUI.Label(r, prompt, st);
    }
}
