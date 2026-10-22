using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlavourTextInteractable : Interactable
{
    [SerializeField] private string flavourText;

    DialogManager dialogManager;

    public SuspectNote note;

    public float characterDelay = 0.1f;

    [Tooltip("Tap the fish tank glass instead of the generic select click.")]
    [SerializeField] private bool fishTankTap;

    protected override bool PlaysSelectSound => !fishTankTap || interactSounds == null || !interactSounds.fishTap.HasAnyClip;

    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }

    //TODO: Do we want to disable the text if the player changes room? 

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!PlaysSelectSound)
            interactSounds.fishTap.Play();
        List<DialogLine> list = new List<DialogLine> { new DialogLine(flavourText) };
        dialogManager.AddDialogueLines(list, characterDelay);
        dialogManager.PlayDialog();
        
        if(note != null && note.text.Length > 0)
        {
            SuspectNotes.Add(note);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Maybe a nice fade out effect for the text, or a sound effect when it disappears.
    }
}
