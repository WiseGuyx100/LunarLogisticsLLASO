using System.Collections;
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

    [Header("Rotating")]
    [SerializeField] private float rotateSpeed = 120f;   // degrees per second while a key is held
    [SerializeField] private float settleSpeed = 360f;   // how fast it straightens inside the container

    [Header("Sliding into racks")]
    [SerializeField] private float slideTime = 0.4f;     // how long the slide takes (seconds)

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
    private StorageZone[] racks;
    private Rigidbody slidingBody;
    private bool isRotatingByKey;

    private void Awake()
    {
        interactAction = new InputAction(
            "Interact",
            InputActionType.Button,
            "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonEast");

        inspector = FindFirstObjectByType<ItemInspector>();
        grid = FindFirstObjectByType<GridContainer>();
        racks = FindObjectsByType<StorageZone>(FindObjectsSortMode.None);
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

        if (heldBody != null)
        {
            HandleRotation();
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
            // Not turning right now? Ease the box straight.
            if (!isRotatingByKey)
            {
                heldBody.rotation = Quaternion.RotateTowards(
                    heldBody.rotation,
                    SnapRotation(heldBody.rotation),
                    settleSpeed * Time.fixedDeltaTime);
                Physics.SyncTransforms();
            }

            // Use the snapped spot instead of the hand spot
            targetPos = grid.Snap(targetPos, HeldBounds().size);
        }

        // Move with physics so walls and floors can block it.
        Vector3 toTarget = targetPos - heldBody.position;
        heldBody.linearVelocity = toTarget * followSpeed;
        heldBody.angularVelocity = Vector3.zero;
    }

    // Turns the held box while the rotate buttons are held down
    private void HandleRotation()
    {
        isRotatingByKey = false;

        // Left / Right: turn the box
        float turn = 0f;
        if (GameControls.TurnRightHeld()) turn += 1f;
        if (GameControls.TurnLeftHeld()) turn -= 1f;

        // Up / Down: tip the box
        float tip = 0f;
        if (GameControls.TipUpHeld()) tip += 1f;
        if (GameControls.TipDownHeld()) tip -= 1f;

        if (turn == 0f && tip == 0f)
        {
            return;
        }

        isRotatingByKey = true;

        float step = rotateSpeed * Time.deltaTime;
        Quaternion r = heldBody.rotation;
        r = Quaternion.AngleAxis(turn * step, Vector3.up) * r;
        r = Quaternion.AngleAxis(tip * step, GetTipAxis()) * r;

        heldBody.rotation = r;
        Physics.SyncTransforms();
    }

    // The side-to-side direction of your view, snapped to the world X or Z line
    private Vector3 GetTipAxis()
    {
        Vector3 right = playerCamera.transform.right;
        right.y = 0f;

        if (Mathf.Abs(right.x) > Mathf.Abs(right.z))
        {
            return new Vector3(Mathf.Sign(right.x), 0f, 0f);
        }
        return new Vector3(0f, 0f, Mathf.Sign(right.z));
    }

    // Rounds a rotation to the nearest 90 degrees on every side
    private Quaternion SnapRotation(Quaternion rotation)
    {
        Vector3 e = rotation.eulerAngles;
        e.x = Mathf.Round(e.x / 90f) * 90f;
        e.y = Mathf.Round(e.y / 90f) * 90f;
        e.z = Mathf.Round(e.z / 90f) * 90f;
        return Quaternion.Euler(e);
    }

    // How big the held box is (all its colliders together)
    private Bounds HeldBounds()
    {
        Bounds b = heldColliders[0].bounds;
        foreach (Collider c in heldColliders)
        {
            b.Encapsulate(c.bounds);
        }
        return b;
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

        // Still sliding into a rack? Wait until it stops.
        if (targetBody == slidingBody)
        {
            return;
        }

        heldBody = targetBody;
        isRotatingByKey = false;

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

        bool docked = false;
        Vector3 dockTarget = Vector3.zero;

        // In front of a rack shelf? Then the box slides in (if it fits).
        // (Not done when the script is being switched off.)
        if (isActiveAndEnabled && racks != null && racks.Length > 0)
        {
            Quaternion originalRotation = heldBody.rotation;

            // Straighten the box first so its size is measured properly
            heldBody.rotation = SnapRotation(originalRotation);
            Physics.SyncTransforms();

            Bounds b = HeldBounds();

            foreach (StorageZone z in racks)
            {
                if (z == null) continue;

                Vector3 newCenter;
                StorageZone.DockResult result = z.TryDock(b, heldBody, out newCenter);

                if (result == StorageZone.DockResult.Docked)
                {
                    // Move the box's pivot by the same amount as its middle
                    dockTarget = heldBody.position + (newCenter - b.center);
                    docked = true;
                    break;
                }

                if (result == StorageZone.DockResult.DoesNotFit)
                {
                    if (inspector != null)
                    {
                        inspector.ShowMessage(
                            "<color=#FF5050>DOES NOT FIT</color>\n" +
                            "This box does not fit on that shelf\n(too big, or no room).");
                    }
                    break;
                }
            }

            // Not docked? Put the rotation back the way the player had it.
            if (!docked)
            {
                heldBody.rotation = originalRotation;
                Physics.SyncTransforms();
            }
        }

        // Not in a rack? Snap to the grid if the box is inside the container
        if (!docked && grid != null && grid.IsInside(heldBody.position))
        {
            // Straighten it to the nearest 90 degrees first
            heldBody.rotation = SnapRotation(heldBody.rotation);
            Physics.SyncTransforms();

            // Snap using the box size
            heldBody.position = grid.Snap(heldBody.position, HeldBounds().size);
        }

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.useGravity = originalUseGravity;
        heldBody.constraints = originalConstraints;
        heldBody.interpolation = originalInterpolation;
        heldBody.collisionDetectionMode = originalCollisionMode;

        if (inspector != null)
        {
            inspector.heldItem = null;
        }

        if (docked)
        {
            StartCoroutine(SlideRoutine(heldBody, dockTarget));
        }

        heldBody = null;
        heldColliders = null;
        isRotatingByKey = false;
    }

    // Glides the box into the rack
    private IEnumerator SlideRoutine(Rigidbody body, Vector3 target)
    {
        slidingBody = body;

        bool wasKinematic = body.isKinematic;
        body.isKinematic = true;

        Vector3 start = body.position;
        float t = 0f;

        while (t < slideTime)
        {
            if (body == null) yield break;

            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / slideTime);
            body.position = Vector3.Lerp(start, target, k);
            yield return null;
        }

        if (body != null)
        {
            body.position = target;
            body.isKinematic = wasKinematic;
        }

        slidingBody = null;
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