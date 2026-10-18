using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerScreen : MonoBehaviour
{
    public RectTransform screen;
    public CanvasGroup screenGroup;
    public CanvasGroup blackout;
    public GameObject login;
    public RectTransform loginCard;
    public TMP_InputField passwordField;
    public Button enterButton;
    public GameObject wrongPasswordText;
    public GameObject desktop;
    public Button powerButton;

    public string password = "password"; //TODO: Change this to be based on the clue

    private ComputerSfxLibrary sounds;
    private Vector2 screenPos;
    private bool loggedIn = false;
    private Sequence sequence;

    private void Awake()
    {
        sounds = ComputerSfxLibrary.Load();
        screenPos = screen.anchoredPosition;

        enterButton.onClick.AddListener(TryLogin);
        passwordField.onSubmit.AddListener(_ => TryLogin());
        passwordField.onValueChanged.AddListener(OnPasswordTyped);
        powerButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (gameObject.activeSelf)
            return;

        gameObject.SetActive(true);
        sequence?.Kill();

        screen.anchoredPosition = screenPos;
        blackout.alpha = 0f;
        screenGroup.alpha = 0f;
        screenGroup.blocksRaycasts = true;

        login.SetActive(!loggedIn);
        desktop.SetActive(loggedIn);
        passwordField.text = "";
        wrongPasswordText.SetActive(false);

        sounds.boot.Play();
        sequence = DOTween.Sequence()
            .Append(screenGroup.DOFade(1f, 0.4f))
            .OnComplete(() =>
            {
                if (!loggedIn)
                    passwordField.ActivateInputField();
            });
    }

    public void Close()
    {
        sequence?.Kill();
        screenGroup.blocksRaycasts = false;
        sounds.shutdown.Play();

        // monitor goes black, then the whole screen flies off the top
        sequence = DOTween.Sequence()
            .Append(blackout.DOFade(1f, 0.25f))
            .Append(screen.DOAnchorPosY(screenPos.y + screen.rect.height + 200f, 0.45f).SetEase(Ease.InBack))
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnPasswordTyped(string text)
    {
        // only real typing, not the field being cleared
        if (passwordField.isFocused)
            sounds.keyPress.Play();
    }

    private void TryLogin()
    {
        sounds.submit.Play();

        if (passwordField.text == password)
        {
            loggedIn = true;
            sounds.startup.Play();
            login.SetActive(false);
            desktop.SetActive(true);
            return;
        }

        sounds.error.Play();
        wrongPasswordText.SetActive(true);
        passwordField.text = "";
        passwordField.ActivateInputField();

        loginCard.DOKill(true);
        loginCard.DOShakeAnchorPos(0.4f, new Vector2(16f, 0f), 20, 0f);
    }
}
