using UnityEngine;
using TMPro;

public class ItemInspector : MonoBehaviour
{
    public Camera playerCamera;
    public TextMeshProUGUI infoText;
    public CanvasGroup canvasGroup;
    public float lookDistance = 4f;

    // Your pickup script sets this when you grab or drop something
    public ItemInfo heldItem;

    void Update()
    {
        ItemInfo itemToShow = null;
        float alpha = 1f;

        if (heldItem != null)
        {
            itemToShow = heldItem;
            alpha = 0.5f;
        }
        else
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
            {
                itemToShow = hit.collider.GetComponentInParent<ItemInfo>();
            }
        }

        if (itemToShow != null)
        {
            ItemData d = InventoryDatabase.Instance.Get(itemToShow.itemId);

            if (d != null)
            {
                float weight = d.mass * 1.62f; // Moon gravity. Use 9.81f for Earth.

                infoText.text =
                    d.itemName + "  (" + d.id + ")\n" +
                    "Category: " + d.category + "\n" +
                    "Mass: " + d.mass + " kg   Weight: " + weight.ToString("0.#") + " N   Volume: " + d.volume + " m3\n" +
                    "Storage: " + d.storage + "   Temp: " + d.temperature + "\n" +
                    "Zone: " + d.requiredZone + "   Access: " + d.access;
            }
            else
            {
                infoText.text = "Unknown item ID: " + itemToShow.itemId;
            }
        }

        canvasGroup.alpha = itemToShow != null ? alpha : 0f;
    }
}