using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    public TextMeshProUGUI jobTitleText;

    public Sprite characterSprite;

    public Image characterImage;

    public CanvasGroup box;
    public CanvasGroup portrait;
    public Image continueArrow;
    // invisible full screen blocker so clicking anywhere but the pda moves the dialog on
    public GameObject clickCatcher;
    public Color placeholderColour = Color.black;

    private float speed;

    public UnityEvent onDialogFinished;
    private Coroutine typingCoroutine;

    // open / next line / close sounds and the per-letter voice blips
    private DialogSfx sfx;
    private DialogSfx Sfx => sfx != null ? sfx : (sfx = DialogSfx.Attach(gameObject));
    private bool boxOpen;
    private Vector2 boxPos;
    private LayoutElement portraitLayout;

    //TODO: Maybe pass in a dialogue speed. Some people talk fast some slow

    public override void Awake()
    {
        base.Awake();

        boxPos = ((RectTransform)box.transform).anchoredPosition;
        box.alpha = 0f;
        box.blocksRaycasts = false;
        ShowContinue(false);

        portraitLayout = portrait.GetComponent<LayoutElement>();
        HidePortrait();
    }

    public void Start()
    {
        speed = 0.05f; // Default speed
        if (onDialogFinished == null)
            onDialogFinished = new UnityEvent();
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        // first click finishes the line, the next one moves on
        if (typingCoroutine != null)
        {
            FinishLine();
            return;
        }

        PlayDialog();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("DialogManager OnPointerUp");
    }

    public void PlayDialog()
    {
        if (currentLine > 0 && currentLine - 1 < dialogLinesWithCharacterInfo.Count)
        {
            dialogLinesWithCharacterInfo[currentLine - 1]
                ?.onLineCompleted?.Invoke();
        }

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
            // fresh box, so the last conversation's speaker shouldn't fade out of it
            if (box.alpha == 0f)
                HidePortrait();

            // narration keeps the last speaker showing while they fade out
            if (character != null)
            {
                characterName.text = character.characterName;
                // the player has no job title, so this only shows for everyone else
                jobTitleText.text = character.jobTitle;
                jobTitleText.gameObject.SetActive(!string.IsNullOrEmpty(character.jobTitle));

                // placeholder square until the headshots are drawn
                characterImage.sprite = character.headshot;
                characterImage.color = characterImage.sprite != null ? Color.white : placeholderColour;
            }
            ShowPortrait(character != null);

            if (boxOpen)
                Sfx.Advance();
            else
            {
                Sfx.Open();
                SetBoxVisible(true);
            }
            boxOpen = true;
            Sfx.StartLine(character);

            // Stop any previous typing coroutine so multiple clicks don't race
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogText.text = line;
            AlignText(character != null);
            dialogText.maxVisibleCharacters = 0;
            ShowContinue(false);
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

    // speakers read from the top left, narration has the box to itself so it sits in the middle
    private void AlignText(bool speaking)
    {
        dialogText.margin = Vector4.zero;
        if (speaking)
        {
            dialogText.alignment = TextAlignmentOptions.TopLeft;
            dialogText.ForceMeshUpdate();
            return;
        }

        // the portrait may have just left the layout, and the text has to wrap at its final width
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)box.transform);

        // tmp centres on the lines typed so far, so the block would jump up each new line:
        // find where the whole text sits centred, then hold it there top aligned
        dialogText.maxVisibleCharacters = int.MaxValue;
        dialogText.alignment = TextAlignmentOptions.Center;
        dialogText.ForceMeshUpdate();
        float centredTop = dialogText.textInfo.lineInfo[0].ascender;

        dialogText.alignment = TextAlignmentOptions.Top;
        dialogText.ForceMeshUpdate();
        float shift = dialogText.textInfo.lineInfo[0].ascender - centredTop;

        // same amount off the bottom so autosizing sees the same space
        dialogText.margin = new Vector4(0f, shift, 0f, -shift);
        dialogText.ForceMeshUpdate();
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

        dialogLinesWithCharacterInfo[currentLine - 1]
            ?.onLineFinishedTyping?.Invoke();

        ShowContinue(true);
    }

    private void FinishLine()
    {
        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
        dialogText.maxVisibleCharacters = dialogText.textInfo.characterCount;
        ShowContinue(true);
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
        // text and portrait are left as they are so they fade out with the box
        if (dialogText != null)
        {
            ShowContinue(false);
            SetBoxVisible(false);
        }
    }

    // box pops up from just below when a conversation starts and fades away when it ends
    private void SetBoxVisible(bool visible)
    {
        RectTransform boxRect = (RectTransform)box.transform;
        box.DOKill();
        boxRect.DOKill();
        box.blocksRaycasts = visible;
        clickCatcher.SetActive(visible);
        box.DOFade(visible ? 1f : 0f, 0.2f);

        if (!visible)
            return;

        if (box.alpha == 0f)
            boxRect.anchoredPosition = boxPos + Vector2.down * 30f;
        boxRect.DOAnchorPos(boxPos, 0.3f).SetEase(Ease.OutBack);
    }

    private void ShowPortrait(bool show)
    {
        bool showing = portrait.gameObject.activeSelf && !portraitLayout.ignoreLayout;
        if (show == showing)
            return;

        // leaves the layout straight away so the text can take its space while it fades
        portrait.DOKill();
        portraitLayout.ignoreLayout = !show;
        if (show)
        {
            portrait.gameObject.SetActive(true);
            portrait.alpha = 0f;
            portrait.DOFade(1f, 0.15f);
        }
        else
            portrait.DOFade(0f, 0.15f).OnComplete(() => portrait.gameObject.SetActive(false));
    }

    private void HidePortrait()
    {
        portrait.DOKill();
        portrait.alpha = 0f;
        portraitLayout.ignoreLayout = true;
        portrait.gameObject.SetActive(false);
    }

    // blinking arrow once the line has finished typing
    private void ShowContinue(bool show)
    {
        continueArrow.DOKill();
        continueArrow.gameObject.SetActive(show);
        if (!show)
            return;

        Color colour = continueArrow.color;
        colour.a = 1f;
        continueArrow.color = colour;
        continueArrow.DOFade(0.2f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(continueArrow.gameObject);
    }

}

[System.Serializable]
public class DialogLine
{
    public string text;
    public Character character;

    public UnityAction onLineFinishedTyping;
    public UnityAction onLineCompleted;

    public DialogLine(
        string text,
        Character character = null,
        UnityAction onLineFinishedTyping = null,
        UnityAction onLineCompleted = null)
    {
        this.text = text;
        this.character = character;
        this.onLineFinishedTyping = onLineFinishedTyping;
        this.onLineCompleted = onLineCompleted;
    }
}
