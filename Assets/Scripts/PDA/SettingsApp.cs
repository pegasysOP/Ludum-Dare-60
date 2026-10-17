using UnityEngine;
using UnityEngine.UI;

public class SettingsApp : MonoBehaviour
{
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button quitButton;

    private void Awake()
    {
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
    }

    private void OnVolumeValueChanged(float newValue)
    {
        SettingsUtils.SetMasterVolume(newValue);

        if (AudioManager.Instance != null)
            AudioManager.Instance.UpdateVolume();
    }

    private void OnMusicVolumeChanged(float newValue)
    {
        SettingsUtils.SetMusicVolume(newValue);

        if (MusicManager.Instance != null)
            MusicManager.Instance.RefreshMasterVolume();
    }

    private void OnSFXVolumeChanged(float newValue)
    {
        SettingsUtils.SetSFXVolume(newValue);

        if (AudioManager.Instance != null)
            AudioManager.Instance.UpdateVolume();
    }

    private void OnQuitButtonClick()
    {
        SceneUtils.LoadMenuScene();
    }
}
