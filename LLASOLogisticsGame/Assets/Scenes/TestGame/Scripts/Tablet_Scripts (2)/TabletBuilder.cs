using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds the tablet model, its screen, and the full-screen home screen,
// then wires up TabletToggle and TabletUI for you.
//
// HOW TO USE:
//   1. Put this script on an empty object on your player (like "TabletSystem").
//   2. Right-click the script's title -> "Build Tablet".
//   3. Press Play, then press 2.
// You can click Build again any time. It replaces the old one.
public class TabletBuilder : MonoBehaviour
{
    [Header("Where the tablet is held (empty = Main Camera)")]
    public Transform holdParent;
    public Vector3 heldPosition = new Vector3(0.12f, -0.12f, 0.45f);
    public Vector3 heldRotation = new Vector3(-12f, 0f, 0f);

    [Header("Size in meters")]
    public Vector2 tabletSize = new Vector2(0.28f, 0.18f);
    public float thickness = 0.01f;

    [Header("Home screen wallpaper (empty = finds turtles.jpg in your project)")]
    public Sprite wallpaper;

    [Header("Higher number = drawn on top of other UI")]
    public int overlaySortingOrder = 1000;

    const string tabletName = "Tablet (built)";
    const string overlayName = "Tablet Overlay Canvas (built)";

    // Colors: purple, yellow and black
    static readonly Color purple = new Color32(124, 58, 237, 255);
    static readonly Color tileColor = new Color32(24, 14, 48, 235);
    static readonly Color yellow = new Color32(255, 200, 61, 255);
    static readonly Color black = new Color32(11, 11, 16, 255);
    static readonly Color lavender = new Color32(196, 181, 253, 255);
    static readonly Color pillColor = new Color(0.04f, 0.03f, 0.08f, 0.7f);

    // What BuildHome gives back
    class HomeRefs
    {
        public GameObject page;
        public TextMeshProUGUI title;
        public TextMeshProUGUI clock;
        public TextMeshProUGUI batteryText;
        public RectTransform batteryFill;
        public TabletTileFx[] tiles;
    }

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

#if UNITY_EDITOR
        if (wallpaper == null) wallpaper = FindTurtles();
#endif
        if (wallpaper == null)
            Debug.LogWarning("TabletBuilder: no wallpaper found. Drag your turtles image into the Wallpaper slot (set it to Sprite), then Build again.");

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
            new Color(0.12f, 0.1f, 0.16f));

        float glassZ = -(thickness / 2f) - 0.0005f;
        float screenW = tabletSize.x - 0.016f;
        float screenH = tabletSize.y - 0.016f;
        MakeCube("Screen Glass", root.transform, new Vector3(0f, 0f, glassZ),
            new Vector3(screenW, screenH, 0.001f),
            new Color(0.02f, 0.02f, 0.04f));

        // ---------- Screen on the tablet model ----------
        float canvasWidth = 1920f;
        float canvasHeight = canvasWidth * screenH / screenW;

        GameObject screenCanvasObj = new GameObject("Screen Canvas", typeof(RectTransform));
        screenCanvasObj.transform.SetParent(root.transform, false);
        Canvas screenCanvas = screenCanvasObj.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.WorldSpace;
        CanvasGroup screenGroup = screenCanvasObj.AddComponent<CanvasGroup>();
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;

        RectTransform screenRect = screenCanvasObj.GetComponent<RectTransform>();
        screenRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        screenRect.localScale = Vector3.one * (screenW / canvasWidth);
        screenRect.localPosition = new Vector3(0f, 0f, glassZ - 0.0006f);

        HomeRefs screenHome = BuildHome(screenRect, 0f);

        // ---------- Full-screen pop-up (drawn on top of everything) ----------
        GameObject overlayObj = new GameObject(overlayName, typeof(RectTransform));
        Canvas overlayCanvas = overlayObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = overlaySortingOrder;
        CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup overlayGroup = overlayObj.AddComponent<CanvasGroup>();
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        RectTransform window = MakeRect("Tablet Window", overlayObj.transform);
        Stretch(window, 0f);

        HomeRefs overlayHome = BuildHome(window, 20f);

        // Hidden while you edit, so it doesn't cover your scene. TabletUI shows it in play.
        overlayCanvas.enabled = false;

        // ---------- Hook everything up ----------
        TabletToggle toggle = GetComponent<TabletToggle>();
        if (toggle == null) toggle = gameObject.AddComponent<TabletToggle>();
        toggle.tablet = root;

        TabletUI ui = GetComponent<TabletUI>();
        if (ui == null) ui = gameObject.AddComponent<TabletUI>();
        ui.overlayCanvas = overlayCanvas;
        ui.overlayGroup = overlayGroup;
        ui.overlayWindow = window;
        ui.overlayTitle = overlayHome.title;
        ui.overlayPages = new GameObject[] { overlayHome.page };
        ui.screenGroup = screenGroup;
        ui.screenTitle = screenHome.title;
        ui.screenPages = new GameObject[] { screenHome.page };
        ui.clockTexts = new TextMeshProUGUI[] { overlayHome.clock, screenHome.clock };
        ui.batteryTexts = new TextMeshProUGUI[] { overlayHome.batteryText, screenHome.batteryText };
        ui.batteryFills = new RectTransform[] { overlayHome.batteryFill, screenHome.batteryFill };
        ui.overlayTiles = overlayHome.tiles;
        ui.screenTiles = screenHome.tiles;
        ui.pageNames = new string[] { "HOME" };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(toggle);
        UnityEditor.EditorUtility.SetDirty(ui);
        UnityEditor.EditorUtility.SetDirty(root);
        UnityEditor.EditorUtility.SetDirty(overlayObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("TabletBuilder: tablet built. Press Play, then press 2.");
    }

