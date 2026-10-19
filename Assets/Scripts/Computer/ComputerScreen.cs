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

    [Header("Taskbar")]
    // whoever this pc belongs to, shown signed in on the taskbar once logged in
    public Character user;
    public GameObject userBadge;
    public Image userPicture;
    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userTitleText;

    private string password = "12aa23dca19z88";

    private ComputerSfxLibrary sounds;
    private Vector2 screenPos;
    [SerializeField] private bool loggedIn = false;
    private Sequence sequence;
    private float fanVolume;
    private int openedFrame;
    private Color insertCDColour;

    private static readonly Color DisabledButtonColour = new Color(0.6f, 0.63f, 0.68f);

    private void Awake()
    {
        sounds = ComputerSfxLibrary.Load();
        screenPos = screen.anchoredPosition;

        fanVolume = fanSource.volume;

        enterButton.onClick.AddListener(TryLogin);
        passwordField.onSubmit.AddListener(_ => TryLogin());
        passwordField.onValueChanged.AddListener(OnPasswordTyped);
        powerButton.onClick.AddListener(Close);
        if (user != null)
        {
            userNameText.text = user.characterName;
            userTitleText.text = user.jobTitle;
            userPicture.sprite = user.headshot;
        }

        if(insertCDButton != null)
        {
            insertCDButton.onClick.AddListener(CheckCD);
            insertCDColour = insertCDButton.targetGraphic.color;
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
        if (userBadge != null)
            userBadge.SetActive(loggedIn);
        passwordField.text = "";
        wrongPasswordText.SetActive(false);
        RefreshInsertCD();

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
            CDPopup.PlayInserted();
            insertCDButton.gameObject.SetActive(false);
            //TODO: Add a loading icon and wait then show? 
            ReportGameObject.gameObject.SetActive(true);
            ReportGameObject.transform.SetAsLastSibling();
            GameManager.Instance.ActivateUsedCDFlag();
            GameMusicGuy.SetMood(GameMood.Yellow);
        }   
    }

    // greyed out until there's a CD to put in
    private void RefreshInsertCD()
    {
        if (insertCDButton == null)
            return;

        bool canInsert = GameManager.Instance.HasCD() && !GameManager.Instance.UsedCD();
        insertCDButton.interactable = canInsert;
        insertCDButton.targetGraphic.color = canInsert ? insertCDColour : DisabledButtonColour;
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
            if (userBadge != null)
                userBadge.SetActive(true);
            RefreshInsertCD();
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
