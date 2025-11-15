using UnityEngine;

namespace EnemyBehavior.Boss
{
    // Attach to a child GameObject that covers the top surface of the boss.
    // Requires a Collider set as trigger. Detects when the Player is on top and informs the brain.
    [RequireComponent(typeof(Collider))]
    public sealed class BossTopZone : MonoBehaviour
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "BossTopZone: trigger volume over the boss top surface.\n" +
            "When the Player enters, the boss can perform knock-off spin. Resize the collider to account for the boss model.\n" +
            "Optional: parent the player to carry on movement.";

        [SerializeField] private BossRoombaBrain brain;
        [SerializeField, Tooltip("If true, temporarily parent the player under the boss while on top to be carried by movement.")]
        private bool parentPlayerWhileOnTop = true;

        private Transform cachedPlayer;
        private Transform originalParent;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnValidate()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (brain == null) brain = GetComponentInParent<BossRoombaBrain>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            cachedPlayer = other.transform;
            if (brain == null) brain = GetComponentInParent<BossRoombaBrain>();
            if (brain != null) brain.SetPlayerOnTop(true);

            if (parentPlayerWhileOnTop && cachedPlayer != null)
            {
                originalParent = cachedPlayer.parent;
                cachedPlayer.SetParent(brain != null ? brain.transform : transform.root, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (brain == null) brain = GetComponentInParent<BossRoombaBrain>();
            if (brain != null) brain.SetPlayerOnTop(false);

            if (parentPlayerWhileOnTop && cachedPlayer == other.transform)
            {
                cachedPlayer.SetParent(originalParent, true);
                cachedPlayer = null;
                originalParent = null;
            }
        }
    }
}
