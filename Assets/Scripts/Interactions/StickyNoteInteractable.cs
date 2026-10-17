using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class StickyNoteInteractable : Interactable
{
    [SerializeField] private Canvas stickyNoteCanvas;
    //FIXME: Replace this with a proper way to exit the sticky note
    Keyboard keyboard;

    public void Start()
    {
        stickyNoteCanvas.gameObject.SetActive(false);
        keyboard = Keyboard.current;
    }

    public void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            stickyNoteCanvas.gameObject.SetActive(false);
        }

    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        stickyNoteCanvas.gameObject.SetActive(true);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: should we do anything on pointer up? 
    }
}
