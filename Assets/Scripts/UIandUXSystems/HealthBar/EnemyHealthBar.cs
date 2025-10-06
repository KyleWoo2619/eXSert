using UnityEngine;
using UnityEngine.UI;
using Behaviors;

[RequireComponent(typeof(Canvas))]
public class EnemyHealthBar : MonoBehaviour
{
    private BaseEnemy<EnemyState, EnemyTrigger> enemy;
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

    public void SetEnemy(BaseEnemy<EnemyState, EnemyTrigger> e)
    {
        enemy = e;
        target = e.transform;

        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = e.maxHP;
            slider.value    = e.currentHP;
        }
    }

    void LateUpdate()
    {
        if (!enemy || !slider || !target) return;

        // value update
        slider.value = enemy.currentHP;

        // follow + face camera (upright)
        transform.position = target.position + worldOffset;

        var cam = Camera.main;
        if (cam)
        {
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
