using UnityEngine;

[DefaultExecutionOrder(5000)]
public class AnimatorStateLogger : MonoBehaviour
{
    public Animator animator;
    public int layer = 0;
    public string prefix = "[AnimatorState]";

    void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!animator) return;
        var st = animator.GetCurrentAnimatorStateInfo(layer);
        var next = animator.GetNextAnimatorStateInfo(layer);
        string name = $"{prefix} L{layer} curr={st.fullPathHash} time={st.normalizedTime:F2} len={st.length:F2} speed={animator.speed:F2}";
        if (animator.IsInTransition(layer))
        {
            name += $" -> next={next.fullPathHash} t={animator.GetAnimatorTransitionInfo(layer).normalizedTime:F2}";
        }
        Debug.Log(name);
    }
}
