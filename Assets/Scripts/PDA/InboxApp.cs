using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class InboxApp : MonoBehaviour, IAppContent, IAppNotification
{
    public EmailRow rowTemplate;
    public RectTransform list;
    public GameObject emailView;
    public RectTransform emailCard;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI fromText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI bodyText;
    public List<Email> emails = new List<Email>();

    public event Action NotificationChanged;

    private PhoneSfxLibrary sounds;
    private Vector2 emailCardPos;
    private bool built = false;

    public bool HasNotification => emails.Exists(email => !email.read);

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();
        emailCardPos = emailCard.anchoredPosition;

        foreach (Email email in emails)
            CreateRow(email);

        built = true;
    }

    // new mail lands unread at the top, works even if the inbox hasn't been opened yet
    public void AddEmail(string from, string subject, string body, string date = "Just now")
    {
        Email email = new Email { from = from, subject = subject, body = body, date = date };
        emails.Insert(0, email);

        if (built)
            CreateRow(email).transform.SetAsFirstSibling();

        if (sounds == null)
            sounds = PhoneSfxLibrary.Load();

        sounds.notifMessage.Play();
        NotificationChanged?.Invoke();
    }

    [ContextMenu("Add Test Email")]
    private void AddTestEmail()
    {
        AddEmail("Test Sender", "Test email", "This is a test email.");
    }

    public bool Back()
    {
        if (!emailView.activeSelf)
            return false;

        sounds.back.Play();
        emailView.SetActive(false);
        return true;
    }

    public void OnAppOpened() { }

    public void OnAppClosed()
    {
        emailView.SetActive(false);
    }

    private void OpenEmail(EmailRow row, Email email)
    {
        sounds.tap.Play();
        email.read = true;
        row.SetRead(true);
        NotificationChanged?.Invoke();

        subjectText.text = email.subject;
        fromText.text = "From: " + email.from;
        dateText.text = "Sent: " + email.date;
        bodyText.text = email.body;
        // body scrolls, start each email at the top
        bodyText.rectTransform.anchoredPosition = Vector2.zero;
        emailView.SetActive(true);

        // grow the email out of the row that was tapped
        RectTransform rowRect = (RectTransform)row.transform;
        emailCard.DOKill();
        emailCard.position = rowRect.position;
        emailCard.localScale = Vector3.one * (rowRect.rect.height / emailCard.rect.height);
        emailCard.DOAnchorPos(emailCardPos, 0.3f).SetEase(Ease.OutExpo);
        emailCard.DOScale(1f, 0.3f).SetEase(Ease.OutExpo);
    }

    private EmailRow CreateRow(Email email)
    {
        EmailRow row = Instantiate(rowTemplate, list);
        row.gameObject.SetActive(true);
        row.Show(email);
        row.button.onClick.AddListener(() => OpenEmail(row, email));
        return row;
    }
}

[System.Serializable]
public class Email
{
    public string from;
    public string subject;
    public string date;
    [TextArea(3, 12)] public string body;
    public bool read;
    // one the owner sent rather than received, shown with who it went to
    public bool sent;
    public string to;
}
