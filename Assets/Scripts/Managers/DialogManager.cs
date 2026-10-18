using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DialogManager :Interactable
{
    List<string> dialogLines = new List<string>();
    int currentLine = 0;

    TextMeshProUGUI dialogText;

    private float speed;

    public UnityEvent onDialogFinished;
    private Coroutine typingCoroutine;

    //TODO: Maybe pass in a dialogue speed. Some people talk fast some slow

    public void Start()
    {
        // Try to find the TMP text even if the dialog UI is inactive at start
        dialogText = GetComponentInChildren<TextMeshProUGUI>(true);
        speed = 0.05f; // Default speed
        if (onDialogFinished == null)
            onDialogFinished = new UnityEvent();
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("DialogManager OnPointerUp");
    }

    public void PlayDialog()
    {
        Debug.Log("Playing dialog. Current line: " + currentLine + ", Total lines: " + dialogLines.Count);
        if (currentLine >= dialogLines.Count)
        {
            ClearDialog();

            Debug.Log("There are no more lines so we are clearing");
            onDialogFinished.Invoke();
            return;
        }

        //If the user clicks do we want to finish the line immediately or do we want to do the next line? 

        if (currentLine < dialogLines.Count)
        {
            Debug.Log("Displaying line: " + currentLine);
            string line = dialogLines[currentLine];

            // Stop any previous typing coroutine so multiple clicks don't race
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogText.text = line;
            dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;
            typingCoroutine = StartCoroutine(AddCharactersOneByOne());
            currentLine++;
        }
    }

    public void AddDialogueLines(List<string> lines, float dialogSpeed = 0.05f)
    {
        // Copy the incoming list so callers (inspectable lists) are not cleared when
        // the dialog manager clears its internal queue. This prevents shared-reference
        // bugs where the source list (e.g. a serialized list on an Interactable)
        // gets emptied unexpectedly.
        dialogLines = (lines == null) ? new List<string>() : new List<string>(lines);
        this.speed = dialogSpeed;
        Debug.Log("Added " + lines.Count + " lines of dialog.");
        currentLine = 0;
    }

    private IEnumerator AddCharactersOneByOne()
    {
        while (dialogText.maxVisibleCharacters < dialogText.textInfo.characterCount)
        {
            yield return new WaitForSeconds(speed);
            dialogText.maxVisibleCharacters++;
            //TODO: Add a sound for each letter?
        }
        typingCoroutine = null;
    }

    public void ClearDialog()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        dialogLines.Clear();
        currentLine = 0;
        if (dialogText != null)
        {
            dialogText.text = "";
            dialogText.maxVisibleCharacters = 0;
        }
    }

}
