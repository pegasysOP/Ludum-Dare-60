using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// a pc's desktop: icons that open their windows, and a mail icon that flashes while anything in it is unread
public class ComputerDesktop : MonoBehaviour
{
    [Serializable]
    public class App
    {
        public Button icon;
        public GameObject window;
    }

    public App[] apps;
    // every window on this desktop, each shut by the close button at the same index
    public GameObject[] windows;
    public Button[] closeButtons;
    public DesktopMail mail;
    // leave empty on a pc whose mail never has anything waiting
    public CanvasGroup mailNotification;

    public event Action MailRead;

    public bool HasUnreadMail => mail != null && mail.HasUnread;

    private Tween flash;

    private void Awake()
    {
        foreach (App app in apps)
        {
            GameObject window = app.window;
            app.icon.onClick.AddListener(() => Open(window));
        }

        for (int i = 0; i < closeButtons.Length; i++)
        {
            GameObject window = windows[i];
            closeButtons[i].onClick.AddListener(() => window.SetActive(false));
        }

        if (mail != null)
            mail.AllRead += OnMailRead;
    }

    // the desktop starts clear every time the computer is switched on
    private void OnEnable()
    {
        foreach (GameObject window in windows)
            window.SetActive(false);

        flash?.Kill();
        if (mailNotification == null)
            return;

        mailNotification.gameObject.SetActive(HasUnreadMail);
        if (!HasUnreadMail)
            return;

        mailNotification.alpha = 1f;
        flash = mailNotification.DOFade(0.2f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    public void Open(GameObject window)
    {
        window.SetActive(true);
        window.transform.SetAsLastSibling();
    }

    // only once the unread email itself has been opened, just opening the app isn't enough
    private void OnMailRead()
    {
        flash?.Kill();
        if (mailNotification != null)
            mailNotification.gameObject.SetActive(false);
        MailRead?.Invoke();
    }
}
