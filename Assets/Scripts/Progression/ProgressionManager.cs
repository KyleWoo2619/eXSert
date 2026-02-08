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
        /// <summary>
        /// Indicates whether all encounters in the scene have been completed
        /// </summary>
        private bool allZonesComplete = false;

        private List<BasicEncounter> encounterCompletionMap = new List<BasicEncounter>();

        protected override void Awake()
        {
            base.Awake();

            this.gameObject.name = $"[{SceneAsset.GetSceneAssetOfObject(this.gameObject).name}] Progression Manager";
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

