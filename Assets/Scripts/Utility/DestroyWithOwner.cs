using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps a helper GameObject (for example, an IK target) alive only while its owner exists.
/// Once the referenced owner Transform is destroyed or disabled, this helper destroys itself
/// to avoid dangling references or warning icons in the Rig Builder.
/// </summary>
public class DestroyWithOwner : MonoBehaviour
{
    [SerializeField, Tooltip("Transform that owns this helper. When it is destroyed, this object is removed as well.")]
    private Transform ownerTransform;

    [SerializeField, Tooltip("Optional delay before destroying the helper after the owner disappears.")]
    private float destroyDelay = 0f;

    [SerializeField, Tooltip("If true, the helper is also destroyed when the owner GameObject is merely disabled.")]
    private bool destroyWhenOwnerDisabled = true;

    private bool destroyScheduled = false;

    private void Reset()
    {
        ownerTransform = transform.parent;
    }

    private void Awake()
    {
        if (ownerTransform == null && transform.parent != null)
            ownerTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (destroyScheduled)
            return;

        if (OwnerMissing())
        {
            StartCoroutine(DestroyRoutine());
        }
    }

    public void SetOwner(Transform owner)
    {
        ownerTransform = owner;
    }

    private bool OwnerMissing()
    {
        if (ownerTransform == null)
            return true;

        if (!destroyWhenOwnerDisabled)
            return false;

        return !ownerTransform.gameObject.activeInHierarchy;
    }

    private IEnumerator DestroyRoutine()
    {
        destroyScheduled = true;
        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
