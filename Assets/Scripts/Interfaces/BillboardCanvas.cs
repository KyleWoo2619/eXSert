using UnityEngine;

/*
Written by Kyle Woo
This script makes a UI element (with a Canvas component) always face a target (usually the main camera).
It can optionally lock rotation around the Y axis to keep the UI upright.
*/

public class BillboardUI : MonoBehaviour
{
    [Tooltip("What this should face. Leave empty to use the main camera.")]
    public Transform target;

    [Tooltip("Keep the bar upright (rotate only around Y).")]
    public bool lockY = true;

    [Tooltip("Higher = snappier; 0 = instant.")]
    public float smooth = 12f;

    void Awake()
    {
        if (target == null) target = Camera.main ? Camera.main.transform : null;

        // Optional but recommended: set the Canvas' worldCamera to the main Camera
        var c = GetComponent<Canvas>();
        if (c && c.renderMode == RenderMode.WorldSpace && c.worldCamera == null && Camera.main)
            c.worldCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Direction from UI to target (so the front faces the target).
        Vector3 dir = target.position - transform.position;

        if (lockY) dir.y = 0f;                 // keep it upright
        if (dir.sqrMagnitude < 1e-6f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Smooth face
        if (smooth <= 0f) transform.rotation = look;
        else transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
