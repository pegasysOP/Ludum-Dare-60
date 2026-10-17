using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//TODO: Could this be a scriptable object? 
public class FlavourTextInteractable : Interactable
{
    [SerializeField] private string flavourText;
    [SerializeField] TextMeshProUGUI flavourTextUI;

    public float characterDelay = 0.1f; 

    public void Start()
    {
        flavourTextUI.text = flavourText;
        flavourTextUI.maxVisibleCharacters = 0;
    }

    //TODO: Do we want to disable the text if the player changes room? 

    public override void OnPointerDown(PointerEventData eventData)
    {
        if(flavourTextUI.text != flavourText)
        {
            flavourTextUI.text = flavourText;
        }

        flavourTextUI.maxVisibleCharacters = 0;
        StartCoroutine(AddCharactersOneByOne());
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Maybe a nice fade out effect for the text, or a sound effect when it disappears.
    }

    private IEnumerator AddCharactersOneByOne()
    {
        while(flavourTextUI.maxVisibleCharacters < flavourTextUI.textInfo.characterCount)
        {
            yield return new WaitForSeconds(characterDelay);
            flavourTextUI.maxVisibleCharacters++;
            //TODO: Add a sound for each letter?
        }
    }
}
