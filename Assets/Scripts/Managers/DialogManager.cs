using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogManager :Interactable
{
    List<DialogLine> dialogLinesWithCharacterInfo = new List<DialogLine>();
    int currentLine = 0;

    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI characterName;

    public Sprite characterSprite;

    public Image characterImage;

    private float speed;

    public UnityEvent onDialogFinished;
    private Coroutine typingCoroutine;

    // open / next line / close sounds and the per-letter voice blips
    private DialogSfx sfx;
    private DialogSfx Sfx => sfx != null ? sfx : (sfx = DialogSfx.Attach(gameObject));
    private bool boxOpen;

    //TODO: Maybe pass in a dialogue speed. Some people talk fast some slow

    public void Start()
    {
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
        if (currentLine >= dialogLinesWithCharacterInfo.Count)
        {
            bool wasOpen = boxOpen;
            ClearDialog();
            if (wasOpen)
                Sfx.Close();

            Debug.Log("There are no more lines so we are clearing");
            onDialogFinished.Invoke();
            return;
        }

        //If the user clicks do we want to finish the line immediately or do we want to do the next line? 

        if (currentLine < dialogLinesWithCharacterInfo.Count)
        {
            string line = dialogLinesWithCharacterInfo[currentLine].text;

            // no character means narration, so no name or portrait
            Character character = dialogLinesWithCharacterInfo[currentLine].character;
            characterName.text = character != null ? character.characterName : "";

            characterImage.sprite = character != null ? character.headshot : null;

            if (boxOpen)
                Sfx.Advance();
            else
                Sfx.Open();
            boxOpen = true;
            Sfx.StartLine(character);

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

    public void AddDialogueLines(List<DialogLine> lines, float dialogSpeed = 0.05f)
    {
        // copy incoming list to avoid shared-reference mutation bugs
        dialogLinesWithCharacterInfo = (lines == null) ? new List<DialogLine>() : new List<DialogLine>(lines);
        this.speed = dialogSpeed;
        Debug.Log("Added " + dialogLinesWithCharacterInfo.Count + " lines of dialog.");
        currentLine = 0;
    }

    private IEnumerator AddCharactersOneByOne()
    {
        float delay = speed;
        while (dialogText.maxVisibleCharacters < dialogText.textInfo.characterCount)
        {
            yield return new WaitForSeconds(delay);
            dialogText.maxVisibleCharacters++;

            // voice blip for the letter that just appeared, then hold a little longer on punctuation
            int i = dialogText.maxVisibleCharacters - 1;
            TMP_CharacterInfo[] chars = dialogText.textInfo.characterInfo;
            char c = chars[i].character;
            char next = i + 1 < dialogText.textInfo.characterCount ? chars[i + 1].character : '\0';
            Sfx.Letter(c);
            delay = speed * Sfx.PauseFactor(c, next);
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
        dialogLinesWithCharacterInfo.Clear();
        currentLine = 0;
        boxOpen = false;
        if (dialogText != null)
        {
            characterName.text = "";
            characterImage.sprite = null;
            dialogText.text = "";
            dialogText.maxVisibleCharacters = 0;
        }
    }

}

[System.Serializable]
public struct DialogLine
{
    public string text;
    public Character character;
    public DialogLine(string text, Character character = null)
    {
        this.text = text;
        this.character = character;
    }
}
