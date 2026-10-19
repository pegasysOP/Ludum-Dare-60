using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// cctv archive on the security computer, a list of recordings that each open full screen on the monitor
public class SecurityScreen : MonoBehaviour
{
    public RectTransform screen;
    public CanvasGroup screenGroup;
    public CanvasGroup blackout;
    public Button powerButton;
    public TextMeshProUGUI statusText;

    public CanvasGroup listView;
    public RectTransform rows;
    public Button rowTemplate;
    public Color rowColour = new Color(0.11f, 0.14f, 0.19f);
    public Color altRowColour = new Color(0.14f, 0.17f, 0.23f);

    public CanvasGroup footageView;
    public RawImage footage;
    public AspectRatioFitter footageFitter;
    public RawImage scanlines;
    public TextMeshProUGUI stampText;
    public TextMeshProUGUI stampShadow;
    public Graphic recLight;
    public Button backButton;

    public Recording[] recordings;

    private ComputerSfxLibrary sounds;
    private Vector2 screenPos;
    private Sequence sequence;
    private int openedFrame;

    private void Awake()
    {
        sounds = ComputerSfxLibrary.Load();
        screenPos = screen.anchoredPosition;

        powerButton.onClick.AddListener(Close);
        backButton.onClick.AddListener(ShowList);

        // alternate row shades like an old spreadsheet
        for (int i = 0; i < recordings.Length; i++)
        {
            Recording recording = recordings[i];
            Button row = Instantiate(rowTemplate, rows);
            row.gameObject.SetActive(true);

            TextMeshProUGUI[] columns = row.GetComponentsInChildren<TextMeshProUGUI>();
            columns[0].text = recording.date;
            columns[1].text = recording.time;
            columns[2].text = recording.location;

            ColorBlock colours = row.colors;
            colours.normalColor = i % 2 == 0 ? rowColour : altRowColour;
            colours.selectedColor = colours.normalColor;
            row.colors = colours;
            row.onClick.AddListener(() => ShowFootage(recording));
        }

        recLight.DOFade(0.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    public void Open()
    {
        if (gameObject.activeSelf)
            return;

        gameObject.SetActive(true);
        openedFrame = Time.frameCount;
        sequence?.Kill();

        screen.anchoredPosition = screenPos;
        blackout.alpha = 0f;
        screenGroup.alpha = 0f;
        screenGroup.blocksRaycasts = true;
        SetView(listView, true, 0f);
        SetView(footageView, false, 0f);
        ShowListStatus();

        sounds.wake.Play();
        sequence = DOTween.Sequence().Append(screenGroup.DOFade(1f, 0.4f));
    }

    private void Update()
    {
        // every press of the real mouse clicks, whatever it lands on. Skip the click that opened the screen
        if (screenGroup.blocksRaycasts && Time.frameCount != openedFrame
            && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            sounds.click.Play();
    }

    public void Close()
    {
        // already shutting down
        if (!screenGroup.blocksRaycasts)
            return;

        sequence?.Kill();
        screenGroup.blocksRaycasts = false;
        sounds.shutdown.Play();

        // monitor goes black, then the whole screen flies off the top, same as the office computers
        sequence = DOTween.Sequence()
            .Append(blackout.DOFade(1f, 0.25f))
            .Append(screen.DOAnchorPosY(screenPos.y + screen.rect.height + 200f, 0.45f).SetEase(Ease.InBack))
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void ShowFootage(Recording recording)
    {
        footage.texture = recording.image;
        footageFitter.aspectRatio = recording.image != null ? (float)recording.image.width / recording.image.height : 4f / 3f;

        string stamp = $"{recording.location.ToUpper()}\n{recording.date}  {recording.time}";
        stampText.text = stamp;
        stampShadow.text = stamp;
        statusText.text = $"Playback  |  {recording.location}  |  {recording.date} {recording.time}";

        // scanlines stay the same size whatever the monitor's size
        Rect area = scanlines.rectTransform.rect;
        scanlines.uvRect = new Rect(0f, 0f, area.width / 4f, area.height / 4f);

        SetView(listView, false, 0.15f);
        SetView(footageView, true, 0.15f);

        // picture tunes in from black
        footage.DOKill();
        footage.color = Color.black;
        footage.DOColor(Color.white, 0.35f).SetEase(Ease.OutQuad);
    }

    private void ShowList()
    {
        ShowListStatus();
        SetView(footageView, false, 0.15f);
        SetView(listView, true, 0.15f);
    }

    private void ShowListStatus()
    {
        statusText.text = $"{recordings.Length} recordings on file  |  Disk 87% full  |  Connected to CAM SERVER 01";
    }

    private void SetView(CanvasGroup view, bool visible, float duration)
    {
        view.DOKill();
        view.blocksRaycasts = visible;
        view.interactable = visible;
        if (duration <= 0f)
            view.alpha = visible ? 1f : 0f;
        else
            view.DOFade(visible ? 1f : 0f, duration);
    }

    [Serializable]
    public class Recording
    {
        public string date;
        public string time;
        public string location;
        public Texture image;
    }
}
