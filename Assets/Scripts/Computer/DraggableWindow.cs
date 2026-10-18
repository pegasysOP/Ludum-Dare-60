using UnityEngine;
using UnityEngine.EventSystems;

// fake desktop window: drag it by its title bar, click anywhere on it to bring it to the front
public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    public RectTransform titleBar;

    private RectTransform rectTransform;
    private Canvas canvas;
    private bool dragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // root canvas holds the real scale, the desktop is a nested canvas
        canvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    private void Start()
    {
        ClampToParent();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // only the title bar picks the window up
        GameObject pressed = eventData.pointerPressRaycast.gameObject;
        dragging = pressed != null && pressed.transform.IsChildOf(titleBar);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        ClampToParent();
    }

    // keep the whole window inside the desktop
    private void ClampToParent()
    {
        Rect bounds = ((RectTransform)rectTransform.parent).rect;
        Rect window = rectTransform.rect;
        Vector3 position = rectTransform.localPosition;
        position.x = Mathf.Clamp(position.x, bounds.xMin - window.xMin, bounds.xMax - window.xMax);
        position.y = Mathf.Clamp(position.y, bounds.yMin - window.yMin, bounds.yMax - window.yMax);
        rectTransform.localPosition = position;
    }
}
