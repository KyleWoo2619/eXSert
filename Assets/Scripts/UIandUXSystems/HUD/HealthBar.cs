using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Assign FillBlue in inspector

    public void SetHealth(float current, float max)
    {
        if (fillImage == null)
            return;

        if (max <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
