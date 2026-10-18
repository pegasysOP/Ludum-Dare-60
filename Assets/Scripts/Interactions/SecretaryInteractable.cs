using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SecretaryInteractable : Interactable
{
    [SerializeField] private List<string> dialogLines;

    DialogManager dialogManager;

    public void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
        dialogManager.onDialogFinished.AddListener(OnDialogFinished);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        dialogManager.ClearDialog();
        dialogManager.AddDialogueLines(dialogLines);
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
