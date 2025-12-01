using System;
using System.Collections;
using UnityEngine;

using Utilities.Combat;
using Utilities.Combat.Attacks;

public class PlayerAttackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private TierComboManager tierComboManager;
    [SerializeField] private AerialComboManager aerialComboManager;
    [SerializeField] private AttackDatabase attackDatabase;
    [SerializeField] private CharacterController characterController;

    [Header("Stance Switching")]
    [SerializeField, Range(0.1f, 5f)] private float stanceCooldownTime = 1f;
    [SerializeField] private AudioClip changeStanceAudio;

    private AudioSource sfxSource;
    private Coroutine stanceCooldownRoutine;
    private GameObject activeHitbox;
    private PlayerAttack currentAttack;
    private Coroutine hitboxLifetimeRoutine;

    [Header("Input Buffering")]
    [SerializeField, Range(0.05f, 0.6f)] private float inputBufferWindow = 0.25f;

    private enum AttackButton { None, Light, Heavy }
    private AttackButton bufferedAttackButton = AttackButton.None;
    private float bufferedAttackExpiresAt = -1f;

    public static event Action<PlayerAttack> OnAttack;

    private void Awake()
    {
        animationController ??= GetComponent<PlayerAnimationController>() ?? GetComponentInChildren<PlayerAnimationController>();
        tierComboManager ??= GetComponent<TierComboManager>() ?? GetComponentInChildren<TierComboManager>() ?? GetComponentInParent<TierComboManager>();
        aerialComboManager ??= GetComponent<AerialComboManager>() ?? GetComponentInChildren<AerialComboManager>() ?? GetComponentInParent<AerialComboManager>();
        characterController ??= GetComponent<CharacterController>();

        if (attackDatabase == null)
        {
            attackDatabase = Resources.Load<AttackDatabase>("AttackDatabase");
            if (attackDatabase == null)
                Debug.LogWarning("[PlayerAttackManager] AttackDatabase reference is missing.");
        }
    }

    private void Start()
    {
        sfxSource = SoundManager.Instance != null ? SoundManager.Instance.sfxSource : null;
    }

    private void OnDisable()
    {
        if (stanceCooldownRoutine != null)
        {
            StopCoroutine(stanceCooldownRoutine);
            stanceCooldownRoutine = null;
        }
        if (hitboxLifetimeRoutine != null)
        {
            StopCoroutine(hitboxLifetimeRoutine);
            hitboxLifetimeRoutine = null;
        }

        ClearHitbox();
        InputReader.inputBusy = false;
    }

    private void Update()
    {
        if (InputReader.LightAttackTriggered)
            ProcessAttackInput(true);

        if (InputReader.HeavyAttackTriggered)
            ProcessAttackInput(false);

        if (InputReader.ChangeStanceTriggered)
            TryChangeStance();
    }

    private void ProcessAttackInput(bool lightAttack)
    {
        if (!InputReader.inputBusy)
        {
            if (lightAttack)
                OnLightAttack();
            else
                OnHeavyAttack();
        }
        else
        {
            BufferAttack(lightAttack);
        }
    }

    private void TryChangeStance()
    {
        if (stanceCooldownRoutine != null)
            return;

        CombatManager.ChangeStance();

        if (changeStanceAudio != null && sfxSource != null)
            sfxSource.PlayOneShot(changeStanceAudio);

        stanceCooldownRoutine = StartCoroutine(StanceChangeCooldown());
    }

    private IEnumerator StanceChangeCooldown()
    {
        yield return new WaitForSeconds(stanceCooldownTime);
        stanceCooldownRoutine = null;
    }

    public void OnLightAttack()
    {
        if (InputReader.inputBusy)
            return;

        AttemptAttack(true);
    }

    public void OnHeavyAttack()
    {
        if (InputReader.inputBusy)
            return;

        AttemptAttack(false);
    }

    private void BufferAttack(bool lightAttack)
    {
        bufferedAttackButton = lightAttack ? AttackButton.Light : AttackButton.Heavy;
        bufferedAttackExpiresAt = Time.time + inputBufferWindow;
    }

    private void AttemptAttack(bool lightAttack)
    {
        tierComboManager?.CancelComboResetCountdown();

        string attackId = ResolveAttackId(lightAttack);
        if (string.IsNullOrEmpty(attackId))
            return;

        PlayerAttack attackData = attackDatabase != null ? attackDatabase.GetAttack(attackId) : null;
        if (attackData == null)
        {
            Debug.LogWarning($"[PlayerAttackManager] Attack '{attackId}' not found in database.");
            return;
        }

        ExecuteAttack(attackData, attackId);
    }

    private string ResolveAttackId(bool lightAttack)
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            if (tierComboManager == null)
            {
                Debug.LogWarning("[PlayerAttackManager] TierComboManager missing; cannot resolve grounded attack.");
                return null;
            }

            TierComboManager.AttackStance stance = CombatManager.singleTargetMode
                ? TierComboManager.AttackStance.Single
                : TierComboManager.AttackStance.AOE;

            return lightAttack
                ? tierComboManager.RequestFastAttack(stance)
                : tierComboManager.RequestHeavyAttack(stance);
        }

        if (aerialComboManager == null)
        {
            Debug.LogWarning("[PlayerAttackManager] AerialComboManager missing; cannot resolve aerial attack.");
            return null;
        }

        return lightAttack
            ? aerialComboManager.RequestAerialFastAttack()
            : aerialComboManager.RequestAerialHeavyAttack();
    }

    private void ExecuteAttack(PlayerAttack attackData, string attackId)
    {
        currentAttack = attackData;
        InputReader.inputBusy = true;

        if (attackData.attackSFX != null)
        {
            AudioSource attackSource = attackData._sfxSource;
            if (attackSource != null)
                attackSource.PlayOneShot(attackData.attackSFX);
        }

        animationController?.PlayAttack(attackId);

        OnAttack?.Invoke(attackData);

        Debug.Log($"[PlayerAttackManager] Executing attack {attackData.attackName} ({attackId})");
    }

    private bool IsGrounded()
    {
        if (characterController != null)
            return characterController.isGrounded;

        return PlayerMovement.isGrounded;
    }

    private void ClearHitbox()
    {
        if (hitboxLifetimeRoutine != null)
        {
            StopCoroutine(hitboxLifetimeRoutine);
            hitboxLifetimeRoutine = null;
        }

        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }
    }

    private void SpawnHitbox(PlayerAttack attack)
    {
        ClearHitbox();

        activeHitbox = attack.createHitBox(transform.position, transform.forward);
        if (activeHitbox != null)
            activeHitbox.transform.SetParent(transform, worldPositionStays: true);
    }

    public void SpawnOneShotHitbox(string attackId, float activeDuration)
    {
        var attackData = attackDatabase?.GetAttack(attackId);
        if (attackData == null)
        {
            Debug.LogWarning($"[PlayerAttackManager] Cannot spawn hitbox; attack '{attackId}' missing.");
            return;
        }

        TriggerHitboxWindow(attackData, Mathf.Max(0f, activeDuration));
    }

    private void TriggerHitboxWindow(PlayerAttack attack, float overrideDuration)
    {
        if (attack == null)
            return;

        SpawnHitbox(attack);

        float lifetime = overrideDuration >= 0f
            ? overrideDuration
            : attack.hitboxDuration;

        BeginHitboxLifetime(lifetime);
    }

    private void BeginHitboxLifetime(float duration)
    {
        if (duration <= 0f)
        {
            ClearHitbox();
            return;
        }

        if (hitboxLifetimeRoutine != null)
            StopCoroutine(hitboxLifetimeRoutine);

        hitboxLifetimeRoutine = StartCoroutine(HitboxLifetimeRoutine(duration));
    }

    private IEnumerator HitboxLifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        hitboxLifetimeRoutine = null;
        ClearHitbox();
    }

    #region Animation Event Hooks
    public void HandleAnimationHitbox()
    {
        HandleAnimationHitbox(-1f);
    }

    public void HandleAnimationHitbox(float overrideDuration)
    {
        if (currentAttack == null)
        {
            Debug.LogWarning("[PlayerAttackManager] Animation requested a hitbox but no attack is active.");
            return;
        }

        TriggerHitboxWindow(currentAttack, overrideDuration);
    }

    public void HandleAnimationCancelWindow()
    {
        InputReader.inputBusy = false;

        if (TryConsumeBufferedAttack())
            return;

        tierComboManager?.StartComboResetCountdown();
    }

    private bool TryConsumeBufferedAttack()
    {
        if (bufferedAttackButton == AttackButton.None)
            return false;

        if (Time.time > bufferedAttackExpiresAt)
        {
            bufferedAttackButton = AttackButton.None;
            bufferedAttackExpiresAt = -1f;
            return false;
        }

        bool lightAttack = bufferedAttackButton == AttackButton.Light;
        bufferedAttackButton = AttackButton.None;
        bufferedAttackExpiresAt = -1f;

        AttemptAttack(lightAttack);
        return true;
    }

    private void ClearBufferedAttack()
    {
        bufferedAttackButton = AttackButton.None;
        bufferedAttackExpiresAt = -1f;
    }

    public void ForceCancelCurrentAttack(bool resetCombo = true)
    {
        ClearBufferedAttack();
        ClearHitbox();
        currentAttack = null;
        InputReader.inputBusy = false;

        if (resetCombo)
        {
            tierComboManager?.ResetCombo();
        }
        else
        {
            tierComboManager?.CancelComboResetCountdown();
        }
    }
    #endregion
}
