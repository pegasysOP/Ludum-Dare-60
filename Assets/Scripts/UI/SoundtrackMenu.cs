using UnityEngine;
using UnityEngine.UI;
using System;

public class SoundtrackMenu : MonoBehaviour
{
    public Button previousSongButton;
    public Button playSongButton;
    public Button nextSongButton;
    public Button menuButton; 
    public MusicLibrary musicLibrary;

    public Slider clipScrubberSlider;

    bool isPlaying = false;

    private MusicTrack[] tracks;

    //We probably want to add the image to the MusicTrack itself. 
    //We also probably want to dynamically create each Song Game Object rather than do it this way. 
    public GameObject[] songContainerGameObjects;

    public GameObject playlistContainer;
    public GameObject songContainerPrefab;


    //FIXME: SoundtrackMenu shouldn't be keeping track of index itself; it should query the MusicManager for the currently playing track.
    private int currentIndex = 0;

    private void Awake()
    {
        AddListeners();

        tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
        songContainerGameObjects = new GameObject[tracks.Length];

        if (musicLibrary == null)
            musicLibrary = Resources.Load<MusicLibrary>("MusicLibrary");
    }

    private void AddListeners()
    {
        previousSongButton.onClick.AddListener(OnPreviousSongClicked);
        playSongButton.onClick.AddListener(OnPlaySongClicked);
        nextSongButton.onClick.AddListener(OnNextSongClicked);
        menuButton.onClick.AddListener(SceneUtils.LoadMenuScene);

        if (clipScrubberSlider != null)
            clipScrubberSlider.onValueChanged.AddListener(OnScrubberValueChanged);
    }

    private void Start()
    {
        foreach (MusicTrack track in tracks)
        {
            GameObject songContainer = Instantiate(songContainerPrefab, playlistContainer.transform);
            songContainer.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = musicLibrary.entries[Array.IndexOf(tracks, track)].displayText;
            songContainer.GetComponentInChildren<Image>().sprite = musicLibrary.entries[Array.IndexOf(tracks, track)].sprite;
            songContainer.GetComponentInChildren<PulseText>().enabled = false;
            songContainerGameObjects[Array.IndexOf(tracks, track)] = songContainer;
        }
    }
    
    private void OnPreviousSongClicked()
    {
        if (musicLibrary == null)
        {
            Debug.LogWarning("SoundtrackMenu: no MusicLibrary assigned.");
            return;
        }

        if (currentIndex > 0)
        {
            currentIndex--;
            FlashCurrentSong();
            PlayCurrentIndex();
            return;
        }

        // At first track: restart the current song from the start.
        //TODO: Should this just wrap around to the last song instead? 
        PlayCurrentIndex(restart: true);
        
    }

    private void OnPlaySongClicked()
    {
        if (musicLibrary == null)
        {
            Debug.LogWarning("SoundtrackMenu: no MusicLibrary assigned.");
            return;
        }
        if (isPlaying)
        {
            isPlaying = false;
            MusicManager.Instance.PauseMusic();
            playSongButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "||";
        }
        else
        {
            PlayCurrentIndex();
            isPlaying = true;
            playSongButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "D";
        }

        FlashCurrentSong();
    }

    private void OnNextSongClicked()
    {
        if (musicLibrary == null)
        {
            Debug.LogWarning("SoundtrackMenu: no MusicLibrary assigned.");
            return;
        }

        currentIndex++;
        currentIndex %= tracks.Length;
        FlashCurrentSong();
        PlayCurrentIndex();
        return;
    }

    private void PlayCurrentIndex(bool restart = false)
    {
        if (musicLibrary == null) return;
        
        AudioClip clip = musicLibrary.Get(tracks[currentIndex]);

        if (clip == null)
        {
            Debug.LogWarning($"SoundtrackMenu: clip for track {tracks[currentIndex]} is not assigned.");
            return;
        }

        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("SoundtrackMenu: no MusicManager present in scene.");
            return;
        }

        MusicManager.Instance.PlayMusic(clip);

        if (restart)
        {
            MusicManager.Instance.RestartActive();
        }
    }

    private void Update()
    {
        ScrubMusicScrubber();
    }

    private void ScrubMusicScrubber()
    {
        if (clipScrubberSlider == null || MusicManager.Instance == null)
            return;

        AudioClip clip = MusicManager.Instance.CurrentClip;
        if (clip == null)
        {
            clipScrubberSlider.SetValueWithoutNotify(0f);
            clipScrubberSlider.interactable = false;
            return;
        }

        clipScrubberSlider.interactable = true;
        float duration = clip.length;
        float t = 0f;
        if (duration > 0f)
            t = Mathf.Clamp01(MusicManager.Instance.CurrentTime / duration);

        clipScrubberSlider.SetValueWithoutNotify(t);
    }

    private void FlashCurrentSong()
    {
        for (int i = 0; i < songContainerGameObjects.Length; i++)
        {
            if (i == currentIndex)
            {
                songContainerGameObjects[i].GetComponentInChildren<PulseText>().enabled = true;
            }
            else
            {
                songContainerGameObjects[i].GetComponentInChildren<PulseText>().enabled = false;
            }
        }
    }

    private void OnScrubberValueChanged(float value)
    {
        if (MusicManager.Instance == null) return;
        AudioClip clip = MusicManager.Instance.CurrentClip;
        if (clip == null) return;

        float time = Mathf.Clamp01(value) * clip.length;
        MusicManager.Instance.SeekActive(time);
    }
}
