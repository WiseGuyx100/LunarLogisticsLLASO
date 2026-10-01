using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    public string itemName = "Box";
    public float massKg = 5f;
    public float weightN = 49f;
    public float volumeL = 20f;
    [TextArea] public string description = "A storage box.";
}