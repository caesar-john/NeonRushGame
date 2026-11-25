using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runtime "neon cyberpunk" theme for the Programmer 2 UI.
// It only restyles the existing canvas at runtime, so no scene or teammate
// asset has to be edited. Works with the additive Game scene as well.
public class NeonUITheme : MonoBehaviour
{
    public static readonly Color Cyan = new Color(0.1f, 0.96f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.22f, 0.9f);
    public static readonly Color Gold = new Color(1f, 0.83f, 0.25f);
    public static readonly Color Red = new Color(1f, 0.2f, 0.4f);
    public static readonly Color TextMain = new Color(0.93f, 0.97f, 1f);
    public static readonly Color Muted = new Color(0.62f, 0.74f, 0.85f);

    private static NeonUITheme instance;
    private NeonSceneFade fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static NeonUITheme EnsureInstance()
    {
        if (instance != null) return instance;

        GameObject existing = GameObject.Find("__NeonUITheme");
        if (existing != null)
        {
            instance = existing.GetComponent<NeonUITheme>();
            if (instance != null) return instance;
        }

        GameObject go = new GameObject("__NeonUITheme");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<NeonUITheme>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        fade = NeonSceneFade.Create();
        ApplyAll();
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAll();
        if (fade != null) fade.PlayIn();
    }

