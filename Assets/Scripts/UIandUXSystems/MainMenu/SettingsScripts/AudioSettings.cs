/*
    Controls the settings that involve audio

    written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettings : MonoBehaviour, ISettings
{
    [SerializeField] private float defaultVolume = 0.5f;
    [SerializeField] private TMP_Text masterVolumeTextValue = null;
    [SerializeField] private Slider masterVolumeSlider = null;
    [SerializeField] private TMP_Text musicVolumeTextValue = null;
    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private TMP_Text sfxVolumeTextValue = null;
    [SerializeField] private Slider sfxVolumeSlider = null;
    [SerializeField] private TMP_Text voiceVolumeTextValue = null;
    [SerializeField] private Slider voiceVolumeSlider = null;

    //all functions below sets the volumes for each mixer depending on the slider
    public void SetSFXVolume(float volume)
    {
        SoundManager.Instance.sfxSource.volume = volume;

        sfxVolumeTextValue.text = volume.ToString("0.0");
    }

    public void SetMusicVolume(float volume)
    {
        SoundManager.Instance.musicSource.volume = volume;

        musicVolumeTextValue.text = volume.ToString("0.0");
    }

    public void SetVoiceVolume(float volume)
    {
        SoundManager.Instance.voiceSource.volume = volume;

        voiceVolumeTextValue.text = volume.ToString("0.0");
    }


    public void SetMasterVolume(float volume)
    {

        SoundManager.Instance.masterSource.volume = volume;

        masterVolumeTextValue.text = volume.ToString("0.0");
    }

    //Applies volume levels
    public void VolumeApply()
    {
        PlayerPrefs.SetFloat("masterVolume", SoundManager.Instance.masterSource.volume);
        PlayerPrefs.SetFloat("sfxVolume", SoundManager.Instance.sfxSource.volume);
        PlayerPrefs.SetFloat("musicVolume", SoundManager.Instance.musicSource.volume);
        PlayerPrefs.SetFloat("voiceVolume", SoundManager.Instance.voiceSource.volume);

    }

    //Resets settings
    public void ResetButton()
    {
        SoundManager.Instance.masterSource.volume = defaultVolume;
        masterVolumeSlider.value = defaultVolume;
        masterVolumeTextValue.text = defaultVolume.ToString("0.0");

        SoundManager.Instance.musicSource.volume = defaultVolume;
        musicVolumeSlider.value = defaultVolume;
        musicVolumeTextValue.text = defaultVolume.ToString("0.0");

        SoundManager.Instance.sfxSource.volume = defaultVolume;
        sfxVolumeSlider.value = defaultVolume;
        sfxVolumeTextValue.text = defaultVolume.ToString("0.0");

        SoundManager.Instance.voiceSource.volume = defaultVolume;
        voiceVolumeSlider.value = defaultVolume;
        voiceVolumeTextValue.text = defaultVolume.ToString("0.0");

        VolumeApply();
    }
}
