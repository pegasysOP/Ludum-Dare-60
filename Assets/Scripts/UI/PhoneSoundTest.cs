using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sound bench for the phone. Left side is a mock phone (home screen with two swipeable pages,
/// apps, a scrolling list, toggles, locked apps, an incoming-message banner). Right side is a
/// soundboard that fires each bank directly. The UI is built in code so the scene stays tiny;
/// every sound comes from <see cref="PhoneSfxLibrary"/>, so tuning that asset here tunes the
/// real phone too. Escape acts as the phone's back button.
/// </summary>
public class PhoneSoundTest : MonoBehaviour
{
    public PhoneSfxLibrary library;

    [Header("Feel")]
    [Tooltip("Canvas units of scroll travel between scroll ticks.")]
    public float scrollTickSpacing = 56f;
    [Tooltip("Minimum seconds between scroll ticks so a fast fling doesn't machine-gun.")]
    public float scrollTickMinInterval = 0.05f;
    [Tooltip("Horizontal drag distance (canvas units) that counts as a home-screen swipe.")]
    public float swipeThreshold = 70f;
    [Tooltip("Seconds the incoming-message banner stays up.")]
    public float bannerDuration = 3f;

    private class AppDef
    {
        public string name;
        public Color color;
        public bool locked;
        public RectTransform tile;
        public RectTransform panel;
    }

    /// <summary>Forwards drag events from the home-screen viewport so it can be swiped between pages.</summary>
    private class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action onBegin;
        public Action<float> onDrag;
        public Action<float> onEnd;
        private float startX;

        public void OnBeginDrag(PointerEventData e) { startX = e.position.x; onBegin?.Invoke(); }
        public void OnDrag(PointerEventData e) => onDrag?.Invoke(e.position.x - startX);
        public void OnEndDrag(PointerEventData e) => onEnd?.Invoke(e.position.x - startX);
    }

    // Phone screen geometry, in canvas units.
    private const float ScreenWidth = 384f;
    private const float StatusBarHeight = 40f;
    private const float HomeBarHeight = 56f;
    private const float AppHeaderHeight = 56f;
    private const int SpriteRadius = 32;

    private static readonly Color Ink = new Color(0.10f, 0.16f, 0.27f);
    private static readonly Color InkSoft = new Color(0.40f, 0.47f, 0.58f);
    private static readonly Color Aqua = new Color(0.16f, 0.70f, 0.93f);
    private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color Danger = new Color(0.86f, 0.25f, 0.32f);

    private static readonly string[] Senders = { "Mom", "Unknown Number", "Jess", "Detective Park", "Corp HR" };
    private static readonly string[] Previews =
    {
        "did you eat today??", "I know what you're looking for.", "omg call me. wait no, text me",
        "Meet me at the docks, 9pm.", "Reminder: badge audit on Friday :)"
    };

    private Canvas canvas;
    private Sprite rounded;
    private RectTransform pagesStrip;
    private readonly List<Image> pageDots = new List<Image>();
    private int homePage;
    private const int PageCount = 2;

    private readonly List<AppDef> allApps = new List<AppDef>();
    private RectTransform appRoot;
    private CanvasGroup appGroup;
    private TextMeshProUGUI appTitle;
    private AppDef openApp;
    private AppDef messagesApp;
    private RectTransform conversationPanel;
    private RectTransform conversationContent;

    private RectTransform banner;
    private TextMeshProUGUI bannerText;
    private Tween bannerHide;
    private int nextMessage;

    private TextMeshProUGUI clockText;
    private TextMeshProUGUI lastPlayedText;
    private float lastScrollTickTime = -1f;
    private Coroutine playAllRoutine;

    private void Awake()
    {
        if (library == null)
            library = PhoneSfxLibrary.Load();

        rounded = CreateRoundedSprite(SpriteRadius * 2 + 8, SpriteRadius);
        BuildCanvas();
    }

    private void Start()
    {
        if (AudioManager.Instance == null)
            Debug.LogWarning("PhoneSoundTest: no AudioManager in scene, nothing will play.");
        InvokeRepeating(nameof(UpdateClock), 0f, 5f);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GoBack();
    }

    //==================== Sound ====================

    /// <summary>Name of the last bank played and how many plays so far; shown on the soundboard.</summary>
    public string LastPlayed { get; private set; } = "";
    public int PlayCount { get; private set; }

    private void Play(Func<PhoneSfxLibrary, SfxBank> pick)
    {
        if (library == null) return;
        Play(pick(library));
    }

    private void Play(SfxBank bank)
    {
        if (bank == null) return;
        bank.Play();
        PlayCount++;
        LastPlayed = "?";
        foreach (KeyValuePair<string, SfxBank> entry in library.AllBanks())
        {
            if (entry.Value == bank)
                LastPlayed = entry.Key;
        }
        if (lastPlayedText != null)
            lastPlayedText.text = $"Last played: <b>{LastPlayed}</b>  <color=#{ColorUtility.ToHtmlStringRGB(InkSoft)}>(#{PlayCount})</color>";
    }

    //==================== Phone behaviour ====================

    private void OnAppTileClicked(AppDef app)
    {
        if (openApp != null) return;

        if (app.locked)
        {
            Play(l => l.error);
            Shake(app.tile);
            return;
        }

        Pop(app.tile);
        OpenApp(app);
    }

    private void OpenApp(AppDef app)
    {
        Play(l => l.appOpen);
        openApp = app;
        appTitle.text = app.name;

        foreach (Transform child in appRoot.Find("Body"))
            child.gameObject.SetActive(false);
        app.panel.gameObject.SetActive(true);
        if (conversationPanel != null)
            conversationPanel.gameObject.SetActive(false);

        appRoot.gameObject.SetActive(true);
        appRoot.DOKill();
        appGroup.DOKill();
        appGroup.blocksRaycasts = true;
        appGroup.alpha = 0f;
        appRoot.localScale = Vector3.one * 0.88f;
        appRoot.DOScale(1f, 0.22f).SetEase(Ease.OutBack);
        appGroup.DOFade(1f, 0.15f);
    }

    private void CloseApp()
    {
        if (openApp == null) return;

        Play(l => l.appClose);
        openApp = null;
        appGroup.blocksRaycasts = false;
        appRoot.DOKill();
        appGroup.DOKill();
        appRoot.DOScale(0.9f, 0.18f).SetEase(Ease.InQuad);
        appGroup.DOFade(0f, 0.18f).OnComplete(() => appRoot.gameObject.SetActive(false));
    }

    /// <summary>Back = leave the current sub-page, or close the app at its root.</summary>
    private void GoBack()
    {
        if (openApp == null) return;

        if (conversationPanel != null && conversationPanel.gameObject.activeSelf)
        {
            Play(l => l.back);
            conversationPanel.gameObject.SetActive(false);
            messagesApp.panel.gameObject.SetActive(true);
            appTitle.text = messagesApp.name;
            return;
        }

        CloseApp();
    }

    private void OnHomeBarClicked()
    {
        if (openApp != null)
            CloseApp();
        else if (homePage != 0)
            GoToPage(0);
    }

    private void OpenConversation(string sender, string preview)
    {
        // From the banner this also launches the app, so let app-open speak instead of a tap.
        if (openApp == messagesApp)
            Play(l => l.tap);
        else
            OpenApp(messagesApp);

        messagesApp.panel.gameObject.SetActive(false);
        conversationPanel.gameObject.SetActive(true);
        appTitle.text = sender;

        foreach (Transform child in conversationContent)
            Destroy(child.gameObject);
        AddBubble("hey, quick question", true);
        AddBubble(preview, false);
        AddBubble("...ok", true);
        AddBubble("Talk later.", false);
    }

    private void GoToPage(int page)
    {
        page = Mathf.Clamp(page, 0, PageCount - 1);
        if (page != homePage)
            Play(l => l.swipe);
        homePage = page;

        pagesStrip.DOKill();
        pagesStrip.DOAnchorPosX(-homePage * ScreenWidth, 0.28f).SetEase(Ease.OutCubic);
        for (int i = 0; i < pageDots.Count; i++)
            pageDots[i].color = i == homePage ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0.25f);
    }

    private void OnHomeDrag(float screenDelta)
    {
        float delta = screenDelta / canvas.scaleFactor;
        float x = -homePage * ScreenWidth + delta;
        float min = -(PageCount - 1) * ScreenWidth;
        // Rubber-band past the first/last page.
        if (x > 0f) x *= 0.35f;
        else if (x < min) x = min + (x - min) * 0.35f;
        pagesStrip.anchoredPosition = new Vector2(x, 0f);
    }

    private void OnHomeDragEnd(float screenDelta)
    {
        float delta = screenDelta / canvas.scaleFactor;
        int target = homePage;
        if (delta < -swipeThreshold) target++;
        else if (delta > swipeThreshold) target--;
        GoToPage(target);
    }

    public void ShowIncomingMessage()
    {
        string sender = Senders[nextMessage % Senders.Length];
        string preview = Previews[nextMessage % Previews.Length];
        nextMessage++;

        Play(l => l.notifMessage);
        bannerText.text = $"<b>{sender}</b>\n<color=#{ColorUtility.ToHtmlStringRGB(InkSoft)}>{preview}</color>";

        Button button = banner.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            HideBanner();
            OpenConversation(sender, preview);
        });

        bannerHide?.Kill();
        banner.DOKill();
        banner.DOAnchorPosY(-StatusBarHeight - 8f, 0.35f).SetEase(Ease.OutBack);
        bannerHide = DOVirtual.DelayedCall(bannerDuration, HideBanner);
    }

    private void HideBanner()
    {
        bannerHide?.Kill();
        banner.DOKill();
        banner.DOAnchorPosY(120f, 0.25f).SetEase(Ease.InQuad);
    }

    private void AttachScrollTicks(ScrollRect scroll)
    {
        float lastY = scroll.content.anchoredPosition.y;
        float travelled = 0f;
        scroll.onValueChanged.AddListener(_ =>
        {
            float y = scroll.content.anchoredPosition.y;
            travelled += Mathf.Abs(y - lastY);
            lastY = y;
            if (travelled < scrollTickSpacing) return;

            travelled %= scrollTickSpacing;
            if (Time.unscaledTime - lastScrollTickTime < scrollTickMinInterval) return;
            lastScrollTickTime = Time.unscaledTime;
            Play(l => l.scrollTick);
        });
    }

    private void UpdateClock()
    {
        if (clockText != null)
            clockText.text = DateTime.Now.ToString("h:mm");
    }

    private IEnumerator PlayAll()
    {
        foreach (KeyValuePair<string, SfxBank> entry in library.AllBanks())
        {
            Play(entry.Value);
            float length = entry.Value.HasAnyClip ? entry.Value.clips[0].Clip.length : 0f;
            yield return new WaitForSeconds(Mathf.Max(0.5f, length) + 0.25f);
        }
        playAllRoutine = null;
    }

    private static void Shake(RectTransform target)
    {
        target.DOKill(true);
        target.DOShakeAnchorPos(0.35f, new Vector2(10f, 0f), 18, 0f);
    }

    private static void Pop(RectTransform target)
    {
        target.DOKill(true);
        target.DOPunchScale(Vector3.one * -0.1f, 0.2f, 6, 0.5f);
    }

    //==================== Build: layout ====================

    private void BuildCanvas()
    {
        RectTransform root = CreateRect("PhoneTestCanvas", transform);
        canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        root.gameObject.AddComponent<GraphicRaycaster>();

        RawImage sky = CreateGradient("Background", root, new Color(0.20f, 0.55f, 0.90f), new Color(0.70f, 0.93f, 0.88f));
        Stretch(sky.rectTransform);

        BuildPhone(root);
        BuildSoundboard(root);
    }

    private void BuildPhone(RectTransform root)
    {
        RectTransform body = CreatePanel("Phone", root, new Color(0.07f, 0.09f, 0.13f), 60f);
        Place(body, new Vector2(-300f, 0f), new Vector2(ScreenWidth + 36f, 860f));
        RectTransform screen = CreatePanel("Screen", body, Color.white, 44f);
        Stretch(screen, 18f);
        screen.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        RawImage wallpaper = CreateGradient("Wallpaper", screen, new Color(0.45f, 0.80f, 0.98f), new Color(0.80f, 0.97f, 0.86f));
        Stretch(wallpaper.rectTransform);

        BuildHome(screen);
        BuildAppRoot(screen);
        BuildStatusBar(screen);
        BuildHomeBar(screen);
        BuildBanner(screen);
    }

    private void BuildStatusBar(RectTransform screen)
    {
        RectTransform bar = CreateRect("StatusBar", screen);
        AnchorTop(bar, StatusBarHeight, 0f);
        clockText = CreateText(bar, "9:41", 18f, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Stretch(clockText.rectTransform, 22f, 0f);
        TextMeshProUGUI battery = CreateText(bar, "100%", 16f, Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        Stretch(battery.rectTransform, 22f, 0f);
    }

    private void BuildHomeBar(RectTransform screen)
    {
        RectTransform bar = CreateRect("HomeBar", screen);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(200f, HomeBarHeight);
        bar.anchoredPosition = Vector2.zero;
        Image hit = bar.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        bar.gameObject.AddComponent<Button>().onClick.AddListener(OnHomeBarClicked);

        RectTransform pill = CreatePanel("Pill", bar, new Color(Ink.r, Ink.g, Ink.b, 0.6f), 3f);
        Place(pill, Vector2.zero, new Vector2(130f, 6f));
        pill.GetComponent<Image>().raycastTarget = false;
    }

    private void BuildHome(RectTransform screen)
    {
        RectTransform home = CreateRect("Home", screen);
        Stretch(home);
        home.offsetMin = new Vector2(0f, HomeBarHeight);
        home.offsetMax = new Vector2(0f, -StatusBarHeight);

        RectTransform viewport = CreateRect("Pages", home);
        Stretch(viewport);
        viewport.offsetMin = new Vector2(0f, 40f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        SwipeArea swipe = viewport.gameObject.AddComponent<SwipeArea>();
        swipe.onBegin = () => pagesStrip.DOKill();
        swipe.onDrag = OnHomeDrag;
        swipe.onEnd = OnHomeDragEnd;

        pagesStrip = CreateRect("Strip", viewport);
        pagesStrip.anchorMin = Vector2.zero;
        pagesStrip.anchorMax = new Vector2(0f, 1f);
        pagesStrip.pivot = Vector2.zero;
        pagesStrip.sizeDelta = new Vector2(ScreenWidth * PageCount, 0f);
        pagesStrip.anchoredPosition = Vector2.zero;

        AppDef[][] pages =
        {
            new[]
            {
                App("Messages", new Color(0.30f, 0.80f, 0.40f)),
                App("Mail", new Color(0.20f, 0.55f, 0.95f)),
                App("Notes", new Color(0.98f, 0.80f, 0.25f)),
                App("Settings", new Color(0.55f, 0.62f, 0.72f)),
                App("Weather", new Color(0.35f, 0.75f, 0.98f)),
                App("Music", new Color(0.95f, 0.40f, 0.62f)),
                App("Camera", new Color(0.25f, 0.28f, 0.34f), locked: true),
                App("Maps", new Color(0.30f, 0.72f, 0.62f), locked: true),
            },
            new[]
            {
                App("Browser", new Color(0.25f, 0.65f, 0.90f)),
                App("Clock", new Color(0.15f, 0.18f, 0.24f)),
                App("Gallery", new Color(0.95f, 0.60f, 0.25f), locked: true),
                App("Contacts", new Color(0.60f, 0.50f, 0.90f)),
            },
        };

        for (int p = 0; p < pages.Length; p++)
        {
            RectTransform page = CreateRect($"Page{p + 1}", pagesStrip);
            page.anchorMin = Vector2.zero;
            page.anchorMax = new Vector2(0f, 1f);
            page.pivot = Vector2.zero;
            page.sizeDelta = new Vector2(ScreenWidth, 0f);
            page.anchoredPosition = new Vector2(p * ScreenWidth, 0f);

            GridLayoutGroup grid = page.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(21, 21, 28, 0);
            grid.cellSize = new Vector2(72f, 96f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            foreach (AppDef app in pages[p])
                BuildAppTile(page, app);
        }

        RectTransform dots = CreateRect("Dots", home);
        dots.anchorMin = new Vector2(0.5f, 0f);
        dots.anchorMax = new Vector2(0.5f, 0f);
        dots.sizeDelta = new Vector2(60f, 20f);
        dots.anchoredPosition = new Vector2(0f, 20f);
        HorizontalLayoutGroup dotLayout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotLayout.spacing = 10f;
        dotLayout.childAlignment = TextAnchor.MiddleCenter;
        dotLayout.childControlWidth = false;
        dotLayout.childControlHeight = false;
        for (int i = 0; i < PageCount; i++)
        {
            RectTransform dot = CreatePanel("Dot", dots, Ink, 5f);
            dot.sizeDelta = new Vector2(10f, 10f);
            pageDots.Add(dot.GetComponent<Image>());
        }
        GoToPage(0);
    }

    private AppDef App(string name, Color color, bool locked = false)
    {
        AppDef app = new AppDef { name = name, color = color, locked = locked };
        if (name == "Messages") messagesApp = app;
        allApps.Add(app);
        return app;
    }

    private void BuildAppTile(RectTransform page, AppDef app)
    {
        RectTransform cell = CreateRect(app.name, page);
        Button button = cell.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        Image hit = cell.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        button.onClick.AddListener(() => OnAppTileClicked(app));

        RectTransform tile = CreatePanel("Tile", cell, app.color, 18f);
        tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 1f);
        tile.pivot = new Vector2(0.5f, 1f);
        tile.sizeDelta = new Vector2(68f, 68f);
        tile.anchoredPosition = Vector2.zero;
        tile.GetComponent<Image>().raycastTarget = false;
        app.tile = tile;

        RectTransform shine = CreatePanel("Shine", tile, new Color(1f, 1f, 1f, 0.35f), 15f);
        shine.anchorMin = new Vector2(0f, 0.5f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(4f, 0f);
        shine.offsetMax = new Vector2(-4f, -3f);
        shine.GetComponent<Image>().raycastTarget = false;

        TextMeshProUGUI letter = CreateText(tile, app.locked ? "x" : app.name.Substring(0, 1), 32f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(letter.rectTransform);

        TextMeshProUGUI label = CreateText(cell, app.name, 14f, Ink, TextAlignmentOptions.Top, FontStyles.Normal);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = new Vector2(1f, 0f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.sizeDelta = new Vector2(20f, 24f);
        label.rectTransform.anchoredPosition = Vector2.zero;
        if (app.locked) label.alpha = 0.55f;
    }

    private void BuildAppRoot(RectTransform screen)
    {
        appRoot = CreatePanel("App", screen, new Color(0.95f, 0.97f, 1f), 0f);
        Stretch(appRoot);
        appGroup = appRoot.gameObject.AddComponent<CanvasGroup>();

        RectTransform header = CreateRect("Header", appRoot);
        AnchorTop(header, AppHeaderHeight, -StatusBarHeight);
        appTitle = CreateText(header, "", 20f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(appTitle.rectTransform);

        RectTransform backButton = CreateRect("Back", header);
        backButton.anchorMin = new Vector2(0f, 0f);
        backButton.anchorMax = new Vector2(0f, 1f);
        backButton.pivot = new Vector2(0f, 0.5f);
        backButton.sizeDelta = new Vector2(96f, 0f);
        backButton.anchoredPosition = new Vector2(8f, 0f);
        backButton.gameObject.AddComponent<Image>().color = Color.clear;
        backButton.gameObject.AddComponent<Button>().onClick.AddListener(GoBack);
        TextMeshProUGUI backLabel = CreateText(backButton, "< Back", 18f, Aqua, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Stretch(backLabel.rectTransform, 8f, 0f);

        RectTransform bodyRoot = CreateRect("Body", appRoot);
        Stretch(bodyRoot);
        bodyRoot.offsetMin = new Vector2(0f, HomeBarHeight);
        bodyRoot.offsetMax = new Vector2(0f, -(StatusBarHeight + AppHeaderHeight));

        foreach (AppDef app in allApps)
        {
            if (app.locked) continue;
            app.panel = app == messagesApp ? BuildMessages(bodyRoot)
                : app.name == "Settings" ? BuildSettings(bodyRoot)
                : BuildGenericApp(bodyRoot, app);
            app.panel.gameObject.SetActive(false);
        }

        conversationPanel = CreateScrollList("Conversation", bodyRoot, out conversationContent);
        conversationPanel.gameObject.SetActive(false);

        appRoot.gameObject.SetActive(false);
    }

    private RectTransform BuildMessages(RectTransform bodyRoot)
    {
        RectTransform panel = CreateScrollList("Messages", bodyRoot, out RectTransform content);
        for (int i = 0; i < 12; i++)
        {
            string sender = Senders[i % Senders.Length];
            string preview = Previews[(i * 3 + 1) % Previews.Length];
            string label = $"<b>{sender}</b>\n<size=80%><color=#{ColorUtility.ToHtmlStringRGB(InkSoft)}>{preview}</color></size>";
            CreateRow(content, label, 72f, Ink, () => OpenConversation(sender, preview));
        }
        return panel;
    }

    private RectTransform BuildSettings(RectTransform bodyRoot)
    {
        RectTransform panel = CreateScrollList("Settings", bodyRoot, out RectTransform content);
        CreateToggleRow(content, "Wi-Fi", true);
        CreateToggleRow(content, "Bluetooth", false);
        CreateToggleRow(content, "Airplane Mode", false);
        CreateToggleRow(content, "Location Services", true);
        CreateToggleRow(content, "Silent Mode", false);
        CreateToggleRow(content, "Vibrate on Ring", true);
        CreateToggleRow(content, "Case Notes Sync", true);
        foreach (string label in new[] { "Wallpaper", "Display & Brightness", "Battery", "Privacy", "About This Phone" })
            CreateRow(content, label, 60f, Ink, () => Play(l => l.tap));

        RectTransform erase = null;
        erase = CreateRow(content, "Erase All Data", 60f, Danger, () =>
        {
            Play(l => l.error);
            Shake(erase.GetChild(0) as RectTransform);
        });
        return panel;
    }

    private RectTransform BuildGenericApp(RectTransform bodyRoot, AppDef app)
    {
        RectTransform panel = CreateScrollList(app.name, bodyRoot, out RectTransform content);
        for (int i = 1; i <= 14; i++)
            CreateRow(content, $"{app.name} item {i}", 60f, Ink, () => Play(l => l.tap));
        return panel;
    }

    private void AddBubble(string text, bool mine)
    {
        RectTransform row = CreateRect("Bubble", conversationContent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

        RectTransform bubble = CreatePanel("Body", row, mine ? Aqua : Color.white, 18f);
        bubble.anchorMin = new Vector2(mine ? 1f : 0f, 0f);
        bubble.anchorMax = new Vector2(mine ? 1f : 0f, 1f);
        bubble.pivot = new Vector2(mine ? 1f : 0f, 0.5f);
        bubble.sizeDelta = new Vector2(250f, 0f);
        bubble.anchoredPosition = Vector2.zero;
        TextMeshProUGUI label = CreateText(bubble, text, 16f, mine ? Color.white : Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        Stretch(label.rectTransform, 14f, 4f);
    }

    private void BuildBanner(RectTransform screen)
    {
        banner = CreatePanel("Banner", screen, new Color(1f, 1f, 1f, 0.97f), 20f);
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
        banner.pivot = new Vector2(0.5f, 1f);
        banner.sizeDelta = new Vector2(ScreenWidth - 24f, 76f);
        banner.anchoredPosition = new Vector2(0f, 120f);
        banner.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        RectTransform icon = CreatePanel("Icon", banner, new Color(0.30f, 0.80f, 0.40f), 10f);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.sizeDelta = new Vector2(40f, 40f);
        icon.anchoredPosition = new Vector2(14f, 0f);
        icon.GetComponent<Image>().raycastTarget = false;

        bannerText = CreateText(banner, "", 16f, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        Stretch(bannerText.rectTransform);
        bannerText.rectTransform.offsetMin = new Vector2(66f, 6f);
        bannerText.rectTransform.offsetMax = new Vector2(-14f, -6f);
    }

    private void BuildSoundboard(RectTransform root)
    {
        RectTransform panel = CreatePanel("Soundboard", root, new Color(1f, 1f, 1f, 0.82f), 32f);
        Place(panel, new Vector2(360f, 0f), new Vector2(580f, 860f));

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(panel, "Phone SFX", 34f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        TextMeshProUGUI hint = CreateText(panel,
            "Click a sound to audition it. On the phone: tap apps, drag the home screen sideways, " +
            "scroll lists, flip toggles in Settings. Camera, Maps and Gallery are locked. Esc = back.\n" +
            "Tune the banks in Resources/PhoneSfxLibrary.",
            15f, InkSoft, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        lastPlayedText = CreateText(panel, "Last played: -", 17f, Ink, TextAlignmentOptions.Left, FontStyles.Normal);
        lastPlayedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        if (library == null) return;

        foreach (KeyValuePair<string, SfxBank> entry in library.AllBanks())
        {
            SfxBank bank = entry.Value;
            string clipInfo = !bank.HasAnyClip ? "(no clip)"
                : bank.clips.Count > 1 ? $"{bank.clips.Count} clips"
                : bank.clips[0].Clip.name;
            string label = $"<b>{entry.Key}</b>  <size=80%><color=#{ColorUtility.ToHtmlStringRGB(InkSoft)}>{clipInfo}</color></size>";
            CreateRow(panel, label, 50f, Ink, () => Play(bank));
        }

        RectTransform actions = CreateRect("Actions", panel);
        actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 12f;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = true;

        CreateActionButton(actions, "Incoming Message", ShowIncomingMessage);
        CreateActionButton(actions, "Play All", () =>
        {
            if (playAllRoutine != null) StopCoroutine(playAllRoutine);
            playAllRoutine = StartCoroutine(PlayAll());
        });
    }

    //==================== Build: widgets ====================

    private RectTransform CreateScrollList(string name, RectTransform parent, out RectTransform content)
    {
        RectTransform view = CreateRect(name, parent);
        Stretch(view);
        view.gameObject.AddComponent<RectMask2D>();
        view.gameObject.AddComponent<Image>().color = Color.clear;

        content = CreateRect("Content", view);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 16);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.viewport = view;
        scroll.content = content;
        scroll.scrollSensitivity = 30f;
        AttachScrollTicks(scroll);
        return view;
    }

    /// <summary>A rounded, full-width clickable row. Child 0 is the visual so it can be shaken without fighting the layout.</summary>
    private RectTransform CreateRow(RectTransform parent, string label, float height, Color textColor, Action onClick)
    {
        RectTransform row = CreateRect("Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        row.gameObject.AddComponent<Image>().color = Color.clear;
        Button button = row.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        RectTransform visual = CreatePanel("Visual", row, RowColor, 14f);
        Stretch(visual);
        visual.GetComponent<Image>().raycastTarget = false;
        TextMeshProUGUI text = CreateText(visual, label, 17f, textColor, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        Stretch(text.rectTransform, 18f, 4f);

        button.onClick.AddListener(() =>
        {
            Pop(visual);
            onClick?.Invoke();
        });
        return row;
    }

    private void CreateToggleRow(RectTransform parent, string label, bool startOn)
    {
        bool on = startOn;
        RectTransform knob = null;
        Image track = null;

        void Apply(bool animate)
        {
            track.color = on ? Aqua : new Color(0.75f, 0.79f, 0.85f);
            float x = on ? 12f : -12f;
            knob.DOKill();
            if (animate) knob.DOAnchorPosX(x, 0.15f).SetEase(Ease.OutQuad);
            else knob.anchoredPosition = new Vector2(x, 0f);
        }

        RectTransform row = CreateRow(parent, label, 60f, Ink, () =>
        {
            on = !on;
            Play(l => on ? l.toggleOn : l.toggleOff);
            Apply(true);
        });

        RectTransform trackRect = CreatePanel("Switch", row.GetChild(0) as RectTransform, Color.white, 16f);
        trackRect.anchorMin = trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(56f, 32f);
        trackRect.anchoredPosition = new Vector2(-16f, 0f);
        track = trackRect.GetComponent<Image>();
        track.raycastTarget = false;

        knob = CreatePanel("Knob", trackRect, Color.white, 13f);
        Place(knob, Vector2.zero, new Vector2(26f, 26f));
        knob.GetComponent<Image>().raycastTarget = false;
        Apply(false);
    }

    private void CreateActionButton(RectTransform parent, string label, Action onClick)
    {
        RectTransform button = CreatePanel(label, parent, Aqua, 18f);
        RectTransform shine = CreatePanel("Shine", button, new Color(1f, 1f, 1f, 0.3f), 14f);
        shine.anchorMin = new Vector2(0f, 0.5f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(4f, 0f);
        shine.offsetMax = new Vector2(-4f, -3f);
        shine.GetComponent<Image>().raycastTarget = false;
        TextMeshProUGUI text = CreateText(button, label, 18f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform);

        Button b = button.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() =>
        {
            Pop(button);
            onClick();
        });
    }

    //==================== Build: primitives ====================

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color, float cornerRadius)
    {
        RectTransform rt = CreateRect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        if (cornerRadius > 0f)
        {
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SpriteRadius / cornerRadius;
        }
        return rt;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, FontStyles style)
    {
        RectTransform rt = CreateRect("Text", parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RawImage CreateGradient(string name, RectTransform parent, Color top, Color bottom)
    {
        const int height = 64;
        Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        for (int y = 0; y < height; y++)
            tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
        tex.Apply();

        RectTransform rt = CreateRect(name, parent);
        RawImage image = rt.gameObject.AddComponent<RawImage>();
        image.texture = tex;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>White rounded-rect sprite with 9-slice borders; tint it and set pixelsPerUnitMultiplier for the corner size.</summary>
    private static Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte alpha = (byte)(Mathf.Clamp01(radius - dist + 0.5f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static void Stretch(RectTransform rt, float inset = 0f) => Stretch(rt, inset, inset);

    private static void Stretch(RectTransform rt, float insetX, float insetY)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(insetX, insetY);
        rt.offsetMax = new Vector2(-insetX, -insetY);
    }

    private static void Place(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }

    private static void AnchorTop(RectTransform rt, float height, float y)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, y);
    }
}
