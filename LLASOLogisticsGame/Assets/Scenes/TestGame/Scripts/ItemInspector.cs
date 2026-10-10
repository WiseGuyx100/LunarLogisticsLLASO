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

    [Header("Scanner sound")]
    public AudioClip scanSound;                    // leave empty to use the built-in beep
    [Range(0f, 1f)] public float scanVolume = 0.8f;

    private AudioSource audioSource;

    // The box whose label is on the screen right now
    private ItemInfo shownItem;

    void Start()
    {
        canvasGroup.alpha = 0f;

        // Get a speaker for the beep (it makes one if there isn't one)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 0 = plain sound, not 3D

        // If you didn't give it a sound file, make a beep
        if (scanSound == null) scanSound = MakeBeep();
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

            // Only beep when Q really found a box
            if (target != null)
            {
                shownItem = target;
                ShowLabel(target);
                audioSource.PlayOneShot(scanSound, scanVolume);
            }
        }
    }

    void ShowLabel(ItemInfo item)
    {
        ItemData d = InventoryDatabase.Instance.Get(item.itemId);

        if (d != null)
        {
            float weight = d.mass * 1.62f; // Moon gravity. Use 9.81f for Earth.

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

    // Makes a short "bee-boop" scanner sound with code
    AudioClip MakeBeep()
    {
        int rate = 44100;
        int samples = (int)(rate * 0.18f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float pitch = t < 0.08f ? 1800f : 2400f;      // low tone, then high tone
            float fade = 1f - (float)i / samples;         // gets quieter at the end
            data[i] = Mathf.Sin(2f * Mathf.PI * pitch * t) * 0.4f * fade;
        }

        AudioClip clip = AudioClip.Create("ScanBeep", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
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