using UnityEngine;
using Utilities.Combat;
using Utilities.Combat.Attacks;

/// <summary>
/// Drives stance-aware idle playback (breathing, combat, weapon-check) using gameplay signals rather than animator logic.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCombatIdleController : MonoBehaviour
{
    private const float WeaponCheckCrossfade = 0.3f;
    private const float WeaponCheckCompletionThreshold = 0.99f;
    private const string SingleTargetBreathingState = "ST_Breathing";
    private const string AreaBreathingState = "AOE_Breathing";
    private const string SingleTargetCombatState = "ST_Idle_Combat";
    private const string AreaCombatState = "AOE_Idle_Combat";
    private const string SingleTargetWeaponCheckState = "ST_Idle_WC";
    private const string AreaWeaponCheckState = "AOE_Idle_WC";

    private enum IdlePose
    {
        Breathing,
        Combat,
        WeaponCheck,
    }

    [Header("References")]
    [SerializeField]
    private PlayerAnimationController animationController;

    [SerializeField]
    private CharacterController characterController;

    [Header("Timing")]
    [SerializeField]
    [Tooltip("Seconds the player stays flagged as in combat after an attack or taking damage.")]
    private float combatDuration = 10f;

    [SerializeField]
    [Tooltip(
        "Seconds of total inactivity before the weapon-check idle fires (only counts during breathing idle).")]
    private float weaponCheckDelay = 15f;

    [SerializeField]
    [Tooltip("Minimum squared magnitude required to treat MoveInput as real movement.")]
    private float movementThreshold = 0.02f;

    private float combatTimer;
    private float breathingIdleTimer;
    private bool weaponCheckActive;
    private string activeWeaponCheckState = string.Empty;
    private string lastIdleKey = string.Empty;
    private string lastStateName = string.Empty;
    private IdlePose currentPose = IdlePose.Breathing;
    private bool inputBusyLastFrame;
    private bool guardActiveLastFrame;

    private void Awake()
    {
        animationController ??= GetComponent<PlayerAnimationController>();
        characterController ??= GetComponent<CharacterController>();
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();
    }

    private void Start()
    {
        ForceImmediateIdle();
    }

    private void OnEnable()
    {
        PlayerAttackManager.OnAttack += HandleAttackEvent;
        PlayerHealthBarManager.OnPlayerDamaged += HandleDamageEvent;
        CombatManager.OnStanceChanged += HandleStanceChanged;
    }

    private void OnDisable()
    {
        PlayerAttackManager.OnAttack -= HandleAttackEvent;
        PlayerHealthBarManager.OnPlayerDamaged -= HandleDamageEvent;
        CombatManager.OnStanceChanged -= HandleStanceChanged;
    }

    private void HandleAttackEvent(PlayerAttack _)
    {
        EnterCombatState();
    }

    private void HandleDamageEvent(float _)
    {
        EnterCombatState();
    }

    private void HandleStanceChanged()
    {
        lastIdleKey = string.Empty;
        lastStateName = string.Empty;
        TryForceStanceIdleUpdate();
    }

    private void EnterCombatState()
    {
        combatTimer = combatDuration;
        ResetBreathingTimer();
        if (weaponCheckActive)
            CancelWeaponCheck(false);
    }

    private void Update()
    {
        if (animationController == null)
            return;

        bool guardActive = CombatManager.isGuarding;
        if (guardActive)
        {
            guardActiveLastFrame = true;
            ResetBreathingTimer();
            if (weaponCheckActive)
                CancelWeaponCheck(false);
            return;
        }

        if (guardActiveLastFrame)
        {
            guardActiveLastFrame = false;
            lastIdleKey = string.Empty;
            lastStateName = string.Empty;
        }

        bool inputBusy = InputReader.inputBusy;
        if (inputBusy)
        {
            inputBusyLastFrame = true;
            ResetBreathingTimer();
            if (weaponCheckActive)
                CancelWeaponCheck(false);
            return;
        }

        if (inputBusyLastFrame)
        {
            inputBusyLastFrame = false;
            lastIdleKey = string.Empty;
            lastStateName = string.Empty;
        }

        bool grounded = characterController == null || characterController.isGrounded;
        bool hasMovementInput = InputReader.MoveInput.sqrMagnitude >= movementThreshold;

        if (combatTimer > 0f)
            combatTimer = Mathf.Max(0f, combatTimer - Time.deltaTime);

        if (!grounded)
        {
            ResetBreathingTimer();
            return;
        }

        if (weaponCheckActive)
        {
            if (hasMovementInput || combatTimer > 0f)
            {
                CancelWeaponCheck(true);
            }
            else if (HasWeaponCheckFinished())
            {
                CancelWeaponCheck(true);
            }
            else
            {
                return;
            }
        }

        if (hasMovementInput)
        {
            ResetBreathingTimer();
            return;
        }

        bool inCombat = combatTimer > 0f;
        IdlePose desiredPose = inCombat ? IdlePose.Combat : IdlePose.Breathing;

        bool breathingActive = desiredPose == IdlePose.Breathing
            && !weaponCheckActive
            && IsPoseActive(IdlePose.Breathing);
        if (breathingActive)
        {
            breathingIdleTimer += Time.deltaTime;
            if (breathingIdleTimer >= weaponCheckDelay)
            {
                ResetBreathingTimer();
                PlayWeaponCheck();
                return;
            }
        }
        else
        {
            ResetBreathingTimer();
        }

        EnsureIdlePose(desiredPose);
    }

    private void PlayWeaponCheck()
    {
        SetIdlePose(IdlePose.WeaponCheck, WeaponCheckCrossfade);
    }

    private void CancelWeaponCheck(bool returnToBreathing)
    {
        if (!weaponCheckActive)
            return;

        weaponCheckActive = false;
        activeWeaponCheckState = string.Empty;
        ResetBreathingTimer();

        if (returnToBreathing)
            SetIdlePose(IdlePose.Breathing, WeaponCheckCrossfade);
    }

    private bool HasWeaponCheckFinished()
    {
        if (string.IsNullOrEmpty(activeWeaponCheckState))
            return true;

        if (!animationController.IsPlaying(activeWeaponCheckState, out float normalizedTime))
            return true;

        return normalizedTime >= WeaponCheckCompletionThreshold;
    }

    private void SetIdlePose(IdlePose pose, float transitionOverride = -1f)
    {
        string newKey = ComposeIdleKey(pose);
        string stateName = GetStateNameForPose(pose);
        bool poseAllowsRefresh = pose == IdlePose.WeaponCheck;

        if (!poseAllowsRefresh && newKey == lastIdleKey && !NeedsStateRefresh(stateName))
            return;

        switch (pose)
        {
            case IdlePose.Breathing:
                if (CombatManager.singleTargetMode)
                    animationController.PlaySingleTargetBreathing(transitionOverride);
                else
                    animationController.PlayAoeBreathing(transitionOverride);
                weaponCheckActive = false;
                activeWeaponCheckState = string.Empty;
                ResetBreathingTimer();
                break;
            case IdlePose.Combat:
                if (CombatManager.singleTargetMode)
                    animationController.PlaySingleTargetIdleCombat();
                else
                    animationController.PlayAoeIdleCombat();
                weaponCheckActive = false;
                activeWeaponCheckState = string.Empty;
                ResetBreathingTimer();
                break;
            case IdlePose.WeaponCheck:
                if (CombatManager.singleTargetMode)
                {
                    animationController.PlaySingleTargetIdleWorld(transitionOverride);
                    activeWeaponCheckState = SingleTargetWeaponCheckState;
                }
                else
                {
                    animationController.PlayAoeIdleWorld(transitionOverride);
                    activeWeaponCheckState = AreaWeaponCheckState;
                }
                weaponCheckActive = true;
                ResetBreathingTimer();
                break;
        }

        currentPose = pose;
        lastIdleKey = newKey;
        lastStateName = stateName;
    }

    private void ForceImmediateIdle()
    {
        if (animationController == null)
            return;

        lastIdleKey = string.Empty;
        lastStateName = string.Empty;
        currentPose = IdlePose.Breathing;
        ResetBreathingTimer();
        SetIdlePose(IdlePose.Breathing, 0f);
    }

    private void EnsureIdlePose(IdlePose pose)
    {
        string desiredKey = ComposeIdleKey(pose);
        string desiredState = GetStateNameForPose(pose);
        bool needsStanceRefresh = desiredKey != lastIdleKey;
        bool statePlaying = !string.IsNullOrEmpty(desiredState)
            && animationController.IsPlaying(desiredState, out _);

        if (!needsStanceRefresh && pose == currentPose && statePlaying)
            return;

        SetIdlePose(pose);
    }

    private string ComposeIdleKey(IdlePose pose)
    {
        string prefix = CombatManager.singleTargetMode ? "ST" : "AOE";
        return $"{prefix}_{pose}";
    }

    private bool IsPoseActive(IdlePose pose)
    {
        string stateName = GetStateNameForPose(pose);
        return !string.IsNullOrEmpty(stateName) && animationController.IsPlaying(stateName, out _);
    }

    private static string GetStateNameForPose(bool singleTarget, IdlePose pose)
    {
        if (singleTarget)
        {
            switch (pose)
            {
                case IdlePose.Breathing:
                    return SingleTargetBreathingState;
                case IdlePose.Combat:
                    return SingleTargetCombatState;
                case IdlePose.WeaponCheck:
                    return SingleTargetWeaponCheckState;
            }
        }
        else
        {
            switch (pose)
            {
                case IdlePose.Breathing:
                    return AreaBreathingState;
                case IdlePose.Combat:
                    return AreaCombatState;
                case IdlePose.WeaponCheck:
                    return AreaWeaponCheckState;
            }
        }

        return string.Empty;
    }

    private string GetStateNameForPose(IdlePose pose)
    {
        return GetStateNameForPose(CombatManager.singleTargetMode, pose);
    }

    private bool NeedsStateRefresh(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
            return true;

        return !animationController.IsPlaying(stateName, out _);
    }

    private void ResetBreathingTimer()
    {
        breathingIdleTimer = 0f;
    }

    private void TryForceStanceIdleUpdate()
    {
        if (animationController == null || InputReader.inputBusy)
            return;

        bool grounded = characterController == null || characterController.isGrounded;
        if (!grounded)
            return;

        bool hasMovementInput = InputReader.MoveInput.sqrMagnitude >= movementThreshold;
        if (hasMovementInput)
            return;

        IdlePose desiredPose = combatTimer > 0f ? IdlePose.Combat : IdlePose.Breathing;
        SetIdlePose(desiredPose, 0f);
    }
}

