using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SecretaryInteractable : Interactable
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    DialogManager dialogManager;

    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
        dialogManager.onDialogFinished.AddListener(OnDialogFinished);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        dialogManager.ClearDialog();
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("SecretaryInteractable OnPointerUp");
    }
    
    private void OnDialogFinished()
    {
        Debug.Log("SecretaryInteractable OnDialogFinished");
    }
    
}
