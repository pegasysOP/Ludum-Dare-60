using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SecretaryInteractable : Interactable
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public DialogLine repeatableLine;

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
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("SecretaryInteractable OnPointerUp");
    }
    
    private void OnFinalLineCompleted()
    {

        GameManager.Instance.ActivateGotCDFlag();
        
        dialogLinesWithCharacterInfo.Clear();
        dialogLinesWithCharacterInfo = new List<DialogLine>();
        dialogLinesWithCharacterInfo.Add(repeatableLine);
        Debug.Log("SecretaryInteractable OnDialogFinished");
    }
    
}
