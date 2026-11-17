using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Procedural sprites + small runtime effect components used by NeonUITheme.
// Everything is generated at runtime so no extra art assets are required.
public static class NeonUISprites
{
    private static Sprite buttonFill;
    private static Sprite buttonOutline;
    private static Sprite cardFill;
    private static Sprite cardOutline;
    private static Sprite gradientTop;
    private static Sprite gradientBottom;
    private static Sprite vignette;
    private static Sprite circle;
    private static Sprite cornerBracket;
    private static Texture2D scanlines;

    public static Sprite ButtonFill
    {
        get { if (buttonFill == null) buttonFill = BuildRounded(64, 16f, 0f, false); return buttonFill; }
    }

    public static Sprite ButtonOutline
    {
        get { if (buttonOutline == null) buttonOutline = BuildRounded(64, 16f, 2.5f, true); return buttonOutline; }
    }

    public static Sprite CardFill
    {
        get { if (cardFill == null) cardFill = BuildRounded(128, 34f, 0f, false); return cardFill; }
    }

    public static Sprite CardOutline
    {
        get { if (cardOutline == null) cardOutline = BuildRounded(128, 34f, 2f, true); return cardOutline; }
    }

    public static Sprite GradientTop
    {
        get { if (gradientTop == null) gradientTop = BuildVerticalGradient(true); return gradientTop; }
    }

    public static Sprite GradientBottom
    {
        get { if (gradientBottom == null) gradientBottom = BuildVerticalGradient(false); return gradientBottom; }
    }

    public static Sprite Vignette
    {
        get { if (vignette == null) vignette = BuildVignette(256); return vignette; }
    }

    public static Sprite Circle
    {
        get { if (circle == null) circle = BuildCircle(64); return circle; }
    }

    public static Sprite CornerBracket
    {
        get { if (cornerBracket == null) cornerBracket = BuildCorner(32, 5f); return cornerBracket; }
    }

    public static Texture2D Scanlines
    {
        get { if (scanlines == null) scanlines = BuildScanlines(); return scanlines; }
    }

    private static Sprite BuildRounded(int size, float radius, float thickness, bool outline)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = outline ? "NeonRoundedOutline" : "NeonRoundedFill";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        float inner = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float qx = Mathf.Abs(px) - inner;
                float qy = Mathf.Abs(py) - inner;
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float dist = outside + inside - radius;
                float d = -dist;

