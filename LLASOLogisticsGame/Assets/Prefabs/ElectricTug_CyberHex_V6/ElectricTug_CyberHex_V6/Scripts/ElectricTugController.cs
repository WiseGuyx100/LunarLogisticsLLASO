using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Driver entry, exit, heavy driving physics, and smooth Z-key cargo hatch trunk animation controller.
/// Preserves player scale (scaling the truck never alters player size).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class ElectricTugController : MonoBehaviour
{
    [Header("Driving Parameters")]
    [SerializeField, Min(0.1f)] private float forwardSpeed = 7.0f;
    [SerializeField, Min(0.1f)] private float reverseSpeed = 2.8f;
    [SerializeField, Min(0.1f)] private float acceleration = 6.5f;
    [SerializeField, Min(0.1f)] private float braking = 12.0f;
    [SerializeField, Min(1f)] private float steeringSpeed = 70f;
    [SerializeField, Min(0f)] private float lateralGrip = 9.0f;

    [Header("Player Mount Anchors & Animation")]
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform cargoHatchPivot;
    [SerializeField, Range(45f, 110f)] private float hatchOpenAngle = 95f;
    [SerializeField, Min(1f)] private float hatchAnimationSpeed = 8.0f;

    [Header("Visual Rig (assigned by the V7 builder, all optional)")]
    [SerializeField] private Transform[] wheelSpinPivots;
    [SerializeField] private Transform[] frontSteerPivots;
    [SerializeField] private Transform steeringYoke;
    [SerializeField] private Light[] headlights;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.37f;
    [SerializeField, Range(0f, 45f)] private float maxWheelSteerAngle = 28f;
    [SerializeField, Range(0f, 180f)] private float maxYokeRoll = 70f;

    private Rigidbody body;
    private float wheelSpin;
    private float visualSteer;
    private FPSMovement nearbyPlayer;
    private Transform playerRoot;
    private CharacterController playerController;
    private Behaviour[] playerBehaviours;
    private bool[] originalEnabledStates;
    private Renderer[] playerRenderers;
    private bool[] originalRendererStates;
    private Vector3 originalPlayerScale = Vector3.one;
    private bool occupied;
    private bool hatchOpen;
    private float currentSpeed;

    public bool IsOccupied => occupied;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 5500f;
        body.linearDamping = 2.2f;
        body.angularDamping = 5.0f;
        body.centerOfMass = new Vector3(0f, 0.30f, -0.9f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 1.5f;
    }

    private void Update()
    {
        if (!occupied && nearbyPlayer != null && Vector3.Distance(nearbyPlayer.transform.position, transform.position) > 4.5f)
        {
            nearbyPlayer = null;
        }

        Keyboard keyboard = Keyboard.current;
        float steerInput = 0f;
        if (keyboard != null)
        {
            // Enter / Exit via F key
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (occupied) ExitVehicle();
                else if (nearbyPlayer != null) EnterVehicle(nearbyPlayer);
            }

            // Toggle Cargo Trunk Hatch via Z key
            if (occupied && keyboard.zKey.wasPressedThisFrame)
            {
                hatchOpen = !hatchOpen;
            }

            if (occupied)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
            }
        }

        // Smoothly animate cargo hatch open/close rotation
        if (cargoHatchPivot != null)
        {
            float targetAngle = hatchOpen ? hatchOpenAngle : 0f;
            Quaternion targetRot = Quaternion.Euler(targetAngle, 0f, 0f);
            cargoHatchPivot.localRotation = Quaternion.Slerp(cargoHatchPivot.localRotation, targetRot, hatchAnimationSpeed * Time.deltaTime);
        }

        AnimateRig(steerInput);
    }

    /// <summary>Rolls the wheels, steers the front wheels and the yoke, and switches headlights (visual only).</summary>
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
        // Smoothly follow seat mount position/rotation while occupied, keeping world scale intact
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
        float throttle = 0f;
        float steering = 0f;

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

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 0.8f);
        float reverseSign = currentSpeed < -0.05f ? -1f : 1f;
        float yaw = steering * steeringSpeed * speedFactor * reverseSign;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw * Time.fixedDeltaTime, 0f));
    }

    private void OnTriggerEnter(Collider other) => CacheNearbyPlayer(other);
    private void OnTriggerStay(Collider other) => CacheNearbyPlayer(other);
    private void OnTriggerExit(Collider other) { }

    private void CacheNearbyPlayer(Collider other)
    {
        if (occupied || nearbyPlayer != null) return;
        FPSMovement movement = other.GetComponentInParent<FPSMovement>();
        if (movement != null) nearbyPlayer = movement;
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
                if (playerBehaviours[i] != null)
                    playerBehaviours[i].enabled = originalEnabledStates[i];
        }

        if (playerRenderers != null && originalRendererStates != null)
        {
            int count = Mathf.Min(playerRenderers.Length, originalRendererStates.Length);
            for (int i = 0; i < count; i++)
                if (playerRenderers[i] != null)
                    playerRenderers[i].enabled = originalRendererStates[i];
        }

        playerRenderers = null;
        originalRendererStates = null;
        playerRoot = null;
        playerController = null;
        playerBehaviours = null;
        originalEnabledStates = null;
    }
}
