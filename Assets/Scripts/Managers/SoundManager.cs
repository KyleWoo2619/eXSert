/*
    This singleton will be used to manage the playing of music and sfx in any given scene. Here you can assign the source and adjust the volume of said source.
*/

using System;
using UnityEngine;
using Singletons;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{
    protected override bool ShouldPersistAcrossScenes => true;

    // Main Categories
    public AudioSource masterSource;
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    // Sub Categories
    public AudioSource ambienceSource;
    public AudioSource uiSource;
    public AudioSource puzzleSource;

    //Debug logs
    override protected void Awake()
    {
        if (masterSource == null)
        {
            Debug.LogWarning("To have sound in the scene you must have a master volume source assigned!");
        }

        if (musicSource == null)
        {
            Debug.LogWarning("To play music in the scene you must have a music source assigned!");
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("To play SFX you must have an SFX source assigned!");
        }

        if (voiceSource == null)
        {
            Debug.LogWarning("To play voices in the scene you must have a voice source assigned!");
        }

        base.Awake();

        if (SoundManager.Instance != this)
            return;

        ApplySavedVolumes();
    }

    private void ApplySavedVolumes()
    {
        if (masterSource != null && PlayerPrefs.HasKey("masterVolume"))
            masterSource.volume = PlayerPrefs.GetFloat("masterVolume");

        if (musicSource != null && PlayerPrefs.HasKey("musicVolume"))
            musicSource.volume = PlayerPrefs.GetFloat("musicVolume");

        if (sfxSource != null && PlayerPrefs.HasKey("sfxVolume"))
            sfxSource.volume = PlayerPrefs.GetFloat("sfxVolume");

        if (voiceSource != null && PlayerPrefs.HasKey("voiceVolume"))
            voiceSource.volume = PlayerPrefs.GetFloat("voiceVolume");
    }
}
