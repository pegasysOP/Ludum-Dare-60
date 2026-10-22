using TMPro;
using UnityEngine;
using UnityEngine.UI;

// one bullet in a suspect's notes
public class SuspectNoteRow : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Image bullet;

    public void Show(SuspectNote note, Color colour)
    {
        text.text = note.text;
        bullet.color = colour;
    }
}
