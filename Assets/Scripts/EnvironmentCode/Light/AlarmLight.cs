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

    private Light alarmLight;

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
    }
}
