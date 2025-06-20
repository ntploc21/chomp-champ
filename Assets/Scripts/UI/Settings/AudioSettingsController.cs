using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private void Start()
    {
        // Load initial volume settings
        LoadSettings();
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void LoadSettings()
    {
        masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
        musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
    }
}
