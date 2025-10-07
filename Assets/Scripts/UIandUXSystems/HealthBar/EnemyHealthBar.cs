using UnityEngine;
using UnityEngine.UI;
using Behaviors;

[RequireComponent(typeof(Canvas))]
public class EnemyHealthBar : MonoBehaviour
{
    private EnemyHealthManager healthManager;
    private BaseEnemy<EnemyState, EnemyTrigger> enemy;
    private IHealthSystem healthSystem; // Can be either enemy or healthManager
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0);
    private Transform target;

    void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);

        var c = GetComponent<Canvas>();
        if (c.renderMode == RenderMode.WorldSpace && c.worldCamera == null && Camera.main)
            c.worldCamera = Camera.main;
    }

    // Method to set up with EnemyHealthManager (preferred)
    public void SetEnemyHealthManager(EnemyHealthManager manager)
    {
        healthManager = manager;
        healthSystem = manager;
        target = manager.transform;
        
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = manager.maxHP;
            slider.value = manager.currentHP;
        }
        
        Debug.Log($"{gameObject.name}: Health bar set up with EnemyHealthManager");
    }

    // Method to set up with enemy directly (for backward compatibility)
    public void SetEnemy(BaseEnemy<EnemyState, EnemyTrigger> e)
    {
        enemy = e;
        healthSystem = e;
        target = e.transform;

        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = e.maxHP;
            slider.value = e.currentHP;
        }
        
        Debug.Log($"{gameObject.name}: Health bar set up with BaseEnemy health system");
    }

    void LateUpdate()
    {
        if (healthSystem == null || !slider || !target) return;

        // value update using the health system interface
        slider.value = healthSystem.currentHP;

        // follow enemy position (BillboardUI component handles rotation)
        transform.position = target.position + worldOffset;
        
        // DEBUG: Log any unwanted rotation
        Vector3 currentRotation = transform.eulerAngles;
        if (Mathf.Abs(currentRotation.x) > 0.1f || Mathf.Abs(currentRotation.z) > 0.1f)
        {
            // Debug.LogWarning($"{gameObject.name}: Unwanted rotation detected! X:{currentRotation.x:F2}, Y:{currentRotation.y:F2}, Z:{currentRotation.z:F2}");
        }
    }
}
