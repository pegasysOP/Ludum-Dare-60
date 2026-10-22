using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SecretaryInteractable : Interactable
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public DialogLine repeatableLine;

    // goes into SusPX once the conversation's been heard out
    public SuspectNote note;

    // the line where the CD gets handed over, it shows on screen as that line starts
    public int cdLineIndex = 2;

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
        if (!GameManager.Instance.HasCD() && cdLineIndex > 0 && cdLineIndex < dialogLinesWithCharacterInfo.Count)
        {
            dialogLinesWithCharacterInfo[cdLineIndex - 1].onLineCompleted = OnCDHandedOver;
        }
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("SecretaryInteractable OnPointerUp");
    }
    
    private void OnCDHandedOver()
    {
        interactSounds.itemGet.Play();
        CDPopup.PlayReceived();
    }

    private void OnFinalLineCompleted()
    {
        GameManager.Instance.ActivateGotCDFlag();
        SuspectNotes.Add(note);
        
        dialogLinesWithCharacterInfo.Clear();
        dialogLinesWithCharacterInfo = new List<DialogLine>();
        dialogLinesWithCharacterInfo.Add(repeatableLine);
        Debug.Log("SecretaryInteractable OnDialogFinished");
    }
    
}
