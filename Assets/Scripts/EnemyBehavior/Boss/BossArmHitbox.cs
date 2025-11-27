using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Attach to arm hitbox colliders. Enable/disable via Animation Events during attack sequences.
    /// Call EnableHitbox() at start of active frames, DisableHitbox() at start of recovery.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BossArmHitbox : MonoBehaviour
    {
        [SerializeField, Tooltip("Damage dealt by this hitbox")]
        private float damage = 1f;

        [SerializeField, Tooltip("Reference to boss brain for attack info")]
        private BossRoombaBrain bossBrain;

        [SerializeField, Tooltip("Which arm is this")]
        private ArmSide armSide = ArmSide.Left;

        private Collider hitboxCollider;
        private bool isActive;

        public enum ArmSide { Left, Right, Center }

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider>();
            hitboxCollider.isTrigger = true;
            
            if (bossBrain == null)
            {
                bossBrain = GetComponentInParent<BossRoombaBrain>();
            }

            DisableHitbox();
        }

        public void EnableHitbox()
        {
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = true;
                isActive = true;
                Debug.Log($"[BossArmHitbox] {armSide} arm hitbox ENABLED");
            }
        }

        public void DisableHitbox()
        {
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
                isActive = false;
                Debug.Log($"[BossArmHitbox] {armSide} arm hitbox DISABLED");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;

            if (other.CompareTag("Player"))
            {
                ApplyDamageToPlayer(other.gameObject);
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
            if (hitboxCollider == null) hitboxCollider = GetComponent<Collider>();

            Gizmos.color = isActive ? Color.red : Color.gray;
            
            if (hitboxCollider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (hitboxCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (hitboxCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
            }
        }
    }
}
