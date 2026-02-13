/*
Written for modular enemy health management
Separates health logic from individual enemy scripts so it can be reused across all enemy types
*/

using UnityEngine;
using UnityEngine.Events;
using Behaviors;
using Stateless;
using System.Collections;

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

    public delegate void KillCountProgression();
    public static event KillCountProgression onDeathEvent;

    private bool isDead = false;
    private BaseEnemy<EnemyState, EnemyTrigger> enemyScript;
    private bool subscribedToStateMachine;
    private Coroutine deathFallbackRoutine;

    // IHealthSystem implementation
    public float currentHP => currentHealth;
    public float maxHP => maxHealth;

    void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get reference to the enemy script on this GameObject
        enemyScript = GetComponent<BaseEnemy<EnemyState, EnemyTrigger>>();

        TrySubscribeToEnemyStateMachine();
    }

    void Start()
    {
        // Notify listeners of initial health
        onHealthChanged?.Invoke(currentHealth / maxHealth);

        // Some enemies hydrate their AI in Start/Awake order we don't control, so poll briefly.
        if (!subscribedToStateMachine && enemyScript != null)
        {
            StartCoroutine(WaitForStateMachineAndSubscribe());
        }
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
        
        // Note: We intentionally do NOT call enemyScript.CheckHealthThreshold() here
        // because EnemyHealthManager handles the death flow independently via its own
        // TryFireTriggerByName("Die") call below. Calling CheckHealthThreshold would
        // result in duplicate death events being fired.
        // However, we still need to check for LowHealth threshold for flee/recover behaviors.
        if (enemyScript != null && currentHealth > 0f)
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
                else
                {
                    ScheduleDeathFallback();
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

        if (deathFallbackRoutine != null)
        {
            StopCoroutine(deathFallbackRoutine);
            deathFallbackRoutine = null;
        }

        Debug.Log($"{gameObject.name} entering death flow (handled by EnemyHealthManager)");

        // Trigger death event for any listeners (VFX, SFX, UI)
        onDeathEvent?.Invoke();
        onDeath?.Invoke();

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

    private void TrySubscribeToEnemyStateMachine()
    {
        if (subscribedToStateMachine || enemyScript == null)
            return;

        if (enemyScript.enemyAI == null)
            return;

        try
        {
            enemyScript.enemyAI.OnTransitioned(t =>
            {
                if (t.Destination.Equals(EnemyState.Death))
                {
                    HandleStateMachineDeath();
                }
            });
            subscribedToStateMachine = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"EnemyHealthManager: Failed to subscribe to enemy state transitions: {ex}");
        }
    }

    private IEnumerator WaitForStateMachineAndSubscribe()
    {
        const float timeout = 3f;
        float elapsed = 0f;

        while (!subscribedToStateMachine && enemyScript != null && enemyScript.enemyAI == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        TrySubscribeToEnemyStateMachine();
    }

    private void ScheduleDeathFallback()
    {
        if (deathFallbackRoutine != null || isDead)
            return;

        deathFallbackRoutine = StartCoroutine(DeathFallbackRoutine());
    }

    private IEnumerator DeathFallbackRoutine()
    {
        // Allow time for the state machine to play its death animation before forcing a cleanup.
        yield return WaitForSecondsCache.Get(4f);
        deathFallbackRoutine = null;
        HandleStateMachineDeath();
    }
}