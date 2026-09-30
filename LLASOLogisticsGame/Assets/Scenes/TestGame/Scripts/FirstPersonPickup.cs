using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonPickup : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float pickupDistance = 3f;

    private InputAction interactAction;
    private Rigidbody heldBody;
    private Transform originalParent;
    private bool originalUseGravity;
    private bool originalIsKinematic;

    private void Awake()
    {
        interactAction = new InputAction(
            "Interact",
            InputActionType.Button,
            "<Keyboard>/e");
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
        originalParent = heldBody.transform.parent;
        originalUseGravity = heldBody.useGravity;
        originalIsKinematic = heldBody.isKinematic;

        heldBody.isKinematic = true;
        heldBody.useGravity = false;

        heldBody.transform.SetParent(holdPoint, false);
        heldBody.transform.localPosition = Vector3.zero;
        heldBody.transform.localRotation = Quaternion.identity;

        Debug.Log("Pickup check: Picked up " + heldBody.name);
    }


    private void DropObject()
    {
        if (heldBody == null)
        {
            return;
        }

        heldBody.transform.SetParent(originalParent, true);
        heldBody.useGravity = originalUseGravity;
        heldBody.isKinematic = originalIsKinematic;

        heldBody = null;
        originalParent = null;
    }
}
