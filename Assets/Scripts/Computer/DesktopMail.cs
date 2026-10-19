using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// mail program on a desktop pc, a cut down take on the phone's inbox
public class DesktopMail : MonoBehaviour
{
    public EmailRow rowTemplate;
    public RectTransform list;
    public GameObject listView;
    public GameObject emailView;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI fromText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI bodyText;
    public Button backButton;
    public List<Email> emails = new List<Email>();

    public event Action AllRead;

    public bool HasUnread => emails.Exists(email => !email.read);

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        backButton.onClick.AddListener(ShowList);

        foreach (Email email in emails)
        {
            EmailRow row = Instantiate(rowTemplate, list);
            row.gameObject.SetActive(true);
            row.Show(email);
            row.button.onClick.AddListener(() => OpenEmail(row, email));
        }
    }

    // opens on the inbox each time the window does
    private void OnEnable()
    {
        ShowList();
    }

    private void ShowList()
    {
        emailView.SetActive(false);
        listView.SetActive(true);
    }

    private void OpenEmail(EmailRow row, Email email)
    {
        bool hadUnread = HasUnread;
        email.read = true;
        row.SetRead(true);
        if (hadUnread && !HasUnread)
            AllRead?.Invoke();

        subjectText.text = email.subject;
        fromText.text = email.sent ? "To: " + email.to : "From: " + email.from;
        dateText.text = (email.sent ? "Sent: " : "Received: ") + email.date;
        bodyText.text = email.body;

        listView.SetActive(false);
        emailView.SetActive(true);
    }
}
