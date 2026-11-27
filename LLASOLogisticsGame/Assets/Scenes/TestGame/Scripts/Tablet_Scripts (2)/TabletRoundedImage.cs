using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Gives a UI Image smooth rounded corners (no sprite files needed).
// The tablet builder puts this on every rounded panel, button and icon.
[RequireComponent(typeof(Image))]
public class TabletRoundedImage : MonoBehaviour
{
    public int radius = 24;

    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

    void Awake()
    {
        Image img = GetComponent<Image>();
        img.sprite = Get(radius);
        img.type = Image.Type.Sliced;
    }

    // Makes (or reuses) a white rounded-rectangle sprite with this corner radius
    public static Sprite Get(int radius)
    {
        radius = Mathf.Max(1, radius);

        Sprite sprite;
        if (cache.TryGetValue(radius, out sprite) && sprite != null) return sprite;

        int size = radius * 2 + 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.hideFlags = HideFlags.HideAndDontSave;

        cache[radius] = sprite;
        return sprite;
    }
}
