using System.Collections.Generic;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
    }

    public void StartEnding(ENDINGS ending)
    {
        endingCanvas.gameObject.SetActive(true);
        if(ending == ENDINGS.GOOD)
        {
            LoadGoodEndingText();
        }

        switch (ending)
        {
            case ENDINGS.GOOD:
                LoadGoodEndingText();
                endingImage.sprite = goodEndingSprite;
                break;
            case ENDINGS.BAD:
                LoadBadEndingText();
                endingImage.sprite = badEndingSprite;
                break;
            case ENDINGS.LAZY:
                LoadLazyEndingText();
                endingImage.sprite = speedEndingSprite;
                break;
            default:
                Debug.LogError("Error: " + ending + " not implemented yet!");
                break;
        }


        
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
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
