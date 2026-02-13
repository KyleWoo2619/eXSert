/* 
    Written by Brandon Wahl

    This script manages the progression of a zone by tracking the completion status of multiple puzzles and combat encounters.
    It keeps track of all the encounters within the scene and manages communication between the different encounters.
    
    Written later on by Will T
*/

using Singletons;
using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    using Encounters;

    public class ProgressionManager : SceneSingleton<ProgressionManager>
    {
        #region Inspector Setup
        [Header("Progression Settings")]
        [SerializeField, Tooltip("The next scene to load at the end of the level.")]
        private SceneAsset nextScene;
        [Space]
        [SerializeField, Tooltip("Use when the player would load already inside an encounter, such as the elevator fight")]
        private bool startEncounterOnStart = false;
        [SerializeField, Tooltip("The encounter to automatically start")]
        private BasicEncounter encounterToStart;

        #endregion
        
        /// <summary>
        /// Indicates whether all encounters in the scene have been completed
        /// </summary>
        private bool allZonesComplete = false;

        private readonly List<BasicEncounter> encounterCompletionMap = new();

        protected override void Awake()
        {
            base.Awake(); // Singleton behavior

            this.gameObject.name = $"[{SceneAsset.GetSceneAssetOfObject(this.gameObject).name}] Progression Manager";
        }

        protected void Start()
        {
            if (startEncounterOnStart && encounterToStart != null)
            {
                encounterToStart.ManualEncounterStart();
            }
        }

        private void OnDisable()
        {
            foreach (BasicEncounter encounter in encounterCompletionMap)
            {
                if (encounter != null && !encounter.isCleanedUp)
                    encounter.ManualCleanUpCall();
            }

            encounterCompletionMap.Clear();
        }
        

        /// <summary>
        /// Adds the encounter to the manager's database
        /// </summary>
        /// <param name="encounter"></param>
        public void AddEncounter(BasicEncounter encounter)
        {
            encounterCompletionMap.Add(encounter);
        }
    }
}

