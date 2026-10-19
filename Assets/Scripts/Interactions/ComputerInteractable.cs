using UnityEngine;
using UnityEngine.EventSystems;

public class ComputerInteractable : Interactable
{
    [SerializeField] private ComputerScreen computerScreen;

    private ComputerDesktop desktop;

    // a pc with unread mail only counts as looked at once that mail has been read
    protected override bool MarkUsedOnClick => desktop == null || !desktop.HasUnreadMail;

    public void Start()
    {
        // the screen is on its own canvas, so find it if the scene didn't link it
        if (computerScreen == null)
        {
            Debug.LogError("ComputerInteractable: computerScreen is not set in the inspector.");
            return;
        }

        desktop = computerScreen.GetComponentInChildren<ComputerDesktop>(true);
        if (desktop != null)
            desktop.MailRead += MarkUsed;
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
