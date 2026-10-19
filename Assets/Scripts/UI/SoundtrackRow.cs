using TMPro;
using UnityEngine;
using UnityEngine.UI;

// one song in the soundtrack list
public class SoundtrackRow : MonoBehaviour
{
    public Button button;
    public Image background;
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI lengthText;

    public Color idleColour = new Color(1f, 1f, 1f, 0.55f);
    public Color currentColour = new Color(0.22f, 0.68f, 0.3f, 1f);
    public Color numberColour = new Color(0.15f, 0.52f, 0.2f, 1f);
    public Color titleColour = new Color(0.05f, 0.22f, 0.55f, 1f);
    public Color lengthColour = new Color(0.08f, 0.3f, 0.62f, 0.65f);

    public void Show(int number, string title, AudioClip clip)
    {
        numberText.text = number.ToString("00");
        titleText.text = title;
        lengthText.text = clip != null ? SoundtrackMenu.FormatTime(clip.length) : "";
        SetCurrent(false);
    }

    public void SetCurrent(bool current)
    {
        background.color = current ? currentColour : idleColour;
        numberText.color = current ? Color.white : numberColour;
        titleText.color = current ? Color.white : titleColour;
        lengthText.color = current ? new Color(1f, 1f, 1f, 0.8f) : lengthColour;
    }
}
