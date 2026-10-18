using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SusPXApp : MonoBehaviour, IAppContent
{
    public SuspectCard rowTemplate;
    public RectTransform list;
    public GameObject detailView;
    public SuspectCard detailCard;
    public Button reportButton;
    public TextMeshProUGUI reportText;
    public Character[] suspects;

    // fires with the reported suspect once confirmed, for kicking off the ending
    public UnityEvent<Character> onReport;

    private PhoneSfxLibrary sounds;
    private Vector2 detailCardPos;
    private Character shownSuspect;
    private string reportLabel;
    private bool confirmingReport = false;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();
        detailCardPos = ((RectTransform)detailCard.transform).anchoredPosition;
        reportLabel = reportText.text;

        reportButton.onClick.AddListener(Report);

        foreach (Character suspect in suspects)
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
        ResetReport();
        return true;
    }

    public void OnAppOpened() { }

    public void OnAppClosed()
    {
        detailView.SetActive(false);
        ResetReport();
    }

    private void ShowSuspect(SuspectCard row, Character suspect)
    {
        sounds.tap.Play();
        shownSuspect = suspect;
        detailCard.Show(suspect);
        detailView.SetActive(true);
        ResetReport();

        // grow the badge out of the row that was tapped
        RectTransform cardRect = (RectTransform)detailCard.transform;
        RectTransform rowRect = (RectTransform)row.transform;
        cardRect.DOKill();
        cardRect.position = rowRect.position;
        cardRect.localScale = Vector3.one * (rowRect.rect.height / cardRect.rect.height);
        cardRect.DOAnchorPos(detailCardPos, 0.3f).SetEase(Ease.OutExpo);
        cardRect.DOScale(1f, 0.3f).SetEase(Ease.OutExpo);
    }

    // reporting ends the game, so the first tap only arms it
    private void Report()
    {
        if (!confirmingReport)
        {
            sounds.toggleOn.Play();
            confirmingReport = true;
            reportText.text = "CONFIRM REPORT?";
            reportButton.transform.DOScale(1.05f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(reportButton.gameObject);
            return;
        }

        sounds.reportConfirm.Play();
        reportButton.transform.DOKill();
        reportButton.transform.localScale = Vector3.one;
        reportButton.interactable = false;
        reportText.text = "REPORTED";
        onReport.Invoke(shownSuspect);
    }

    private void ResetReport()
    {
        confirmingReport = false;
        reportButton.transform.DOKill();
        reportButton.transform.localScale = Vector3.one;
        reportButton.interactable = true;
        reportText.text = reportLabel;
    }
}
