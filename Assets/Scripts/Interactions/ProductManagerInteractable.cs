using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProductManagerInteractable : Interactable
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public DialogLine repeatableLine;

    // goes into SusPX once the conversation's been heard out
    public SuspectNote note;

    DialogManager dialogManager;

    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        dialogManager.ClearDialog();
        if (dialogLinesWithCharacterInfo.Count > 0)
        {
            dialogLinesWithCharacterInfo[^1].onLineCompleted = OnFinalLineCompleted;
        }

        //TODO: We probably want a DOTWeen animation for him moving away from the computer. 
        //Check what dialog line that is and add method for that
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("ProductManagerInteractable OnPointerUp");
    }
    
    private void OnFinalLineCompleted()
    {   
        SuspectNotes.Add(note);
        dialogLinesWithCharacterInfo.Clear();
        dialogLinesWithCharacterInfo = new List<DialogLine>();
        dialogLinesWithCharacterInfo.Add(repeatableLine);
        Debug.Log("ProductManagerInteractable OnDialogFinished");
    }
    
}
