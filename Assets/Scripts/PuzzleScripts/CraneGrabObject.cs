/*
    Written by Brandon W

    This script should be attached to the magnet extender and handles the grabbing and releasing of the desired object.
*/

using UnityEngine;

public class CraneGrabObject : MonoBehaviour
{
    [SerializeField] private CargoBayCrane cargoBayCrane;

    private void Start()
    {
        // Verify setup
        if (cargoBayCrane == null)
        {
            Debug.LogError("CraneGrabObject: CranePuzzle reference not set!");
            return;
        }
        
        if (cargoBayCrane.magnetExtender == null)
        {
            Debug.LogError("CraneGrabObject: magnetExtender not set on CranePuzzle!");
            return;
        }

        // Verify this GameObject has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"CraneGrabObject: {gameObject.name} has no Collider component!");
            return;
        }
    }

    public void GrabObject(GameObject obj)
    {
        // Parent to the magnet extender while preserving world position
        obj.transform.SetParent(cargoBayCrane.magnetExtender.transform, true);;
    }

    public void ReleaseObject(GameObject obj)
    {
        // Unparent the object
        obj.transform.SetParent(null, true);;
    }

}
