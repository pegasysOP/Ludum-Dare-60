using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SusPXApp : MonoBehaviour, IAppContent, IAppNotification
{
    public SuspectCard rowTemplate;
    public RectTransform list;
    public GameObject detailView;
    public SuspectCard detailCard;
    public Button reportButton;
    public TextMeshProUGUI reportText;
    public Character[] suspects;

    [Header("Notes")]
    public RectTransform notesList;
    public SuspectNoteRow noteTemplate;
    public GameObject noNotesText;
    public Color againstColour = new Color(1f, 0.35f, 0.32f);
    public Color forColour = new Color(0.4f, 0.85f, 0.5f);
    public Color neutralColour = new Color(1f, 0.85f, 0.82f, 0.7f);

    // fires with the reported suspect once confirmed, for kicking off the ending
    public UnityEvent<Character> onReport;

    private PhoneSfxLibrary sounds;
    private Vector2 detailCardPos;
    private Character shownSuspect;
    private string reportLabel;
    private bool confirmingReport = false;

    private GameMood currentMood;
    private readonly Dictionary<Character, SuspectCard> rows = new Dictionary<Character, SuspectCard>();
    private readonly List<SuspectNoteRow> noteRows = new List<SuspectNoteRow>();

    // notes can arrive before the app has ever been opened, so the dot reads straight from them
    public bool HasNotification => SuspectNotes.HasUnseen;

    // notes pile up through the game, so one buzz is enough
    public bool KeepsBuzzing => false;

    public event Action NotificationChanged
    {
        add { SuspectNotes.Changed += value; }
        remove { SuspectNotes.Changed -= value; }
    }

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
            rows[suspect] = row;
        }
        noteTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        SuspectNotes.Changed += OnNotesChanged;
        OnNotesChanged();
    }

    private void OnDisable()
    {
        SuspectNotes.Changed -= OnNotesChanged;
    }

    private void OnNotesChanged()
    {
        foreach (KeyValuePair<Character, SuspectCard> row in rows)
            row.Value.ShowNotes(SuspectNotes.For(row.Key).Count, SuspectNotes.HasUnseenFor(row.Key));

        // a note can land while their card is open
        if (detailView.activeSelf && shownSuspect != null && SuspectNotes.HasUnseenFor(shownSuspect))
            ShowNotes(shownSuspect);
    }

    private void ShowNotes(Character suspect)
    {
        foreach (SuspectNoteRow noteRow in noteRows)
            Destroy(noteRow.gameObject);
        noteRows.Clear();

        List<SuspectNote> notes = SuspectNotes.For(suspect);
        foreach (SuspectNote note in notes)
        {
            SuspectNoteRow noteRow = Instantiate(noteTemplate, notesList);
            noteRow.gameObject.SetActive(true);
            noteRow.Show(note, note.lean == NoteLean.Against ? againstColour : note.lean == NoteLean.For ? forColour : neutralColour);
            noteRows.Add(noteRow);
        }
        noNotesText.SetActive(notes.Count == 0);
        notesList.anchoredPosition = Vector2.zero;

        SuspectNotes.MarkSeen(suspect);
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

    public void OnAppOpened()
    {
        currentMood = GameMusicGuy.CurrentMood;
        GameMusicGuy.SetMood(GameMood.Red);
    }

    public void OnAppClosed()
    {
        detailView.SetActive(false);
        ResetReport();
        GameMusicGuy.SetMood(currentMood);
    }

    private void ShowSuspect(SuspectCard row, Character suspect)
    {
        sounds.tap.Play();
        shownSuspect = suspect;
        detailCard.Show(suspect);
        detailView.SetActive(true);
        ShowNotes(suspect);
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
