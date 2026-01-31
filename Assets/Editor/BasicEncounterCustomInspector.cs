/*
 * Written by Will T
 * 
 * editor script to create a drop-down inspector for selecting from a list of game objects within the scene
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Progression.Encounters;

[CustomEditor(typeof(BasicEncounter))]
public class BasicEncounterCustomInspector : Editor
{
    private int selectedIndex = -1;
    private List<BasicEncounter> foundEncounters = new List<BasicEncounter>();
    private List<string> encounterNames = new List<string>();

    private void OnEnable()
    {
        // Find all BasicEncounter objects in the scene
        foundEncounters = FindObjectsByType<BasicEncounter>(FindObjectsSortMode.InstanceID).ToList();
        encounterNames = foundEncounters.Select(e => e.encounterName).ToList();

        encounterNames.Insert(0, "None"); // Add a "None" option at the start

        BasicEncounter user = (BasicEncounter)target;
        if (user.encounterToEnable != null)
        {
            int index = foundEncounters.IndexOf(user.encounterToEnable);
            if (index != -1)
                selectedIndex = index + 1; // +1 for "None" option
        }
        if (selectedIndex == -1) selectedIndex = 0; // Default to "None"
    }

    public override void OnInspectorGUI()
    {
        // Draws the default inspector fields first
        DrawDefaultInspector();

        // Adds the drop-down menu
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);

        int newIndex = EditorGUILayout.Popup("Encounter to Enable on Complete", selectedIndex, encounterNames.ToArray());

        if(newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            BasicEncounter user = (BasicEncounter)target;

            // Update the reference based on selection
            if (selectedIndex == 0)
                user.encounterToEnable = null; // "None" selected

            else
                user.encounterToEnable = foundEncounters[selectedIndex - 1]; // -1 for "None" option

            // Mark the object as dirty to ensure changes are saved
            EditorUtility.SetDirty(user);
        }
    }
}
