using UnityEngine;
using UnityEngine.UI;

public class SettingsApp : MonoBehaviour
{
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button quitButton;

    private const float SliderTickInterval = 0.06f;

    private PhoneSfxLibrary sounds;
    private float lastTickTime;

    private void Awake()
    {
        sounds = PhoneSfxLibrary.Load();

        sensitivitySlider.value = SettingsUtils.GetSensitivity();
        volumeSlider.value = SettingsUtils.GetMasterVolume();
        musicSlider.value = SettingsUtils.GetMusicVolume();
        sfxSlider.value = SettingsUtils.GetSFXVolume();
    }

    private void OnEnable()
    {
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeValueChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        quitButton.onClick.AddListener(OnQuitButtonClick);
    }

    private void OnDisable()
    {
        sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityValueChanged);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeValueChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        quitButton.onClick.RemoveListener(OnQuitButtonClick);
    }

    private void OnSensitivityValueChanged(float newValue)
    {
        SettingsUtils.SetSensitivity(newValue);
        PlaySliderTick();
    }

    private void OnVolumeValueChanged(float newValue)
    {
        SettingsUtils.SetMasterVolume(newValue);

        if (AudioManager.Instance != null)
            AudioManager.Instance.UpdateVolume();

        PlaySliderTick();
    }

    private void OnMusicVolumeChanged(float newValue)
    {
        SettingsUtils.SetMusicVolume(newValue);

        if (MusicManager.Instance != null)
            MusicManager.Instance.RefreshMasterVolume();

        PlaySliderTick();
    }

    private void OnSFXVolumeChanged(float newValue)
    {
        SettingsUtils.SetSFXVolume(newValue);

        if (AudioManager.Instance != null)
            AudioManager.Instance.UpdateVolume();

        PlaySliderTick();
    }

    private void OnQuitButtonClick()
    {
        sounds.tap.Play();
        SceneUtils.LoadMenuScene();
    }

    // ratchet tick while dragging, plays at the new volume so it doubles as a preview
    private void PlaySliderTick()
    {
        if (Time.unscaledTime - lastTickTime < SliderTickInterval)
            return;

        lastTickTime = Time.unscaledTime;
        sounds.scrollTick.Play();
    }
}
