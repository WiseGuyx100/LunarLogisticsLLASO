using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ItemInspector : MonoBehaviour
{
    public Camera playerCamera;
    public TextMeshProUGUI infoText;
    public CanvasGroup canvasGroup;
    public float lookDistance = 4f;

    // Your pickup script sets this when you grab or drop something
    public ItemInfo heldItem;

    // The box whose label is on the screen right now
    private ItemInfo shownItem;

    void Start()
    {
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (QWasPressed())
        {
            ItemInfo target = null;

            if (heldItem != null)
            {
                target = heldItem;
            }
            else
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
                {
                    target = hit.collider.GetComponentInParent<ItemInfo>();
                }
            }

            if (target != null)
            {
                shownItem = target;
                ShowLabel(target);
            }
        }
    }

    void ShowLabel(ItemInfo item)
    {
        ItemData d = InventoryDatabase.Instance.Get(item.itemId);

        if (d != null)
        {
            float weight = d.mass * 1.62f; // Moon gravity. Use 9.81f for Earth.

            // Only show the power line for boxes that use power
            string power = "";
            if (d.powerW > 0)
            {
                power = "   Power: " + d.powerW + " W";
            }

            infoText.text =
                d.itemName + "  (" + d.id + ")\n" +
                "Category: " + d.category + "   Qty needed: " + d.qty + "   Day: " + d.missionDay + "\n" +
                "Mass: " + d.mass + " kg   Weight: " + weight.ToString("0.#") + " N   Volume: " + d.volume + " m3\n" +
                "Storage: " + d.storage + "   Temp: " + d.temperature + "\n" +
                "Zone: " + d.requiredZone + "   Access: " + d.access + "\n" +
                "Fragility: " + d.fragility + "   Hazard: " + d.hazard + "\n" +
                "Container: " + d.container + power;
        }
        else
        {
            infoText.text = "Unknown item ID: " + item.itemId;
        }

        canvasGroup.alpha = 1f;
    }

    bool QWasPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }
}