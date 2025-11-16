// BaseEnemyEditor.cs
// Purpose: Custom inspector for BaseEnemy derived types to provide enhanced visualization and debugging in the Unity Editor.
// Works with: BaseEnemy, ReadOnlyAttribute, MaxHealthSlider.
// Notes: Editor-only code; not included in builds.

using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(BaseEnemy<,>), true)]
public class BaseEnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var baseEnemy = target as MonoBehaviour;
        var maxHealthProp = serializedObject.FindProperty("maxHealth");
        if (maxHealthProp != null && maxHealthProp.floatValue <= 0f)
        {
            EditorGUILayout.HelpBox("maxHealth must be greater than 0!", MessageType.Error);
        }

        // Add "Test Enemy Death" button under Health section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health Testing", EditorStyles.boldLabel);
        if (GUILayout.Button("Test Enemy Death"))
        {
            // Use reflection to call SetHealth(0f) on the enemy
            var method = baseEnemy.GetType().GetMethod("SetHealth");
            if (method != null)
            {
                method.Invoke(baseEnemy, new object[] { 0f });
            }
            else
            {
                Debug.LogWarning($"{baseEnemy.name}: SetHealth(float) method not found.");
            }
        }
        // Test Enemy Take Damage button
        if (GUILayout.Button("Test Enemy Taking Damage"))
        {
            var method = baseEnemy.GetType().GetMethod("LoseHP");
            if (method != null)
            {
                method.Invoke(baseEnemy, new object[] { 10f });
            }
            else
            {
                Debug.LogWarning($"{baseEnemy.name}: LoseHP(float) method not found.");
            }
        }
    }
}
#endif
