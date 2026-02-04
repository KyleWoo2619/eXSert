/*
 * Attack Hitbox Visualizer
 * 
 * Draws bright, visible gizmos for player attack hitboxes in Scene view.
 * Makes it easy to see attack range, width, and position when testing.
 */

using UnityEngine;

public class AttackHitboxVisualizer : MonoBehaviour
{
    [HideInInspector] public float width = 1f;
    [HideInInspector] public float range = 1.5f;
    [HideInInspector] public string attackName = "";

    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null) return;

        // VERY BRIGHT and VISIBLE colors for player attack hitbox
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        // Draw bright cyan/blue filled cube (player attack = blue)
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f); // Bright cyan with 60% opacity
        Gizmos.DrawCube(boxCollider.center, boxCollider.size);

        // Draw bright yellow wireframe
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

        // Draw outer glow (white)
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size * 1.05f);

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;

        // Draw label in Scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"ATTACK: {attackName}\nSize: {width}x{range}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            }
        );
        #endif
    }
}
