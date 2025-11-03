/*
 * Tier-Based Combo Manager
 * 
 * Handles 3-tier combo progression with stance swapping support.
 * Works with AnimFacade to route attacks based on current tier and stance.
 * 
 * Tier Structure:
 * Single: SX1,SX2 | SX3,SX4 | SX5   and  SY1 | SY2 | SY3
 * AOE:    AX1,AX2 | AX3     | AX4   and  AY1 | AY2 | AY3
 * 
 * Cross-stance routing:
 * - Tier-1 attacks progress to Tier-2 on next input
 * - Tier-2 attacks progress to Tier-3 on next input
 * - Stance swaps jump to the equivalent tier in the new stance
 */

using UnityEngine;
using System.Collections;

public class TierComboManager : MonoBehaviour
{
    [Header("Combo Settings")]
    [SerializeField, Range(0.5f, 3f)] private float comboResetTime = 1.5f;
    [SerializeField] private bool debugMode = false;

    [Header("References")]
    [SerializeField, Tooltip("Assign manually if AnimFacade is on different GameObject")]
    private AnimFacade animFacade;

    // Current combo state
    private int currentTier = 1;          // 1, 2, or 3
    private bool isHeavyChain = false;    // tracking if we're in a heavy (Y) chain
    private int fastAttackIndex = 0;      // within tier: 0 or 1 for tier-1, 0 for tier-2/3
    private AttackStance lastStance = AttackStance.Single;
    
    private float lastAttackTime = -999f;
    private Coroutine resetCoroutine;

