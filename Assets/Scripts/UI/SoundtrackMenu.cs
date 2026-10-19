using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SoundtrackMenu : MonoBehaviour
{
    public MusicLibrary musicLibrary;

    [Header("Tracklist")]
    public SoundtrackRow rowTemplate;
    public RectTransform list;
    public TextMeshProUGUI countText;

    [Header("Player")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI numberText;
    public RectTransform disc;
    public Slider scrubber;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI totalTimeText;
    public Button previousButton;
    public Button playButton;
    public Button nextButton;
    public Image playIcon;
    public Sprite playSprite;
    public Sprite pauseSprite;
    public float discSpinSpeed = 90f;

    [Header("Screen")]
    public Button menuButton;
    // sky wash the menu leaves on, lifted on the way in and brought back on the way out
    public CanvasGroup fade;
    // faded and slid up one after another as the wash lifts
    public CanvasGroup[] entrance;
    public float stagger = 0.08f;

    private MusicTrack[] tracks;
    private readonly List<SoundtrackRow> rows = new List<SoundtrackRow>();
    private int currentIndex = 0;
    private bool userPaused = true;
    private bool wasPlaying = false;
    private bool leaving = false;
    private bool scrubbing = false;
    private UiSfxLibrary uiSounds;
    private PhoneSfxLibrary sounds;
    private SmoothScrollRect scroll;

    private void Awake()
    {
        if (musicLibrary == null)
            musicLibrary = Resources.Load<MusicLibrary>("MusicLibrary");

        uiSounds = UiSfxLibrary.Load();
        sounds = PhoneSfxLibrary.Load();
        tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
        scroll = list.GetComponentInParent<SmoothScrollRect>();

        previousButton.onClick.AddListener(OnPreviousClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
        scrubber.onValueChanged.AddListener(OnScrubbed);

        rowTemplate.gameObject.SetActive(false);
        for (int i = 0; i < tracks.Length; i++)
        {
            int index = i;
            SoundtrackRow row = Instantiate(rowTemplate, list);
            row.gameObject.SetActive(true);
            row.Show(i + 1, musicLibrary.GetDisplayText(tracks[i]), musicLibrary.Get(tracks[i]));
            row.button.onClick.AddListener(() => OnRowClicked(index));
            rows.Add(row);
        }
        countText.text = tracks.Length + " tracks";

        fade.alpha = 1f;
        fade.blocksRaycasts = true;
        foreach (CanvasGroup group in entrance)
            group.alpha = 0f;

        ShowTrack();
    }

    private void Start()
    {
        Sequence intro = DOTween.Sequence().SetLink(gameObject);
        intro.Append(fade.DOFade(0f, 0.4f).SetEase(Ease.InOutSine));
        intro.AppendCallback(() => fade.blocksRaycasts = false);

        for (int i = 0; i < entrance.Length; i++)
        {
            RectTransform rect = (RectTransform)entrance[i].transform;
            Vector2 target = rect.anchoredPosition;
            rect.anchoredPosition = target + Vector2.down * 24f;

            float at = 0.15f + i * stagger;
            intro.Insert(at, entrance[i].DOFade(1f, 0.4f));
            intro.Insert(at, rect.DOAnchorPos(target, 0.5f).SetEase(Ease.OutCubic));
        }
    }

    private void Update()
    {
        MusicManager music = MusicManager.Instance;
        AudioClip clip = CurrentClip;
        bool ours = music != null && clip != null && music.CurrentClip == clip;
        bool playing = ours && music.IsMusicPlaying;

        // one shot tracks stop at the end, so carry on to the next like an album would
        if (wasPlaying && !playing && !userPaused)
            PlayTrack((currentIndex + 1) % tracks.Length);
        wasPlaying = playing;

        playIcon.sprite = playing ? pauseSprite : playSprite;
        if (playing)
            disc.Rotate(0f, 0f, -discSpinSpeed * Time.deltaTime);

        float length = clip != null ? clip.length : 0f;
        // seek once on release, restarting the music on every frame of a drag would just stutter
        if (scrubbing && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
        {
            scrubbing = false;
            Seek(scrubber.value);
        }

        float time = ours ? music.CurrentTimeSamples / (float)clip.frequency : 0f;
        scrubber.interactable = ours;
        // while dragging, the handle and time show where it'll land instead of where it's playing
        if (scrubbing)
            time = scrubber.value * length;
        else
            scrubber.SetValueWithoutNotify(length > 0f ? Mathf.Clamp01(time / length) : 0f);
        currentTimeText.text = FormatTime(time);
        totalTimeText.text = FormatTime(length);
    }

    private AudioClip CurrentClip => musicLibrary != null ? musicLibrary.Get(tracks[currentIndex]) : null;

    private void OnRowClicked(int index)
    {
        Click();
        PlayTrack(index);
    }

    private void OnPlayClicked()
    {
        Click();
        MusicManager music = MusicManager.Instance;
        if (music == null)
            return;

        if (music.CurrentClip == CurrentClip && music.IsMusicPlaying)
        {
            music.PauseMusic();
            userPaused = true;
            return;
        }

        PlayTrack(currentIndex);
    }

    private void OnPreviousClicked()
    {
        Click();
        PlayTrack((currentIndex - 1 + tracks.Length) % tracks.Length);
    }

    private void OnNextClicked()
    {
        Click();
        PlayTrack((currentIndex + 1) % tracks.Length);
    }

    // picks up where it left off if that track is just paused
    private void PlayTrack(int index)
    {
        currentIndex = index;
        ShowTrack();
        // skipping can land on a song scrolled out of view
        if (scroll != null)
            scroll.ScrollToShow((RectTransform)rows[index].transform);

        MusicManager music = MusicManager.Instance;
        AudioClip clip = CurrentClip;
        if (music == null || clip == null)
        {
            Debug.LogWarning($"SoundtrackMenu: can't play {tracks[index]}, no MusicManager or clip.");
            userPaused = true;
            return;
        }

        if (music.CurrentClip == clip && !music.IsMusicPlaying)
            music.UnPauseMusic();
        else
            music.PlayMusic(clip);

        userPaused = false;
    }

    private void ShowTrack()
    {
        titleText.text = musicLibrary.GetDisplayText(tracks[currentIndex]);
        numberText.text = $"Track {currentIndex + 1} of {tracks.Length}";
        for (int i = 0; i < rows.Count; i++)
            rows[i].SetCurrent(i == currentIndex);
    }

    private void OnScrubbed(float value)
    {
        scrubbing = true;
    }

    private void Seek(float value)
    {
        MusicManager music = MusicManager.Instance;
        AudioClip clip = CurrentClip;
        if (music == null || clip == null || music.CurrentClip != clip)
            return;

        music.SeekActive(Mathf.Clamp01(value) * clip.length);
    }

    private void Click()
    {
        if (uiSounds != null)
            uiSounds.buttonPress.Play();
    }

    // sky wash back over everything before the menu loads, so the click isn't cut off
    private void OnMenuClicked()
    {
        if (leaving)
            return;

        leaving = true;
        sounds.tap.Play();
        fade.blocksRaycasts = true;
        fade.DOFade(1f, 0.3f).SetEase(Ease.InOutSine).SetLink(gameObject).OnComplete(SceneUtils.LoadMenuScene);
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }
}
