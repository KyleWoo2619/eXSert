using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Attach to pillar GameObjects to detect boss collision during Cage Bull charges.
    /// Reports collision back to BossRoombaBrain to trigger stun and form change.
    /// </summary>
    public sealed class BossPillarCollider : MonoBehaviour
    {
        [SerializeField, Tooltip("Index of this pillar in the arena")]
        private int pillarIndex;

        [SerializeField, Tooltip("Reference to boss brain")]
        private BossRoombaBrain bossBrain;

        private void OnValidate()
        {
            if (bossBrain == null)
            {
                bossBrain = FindAnyObjectByType<BossRoombaBrain>();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Boss") || collision.gameObject.GetComponent<BossRoombaBrain>() != null)
            {
                if (bossBrain != null)
                {
                    Debug.Log($"[Pillar {pillarIndex}] Boss collision detected!");
                    bossBrain.OnPillarCollision(pillarIndex);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Boss") || other.GetComponent<BossRoombaBrain>() != null)
            {
                if (bossBrain != null)
                {
                    Debug.Log($"[Pillar {pillarIndex}] Boss trigger detected!");
                    bossBrain.OnPillarCollision(pillarIndex);
                }
            }
        }
    }
}
