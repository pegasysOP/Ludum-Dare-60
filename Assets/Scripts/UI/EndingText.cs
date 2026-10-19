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

    public void StartEndingText(ENDINGS ending)
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
        DialogLine line1 = new DialogLine("The Product Manager did it! Seeing the CEO buy his 4th Yacht while they're toiling away. No reward. No promotion.");
        DialogLine line2 = new DialogLine("2 years later");
        DialogLine line3 = new DialogLine("ClearSky™ saw an 8th consecutive quarter of record profits.");
        DialogLine line4 = new DialogLine("Congratulations, you got the good ending!",
        null,
        null,
        () => SceneUtils.LoadCreditScene()
        );

        dialogLinesWithCharacterInfo.Add(line1);
        dialogLinesWithCharacterInfo.Add(line2);
        dialogLinesWithCharacterInfo.Add(line3);
        dialogLinesWithCharacterInfo.Add(line4);
    }

    public void LoadBadEndingText()
    {
        DialogLine line1 = new DialogLine("An innocent condemned. How could you? Soap trusted you. I thought I could too.");
        DialogLine line2 = new DialogLine("2 years later");
        DialogLine line3 = new DialogLine("ClearSky™ is foreclosed. After years of embezzlement the coffers are empty.");
        DialogLine line4 = new DialogLine("Company wide email reads: Everyone is fired. Effective immediately!");
        DialogLine line5 = new DialogLine("Better luck next time, you got the bad ending!",
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
        DialogLine line1 = new DialogLine("Did you even play the game? Yes you! I get it, theres a lot of games to rate. But condemning someone without evidence is inexcusable!");
        DialogLine line2 = new DialogLine("2 days later");
        DialogLine line3 = new DialogLine("You're pulled into the HR office at DwT");
        DialogLine line4 = new DialogLine("You've made this company look like an absolute joke. You're fired!");
        DialogLine line5 = new DialogLine("Better luck next time, you got the Speedrunner ending!",
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

}

public enum ENDINGS
{
    BAD, 
    GOOD, 
    LAZY 
}
