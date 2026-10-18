using UnityEngine;
using UnityEngine.EventSystems;

public class ComputerInteractable : Interactable
{
    [SerializeField] private ComputerScreen computerScreen;

    public void Start()
    {
        // the screen is on its own canvas, so find it if the scene didn't link it
        if (computerScreen == null)
            Debug.LogError("ComputerInteractable: computerScreen is not set in the inspector.");
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        computerScreen.Open();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Should we do anything on pointer up?
    }
}
