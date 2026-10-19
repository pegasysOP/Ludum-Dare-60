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
    public Image background;
    public Color unreadColour = new Color(0.09f, 0.36f, 0.76f, 0.92f);
    public Color readColour = new Color(0.05f, 0.19f, 0.4f, 0.82f);

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
        subjectText.alpha = read ? 0.75f : 1f;
        fromText.alpha = read ? 0.6f : 0.9f;
        background.color = read ? readColour : unreadColour;
        unreadDot.gameObject.SetActive(!read);

        pulse?.Kill();
        if (read)
            return;

        // halo behind the orb breathes so new mail stands out
        Color colour = unreadDot.color;
        colour.a = 0.9f;
        unreadDot.color = colour;
        pulse = unreadDot.DOFade(0.15f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(unreadDot.gameObject);
    }
}
