/*
Written by Brandon Wahl

Uses the health interfaces to increase or decreae hp amount and sets the healthbar accordingly

*/

using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerHealthBarManager : MonoBehaviour, IHealthSystem, IDataPersistenceManager
{
    [Serializable]
    public readonly struct HealthSnapshot
    {
        public readonly float current;
        public readonly float max;

        public float Normalized => max <= 0f ? 0f : current / max;

        public HealthSnapshot(float current, float max)
        {
            this.current = Mathf.Max(0f, current);
            this.max = Mathf.Max(0f, max);
        }
    }

    public static event Action<float> OnPlayerDamaged;
    public static event Action<float> OnPlayerHealed;
    public static event Action<HealthSnapshot> OnPlayerHealthChanged;
    public static event Action OnPlayerDied;
    public static event Action<PlayerHealthBarManager> OnPlayerHealthRegistered;

    [Header("Health Settings")]
    [SerializeField, Min(1f)] private float maxHealth = 500f;
    [SerializeField] private float currentHealth = -1f;
    [SerializeField, Range(0f, 1f)] private float startingHealthPercent = 1f;
    [SerializeField, Tooltip("When true, all incoming damage is ignored.")] private bool invulnerable = false;

    [Header("Death Handling")]
    [SerializeField, Tooltip("Automatically restart from the active checkpoint when the player dies.")]
    private bool restartFromCheckpointOnDeath = true;
    [SerializeField, Tooltip("Destroy the player GameObject after death once cleanup logic runs.")]
    private bool destroyPlayerOnDeath = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent<float> onHealthChanged;
    [SerializeField] private UnityEvent onTakeDamage;

    public static PlayerHealthBarManager Instance { get; private set; }

    float IHealthSystem.currentHP => CurrentHealth;
    float IHealthSystem.maxHP => MaxHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public bool IsDead => isDead;

    private bool isDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(PlayerHealthBarManager)} detected on {name}. Destroying duplicate component.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (currentHealth < 0f)
        {
            currentHealth = Mathf.Clamp(maxHealth * Mathf.Clamp01(startingHealthPercent), 0f, maxHealth);
        }

        NotifyHealthChanged();
        OnPlayerHealthRegistered?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            OnPlayerHealthRegistered?.Invoke(null);
        }
    }

    public void HealHP(float hp)
    {
        if (isDead || hp <= 0f)
            return;

        float previous = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + hp);
        float actual = currentHealth - previous;
        if (actual <= 0f)
            return;

        OnPlayerHealed?.Invoke(actual);
        NotifyHealthChanged();
    }

    public void LoseHP(float damage)
    {
        if (isDead || invulnerable || damage <= 0f)
            return;

        float previous = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        float actual = previous - currentHealth;
        if (actual <= 0f)
            return;

        onTakeDamage?.Invoke();
        OnPlayerDamaged?.Invoke(actual);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void ForceFullHeal(bool notifyListeners = true)
    {
        isDead = false;
        currentHealth = maxHealth;
        if (notifyListeners)
        {
            NotifyHealthChanged();
        }
    }

    public void Revive(float percentOfMax = 1f)
    {
        isDead = false;
        currentHealth = Mathf.Clamp(maxHealth * Mathf.Clamp01(percentOfMax), 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        NotifyHealthChanged();
    }
    
    public void SetCurrentHealth(float newCurrentHealth)
    {
        currentHealth = Mathf.Clamp(newCurrentHealth, 0f, maxHealth);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void LoadData(GameData data)
    {
        maxHealth = data.maxHealth > 0 ? data.maxHealth : maxHealth;
        currentHealth = Mathf.Clamp(data.health, 0f, maxHealth);
        isDead = currentHealth <= 0f;
        NotifyHealthChanged();
    }

    public void SaveData(GameData data)
    {
        data.maxHealth = maxHealth;
        data.health = currentHealth;
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0f;

        onDeath?.Invoke();
        OnPlayerDied?.Invoke();

        if (restartFromCheckpointOnDeath && SceneLoader.Instance != null)
        {
            SceneLoader.Instance.RestartFromCheckpoint();
        }

        if (destroyPlayerOnDeath)
        {
            Destroy(gameObject);
        }
    }

    private void NotifyHealthChanged()
    {
        var snapshot = new HealthSnapshot(currentHealth, maxHealth);
        onHealthChanged?.Invoke(snapshot.Normalized);
        OnPlayerHealthChanged?.Invoke(snapshot);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        if (!other.TryGetComponent(out HitboxDamageManager hitbox))
            return;

        LoseHP(hitbox.damageAmount);
    }

    public void SetInvulnerable(bool value) => invulnerable = value;
}
