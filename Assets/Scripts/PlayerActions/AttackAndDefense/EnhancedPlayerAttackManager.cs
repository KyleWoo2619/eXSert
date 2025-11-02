/*
 * Enhanced Player Attack Manager
 * 
 * Integrates with TierComboManager and AnimFacade to handle the 3-tier combo system
 * with stance swapping support.
 * 
 * Replaces the old simple attack manager with proper tier-based routing.
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnhancedPlayerAttackManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _lightAttackAction;
    [SerializeField] private InputActionReference _heavyAttackAction;

    [Header("References")]
    [SerializeField, Tooltip("Assign manually if on different GameObject")] 
    private TierComboManager comboManager;
    [SerializeField, Tooltip("Assign manually if on different GameObject")] 
    private AerialComboManager aerialComboManager;
    [SerializeField, Tooltip("Assign manually if AnimFacade is on different GameObject")] 
    private AnimFacade animFacade;
    [SerializeField] private CharacterController characterController;

    [Header("Attack Data")]
    [SerializeField] private AttackDatabase attackDatabase;

    [Header("Audio")]
    [SerializeField] private AudioSource attackAudioSource;

    private TierComboManager.AttackStance currentStance = TierComboManager.AttackStance.Single;

    public static event Action onAttack;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (comboManager == null)
        {
            comboManager = GetComponent<TierComboManager>();
            if (comboManager == null)
                comboManager = GetComponentInChildren<TierComboManager>();
        }
        
        if (aerialComboManager == null)
        {
            aerialComboManager = GetComponent<AerialComboManager>();
            if (aerialComboManager == null)
                aerialComboManager = GetComponentInChildren<AerialComboManager>();
        }
        
        if (animFacade == null)
        {
            animFacade = GetComponent<AnimFacade>();
            if (animFacade == null)
            {
                animFacade = GetComponentInParent<AnimFacade>();
                if (animFacade == null)
                    animFacade = GetComponentInChildren<AnimFacade>();
            }
        }
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (comboManager == null)
            Debug.LogError("EnhancedPlayerAttackManager: TierComboManager not found! Assign manually in Inspector.");
        
        if (aerialComboManager == null)
            Debug.LogError("EnhancedPlayerAttackManager: AerialComboManager not found! Assign manually in Inspector.");
        
        if (animFacade == null)
            Debug.LogError("EnhancedPlayerAttackManager: AnimFacade not found! Assign manually in Inspector.");
        
        if (characterController == null)
            Debug.LogError("EnhancedPlayerAttackManager: CharacterController not found!");
    }

    private void Start()
    {
        if (_lightAttackAction.action == null)
            Debug.LogError("Light Attack Action is NULL! Assign the Light Attack Action.");

        if (_heavyAttackAction.action == null)
            Debug.LogError("Heavy Attack Action is NULL! Assign the Heavy Attack Action.");
    }

    private void Update()
    {
        // Check for attack inputs
        if (_lightAttackAction.action.triggered && !InputReader.inputBusy)
            OnLightAttack();

        if (_heavyAttackAction.action.triggered && !InputReader.inputBusy)
            OnHeavyAttack();

        // Update current stance from CombatManager
        currentStance = CombatManager.singleTargetMode 
            ? TierComboManager.AttackStance.Single 
            : TierComboManager.AttackStance.AOE;
    }

    public void OnLightAttack()
    {
        if (InputReader.inputBusy) return;

        Debug.Log("Light Attack Input Detected");

        // Drive animator trigger immediately so graph can react/buffer
        animFacade?.PressLight();

        string attackId = null;

        // Route to appropriate combo system based on grounded state
        if (IsGrounded())
        {
            // Ground combo - use tier system
            if (comboManager != null)
                attackId = comboManager.RequestFastAttack(currentStance);
        }
        else
        {
            // Aerial combo - use aerial system
            if (aerialComboManager != null)
                attackId = aerialComboManager.RequestAerialFastAttack();
        }
        
        // Execute attack
        if (attackId != null)
            ExecuteAttack(attackId);
    }

    public void OnHeavyAttack()
    {
        if (InputReader.inputBusy) return;

        Debug.Log("Heavy Attack Input Detected");

        // Drive animator trigger immediately so graph can react/buffer
        animFacade?.PressHeavy();

        string attackId = null;

        // Route to appropriate combo system based on grounded state
        if (IsGrounded())
        {
            // Ground combo - use tier system
            if (comboManager != null)
                attackId = comboManager.RequestHeavyAttack(currentStance);
        }
        else
        {
            // Aerial combo - plunge attack
            if (aerialComboManager != null)
                attackId = aerialComboManager.RequestAerialHeavyAttack();
        }
        
        // Execute attack
        if (attackId != null)
            ExecuteAttack(attackId);
    }

    /// <summary>
    /// Called by PlayerMovement when air dash is performed.
    /// </summary>
    public void OnAirDash()
    {
        if (aerialComboManager != null)
            aerialComboManager.TryAirDash();
    }

    /// <summary>
    /// Check if player is grounded.
    /// </summary>
    private bool IsGrounded()
    {
        return characterController != null && characterController.isGrounded;
    }

    private void ExecuteAttack(string attackId)
    {
        // Get attack data from database
        PlayerAttack attackData = attackDatabase?.GetAttack(attackId);
        
        if (attackData != null)
        {
            // Play attack audio
            if (attackData.attackSFX != null && attackAudioSource != null)
            {
                attackAudioSource.clip = attackData.attackSFX;
                attackAudioSource.Play();
            }

            // Start attack coroutine with timing
            StartCoroutine(PerformAttack(attackData));
        }
        else
        {
            Debug.LogWarning($"Attack data not found for: {attackId}");
        }

        // Invoke attack event for other systems (e.g., aerial hop)
        onAttack?.Invoke();

        // Mark in combat (AnimFacade handles this internally via PressLight/Heavy)
        if (animFacade != null)
            animFacade.MarkInCombat();

        Debug.Log($"Executed Attack: {attackId}");
    }

    private IEnumerator PerformAttack(PlayerAttack attack)
    {
        InputReader.inputBusy = true;

        // Start lag (wind-up)
        yield return new WaitForSeconds(attack.startLag);

        // Active frames (hitbox would be enabled here)
        Debug.Log($"Attack Active: {attack.attackName} | Damage: {attack.damage}");

        // End lag (recovery)
        yield return new WaitForSeconds(attack.endLag);
        
        InputReader.inputBusy = false;
    }

    /// <summary>
    /// Force reset combo (called from external systems if needed).
    /// </summary>
    public void ResetCombo()
    {
        comboManager?.ResetCombo();
    }

    /// <summary>
    /// Get current combo tier for UI/debugging.
    /// </summary>
    public int GetCurrentTier()
    {
        return comboManager?.CurrentTier ?? 1;
    }
}
