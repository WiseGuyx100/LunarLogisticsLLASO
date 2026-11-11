using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CyberHex Gorilla Cart (small, hand-pulled).
///  F : grab / release the glowing hex handle while you stand near the cart.
///      While held the cart trails behind you like a wagon (you keep walking normally, so your view is never blocked).
///  Z : dump! The glass tub tilts and the glass gate swings open so cargo slides out
///      (works while holding the handle or when standing next to the cart).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class HexGorillaCartController : MonoBehaviour
{
    [Header("Hand Pulling")]
    [SerializeField] private Transform handleGrip;
    [SerializeField, Min(0.5f)] private float maxSpeed = 6.0f;
    [SerializeField, Min(0.1f)] private float pushSpeed = 1.6f;
    [SerializeField, Min(0.5f)] private float followGain = 4.0f;
    [SerializeField, Min(1f)] private float acceleration = 14.0f;
    [SerializeField, Min(10f)] private float turnRate = 260f;
    [SerializeField, Min(0f)] private float lateralGrip = 14.0f;
    [Tooltip("Where the hand holds the handle relative to the player (right / back), in metres.")]
    [SerializeField] private float handRight = 0.35f;
    [SerializeField] private float handBack = 0.25f;
    [SerializeField, Min(1f)] private float autoReleaseDistance = 6.0f;

    [Header("Dump (Z)")]
    [SerializeField] private Transform dumpPivot;
    [SerializeField] private Transform gatePivot;
    [SerializeField, Range(20f, 75f)] private float dumpAngle = 55f;
    [SerializeField, Range(45f, 110f)] private float gateOpenAngle = 90f;
    [SerializeField, Min(1f)] private float dumpSpeed = 3.5f;

    [Header("Visual Rig (assigned by the builder)")]
    [SerializeField] private Transform[] wheelSpinPivots;
    [SerializeField, Min(0.05f)] private float wheelRadius = 0.20f;

    private Rigidbody body;
    private FPSMovement nearbyPlayer;
    private float nearbyTime = -10f;
    private Transform playerRoot;
    private CharacterController playerController;
    private readonly List<Collider> ownColliders = new List<Collider>();
    private bool grabbed;
    private bool dumpOpen;
    private float currentSpeed;
    private float gripDistance;
    private float wheelSpin;

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
        if (keyboard != null)
        {
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (grabbed) Release();
                else if (PlayerIsNear) Grab(nearbyPlayer);
            }
            if (keyboard.zKey.wasPressedThisFrame && (grabbed || PlayerIsNear))
                dumpOpen = !dumpOpen;
        }

        if (dumpPivot != null)
        {
            Quaternion tilt = dumpOpen ? Quaternion.Euler(dumpAngle, 0f, 0f) : Quaternion.identity;
            dumpPivot.localRotation = Quaternion.Slerp(dumpPivot.localRotation, tilt, dumpSpeed * Time.deltaTime);
        }
        if (gatePivot != null)
        {
            // the gate only opens once the tub has started to tilt
            bool open = dumpOpen && dumpPivot != null && Quaternion.Angle(dumpPivot.localRotation, Quaternion.identity) > dumpAngle * 0.25f;
            Quaternion g = open ? Quaternion.Euler(gateOpenAngle, 0f, 0f) : Quaternion.identity;
            gatePivot.localRotation = Quaternion.Slerp(gatePivot.localRotation, g, dumpSpeed * 1.6f * Time.deltaTime);
        }

        if (wheelSpinPivots != null && body != null)
        {
            float fwd = transform.InverseTransformDirection(body.linearVelocity).z;
            wheelSpin = Mathf.Repeat(wheelSpin + fwd / wheelRadius * Mathf.Rad2Deg * Time.deltaTime, 360f);
            Quaternion spin = Quaternion.Euler(wheelSpin, 0f, 0f);
            for (int i = 0; i < wheelSpinPivots.Length; i++)
                if (wheelSpinPivots[i] != null) wheelSpinPivots[i].localRotation = spin;
        }
    }

    private void FixedUpdate()
    {
        if (!grabbed) return;
        if (playerRoot == null) { Release(); return; }

        // Point where the player's hand holds the handle (beside / slightly behind the player => out of the camera's view).
        Vector3 hand = playerRoot.position + playerRoot.right * handRight - playerRoot.forward * handBack;
        Vector3 toHand = hand - body.position;
        toHand.y = 0f;
        float dist = toHand.magnitude;
        if (dist > autoReleaseDistance) { Release(); return; }

        // 1) swing the cart so its handle end points at the hand
        float yaw = body.rotation.eulerAngles.y;
        if (dist > 0.05f)
        {
            float targetYaw = Mathf.Atan2(toHand.x, toHand.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, turnRate * Time.fixedDeltaTime);
            body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        // 2) roll toward the hand until the handle is the right distance away (and gently back off if pushed)
        float error = dist - gripDistance;
        float targetSpeed = Mathf.Clamp(error * followGain, -pushSpeed, maxSpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 v = body.linearVelocity;
        Vector3 planar = forward * currentSpeed;
        // kill sideways slide like real wheels
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        float lateral = Vector3.Dot(new Vector3(v.x, 0f, v.z), side);
        lateral = Mathf.MoveTowards(lateral, 0f, lateralGrip * Time.fixedDeltaTime);
        body.linearVelocity = new Vector3(planar.x + side.x * lateral, v.y, planar.z + side.z * lateral);
    }

    private void OnTriggerEnter(Collider other) => CacheNearbyPlayer(other);
    private void OnTriggerStay(Collider other) => CacheNearbyPlayer(other);

    private void CacheNearbyPlayer(Collider other)
    {
        if (grabbed) return;
        FPSMovement movement = other.GetComponentInParent<FPSMovement>();
        if (movement == null) return;
        nearbyPlayer = movement;
        nearbyTime = Time.time;
    }

    private void Grab(FPSMovement movement)
    {
        if (grabbed || movement == null) return;
        playerRoot = movement.transform;
        playerController = playerRoot.GetComponent<CharacterController>();

        // The cart must never shove the player around while it is being pulled.
        ownColliders.Clear();
        GetComponentsInChildren(false, ownColliders);
        if (playerController != null)
            foreach (Collider c in ownColliders)
                if (c != null && !c.isTrigger) Physics.IgnoreCollision(playerController, c, true);

        Vector3 flatGrip = (handleGrip != null ? handleGrip.position : transform.position + transform.forward * 1.65f) - body.position;
        flatGrip.y = 0f;
        gripDistance = flatGrip.magnitude;

        grabbed = true;
        nearbyPlayer = null;
        currentSpeed = 0f;
    }

    private void Release()
    {
        if (playerController != null)
            foreach (Collider c in ownColliders)
                if (c != null && !c.isTrigger) Physics.IgnoreCollision(playerController, c, false);

        ownColliders.Clear();
        grabbed = false;
        currentSpeed = 0f;
        playerRoot = null;
        playerController = null;
    }

    private void OnDisable()
    {
        if (grabbed) Release();
    }
}
