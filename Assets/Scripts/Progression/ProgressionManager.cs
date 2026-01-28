/* 
    Written by Brandon Wahl

    This script manages the progression of a zone by tracking the completion status of multiple puzzles and combat encounters.
    It keeps track of all the encounters within the scene and manages communication between the different encounters.
    
    Written later on by Will T
*/

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor.EditorTools;

public class ProgressionManager : MonoBehaviour
{
    #region Singletonish Functionality
    // Singleton instance dictionary to ensure one ProgressionManager per scene
    private static Dictionary<SceneAsset, ProgressionManager> instances = new Dictionary<SceneAsset, ProgressionManager>();

    // Get the ProgressionManager instance for a specific scene
    public static ProgressionManager GetInstance(SceneAsset scene)
    {
        if (instances.TryGetValue(scene, out ProgressionManager instance))
        {
            return instance;
        }
        else
        {
            Debug.LogError($"[ProgressionManager] No ProgressionManager instance found for scene: {scene.name}");
            return null;
        }
    }
    #endregion


    [Tooltip("Add scripts of puzzles that are in this zone. ie SlowDownElevator.cs if you're in the ElevatorScene")]
    [SerializeField] private BasicEncounter[] encountersToComplete;

    [Tooltip("Interval in seconds to check if all puzzles are complete")]
    [SerializeField] [Range(0.1f, .5f)] private float checkInterval = .2f;

    private bool zoneIsComplete = false;

    private Dictionary<BasicEncounter, bool> encounterCompletionMap = new Dictionary<BasicEncounter, bool>();

    private void Awake()
    {
        // implement singletonish functionality

        if (instances.ContainsKey())
        {

        }

        LoadArrayIntoDictionary();
    }

    private void Start()
    {
        StartCoroutine(IsZoneComplete());
    }

    private void LoadArrayIntoDictionary()
    {
        foreach (BasicEncounter encounter in encountersToComplete)
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
