using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuspectCard : MonoBehaviour
{
    public Button button;
    public Image headshot;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI titleText;
    public Color placeholderColour = Color.black;

    public void Show(Character character)
    {
        nameText.text = character.characterName;
        titleText.text = character.jobTitle;

        // placeholder square until the headshots are drawn
        headshot.sprite = character.headshot;
        headshot.color = character.headshot != null ? Color.white : placeholderColour;
    }
}
