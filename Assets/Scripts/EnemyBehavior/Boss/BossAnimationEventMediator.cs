using UnityEngine;

namespace EnemyBehavior.Boss
{
    /// <summary>
    /// Mediator for Animation Events to control hitboxes.
    /// Attach to the GameObject with the Animator component.
    /// Animation Events call methods on this script, which forwards to appropriate hitboxes.
    /// </summary>
    public sealed class BossAnimationEventMediator : MonoBehaviour
    {
        [Header("Hitbox References")]
        [SerializeField, Tooltip("Hitbox for left arm attacks")]
        private BossArmHitbox leftArmHitbox;

        [SerializeField, Tooltip("Hitbox for right arm attacks")]
        private BossArmHitbox rightArmHitbox;

        [SerializeField, Tooltip("Hitbox for center/both arm attacks")]
        private BossArmHitbox centerArmHitbox;

        [SerializeField, Tooltip("Hitbox for spin attack (usually body-based)")]
        private BossArmHitbox spinHitbox;

        [SerializeField, Tooltip("Hitbox for charge attacks (body-based)")]
        private BossArmHitbox chargeHitbox;

        [Header("Boss Brain Reference")]
        [SerializeField, Tooltip("Boss brain for arm deploy/retract callbacks (auto-found if null)")]
        private BossRoombaBrain bossBrain;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip windupSound;
        [SerializeField] private AudioClip swingSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip recoverySound;

        private void Awake()
        {
            if (bossBrain == null)
            {
                bossBrain = GetComponentInParent<BossRoombaBrain>();
            }
        }

        private void OnValidate()
        {
            if (leftArmHitbox == null || rightArmHitbox == null || centerArmHitbox == null ||
                spinHitbox == null || chargeHitbox == null)
            {
                var hitboxes = GetComponentsInChildren<BossArmHitbox>(true);
                foreach (var hitbox in hitboxes)
                {
                    if (hitbox.name.ToLower().Contains("left") && leftArmHitbox == null)
                        leftArmHitbox = hitbox;
                    else if (hitbox.name.ToLower().Contains("right") && rightArmHitbox == null)
                        rightArmHitbox = hitbox;
                    else if (hitbox.name.ToLower().Contains("center") && centerArmHitbox == null)
                        centerArmHitbox = hitbox;
                    else if (hitbox.name.ToLower().Contains("spin") && spinHitbox == null)
                        spinHitbox = hitbox;
                    else if (hitbox.name.ToLower().Contains("charge") && chargeHitbox == null)
                        chargeHitbox = hitbox;
                }
            }

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (bossBrain == null)
                bossBrain = GetComponentInParent<BossRoombaBrain>();
        }

        #region Left Arm Events
        public void EnableLeftArm()
        {
            if (leftArmHitbox != null)
            {
                leftArmHitbox.EnableHitbox();
                Debug.Log("[AnimMediator] Left arm enabled");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Left arm hitbox not assigned!");
            }
        }

        public void DisableLeftArm()
        {
            if (leftArmHitbox != null)
            {
                leftArmHitbox.DisableHitbox();
                Debug.Log("[AnimMediator] Left arm disabled");
            }
        }
        #endregion

        #region Right Arm Events
        public void EnableRightArm()
        {
            if (rightArmHitbox != null)
            {
                rightArmHitbox.EnableHitbox();
                Debug.Log("[AnimMediator] Right arm enabled");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Right arm hitbox not assigned!");
            }
        }

        public void DisableRightArm()
        {
            if (rightArmHitbox != null)
            {
                rightArmHitbox.DisableHitbox();
                Debug.Log("[AnimMediator] Right arm disabled");
            }
        }
        #endregion

        #region Center/Both Arms Events
        public void EnableCenterArm()
        {
            if (centerArmHitbox != null)
            {
                centerArmHitbox.EnableHitbox();
                Debug.Log("[AnimMediator] Center arm enabled");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Center arm hitbox not assigned!");
            }
        }

        public void DisableCenterArm()
        {
            if (centerArmHitbox != null)
            {
                centerArmHitbox.DisableHitbox();
                Debug.Log("[AnimMediator] Center arm disabled");
            }
        }


        public void EnableBothArms()
        {
            EnableLeftArm();
            EnableRightArm();
        }
        
