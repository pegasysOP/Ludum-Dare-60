using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmailRow : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI fromText;
    public Image unreadDot;

    private Tween pulse;

    public void Show(Email email)
    {
        subjectText.text = email.subject;
        fromText.text = email.from;
        SetRead(email.read);
    }

    public void SetRead(bool read)
    {
        subjectText.fontStyle = read ? FontStyles.Normal : FontStyles.Bold;
        unreadDot.gameObject.SetActive(!read);

        pulse?.Kill();
        if (read)
            return;

        // slow gentle pulse so new mail stands out without shouting
        Color colour = unreadDot.color;
        colour.a = 1f;
        unreadDot.color = colour;
        pulse = unreadDot.DOFade(0.3f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(unreadDot.gameObject);
    }
}
