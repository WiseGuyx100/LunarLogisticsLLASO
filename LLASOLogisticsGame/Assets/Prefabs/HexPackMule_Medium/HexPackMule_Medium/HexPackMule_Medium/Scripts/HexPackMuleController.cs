using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CyberHex Pack Mule CLAW CRANE (medium vehicle).
///  F      : step on / off the operator platform (stand next to it)
///  W/S    : drive,  A/D : steer
///  Z (tap): load cycle (works while mounted OR when you stand next to the mule)
///             1st tap -> the boom lowers the hex claw to the floor and the two jaws swing open SIDEWAYS.
///                        There is no floor while open, so nothing gets shoved away: drive up to the boxes.
///             2nd tap -> the jaws CHOMP shut (their halves form the floor), a magnet field pulls the boxes in,
///                        the boom lifts and swings the claw up and over the bin, the jaws open and the boxes drop in.
///  Z (hold ~0.6 s): unload. The claw swings over the bin, grabs the boxes out of it, carries them to the floor in front
///                   of the mule, opens and sets them down.
/// Boxes are CARRIED by the claw (kinematic while held), so nothing depends on friction or on boxes rolling.
/// While the claw is away from its parked position the mule is speed-limited.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class HexPackMuleController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField, Min(0.1f)] private float forwardSpeed = 5.0f;
    [SerializeField, Min(0.1f)] private float reverseSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float acceleration = 5.5f;
    [SerializeField, Min(0.1f)] private float braking = 10.0f;
    [SerializeField, Min(1f)] private float steeringSpeed = 85f;
    [SerializeField, Min(0f)] private float lateralGrip = 10.0f;
    [Tooltip("Speed multiplier while the claw is lowered / working.")]
    [SerializeField, Range(0.1f, 1f)] private float clawSpeedFactor = 0.4f;

    [Header("Operator Mount")]
    [Tooltip("Where the player's root sits while standing on the platform. Move this up/down to change eye height.")]
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform exitPoint;

    [Header("Claw Crane (assigned by the builder)")]
    [SerializeField] private Transform boomPivot;
    [SerializeField] private Transform tipFrame;
    [SerializeField] private Transform jawPivotR;
    [SerializeField] private Transform jawPivotL;
    [SerializeField] private Transform clawRoot;
    [Tooltip("Boom angle (degrees, + = up) when parked / when the claw sits on the floor / when it hangs over the bin.")]
    [SerializeField] private float boomStowAngle = -44.4f;
    [SerializeField] private float boomDownAngle = -60.4f;
    [SerializeField, Range(150f, 175f)] private float boomOverAngle = 165f;
    [SerializeField, Range(30f, 80f)] private float jawOpenAngle = 65f;
    [Tooltip("Jaw opening used to drop boxes into the bin (must stay small so the jaws clear the bin).")]
    [SerializeField, Range(10f, 35f)] private float jawDropAngle = 30f;
    [SerializeField, Range(0.2f, 2f)] private float animationSpeed = 1.0f;
    [SerializeField, Min(0.2f)] private float holdToUnloadSeconds = 0.6f;

    [Header("Magnet / Carrying")]
    [Tooltip("Only objects on these layers can be picked up. Best fix: put your cargo boxes on their own layer and select it here.")]
    [SerializeField] private LayerMask carryLayers = ~0;
    [Tooltip("If not empty, ONLY objects with this tag are picked up (e.g. your cargo box tag).")]
    [SerializeField] private string requiredTag = "";
    [Tooltip("Anything whose name, parent name or component type contains one of these words is never picked up (vehicles etc).")]
    [SerializeField] private string[] ignoreKeywords = { "Tug", "Cart", "Mule", "Vehicle", "Truck", "Forklift", "Player" };
    [Tooltip("Objects larger than this in any direction are treated as vehicles/scenery and ignored.")]
    [SerializeField, Min(0.1f)] private float maxCarrySize = 1.3f;
    [Tooltip("Objects heavier than this are ignored (0 = no limit).")]
    [SerializeField, Min(0f)] private float maxCarryMass = 0f;
    [Tooltip("Objects with more colliders than this are assumed to be vehicles and ignored.")]
    [SerializeField, Range(1, 20)] private int maxCollidersPerObject = 6;
    [SerializeField, Range(1, 12)] private int maxCarried = 6;
    [SerializeField, Min(0.1f)] private float pullSeconds = 0.7f;
    [Tooltip("Floor pickup zone in claw space (centre / half size).")]
    [SerializeField] private Vector3 groundZoneCenter = new Vector3(0f, 0f, 0.4f);
    [SerializeField] private Vector3 groundZoneHalfSize = new Vector3(0.85f, 0.45f, 0.6f);
    [Tooltip("Bin pickup zone in claw space (reaches down into the bin).")]
    [SerializeField] private Vector3 binZoneCenter = new Vector3(0f, -0.85f, 0.2f);
    [SerializeField] private Vector3 binZoneHalfSize = new Vector3(0.85f, 0.6f, 0.9f);

    [Header("Visual Rig (assigned by the builder)")]
    [SerializeField] private Transform[] wheelSpinPivots;
    [SerializeField] private Transform[] frontSteerPivots;
    [SerializeField] private Transform steeringYoke;
    [SerializeField] private Light[] headlights;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.22f;
    [SerializeField, Range(0f, 60f)] private float maxWheelSteerAngle = 30f;
    [SerializeField, Range(0f, 180f)] private float maxYokeRoll = 70f;

    // ---------------------------------------------------------------- crane pose + step sequencer
    private struct ClawPose
    {
        public float boom;   // boom angle, + = up
        public float jaw;    // jaw opening in degrees (0 = shut)
    }

    private enum ClawEvent { None, CaptureGround, CaptureBin, Release }

    private struct Step
    {
        public ClawPose target;
        public float duration;
        public ClawEvent atStart;
        public ClawEvent atEnd;
    }

    private enum ClawState { Stowed, Lowered }

    private sealed class Held
    {
        public Rigidbody rb;
        public Vector3 startPos;
        public Quaternion startRot;
        public Vector3 slotLocal;
        public float t;
    }

    private ClawPose pose;
    private ClawPose stepStart;
    private readonly List<Step> steps = new List<Step>();
    private int stepIndex = -1;
    private float stepTime;
    private bool stepStarted;
    private ClawState clawState = ClawState.Stowed;
    private ClawState pendingState = ClawState.Stowed;
    private readonly List<Held> held = new List<Held>();

    private float zHeldTime;
    private bool zHoldHandled;

    private Rigidbody body;
    private float wheelSpin;
    private float visualSteer;
    private FPSMovement nearbyPlayer;
    private float nearbyTime = -10f;
    private Transform playerRoot;
    private CharacterController playerController;
    private Behaviour[] playerBehaviours;
    private bool[] originalEnabledStates;
    private Renderer[] playerRenderers;
    private bool[] originalRendererStates;
    private Vector3 originalPlayerScale = Vector3.one;
    private bool occupied;
    private float currentSpeed;

    private bool PlayerIsNear => nearbyPlayer != null && Time.time - nearbyTime < 0.25f;
    private bool Busy => stepIndex >= 0;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        pose = new ClawPose { boom = boomStowAngle, jaw = 0f };
        ApplyPose();
    }

    // ---------------------------------------------------------------- sequences
    private void QueueStep(float boom, float jaw, float duration, ClawEvent atStart = ClawEvent.None, ClawEvent atEnd = ClawEvent.None)
    {
        steps.Add(new Step { target = new ClawPose { boom = boom, jaw = jaw }, duration = duration, atStart = atStart, atEnd = atEnd });
    }

    private void StartSequence(ClawState finalState)
    {
        pendingState = finalState;
        stepIndex = 0;
        stepTime = 0f;
        stepStarted = false;
        stepStart = pose;
    }

    private void TapZ()
    {
        if (Busy) return;
        steps.Clear();

        if (clawState == ClawState.Stowed)
        {
            // lower to the floor, then swing the jaws open sideways
            QueueStep(boomDownAngle, 0f, 2.2f);
            QueueStep(boomDownAngle, jawOpenAngle, 0.9f);
            StartSequence(ClawState.Lowered);
        }
        else
        {
            QueueStep(boomDownAngle, 0f, 0.9f, ClawEvent.None, ClawEvent.CaptureGround);         // CHOMP + magnet
            QueueStep(boomDownAngle, 0f, pullSeconds + 0.1f);                                     // boxes slide in
            QueueStep(boomOverAngle, 0f, 3.4f);                                                   // lift + swing over the bin
            QueueStep(boomOverAngle, jawDropAngle, 0.8f, ClawEvent.None, ClawEvent.Release);      // open, then let go: boxes drop in
            QueueStep(boomOverAngle, jawDropAngle, 0.9f);                                         // let them fall
            QueueStep(boomOverAngle, 0f, 0.7f);                                                   // close
            QueueStep(boomStowAngle, 0f, 3.0f);                                                   // park
            StartSequence(ClawState.Stowed);
        }
    }

    private void HoldZ()
    {
        if (Busy) return;
        steps.Clear();

        if (clawState == ClawState.Lowered) QueueStep(boomDownAngle, 0f, 0.8f);                  // close the jaws first
        QueueStep(boomOverAngle, 0f, 3.4f);                                                       // swing over the bin
        QueueStep(boomOverAngle, 0f, 0.5f, ClawEvent.None, ClawEvent.CaptureBin);                 // magnet pulls boxes up
        QueueStep(boomOverAngle, 0f, pullSeconds + 0.2f);
        QueueStep(boomDownAngle, 0f, 3.4f);                                                       // carry them to the floor
        QueueStep(boomDownAngle, jawOpenAngle, 0.9f, ClawEvent.None, ClawEvent.Release);          // open, then set them down
        QueueStep(boomDownAngle, jawOpenAngle, 0.8f);
        QueueStep(boomDownAngle, 0f, 0.7f);
        QueueStep(boomStowAngle, 0f, 2.2f);
        StartSequence(ClawState.Stowed);
    }

    private void UpdateSequence()
    {
        if (stepIndex < 0) return;
        Step s = steps[stepIndex];

        if (!stepStarted)
        {
            stepStarted = true;
            RunEvent(s.atStart);
        }

        stepTime += Time.deltaTime * animationSpeed;
        float t = Mathf.Clamp01(stepTime / Mathf.Max(0.01f, s.duration));
        float e = t * t * (3f - 2f * t);
        pose.boom = Mathf.Lerp(stepStart.boom, s.target.boom, e);
        pose.jaw = Mathf.Lerp(stepStart.jaw, s.target.jaw, e);
        ApplyPose();

        if (t >= 1f)
        {
            pose = s.target;
            stepStart = pose;
            RunEvent(s.atEnd);
            stepTime = 0f;
            stepStarted = false;
            stepIndex++;
            if (stepIndex >= steps.Count)
            {
                stepIndex = -1;
                steps.Clear();
                clawState = pendingState;
            }
        }
    }

    private void ApplyPose()
    {
        if (boomPivot != null) boomPivot.localRotation = Quaternion.Euler(-pose.boom, 0f, 0f);
        if (tipFrame != null) tipFrame.localRotation = Quaternion.Euler(pose.boom, 0f, 0f);   // keeps the claw level
        if (jawPivotR != null) jawPivotR.localRotation = Quaternion.Euler(0f, pose.jaw, 0f);
        if (jawPivotL != null) jawPivotL.localRotation = Quaternion.Euler(0f, -pose.jaw, 0f);
    }

    // ---------------------------------------------------------------- magnet / carrying
    private void RunEvent(ClawEvent evt)
    {
        switch (evt)
        {
            case ClawEvent.CaptureGround: Capture(groundZoneCenter, groundZoneHalfSize); break;
            case ClawEvent.CaptureBin: Capture(binZoneCenter, binZoneHalfSize); break;
            case ClawEvent.Release: ReleaseAll(); break;
        }
    }

    private void Capture(Vector3 centerLocal, Vector3 halfSize)
    {
        if (clawRoot == null) return;
        ReleaseAll();

        Vector3 center = clawRoot.TransformPoint(centerLocal);
        Collider[] hits = Physics.OverlapBox(center, halfSize, clawRoot.rotation, carryLayers, QueryTriggerInteraction.Ignore);
        List<Rigidbody> found = new List<Rigidbody>();
        foreach (Collider c in hits)
        {
            Rigidbody r = c.attachedRigidbody;
            if (r == null || r == body || r.isKinematic) continue;
            if (found.Contains(r)) continue;
            if (!IsCarryable(r, center, halfSize)) continue;
            found.Add(r);
        }
        found.Sort((a, b) => (a.position - center).sqrMagnitude.CompareTo((b.position - center).sqrMagnitude));

        int count = Mathf.Min(found.Count, maxCarried);
        for (int i = 0; i < count; i++)
        {
            Rigidbody r = found[i];
            Held h = new Held
            {
                rb = r,
                startPos = r.position,
                startRot = r.rotation,
                slotLocal = SlotFor(i),
                t = 0f
            };
            r.linearVelocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;
            r.isKinematic = true;
            held.Add(h);
        }
    }

    /// <summary>Strict filter so the claw only grabs cargo (never vehicles, the player, scenery or anything huge).</summary>
    private bool IsCarryable(Rigidbody r, Vector3 zoneCenterWorld, Vector3 zoneHalfSize)
    {
        if (r.transform.IsChildOf(transform) || transform.IsChildOf(r.transform)) return false;     // ourselves
        if (r.GetComponentInParent<FPSMovement>() != null) return false;                              // the player
        if (r.GetComponentInParent<CharacterController>() != null) return false;
        if (r.GetComponentInChildren<WheelCollider>() != null) return false;

        if (!string.IsNullOrEmpty(requiredTag) && !r.CompareTag(requiredTag)) return false;
        if (maxCarryMass > 0f && r.mass > maxCarryMass) return false;

        // vehicles carry controller scripts / have lots of colliders / are big
        Collider[] cols = r.GetComponentsInChildren<Collider>();
        int solid = 0;
        Bounds bounds = new Bounds(r.position, Vector3.zero);
        bool first = true;
        foreach (Collider c in cols)
        {
            if (c == null || c.isTrigger) continue;
            solid++;
            if (first) { bounds = c.bounds; first = false; } else bounds.Encapsulate(c.bounds);
        }
        if (solid == 0 || solid > maxCollidersPerObject) return false;
        Vector3 size = bounds.size;
        if (size.x > maxCarrySize || size.y > maxCarrySize || size.z > maxCarrySize) return false;

        // keyword filter on object names and on attached script type names
        if (ignoreKeywords != null && ignoreKeywords.Length > 0)
        {
            for (Transform tr = r.transform; tr != null; tr = tr.parent)
                if (ContainsKeyword(tr.name)) return false;
            foreach (MonoBehaviour mb in r.GetComponentsInParent<MonoBehaviour>())
                if (mb != null && ContainsKeyword(mb.GetType().Name)) return false;
        }

        // the box must really be INSIDE the pickup zone, not just touching its edge
        Vector3 local = clawRoot != null ? clawRoot.InverseTransformPoint(r.worldCenterOfMass) : Vector3.zero;
        Vector3 zoneLocal = clawRoot != null ? clawRoot.InverseTransformPoint(zoneCenterWorld) : Vector3.zero;
        Vector3 d = local - zoneLocal;
        if (Mathf.Abs(d.x) > zoneHalfSize.x || Mathf.Abs(d.y) > zoneHalfSize.y || Mathf.Abs(d.z) > zoneHalfSize.z) return false;
        return true;
    }

    private bool ContainsKeyword(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int i = 0; i < ignoreKeywords.Length; i++)
        {
            string k = ignoreKeywords[i];
            if (!string.IsNullOrEmpty(k) && text.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    // a tidy grid of positions inside the claw (2 x 2 per layer)
    private static Vector3 SlotFor(int i)
    {
        float x = ((i % 2) - 0.5f) * 0.42f;
        float z = 0.40f + (((i / 2) % 2) - 0.5f) * 0.40f;
        float y = -0.10f + (i / 4) * 0.40f;
        return new Vector3(x, y, z);
    }

    private void ReleaseAll()
    {
        for (int i = 0; i < held.Count; i++)
        {
            Rigidbody r = held[i].rb;
            if (r == null) continue;
            r.isKinematic = false;
            r.linearVelocity = body != null ? body.linearVelocity : Vector3.zero;
            r.angularVelocity = Vector3.zero;
            r.WakeUp();
        }
        held.Clear();
    }

    private void CarryHeldBoxes()
    {
        if (held.Count == 0 || clawRoot == null) return;
        for (int i = 0; i < held.Count; i++)
        {
            Held h = held[i];
            if (h.rb == null) continue;
            h.t = Mathf.Min(1f, h.t + Time.deltaTime / Mathf.Max(0.05f, pullSeconds));
            float e = h.t * h.t * (3f - 2f * h.t);
            Vector3 targetPos = clawRoot.TransformPoint(h.slotLocal);
            Quaternion targetRot = clawRoot.rotation;
            Vector3 pos = Vector3.Lerp(h.startPos, targetPos, e);
            Quaternion rot = Quaternion.Slerp(h.startRot, targetRot, e);
            h.rb.position = pos;
            h.rb.rotation = rot;
            h.rb.transform.SetPositionAndRotation(pos, rot);
        }
    }

    // ---------------------------------------------------------------- update
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        float steerInput = 0f;
        if (keyboard != null)
        {
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (occupied) ExitVehicle();
                else if (PlayerIsNear) EnterVehicle(nearbyPlayer);
            }

            // Z: tap = load cycle, hold = unload
            bool canUse = occupied || PlayerIsNear;
            if (keyboard.zKey.wasPressedThisFrame) { zHeldTime = 0f; zHoldHandled = false; }
            if (keyboard.zKey.isPressed && canUse && !zHoldHandled)
            {
                zHeldTime += Time.deltaTime;
                if (zHeldTime >= holdToUnloadSeconds) { zHoldHandled = true; HoldZ(); }
            }
            if (keyboard.zKey.wasReleasedThisFrame)
            {
                if (!zHoldHandled && canUse && zHeldTime < holdToUnloadSeconds) TapZ();
                zHeldTime = 0f;
            }

            if (occupied)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
            }
        }

        UpdateSequence();
        AnimateRig(steerInput);
    }

    private void AnimateRig(float steerInput)
    {
        float forwardVelocity = transform.InverseTransformDirection(body.linearVelocity).z;
        wheelSpin = Mathf.Repeat(wheelSpin + forwardVelocity / wheelRadius * Mathf.Rad2Deg * Time.deltaTime, 360f);
        visualSteer = Mathf.MoveTowards(visualSteer, steerInput * maxWheelSteerAngle, 160f * Time.deltaTime);

        if (wheelSpinPivots != null)
        {
            Quaternion spin = Quaternion.Euler(wheelSpin, 0f, 0f);
            for (int i = 0; i < wheelSpinPivots.Length; i++)
                if (wheelSpinPivots[i] != null) wheelSpinPivots[i].localRotation = spin;
        }
        if (frontSteerPivots != null)
        {
            Quaternion steer = Quaternion.Euler(0f, visualSteer, 0f);
            for (int i = 0; i < frontSteerPivots.Length; i++)
                if (frontSteerPivots[i] != null) frontSteerPivots[i].localRotation = steer;
        }
        if (steeringYoke != null && maxWheelSteerAngle > 0.01f)
            steeringYoke.localRotation = Quaternion.Euler(0f, 0f, -(visualSteer / maxWheelSteerAngle) * maxYokeRoll);
        if (headlights != null)
            for (int i = 0; i < headlights.Length; i++)
                if (headlights[i] != null && headlights[i].enabled != occupied) headlights[i].enabled = occupied;
    }

    private void LateUpdate()
    {
        if (occupied && playerRoot != null && seatMount != null)
        {
            playerRoot.position = seatMount.position;
            playerRoot.rotation = seatMount.rotation;
            playerRoot.localScale = originalPlayerScale;
        }
        CarryHeldBoxes();
    }

    private void FixedUpdate()
    {
        if (!occupied) return;

        Keyboard keyboard = Keyboard.current;
        float throttle = 0f, steering = 0f;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttle -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steering -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steering += 1f;
        }

        float limit = (clawState == ClawState.Stowed && !Busy) ? 1f : clawSpeedFactor;
        float targetSpeed = (throttle >= 0f ? throttle * forwardSpeed : throttle * reverseSpeed) * limit;
        float rate = Mathf.Abs(throttle) > 0.01f ? acceleration : braking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        Vector3 localVel = transform.InverseTransformDirection(body.linearVelocity);
        localVel.x = Mathf.MoveTowards(localVel.x, 0f, lateralGrip * Time.fixedDeltaTime);
        localVel.z = currentSpeed;
        body.linearVelocity = transform.TransformDirection(localVel);

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 0.6f);
        float reverseSign = currentSpeed < -0.05f ? -1f : 1f;
        float yaw = steering * steeringSpeed * speedFactor * reverseSign;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw * Time.fixedDeltaTime, 0f));
    }

    private void OnDisable()
    {
        ReleaseAll();
    }

    // ---------------------------------------------------------------- player enter / exit
    private void OnTriggerEnter(Collider other) => CacheNearbyPlayer(other);
    private void OnTriggerStay(Collider other) => CacheNearbyPlayer(other);

    private void CacheNearbyPlayer(Collider other)
    {
        if (occupied) return;
        FPSMovement movement = other.GetComponentInParent<FPSMovement>();
        if (movement == null) return;
        nearbyPlayer = movement;
        nearbyTime = Time.time;
    }

    private void EnterVehicle(FPSMovement movement)
    {
        if (occupied || movement == null || seatMount == null) return;

        playerRoot = movement.transform;
        originalPlayerScale = playerRoot.localScale;
        playerController = playerRoot.GetComponent<CharacterController>();

        playerBehaviours = playerRoot.GetComponents<Behaviour>();
        originalEnabledStates = new bool[playerBehaviours.Length];
        for (int i = 0; i < playerBehaviours.Length; i++)
        {
            Behaviour b = playerBehaviours[i];
            if (b == null) continue;
            originalEnabledStates[i] = b.enabled;
            if (b is FPSMovement) b.enabled = false;
        }
        if (playerController != null) playerController.enabled = false;

        // Hide the player's own body so it never blocks the camera.
        playerRenderers = playerRoot.GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null) continue;
            originalRendererStates[i] = playerRenderers[i].enabled;
            playerRenderers[i].enabled = false;
        }

        playerRoot.position = seatMount.position;
        playerRoot.rotation = seatMount.rotation;

        occupied = true;
        nearbyPlayer = null;
        currentSpeed = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitVehicle()
    {
        if (!occupied) return;

        occupied = false;
        currentSpeed = 0f;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        if (playerRoot != null)
        {
            if (exitPoint != null)
            {
                playerRoot.position = exitPoint.position;
                playerRoot.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
            playerRoot.localScale = originalPlayerScale;
        }
        if (playerController != null) playerController.enabled = true;

        if (playerBehaviours != null && originalEnabledStates != null)
        {
            int count = Mathf.Min(playerBehaviours.Length, originalEnabledStates.Length);
            for (int i = 0; i < count; i++)
                if (playerBehaviours[i] != null) playerBehaviours[i].enabled = originalEnabledStates[i];
        }
        if (playerRenderers != null && originalRendererStates != null)
        {
            int count = Mathf.Min(playerRenderers.Length, originalRendererStates.Length);
            for (int i = 0; i < count; i++)
                if (playerRenderers[i] != null) playerRenderers[i].enabled = originalRendererStates[i];
        }

        playerRenderers = null; originalRendererStates = null;
        playerRoot = null; playerController = null;
        playerBehaviours = null; originalEnabledStates = null;
    }
}
