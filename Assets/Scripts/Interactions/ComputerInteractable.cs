using UnityEngine;
using UnityEngine.EventSystems;

public class ComputerInteractable : Interactable
{
    public override void OnPointerDown(PointerEventData eventData)
    {
        //Enable the computer Canvas

        //Wait for user input into password field 

        //Check if password is correct

        //If incorrect, play an error sound

        //If correct, change to homescreen canvas

        //Show invoicing PDF, Timesheet with Annual Leave and other clutter (Optionally, allow drag and drop of UI elements) 
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Should we do anything on pointer up? 
    }
}
