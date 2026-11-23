using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds a simple base tablet model (body + screen) and both GUIs, then wires up
// TabletToggle and TabletUI for you.
//
// HOW TO USE:
//   1. Make an empty object on your player (call it "TabletSystem") and add this script.
//   2. Click the three dots (or right-click) on this script's title -> "Build Tablet".
//   3. Press Play, then press 2.
// You can click Build again any time. It replaces the old one.
// Later you can swap the cube model for a real one: just drag the new model
// into TabletToggle's "Tablet" slot.
public class TabletBuilder : MonoBehaviour
{
    [Header("Where the tablet is held (empty = Main Camera)")]
    public Transform holdParent;
    public Vector3 heldPosition = new Vector3(0.12f, -0.12f, 0.45f);
    public Vector3 heldRotation = new Vector3(-12f, 0f, 0f);

    [Header("Size in meters")]
    public Vector2 tabletSize = new Vector2(0.28f, 0.18f);
    public float thickness = 0.01f;

    const string tabletName = "Tablet (built)";
    const string overlayName = "Tablet Overlay Canvas (built)";

    [ContextMenu("Build Tablet")]
    public void Build()
    {
        Transform parent = holdParent;
        if (parent == null && Camera.main != null) parent = Camera.main.transform;
        if (parent == null)
        {
            Debug.LogError("TabletBuilder: no Hold Parent and no Main Camera found.");
            return;
        }

        // Remove the old one
        Transform old = parent.Find(tabletName);
        if (old != null) DestroyImmediate(old.gameObject);
        GameObject oldOverlay = GameObject.Find(overlayName);
        if (oldOverlay != null) DestroyImmediate(oldOverlay);

        // ---------- The tablet model ----------
        GameObject root = new GameObject(tabletName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = heldPosition;
        root.transform.localEulerAngles = heldRotation;

        // Screen faces the player, which is the -Z side of the tablet
        MakeCube("Body", root.transform, Vector3.zero,
            new Vector3(tabletSize.x, tabletSize.y, thickness),
            new Color(0.35f, 0.36f, 0.4f));

        float glassZ = -(thickness / 2f) - 0.0005f;
        MakeCube("Screen Glass", root.transform, new Vector3(0f, 0f, glassZ),
            new Vector3(tabletSize.x - 0.016f, tabletSize.y - 0.016f, 0.001f),
            new Color(0.03f, 0.03f, 0.06f));

        // ---------- GUI on the tablet's screen ----------
        float canvasWidth = 1000f;
        float canvasHeight = canvasWidth * (tabletSize.y - 0.016f) / (tabletSize.x - 0.016f);

        GameObject screenCanvasObj = new GameObject("Screen Canvas", typeof(RectTransform));
        screenCanvasObj.transform.SetParent(root.transform, false);
        Canvas screenCanvas = screenCanvasObj.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.WorldSpace;
        CanvasGroup screenGroup = screenCanvasObj.AddComponent<CanvasGroup>();
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;

        RectTransform screenRect = screenCanvasObj.GetComponent<RectTransform>();
        screenRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        screenRect.localScale = Vector3.one * ((tabletSize.x - 0.016f) / canvasWidth);
        screenRect.localPosition = new Vector3(0f, 0f, glassZ - 0.0006f);

        string[] names = { "HOME", "PAGE 2" };

        TextMeshProUGUI screenTitle;
        GameObject[] screenPages;
        BuildPanel(screenRect, names, 60f, 44f, out screenTitle, out screenPages);

        // ---------- GUI that pops up on the player's screen ----------
        GameObject overlayObj = new GameObject(overlayName, typeof(RectTransform));
        Canvas overlayCanvas = overlayObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        CanvasGroup overlayGroup = overlayObj.AddComponent<CanvasGroup>();
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        RectTransform panel = MakeRect("Panel", overlayObj.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1100f, 650f);
        panel.anchoredPosition = Vector2.zero;

        TextMeshProUGUI overlayTitle;
        GameObject[] overlayPages;
        BuildPanel(panel, names, 60f, 44f, out overlayTitle, out overlayPages);

        // ---------- Hook everything up ----------
        TabletToggle toggle = GetComponent<TabletToggle>();
        if (toggle == null) toggle = gameObject.AddComponent<TabletToggle>();
        toggle.tablet = root;

        TabletUI ui = GetComponent<TabletUI>();
        if (ui == null) ui = gameObject.AddComponent<TabletUI>();
        ui.overlayGroup = overlayGroup;
        ui.overlayTitle = overlayTitle;
        ui.overlayPages = overlayPages;
        ui.screenGroup = screenGroup;
        ui.screenTitle = screenTitle;
        ui.screenPages = screenPages;
        ui.pageNames = names;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(toggle);
        UnityEditor.EditorUtility.SetDirty(ui);
        UnityEditor.EditorUtility.SetDirty(root);
        UnityEditor.EditorUtility.SetDirty(overlayObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("TabletBuilder: tablet built. Press Play, then press 2.");
    }

    // A dark background, a title, and one placeholder object per page
    void BuildPanel(RectTransform panel, string[] pageNames, float titleSize, float bodySize,
        out TextMeshProUGUI title, out GameObject[] pages)
    {
        MakeImage("Background", panel, new Color(0.08f, 0.1f, 0.22f, 0.95f));

        title = MakeText("Title", panel, "", titleSize, TextAlignmentOptions.TopLeft, 30f);

        pages = new GameObject[pageNames.Length];
        for (int i = 0; i < pageNames.Length; i++)
        {
            RectTransform page = MakeRect("Page - " + pageNames[i], panel);
            Stretch(page, 0f);

            // Placeholder. Delete this and put your real GUI inside the page object.
            MakeText("Placeholder", page, pageNames[i] + " (empty for now)", bodySize,
                TextAlignmentOptions.Center, 0f);

            pages[i] = page.gameObject;
        }
    }

    // ---------- small helpers ----------
    GameObject MakeCube(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = pos;
        cube.transform.localScale = scale;

        // The tablet is only for show, so it shouldn't bump into anything
        DestroyImmediate(cube.GetComponent<Collider>());

        Renderer r = cube.GetComponent<Renderer>();
        Material m = new Material(r.sharedMaterial);
        m.color = color;
        r.sharedMaterial = m;
        return cube;
    }

    RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform r, float margin)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(margin, margin);
        r.offsetMax = new Vector2(-margin, -margin);
    }

    Image MakeImage(string name, Transform parent, Color color)
    {
        RectTransform r = MakeRect(name, parent);
        Stretch(r, 0f);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string text, float size,
        TextAlignmentOptions align, float margin)
    {
        RectTransform r = MakeRect(name, parent);
        Stretch(r, margin);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
