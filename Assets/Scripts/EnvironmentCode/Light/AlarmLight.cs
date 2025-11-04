using UnityEngine;

/*
Writen By Kyle
Manages a light to flash between two colors, typically used for alarm lights.
*/

[RequireComponent(typeof(Light))]
public class AlarmLight : MonoBehaviour
{
    [Header("Alarm Settings")]
    public Color colorA = Color.red;      // First color (usually red)
    public Color colorB = Color.white;    // Second color (usually white)
    public float speed = 2f;              // How fast the color changes
    public bool useSmoothPingPong = true; // Smooth transition or instant flash

    [Header("Sound Settings")]
    [SerializeField] private AudioClip alarmSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.5f;
    [SerializeField] private float soundInterval = 3f; // Play sound every 3 seconds

    private Light alarmLight;
    private float nextSoundTime = 0f;

    void Start()
    {
        alarmLight = GetComponent<Light>();
    }

    void Update()
    {
        if (useSmoothPingPong)
        {
            // Smoothly lerp between the two colors
            float t = Mathf.PingPong(Time.time * speed, 1f);
            alarmLight.color = Color.Lerp(colorA, colorB, t);
        }
        else
        {
            // Hard flashing (on/off style)
            float t = Mathf.PingPong(Time.time * speed, 1f);
            alarmLight.color = (t > 0.5f) ? colorA : colorB;
        }

        // Play alarm sound every soundInterval seconds
        if (alarmSound != null && Time.time >= nextSoundTime)
        {
            PlayAlarmSound();
            nextSoundTime = Time.time + soundInterval;
        }
    }

    private void PlayAlarmSound()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(alarmSound, soundVolume);
        }
        else
        {
            // Fallback: play at alarm light position
            AudioSource.PlayClipAtPoint(alarmSound, transform.position, soundVolume);
        }
    }
}
