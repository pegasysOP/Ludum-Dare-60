using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class StickyNoteInteractable : Interactable
{
    [SerializeField] private Canvas stickyNoteCanvas;
    //FIXME: Replace this with a proper way to exit the sticky note
    Keyboard keyboard;
    // click outside closes the canvas directly, so watch it to hear every close
    bool wasOpen;

    protected override bool PlaysSelectSound => false;

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

        bool open = stickyNoteCanvas.gameObject.activeSelf;
        if (wasOpen && !open)
            interactSounds.paperClose.Play();
        wasOpen = open;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!stickyNoteCanvas.gameObject.activeSelf)
            interactSounds.paperOpen.Play();
        stickyNoteCanvas.gameObject.SetActive(true);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: should we do anything on pointer up? 
    }
}
