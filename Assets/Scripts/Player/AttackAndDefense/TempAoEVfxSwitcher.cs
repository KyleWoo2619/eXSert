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
    [SerializeField] private AudioSource audioSource;

    [Header("Attack VFX")]
    [SerializeField, Tooltip("Rig-mounted VFX toggled for AoE/plunge attacks.")]
    private GameObject[] attackVfxObjects = Array.Empty<GameObject>();
    [SerializeField, Tooltip("Duration before attack VFX are hidden again.")]
    private float attackDuration = 1f;
    [SerializeField, Tooltip("Toggle when attacks are tagged as HeavyAOE (AY1-AY3).")]
    private bool includeAoEAttacks = true;
    [SerializeField, Tooltip("Toggle when the heavy aerial plunge attack fires.")]
    private bool includePlungeAttacks = true;
    [SerializeField, Tooltip("Audio clip played when attack VFX enable.")]
    private AudioClip attackAudioClip;

    [Header("Air Move VFX (Double Jump & Air Dash)")]
    [SerializeField, Tooltip("Rig-mounted VFX toggled for double jump / air dash.")]
    private GameObject[] airMoveVfxObjects = Array.Empty<GameObject>();
    [SerializeField, Tooltip("Duration before air-move VFX are hidden again.")]
    private float airMoveDuration = 0.75f;
    [SerializeField, Tooltip("Audio clip played when double jump / air dash VFX enable.")]
    private AudioClip airMoveAudioClip;

    private Coroutine attackDeactivateRoutine;
    private Coroutine airMoveDeactivateRoutine;
    private bool airMoveCallbacksRegistered;

    private void Awake()
    {
        attackManager ??= GetComponentInChildren<PlayerAttackManager>() ?? GetComponent<PlayerAttackManager>();
        playerMovement ??= GetComponentInChildren<PlayerMovement>() ?? GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
        audioSource ??= GetComponent<AudioSource>() ?? SoundManager.Instance?.sfxSource;

        SetVfxActive(attackVfxObjects, false);
        SetVfxActive(airMoveVfxObjects, false);
    }

    private void OnEnable()
    {
        PlayerAttackManager.OnAttack += HandleAttackStarted;

        playerMovement ??= GetComponentInChildren<PlayerMovement>() ?? GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
        RegisterAirMoveCallbacks();
    }

    private void OnDisable()
    {
        PlayerAttackManager.OnAttack -= HandleAttackStarted;
        UnregisterAirMoveCallbacks();

        StopAndClearRoutine(ref attackDeactivateRoutine);
        StopAndClearRoutine(ref airMoveDeactivateRoutine);

        SetVfxActive(attackVfxObjects, false);
        SetVfxActive(airMoveVfxObjects, false);
    }

    private void HandleAttackStarted(PlayerAttack attack)
    {
        if (attack == null)
            return;

        if (!ShouldReactToAttack(attack.attackType))
            return;

        TriggerAttackVfx();
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

    private void TriggerAttackVfx()
    {
        if (attackVfxObjects == null || attackVfxObjects.Length == 0)
            return;

        SetVfxActive(attackVfxObjects, true);
        PlayAudio(attackAudioClip);
        RestartGroupRoutine(
            ref attackDeactivateRoutine,
            attackDuration,
            attackVfxObjects,
            () => attackDeactivateRoutine = null);
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

    private IEnumerator DisableAfter(float duration, GameObject[] targets, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        SetVfxActive(targets, false);
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

    private bool ShouldReactToAttack(AttackType attackType)
    {
        bool isAoE = attackType == AttackType.HeavyAOE;
        bool isPlunge = attackType == AttackType.HeavyAerial;

        return (includeAoEAttacks && isAoE)
            || (includePlungeAttacks && isPlunge);
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
