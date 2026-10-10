using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonPickup : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float pickupDistance = 3f;

    [Header("Holding")]
    [SerializeField] private float followSpeed = 15f;   // how fast the item chases the hold point
    [SerializeField] private float maxHoldDistance = 2f; // if the item gets stuck this far away, it drops

    private InputAction interactAction;
    private Rigidbody heldBody;
    private bool originalUseGravity;
    private RigidbodyConstraints originalConstraints;
    private RigidbodyInterpolation originalInterpolation;
    private CollisionDetectionMode originalCollisionMode;
    private Collider[] heldColliders;
    private Collider[] playerColliders;
    private ItemInspector inspector;
    private GridContainer grid;

    private void Awake()
    {
        interactAction = new InputAction(
            "Interact",
            InputActionType.Button,
            "<Keyboard>/e");

        inspector = FindFirstObjectByType<ItemInspector>();
        grid = FindFirstObjectByType<GridContainer>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
        DropObject();
    }

    private void Update()
    {
        if (interactAction.WasPressedThisFrame())
        {
            if (heldBody == null)
            {
                TryPickupObject();
            }
            else
            {
                DropObject();
            }
        }
    }

    private void FixedUpdate()
    {
        if (heldBody == null)
        {
            return;
        }

        // Where your hands want the box to be
        Vector3 targetPos = holdPoint.position;

        // Stuck behind a wall? Let go.
        if ((targetPos - heldBody.position).magnitude > maxHoldDistance)
        {
            DropObject();
            return;
        }

        // Inside the container? Then snap the target to the grid.
        if (grid != null && grid.IsInside(targetPos))
        {
            // Straighten the box to the nearest 90 degrees
            float y = Mathf.Round(heldBody.rotation.eulerAngles.y / 90f) * 90f;
            heldBody.rotation = Quaternion.Euler(0f, y, 0f);
            Physics.SyncTransforms();

            // Measure how big the box is
            Bounds b = heldColliders[0].bounds;
            foreach (Collider c in heldColliders)
            {
                b.Encapsulate(c.bounds);
            }

            // Use the snapped spot instead of the hand spot
            targetPos = grid.Snap(targetPos, b.size);
        }

        // Move with physics so walls and floors can block it.
        Vector3 toTarget = targetPos - heldBody.position;
        heldBody.linearVelocity = toTarget * followSpeed;
        heldBody.angularVelocity = Vector3.zero;
    }

    private void TryPickupObject()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (holdPoint == null)
        {
            Debug.LogError("Pickup check: Hold Point is empty.");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, pickupDistance);

        bool foundHit = false;
        RaycastHit hit = default;

        foreach (RaycastHit possibleHit in hits)
        {
            Transform hitTransform = possibleHit.collider.transform;

            // Ignore the player and objects attached beneath the player.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (!foundHit || possibleHit.distance < hit.distance)
            {
                hit = possibleHit;
                foundHit = true;
            }
        }

        if (!foundHit)
        {
            Debug.Log("Pickup check: No object found in front of the camera.");
            return;
        }

        Debug.Log("Pickup check: Camera hit " + hit.collider.name);

        Rigidbody targetBody = hit.collider.attachedRigidbody;

        if (targetBody == null)
        {
            Debug.Log("Pickup check: The object hit does not have a Rigidbody.");
            return;
        }

        heldBody = targetBody;

        // Remember the original settings so we can put them back.
        originalUseGravity = heldBody.useGravity;
        originalConstraints = heldBody.constraints;
        originalInterpolation = heldBody.interpolation;
        originalCollisionMode = heldBody.collisionDetectionMode;

        // Stay a normal physics object (NOT kinematic) so it hits walls.
        heldBody.useGravity = false;
        heldBody.constraints = RigidbodyConstraints.FreezeRotation;
        heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // The item should not bump into the player.
        heldColliders = heldBody.GetComponentsInChildren<Collider>();
        SetIgnorePlayerCollision(true);

        Debug.Log("Pickup check: Picked up " + heldBody.name);

        if (inspector != null)
        {
            inspector.heldItem = heldBody.GetComponentInParent<ItemInfo>();
        }
    }

    private void DropObject()
    {
        if (heldBody == null)
        {
            return;
        }

        SetIgnorePlayerCollision(false);
        // Snap to the grid if the box is inside the container
        if (grid != null && grid.IsInside(heldBody.position))
        {
            // Straighten it to the nearest 90 degrees first
            float y = Mathf.Round(heldBody.rotation.eulerAngles.y / 90f) * 90f;
            heldBody.rotation = Quaternion.Euler(0f, y, 0f);
            Physics.SyncTransforms();

            // Measure how big the box is
            Bounds b = heldColliders[0].bounds;
            foreach (Collider c in heldColliders)
            {
                b.Encapsulate(c.bounds);
            }

            // Snap using the box size
            heldBody.position = grid.Snap(heldBody.position, b.size);
        }

        heldBody.linearVelocity = Vector3.zero;
        heldBody.useGravity = originalUseGravity;
        heldBody.constraints = originalConstraints;
        heldBody.interpolation = originalInterpolation;
        heldBody.collisionDetectionMode = originalCollisionMode;

        if (inspector != null)
        {
            inspector.heldItem = null;
        }

        heldBody = null;
        heldColliders = null;
    }

    private void SetIgnorePlayerCollision(bool ignore)
    {
        if (heldColliders == null)
        {
            return;
        }

        foreach (Collider held in heldColliders)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                Physics.IgnoreCollision(held, playerCollider, ignore);
            }
        }
    }
}