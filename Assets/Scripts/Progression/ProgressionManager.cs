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
        private bool zoneIsComplete = false;

        private Dictionary<BasicEncounter, bool> encounterCompletionMap = new Dictionary<BasicEncounter, bool>();

        protected override void Awake()
        {
            base.Awake();

            this.gameObject.name = $"[{SceneAsset.GetSceneAssetOfObject(this.gameObject).name}] Progression Manager";
        }

        public void AddEncounter(BasicEncounter encounter)
        {
            encounterCompletionMap.Add(encounter, false);
        }
    }
}

