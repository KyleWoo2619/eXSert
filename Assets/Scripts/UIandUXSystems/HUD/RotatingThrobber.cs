using UnityEngine;

public class RotatingThrobber : MonoBehaviour
{
    [SerializeField, Tooltip("Rotation speed in degrees per second (positive = clockwise)")]
    private float rotationSpeed = 180f;

    // Update is called once per frame
    void Update()
    {
        // Rotate clockwise around the Z-axis (for UI elements)
        // Negative rotation = clockwise in Unity's UI coordinate system
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
