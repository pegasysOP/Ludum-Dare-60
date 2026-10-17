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

    public void Show(Suspect suspect)
    {
        nameText.text = suspect.name;
        titleText.text = suspect.jobTitle;

        // placeholder square until the headshots are drawn
        headshot.sprite = suspect.headshot;
        headshot.color = suspect.headshot != null ? Color.white : placeholderColour;
    }
}
