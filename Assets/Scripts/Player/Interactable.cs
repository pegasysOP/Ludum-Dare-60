using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Interactable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

    public abstract void OnPointerDown(PointerEventData eventData);


    public abstract void OnPointerUp(PointerEventData eventData);


    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Enter");
        //TODO: highlight object in red
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Exit");
        //TODO: remove highlight from object
    }
}
