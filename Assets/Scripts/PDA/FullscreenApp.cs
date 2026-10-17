using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenApp : MonoBehaviour
{
    public Button backButton;
    public RectTransform rect;
    public CanvasGroup canvasGroup;

    public float openDuration = 0.4f;
    public float closeDuration = 0.25f;
    public float iconScale = 0.25f;

    private Sequence sequence;

    public void Open(RectTransform icon)
    {
        sequence?.Kill();

        gameObject.SetActive(true);
        rect.SetAsLastSibling();

        // start shrunk over the icon so it zooms out of it
        rect.position = icon.position;
        rect.localScale = Vector3.one * iconScale;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        sequence = DOTween.Sequence()
            .Join(rect.DOAnchorPos(Vector2.zero, openDuration).SetEase(Ease.OutExpo))
            .Join(rect.DOScale(1f, openDuration).SetEase(Ease.OutExpo))
            .Join(canvasGroup.DOFade(1f, openDuration * 0.5f));

        OnOpened();
    }

    public void Close(RectTransform icon)
    {
        sequence?.Kill();

        // let the homescreen take clicks again while this shrinks away
        canvasGroup.blocksRaycasts = false;

        Vector3 iconPos = rect.parent.InverseTransformPoint(icon.position);

        sequence = DOTween.Sequence()
            .Join(rect.DOLocalMove(iconPos, closeDuration).SetEase(Ease.InQuad))
            .Join(rect.DOScale(iconScale, closeDuration).SetEase(Ease.InQuad))
            .Join(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad))
            .OnComplete(() => gameObject.SetActive(false));

        OnClosed();
    }

    // return true if the app dealt with back itself, e.g. closing a sub view
    public virtual bool Back() { return false; }

    protected virtual void OnOpened() { }

    protected virtual void OnClosed() { }
}
