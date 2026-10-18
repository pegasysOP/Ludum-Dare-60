using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public AudioSource fanSource;
    public Button insertCDButton;
    public GameObject ReportGameObject;

    public string password = "password"; //TODO: Change this to be based on the clue

    private ComputerSfxLibrary sounds;
    private Vector2 screenPos;
    [SerializeField] private bool loggedIn = false;
    private Sequence sequence;
    private float fanVolume;
    private int openedFrame;

    private void Awake()
    {
        sounds = ComputerSfxLibrary.Load();
        screenPos = screen.anchoredPosition;

        fanVolume = fanSource.volume;

        enterButton.onClick.AddListener(TryLogin);
        passwordField.onSubmit.AddListener(_ => TryLogin());
        passwordField.onValueChanged.AddListener(OnPasswordTyped);
        powerButton.onClick.AddListener(Close);
        if(insertCDButton != null)
        {
            insertCDButton.onClick.AddListener(CheckCD);
        }
    }

    public void Open()
    {
        if (gameObject.activeSelf)
            return;

        gameObject.SetActive(true);
        openedFrame = Time.frameCount;
        sequence?.Kill();

        screen.anchoredPosition = screenPos;
        blackout.alpha = 0f;
        screenGroup.alpha = 0f;
        screenGroup.blocksRaycasts = true;

        login.SetActive(!loggedIn);
        desktop.SetActive(loggedIn);
        passwordField.text = "";
        wrongPasswordText.SetActive(false);

        // fan source plays on awake, so it starts whenever this canvas turns on
        sounds.wake.Play();
        sequence = DOTween.Sequence()
            .Append(screenGroup.DOFade(1f, 0.4f))
            .OnComplete(() =>
            {
                if (!loggedIn)
                    passwordField.ActivateInputField();
            });
    }

    private void Update()
    {
        // every press of the real mouse clicks, whatever it lands on. Skip the click that opened the screen
        if (screenGroup.blocksRaycasts && Time.frameCount != openedFrame
            && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            sounds.click.Play();
    }

    public void Close()
    {
        // already shutting down
        if (!screenGroup.blocksRaycasts)
            return;

        sequence?.Kill();
        screenGroup.blocksRaycasts = false;
        sounds.shutdown.Play();

        const float blackoutDuration = 0.25f;
        const float flyDuration = 0.45f;

        // monitor goes black, then the whole screen flies off the top
        sequence = DOTween.Sequence()
            .Append(blackout.DOFade(1f, blackoutDuration))
            .Append(screen.DOAnchorPosY(screenPos.y + screen.rect.height + 200f, flyDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                fanSource.volume = fanVolume;
            });

        // fan winds down over the whole shut down
        fanSource.DOFade(0f, blackoutDuration + flyDuration);
    }

    public void CheckCD()
    {
        if (!GameManager.Instance.HasCD())
        {
            sounds.error.Play();
        }
        else
        {
            Debug.Log("CD inserted");
            sounds.cdInsert.Play();
            insertCDButton.gameObject.SetActive(false);
            //TODO: Add a loading icon and wait then show? 
            ReportGameObject.gameObject.SetActive(true);
        }   
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
            sounds.boot.Play();
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
