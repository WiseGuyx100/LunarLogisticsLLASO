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
            // Holding something: show it, but faded so it isn't annoying
            itemToShow = heldItem;
            alpha = 0.5f;
        }
        else
        {
            // Not holding: check what the crosshair is looking at
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
            {
                itemToShow = hit.collider.GetComponentInParent<ItemInfo>();
            }
        }

        if (itemToShow != null)
        {
            infoText.text =
                itemToShow.itemName + "\n" +
                itemToShow.description + "\n" +
                "Mass: " + itemToShow.massKg + " kg   " +
                "Weight: " + itemToShow.weightN + " N   " +
                "Volume: " + itemToShow.volumeL + " L";
        }

        canvasGroup.alpha = itemToShow != null ? alpha : 0f;
    }
}