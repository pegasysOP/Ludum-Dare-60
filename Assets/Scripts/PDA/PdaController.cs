using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PdaController : MonoBehaviour
{
    public Button openButton;
    public Button closeButton;
    public RectTransform rect;
    public CanvasGroup canvasGroup;
    public HomescreenApp[] apps;
    public Image notificationLight;
    public CanvasGroup openGroup;
    public CanvasGroup closedGroup;

    // top left of the phone from the bottom left of the screen
    public Vector2 openPosition = new Vector2(-120f, 1100f);
    public Vector2 closedPosition = new Vector2(16f, 360f);
    public float closedScale = 0.38f;

    private bool open = false;
    private bool putAway = false;
    private HomescreenApp openApp;
    private InputAction escapeAction;
    private PhoneSfxLibrary sounds;
    private List<IAppNotification> notifiers = new List<IAppNotification>();
    private Tween lightPulse;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();

        openButton.onClick.AddListener(() => SetOpen(true));
        closeButton.onClick.AddListener(() => SetOpen(false));

        foreach (HomescreenApp app in apps)
        {
            app.button.onClick.AddListener(() => OpenApp(app));
            app.fullscreenApp.backButton.onClick.AddListener(Back);

            // apps start hidden, so hook up any that can light their icon now
            IAppNotification notifier = app.fullscreenApp.GetComponentInChildren<IAppNotification>(true);
            if (notifier != null)
            {
                notifier.NotificationChanged += () =>
                {
                    app.SetNotification(notifier.HasNotification);
                    RefreshNotificationLight();
                };
                app.SetNotification(notifier.HasNotification);
                notifiers.Add(notifier);
            }
        }

        RefreshNotificationLight();

        GetComponentInChildren<SusPXApp>(true).onReport.AddListener(_ => PutAway());

        rect.anchoredPosition = closedPosition;
        rect.localScale = Vector3.one * closedScale;
        openGroup.alpha = 0f;
        openGroup.blocksRaycasts = false;
        closedGroup.alpha = 1f;
        closedGroup.blocksRaycasts = true;
    }

    private void Start()
    {
        escapeAction = InputSystem.actions.FindAction("Escape");
    }

    private void Update()
    {
        if (putAway)
            return;

        if (escapeAction != null && escapeAction.triggered)
        {
            if (openApp != null)
                Back();
            else
                ToggleOpen();
        }
    }

    public void ToggleOpen()
    {
        SetOpen(!open);
    }

    public void SetOpen(bool opening)
    {
        if (opening == open)
            return;

        // phone down sound covers the app closing too
        if (!opening)
            CloseApp(false);

        (opening ? sounds.modeOpen : sounds.modeClose).Play();
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetPhoneMode(opening);

        rect.DOKill();
        openGroup.DOKill();
        closedGroup.DOKill();

        Ease ease = opening ? Ease.OutBack : Ease.OutExpo;
        rect.DOAnchorPos(opening ? openPosition : closedPosition, 0.45f).SetEase(ease);
        rect.DOScale(opening ? 1f : closedScale, 0.45f).SetEase(ease);

        // closed lid sits on top, so fade it rather than the open phone to avoid seeing through both
        if (opening)
        {
            openGroup.alpha = 1f;
            closedGroup.DOFade(0f, 0.15f);
        }
        else
            closedGroup.DOFade(1f, 0.15f).OnComplete(() => openGroup.alpha = 0f);
        openGroup.blocksRaycasts = opening;
        closedGroup.blocksRaycasts = !opening;
        open = opening;
    }

    // led on the frame breathes while any app has something new
    private void RefreshNotificationLight()
    {
        bool lit = notifiers.Exists(notifier => notifier.HasNotification);
        if (lit == notificationLight.gameObject.activeSelf)
            return;

        lightPulse?.Kill();
        notificationLight.gameObject.SetActive(lit);
        if (!lit)
            return;

        Color colour = notificationLight.color;
        colour.a = 1f;
        notificationLight.color = colour;
        lightPulse = notificationLight.DOFade(0.1f, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    public void SetVisible(bool visible)
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(visible ? 1f : 0f, 0.3f);
        canvasGroup.blocksRaycasts = visible;
    }

    // once someone's reported the phone closes and fades away for good
    private void PutAway()
    {
        putAway = true;
        SetOpen(false);

        canvasGroup.DOKill();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0f, 0.6f).SetDelay(0.6f);
    }

    public void OpenApp(HomescreenApp app)
    {
        if (openApp == app)
            return;

        CloseApp(false);

        if (!open)
            SetOpen(true);

        sounds.appOpen.Play();
        openApp = app;
        app.fullscreenApp.Open(app.rect);
    }

    public void Back()
    {
        if (openApp != null && openApp.fullscreenApp.Back())
            return;

        CloseApp();
    }

    public void CloseApp(bool playSound = true)
    {
        if (openApp == null)
            return;

        if (playSound)
            sounds.appClose.Play();

        openApp.fullscreenApp.Close(openApp.rect);
        openApp = null;
    }
}
