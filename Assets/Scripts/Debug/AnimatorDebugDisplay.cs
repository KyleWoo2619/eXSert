using UnityEngine;

/// <summary>
/// Diagnostic tool to verify AnimFacade and Animator are working correctly.
/// Attach this to your Player GameObject to see real-time animator state in the Scene view.
/// </summary>
public class AnimatorDebugDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimFacade animFacade;
    
    [Header("Display Options")]
    [SerializeField] private bool showInGameGUI = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float displayYOffset = 2f;
    
    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        animFacade = GetComponentInChildren<AnimFacade>();
    }

    private void OnValidate()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animFacade == null) animFacade = GetComponentInChildren<AnimFacade>();
    }

    private void OnGUI()
    {
        if (!showInGameGUI || animator == null) return;

        // Display animator state info in top-left corner
        GUILayout.BeginArea(new Rect(10, 10, 450, 350));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("<b>Animator Debug Info</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
        GUILayout.Space(5);

        // Current state
        if (animator.GetCurrentAnimatorClipInfo(0).Length > 0)
        {
            string clipName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
            GUILayout.Label($"<color=yellow>Current Clip: {clipName}</color>", new GUIStyle(GUI.skin.label) { richText = true });
        }
        else
        {
            GUILayout.Label("<color=red>❌ NO CLIP PLAYING!</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12, fontStyle = FontStyle.Bold });
            GUILayout.Label("<color=orange>→ Check Router transitions!</color>", new GUIStyle(GUI.skin.label) { richText = true });
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        GUILayout.Label($"State Hash: {stateInfo.shortNameHash}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 10 });

        GUILayout.Space(10);

        // Parameters
        GUILayout.Label("<b>Parameters:</b>", new GUIStyle(GUI.skin.label) { richText = true });
        
        float speed = animator.GetFloat("Speed");
        bool isGrounded = animator.GetBool("IsGrounded");
        float vertSpeed = animator.GetFloat("VertSpeed");
        int stance = animator.GetInteger("Stance");
        bool inCombat = animator.GetBool("InCombat");

        GUILayout.Label($"Speed: <color={(speed > 0.1f ? "lime" : "white")}>{speed:F2}</color>", new GUIStyle(GUI.skin.label) { richText = true });
        
        // Highlight IsGrounded status more clearly
        string groundedStatus = isGrounded ? "<color=lime><b>TRUE ✓</b></color>" : "<color=red><b>FALSE ✗</b></color>";
        GUILayout.Label($"IsGrounded: {groundedStatus}", new GUIStyle(GUI.skin.label) { richText = true });
        if (!isGrounded)
        {
            GUILayout.Label("<color=yellow>→ Check LayerMask & maxDistance!</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 10 });
        }
        
        GUILayout.Label($"VertSpeed: <color={(Mathf.Abs(vertSpeed) < 0.5f ? "white" : "yellow")}>{vertSpeed:F2}</color>", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"Stance: <color=cyan>{stance}</color> {GetStanceName(stance)}", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"InCombat: <color={(inCombat ? "orange" : "white")}>{inCombat}</color>", new GUIStyle(GUI.skin.label) { richText = true });

        GUILayout.Space(10);

        // AnimFacade check
        if (animFacade != null)
        {
            GUILayout.Label("<color=lime>✓ AnimFacade Connected</color>", new GUIStyle(GUI.skin.label) { richText = true });
        }
        else
        {
            GUILayout.Label("<color=red>✗ AnimFacade Missing!</color>", new GUIStyle(GUI.skin.label) { richText = true });
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || animator == null) return;

        // Draw parameter info above player head
        Vector3 labelPos = transform.position + Vector3.up * displayYOffset;
        
        float speed = animator.GetFloat("Speed");
        bool isGrounded = animator.GetBool("IsGrounded");
        float vertSpeed = animator.GetFloat("VertSpeed");
        
        string info = $"Speed: {speed:F1} | Ground: {isGrounded} | VSpeed: {vertSpeed:F1}";
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, info, new GUIStyle()
        {
            fontSize = 12,
            normal = new GUIStyleState()
            {
                textColor = isGrounded ? Color.green : Color.yellow
            }
        });
#endif
    }

    private string GetStanceName(int stance)
    {
        return stance switch
        {
            0 => "(Single Target)",
            1 => "(AOE)",
            2 => "(Guard)",
            _ => "(Unknown)"
        };
    }

    // Debug menu for testing
    [ContextMenu("Test: Set Stance 0 (Single)")]
    private void TestSetStanceSingle()
    {
        if (animFacade != null)
        {
            animFacade.SetStance(0);
            Debug.Log("Set Stance to 0 (Single Target)");
        }
    }

    [ContextMenu("Test: Set Stance 1 (AOE)")]
    private void TestSetStanceAOE()
    {
        if (animFacade != null)
        {
            animFacade.SetStance(1);
            Debug.Log("Set Stance to 1 (AOE)");
        }
    }

    [ContextMenu("Test: Set Stance 2 (Guard)")]
    private void TestSetStanceGuard()
    {
        if (animFacade != null)
        {
            animFacade.SetStance(2);
            Debug.Log("Set Stance to 2 (Guard)");
        }
    }

    [ContextMenu("Print All Animator Parameters")]
    private void PrintAllParameters()
    {
        if (animator == null)
        {
            Debug.LogError("No Animator assigned!");
            return;
        }

        Debug.Log("=== All Animator Parameters ===");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            string value = param.type switch
            {
                AnimatorControllerParameterType.Float => animator.GetFloat(param.name).ToString("F2"),
                AnimatorControllerParameterType.Int => animator.GetInteger(param.name).ToString(),
                AnimatorControllerParameterType.Bool => animator.GetBool(param.name).ToString(),
                AnimatorControllerParameterType.Trigger => "(Trigger)",
                _ => "Unknown"
            };
            
            Debug.Log($"{param.name} ({param.type}): {value}");
        }
        Debug.Log("==============================");
    }
}
