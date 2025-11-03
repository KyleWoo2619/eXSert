using UnityEngine;

public class TurretPooledProjectile : MonoBehaviour
{
    private Transform returnParent;
    private Rigidbody rb;

    public void InitReturnParent(Transform parent)
    {
        returnParent = parent;
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    // Call this instead of SetActive(false) when you want to return to the pool
    public void ReparentToPoolAndDeactivate()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (returnParent != null)
        {
            // Reparent BEFORE deactivation to avoid the activation/deactivation error
            transform.SetParent(returnParent, false);
        }

        gameObject.SetActive(false);
    }

    // Do not reparent here; it's invoked during activation state change
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}