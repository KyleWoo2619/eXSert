using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Simple health component for the boss. Triggers defeat when health reaches zero.
    /// You can replace this with your own health system if needed.
    /// </summary>
    public class BossHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        
        [Header("References")]
        [SerializeField] private BossRoombaBrain brain;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private bool isDefeated = false;

        void Awake()
        {
            currentHealth = maxHealth;
            
            if (brain == null)
            {
                brain = GetComponent<BossRoombaBrain>();
            }
        }

        /// <summary>
        /// Apply damage to the boss.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDefeated) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            Log($"Boss took {damage} damage. Current health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0 && !isDefeated)
            {
                OnDefeated();
            }
        }

        private void OnDefeated()
        {
            isDefeated = true;
            Log("Boss defeated!");
            
            if (brain != null)
            {
                brain.OnBossDefeated();
            }
            else
            {
                Debug.LogError("[BossHealth] BossRoombaBrain reference is missing!");
            }
        }

        /// <summary>
        /// Get the current health percentage (0-1).
        /// </summary>
        public float GetHealthPercent() => currentHealth / maxHealth;

        /// <summary>
        /// Check if the boss is defeated.
        /// </summary>
        public bool IsDefeated => isDefeated;

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BossHealth] {message}");
            }
        }

        #region Debug Context Menu
        
        [ContextMenu("Debug: Take 100 Damage")]
        private void DebugTakeDamage100()
        {
            TakeDamage(100f);
        }
        
        [ContextMenu("Debug: Instant Kill")]
        private void DebugInstantKill()
        {
            TakeDamage(currentHealth);
        }
        
        #endregion
    }
}
