/*
    Temporary sound test script

    by Brandon Wahl
*/
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    public AudioClip clip; //Define a clip that will be played
    public void OnClick()
    {
        AudioSource play = SoundManager.Instance.musicSource; //Grab the instance of the sound manager and the specifc source you want to use
        play.clip = clip; //Define that the clip that will be played is the clip defined above
        play.Play(); //Play clip; Usually use .Play for music tracks and .PlayOneShot for SFX
    }
}
