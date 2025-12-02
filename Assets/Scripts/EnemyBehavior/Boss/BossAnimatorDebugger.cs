using UnityEngine;

/// <summary>
/// Diagnostic tool: attach to Boss_Roomba to log all animator.SetTrigger() calls.
/// This helps debug why animations aren't playing.
/// </summary>
public class BossAnimatorDebugger : MonoBehaviour
{
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[BossAnimatorDebugger] No Animator found!");
            return;
        }
        
        Debug.Log($"[BossAnimatorDebugger] Monitoring animator on '{animator.gameObject.name}'");
        
        // CRITICAL DIAGNOSTICS
        Debug.Log($"[BossAnimatorDebugger] ===== ANIMATOR CONFIGURATION =====");
        Debug.Log($"  Animator Enabled: {animator.enabled}");
        Debug.Log($"  Animator Speed: {animator.speed}");
        Debug.Log($"  Controller Assigned: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE - THIS IS THE PROBLEM!")}");
        Debug.Log($"  Update Mode: {animator.updateMode}");
        Debug.Log($"  Culling Mode: {animator.cullingMode}");
        Debug.Log($"  Layer Count: {animator.layerCount}");
        
        for (int i = 0; i < animator.layerCount; i++)
        {
            Debug.Log($"    Layer {i}: {animator.GetLayerName(i)}, Weight: {animator.GetLayerWeight(i)}");
        }
        Debug.Log($"==========================================");
        
        LogAnimatorParameters();
    }
    
    private void Update()
    {
        if (animator == null) return;
        
        // Log current state every frame (can be noisy, toggle off if needed)
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var stateName = GetCurrentStateName();
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log($"[BossAnimatorDebugger] ===== CURRENT STATE =====");
            Debug.Log($"  State: {stateName}");
            Debug.Log($"  State Hash: {stateInfo.shortNameHash}");
            Debug.Log($"  Normalized Time: {stateInfo.normalizedTime:F2}");
            Debug.Log($"  Speed Param: {animator.GetFloat("Speed"):F2}");
            Debug.Log($"  IsMoving Param: {animator.GetBool("IsMoving")}");
            Debug.Log($"  Animator.speed: {animator.speed}");
            
            // Check if any transitions are active
            if (animator.IsInTransition(0))
            {
                var transInfo = animator.GetAnimatorTransitionInfo(0);
                Debug.Log($"  IN TRANSITION - Progress: {transInfo.normalizedTime:F2}");
            }
            else
            {
                Debug.Log($"  Not in transition");
            }
            
            // Check for triggers that are set
            Debug.Log($"  Checking trigger states...");
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    bool isSet = animator.GetBool(param.nameHash); // Triggers are stored as bools internally
                    if (isSet)
                    {
                        Debug.Log($"    TRIGGER SET: {param.name}");
                    }
                }
            }
            
            Debug.Log($"=============================");
            LogAnimatorParameters();
            
            // Check next state info
            var nextInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextInfo.fullPathHash != 0)
            {
                Debug.Log($"  Next State Hash: {nextInfo.shortNameHash}");
            }
        }
        
        // Auto-log every 2 seconds to catch state changes
        if (Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
        {
            Debug.Log($"[BossAnimatorDebugger] Current state: {stateName}, Normalized time: {stateInfo.normalizedTime:F2}");
        }
    }
    
    private string GetCurrentStateName()
    {
        if (animator == null) return "None";
        
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length > 0)
        {
            return clips[0].clip.name;
        }
        
        // Fallback: try to get state name from state info
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Try to find matching state by hash
        var controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            // This won't give us the name directly, but at least we have the hash
            return $"StateHash_{stateInfo.shortNameHash}";
        }
        
        return "Unknown";
    }
    
    private void LogAnimatorParameters()
    {
        if (animator == null) return;
        
        Debug.Log("[BossAnimatorDebugger] ===== ANIMATOR PARAMETERS =====");
        foreach (var param in animator.parameters)
        {
            string value = param.type switch
            {
                AnimatorControllerParameterType.Float => animator.GetFloat(param.name).ToString("F2"),
                AnimatorControllerParameterType.Int => animator.GetInteger(param.name).ToString(),
                AnimatorControllerParameterType.Bool => animator.GetBool(param.name).ToString(),
                AnimatorControllerParameterType.Trigger => "(trigger)",
                _ => "Unknown"
            };
            
            Debug.Log($"  {param.type} '{param.name}' = {value}");
        }
        Debug.Log("==========================================");
    }
}
