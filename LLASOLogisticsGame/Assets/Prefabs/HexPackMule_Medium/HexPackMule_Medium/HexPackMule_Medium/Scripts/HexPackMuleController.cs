using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CyberHex Pack Mule (medium vehicle).
///  F  : step on / step off the operator platform (works only when you are standing next to it)
///  W/S: drive, A/D: steer
///  Z  : lower / raise the hex glass lift-gate ramp (works while mounted OR when you stand next to the mule),
///       so boxes can be rolled straight on and off. The ramp closes itself when you drive away quickly.
/// The player is carried standing on the platform with a clear view over the see-through tray.
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

    [Header("Operator Mount")]
    [Tooltip("Where the player's root sits while standing on the platform. Move this up/down to change eye height.")]
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform exitPoint;

    [Header("Lift-Gate Ramp (Z)")]
    [SerializeField] private Transform rampPivot;
    [SerializeField, Range(90f, 140f)] private float rampOpenAngle = 125f;
    [SerializeField, Min(1f)] private float rampSpeed = 5.0f;
    [SerializeField, Min(0f)] private float autoCloseSpeed = 1.5f;

    [Header("Visual Rig (assigned by the builder)")]
    [SerializeField] private Transform[] wheelSpinPivots;
    [SerializeField] private Transform[] frontSteerPivots;
    [SerializeField] private Transform steeringYoke;
    [SerializeField] private Light[] headlights;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.22f;
    [SerializeField, Range(0f, 60f)] private float maxWheelSteerAngle = 30f;
    [SerializeField, Range(0f, 180f)] private float maxYokeRoll = 70f;

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
    private bool rampOpen;
    private float currentSpeed;

    private bool PlayerIsNear => nearbyPlayer != null && Time.time - nearbyTime < 0.25f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

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

            if (keyboard.zKey.wasPressedThisFrame && (occupied || PlayerIsNear))
                rampOpen = !rampOpen;

            if (occupied)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
            }
        }

        // Ramp closes itself when the mule drives off quickly so nothing drags on the ground.
        if (rampOpen && occupied && Mathf.Abs(currentSpeed) > autoCloseSpeed) rampOpen = false;

        if (rampPivot != null)
        {
            Quaternion target = rampOpen ? Quaternion.Euler(rampOpenAngle, 0f, 0f) : Quaternion.identity;
            rampPivot.localRotation = Quaternion.Slerp(rampPivot.localRotation, target, rampSpeed * Time.deltaTime);
        }

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

        float targetSpeed = throttle >= 0f ? throttle * forwardSpeed : throttle * reverseSpeed;
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
