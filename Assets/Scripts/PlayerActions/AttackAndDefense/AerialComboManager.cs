/*
 * Aerial Combo Manager
 * 
 * Handles aerial attack combos with different rules than ground combos:
 * - 2 Fast attacks (AerialX1, AerialX2) 
 * - 1 Heavy plunge attack (AerialY1)
 * - After 2 fast attacks, player falls unless they dash or plunge
 * - Heavy attack immediately plunges player down
 * - Can air dash once to reset aerial attacks (X X Dash X X Y possible)
 * - Fast attacks can be canceled by dash
 * 
 * Usage: X X Y, X Y, Y, X X Dash X X Y, X Dash X X Y, etc.
 */

using UnityEngine;
using System.Collections;

public class AerialComboManager : MonoBehaviour
{
    [Header("Aerial Combo Settings")]
    [SerializeField, Range(0.5f, 2f)] private float comboResetTime = 1.0f;
    [SerializeField] private bool debugMode = false;

    [Header("References")]
    [SerializeField, Tooltip("Assign manually if AnimFacade is on different GameObject")]
    private AnimFacade animFacade;
    [SerializeField] private CharacterController characterController;

    // Aerial combo state
    private int aerialFastCount = 0;  // 0, 1, or 2
    private bool hasUsedAirDash = false;
    private bool hasUsedAerialHeavy = false;  // NEW: limit heavy to once per airtime
    private bool isInAerialCombo = false;
    private float lastAerialAttackTime = -999f;

    private const int MAX_AERIAL_FAST_ATTACKS = 2;

    // Public property for external checks
    public bool HasUsedAerialHeavy => hasUsedAerialHeavy;

    void Awake()
    {
        // Auto-find AnimFacade if not assigned
        if (animFacade == null)
        {
            animFacade = GetComponent<AnimFacade>();
            if (animFacade == null)
            {
                animFacade = GetComponentInParent<AnimFacade>();
                if (animFacade == null)
                    animFacade = GetComponentInChildren<AnimFacade>();
                if (animFacade == null)
                    Debug.LogError("AerialComboManager: AnimFacade not found! Assign manually in Inspector.");
            }
        }
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController == null)
            Debug.LogError("AerialComboManager: CharacterController not found!");
    }

    void Update()
    {
        // Reset aerial combo when grounded
        if (characterController != null && characterController.isGrounded && isInAerialCombo)
        {
            ResetAerialCombo();
        }
    }

    /// <summary>
    /// Request aerial fast attack. Returns attack ID or null if not allowed.
    /// </summary>
    public string RequestAerialFastAttack()
    {
        if (!CanPerformAerialFastAttack())
        {
            if (debugMode)
                Debug.LogWarning("Cannot perform aerial fast attack - max reached or grounded");
            return null;
        }

        aerialFastCount++;
        lastAerialAttackTime = Time.time;
        isInAerialCombo = true;

        string attackId = aerialFastCount == 1 ? "AerialX1" : "AerialX2";

        // Call AnimFacade for animation
        if (animFacade != null)
            animFacade.PressLight();

        if (debugMode)
            Debug.Log($"Aerial Fast Attack: {attackId} | Count: {aerialFastCount}/{MAX_AERIAL_FAST_ATTACKS}");

        // Check if we've hit the limit
        if (aerialFastCount >= MAX_AERIAL_FAST_ATTACKS)
        {
            if (debugMode)
                Debug.Log("Max aerial fast attacks reached - player will fall after animation");
        }

        return attackId;
    }

    /// <summary>
    /// Request aerial heavy (plunge) attack. Only allowed once per airtime.
    /// </summary>
    public string RequestAerialHeavyAttack()
    {
        if (characterController != null && characterController.isGrounded)
        {
            if (debugMode)
                Debug.LogWarning("Cannot perform aerial heavy - already grounded");
            return null;
        }

        if (hasUsedAerialHeavy)
        {
            if (debugMode)
                Debug.LogWarning("Aerial heavy already used this airtime");
            return null;
        }

        hasUsedAerialHeavy = true;
        lastAerialAttackTime = Time.time;
        isInAerialCombo = true;

        // Trigger plunge movement (freeze + fast drop)
        EnhancedPlayerMovement playerMovement = GetComponent<EnhancedPlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.StartPlunge();
        }

        // Call AnimFacade for animation AFTER validation
        if (animFacade != null)
            animFacade.PressHeavy();

        if (debugMode)
            Debug.Log("Aerial Heavy Attack: AerialY1 (Plunge) - plunge movement triggered");

        // Heavy attack forces plunge, will reset on landing
        return "AerialY1";
    }

    /// <summary>
    /// Called when player performs an air dash.
    /// Resets aerial fast attack count once per air time.
    /// </summary>
    public bool TryAirDash()
    {
        if (hasUsedAirDash)
        {
            if (debugMode)
                Debug.LogWarning("Air dash already used - cannot reset aerial attacks");
            return false;
        }

        // Air dash resets fast attack count
        aerialFastCount = 0;
        hasUsedAirDash = true;
        
        if (debugMode)
            Debug.Log("Air dash performed - aerial fast attacks reset");

        return true;
    }

    /// <summary>
    /// Check if player can perform another aerial fast attack.
    /// </summary>
    public bool CanPerformAerialFastAttack()
    {
        // Must be in air
        if (characterController != null && characterController.isGrounded)
            return false;

        // Must not have exceeded max fast attacks
        return aerialFastCount < MAX_AERIAL_FAST_ATTACKS;
    }

    /// <summary>
    /// Check if player has reached max aerial attacks and should fall.
    /// </summary>
    public bool ShouldFallAfterAnimation()
    {
        return aerialFastCount >= MAX_AERIAL_FAST_ATTACKS && !hasUsedAirDash;
    }

    /// <summary>
    /// Reset aerial combo state (called on landing or timeout).
    /// </summary>
    public void ResetAerialCombo()
    {
        aerialFastCount = 0;
        hasUsedAirDash = false;
        hasUsedAerialHeavy = false;  // Reset heavy flag for next airtime
        isInAerialCombo = false;

        if (debugMode)
            Debug.Log("Aerial combo reset");
    }

    /// <summary>
    /// Called when player lands to reset air dash availability.
    /// </summary>
    public void OnLanded()
    {
        ResetAerialCombo();
    }

    // Public accessors for debugging/UI
    public int AerialFastCount => aerialFastCount;
    public bool HasUsedAirDash => hasUsedAirDash;
    public bool IsInAerialCombo => isInAerialCombo;
    public bool CanAirDash => !hasUsedAirDash && !characterController.isGrounded;
}
