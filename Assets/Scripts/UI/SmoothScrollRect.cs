using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// mouse wheel glides to where it's going instead of jumping a notch at a time
public class SmoothScrollRect : ScrollRect
{
    public float smoothTime = 0.12f;

    private float target;
    private float velocity;
    private bool gliding = false;

    public override void OnScroll(PointerEventData data)
    {
        if (!IsActive() || !vertical)
            return;

        float scrollable = content.rect.height - viewport.rect.height;
        if (scrollable <= 0f)
            return;

        // keep adding to where it's heading, so quick flicks build up rather than restart
        if (!gliding)
            target = verticalNormalizedPosition;
        target = Mathf.Clamp01(target + data.scrollDelta.y * scrollSensitivity / scrollable);
        gliding = true;
    }

    // glides just far enough that the item sits fully in view
    public void ScrollToShow(RectTransform item, float margin = 8f)
    {
        float scrollable = content.rect.height - viewport.rect.height;
        if (scrollable <= 0f)
            return;

        // distances down from the top of the content
        Vector3[] corners = new Vector3[4];
        item.GetWorldCorners(corners);
        float contentTop = content.rect.yMax;
        float itemTop = contentTop - content.InverseTransformPoint(corners[1]).y;
        float itemBottom = contentTop - content.InverseTransformPoint(corners[0]).y;

        float from = gliding ? target : verticalNormalizedPosition;
        float offset = (1f - from) * scrollable;
        float view = viewport.rect.height;
        if (itemTop - margin < offset)
            offset = itemTop - margin;
        else if (itemBottom + margin > offset + view)
            offset = itemBottom + margin - view;

        target = 1f - Mathf.Clamp01(offset / scrollable);
        gliding = true;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        gliding = false;
        base.OnBeginDrag(eventData);
    }

    // views get hidden and reused, so don't carry an old glide into the next showing
    protected override void OnDisable()
    {
        gliding = false;
        base.OnDisable();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (!gliding)
            return;

        float position = Mathf.SmoothDamp(verticalNormalizedPosition, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        if (Mathf.Abs(position - target) < 0.0005f)
        {
            position = target;
            gliding = false;
        }
        verticalNormalizedPosition = position;
    }
}
