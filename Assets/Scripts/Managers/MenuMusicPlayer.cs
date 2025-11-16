using UnityEngine;

/*
 * Simple script to play background music in menus.
 * Attach this to any GameObject in your main menu scene.
 * Uses SoundManager singleton to play music.
 */

public class MenuMusicPlayer : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField, Tooltip("The music clip to play in this menu")]
    private AudioClip backgroundMusic;

    [SerializeField, Range(0f, 1f), Tooltip("Volume for the background music")]
    private float musicVolume = 0.7f;

    [SerializeField, Tooltip("Should the music loop?")]
    private bool loopMusic = true;

    [SerializeField, Tooltip("Start playing music automatically when scene loads?")]
    private bool playOnStart = true;

    private void Start()
    {
        if (playOnStart)
        {
            PlayBackgroundMusic();
        }
    }

    /// <summary>
    /// Plays the background music using SoundManager
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            Debug.LogWarning("[MenuMusicPlayer] No background music assigned!");
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogError("[MenuMusicPlayer] SoundManager not found in scene! Make sure you have a SoundManager GameObject.");
            return;
        }

        if (SoundManager.Instance.musicSource == null)
        {
            Debug.LogError("[MenuMusicPlayer] SoundManager has no musicSource assigned!");
            return;
        }

        // Set the music clip
        SoundManager.Instance.musicSource.clip = backgroundMusic;
        SoundManager.Instance.musicSource.volume = musicVolume;
        SoundManager.Instance.musicSource.loop = loopMusic;

        // Play the music
        SoundManager.Instance.musicSource.Play();

        Debug.Log($"🎵 [MenuMusicPlayer] Playing background music: {backgroundMusic.name}");
    }

    /// <summary>
    /// Stops the background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.Stop();
            Debug.Log("🔇 [MenuMusicPlayer] Background music stopped");
        }
    }

    /// <summary>
    /// Pauses the background music
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.Pause();
            Debug.Log("⏸️ [MenuMusicPlayer] Background music paused");
        }
    }

    /// <summary>
    /// Resumes the background music
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.UnPause();
            Debug.Log("▶️ [MenuMusicPlayer] Background music resumed");
        }
    }

    /// <summary>
    /// Changes the background music to a different clip
    /// </summary>
    public void ChangeBackgroundMusic(AudioClip newMusic)
    {
        if (newMusic == null)
        {
            Debug.LogWarning("[MenuMusicPlayer] Cannot change to null music clip!");
            return;
        }

        backgroundMusic = newMusic;
        PlayBackgroundMusic();
    }
}
