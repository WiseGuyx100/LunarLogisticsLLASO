using UnityEngine;
using UnityEngine.InputSystem;

public class ScannerToggle : MonoBehaviour
{
    [Header("Drag your scanner here")]
    public GameObject scanner;

    [Header("Does the scanner start out showing?")]
    public bool startEquipped = true;

    [Header("Animation")]
    public float hideDistance = 0.6f;  // how far down it slides
    public float slideSpeed = 8f;      // higher = faster

    private InputAction toggleAction;
    private bool isEquipped;
    public bool IsEquipped { get { return isEquipped; } }
    private Vector3 equippedPosition;
    private Vector3 hiddenPosition;

    void Awake()
    {
        toggleAction = new InputAction(
            "ToggleScanner",
            InputActionType.Button,
            "<Keyboard>/1");
    }

    void OnEnable()
    {
        toggleAction.Enable();
    }

    void OnDisable()
    {
        toggleAction.Disable();
    }

    void Start()
    {
        // Remember where you placed the scanner in the editor
        equippedPosition = scanner.transform.localPosition;
        hiddenPosition = equippedPosition + Vector3.down * hideDistance;

        isEquipped = startEquipped;

        if (isEquipped)
        {
            scanner.transform.localPosition = equippedPosition;
            scanner.SetActive(true);
        }
        else
        {
            scanner.transform.localPosition = hiddenPosition;
            scanner.SetActive(false);
        }
    }

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            isEquipped = !isEquipped;

            // Show it right away so you can see it slide up
            if (isEquipped)
            {
                scanner.SetActive(true);
            }
        }

        // Pick where the scanner should be heading
        Vector3 target = isEquipped ? equippedPosition : hiddenPosition;

        // Slide toward it
        scanner.transform.localPosition = Vector3.Lerp(
            scanner.transform.localPosition,
            target,
            slideSpeed * Time.deltaTime);

        // Once it has slid all the way down, hide it
        if (!isEquipped &&
            Vector3.Distance(scanner.transform.localPosition, hiddenPosition) < 0.01f)
        {
            scanner.SetActive(false);
        }
    }
}