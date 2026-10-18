using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PdaController : MonoBehaviour
{
    public Button sideButton;
    public TextMeshProUGUI sideText;
    public RectTransform rect;
    public CanvasGroup canvasGroup;
    public HomescreenApp[] apps;
    public Image notificationLight;

    private bool open = false;
    private HomescreenApp openApp;
    private InputAction escapeAction;
    private PhoneSfxLibrary sounds;
    private List<IAppNotification> notifiers = new List<IAppNotification>();
    private Tween lightPulse;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();

        sideButton.onClick.AddListener(ToggleOpen);

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
    }

    private void Start()
    {
        escapeAction = InputSystem.actions.FindAction("Escape");
    }

    private void Update()
    {
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
        rect.DOAnchorPosX(opening ? 50 : -550, 0.5f).SetEase(Ease.OutExpo);
        sideText.text = opening ? "<" : ">";
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
