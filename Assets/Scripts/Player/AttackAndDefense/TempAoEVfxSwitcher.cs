using System;
using System.Collections;
using UnityEngine;
using Utilities.Combat.Attacks;

/// <summary>
/// Temporary helper that toggles pre-placed VFX objects when specific combat actions fire.
/// Attach this to the player, assign the PlayerAttackManager/PlayerMovement references, and
/// populate the VFX objects for attack and aerial movement effects.
/// </summary>
public sealed class TempAoEVfxSwitcher : MonoBehaviour
{
    [SerializeField] private PlayerAttackManager attackManager;
    [SerializeField] private PlayerMovement playerMovement;
    private AudioSource audioSource;

    [Header("Attack VFX")]
    [SerializeField, Tooltip("Rig-mounted left-hand VFX (enabled by LeftFire animation event).")]
    private GameObject leftAttackVfx;
    [SerializeField, Tooltip("Rig-mounted right-hand VFX (enabled by RightFire animation event).")]
    private GameObject rightAttackVfx;
    [SerializeField, Tooltip("Duration before attack VFX are hidden again.")]
    private float attackDuration = 1f;
    [SerializeField, Tooltip("Audio clip played when attack VFX enable.")]
    private AudioClip attackAudioClip;

    [Header("Air Move VFX (Double Jump & Air Dash)")]
    [SerializeField, Tooltip("Rig-mounted VFX toggled for double jump / air dash.")]
    private GameObject[] airMoveVfxObjects = Array.Empty<GameObject>();
    [SerializeField, Tooltip("Duration before air-move VFX are hidden again.")]
    private float airMoveDuration = 0.75f;
    [SerializeField, Tooltip("Audio clip played when double jump / air dash VFX enable.")]
    private AudioClip airMoveAudioClip;

    private Coroutine leftAttackDeactivateRoutine;
    private Coroutine rightAttackDeactivateRoutine;
    private Coroutine airMoveDeactivateRoutine;
    private bool airMoveCallbacksRegistered;

    private void Awake()
    {
        attackManager ??= GetComponentInChildren<PlayerAttackManager>() ?? GetComponent<PlayerAttackManager>();
        playerMovement ??= GetComponentInChildren<PlayerMovement>() ?? GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
        audioSource = SoundManager.Instance?.sfxSource;

        SetVfxActive(leftAttackVfx, false);
        SetVfxActive(rightAttackVfx, false);
        SetVfxActive(airMoveVfxObjects, false);
    }

    private void OnEnable()
    {
        playerMovement ??= GetComponentInChildren<PlayerMovement>() ?? GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
        PlayerAttackManager.OnAttack += HandleAttackStarted;
        RegisterAirMoveCallbacks();
    }

    private void OnDisable()
    {
        PlayerAttackManager.OnAttack -= HandleAttackStarted;
        UnregisterAirMoveCallbacks();

        StopAndClearRoutine(ref leftAttackDeactivateRoutine);
        StopAndClearRoutine(ref rightAttackDeactivateRoutine);
        StopAndClearRoutine(ref airMoveDeactivateRoutine);

        SetVfxActive(leftAttackVfx, false);
        SetVfxActive(rightAttackVfx, false);
        SetVfxActive(airMoveVfxObjects, false);
    }

    private void HandleAirMoveTriggered()
    {
        if (airMoveVfxObjects == null || airMoveVfxObjects.Length == 0)
            return;

        SetVfxActive(airMoveVfxObjects, true);
        PlayAudio(airMoveAudioClip);
        RestartGroupRoutine(
            ref airMoveDeactivateRoutine,
            airMoveDuration,
            airMoveVfxObjects,
            () => airMoveDeactivateRoutine = null);
    }

    private void HandleAttackStarted(PlayerAttack attack)
    {
        if (attack == null)
            return;

        bool isAerial = attack.attackType == AttackType.LightAerial
            || attack.attackType == AttackType.HeavyAerial;
        bool isLauncher = string.Equals(attack.attackId, "Launcher", StringComparison.OrdinalIgnoreCase);
        bool isAirDash = string.Equals(attack.attackId, "AirDash", StringComparison.OrdinalIgnoreCase);

        if ((isAerial || isLauncher) && !isAirDash)
            PlayAudio(airMoveAudioClip);
    }

    public void LeftFire()
    {
        TriggerLeftAttackVfx();
    }

    public void RightFire()
    {
        TriggerRightAttackVfx();
    }

    private void TriggerLeftAttackVfx()
    {
        if (leftAttackVfx == null)
            return;

        SetVfxActive(leftAttackVfx, true);
        PlayAudio(attackAudioClip);
        RestartSingleRoutine(
            ref leftAttackDeactivateRoutine,
            attackDuration,
            leftAttackVfx,
            ClearLeftAttackRoutine);
    }

    private void TriggerRightAttackVfx()
    {
        if (rightAttackVfx == null)
            return;

        SetVfxActive(rightAttackVfx, true);
        PlayAudio(attackAudioClip);
        Debug.Log("Playing right attack audio clip: " + (attackAudioClip != null ? attackAudioClip.name : "null"));
        RestartSingleRoutine(
            ref rightAttackDeactivateRoutine,
            attackDuration,
            rightAttackVfx,
            ClearRightAttackRoutine);
    }

    private void RestartGroupRoutine(ref Coroutine routine, float duration, GameObject[] targets, Action onComplete)
    {
        StopAndClearRoutine(ref routine);

        if (duration <= 0f)
        {
            SetVfxActive(targets, false);
            onComplete?.Invoke();
            return;
        }

        routine = StartCoroutine(DisableAfter(duration, targets, onComplete));
    }

    private void RestartSingleRoutine(ref Coroutine routine, float duration, GameObject target, Action onComplete)
    {
        StopAndClearRoutine(ref routine);

        if (duration <= 0f)
        {
            SetVfxActive(target, false);
            onComplete?.Invoke();
            return;
        }

        routine = StartCoroutine(DisableAfter(duration, target, onComplete));
    }

    private void ClearLeftAttackRoutine()
    {
        leftAttackDeactivateRoutine = null;
    }

    private void ClearRightAttackRoutine()
    {
        rightAttackDeactivateRoutine = null;
    }

    private IEnumerator DisableAfter(float duration, GameObject[] targets, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        SetVfxActive(targets, false);
        onComplete?.Invoke();
    }

    private IEnumerator DisableAfter(float duration, GameObject target, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        SetVfxActive(target, false);
        onComplete?.Invoke();
    }

    private void SetVfxActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(active);
        }
    }

    private void SetVfxActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void RegisterAirMoveCallbacks()
    {
        if (playerMovement == null || airMoveCallbacksRegistered)
            return;

        playerMovement.DoubleJumpPerformed += HandleAirMoveTriggered;
        playerMovement.AirDashPerformed += HandleAirMoveTriggered;
        airMoveCallbacksRegistered = true;
    }

    private void UnregisterAirMoveCallbacks()
    {
        if (playerMovement == null || !airMoveCallbacksRegistered)
            return;

        playerMovement.DoubleJumpPerformed -= HandleAirMoveTriggered;
        playerMovement.AirDashPerformed -= HandleAirMoveTriggered;
        airMoveCallbacksRegistered = false;
    }

    private void StopAndClearRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
