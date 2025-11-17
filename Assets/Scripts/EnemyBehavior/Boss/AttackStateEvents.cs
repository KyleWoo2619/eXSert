using UnityEngine;

namespace EnemyBehavior.Boss
{
    // Attach this to Animator states (usually Active/Recovery). It will call methods on BossAnimationHooks.
    public sealed class AttackStateEvents : StateMachineBehaviour
    {
        [Header("Hitbox/SFX IDs")]
        public string hitboxIdOnEnter;
        public string hitboxIdOnExit;
        public string sfxOnEnter;
        public string sfxOnExit;

        private BossAnimationHooks hooks;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (hooks == null) hooks = animator.GetComponentInParent<BossAnimationHooks>();
            if (hooks != null)
            {
                if (!string.IsNullOrEmpty(hitboxIdOnEnter)) hooks.EnableHitbox(hitboxIdOnEnter);
                if (!string.IsNullOrEmpty(sfxOnEnter)) hooks.PlaySfx(sfxOnEnter);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (hooks == null) hooks = animator.GetComponentInParent<BossAnimationHooks>();
            if (hooks != null)
            {
                if (!string.IsNullOrEmpty(hitboxIdOnExit)) hooks.DisableHitbox(hitboxIdOnExit);
                if (!string.IsNullOrEmpty(sfxOnExit)) hooks.StopSfx(sfxOnExit);
            }
        }
    }
}
