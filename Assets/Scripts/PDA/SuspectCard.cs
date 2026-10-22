using DG.Tweening;
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
    // list rows only, how many notes there are and whether any are new
    public TextMeshProUGUI noteCountText;
    public Image newTag;

    private Tween pulse;

    public void Show(Character character)
    {
        nameText.text = character.characterName;
        titleText.text = character.jobTitle;

        // placeholder square until the headshots are drawn
        headshot.sprite = character.headshot;
        headshot.color = character.headshot != null ? Color.white : placeholderColour;
    }

    public void ShowNotes(int count, bool unseen)
    {
        if (noteCountText != null)
            noteCountText.text = count == 0 ? "" : count == 1 ? "1 note" : count + " notes";

        if (newTag == null)
            return;

        pulse?.Kill();
        newTag.transform.localScale = Vector3.one;
        newTag.gameObject.SetActive(unseen);
        if (!unseen)
            return;

        // gentle throb so a fresh note catches the eye against the red row
        pulse = newTag.transform.DOScale(1.12f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(newTag.gameObject);
    }
}
