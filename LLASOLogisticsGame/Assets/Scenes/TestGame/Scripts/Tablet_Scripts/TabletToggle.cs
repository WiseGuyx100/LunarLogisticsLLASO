using System;
using UnityEngine;

// Pulls the tablet up and down (key 2). Works like ScannerToggle.
// Other scripts can check  TabletToggle.IsOut  to know if the tablet is out.
public class TabletToggle : MonoBehaviour
{
    // True while the tablet is out. Tablet-only buttons check this.
    public static bool IsOut { get; private set; }

    // Other scripts can listen to these (example: TabletToggle.TabletOpened += MyMethod;)
    public static event Action TabletOpened;
    public static event Action TabletClosed;

    [Header("Drag your tablet model here")]
    public GameObject tablet;

    [Header("Does the tablet start out showing?")]
    public bool startOut = false;

    [Header("Animation")]
    public float hideDistance = 0.6f;  // how far down it slides
    public float slideSpeed = 8f;      // higher = faster

    private Vector3 outPosition;
    private Vector3 hiddenPosition;

    void OnDisable()
    {
        IsOut = false;   // so the tablet-only buttons never get stuck "on"
    }

    void Start()
    {
        if (tablet == null)
        {
            Debug.LogError("TabletToggle: drag the tablet model into the Tablet slot.");
            enabled = false;
            return;
        }

        // Remember where you placed the tablet in the editor
        outPosition = tablet.transform.localPosition;
        hiddenPosition = outPosition + Vector3.down * hideDistance;

        IsOut = startOut;

        if (IsOut)
        {
            tablet.transform.localPosition = outPosition;
            tablet.SetActive(true);
        }
        else
        {
            tablet.transform.localPosition = hiddenPosition;
            tablet.SetActive(false);
        }
    }

    void Update()
    {
        // Don't react while the player is typing in the AI chat box
        bool typing = AIHelper.IsChatOpen;

        if (!typing && TabletControls.TogglePressed())
        {
            SetOut(!IsOut);
        }

        // Slide toward where it should be
        Vector3 target = IsOut ? outPosition : hiddenPosition;

        tablet.transform.localPosition = Vector3.Lerp(
            tablet.transform.localPosition,
            target,
            slideSpeed * Time.deltaTime);

        // Once it has slid all the way down, hide it
        if (!IsOut &&
            Vector3.Distance(tablet.transform.localPosition, hiddenPosition) < 0.01f)
        {
            tablet.SetActive(false);
        }
    }

    // Other scripts can also call this: FindFirstObjectByType<TabletToggle>().SetOut(false);
    public void SetOut(bool value)
    {
        if (IsOut == value) return;

        IsOut = value;

        if (IsOut)
        {
            tablet.SetActive(true);   // show it right away so you can see it slide up
            if (TabletOpened != null) TabletOpened();
        }
        else
        {
            if (TabletClosed != null) TabletClosed();
        }
    }
}
