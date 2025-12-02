using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Attach to arm hitbox root. Supports multiple collider segments (shoulder/elbow/hand).
    /// Enable/disable via Animation Events during attack sequences.
    /// </summary>
    public sealed class BossArmHitbox : MonoBehaviour
    {
        [SerializeField, Tooltip("Damage dealt by this hitbox")]
        private float damage = 1f;

        [SerializeField, Tooltip("Reference to boss brain for attack info")]
        private BossRoombaBrain bossBrain;

        [SerializeField, Tooltip("Which arm is this")]
        private ArmSide armSide = ArmSide.Left;
        
        [SerializeField, Tooltip("Root transform to search for colliders. If null, uses this GameObject. Use this for complex arm hierarchies where colliders are spread across multiple bones.")]
        private Transform colliderSearchRoot;

        private Collider[] hitboxColliders;
        private bool isActive;
        private bool hasHitThisActivation;

        public enum ArmSide { Left, Right, Center }

        private void Awake()
        {
            // If no search root specified, search from this GameObject
            Transform root = colliderSearchRoot != null ? colliderSearchRoot : transform;
            
            // Get all colliders on the root and its children (multi-segment arms)
            hitboxColliders = root.GetComponentsInChildren<Collider>(true);
            
            if (hitboxColliders.Length == 0)
            {
                Debug.LogWarning($"[BossArmHitbox] No colliders found under '{root.name}'! Hitbox will not work. Make sure colliders exist as children of the specified root.");
            }
            
            foreach (var col in hitboxColliders)
            {
                col.isTrigger = true;
            }
            
            if (bossBrain == null)
            {
                bossBrain = GetComponentInParent<BossRoombaBrain>();
            }

            DisableHitbox();
        }

        public void EnableHitbox()
        {
            foreach (var col in hitboxColliders)
            {
                if (col != null) col.enabled = true;
            }
            isActive = true;
            hasHitThisActivation = false;
            Debug.Log($"[BossArmHitbox] {armSide} arm hitbox ENABLED ({hitboxColliders.Length} segments)");
        }

        public void DisableHitbox()
        {
            foreach (var col in hitboxColliders)
            {
                if (col != null) col.enabled = false;
            }
            isActive = false;
            hasHitThisActivation = false;
            Debug.Log($"[BossArmHitbox] {armSide} arm hitbox DISABLED");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (hasHitThisActivation) return;

            if (other.CompareTag("Player"))
            {
                ApplyDamageToPlayer(other.gameObject);
                hasHitThisActivation = true;
            }
        }

        private void ApplyDamageToPlayer(GameObject player)
        {
            var healthSystem = player.GetComponent<IHealthSystem>();
            if (healthSystem != null)
            {
                if (CombatManager.isParrying && bossBrain != null)
                {
                    var currentAttack = bossBrain.GetCurrentAttack();
                    if (currentAttack != null && currentAttack.Parryable)
                    {
                        CombatManager.ParrySuccessful();
                        Debug.Log($"Player parried {currentAttack.Id}");
                        DisableHitbox();
                        return;
                    }
                }

                float finalDamage = damage;
                if (CombatManager.isGuarding)
                {
                    finalDamage *= 0.5f;
                    Debug.Log($"Player guarded - damage reduced to {finalDamage}");
                }

                healthSystem.LoseHP(finalDamage);
                Debug.Log($"Boss arm hit player for {finalDamage} damage");

                DisableHitbox();
            }
        }

        private void OnDisable()
        {
            DisableHitbox();
        }

        private void OnDrawGizmos()
        {
            if (hitboxColliders == null || hitboxColliders.Length == 0)
                hitboxColliders = GetComponentsInChildren<Collider>(true);

            Gizmos.color = isActive ? Color.red : Color.gray;
            
            foreach (var col in hitboxColliders)
            {
                if (col == null) continue;
                
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = col.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(col.transform.position + sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    Gizmos.DrawWireSphere(col.transform.position + capsule.center, capsule.radius);
                }
            }
        }
    }
}
