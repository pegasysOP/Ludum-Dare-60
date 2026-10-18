using System.Collections.Generic;
using UnityEngine;

public class EndingText : MonoBehaviour
{
    public List<DialogLine> dialogLinesWithCharacterInfo;

    public Canvas endingCanvas;

    DialogManager dialogManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogManager = FindAnyObjectByType<DialogManager>();
        dialogManager.onDialogFinished.AddListener(OnDialogFinished);
    }

    public void StartEndingText(ENDINGS ending)
    {
        endingCanvas.gameObject.SetActive(true);
        if(ending == ENDINGS.GOOD)
        {
            LoadGoodEndingText();
        }

        
        dialogManager.AddDialogueLines(dialogLinesWithCharacterInfo);
        dialogManager.PlayDialog();
    }

    private void OnDialogFinished()
    {
        Debug.Log("EndingText OnDialogFinished");
        //Load the credits automatically for now. Perhaps add a manual credits later
        SceneUtils.LoadCreditScene();
    }

    public void LoadGoodEndingText()
    {
        DialogLine line1 = new DialogLine("The Product Manager did it! Seeing the CEO buy his 4th Yacht while they're toiling away. No reward. No promotion.");
        DialogLine line2 = new DialogLine("2 years later");
        DialogLine line3 = new DialogLine("ClearSky™ saw an 8th consecutive quarter of record profits.");
        DialogLine line4 = new DialogLine("Congratulations, you got the good ending!");

        dialogLinesWithCharacterInfo.Add(line1);
        dialogLinesWithCharacterInfo.Add(line2);
        dialogLinesWithCharacterInfo.Add(line3);
        dialogLinesWithCharacterInfo.Add(line4);
    }

}

public enum ENDINGS
{
    BAD, 
    GOOD, 
    LAZY 
}
