using UnityEngine;

/// <summary>
/// Attach this to the SAME GameObject as the Animator that plays your attack clips.
/// It forwards Animation Events to the nearest AnimFacade on this object or its parents.
/// This fixes "AnimationEvent ... has no receiver" when AnimFacade lives on a different object.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimEventRelay : MonoBehaviour
{
    [Tooltip("Optional explicit target; if null, will search on self then parents.")]
    public AnimFacade target;

    void Awake()
    {
        if (target == null)
        {
            target = GetComponent<AnimFacade>();
            if (target == null) target = GetComponentInParent<AnimFacade>();
        }
    }

    // --- Methods expected by clip events ---
    public void SetComboStage(int i) { target?.SetComboStage(i); }
    public void MarkInCombat()       { target?.MarkInCombat(); }
    public void OpenChainWindow()    { target?.OpenChainWindow(); }
    public void CloseChainWindow()   { target?.CloseChainWindow(); }
    public void ReturnToIdle()       { target?.ReturnToIdle(); }
    public void EnableCancel()       { target?.EnableCancel(); }
    public void DisableCancel()      { target?.DisableCancel(); }

    // Safety logging (optional)
    void OnValidate()
    {
        if (GetComponent<Animator>() == null)
        {
            Debug.LogWarning("AnimEventRelay: No Animator on this GameObject. Add this where the Animator is.", this);
        }
    }
}
