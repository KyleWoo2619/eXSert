/*
    Controls the settings that involve audio

    written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AudioSettings : MonoBehaviour
{

    [Header("Volume Settings Container Reference")]
    [SerializeField] private GameObject volumeSettingsContainer;

    [Space(20)]

    [SerializeField] private float defaultVolume = 0.5f;
    [SerializeField] private Slider masterVolumeSlider = null;
    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider sfxVolumeSlider = null;
    [SerializeField] private Slider voiceVolumeSlider = null;

    [Space(20)]

    [SerializeField] private InputActionReference _applyAction;

    void Update()
    {
        if (_applyAction.action.WasPerformedThisFrame() && volumeSettingsContainer.gameObject.activeSelf)
        {
            VolumeApply();
            Debug.Log("Audio Settings Applied");
        } 
        else 
        {
            return;
        }
    }

    //all functions below sets the volumes for each mixer depending on the slider
    public void SetSFXVolume(float volume)
    {
        SoundManager.Instance.sfxSource.volume = volume * SoundManager.Instance.masterSource.volume;
    }

    public void SetMusicVolume(float volume)
    {
        SoundManager.Instance.musicSource.volume = volume * SoundManager.Instance.masterSource.volume;
    }

    public void SetVoiceVolume(float volume)
    {
        SoundManager.Instance.voiceSource.volume = volume * SoundManager.Instance.masterSource.volume;
    }


    public void SetMasterVolume(float volume)
    {
        SoundManager.Instance.masterSource.volume = volume;
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

        SoundManager.Instance.musicSource.volume = defaultVolume;
        musicVolumeSlider.value = defaultVolume;

        SoundManager.Instance.sfxSource.volume = defaultVolume;
        sfxVolumeSlider.value = defaultVolume;

        SoundManager.Instance.voiceSource.volume = defaultVolume;
        voiceVolumeSlider.value = defaultVolume;


        VolumeApply();
    }
}
