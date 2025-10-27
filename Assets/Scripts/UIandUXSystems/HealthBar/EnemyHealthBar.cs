using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    private IHealthSystem enemy;
    private Transform enemyTransform;
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0); // Offset above enemy

    public void SetEnemy(IHealthSystem enemy)
    {
        this.enemy = enemy;
        // Try to cast to MonoBehaviour to get the transform
        if (enemy is MonoBehaviour mb)
            enemyTransform = mb.transform;
        else
            enemyTransform = null;

        if (slider != null)
        {
            slider.maxValue = enemy.maxHP;
            slider.value = enemy.currentHP;
        }
    }

    public static EnemyHealthBar SetupHealthBar(GameObject healthBarPrefab, IHealthSystem enemy)
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene for health bar instantiation.");
            return null;
        }
        var healthBarObj = Object.Instantiate(healthBarPrefab, canvas.transform);
        var healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            Debug.LogError("healthBarPrefab does not have an EnemyHealthBar component.");
            return null;
        }
        healthBar.SetEnemy(enemy);
        return healthBar;
    }

    void Update()
    {
        if (enemy == null || slider == null || enemyTransform == null)
            return;

        // Update health value
        slider.value = enemy.currentHP;

        // Position health bar above enemy in screen space
        if (Camera.main != null)
        {
            Vector3 worldPos = enemyTransform.position + worldOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            transform.position = screenPos;
        }
    }
}
