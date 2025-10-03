using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    private TestingEnemy enemy;
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0); // Offset above enemy

    public void SetEnemy(TestingEnemy enemy)
    {
        this.enemy = enemy;
        if (slider != null)
        {
            slider.maxValue = enemy.maxHP;
            slider.value = enemy.currentHP;
        }
    }

    void Update()
    {
        if (enemy == null || slider == null)
            return;

        // Update health value
        slider.value = enemy.currentHP;

        // Position health bar above enemy in screen space
        if (Camera.main != null)
        {
            Vector3 worldPos = enemy.transform.position + worldOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            transform.position = screenPos;
        }
    }
}
