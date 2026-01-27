/*
    Written by Brandon Wahl

    This script manages the progression of a zone by tracking the completion status of multiple puzzles.
    It periodically checks if all puzzles are completed and marks the zone as complete when they are.
    So if any designers want to lock any progression behind zone completion, they can reference this script.

*/

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor.EditorTools;

public class ProgressionManager : MonoBehaviour
{
    [Tooltip("Add scripts of puzzles that are in this zone. ie SlowDownElevator.cs if you're in the ElevatorScene")]
    [SerializeField] private EncounterManager[] puzzlesToComplete;

    [Tooltip("Interval in seconds to check if all puzzles are complete")]
    [SerializeField] [Range(0.1f, .5f)] private float checkInterval = .2f;

    private bool zoneIsComplete = false;

    private Dictionary<EncounterManager, bool> encounterCompletionMap = new Dictionary<EncounterManager, bool>();

    private void Awake()
    {
        LoadArrayIntoDictionary();
    }

    private void Start()
    {
        StartCoroutine(IsZoneComplete());
    }

    private void LoadArrayIntoDictionary()
    {
        foreach (EncounterManager encounter in puzzlesToComplete)
        {
            if (!encounterCompletionMap.ContainsKey(encounter))
            {
                encounterCompletionMap.Add(encounter, false);
            }
        }
    }

    private IEnumerator IsZoneComplete()
    {
        var wait = new WaitForSeconds(checkInterval);
        var numberOfEncountersLeft = encounterCompletionMap.Count;
        while (!zoneIsComplete)
        {

            if(numberOfEncountersLeft <= 0)
            {
                zoneIsComplete = true;
                Debug.Log("[ProgressionManager] Zone is complete!");
                yield break;
            }

            yield return wait;
        }
        
    }
}
