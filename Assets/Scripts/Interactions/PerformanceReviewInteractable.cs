using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PerformanceReviewInteractable : Interactable
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public DialogLine repeatableLine;

    DialogManager dialogManager;

    public Canvas performanceCanvas;

    public SuspectNote note; 
    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        GameManager.Instance.ActivateCheckedReviewFlag();
        performanceCanvas.gameObject.SetActive(true);
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
        performanceCanvas.gameObject.SetActive(false);
        dialogLinesWithCharacterInfo.Clear();
        dialogLinesWithCharacterInfo = new List<DialogLine>();
        dialogLinesWithCharacterInfo.Add(repeatableLine);
        Debug.Log("ProductManagerInteractable OnDialogFinished");
        if(note != null && note.text.Length > 0)
        {
            SuspectNotes.Add(note);
        }
    }
    
}
