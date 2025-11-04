/*
Written for modular enemy health management
Separates health logic from individual enemy scripts so it can be reused across all enemy types
*/

using UnityEngine;
using UnityEngine.Events;
using Behaviors;
using Stateless;

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
        
        // If the enemy has a state machine, listen for transitions so we can react to the
        // Death state when it is entered by the AI (for example external triggers like AlarmCarrier)
        if (enemyScript != null && enemyScript.enemyAI != null)
        {
            try
            {
                enemyScript.enemyAI.OnTransitioned(t => {
                    // When the state machine transitions to Death, invoke the death flow here.
                    if (t.Destination.Equals(EnemyState.Death))
                    {
                        // Ensure we only run death logic once
                        if (!isDead)
                        {
                            HandleStateMachineDeath();
                        }
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"EnemyHealthManager: Failed to subscribe to enemy state transitions: {ex}");
            }
        }
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
        
        Debug.Log($"❤️ {gameObject.name} took {damage} damage. Current HP: {currentHealth}/{maxHealth}");
        
        // Check if we need to trigger health thresholds for the enemy AI
        if (enemyScript != null)
        {
            enemyScript.CheckHealthThreshold();
        }
        
        if (currentHealth <= 0f)
        {
            // Prefer to let the enemy's state machine drive the Death state. Fire the Die trigger
            // so the AI can transition and the OnTransitioned handler above will run the death flow.
            if (enemyScript != null)
            {
                bool fired = enemyScript.TryFireTriggerByName("Die");
                if (!fired)
                {
                    // If we couldn't fire the trigger, fall back to immediate death handling
                    HandleStateMachineDeath();
                }
            }
            else
            {
                HandleStateMachineDeath();
            }
        }
    }

    private void Die()
    {
        // Kept for backward-compat immediate death calls. Use HandleStateMachineDeath for
        // state-machine-driven deaths.
        if (isDead) return;
        HandleStateMachineDeath();
    }

    // Centralized death handling called either when the health reaches zero and the AI
    // could not transition, or when the state machine transitions into the Death state.
    private void HandleStateMachineDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"💀 {gameObject.name} entering death flow (handled by EnemyHealthManager)");
        Debug.Log($"💀 onDeath has {(onDeath != null ? onDeath.GetPersistentEventCount() : 0)} listeners");

        // Trigger death event for any listeners (VFX, SFX, UI)
        if (onDeath != null)
        {
            Debug.Log($"💀 Invoking onDeath event now!");
            onDeath.Invoke();
            Debug.Log($"💀 onDeath event invoked successfully");
        }
        else
        {
            Debug.LogWarning($"💀 onDeath event is NULL - no listeners will be called!");
        }

        // If there's an enemy script, notify it that death was requested (this will call
        // TriggerEnemyDeath which is safe because we guard with isDead above)
        // We avoid calling TriggerEnemyDeath here to prevent infinite loops in cases where
        // the state machine already invoked this handler. Only call if enemy hasn't
        // already transitioned to Death (defensive).
        try
        {
            if (enemyScript != null)
            {
                // If the AI isn't already in Death, request the Die trigger so other subsystems
                // can react via the state machine. If it's already in Death, this will be ignored
                // by the state machine's configuration.
                enemyScript.TryFireTriggerByName("Die");
            }
        }
        catch { /* swallow exceptions to keep death flow robust */ }

        // Destroy the GameObject after a delay if configured
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