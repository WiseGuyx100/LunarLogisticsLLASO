using UnityEngine;
using UnityEngine.UI;

// The hover look for one tablet button.
// Every button grows a little, lights up yellow and glows.
// On top of that, each button has its own effect:
//   Camera  : the lens "focuses" (pulses) and the little light blinks
//   Radar   : a ping ripples outward from the center
//   Package : the box hops up and down
//   Notepad : the lines write themselves out, one after another
// TabletBuilder fills in all the slots below. TabletUI calls SetHovered().
public class TabletTileFx : MonoBehaviour
{
    public enum Kind { Camera, Radar, Package, Notepad }
    public Kind kind;

    [Header("Look")]
    public Image edge;
    public Image glow;
    public Color normalEdge = new Color32(124, 58, 237, 255);   // purple
    public Color hoverEdge = new Color32(255, 200, 61, 255);    // yellow
    public float hoverScale = 1.07f;
    public float glowStrength = 0.4f;

    [Header("Effect parts")]
    public RectTransform iconHolder;
    public RectTransform partA;
    public RectTransform partB;
    public RectTransform[] lines;

    private bool hovered = false;
    private float amount = 0f;      // 0 = not hovered, 1 = fully hovered
    private float time = 0f;        // how long it has been hovered
    private Image partAImage;
    private Vector2 holderStart;

    void Awake()
    {
        if (partA != null) partAImage = partA.GetComponent<Image>();
        if (iconHolder != null) holderStart = iconHolder.anchoredPosition;
    }

    public void SetHovered(bool value)
    {
        hovered = value;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        amount = Mathf.MoveTowards(amount, hovered ? 1f : 0f, dt * 6f);
        float e = amount * amount * (3f - 2f * amount);   // smooth

        if (amount > 0f) time += dt; else time = 0f;

        // ---- Look shared by every button ----
        transform.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, e);

        if (edge != null) edge.color = Color.Lerp(normalEdge, hoverEdge, e);

        if (glow != null)
        {
            Color g = hoverEdge;
            g.a = glowStrength * e;
            glow.color = g;
        }

        // ---- Each button's own effect ----
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6f);

        switch (kind)
        {
            case Kind.Camera:
                if (partA != null) partA.localScale = Vector3.one * (1f + e * 0.5f * pulse);
                if (partB != null)
                {
                    float blink = Mathf.Sin(time * 10f) > 0f ? 1f : 0.15f;
                    partB.localScale = Vector3.one * Mathf.Lerp(1f, blink, e);
                }
                break;

            case Kind.Radar:
                float p = Mathf.Repeat(time * 0.9f, 1f);
                if (partA != null)
                {
                    partA.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.7f, p);
                    if (partAImage != null)
                    {
                        Color c = partAImage.color;
                        c.a = (1f - p) * 0.55f * e;
                        partAImage.color = c;
                    }
                }
                if (partB != null) partB.localScale = Vector3.one * (1f + e * 0.4f * pulse);
                break;

            case Kind.Package:
                if (iconHolder != null)
                {
                    float hop = e * Mathf.Abs(Mathf.Sin(time * 6f)) * 14f;
                    iconHolder.anchoredPosition = holderStart + new Vector2(0f, hop);
                }
                break;

            case Kind.Notepad:
                if (lines != null)
                {
                    float cycle = Mathf.Repeat(time, 2f);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i] == null) continue;
                        float progress = Mathf.Clamp01((cycle - i * 0.4f) / 0.5f);
                        float width = Mathf.Lerp(1f, Mathf.Max(0.08f, progress), e);
                        lines[i].localScale = new Vector3(width, 1f, 1f);
                    }
                }
                break;
        }
    }
}
