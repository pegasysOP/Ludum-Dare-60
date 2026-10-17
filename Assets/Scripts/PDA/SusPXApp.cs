using DG.Tweening;
using UnityEngine;

public class SusPXApp : MonoBehaviour, IAppContent
{
    public SuspectCard rowTemplate;
    public RectTransform list;
    public GameObject detailView;
    public SuspectCard detailCard;
    public Suspect[] suspects;

    private PhoneSfxLibrary sounds;
    private Vector2 detailCardPos;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();
        detailCardPos = ((RectTransform)detailCard.transform).anchoredPosition;

        foreach (Suspect suspect in suspects)
        {
            SuspectCard row = Instantiate(rowTemplate, list);
            row.gameObject.SetActive(true);
            row.Show(suspect);
            row.button.onClick.AddListener(() => ShowSuspect(row, suspect));
        }
    }

    public bool Back()
    {
        if (!detailView.activeSelf)
            return false;

        sounds.back.Play();
        detailView.SetActive(false);
        return true;
    }

    public void OnAppOpened() { }

    public void OnAppClosed()
    {
        detailView.SetActive(false);
    }

    private void ShowSuspect(SuspectCard row, Suspect suspect)
    {
        sounds.tap.Play();
        detailCard.Show(suspect);
        detailView.SetActive(true);

        // grow the badge out of the row that was tapped
        RectTransform cardRect = (RectTransform)detailCard.transform;
        RectTransform rowRect = (RectTransform)row.transform;
        cardRect.DOKill();
        cardRect.position = rowRect.position;
        cardRect.localScale = Vector3.one * (rowRect.rect.height / cardRect.rect.height);
        cardRect.DOAnchorPos(detailCardPos, 0.3f).SetEase(Ease.OutExpo);
        cardRect.DOScale(1f, 0.3f).SetEase(Ease.OutExpo);
    }
}

[System.Serializable]
public class Suspect
{
    public string name;
    public string jobTitle;
    public Sprite headshot;
}
