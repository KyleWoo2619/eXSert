/*
Written for modular enemy health management
Separates health logic from individual enemy scripts so it can be reused across all enemy types
*/

using UnityEngine;
using UnityEngine.Events;
using Behaviors;

public class EnemyHealthManager : MonoBehaviour, IHealthSystem
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent<float> onHealthChanged; // passes current health percentage (0-1)
    [SerializeField] private UnityEvent onTakeDamage;
    
    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;
    
    private bool isDead = false;
    private BaseEnemy<EnemyState, EnemyTrigger> enemyScript;

    // IHealthSystem implementation
    public float currentHP => currentHealth;
    public float maxHP => maxHealth;

    void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get reference to the enemy script on this GameObject
        enemyScript = GetComponent<BaseEnemy<EnemyState, EnemyTrigger>>();
    }

    void Start()
    {
        // Notify listeners of initial health
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void HealHP(float healAmount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        
        Debug.Log($"{gameObject.name} healed for {healAmount}. Current HP: {currentHealth}/{maxHealth}");
    }

    public void LoseHP(float damage)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        onTakeDamage?.Invoke();
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        
        Debug.Log($"{gameObject.name} took {damage} damage. Current HP: {currentHealth}/{maxHealth}");
        
        // Check if we need to trigger health thresholds for the enemy AI
        if (enemyScript != null)
        {
            enemyScript.CheckHealthThreshold();
        }
        
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} has died!");
        
        // Trigger death event
        onDeath?.Invoke();
        
        // If there's an enemy script, trigger its death state
        if (enemyScript != null)
        {
            enemyScript.TriggerEnemyDeath();
        }
        
        // Handle destruction
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    // Public methods for external access
    public bool IsDead => isDead;
    public float HealthPercentage => currentHealth / maxHealth;
    
    // Method to set max health (useful for different enemy types)
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    
    // Method to set current health (useful for loading saves or specific setups)
    public void SetCurrentHealth(float newCurrentHealth)
    {
        currentHealth = Mathf.Clamp(newCurrentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }
}