#if UNITY_EDITOR
    // Finds turtles.jpg anywhere in the project and turns it into a Sprite
    Sprite FindTurtles()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("turtles t:Texture2D");
        if (guids.Length == 0) return null;

        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif

    // =====================================================================
    //  HOME SCREEN  (laid out for a 1920 x 1080 screen)
    // =====================================================================
    HomeRefs BuildHome(RectTransform window, float bezel)
    {
        HomeRefs h = new HomeRefs();

        // Black tablet frame
        if (bezel > 0f) MakeImage("Bezel", window, Color.black);

        // The screen itself (rounded, and everything inside is cut to its shape)
        RectTransform screen = MakeRect("Home Page", window);
        Stretch(screen, bezel);
        Image screenImg = screen.gameObject.AddComponent<Image>();
        screenImg.color = new Color32(26, 16, 51, 255);
        screenImg.raycastTarget = false;
        TabletRoundedImage screenRound = screen.gameObject.AddComponent<TabletRoundedImage>();
        screenRound.radius = 40;
        Mask mask = screen.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        h.page = screen.gameObject;

        // Wallpaper, scaled to cover the whole screen without stretching
        if (wallpaper != null)
        {
            RectTransform wp = MakeRect("Wallpaper", screen);
            Image wpImg = wp.gameObject.AddComponent<Image>();
            wpImg.sprite = wallpaper;
            wpImg.raycastTarget = false;
            AspectRatioFitter fit = wp.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = wallpaper.rect.width / wallpaper.rect.height;
        }

        // Darkens the wallpaper a little so the tiles and text stand out
        MakeImage("Scrim", screen, new Color(0.04f, 0.02f, 0.1f, 0.38f));

        // Page title (top left)
        h.title = TextPlaced("Title", screen, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(64f, -34f), new Vector2(700f, 84f), "HOME", 46f,
            TextAlignmentOptions.MidlineLeft, yellow, true);

        // Status bar (top right): date & time, then battery
        BuildBattery(screen, h);
        BuildClock(screen, h);

        // The four app buttons
        h.tiles = new TabletTileFx[]
        {
            BuildTile(screen, -600f, "Robot POV", "Live robot camera feeds", TabletTileFx.Kind.Camera),
            BuildTile(screen, -200f, "Robot Tracker", "Where robots are and what they do", TabletTileFx.Kind.Radar),
            BuildTile(screen, 200f, "Deliveries", "Incoming shipments", TabletTileFx.Kind.Package),
            BuildTile(screen, 600f, "Notepad", "Notes and reminders", TabletTileFx.Kind.Notepad)
        };

        // Hint at the bottom
        TextPlaced("Hint", screen, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 34f), new Vector2(900f, 50f), "Press 2 to put the tablet away", 26f,
            TextAlignmentOptions.Center, new Color(lavender.r, lavender.g, lavender.b, 0.85f), false);

        return h;
    }

    void BuildClock(RectTransform screen, HomeRefs h)
    {
        // sits to the left of the battery
        Image pill = ShapeAt("Date Time Pill", screen, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-262f, -28f), new Vector2(440f, 64f), pillColor, 32);

        h.clock = TextPlaced("Date Time", pill.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(420f, 64f), "", 30f, TextAlignmentOptions.Center, Color.white, false);
    }

    void BuildBattery(RectTransform screen, HomeRefs h)
    {
        Image pill = ShapeAt("Battery Pill", screen, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-32f, -28f), new Vector2(210f, 64f), pillColor, 32);

        // battery icon: outline, dark inside, yellow charge, little cap
        Shape("Battery Outline", pill.transform, new Vector2(-52f, 0f), new Vector2(52f, 28f), Color.white, 7);
        Shape("Battery Inside", pill.transform, new Vector2(-52f, 0f), new Vector2(46f, 22f), black, 5);
        Shape("Battery Cap", pill.transform, new Vector2(-23f, 0f), new Vector2(5f, 12f), Color.white, 2);

        RectTransform area = Place("Battery Fill Area", pill.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-52f, 0f), new Vector2(40f, 16f));
        RectTransform fill = MakeRect("Battery Fill", area);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = yellow;
        fillImg.raycastTarget = false;
        TabletRoundedImage fillRound = fill.gameObject.AddComponent<TabletRoundedImage>();
        fillRound.radius = 3;
        h.batteryFill = fill;

        h.batteryText = TextPlaced("Battery Percent", pill.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(110f, 64f), "100%", 30f,
            TextAlignmentOptions.Center, Color.white, false);
    }

    // One app button: purple edge, dark card, yellow icon badge, name and description
    TabletTileFx BuildTile(RectTransform screen, float x, string label, string description, TabletTileFx.Kind kind)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        RectTransform tile = Place("Button - " + label, screen, mid, mid, new Vector2(x, -40f), new Vector2(360f, 440f));

        // Soft glow behind the button (switched on when it is highlighted)
        Image glow = Shape("Glow", tile, Vector2.zero, new Vector2(392f, 472f), new Color(yellow.r, yellow.g, yellow.b, 0f), 56);

        Image edge = Shape("Edge", tile, Vector2.zero, new Vector2(360f, 440f), purple, 40);
        edge.raycastTarget = true;

        // The Button is ready for later. Add your On Click events when the apps exist.
        Button button = edge.gameObject.AddComponent<Button>();
        button.targetGraphic = edge;
        button.transition = Selectable.Transition.None;

        Image card = Shape("Card", edge.transform, Vector2.zero, new Vector2(352f, 432f), tileColor, 36);
        Image badge = Shape("Icon Badge", card.transform, new Vector2(0f, 60f), new Vector2(190f, 190f), yellow, 46);
        RectTransform holder = Place("Icon", badge.transform, mid, mid, Vector2.zero, new Vector2(190f, 190f));

        TabletTileFx fx = tile.gameObject.AddComponent<TabletTileFx>();
        fx.kind = kind;
        fx.edge = edge;
        fx.glow = glow;
        fx.normalEdge = purple;
        fx.hoverEdge = yellow;
        fx.iconHolder = holder;
        BuildIcon(kind, holder, fx);

        TextPlaced("Name", card.transform, mid, mid,
            new Vector2(0f, -85f), new Vector2(330f, 60f), label, 40f, TextAlignmentOptions.Center, Color.white, true);
        TextPlaced("Description", card.transform, mid, mid,
            new Vector2(0f, -135f), new Vector2(330f, 40f), description, 24f, TextAlignmentOptions.Center, lavender, false);

        return fx;
    }

    // Simple black icons drawn from shapes, inside the yellow badge.
    // The parts that animate on hover are handed to the TabletTileFx.
    void BuildIcon(TabletTileFx.Kind kind, Transform holder, TabletTileFx fx)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 leftMid = new Vector2(0f, 0.5f);

        switch (kind)
        {
            case TabletTileFx.Kind.Camera:
                Shape("Camera Hump", holder, new Vector2(-26f, 38f), new Vector2(40f, 16f), black, 6);
                Shape("Camera Body", holder, new Vector2(0f, -6f), new Vector2(112f, 74f), black, 14);
                Shape("Lens", holder, new Vector2(0f, -6f), new Vector2(44f, 44f), yellow, 22);
                fx.partA = Shape("Pupil", holder, new Vector2(0f, -6f), new Vector2(22f, 22f), black, 11).rectTransform;
                fx.partB = Shape("Viewfinder", holder, new Vector2(40f, 14f), new Vector2(9f, 9f), yellow, 4).rectTransform;
                break;

            case TabletTileFx.Kind.Radar:
                Shape("Ring 1", holder, Vector2.zero, new Vector2(112f, 112f), black, 56);
                Shape("Ring 2", holder, Vector2.zero, new Vector2(80f, 80f), yellow, 40);
                Shape("Ring 3", holder, Vector2.zero, new Vector2(48f, 48f), black, 24);
                fx.partB = Shape("Dot", holder, Vector2.zero, new Vector2(18f, 18f), yellow, 9).rectTransform;
                fx.partA = Shape("Ping", holder, Vector2.zero, new Vector2(112f, 112f), new Color(0f, 0f, 0f, 0f), 56).rectTransform;
                break;

            case TabletTileFx.Kind.Package:
                Shape("Box Lid", holder, new Vector2(0f, 30f), new Vector2(114f, 26f), black, 8);
                Shape("Box Body", holder, new Vector2(0f, -12f), new Vector2(96f, 62f), black, 10);
                Shape("Box Tape", holder, new Vector2(0f, 0f), new Vector2(16f, 86f), yellow, 0);
                break;

            case TabletTileFx.Kind.Notepad:
                Shape("Page", holder, Vector2.zero, new Vector2(84f, 108f), black, 12);
                fx.lines = new RectTransform[]
                {
                    ShapeAt("Line 1", holder, mid, leftMid, new Vector2(-26f, 24f), new Vector2(52f, 8f), yellow, 4).rectTransform,
                    ShapeAt("Line 2", holder, mid, leftMid, new Vector2(-26f, 4f), new Vector2(52f, 8f), yellow, 4).rectTransform,
                    ShapeAt("Line 3", holder, mid, leftMid, new Vector2(-26f, -16f), new Vector2(32f, 8f), yellow, 4).rectTransform
                };
                break;
        }
    }

    // =====================================================================
    //  small helpers
    // =====================================================================
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

    RectTransform Place(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform r = MakeRect(name, parent);
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
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

    // A rounded shape placed by its center
    Image Shape(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int radius)
    {
        return ShapeAt(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, color, radius);
    }

    // A rounded shape placed by any anchor
    Image ShapeAt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        Color color, int radius)
    {
        RectTransform r = Place(name, parent, anchor, pivot, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        if (radius > 0)
        {
            TabletRoundedImage round = r.gameObject.AddComponent<TabletRoundedImage>();
            round.radius = radius;
        }
        return img;
    }

    TextMeshProUGUI TextPlaced(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos,
        Vector2 size, string text, float fontSize, TextAlignmentOptions align, Color color, bool bold)
    {
        RectTransform r = Place(name, parent, anchor, pivot, pos, size);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        return t;
    }
}
