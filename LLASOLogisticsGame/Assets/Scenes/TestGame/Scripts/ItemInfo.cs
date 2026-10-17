using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    public string itemId = "EVA-01";
    public int units = 1;         // how many items are packed in this box
    public int day = 0;           // day this box is needed (0 = emergency, 15 = any day)
    public int slotNumber = -1;   // which slot in RackBuilder's Slots list this box belongs in
}