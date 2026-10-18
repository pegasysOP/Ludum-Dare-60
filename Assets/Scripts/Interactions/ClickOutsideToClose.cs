using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// goes on an invisible full screen image behind an overlay, so clicking anywhere but the overlay closes it
public class ClickOutsideToClose : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onClickOutside;

    public void OnPointerClick(PointerEventData eventData)
    {
        onClickOutside.Invoke();
    }
}
