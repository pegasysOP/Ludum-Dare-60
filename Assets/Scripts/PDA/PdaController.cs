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

    private void Awake()
    {
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
        if (!opening)
            CloseApp();

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

        CloseApp();

        if (!open)
            SetOpen(true);

        openApp = app;
        app.fullscreenApp.Open(app.rect);
    }

    public void Back()
    {
        if (openApp != null && openApp.fullscreenApp.Back())
            return;

        CloseApp();
    }

    public void CloseApp()
    {
        if (openApp == null)
            return;

        openApp.fullscreenApp.Close(openApp.rect);
        openApp = null;
    }
}
