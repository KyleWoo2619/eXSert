using UnityEngine;

/// <summary>
/// Animator StateMachineBehaviour that drives per-state attack windows without clip events.
/// Add this to each attack state and set the attackId to match your AttackDatabase.
/// It will request a one-shot hitbox window from EnhancedPlayerAttackManager.
/// </summary>
public class AttackStateDriver : StateMachineBehaviour
{
    [Header("Attack Id")] public string attackId = ""; // e.g., SX1, AX2, SY1, AY2

    [Header("Active Window (seconds)")]
    public float activeStart = 0.15f;   // time after state entry when hitbox turns on
    public float activeDuration = 0.12f; // how long the hitbox stays active

    [Header("Chain Window (normalized time)")]
    [Tooltip("Open the chain/cancel window when normalizedTime >= this value (0..1). Set to -1 to disable.")]
    public float chainOpenAt = 0.7f;

    private bool spawned;
    private bool chainOpened;
    private EnhancedPlayerAttackManager mgr;
    private AnimFacade anim;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        spawned = false;
        chainOpened = false;
        
        // Always try to get the manager reference (important for DontDestroyOnLoad objects)
        if (!mgr)
        {
            // Try on same GameObject first
            mgr = animator.GetComponent<EnhancedPlayerAttackManager>();
            
            // Try on parent
            if (!mgr) mgr = animator.GetComponentInParent<EnhancedPlayerAttackManager>();
            
            // Try on children
            if (!mgr) mgr = animator.GetComponentInChildren<EnhancedPlayerAttackManager>();
            
            // Last resort: Find in scene (works even with DontDestroyOnLoad)
            if (!mgr) mgr = GameObject.FindAnyObjectByType<EnhancedPlayerAttackManager>();
            
            if (!mgr)
            {
                Debug.LogError($"[AttackStateDriver] Could not find EnhancedPlayerAttackManager anywhere! Make sure it exists on the player.");
            }
        }
        
        if (!anim)
        {
            anim = animator.GetComponent<AnimFacade>();
            if (!anim) anim = animator.GetComponentInParent<AnimFacade>();
            if (!anim) anim = animator.GetComponentInChildren<AnimFacade>();
        }

        // Lock input while the state plays
        InputReader.inputBusy = true;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float t = stateInfo.normalizedTime; // can exceed 1 if looping; we only care about first cycle
        if (t > 1f) t = 1f;

        // Spawn hitbox once when time surpasses activeStart
        if (!spawned && stateInfo.length > 0f)
        {
            float currentSec = stateInfo.length * t;
            if (currentSec >= activeStart)
            {
                spawned = true;
                Debug.Log($"[AttackStateDriver] Trying to spawn hitbox for '{attackId}' at time {currentSec}s");
                
                if (mgr == null)
                {
                    Debug.LogError($"[AttackStateDriver] EnhancedPlayerAttackManager is NULL! Cannot spawn hitbox for '{attackId}'");
                }
                else if (string.IsNullOrEmpty(attackId))
                {
                    Debug.LogError($"[AttackStateDriver] attackId is empty! Set it in the Animator state behaviour.");
                }
                else
                {
                    Debug.Log($"[AttackStateDriver] Calling SpawnOneShotHitbox for '{attackId}' with duration {activeDuration}s");
                    mgr.SpawnOneShotHitbox(attackId, activeDuration);
                }
            }
        }

        // Open chain window late in the state so BufferedX/BufferedY transitions can fire
        if (!chainOpened && chainOpenAt >= 0f && t >= chainOpenAt)
        {
            chainOpened = true;
            anim?.OpenChainWindow();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Release input and close chain window on exit
        InputReader.inputBusy = false;
        anim?.CloseChainWindow();
        anim?.ClearBufferedInputs();
    }
}
