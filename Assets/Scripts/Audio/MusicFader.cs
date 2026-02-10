using System.Collections;
using UnityEngine;

public class MusicFader : MonoBehaviour
{

    [SerializeField] private float fadeDuration = 2f;

    public void FadeOutMusic()
    {
        if (SoundManager.Instance == null)
            return;

        AudioSource musicSource = SoundManager.Instance.musicSource;
        if (musicSource != null)
        {
            // MusicFader is on SoundManager, which persists across scenes, so coroutine will survive
            StartCoroutine(FadeOutCoroutine(musicSource, fadeDuration));
        }
    }
    private IEnumerator FadeOutCoroutine(AudioSource musicSource, float fadeDuration)
    {
        if (musicSource == null || fadeDuration <= 0f)
            yield break;

        float startVolume = musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            musicSource.volume = newVolume;
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }
}
