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
        if (!mgr)
        {
            mgr = animator.GetComponent<EnhancedPlayerAttackManager>();
            if (!mgr) mgr = animator.GetComponentInParent<EnhancedPlayerAttackManager>();
        }
        if (!anim)
        {
            anim = animator.GetComponent<AnimFacade>();
            if (!anim) anim = animator.GetComponentInParent<AnimFacade>();
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
                if (mgr != null && !string.IsNullOrEmpty(attackId))
                {
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
