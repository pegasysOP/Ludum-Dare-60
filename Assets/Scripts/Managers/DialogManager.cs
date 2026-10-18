using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    //TODO: Maybe pass in a dialogue speed. Some people talk fast some slow

    public void Start()
    {
        dialogLines.Add("Hello, welcome to the game!");
        dialogLines.Add("This is a simple dialog system.");
        dialogLines.Add("You can add more lines of dialog as needed.");
        dialogText = GetComponentInChildren<TextMeshProUGUI>();
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
            currentLine = 0;
            dialogLines.Clear();
            dialogText.text = "";

            Debug.Log("There are no more lines so we are clearing");
            onDialogFinished.Invoke();
            //Trigger an event or callback to indicate that the dialog has finished
            return;
        }

        //If the user clicks do we want to finish the line immediately or do we want to do the next line? 

        if (currentLine < dialogLines.Count)
        {
            Debug.Log("Displaying line: " + currentLine);
            string line = dialogLines[currentLine];
            dialogText.text = line;
            // Ensure TextMeshPro has generated its mesh/textInfo so characterCount is valid
            //dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;
            StartCoroutine(AddCharactersOneByOne());
            currentLine++;
        }
    }

    public void AddDialogueLines(List<string> lines, float dialogSpeed = 0.05f)
    {
        dialogLines = lines;
        this.speed = dialogSpeed;
        Debug.Log("Added " + lines.Count + " lines of dialog.");
        currentLine = 0;
    }

    private IEnumerator AddCharactersOneByOne()
    {
        while (dialogText.maxVisibleCharacters < dialogText.text.Length)
        {
            yield return new WaitForSeconds(speed);
            dialogText.maxVisibleCharacters++;
            //TODO: Add a sound for each letter?
        }
    }
}
