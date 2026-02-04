using Unity.VisualScripting;
using UnityEngine;

public class PlayAmbienceThroughManager : MonoBehaviour
{
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField, Range(0f, 1f)] private float clipVolume = .25f;
    [SerializeField] private bool loop = true;

    private AudioSource ambienceSource;
    private float originalSourceVolume;

    void Awake()
    {
        ambienceSource = SoundManager.Instance.ambienceSource;
        originalSourceVolume = ambienceSource.volume;
    }

    private void Start()
    {
        if(ambienceClip != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.volume = originalSourceVolume * clipVolume;
            ambienceSource.loop = loop;
            ambienceSource.Play();
        }
        else
        {
            Debug.LogWarning("Ambience clip is null!");
        }
    }
}