    public enum AttackStance
    {
        Single = 0,
        AOE = 1
    }

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
                    Debug.LogError("TierComboManager: AnimFacade not found! Assign manually in Inspector or attach to same GameObject/parent/child.");
            }
        }
    }

    /// <summary>
    /// Call this when player presses fast attack (X button).
    /// Returns the attack identifier for animation/VFX systems.
    /// </summary>
    public string RequestFastAttack(AttackStance currentStance)
    {
        string attackId = GetNextFastAttack(currentStance);
        
        if (animFacade != null)
            animFacade.PressLight();
        
        AdvanceCombo(false, currentStance);
        
        if (debugMode)
            Debug.Log($"Fast Attack: {attackId} | Tier: {currentTier} | Stance: {currentStance}");
        
        return attackId;
    }

    /// <summary>
    /// Call this when player presses heavy attack (Y button).
    /// Returns the attack identifier for animation/VFX systems.
    /// </summary>
    public string RequestHeavyAttack(AttackStance currentStance)
    {
        string attackId = GetNextHeavyAttack(currentStance);
        
        if (animFacade != null)
            animFacade.PressHeavy();
        
        AdvanceCombo(true, currentStance);
        
        if (debugMode)
            Debug.Log($"Heavy Attack: {attackId} | Tier: {currentTier} | Stance: {currentStance}");
        
        return attackId;
    }

    /// <summary>
    /// Determines the next fast attack based on current tier and stance.
    /// Handles cross-stance tier routing.
    /// </summary>
    private string GetNextFastAttack(AttackStance currentStance)
    {
        // Check if we're switching stances mid-combo
        bool stanceChanged = (currentStance != lastStance) && (currentTier > 1);

        if (stanceChanged)
        {
            // Cross-stance routing: jump to equivalent tier in new stance
            return GetCrossStanceFastAttack(currentStance, currentTier);
        }

        // Same stance progression
        if (currentStance == AttackStance.Single)
        {
            return GetSingleFastAttack();
        }
        else // AOE
        {
            return GetAOEFastAttack();
        }
    }

    private string GetSingleFastAttack()
    {
        switch (currentTier)
        {
            case 1:
                // Tier 1: SX1 or SX2
                return fastAttackIndex == 0 ? "SX1" : "SX2";
            case 2:
                // Tier 2: SX3 or SX4
                return fastAttackIndex == 0 ? "SX3" : "SX4";
            case 3:
                // Tier 3: SX5 (finisher)
                return "SX5";
            default:
                return "SX1";
        }
    }

    private string GetAOEFastAttack()
    {
        switch (currentTier)
        {
            case 1:
                // Tier 1: AX1 or AX2
                return fastAttackIndex == 0 ? "AX1" : "AX2";
            case 2:
                // Tier 2: AX3
                return "AX3";
            case 3:
                // Tier 3: AX4 (finisher)
                return "AX4";
            default:
                return "AX1";
        }
    }

    /// <summary>
    /// Cross-stance fast attack routing.
    /// When stance changes mid-combo, jump to the target tier in new stance.
    /// </summary>
    private string GetCrossStanceFastAttack(AttackStance newStance, int tier)
    {
        if (newStance == AttackStance.AOE)
        {
            // Switching from Single to AOE
            switch (tier)
            {
                case 2: return "AX3";  // Tier-2 AOE
                case 3: return "AX4";  // Tier-3 AOE finisher
                default: return "AX1";
            }
        }
        else // Switching to Single
        {
            // Switching from AOE to Single
            switch (tier)
            {
                case 2: return fastAttackIndex == 0 ? "SX3" : "SX4";  // Tier-2 Single
                case 3: return "SX5";  // Tier-3 Single finisher
                default: return "SX1";
            }
        }
    }

    /// <summary>
    /// Determines the next heavy attack based on current tier and stance.
    /// </summary>
    private string GetNextHeavyAttack(AttackStance currentStance)
    {
        // Check if we're switching stances mid-combo
        bool stanceChanged = (currentStance != lastStance) && (currentTier > 1);

        if (stanceChanged)
        {
            return GetCrossStanceHeavyAttack(currentStance, currentTier);
        }

        // Heavy attacks follow simpler progression: Y1 -> Y2 -> Y3 for both stances
        string prefix = currentStance == AttackStance.Single ? "SY" : "AY";
        
        switch (currentTier)
        {
            case 1: return prefix + "1";
            case 2: return prefix + "2";
            case 3: return prefix + "3";
            default: return prefix + "1";
        }
    }

    private string GetCrossStanceHeavyAttack(AttackStance newStance, int tier)
    {
        string prefix = newStance == AttackStance.Single ? "SY" : "AY";
        
        switch (tier)
        {
            case 2: return prefix + "2";
            case 3: return prefix + "3";
            default: return prefix + "1";
        }
    }

    /// <summary>
    /// Advances the combo state after an attack is executed.
    /// </summary>
    private void AdvanceCombo(bool isHeavy, AttackStance currentStance)
    {
        lastAttackTime = Time.time;
        
        // Update stance tracking
        lastStance = currentStance;
        isHeavyChain = isHeavy;

        // Advance tier logic
        if (currentTier == 1)
        {
            // In tier-1, track which attack (first or second)
            if (!isHeavy)
            {
                fastAttackIndex++;
                if (fastAttackIndex >= 2) // After SX2 or AX2, move to tier-2
                {
                    currentTier = 2;
                    fastAttackIndex = 0;
                }
            }
            else
            {
                // Heavy always advances tier immediately
                currentTier = 2;
                fastAttackIndex = 0;
            }
        }
        else if (currentTier == 2)
        {
            // In tier-2, advance based on attack type
            if (!isHeavy && currentStance == AttackStance.Single)
            {
                // Single stance: SX3 or SX4
                fastAttackIndex++;
                if (fastAttackIndex >= 2) // After SX4, move to tier-3
                {
                    currentTier = 3;
                    fastAttackIndex = 0;
                }
            }
            else
            {
                // AOE fast or any heavy: advance to tier-3
                currentTier = 3;
                fastAttackIndex = 0;
            }
        }
        else if (currentTier == 3)
        {
            // Finisher executed, reset combo
            ResetCombo();
            return;
        }

        // Restart reset timer
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);
        resetCoroutine = StartCoroutine(ComboResetTimer());

        // Notify AnimFacade of tier change
        if (animFacade != null)
            animFacade.SetComboStage(currentTier);
    }

    /// <summary>
    /// Resets combo to initial state.
    /// </summary>
    public void ResetCombo()
    {
        currentTier = 1;
        fastAttackIndex = 0;
        isHeavyChain = false;
        
        if (animFacade != null)
            animFacade.SetComboStage(1);
        
        if (debugMode)
            Debug.Log("Combo Reset");
    }

    /// <summary>
    /// Coroutine to reset combo after inactivity.
    /// </summary>
    private IEnumerator ComboResetTimer()
    {
        float elapsed = 0f;
        while (elapsed < comboResetTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ResetCombo();
    }

    /// <summary>
    /// Public accessors for debugging/UI.
    /// </summary>
    public int CurrentTier => currentTier;
    public bool IsHeavyChain => isHeavyChain;
    public int FastAttackIndex => fastAttackIndex;

    // Call this from animation events if needed
    public void OnComboFinisher()
    {
        ResetCombo();
    }
}
