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
    [SerializeField, Tooltip("Animator to play attack states directly when using state-driven mode (no transitions)")]
    private Animator animator;
    [SerializeField] private CharacterController characterController;

    [Header("Attack Data")]
    [SerializeField] private AttackDatabase attackDatabase;

    [Header("Audio")]
    [SerializeField] private AudioSource attackAudioSource;

    [Header("Hitbox")]
    [SerializeField, Tooltip("Default active duration for the hitbox after wind-up (seconds)")]
    private float defaultActiveDuration = 0.12f;
    private GameObject activeHitbox;

    // Simple one-slot input buffer so players can queue the next attack during lag
    private enum QueuedInput { None, Light, Heavy }
    [Header("Input Buffering")]
    [SerializeField, Tooltip("How long a queued input remains valid (seconds)")]
    private float inputBufferSeconds = 0.1f;
    private QueuedInput queuedInput = QueuedInput.None;
    private float queuedTimer = 0f;

    private TierComboManager.AttackStance currentStance = TierComboManager.AttackStance.Single;

    [Header("Execution Mode")]
    [SerializeField, Tooltip("If true, attacks are driven by Animator states (via AttackStateDriver). If false, code spawns hitboxes using start/end lag.")]
    private bool useStateDrivenAttacks = true;

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
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null && animFacade != null)
                animator = animFacade.GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        if (comboManager == null)
            Debug.LogError("EnhancedPlayerAttackManager: TierComboManager not found! Assign manually in Inspector.");
        
        if (aerialComboManager == null)
            Debug.LogError("EnhancedPlayerAttackManager: AerialComboManager not found! Assign manually in Inspector.");
        
        if (animFacade == null)
            Debug.LogError("EnhancedPlayerAttackManager: AnimFacade not found! Assign manually in Inspector.");
        
        if (characterController == null)
            Debug.LogError("EnhancedPlayerAttackManager: CharacterController not found!");
        if (animator == null)
            Debug.LogError("EnhancedPlayerAttackManager: Animator not found! Assign manually in Inspector.");
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
        // Check for attack inputs with buffering support
        if (_lightAttackAction.action.triggered)
            HandleLightInput();

        if (_heavyAttackAction.action.triggered)
            HandleHeavyInput();

        // Buffer timer countdown
        if (queuedTimer > 0f)
        {
            queuedTimer -= Time.deltaTime;
            if (queuedTimer <= 0f)
            {
                queuedInput = QueuedInput.None;
                animFacade?.ClearBufferedInputs();
            }
        }

        // Update current stance from CombatManager
        currentStance = CombatManager.singleTargetMode 
            ? TierComboManager.AttackStance.Single 
            : TierComboManager.AttackStance.AOE;
    }

    private void HandleLightInput()
    {
        if (InputReader.inputBusy)
        {
            queuedInput = QueuedInput.Light;
            queuedTimer = inputBufferSeconds;
            animFacade?.BufferLight();
            return;
        }
        OnLightAttack();
    }

    private void HandleHeavyInput()
    {
        if (InputReader.inputBusy)
        {
            queuedInput = QueuedInput.Heavy;
            queuedTimer = inputBufferSeconds;
            animFacade?.BufferHeavy();
            return;
        }
        OnHeavyAttack();
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

        string attackId = null;

        // Route to appropriate combo system based on grounded state
        if (IsGrounded())
        {
            // Ground combo - trigger animator immediately
            animFacade?.PressHeavy();
            
            // Use tier system
            if (comboManager != null)
                attackId = comboManager.RequestHeavyAttack(currentStance);
        }
        else
        {
            // Aerial combo - RequestAerialHeavyAttack() triggers animator AFTER validation
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

            // Play animation immediately if using state-driven controller (no transitions)
            if (useStateDrivenAttacks)
            {
                if (animator != null)
                {
                    int stateHash = Animator.StringToHash(attackId);
                    // crossfade on base layer with a small blend for responsiveness
                    animator.CrossFade(stateHash, 0.05f, 0);
                }
            }
            else
            {
                // Start attack coroutine with timing
                StartCoroutine(PerformAttack(attackData));
            }
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

        Debug.Log($"Executed Attack (mode={(useStateDrivenAttacks ? "State" : "Code")}): {attackId}");
    }

    // ========================= STATE-DRIVEN (Animator) SUPPORT =========================
    // Allows StateMachineBehaviours to request a one-shot hitbox for a given attackId,
    // independent of the input-driven PerformAttack coroutine.
    public void SpawnOneShotHitbox(string attackId, float activeDuration)
    {
        var attackData = attackDatabase?.GetAttack(attackId);
        if (attackData == null)
        {
            Debug.LogWarning($"SpawnOneShotHitbox: Unknown attackId '{attackId}'");
            return;
        }

        StartCoroutine(SpawnHitboxWindow(attackData, activeDuration));
    }

    private IEnumerator SpawnHitboxWindow(PlayerAttack attack, float activeDuration)
    {
        // Safety: destroy any lingering hitbox
        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }

        // Spawn
        activeHitbox = attack.createHitBox(transform.position, transform.forward);
        if (activeHitbox != null)
        {
            activeHitbox.transform.SetParent(transform, worldPositionStays: true);
            Debug.Log($"[StateDriven] Hitbox Spawned -> {attack.attackName}");
        }

        yield return new WaitForSeconds(activeDuration);

        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
            Debug.Log($"[StateDriven] Hitbox Destroyed <- {attack.attackName}");
        }
    }

    private IEnumerator PerformAttack(PlayerAttack attack)
    {
        InputReader.inputBusy = true;
        // Freeze locomotion-driven transitions during the full attack window
        animFacade?.LockMovementOn();

        // Start lag (wind-up)
        yield return new WaitForSeconds(attack.startLag);

        // Active frames: spawn a transient hitbox using ScriptableObject data
        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }

        // Create and parent so it follows the player if they move during the window
        activeHitbox = attack.createHitBox(transform.position, transform.forward);
        if (activeHitbox != null)
        {
            activeHitbox.transform.SetParent(transform, worldPositionStays: true);
            Debug.Log($"Hitbox Spawned -> {attack.attackName} | Damage: {attack.damage} | Size: {activeHitbox.GetComponent<BoxCollider>()?.size}");
        }

        // Keep the hitbox active for the configured active window
        yield return new WaitForSeconds(defaultActiveDuration);

        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
            Debug.Log($"Hitbox Destroyed <- {attack.attackName}");
        }

    // End lag (recovery)
    yield return new WaitForSeconds(attack.endLag);
        
    InputReader.inputBusy = false;
    animFacade?.LockMovementOff();

        // If a valid input was queued during this attack, consume it now to auto-chain
        if (queuedInput != QueuedInput.None && queuedTimer > 0f)
        {
            var next = queuedInput;
            // clear queue before invoking to avoid accidental re-entry
            queuedInput = QueuedInput.None;
            animFacade?.ClearBufferedInputs();

            if (next == QueuedInput.Light)
                OnLightAttack();
            else if (next == QueuedInput.Heavy)
                OnHeavyAttack();
        }
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
