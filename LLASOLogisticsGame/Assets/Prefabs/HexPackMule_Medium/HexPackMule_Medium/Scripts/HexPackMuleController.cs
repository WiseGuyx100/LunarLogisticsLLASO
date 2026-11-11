using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CyberHex Pack Mule FORKLIFT (medium vehicle).
///  F      : step on / off the operator platform (stand next to it)
///  W/S    : drive,  A/D : steer
///  Z (tap): scoop cycle.
///             1st tap  -> the Pac-Man hex scoop lowers to the floor and opens its jaws
///                         (drive forward into the boxes)
///             2nd tap  -> the jaws CHOMP shut, the scoop lifts, tips back and swallows everything into the hex bin,
///                         then returns to its stowed position. Repeat as often as you like.
///  Z (hold ~0.6 s): unload. The scoop lowers, the bin tilts and everything slides back out through the scoop
///                   (reverse slowly to lay the boxes down). Hold Z again to level the bin and stow the scoop.
/// Z works while you are mounted OR standing next to the mule.
/// While the scoop is not stowed the mule is speed-limited so you can't ram your cargo around.
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
    [Tooltip("Speed multiplier while the scoop is lowered / working.")]
    [SerializeField, Range(0.1f, 1f)] private float scoopSpeedFactor = 0.4f;

    [Header("Operator Mount")]
    [Tooltip("Where the player's root sits while standing on the platform. Move this up/down to change eye height.")]
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform exitPoint;

    [Header("Scoop (assigned by the builder)")]
    [SerializeField] private Transform scoopCarriage;
    [SerializeField] private Transform scoopTilt;
    [SerializeField] private Transform scoopLid;
    [SerializeField] private Transform binTilt;
    [Tooltip("Vertical travel (negative = down) that puts the scoop on the floor.")]
    [SerializeField] private float scoopLowerDistance = -0.675f;
    [SerializeField, Range(10f, 80f)] private float jawOpenAngle = 50f;
    [SerializeField, Range(0f, 15f)] private float jawLoadAngle = 6f;
    [SerializeField, Range(10f, 35f)] private float loadTiltAngle = 28f;
    [SerializeField, Range(0f, 10f)] private float stowTiltAngle = 5f;
    [SerializeField, Range(5f, 18f)] private float unloadBinAngle = 18f;
    [SerializeField, Range(0.2f, 2f)] private float animationSpeed = 1.0f;
    [SerializeField, Min(0.2f)] private float holdToUnloadSeconds = 0.6f;

    [Header("Visual Rig (assigned by the builder)")]
    [SerializeField] private Transform[] wheelSpinPivots;
    [SerializeField] private Transform[] frontSteerPivots;
    [SerializeField] private Transform steeringYoke;
    [SerializeField] private Light[] headlights;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.22f;
    [SerializeField, Range(0f, 60f)] private float maxWheelSteerAngle = 30f;
    [SerializeField, Range(0f, 180f)] private float maxYokeRoll = 70f;

    // ---- scoop pose + tiny step sequencer ----
    private struct ScoopPose
    {
        public float lift;   // carriage Y offset
        public float tilt;   // scoop pitch (negative = front up)
        public float jaw;    // lid opening (0 = shut)
        public float bin;    // bin tilt (positive = rear up)
    }

    private struct Step
    {
        public ScoopPose target;
        public float duration;
    }

    private enum ScoopState { Stowed, Lowered, Unloading }

    private ScoopPose pose;
    private ScoopPose stepStart;
    private readonly List<Step> steps = new List<Step>();
    private int stepIndex = -1;
    private float stepTime;
    private ScoopState scoopState = ScoopState.Stowed;
    private ScoopState pendingState = ScoopState.Stowed;

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
        pose = StowPose();
        ApplyPose();
    }

    // ------------------------------------------------------------------ poses
    private ScoopPose StowPose() { return new ScoopPose { lift = 0f, tilt = -stowTiltAngle, jaw = 0f, bin = 0f }; }

    private void QueueStep(float lift, float tilt, float jaw, float bin, float duration)
    {
        steps.Add(new Step { target = new ScoopPose { lift = lift, tilt = tilt, jaw = jaw, bin = bin }, duration = duration });
    }

    private void StartSequence(ScoopState finalState)
    {
        pendingState = finalState;
        stepIndex = 0;
        stepTime = 0f;
        stepStart = pose;
    }

    // Lowering: level the scoop, drop straight down (jaws shut = collision-free), then open wide.
    private void QueueLower(float finalJaw, float finalTilt)
    {
        QueueStep(0f, 0f, 0f, 0f, 0.4f);
        QueueStep(scoopLowerDistance, 0f, 0f, 0f, 1.1f);
        QueueStep(scoopLowerDistance, finalTilt, finalJaw, 0f, 0.7f);
    }

    private void TapZ()
    {
        if (Busy || scoopState == ScoopState.Unloading) return;
        steps.Clear();

        if (scoopState == ScoopState.Stowed)
        {
            QueueLower(jawOpenAngle, 0f);
            StartSequence(ScoopState.Lowered);
        }
        else // Lowered -> CHOMP, lift, tip into the bin, stow
        {
            QueueStep(scoopLowerDistance, 0f, 0f, 0f, 0.6f);                         // chomp shut
            QueueStep(0f, 0f, 0f, 0f, 1.3f);                                         // lift
            QueueStep(0f, -loadTiltAngle, jawLoadAngle, 0f, 1.2f);                   // tip back into the bin
            QueueStep(0f, -loadTiltAngle, jawLoadAngle, 0f, 0.9f);                   // let everything slide in
            QueueStep(0f, -stowTiltAngle, 0f, 0f, 1.1f);                             // stow
            StartSequence(ScoopState.Stowed);
        }
    }

    private void HoldZ()
    {
        if (Busy) return;
        steps.Clear();

        if (scoopState != ScoopState.Unloading)
        {
            if (scoopState == ScoopState.Stowed) QueueLower(25f, 4f);
            else QueueStep(scoopLowerDistance, 4f, 25f, 0f, 0.8f);
            QueueStep(scoopLowerDistance, 4f, 25f, unloadBinAngle, 1.6f);           // bin tips, cargo slides out through the scoop
            StartSequence(ScoopState.Unloading);
        }
        else
        {
            QueueStep(scoopLowerDistance, 4f, 25f, 0f, 1.4f);                       // level the bin
            QueueStep(scoopLowerDistance, 0f, 0f, 0f, 0.7f);                        // shut jaws
            QueueStep(0f, 0f, 0f, 0f, 1.3f);                                        // lift
            QueueStep(0f, -stowTiltAngle, 0f, 0f, 0.4f);                            // stow
            StartSequence(ScoopState.Stowed);
        }
    }

    private void UpdateSequence()
    {
        if (stepIndex < 0) return;
        Step s = steps[stepIndex];
        stepTime += Time.deltaTime * animationSpeed;
        float t = Mathf.Clamp01(stepTime / Mathf.Max(0.01f, s.duration));
        float e = t * t * (3f - 2f * t);
        pose.lift = Mathf.Lerp(stepStart.lift, s.target.lift, e);
        pose.tilt = Mathf.Lerp(stepStart.tilt, s.target.tilt, e);
        pose.jaw = Mathf.Lerp(stepStart.jaw, s.target.jaw, e);
        pose.bin = Mathf.Lerp(stepStart.bin, s.target.bin, e);
        ApplyPose();

        if (t >= 1f)
        {
            pose = s.target;
            stepStart = pose;
            stepTime = 0f;
            stepIndex++;
            if (stepIndex >= steps.Count)
            {
                stepIndex = -1;
                steps.Clear();
                scoopState = pendingState;
            }
        }
    }

    private void ApplyPose()
    {
        if (scoopCarriage != null) scoopCarriage.localPosition = new Vector3(0f, pose.lift, 0f);
        if (scoopTilt != null) scoopTilt.localRotation = Quaternion.Euler(pose.tilt, 0f, 0f);
        if (scoopLid != null) scoopLid.localRotation = Quaternion.Euler(-pose.jaw, 0f, 0f);
        if (binTilt != null) binTilt.localRotation = Quaternion.Euler(pose.bin, 0f, 0f);
    }

    // ------------------------------------------------------------------ update
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

            // Z: tap = scoop cycle, hold = unload / stow
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

        float limit = (scoopState == ScoopState.Stowed && !Busy) ? 1f : scoopSpeedFactor;
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

    // ------------------------------------------------------------------ player enter / exit
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
