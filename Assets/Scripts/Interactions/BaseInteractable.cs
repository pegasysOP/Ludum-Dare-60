using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Interactable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

    public abstract void OnPointerDown(PointerEventData eventData);


    public abstract void OnPointerUp(PointerEventData eventData);

    private SpriteOutline spriteOutline;
    protected InteractSfxLibrary interactSounds;

    // interactables with their own click sound (paper, footsteps) turn the generic select off
    protected virtual bool PlaysSelectSound => true;

    public virtual void Awake()
    {
        spriteOutline = GetComponent<SpriteOutline>();
        interactSounds = InteractSfxLibrary.Load();
    }

    // the event system calls this one, so every interactable clicks before doing its own thing
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (PlaysSelectSound)
            interactSounds?.select.Play();
        spriteOutline?.MarkUsed();
        OnPointerDown(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        spriteOutline?.ShowOutline();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        spriteOutline?.HideOutline();
    }
}
