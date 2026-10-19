using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// grows a little while hovered, for anything clickable
public class HoverGrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.08f;

    private Selectable selectable;
    private Vector3 baseScale;
    private Tween tween;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable())
            return;

        ScaleTo(baseScale * hoverScale, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(baseScale, Ease.OutQuad);
    }

    // only kills its own tween, so other scale animations on the object keep going
    private void ScaleTo(Vector3 scale, Ease ease)
    {
        tween?.Kill();
        tween = transform.DOScale(scale, 0.15f).SetEase(ease).SetLink(gameObject);
    }

    // apps can close while hovered without an exit, so don't stay stuck grown
    private void OnDisable()
    {
        tween?.Kill();
        transform.localScale = baseScale;
    }
}
