using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingText : MonoBehaviour
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public Canvas endingCanvas;

    DialogManager dialogManager;

    public Image endingImage;

    public Sprite goodEndingSprite;
    public Sprite badEndingSprite;
    public Sprite speedEndingSprite;

    public CanvasGroup fade;
    public Image backdrop;
    public TextMeshProUGUI titleText;

    // tints for the sky behind the art
    public Color goodBackdrop = Color.white;
    public Color badBackdrop = new Color(0.78f, 0.36f, 0.38f);
    public Color speedBackdrop = new Color(0.62f, 0.64f, 0.7f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }

    public void StartEnding(ENDINGS ending)
    {
        endingCanvas.gameObject.SetActive(true);

        switch (ending)
        {
            case ENDINGS.GOOD:
                LoadGoodEndingText();
                ShowEnding(goodEndingSprite, goodBackdrop, "THE GOOD ENDING");
                break;
            case ENDINGS.BAD:
                LoadBadEndingText();
                ShowEnding(badEndingSprite, badBackdrop, "THE BAD ENDING");
                break;
            case ENDINGS.LAZY:
                LoadLazyEndingText();
                ShowEnding(speedEndingSprite, speedBackdrop, "THE SPEEDRUNNER ENDING");
                break;
            default:
                Debug.LogError("Error: " + ending + " not implemented yet!");
                break;
        }

        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        // let the screen fade in before the story starts
        DOVirtual.DelayedCall(1.4f, dialogManager.PlayDialog);
    }

    private void ShowEnding(Sprite sprite, Color backdropColour, string title)
    {
        endingImage.sprite = sprite;
        backdrop.color = backdropColour;
        titleText.text = title;

        fade.alpha = 0f;
        fade.DOFade(1f, 1f);

        // the art drifts in and settles while the text plays
        endingImage.transform.localScale = Vector3.one * 1.08f;
        endingImage.transform.DOScale(1f, 6f).SetEase(Ease.OutSine);

        titleText.alpha = 0f;
        titleText.DOFade(1f, 0.8f).SetDelay(0.8f);
    }

    public void LoadGoodEndingText()
    {
        DialogLine line1 = new DialogLine("The trail leads directly to the Product Manager. The evidence all point to a scheme to quietly siphon money from the company while avoiding suspicion.");
        DialogLine line2 = new DialogLine("Following a brief trial, he is convicted of embezzlement and escorted away in handcuffs.");
        DialogLine line3 = new DialogLine("With the source of the losses removed, confidence returns. The company goes on to achieve eight consecutive quarters of growth.");
        DialogLine line4 = new DialogLine("The CEO throws a company-wide holiday. Karen celebrates by buying more sticky notes. Susan finally finishes the performance reviews.");
        DialogLine finalLine = new DialogLine("Congratulations! You got the GOOD ENDING.",
        null,
        null,
        () => SceneUtils.LoadCreditScene()
        );

        dialogLinesWithCharacterInfo.Add(line1);
        dialogLinesWithCharacterInfo.Add(line2);
        dialogLinesWithCharacterInfo.Add(line3);
        dialogLinesWithCharacterInfo.Add(line4);
        dialogLinesWithCharacterInfo.Add(finalLine);
    }

    public void LoadBadEndingText()
    {
        DialogLine line1 = new DialogLine("Certain of your conclusion, you present the evidence. Unfortunately, you've accused the wrong person.");
        DialogLine line2 = new DialogLine("An innocent employee is dismissed from the company and publicly blamed for the missing funds.");
        DialogLine line3 = new DialogLine("Within a year, the company is drowning in debt. Projects are canceled. Departments are shut down. Employees leave in droves.");
        DialogLine line4 = new DialogLine("Eventually, the company is foreclosed and everyone loses their job.");
        DialogLine line5 = new DialogLine("The mystery remains unsolved. You got the BAD ENDING",
        null,
        null,
        () => SceneUtils.LoadCreditScene()
        );

        dialogLinesWithCharacterInfo.Add(line1);
        dialogLinesWithCharacterInfo.Add(line2);
        dialogLinesWithCharacterInfo.Add(line3);
        dialogLinesWithCharacterInfo.Add(line4);
        dialogLinesWithCharacterInfo.Add(line5);
    }

    public void LoadLazyEndingText()
    {
        DialogLine line1 = new DialogLine("Wow. That was fast. Like... suspiciously fast.");
        DialogLine line2 = new DialogLine("To be fair, this is Ludum Dare. There are a lot of games to rate, and only so many hours in the day.");
        DialogLine line3 = new DialogLine("We understand. Still, you may have missed one or two... or fifty... important details.");
        DialogLine line4 = new DialogLine("The good news is that the office will still be here when you come back. Probably...");
        DialogLine finalLine = new DialogLine("You got the SPEEDRUNNER ENDING.",
        null,
        null,
        () => SceneUtils.LoadCreditScene()
        );

        dialogLinesWithCharacterInfo.Add(line1);
        dialogLinesWithCharacterInfo.Add(line2);
        dialogLinesWithCharacterInfo.Add(line3);
        dialogLinesWithCharacterInfo.Add(line4);
        dialogLinesWithCharacterInfo.Add(finalLine);
    }

}

public enum ENDINGS
{
    BAD, 
    GOOD, 
    LAZY 
}
