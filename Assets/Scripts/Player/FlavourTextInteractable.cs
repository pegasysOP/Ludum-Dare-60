using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//TODO: Could this be a scriptable object? 
public class FlavourTextInteractable : Interactable
{
    public string flavourText;
    public TextMeshProUGUI flavourTextUI;

    public override void OnPointerDown(PointerEventData eventData)
    {
        //TODO: Maybe fade out the old text first? 
        flavourTextUI.text = "";
        //TODO: Show text one letter at a time, with a sound effect for each letter.
        flavourTextUI.text = flavourText;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Maybe a nice fade out effect for the text, or a sound effect when it disappears.
    }
}
