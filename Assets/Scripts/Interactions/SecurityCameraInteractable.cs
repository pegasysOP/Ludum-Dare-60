using UnityEngine;
using UnityEngine.EventSystems;

public class SecurityCameraInteractable : Interactable
{
    [SerializeField] private SecurityScreen securityScreen;

    public override void OnPointerDown(PointerEventData eventData)
    {
        securityScreen.Open();
        GameManager.Instance.ActivateCheckedCameraFlag();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
    }
}
