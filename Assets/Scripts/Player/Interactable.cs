using UnityEngine;
using UnityEngine.EventSystems;

public class Interactable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Click in Progress");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Click ended");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log(name + "Game Object Mouse Exit");
    }
}
