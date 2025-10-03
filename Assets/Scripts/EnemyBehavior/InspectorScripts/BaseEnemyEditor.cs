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
    }
}
#endif
