using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Attach to side panel colliders on the roomba.
    /// Reports damage to BossRoombaBrain which handles panel destruction.
    /// </summary>
    public sealed class BossSidePanelCollider : MonoBehaviour
    {
        [SerializeField, Tooltip("Index of this panel in the boss's SidePanels list")]
        private int panelIndex;

        [SerializeField, Tooltip("Reference to boss brain")]
        private BossRoombaBrain bossBrain;

        [SerializeField, Tooltip("Damage per hit (if using collision-based damage)")]
        private float damagePerHit = 10f;

        private void OnValidate()
        {
            if (bossBrain == null)
            {
                bossBrain = GetComponentInParent<BossRoombaBrain>();
            }
        }

        public void TakeDamage(float damage)
        {
            if (bossBrain != null)
            {
                bossBrain.DamageSidePanel(panelIndex, damage);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("PlayerWeapon"))
            {
                TakeDamage(damagePerHit);
            }
        }
    }
}