        /// <summary>
        /// Enable both arms with dash knockback mode.
        /// Used during dashes to apply knockback even if arm hitboxes don't normally have it enabled.
        /// </summary>
        public void EnableBothArmsWithDashKnockback(float forceOverride = 0f)
        {
            if (leftArmHitbox != null)
            {
                leftArmHitbox.EnableHitboxWithDashKnockback(forceOverride);
                Debug.Log("[AnimMediator] Left arm enabled with DASH KNOCKBACK");
            }
            if (rightArmHitbox != null)
            {
                rightArmHitbox.EnableHitboxWithDashKnockback(forceOverride);
                Debug.Log("[AnimMediator] Right arm enabled with DASH KNOCKBACK");
            }
        }

        public void DisableBothArms()
        {
            DisableLeftArm();
            DisableRightArm();
        }
        #endregion

        #region Spin Attack Events
        public void EnableSpin()
        {
            if (spinHitbox != null)
            {
                spinHitbox.EnableHitbox();
                Debug.Log("[AnimMediator] Spin hitbox enabled");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Spin hitbox not assigned!");
            }
        }

        public void DisableSpin()
        {
            if (spinHitbox != null)
            {
                spinHitbox.DisableHitbox();
                Debug.Log("[AnimMediator] Spin hitbox disabled");
            }
        }
        #endregion

        #region Charge Attack Events
        public void EnableCharge()
        {
            if (chargeHitbox != null)
            {
                chargeHitbox.EnableHitbox();
                Debug.Log("[AnimMediator] Charge hitbox enabled");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Charge hitbox not assigned!");
            }
        }

        /// <summary>
        /// Enables the charge hitbox with dash knockback mode.
        /// Used for DashLungeNoArms where the charge hitbox needs to apply knockback like arms do.
        /// </summary>
        public void EnableChargeWithDashKnockback(float forceOverride = 0f)
        {
            if (chargeHitbox != null)
            {
                chargeHitbox.EnableHitboxWithDashKnockback(forceOverride);
                Debug.Log("[AnimMediator] Charge hitbox enabled with DASH KNOCKBACK");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Charge hitbox not assigned!");
            }
        }

        public void DisableCharge()
        {
            if (chargeHitbox != null)
            {
                chargeHitbox.DisableHitbox();
                Debug.Log("[AnimMediator] Charge hitbox disabled");
            }
        }
        #endregion

        #region Arms Deploy/Retract Events
        /// <summary>
        /// Called by Animation Event at end of Arms_Deploy clip.
        /// Forwards to BossRoombaBrain.
        /// </summary>
        public void OnArmsDeployComplete()
        {
            if (bossBrain != null)
            {
                bossBrain.OnArmsDeployComplete();
                Debug.Log("[AnimMediator] Arms deployment complete - forwarded to brain");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Boss brain not found! Cannot forward OnArmsDeployComplete");
            }
        }

        /// <summary>
        /// Called by Animation Event at end of Arms_Retract clip.
        /// Forwards to BossRoombaBrain.
        /// </summary>
        public void OnArmsRetractComplete()
        {
            if (bossBrain != null)
            {
                bossBrain.OnArmsRetractComplete();
                Debug.Log("[AnimMediator] Arms retract complete - forwarded to brain");
            }
            else
            {
                Debug.LogWarning("[AnimMediator] Boss brain not found! Cannot forward OnArmsRetractComplete");
            }
        }
        #endregion

        #region Disable All
        public void DisableAllHitboxes()
        {
            DisableLeftArm();
            DisableRightArm();
            DisableCenterArm();
            DisableSpin();
            DisableCharge();
            Debug.Log("[AnimMediator] All hitboxes disabled");
        }
        #endregion

        #region Audio Events
        public void PlayWindupSound()
        {
            PlaySound(windupSound);
        }

        public void PlaySwingSound()
        {
            PlaySound(swingSound);
        }

        public void PlayHitSound()
        {
            PlaySound(hitSound);
        }

        public void PlayRecoverySound()
        {
            PlaySound(recoverySound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        #endregion

        #region Visual Effects Events
        public void SpawnWindupEffect()
        {
            Debug.Log("[AnimMediator] Windup effect triggered");
        }

        public void SpawnHitEffect()
        {
            Debug.Log("[AnimMediator] Hit effect triggered");
        }

        public void SpawnRecoveryEffect()
        {
            Debug.Log("[AnimMediator] Recovery effect triggered");
        }
        #endregion

        private void OnDisable()
        {
            DisableAllHitboxes();
        }
    }
}
