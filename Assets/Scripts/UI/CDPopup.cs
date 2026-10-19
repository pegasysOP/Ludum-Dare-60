using DG.Tweening;
using UnityEngine;

// the secretary's cd shown big on screen, when you're handed it and when it goes in the pc
public class CDPopup : MonoBehaviour
{
    public CanvasGroup group;
    public RectTransform holder;
    public RectTransform disc;

    private static CDPopup instance;
    private Sequence sequence;

    // made the first time it's needed, so no scene has to have one
    private static CDPopup Get()
    {
        if (instance == null)
            instance = Instantiate(Resources.Load<CDPopup>("CDPopupCanvas"));
        return instance;
    }

    private void Awake()
    {
        group.alpha = 0f;
        disc.DOLocalRotate(new Vector3(0f, 0f, -360f), 3f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1).SetLink(gameObject);
    }

    // fades in over the middle of the screen, then pops up out of view
    public static void PlayReceived()
    {
        CDPopup popup = Get();
        popup.Begin(0.85f);
        popup.sequence
            .Append(popup.group.DOFade(1f, 0.35f))
            .Join(popup.holder.DOScale(1f, 0.45f).SetEase(Ease.OutBack))
            .AppendInterval(1.2f)
            .Append(popup.holder.DOAnchorPosY(-20f, 0.12f).SetEase(Ease.OutQuad))
            .Append(popup.holder.DOAnchorPosY(900f, 0.45f).SetEase(Ease.InQuad))
            .Join(popup.holder.DOScale(0.6f, 0.45f).SetEase(Ease.InQuad))
            .Join(popup.group.DOFade(0f, 0.45f).SetEase(Ease.InQuad));
    }

    // pops up over the pc, then fades away as it goes in
    public static void PlayInserted()
    {
        CDPopup popup = Get();
        popup.Begin(0.3f);
        popup.sequence
            .Append(popup.group.DOFade(1f, 0.15f))
            .Join(popup.holder.DOScale(1f, 0.35f).SetEase(Ease.OutBack))
            .AppendInterval(0.4f)
            .Append(popup.holder.DOAnchorPosY(-80f, 0.4f).SetEase(Ease.InQuad))
            .Join(popup.holder.DOScale(0.7f, 0.4f).SetEase(Ease.InQuad))
            .Join(popup.group.DOFade(0f, 0.4f).SetEase(Ease.InQuad));
    }

    private void Begin(float scale)
    {
        sequence?.Kill();
        group.alpha = 0f;
        holder.anchoredPosition = Vector2.zero;
        holder.localScale = Vector3.one * scale;
        sequence = DOTween.Sequence().SetLink(gameObject);
    }
}
