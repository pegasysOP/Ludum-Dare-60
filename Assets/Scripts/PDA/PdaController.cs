using System;
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

    private bool open = false;
    private HomescreenApp openApp;
    private InputAction escapeAction;
    private PhoneSfxLibrary sounds;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();

        sideButton.onClick.AddListener(ToggleOpen);

        foreach (HomescreenApp app in apps)
        {
            app.button.onClick.AddListener(() => OpenApp(app));
            app.fullscreenApp.backButton.onClick.AddListener(Back);
        }
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
