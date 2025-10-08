/*
    This singleton will be used to manage the playing of music and sfx in any given scene. Here you can assign the source and adjust the volume of said source.
*/

using System;
using UnityEngine;
using Singletons;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{

    public AudioSource musicSource;
    public AudioSource sfxSource;

    //Adjust volume of the audio sources
    [Tooltip("Adjusts Volume of SFX"), Range(0f, 1f)] public float sfxVolume;
    [Tooltip("Adjusts Volume of Music"), Range(0f, 1f)] public float musicVolume;

    //Debug logs
    override protected void Awake()
    {
        if (musicSource == null)
        {
            Debug.LogWarning("To play music in the scene you must have a music source assigned!");
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("To play SFX you must have an SFX source assigned!");
        }

        base.Awake();
    }

    //Adjusts volume for both sfx and music
    void FixedUpdate()
    {
        sfxSource.volume = sfxVolume;  
        musicSource.volume = musicVolume; 
    }
}
