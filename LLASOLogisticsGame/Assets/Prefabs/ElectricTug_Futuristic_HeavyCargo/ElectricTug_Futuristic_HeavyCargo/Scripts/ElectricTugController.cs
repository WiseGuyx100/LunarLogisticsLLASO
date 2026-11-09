using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Driver entry, exit, and heavy physics motion controller for the Futuristic Cargo Tug.
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

    [Header("Player Mount Anchors")]
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform exitPoint;

    private Rigidbody body;
    private FPSMovement nearbyPlayer;
    private Transform playerRoot;
    private Transform previousParent;
    private CharacterController playerController;
    private Behaviour[] playerBehaviours;
    private bool[] originalEnabledStates;
    private Renderer[] playerRenderers;
    private bool[] originalRendererStates;
    private bool occupied;
    private float currentSpeed;

    public bool IsOccupied => occupied;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 5200f;
        body.linearDamping = 2.2f;
        body.angularDamping = 5.0f;
        body.centerOfMass = new Vector3(0f, -0.18f, -0.4f);
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
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame) return;

        if (occupied) ExitVehicle();
        else if (nearbyPlayer != null) EnterVehicle(nearbyPlayer);
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
        playerController = playerRoot.GetComponent<CharacterController>();
        previousParent = playerRoot.parent;

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

        playerRoot.SetParent(seatMount, false);
        playerRoot.localPosition = Vector3.zero;
        playerRoot.localRotation = Quaternion.identity;

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
            playerRoot.SetParent(previousParent, true);
            if (exitPoint != null)
            {
                playerRoot.position = exitPoint.position;
                playerRoot.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
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
        previousParent = null;
        playerBehaviours = null;
        originalEnabledStates = null;
    }
}
