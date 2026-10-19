using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HomescreenApp : MonoBehaviour
{
    public Button button;
    public RectTransform rect;
    public FullscreenApp fullscreenApp;
    public GameObject notificationDot;

    public void SetNotification(bool show)
    {
        if (show == notificationDot.activeSelf)
            return;

        Transform dot = notificationDot.transform;
        dot.DOKill();
        notificationDot.SetActive(show);
        if (!show)
            return;

        // pops in then keeps a gentle throb so it catches the eye
        dot.localScale = Vector3.zero;
        dot.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(notificationDot).OnComplete(() =>
            dot.DOScale(1.12f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(notificationDot));
    }
}
