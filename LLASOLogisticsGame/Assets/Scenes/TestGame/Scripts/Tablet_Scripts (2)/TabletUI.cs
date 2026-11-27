using System;
using System.Globalization;
using TMPro;
using UnityEngine;

// Runs the tablet's two screens:
//   1) the full-screen pop-up (opens with an animation when the tablet comes out)
//   2) the screen on the tablet model itself
// TabletBuilder creates and fills in all of these slots for you.
public class TabletUI : MonoBehaviour
{
    // Other scripts can listen to this (gives the new page number)
    public static event Action<int> PageChanged;

    [Header("Full-screen pop-up")]
    public Canvas overlayCanvas;
    public CanvasGroup overlayGroup;
    public RectTransform overlayWindow;     // the part that animates
    public TextMeshProUGUI overlayTitle;
    public GameObject[] overlayPages;       // one object per page
    public TabletTileFx[] overlayTiles;     // the four app buttons

    [Header("Screen on the tablet model")]
    public CanvasGroup screenGroup;
    public TextMeshProUGUI screenTitle;
    public GameObject[] screenPages;        // one object per page
    public TabletTileFx[] screenTiles;      // the four app buttons

    [Header("Status bar (one entry per screen)")]
    public TextMeshProUGUI[] clockTexts;
    public TextMeshProUGUI[] batteryTexts;
    public RectTransform[] batteryFills;
    [Range(0f, 1f)] public float batteryLevel = 1f;

    [Header("Pages")]
    public string[] pageNames = { "HOME" };

    [Header("Opening animation")]
    public float openDuration = 0.35f;      // seconds
    public float closedScale = 0.92f;       // starts a little small and grows
    public float riseDistance = 70f;        // starts a little low and rises

    private int pageIndex = 0;
    private int hoverIndex = -1;            // which button is highlighted (-1 = none)
    private float openAmount = 0f;          // 0 = closed, 1 = fully open
    private float nextStatusUpdate = 0f;

    public int CurrentPage { get { return pageIndex; } }
    public int HoverIndex { get { return hoverIndex; } }

    void Start()
    {
        if (screenGroup != null) screenGroup.alpha = 1f;   // the model's screen is always "on"

        ApplyAnimation();
        ShowPage(0);
        UpdateStatusBar();
    }

    void Update()
    {
        // Open / close animation
        float target = TabletToggle.IsOut ? 1f : 0f;
        openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, openDuration));
        ApplyAnimation();

        // Clock and battery
        if (Time.unscaledTime >= nextStatusUpdate)
        {
            nextStatusUpdate = Time.unscaledTime + 1f;
            UpdateStatusBar();
        }

        // These buttons only work while the tablet is out
        if (TabletControls.SwitchAppPressed()) NextPage();
        if (TabletControls.NextPressed()) NextPage();
        if (TabletControls.PreviousPressed()) PreviousPage();

        // Arrow keys move the highlight between the buttons
        if (TabletControls.NavRightPressed() || TabletControls.NavDownPressed()) MoveHover(1);
        if (TabletControls.NavLeftPressed() || TabletControls.NavUpPressed()) MoveHover(-1);

        // Start fresh with nothing highlighted whenever the tablet is put away
        if (!TabletToggle.IsOut && hoverIndex != -1) SetHover(-1);
    }

    // The first arrow press highlights the first button, then it moves from there
    void MoveHover(int step)
    {
        int count = ButtonCount();
        if (count == 0) return;

        if (hoverIndex < 0) SetHover(0);
        else SetHover(Mathf.Clamp(hoverIndex + step, 0, count - 1));
    }

    public void SetHover(int index)
    {
        hoverIndex = index;
        ApplyHover(overlayTiles);
        ApplyHover(screenTiles);
    }

    int ButtonCount()
    {
        if (overlayTiles != null && overlayTiles.Length > 0) return overlayTiles.Length;
        if (screenTiles != null) return screenTiles.Length;
        return 0;
    }

    void ApplyHover(TabletTileFx[] tiles)
    {
        if (tiles == null) return;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null) tiles[i].SetHovered(i == hoverIndex);
        }
    }

    void ApplyAnimation()
    {
        float e = openAmount * openAmount * (3f - 2f * openAmount);   // smooth start and end

        if (overlayGroup != null) overlayGroup.alpha = e;

        if (overlayWindow != null)
        {
            overlayWindow.localScale = Vector3.one * Mathf.Lerp(closedScale, 1f, e);
            overlayWindow.anchoredPosition = new Vector2(0f, Mathf.Lerp(-riseDistance, 0f, e));
        }

        // Fully switched off while closed, so it costs nothing and blocks nothing
        if (overlayCanvas != null) overlayCanvas.enabled = openAmount > 0f;
    }

    // Date/time shows your computer's clock for now.
    // To show your game's clock instead, replace DateTime.Now below.
    void UpdateStatusBar()
    {
        string clock = DateTime.Now.ToString("ddd, MMM d  -  h:mm tt", CultureInfo.InvariantCulture);
        string battery = Mathf.RoundToInt(batteryLevel * 100f) + "%";

        if (clockTexts != null)
        {
            for (int i = 0; i < clockTexts.Length; i++)
                if (clockTexts[i] != null) clockTexts[i].text = clock;
        }

        if (batteryTexts != null)
        {
            for (int i = 0; i < batteryTexts.Length; i++)
                if (batteryTexts[i] != null) batteryTexts[i].text = battery;
        }

        if (batteryFills != null)
        {
            for (int i = 0; i < batteryFills.Length; i++)
                if (batteryFills[i] != null) batteryFills[i].anchorMax = new Vector2(batteryLevel, 1f);
        }
    }

    public void NextPage()
    {
        ShowPage((pageIndex + 1) % PageCount());
    }

    public void PreviousPage()
    {
        ShowPage((pageIndex - 1 + PageCount()) % PageCount());
    }

    public void ShowPage(int index)
    {
        pageIndex = Mathf.Clamp(index, 0, PageCount() - 1);

        SetActivePage(overlayPages);
        SetActivePage(screenPages);

        string title = pageIndex < pageNames.Length ? pageNames[pageIndex] : "";
        if (overlayTitle != null) overlayTitle.text = title;
        if (screenTitle != null) screenTitle.text = title;

        if (PageChanged != null) PageChanged(pageIndex);
    }

    int PageCount()
    {
        return Mathf.Max(1, pageNames.Length);
    }

    // Turns on the object for the current page, turns the others off
    void SetActivePage(GameObject[] pages)
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == pageIndex);
        }
    }
}
