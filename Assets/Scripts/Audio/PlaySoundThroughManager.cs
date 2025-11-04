using UnityEngine;

/// <summary>
/// Helper component to play sounds through the SoundManager singleton.
/// Useful for UnityEvents that need to play global SFX.
/// </summary>
public class PlaySoundThroughManager : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("The audio clip to play through SoundManager's SFX source")]
    [SerializeField] private AudioClip soundClip;
    
    [Tooltip("Volume for the sound (0-1)")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    
    /// <summary>
    /// Call this from UnityEvents to play the assigned sound through SoundManager.
    /// </summary>
    public void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot play sound - no clip assigned!");
            return;
        }
        
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name}: SoundManager instance not found in scene!");
            return;
        }
        
        if (SoundManager.Instance.sfxSource == null)
        {
            Debug.LogWarning($"{gameObject.name}: SoundManager's sfxSource is not assigned!");
            return;
        }
        
        SoundManager.Instance.sfxSource.PlayOneShot(soundClip, volume);
        Debug.Log($"{gameObject.name}: Playing '{soundClip.name}' through SoundManager at volume {volume}");
    }
    
    /// <summary>
    /// Play a specific clip through SoundManager.
    /// </summary>
    public void PlaySpecificSound(AudioClip clip)
    {
        if (clip == null || SoundManager.Instance == null || SoundManager.Instance.sfxSource == null)
            return;
        
        SoundManager.Instance.sfxSource.PlayOneShot(clip, volume);
    }
}