    private void ApplyAll()
    {
        int count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                GameObject root = roots[r];
                if (root == null) continue;
                if (root.name.StartsWith("__Neon")) continue;

                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                for (int c = 0; c < canvases.Length; c++)
                {
                    Canvas canvas = canvases[c];
                    if (canvas == null || !canvas.isRootCanvas || !canvas.isActiveAndEnabled) continue;
                    if (canvas.name.StartsWith("__Neon")) continue;
                    if (canvas.GetComponent<NeonCanvasTag>() != null) continue;
                    StyleCanvas(canvas);
                }
            }
        }
    }

    private void StyleCanvas(Canvas canvas)
    {
        canvas.gameObject.AddComponent<NeonCanvasTag>();
        Debug.Log("[NeonUITheme] Styling canvas '" + canvas.name + "' di scene '" + canvas.gameObject.scene.name + "'.");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        CreateAtmosphere(canvas.transform);

        StyleTexts(canvas.transform);
        StyleButtons(canvas.transform);

        StylePanel(canvas.transform, "PausePanel", Cyan, 0.62f, 0.94f);
        StylePanel(canvas.transform, "GameOverPanel", Red, 0.72f, 0.96f);
        StylePanel(canvas.transform, "MenuPanel", Magenta, 0.45f, 0.93f);

        StyleHud(canvas.transform);
    }

    // ---------------------------------------------------------------- panels

    private static void StylePanel(Transform parent, string panelName, Color accent, float opacity, float cardAlpha)
    {
        Transform panel = parent.Find(panelName);
        if (panel == null) return;

        Image background = panel.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.01f, 0.015f, 0.035f, opacity);
        }

        if (panelName == "MenuPanel") TuneMenu(panel, accent);

        RectTransform card = EnsureCard(panel, accent, cardAlpha);
        if (card == null) return;

        if (panel.GetComponent<NeonPanelFX>() == null)
            panel.gameObject.AddComponent<NeonPanelFX>();
    }

    private static void TuneMenu(Transform panel, Color accent)
    {
        Transform sdg = panel.Find("Sdg");
        if (sdg != null) sdg.gameObject.SetActive(false);

        MoveChild(panel, "Title", new Vector2(0f, 250f));
        MoveChild(panel, "Subtitle", new Vector2(0f, 145f));
        MoveChild(panel, "PlayButton", new Vector2(0f, -20f));
        MoveChild(panel, "QuitButton", new Vector2(0f, -130f));

        AddDivider(panel, accent, 88f, 280f);
    }

    private static void MoveChild(Transform panel, string name, Vector2 position)
    {
        Transform child = panel.Find(name);
        if (child == null) return;

        RectTransform rect = child as RectTransform;
        if (rect != null) rect.anchoredPosition = position;
    }

    private static void AddDivider(Transform panel, Color accent, float y, float width)
    {
        if (panel.Find("__NeonDivider") != null) return;

        GameObject root = new GameObject("__NeonDivider", typeof(RectTransform));
        root.transform.SetParent(panel, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, y);
        rootRect.sizeDelta = new Vector2(width, 8f);

        GameObject lineObject = new GameObject("Line", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(root.transform, false);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0.5f);
        lineRect.anchorMax = new Vector2(1f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(0f, 1.5f);
        Image line = lineObject.GetComponent<Image>();
        line.color = new Color(accent.r, accent.g, accent.b, 0.55f);
        line.raycastTarget = false;

        NeonGlowPulse linePulse = lineObject.AddComponent<NeonGlowPulse>();
        linePulse.target = line;
        linePulse.min = 0.45f;
        linePulse.max = 1f;
        linePulse.speed = 0.9f;

        GameObject diamondObject = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
        diamondObject.transform.SetParent(root.transform, false);
        RectTransform diamondRect = diamondObject.GetComponent<RectTransform>();
        diamondRect.anchorMin = new Vector2(0.5f, 0.5f);
        diamondRect.anchorMax = new Vector2(0.5f, 0.5f);
        diamondRect.pivot = new Vector2(0.5f, 0.5f);
        diamondRect.anchoredPosition = Vector2.zero;
        diamondRect.sizeDelta = new Vector2(9f, 9f);
        diamondRect.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image diamond = diamondObject.GetComponent<Image>();
        diamond.color = accent;
        diamond.raycastTarget = false;
    }

    private static RectTransform EnsureCard(Transform panel, Color accent, float cardAlpha)
    {
        Transform existing = panel.Find("__NeonCard");
        if (existing != null) return existing as RectTransform;

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        bool any = false;

        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child.name.StartsWith("__")) continue;
            if (!child.gameObject.activeSelf) continue;

            RectTransform rect = child as RectTransform;
            if (rect == null) continue;
            if (!SameAnchor(rect)) continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            float width;
            float height;

            if (text != null)
            {
                int length = text.text != null ? text.text.Length : 4;
                width = length * text.fontSize * 0.72f + text.characterSpacing * length;
                height = text.fontSize * 1.3f;

                try
                {
                    text.ForceMeshUpdate(true, true);
                    if (text.preferredWidth > width) width = text.preferredWidth;
                    if (text.preferredHeight > height) height = text.preferredHeight;
                }
                catch { }
            }
            else
            {
                width = rect.sizeDelta.x;
                height = rect.sizeDelta.y;
            }

            Vector2 pivot = rect.pivot;
            Vector2 center = rect.anchoredPosition;
            Vector2 a = center - new Vector2(width, height) * pivot;
            Vector2 b = center + new Vector2(width, height) * (Vector2.one - pivot);

            min = Vector2.Min(min, a);
            max = Vector2.Max(max, b);
            any = true;
        }

        if (!any) return null;

        Vector2 size = max - min + new Vector2(150f, 136f);
        Vector2 cardCenter = (min + max) * 0.5f;

        GameObject cardObject = new GameObject("__NeonCard", typeof(RectTransform), typeof(Image));
        cardObject.transform.SetParent(panel, false);
        cardObject.transform.SetAsFirstSibling();

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = cardCenter;
        cardRect.sizeDelta = size;

        GameObject glowObject = new GameObject("__NeonCardGlow", typeof(RectTransform), typeof(Image));
        glowObject.transform.SetParent(panel, false);
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = cardCenter;
        glowRect.sizeDelta = size + new Vector2(64f, 64f);
        Image glow = glowObject.GetComponent<Image>();
        glow.sprite = NeonUISprites.CardFill;
        glow.type = Image.Type.Sliced;
        glow.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        glow.raycastTarget = false;
        glowObject.transform.SetSiblingIndex(0);

        Image fill = cardObject.GetComponent<Image>();
        fill.sprite = NeonUISprites.CardFill;
        fill.type = Image.Type.Sliced;
        fill.color = new Color(0.03f, 0.055f, 0.11f, cardAlpha);
        fill.raycastTarget = false;

        GameObject gradientObject = new GameObject("__NeonCardGradient", typeof(RectTransform), typeof(Image));
        gradientObject.transform.SetParent(cardObject.transform, false);
        Stretch(gradientObject.GetComponent<RectTransform>(), 0f);
        Image gradient = gradientObject.GetComponent<Image>();
        gradient.sprite = NeonUISprites.GradientBottom;
        gradient.type = Image.Type.Simple;
        gradient.color = new Color(accent.r, accent.g, accent.b, 0.1f);
        gradient.raycastTarget = false;

        GameObject borderObject = new GameObject("__NeonCardBorder", typeof(RectTransform), typeof(Image));
        borderObject.transform.SetParent(cardObject.transform, false);
        Stretch(borderObject.GetComponent<RectTransform>(), -2f);
        Image border = borderObject.GetComponent<Image>();
        border.sprite = NeonUISprites.CardOutline;
        border.type = Image.Type.Sliced;
        border.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        border.raycastTarget = false;

        NeonGlowPulse borderPulse = borderObject.AddComponent<NeonGlowPulse>();
        borderPulse.target = border;
        borderPulse.min = 0.55f;
        borderPulse.max = 1f;
        borderPulse.speed = 1.1f;

        AddCorner(cardObject.transform, accent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), 0f);
        AddCorner(cardObject.transform, accent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), -90f);
        AddCorner(cardObject.transform, accent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 12f), 180f);
        AddCorner(cardObject.transform, accent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), 90f);

        GameObject lineObject = new GameObject("__NeonCardLine", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(cardObject.transform, false);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 1f);
        lineRect.anchorMax = new Vector2(0.5f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.anchoredPosition = new Vector2(0f, 8f);
        lineRect.sizeDelta = new Vector2(size.x * 0.55f, 3f);
        Image line = lineObject.GetComponent<Image>();
        line.color = accent;
        line.raycastTarget = false;

        return cardRect;
    }

    // ------------------------------------------------------------------- hud

    private static void StyleHud(Transform parent)
    {
        Transform hud = parent.Find("HUDPanel");
        if (hud == null) return;
        if (hud.Find("__NeonHUDBar") != null) return;

        // ===================== HUD BAR =====================
        GameObject barObject = new GameObject("__NeonHUDBar", typeof(RectTransform), typeof(Image));
        barObject.transform.SetParent(hud, false);
        barObject.transform.SetAsFirstSibling();

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0f, 154f);

        Image bar = barObject.GetComponent<Image>();
        bar.sprite = NeonUISprites.GradientTop;
        bar.type = Image.Type.Simple;
        bar.color = new Color(0.008f, 0.016f, 0.042f, 0.93f);
        bar.raycastTarget = false;

        // ===================== HUD LINE =====================
        GameObject lineObject = new GameObject("__NeonHUDLine", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(barObject.transform, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 0f);
        lineRect.pivot = new Vector2(0.5f, 0f);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(0f, 2f);

        Image line = lineObject.GetComponent<Image>();
        line.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
        line.raycastTarget = false;

        // ===================== SCORE =====================
        AddHudChip(
            hud,
            "ScoreText",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -14f),
            new Vector2(20f, 86f),
            Cyan,
            1,
            170f
        );

        // ===================== TOP SCORE =====================
        AddHudChip(
            hud,
            "TopScoreText",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(390f, -14f),
            new Vector2(220f, 86f),
            Gold,
            2,
            170f
        );

        // ===================== DISTANCE =====================
        AddHudChip(
            hud,
            "DistanceText",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -14f),
            new Vector2(160f, 86f),
            Cyan,
            3,
            150f
        );

        // ===================== COIN =====================
        AddHudChip(
            hud,
            "CoinText",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -14f),
            new Vector2(240f, 86f),
            Gold,
            4,
            170f
        );
    }

    private static void AddHudChip(Transform hud, string textName, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color accent, int siblingIndex, float minWidth)
    {
        if (hud.Find("__NeonChip_" + textName) != null) return;

        GameObject chipObject = new GameObject("__NeonChip_" + textName, typeof(RectTransform), typeof(Image));
        chipObject.transform.SetParent(hud, false);

        RectTransform chipRect = chipObject.GetComponent<RectTransform>();
        chipRect.anchorMin = anchor;
        chipRect.anchorMax = anchor;
        chipRect.pivot = pivot;
        chipRect.anchoredPosition = position;
        chipRect.sizeDelta = size;
        chipObject.transform.SetSiblingIndex(siblingIndex);

        Image fill = chipObject.GetComponent<Image>();
        fill.sprite = NeonUISprites.ButtonFill;
        fill.type = Image.Type.Sliced;
        fill.color = new Color(0.025f, 0.045f, 0.09f, 0.62f);
        fill.raycastTarget = false;

        GameObject borderObject = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderObject.transform.SetParent(chipObject.transform, false);
        Stretch(borderObject.GetComponent<RectTransform>(), -1.5f);

        Image border = borderObject.GetComponent<Image>();
        border.sprite = NeonUISprites.ButtonOutline;
        border.type = Image.Type.Sliced;
        border.color = new Color(accent.r, accent.g, accent.b, 0.5f);
        border.raycastTarget = false;

        Transform textTransform = hud.Find(textName);
        if (textTransform != null)
        {
            NeonHudChip follower = chipObject.AddComponent<NeonHudChip>();
            follower.text = textTransform.GetComponent<TMP_Text>();
            follower.padding = 52f;
            follower.minWidth = minWidth;
        }
    }

    // -------------------------------------------------------------- atmosphere

    private static void CreateAtmosphere(Transform canvas)
    {
        if (canvas.Find("__NeonAtmosphere") != null) return;

        GameObject rootObject = new GameObject("__NeonAtmosphere", typeof(RectTransform));
        rootObject.transform.SetParent(canvas, false);
        rootObject.transform.SetAsFirstSibling();
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        Stretch(rootRect, 0f);

        GameObject vignetteObject = new GameObject("__NeonVignette", typeof(RectTransform), typeof(Image));
        vignetteObject.transform.SetParent(rootObject.transform, false);
        Stretch(vignetteObject.GetComponent<RectTransform>(), 0f);
        Image vignette = vignetteObject.GetComponent<Image>();
        vignette.sprite = NeonUISprites.Vignette;
        vignette.type = Image.Type.Simple;
        vignette.color = new Color(0f, 0f, 0f, 0.32f);
        vignette.raycastTarget = false;

        GameObject bottomObject = new GameObject("__NeonBottomGlow", typeof(RectTransform), typeof(Image));
        bottomObject.transform.SetParent(rootObject.transform, false);
        Stretch(bottomObject.GetComponent<RectTransform>(), 0f);
        Image bottom = bottomObject.GetComponent<Image>();
        bottom.sprite = NeonUISprites.GradientBottom;
        bottom.type = Image.Type.Simple;
        bottom.color = new Color(0.02f, 0.03f, 0.09f, 0.2f);
        bottom.raycastTarget = false;

        RawImage scan = CreateScanlines(rootObject.transform);

        NeonAtmosphere atmosphere = rootObject.AddComponent<NeonAtmosphere>();
        atmosphere.scanlines = scan;
    }

    private static RawImage CreateScanlines(Transform parent)
    {
        GameObject scanObject = new GameObject("__NeonScanlines", typeof(RectTransform), typeof(RawImage));
        scanObject.transform.SetParent(parent, false);
        Stretch(scanObject.GetComponent<RectTransform>(), 0f);

        RawImage scan = scanObject.GetComponent<RawImage>();
        scan.texture = NeonUISprites.Scanlines;
        scan.uvRect = new Rect(0f, 0f, 1f, 180f);
        scan.color = new Color(1f, 1f, 1f, 0.035f);
        scan.raycastTarget = false;
        return scan;
    }

    private static Image CreateBlob(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float size)
    {
        GameObject blobObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        blobObject.transform.SetParent(parent, false);

        RectTransform rect = blobObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);

        Image image = blobObject.GetComponent<Image>();
        image.sprite = NeonUISprites.Vignette;
        image.type = Image.Type.Simple;
        image.color = new Color(color.r, color.g, color.b, 0.05f);
        image.raycastTarget = false;
        return image;
    }

    // ---------------------------------------------------------------- buttons

    private static void StyleButtons(Transform canvas)
    {
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.name.StartsWith("__")) continue;

            bool primary = button.name == "PlayButton" || button.name == "RestartButton" || button.name == "ResumeButton";

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = NeonUISprites.ButtonFill;
                image.type = Image.Type.Sliced;
                image.color = primary
                    ? new Color(0.05f, 0.1f, 0.19f, 0.97f)
                    : new Color(0.035f, 0.055f, 0.1f, 0.95f);
            }

            Transform borderTransform = button.transform.Find("__NeonBorder");
            Image border;
            if (borderTransform == null)
            {
                GameObject borderObject = new GameObject("__NeonBorder", typeof(RectTransform), typeof(Image));
                borderObject.transform.SetParent(button.transform, false);
                borderObject.transform.SetAsFirstSibling();
                Stretch(borderObject.GetComponent<RectTransform>(), -2f);

                border = borderObject.GetComponent<Image>();
                border.sprite = NeonUISprites.ButtonOutline;
                border.type = Image.Type.Sliced;
                border.raycastTarget = false;
            }
            else
            {
                border = borderTransform.GetComponent<Image>();
            }

            if (border != null) border.color = primary
                ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f)
                : new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f);

            Transform accentTransform = button.transform.Find("__NeonAccent");
            Image accentBar;
            if (accentTransform == null)
            {
                GameObject accentObject = new GameObject("__NeonAccent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(button.transform, false);
                RectTransform accentRect = accentObject.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 0.5f);
                accentRect.anchorMax = new Vector2(0f, 0.5f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.anchoredPosition = new Vector2(18f, 0f);
                accentRect.sizeDelta = new Vector2(4f, 38f);

                accentBar = accentObject.GetComponent<Image>();
                accentBar.raycastTarget = false;
            }
            else
            {
                accentBar = accentTransform.GetComponent<Image>();
            }

            accentBar.color = primary
                ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.95f)
                : new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);

            NeonButtonFX effect = button.GetComponent<NeonButtonFX>();
            if (effect == null) effect = button.gameObject.AddComponent<NeonButtonFX>();
            effect.glow = border;
            effect.label = button.GetComponentInChildren<TMP_Text>(true);
            effect.labelBase = new Color(0.93f, 0.97f, 1f, 1f);
            effect.labelHover = new Color(0.72f, 1f, 1f, 1f);
            effect.baseColor = primary
                ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f)
                : new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f);
            effect.hoverColor = new Color(Cyan.r, Cyan.g, Cyan.b, 1f);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
        }
    }

    // ------------------------------------------------------------------ texts

    private static void StyleTexts(Transform canvas)
    {
        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.name.StartsWith("__")) continue;
            StyleText(text);
        }
    }

    private static void StyleText(TMP_Text text)
    {
        string parent = text.transform.parent != null ? text.transform.parent.name : string.Empty;

        switch (text.name)
        {
            case "Title":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 10f;
                if (parent == "GameOverPanel")
                {
                    text.color = Red;
                    SetUnderlay(text, Red, 0.55f, 0f, 0f, 0.8f, 0.55f);
                }
                else if (parent == "PausePanel")
                {
                    text.color = Cyan;
                    SetUnderlay(text, Cyan, 0.5f, 0f, 0f, 0.7f, 0.5f);
                }
                else
                {
                    text.enableVertexGradient = true;
                    text.colorGradient = new VertexGradient(Cyan, Magenta, Cyan, Magenta);
                    text.color = Color.white;
                    SetUnderlay(text, Cyan, 0.45f, 0f, 0f, 0.7f, 0.55f);
                }

                if (text.GetComponent<NeonTitleFX>() == null)
                {
                    NeonTitleFX titleFx = text.gameObject.AddComponent<NeonTitleFX>();
                    titleFx.text = text;
                    titleFx.speed = parent == "GameOverPanel" ? 1.1f : 1.4f;
                }
                break;

            case "Subtitle":
                text.color = TextMain;
                text.characterSpacing = 3f;
                SetUnderlay(text, Color.black, 0.65f, 0f, -1.5f, 0.2f, 0.3f);
                break;

            case "Sdg":
                text.color = Magenta;
                text.characterSpacing = 2f;
                break;

            case "Controls":
                text.color = Muted;
                text.characterSpacing = 2f;
                SetUnderlay(text, Color.black, 0.6f, 0f, -1f, 0.1f, 0.2f);
                break;

            case "ScoreText":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 2f;
                text.color = Cyan;
                SetUnderlay(text, Cyan, 0.35f, 0f, 0f, 0.6f, 0.45f);
                break;

            case "TopScoreText":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 2f;
                text.color = Gold;
                SetUnderlay(text, Gold, 0.4f, 0f, 0f, 0.6f, 0.45f);
                break;

            case "DistanceText":
                text.fontStyle = FontStyles.Bold;
                text.color = TextMain;
                SetUnderlay(text, Cyan, 0.25f, 0f, 0f, 0.5f, 0.4f);
                break;

            case "CoinText":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 2f;
                text.color = Gold;
                SetUnderlay(text, Gold, 0.4f, 0f, 0f, 0.6f, 0.45f);
                AddPop(text);
                break;

            case "FinalScore":
                text.fontStyle = FontStyles.Bold;
                text.color = TextMain;
                SetUnderlay(text, Cyan, 0.3f, 0f, 0f, 0.5f, 0.4f);
                AddPop(text);
                break;

            case "FinalDistance":
                text.fontStyle = FontStyles.Bold;
                text.color = Cyan;
                SetUnderlay(text, Cyan, 0.35f, 0f, 0f, 0.6f, 0.45f);
                AddPop(text);
                break;

            case "FinalCoin":
                text.fontStyle = FontStyles.Bold;
                text.color = Gold;
                SetUnderlay(text, Gold, 0.4f, 0f, 0f, 0.6f, 0.45f);
                AddPop(text);
                break;

            case "BestScore":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 3f;
                text.color = Magenta;
                SetUnderlay(text, Magenta, 0.4f, 0f, 0f, 0.6f, 0.45f);
                AddPop(text);
                break;

            case "Label":
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 5f;
                text.color = TextMain;
                SetUnderlay(text, Cyan, 0.28f, 0f, 0f, 0.5f, 0.4f);
                break;
        }
    }

    private static void AddPop(TMP_Text text)
    {
        if (text.GetComponent<NeonValuePop>() != null) return;
        NeonValuePop pop = text.gameObject.AddComponent<NeonValuePop>();
        pop.text = text;
    }

    // ---------------------------------------------------------------- helpers

    private static void SetUnderlay(TMP_Text text, Color color, float alpha, float offsetX, float offsetY, float dilate, float softness)
    {
        Material source = text.fontSharedMaterial;
        if (source == null) return;

        Material material = new Material(source);
        material.name = source.name + " (Neon)";
        material.hideFlags = HideFlags.HideAndDontSave;

        bool touched = false;
        if (material.HasProperty("_UnderlayColor"))
        {
            material.SetColor("_UnderlayColor", new Color(color.r, color.g, color.b, alpha));
            touched = true;
        }
        if (material.HasProperty("_UnderlayOffsetX")) material.SetFloat("_UnderlayOffsetX", offsetX);
        if (material.HasProperty("_UnderlayOffsetY")) material.SetFloat("_UnderlayOffsetY", offsetY);
        if (material.HasProperty("_UnderlayDilate")) material.SetFloat("_UnderlayDilate", dilate);
        if (material.HasProperty("_UnderlaySoftness")) material.SetFloat("_UnderlaySoftness", softness);

        if (!touched) return;

        material.EnableKeyword("UNDERLAY_ON");
        material.DisableKeyword("UNDERLAY_INNER");
        text.fontSharedMaterial = material;
    }

    private static void AddCorner(Transform card, Color accent, Vector2 anchor, Vector2 pivot, Vector2 position, float rotation)
    {
        GameObject cornerObject = new GameObject("__NeonCorner", typeof(RectTransform), typeof(Image));
        cornerObject.transform.SetParent(card, false);

        RectTransform rect = cornerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(30f, 30f);
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);

        Image image = cornerObject.GetComponent<Image>();
        image.sprite = NeonUISprites.CornerBracket;
        image.type = Image.Type.Simple;
        image.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        image.raycastTarget = false;
    }

    private static bool SameAnchor(RectTransform rect)
    {
        return Mathf.Abs(rect.anchorMin.x - rect.anchorMax.x) < 0.001f
            && Mathf.Abs(rect.anchorMin.y - rect.anchorMax.y) < 0.001f;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}
