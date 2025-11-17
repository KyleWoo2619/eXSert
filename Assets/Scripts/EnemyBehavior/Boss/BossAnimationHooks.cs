using UnityEngine;
using System.Linq;

namespace EnemyBehavior.Boss
{
    public sealed class BossAnimationHooks : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonoBehaviour brainComponent; // assign BossRoombaBrain in inspector
        private Component brain;

        private void Awake()
        {
            if (brainComponent != null) brain = brainComponent; else brain = FindBrain();
        }

        private Component FindBrain()
        {
            // Late lookup by type name to avoid direct compile-time dependency issues
            var comps = GetComponentsInParent<MonoBehaviour>(true);
            return comps.FirstOrDefault(c => c.GetType().Name == "BossRoombaBrain");
        }

        // Hitbox toggles (extend as needed)
        public void EnableHitbox(string id)
        {
            // TODO: look up and enable a collider by id
            // Example: GetComponentInChildren<BossHitboxRegistry>()?.Enable(id);
        }
        public void DisableHitbox(string id)
        {
            // TODO: look up and disable a collider by id
        }

        // SFX hooks
        public void PlaySfx(string id)
        {
            // TODO: route to audio system
        }
        public void StopSfx(string id)
        {
            // TODO: route to audio system
        }

        // Example: signal brain explicitly if an animation phase is reached (optional)
        public void NotifyPhase(string phase)
        {
            // e.g., brain?.OnAnimPhase(phase);
        }
    }
}
