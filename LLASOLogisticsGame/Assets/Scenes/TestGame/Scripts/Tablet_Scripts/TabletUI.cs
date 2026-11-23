using System;
using TMPro;
using UnityEngine;

// The tablet's two GUIs:
//   1) a pop-up on the player's screen (shows while the tablet is out)
//   2) the screen on the tablet model itself
// Both use the same pages, so they always match.
// Nothing real is on the pages yet. Put your content inside the page objects later.
public class TabletUI : MonoBehaviour
{
    // Other scripts can listen to this (gives the new page number)
    public static event Action<int> PageChanged;

    [Header("Pop-up on the player's screen")]
    public CanvasGroup overlayGroup;
    public TextMeshProUGUI overlayTitle;
    public GameObject[] overlayPages;      // one object per page

    [Header("Screen on the tablet model")]
    public CanvasGroup screenGroup;
    public TextMeshProUGUI screenTitle;
    public GameObject[] screenPages;       // one object per page

    [Header("Pages")]
    public string[] pageNames = { "HOME", "PAGE 2" };
    public float fadeSpeed = 6f;           // higher = fades faster

    private int pageIndex = 0;
    public int CurrentPage { get { return pageIndex; } }

    void Start()
    {
        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (screenGroup != null) screenGroup.alpha = 0f;
        ShowPage(0);
    }

    void Update()
    {
        // Fade both GUIs in when the tablet is out, and out when it is away
        float target = TabletToggle.IsOut ? 1f : 0f;
        Fade(overlayGroup, target);
        Fade(screenGroup, target);

        // These buttons only work while the tablet is out
        if (TabletControls.SwitchAppPressed()) NextPage();
        if (TabletControls.NextPressed()) NextPage();
        if (TabletControls.PreviousPressed()) PreviousPage();
    }

    void Fade(CanvasGroup group, float target)
    {
        if (group == null) return;
        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
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
