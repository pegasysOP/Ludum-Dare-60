using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Interactable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

    public abstract void OnPointerDown(PointerEventData eventData);


    public abstract void OnPointerUp(PointerEventData eventData);
    
    private SpriteOutline spriteOutline;

    public virtual void Awake()
    {
        spriteOutline = GetComponent<SpriteOutline>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Enter");
        spriteOutline?.ShowOutline();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Exit");
        spriteOutline?.HideOutline();
    }
}
