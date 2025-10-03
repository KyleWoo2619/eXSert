using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    private BaseEnemy<EnemyState, EnemyTrigger> enemy;
    private float health;
    private float maxHealth;
    [SerializeField] private Slider slider;

    void Start()
    {
        enemy = this.GetComponent<BaseEnemy<EnemyState, EnemyTrigger>>();

        slider.maxValue = enemy.maxHealth;
        

    }

    // Update is called once per frame
    void Update()
    {
        SetHealth();
    }

    internal void SetHealth()
    {
        health = enemy.currentHealth;
        
        slider.value = health;

    }
}