                float alpha = outline
                    ? Mathf.Clamp01(Mathf.Min(d, thickness - d) + 0.5f)
                    : Mathf.Clamp01(d + 0.5f);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = tex.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite BuildVerticalGradient(bool topOpaque)
    {
        int width = 4;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = topOpaque ? "NeonGradientTop" : "NeonGradientBottom";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            float alpha = topOpaque ? t : 1f - t;
            for (int x = 0; x < width; x++) pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.name = tex.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite BuildVignette(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "NeonVignette";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, r));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.name = tex.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite BuildCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "NeonCircle";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - half;
                float dy = y + 0.5f - half;
                float d = half - Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(d + 0.5f));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.name = tex.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite BuildCorner(int size, float thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "NeonCornerBracket";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        float length = size * 0.78f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool topBar = y >= size - thickness && x <= length;
                bool leftBar = x < thickness && y >= size - length;
                pixels[y * size + x] = new Color(1f, 1f, 1f, (topBar || leftBar) ? 1f : 0f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = tex.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Texture2D BuildScanlines()
    {
        int width = 1;
        int height = 6;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "NeonScanlines";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        float[] profile = { 0f, 0.22f, 0.55f, 0.55f, 0.22f, 0f };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
            pixels[y] = new Color(1f, 1f, 1f, profile[y]);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}

// Marker so a canvas is only themed once.
public class NeonCanvasTag : MonoBehaviour
{
}

// Slow moving scanline + pulsing colour blobs.
// The effect fades away while a run is in progress so gameplay stays clean.
public class NeonAtmosphere : MonoBehaviour
{
    public RawImage scanlines;
    public Image blobCyan;
    public Image blobMagenta;

    private const float ScanAlpha = 0.03f;
    private float strength = 1f;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        GameManager gameManager = GameManager.Instance;
        bool playing = gameManager != null && gameManager.IsPlaying;
        strength = Mathf.MoveTowards(strength, playing ? 0f : 1f, dt * 2.5f);

        if (scanlines != null)
        {
            Rect uv = scanlines.uvRect;
            uv.y = Mathf.Repeat(uv.y + dt * 0.01f, 1f);
            scanlines.uvRect = uv;
            scanlines.color = new Color(1f, 1f, 1f, ScanAlpha * strength);
        }

        Pulse(blobCyan, 0.05f, 0.02f, t * 0.7f);
        Pulse(blobMagenta, 0.04f, 0.02f, t * 0.55f + 1.7f);
    }

    private void Pulse(Image image, float baseAlpha, float amount, float phase)
    {
        if (image == null) return;
        Color color = image.color;
        color.a = (baseAlpha + amount * (0.5f + 0.5f * Mathf.Sin(phase))) * strength;
        image.color = color;
    }
}

// Fade / pop-in when a panel becomes active.
public class NeonPanelFX : MonoBehaviour
{
    private const float Duration = 0.26f;
    private CanvasGroup group;
    private RectTransform rect;
    private float time;

    private void OnEnable()
    {
        Ensure();
        time = 0f;
        if (group != null) group.alpha = 0f;
        if (rect != null) rect.localScale = new Vector3(0.97f, 0.97f, 1f);
    }

    private void Update()
    {
        Ensure();
        if (time >= Duration) return;

        time += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(time / Duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        if (group != null) group.alpha = eased;
        if (rect != null)
        {
            float s = Mathf.Lerp(0.97f, 1f, eased);
            rect.localScale = new Vector3(s, s, 1f);
        }
    }

    private void Ensure()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        if (rect == null) rect = transform as RectTransform;
    }
}

// Gentle breathing glow used on the big titles.
public class NeonTitleFX : MonoBehaviour
{
    public TMP_Text text;
    public float scaleAmplitude = 0.018f;
    public float speed = 1.4f;

    private RectTransform rect;
    private Material material;
    private Color underlay;
    private bool hasUnderlay;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (text == null) text = GetComponent<TMP_Text>();

        if (text != null)
        {
            material = text.fontSharedMaterial;
            if (material != null && material.HasProperty("_UnderlayColor"))
            {
                underlay = material.GetColor("_UnderlayColor");
                hasUnderlay = true;
            }
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        if (rect != null)
        {
            float s = 1f + scaleAmplitude * Mathf.Sin(t * speed);
            rect.localScale = new Vector3(s, s, 1f);
        }

        if (hasUnderlay && material != null)
        {
            Color color = underlay;
            color.a = underlay.a * (0.7f + 0.3f * (0.5f + 0.5f * Mathf.Sin(t * speed * 1.2f)));
            material.SetColor("_UnderlayColor", color);
        }
    }
}

// Soft pulsing on a single graphic (card border, accent lines, ...).
public class NeonGlowPulse : MonoBehaviour
{
    public Graphic target;
    public float min = 0.55f;
    public float max = 1f;
    public float speed = 1.1f;

    private Color baseColor;
    private bool ready;

    private void Awake()
    {
        if (target == null) target = GetComponent<Graphic>();
        if (target != null)
        {
            baseColor = target.color;
            ready = true;
        }
    }

    private void Update()
    {
        if (!ready) return;

        Color color = baseColor;
        color.a = baseColor.a * Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
        target.color = color;
    }
}

// Hover / press feedback for buttons.
public class NeonButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Graphic glow;
    public TMP_Text label;
    public Color baseColor = new Color(0.1f, 0.96f, 1f, 0.55f);
    public Color hoverColor = new Color(0.1f, 0.96f, 1f, 1f);
    public Color labelBase = new Color(0.93f, 0.97f, 1f, 1f);
    public Color labelHover = new Color(0.7f, 1f, 1f, 1f);

    private RectTransform rect;
    private float current = 1f;
    private float target = 1f;
    private bool hover;
    private bool pressed;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    public void OnPointerEnter(PointerEventData eventData) { hover = true; }
    public void OnPointerExit(PointerEventData eventData) { hover = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }

    private void Update()
    {
        if (rect == null) rect = transform as RectTransform;

        target = pressed ? 0.96f : (hover ? 1.04f : 1f);
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        if (rect != null) rect.localScale = new Vector3(current, current, 1f);

        if (glow != null)
        {
            Color color = hover ? hoverColor : baseColor;
            glow.color = Color.Lerp(glow.color, color, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        if (label != null)
        {
            Color color = pressed ? labelHover : (hover ? labelHover : labelBase);
            label.color = Color.Lerp(label.color, color, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }
    }
}

// Keeps a HUD chip exactly as wide as its text so nothing looks clipped or off.
public class NeonHudChip : MonoBehaviour
{
    public TMP_Text text;
    public float padding = 52f;
    public float minWidth = 160f;

    private RectTransform rect;

    private void Awake()
    {
        rect = transform as RectTransform;
    }

    private void LateUpdate()
    {
        if (rect == null || text == null) return;

        float width = text.preferredWidth;
        if (width < 1f) return;

        float target = Mathf.Max(minWidth, width + padding);
        Vector2 size = rect.sizeDelta;
        size.x = Mathf.Lerp(size.x, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        rect.sizeDelta = size;
    }
}

// Quick punch when a TMP value changes.
public class NeonValuePop : MonoBehaviour
{
    public TMP_Text text;
    private RectTransform rect;
    private string last;
    private float pop;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (text == null) text = GetComponent<TMP_Text>();
        if (text != null) last = text.text;
    }

    private void Update()
    {
        if (text == null) return;

        if (text.text != last)
        {
            last = text.text;
            pop = 1f;
        }

        if (pop > 0f) pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 4f);
        if (rect != null)
        {
            float s = 1f + 0.12f * pop * pop;
            rect.localScale = new Vector3(s, s, 1f);
        }
    }
}

// Persistent black overlay that fades in after every scene load.
public class NeonSceneFade : MonoBehaviour
{
    private const float Duration = 0.4f;
    private CanvasGroup group;
    private float time;
    private bool playing;

    public static NeonSceneFade Create()
    {
        GameObject go = new GameObject("__NeonSceneFade", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(go.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = new Color(0.01f, 0.015f, 0.03f, 1f);
        image.raycastTarget = false;

        NeonSceneFade fade = go.AddComponent<NeonSceneFade>();
        fade.group = go.GetComponent<CanvasGroup>();
        fade.PlayIn();
        return fade;
    }

    public void PlayIn()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        time = 0f;
        playing = true;
        if (group != null) group.alpha = 1f;
    }

    private void Update()
    {
        if (!playing) return;

        time += Time.unscaledDeltaTime;
        float alpha = 1f - Mathf.Clamp01(time / Duration);
        if (group != null) group.alpha = alpha;
        if (alpha <= 0f) playing = false;
    }
}
