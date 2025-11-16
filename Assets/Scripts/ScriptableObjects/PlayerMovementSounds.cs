using UnityEngine;

/// <summary>
/// ScriptableObject for player movement sound effects.
/// Contains arrays of sound clips for footsteps, jumps, and landings.
/// Matches the pattern used by PlayerAttack.cs for consistency.
/// </summary>
[CreateAssetMenu(fileName = "PlayerMovementSounds", menuName = "Audio/Player Movement Sounds")]
public class PlayerMovementSounds : ScriptableObject
{
    [Header("Footstep Sounds")]
    [Tooltip("Array of footstep sounds. Will play randomly for variety.")]
    [SerializeField] private AudioClip[] _footstepSounds;
    public AudioClip[] footstepSounds => _footstepSounds;

    [Tooltip("Volume for footstep sounds (0-1)")]
    [SerializeField, Range(0f, 1f)] private float _footstepVolume = 0.7f;
    public float footstepVolume => _footstepVolume;

    [Header("Jump Sounds")]
    [Tooltip("Array of jump/leap sounds (gasps, grunts). Will play randomly.")]
    [SerializeField] private AudioClip[] _jumpSounds;
    public AudioClip[] jumpSounds => _jumpSounds;

    [Tooltip("Volume for jump sounds (0-1)")]
    [SerializeField, Range(0f, 1f)] private float _jumpVolume = 0.8f;
    public float jumpVolume => _jumpVolume;

    [Header("Landing Sounds")]
    [Tooltip("Array of landing sounds. Will play randomly.")]
    [SerializeField] private AudioClip[] _landingSounds;
    public AudioClip[] landingSounds => _landingSounds;

    [Tooltip("Volume for landing sounds (0-1)")]
    [SerializeField, Range(0f, 1f)] private float _landingVolume = 0.8f;
    public float landingVolume => _landingVolume;

    [Header("Dash Sounds")]
    [Tooltip("Array of dash sounds (whoosh, effort grunt). Will play randomly.")]
    [SerializeField] private AudioClip[] _dashSounds;
    public AudioClip[] dashSounds => _dashSounds;

    [Tooltip("Volume for dash sounds (0-1)")]
    [SerializeField, Range(0f, 1f)] private float _dashVolume = 0.75f;
    public float dashVolume => _dashVolume;

    /// <summary>
    /// Gets a random footstep sound from the array.
    /// Returns null if array is empty or null.
    /// </summary>
    public AudioClip GetRandomFootstep()
    {
        if (_footstepSounds == null || _footstepSounds.Length == 0) return null;
        return _footstepSounds[Random.Range(0, _footstepSounds.Length)];
    }

    /// <summary>
    /// Gets a random jump sound from the array.
    /// Returns null if array is empty or null.
    /// </summary>
    public AudioClip GetRandomJump()
    {
        if (_jumpSounds == null || _jumpSounds.Length == 0) return null;
        return _jumpSounds[Random.Range(0, _jumpSounds.Length)];
    }

    /// <summary>
    /// Gets a random landing sound from the array.
    /// Returns null if array is empty or null.
    /// </summary>
    public AudioClip GetRandomLanding()
    {
        if (_landingSounds == null || _landingSounds.Length == 0) return null;
        return _landingSounds[Random.Range(0, _landingSounds.Length)];
    }

    /// <summary>
    /// Gets a random dash sound from the array.
    /// Returns null if array is empty or null.
    /// </summary>
    public AudioClip GetRandomDash()
    {
        if (_dashSounds == null || _dashSounds.Length == 0) return null;
        return _dashSounds[Random.Range(0, _dashSounds.Length)];
    }

    /// <summary>
    /// Helper to play a footstep sound through the SoundManager's SFX source.
    /// Call this from animation events or code.
    /// </summary>
    public void PlayRandomFootstep()
    {
        var clip = GetRandomFootstep();
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(clip, footstepVolume);
        }
    }

    /// <summary>
    /// Helper to play a jump sound through the SoundManager's SFX source.
    /// </summary>
    public void PlayRandomJump()
    {
        var clip = GetRandomJump();
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(clip, jumpVolume);
        }
    }

    /// <summary>
    /// Helper to play a landing sound through the SoundManager's SFX source.
    /// </summary>
    public void PlayRandomLanding()
    {
        var clip = GetRandomLanding();
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(clip, landingVolume);
        }
    }

    /// <summary>
    /// Helper to play a dash sound through the SoundManager's SFX source.
    /// </summary>
    public void PlayRandomDash()
    {
        var clip = GetRandomDash();
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(clip, dashVolume);
        }
    }
}
