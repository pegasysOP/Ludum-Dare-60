using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Interactable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

    public abstract void OnPointerDown(PointerEventData eventData);


    public abstract void OnPointerUp(PointerEventData eventData);

    // for things the player has to look at before an accusation is taken seriously
    [SerializeField] private bool countsAsEvidence = false;
    [SerializeField] private Evidence evidence;

    private SpriteOutline spriteOutline;
    protected InteractSfxLibrary interactSounds;

    // interactables with their own click sound (paper, footsteps) turn the generic select off
    protected virtual bool PlaysSelectSound => true;

    // some only count as looked at once you've found what they're hiding, and mark themselves
    protected virtual bool MarkUsedOnClick => true;

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
        if (MarkUsedOnClick)
            MarkUsed();
        if (countsAsEvidence && GameManager.Instance != null)
            GameManager.Instance.FoundEvidence(evidence);
        OnPointerDown(eventData);
    }

    protected void MarkUsed()
    {
        spriteOutline?.MarkUsed();
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
