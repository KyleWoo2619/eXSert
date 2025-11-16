using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Assign FillBlue in inspector

    public void SetHealth(float current, float max)
    {
        fillImage.fillAmount = current / max;
    }
}
