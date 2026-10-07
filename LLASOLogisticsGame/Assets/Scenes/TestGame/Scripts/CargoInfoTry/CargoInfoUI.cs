using UnityEngine;
using TMPro;

public class CargoInfoUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text idText;
    public TMP_Text categoryText;
    public TMP_Text massText;
    public TMP_Text volumeText;
    public TMP_Text storageText;
    public TMP_Text temperatureText;
    public TMP_Text zoneText;
    public TMP_Text accessText;

    public void ShowItem(CargoData item)
    {
        nameText.text = item.itemName;
        idText.text = "ID: " + item.id;
        categoryText.text = "Category: " + item.category;
        massText.text = "Mass: " + item.mass + " kg";
        volumeText.text = "Volume: " + item.volume + " m³";
        storageText.text = "Storage: " + item.storage;
        temperatureText.text = "Temperature: " + item.temperature;
        zoneText.text = "Required Zone: " + item.requiredZone;
        accessText.text = "Access: " + item.access;
    }
}