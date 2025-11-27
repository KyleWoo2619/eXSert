using UnityEngine;
using UnityEditor;
using EnemyBehavior.Boss;

[CustomEditor(typeof(BossRoombaBrain))]
public class BossRoombaBrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BossRoombaBrain brain = (BossRoombaBrain)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Trigger Vacuum Sequence"))
        {
            brain.DebugTriggerVacuumSequence();
        }

        if (GUILayout.Button("Return to Duelist Form"))
        {
            brain.DebugReturnToDuelistForm();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stun Testing", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Parry Stun"))
        {
            brain.DebugApplyParryStun();
        }

        if (GUILayout.Button("Apply Pillar Stun"))
        {
            brain.DebugApplyPillarStun();
        }
    }
}
