using UnityEngine;
using UnityEngine.EventSystems;

public class FlavourTextInteractable : Interactable
{
    [SerializeField] private string flavourText;

    DialogManager dialogManager;

    public float characterDelay = 0.1f;

    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }

    //TODO: Do we want to disable the text if the player changes room? 

    public override void OnPointerDown(PointerEventData eventData)
    {
        dialogManager.AddDialogueLines(new System.Collections.Generic.List<string> { flavourText }, characterDelay);
        dialogManager.PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Maybe a nice fade out effect for the text, or a sound effect when it disappears.
    }
}
