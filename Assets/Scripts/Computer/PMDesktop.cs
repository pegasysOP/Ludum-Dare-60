using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// the product manager's desktop: icons that open their windows, and mail that flashes until it's read
public class PMDesktop : MonoBehaviour
{
    public Button cdDriveIcon;
    public GameObject cdDriveWindow;
    // what's on the cd, opened straight from the drive icon once it's been loaded
    public GameObject cdContentsWindow;
    // only clickable once there's a cd to load, so a click always means it went in
    public Button loadCDButton;
    public Button mailIcon;
    public GameObject mailWindow;
    public CanvasGroup mailNotification;
    // every window on this desktop, each shut by the close button at the same index
    public GameObject[] windows;
    public Button[] closeButtons;

    public event Action MailOpened;

    public bool MailRead { get; private set; }

    private Tween flash;

    private void Awake()
    {
        cdDriveIcon.onClick.AddListener(OnCDDriveClicked);
        mailIcon.onClick.AddListener(OnMailClicked);
        loadCDButton.onClick.AddListener(() => cdDriveWindow.SetActive(false));

        for (int i = 0; i < closeButtons.Length; i++)
        {
            GameObject window = windows[i];
            closeButtons[i].onClick.AddListener(() => window.SetActive(false));
        }
    }

    // the desktop starts clear every time the computer is switched on
    private void OnEnable()
    {
        foreach (GameObject window in windows)
            window.SetActive(false);

        flash?.Kill();
        mailNotification.gameObject.SetActive(!MailRead);
        if (MailRead)
            return;

        mailNotification.alpha = 1f;
        flash = mailNotification.DOFade(0.2f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    private void OnCDDriveClicked()
    {
        Open(GameManager.Instance.UsedCD() ? cdContentsWindow : cdDriveWindow);
    }

    private void OnMailClicked()
    {
        Open(mailWindow);
        if (MailRead)
            return;

        MailRead = true;
        flash?.Kill();
        mailNotification.gameObject.SetActive(false);
        MailOpened?.Invoke();
    }

    private void Open(GameObject window)
    {
        window.SetActive(true);
        window.transform.SetAsLastSibling();
    }
}
