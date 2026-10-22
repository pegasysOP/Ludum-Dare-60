using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PdaController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button openButton;
    public Button closeButton;
    public RectTransform rect;
    public CanvasGroup canvasGroup;
    public HomescreenApp[] apps;
    public CanvasGroup notificationLight;
    public CanvasGroup openGroup;
    public CanvasGroup closedGroup;

    // top left of the phone from the bottom left of the screen
    public Vector2 openPosition = new Vector2(-120f, 1100f);
    public Vector2 closedPosition = new Vector2(16f, 360f);
    public float closedScale = 0.38f;
    // closed phone peeks up this far on hover to show it can be opened
    public float closedHoverLift = 16f;
    // seconds between buzzes while something's waiting
    public float buzzInterval = 4f;

    private bool open = false;
    private bool putAway = false;
    private HomescreenApp openApp;
    private InputAction escapeAction;
    private PhoneSfxLibrary sounds;
    private List<IAppNotification> notifiers = new List<IAppNotification>();
    private Tween lightPulse;
    private Tween moveTween;
    private Tween hoverLift;
    private Sequence buzzLoop;

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
                bool had = notifier.HasNotification;
                notifier.NotificationChanged += () =>
                {
                    bool has = notifier.HasNotification;
                    app.SetNotification(has);
                    if (has && !had && !notifier.KeepsBuzzing)
                        DOVirtual.DelayedCall(0.3f, Buzz).SetLink(gameObject);
                    had = has;
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
        moveTween = rect.DOAnchorPos(opening ? openPosition : closedPosition, 0.45f).SetEase(ease);
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

    // led on the frame breathes while any app has something new, and the phone keeps buzzing for the apps that want it
    private void RefreshNotificationLight()
    {
        bool lit = notifiers.Exists(notifier => notifier.HasNotification);
        bool nagging = notifiers.Exists(notifier => notifier.HasNotification && notifier.KeepsBuzzing);

        if (nagging != (buzzLoop != null && buzzLoop.IsActive()))
        {
            buzzLoop?.Kill();
            buzzLoop = null;
            if (nagging)
                buzzLoop = DOTween.Sequence()
                    .AppendInterval(0.3f)
                    .AppendCallback(Buzz)
                    .AppendInterval(buzzInterval)
                    .SetLoops(-1)
                    .SetLink(gameObject);
        }

        if (lit == notificationLight.gameObject.activeSelf)
            return;

        lightPulse?.Kill();
        notificationLight.gameObject.SetActive(lit);
        if (!lit)
            return;

        // quick swell, slow fade, then a beat of dark like a real notification led
        notificationLight.alpha = 0f;
        lightPulse = DOTween.Sequence()
            .Append(notificationLight.DOFade(1f, 0.4f).SetEase(Ease.OutSine))
            .Append(notificationLight.DOFade(0f, 1f).SetEase(Ease.InSine))
            .AppendInterval(0.6f)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    // two short rattles like a phone on vibrate
    private void Buzz()
    {
        if (open || putAway)
            return;

        RectTransform closedRect = (RectTransform)closedGroup.transform;
        closedRect.DOComplete();
        sounds.vibrate.Play();
        DOTween.Sequence()
            .Append(closedRect.DOShakeAnchorPos(0.25f, new Vector2(10f, 3f), 40, 90f))
            .Join(closedRect.DOShakeRotation(0.25f, new Vector3(0f, 0f, 1.2f), 40, 90f))
            .AppendInterval(0.12f)
            .Append(closedRect.DOShakeAnchorPos(0.25f, new Vector2(10f, 3f), 40, 90f))
            .Join(closedRect.DOShakeRotation(0.25f, new Vector3(0f, 0f, 1.2f), 40, 90f))
            .SetTarget(closedRect)
            .SetLink(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverLift(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverLift(false);
    }

    private void SetHoverLift(bool lifted)
    {
        // leave the phone alone while it's open or still flipping shut
        if (open || putAway || (moveTween != null && moveTween.IsActive()))
            return;

        hoverLift?.Kill();
        hoverLift = rect.DOAnchorPos(closedPosition + Vector2.up * (lifted ? closedHoverLift : 0f), 0.2f)
            .SetEase(lifted ? Ease.OutBack : Ease.OutQuad);
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
