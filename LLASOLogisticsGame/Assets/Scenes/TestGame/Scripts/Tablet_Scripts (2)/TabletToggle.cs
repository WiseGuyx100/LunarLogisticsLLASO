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

    [Header("Your tablet model (empty = finds 'Tablet (built)' by itself)")]
    public GameObject tablet;

    [Header("Does the tablet start out showing?")]
    public bool startOut = false;

    [Header("Animation")]
    public float hideDistance = 0.6f;  // how far down it slides
    public float slideSpeed = 8f;      // higher = faster

    const string builtTabletName = "Tablet (built)";

    private static TabletToggle active;   // only one TabletToggle is allowed
    private Vector3 outPosition;
    private Vector3 hiddenPosition;
    private bool ready = false;
    private bool warned = false;
    private float nextSearchTime = 0f;

    void Awake()
    {
        // Two of these at once would cancel each other out (both react to the same key press)
        if (active != null && active != this)
        {
            Debug.LogWarning("TabletToggle: there is more than one TabletToggle in the scene. " +
                "Turning off the extra one on '" + name + "'. Delete it so only one is left.");
            enabled = false;
            return;
        }

        active = this;
        IsOut = false;

        // Hide the tablet right now, before the first frame is drawn
        Setup();
    }

    void OnDisable()
    {
        if (active == this)
        {
            IsOut = false;   // so the tablet-only buttons never get stuck "on"
            active = null;
        }
    }

    // Finds the tablet, remembers where it sits, and puts it in its starting state.
    // Returns false if the tablet can't be found yet (it keeps trying).
    bool Setup()
    {
        if (ready) return true;

        if (tablet == null) tablet = FindTablet();
        if (tablet == null) return false;

        // Remember where you placed the tablet in the editor
        outPosition = tablet.transform.localPosition;
        hiddenPosition = outPosition + Vector3.down * hideDistance;

        IsOut = startOut;
        tablet.transform.localPosition = startOut ? outPosition : hiddenPosition;
        tablet.SetActive(startOut);

        ready = true;
        return true;
    }

    GameObject FindTablet()
    {
        // 1) under the main camera  2) anywhere in this object's hierarchy
        Transform found = null;

        if (Camera.main != null) found = Camera.main.transform.Find(builtTabletName);

        if (found == null)
        {
            Transform[] all = transform.root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == builtTabletName) { found = all[i]; break; }
            }
        }

        return found != null ? found.gameObject : null;
    }

    void Update()
    {
        if (!ready)
        {
            // Look again about once a second (the player might spawn in a moment later)
            if (Time.unscaledTime < nextSearchTime) return;
            nextSearchTime = Time.unscaledTime + 1f;

            if (!Setup())
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogError("TabletToggle: can't find the tablet model. Run TabletBuilder > Build Tablet, " +
                        "or drag the model into the Tablet slot.");
                }
                return;
            }
        }

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
        if (!ready || IsOut == value) return;

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
