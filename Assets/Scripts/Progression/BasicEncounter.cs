/*
 * Made by Brandon, Implemented by Will
 * 
 * The core framework for implementing the Encounters
 * 
 */

using UnityEngine;

namespace Progression.Encounters
{
    [RequireComponent(typeof(BoxCollider))]
    public abstract class BasicEncounter : MonoBehaviour
    {
        private ProgressionManager progressionManager;

        protected bool zoneActive = false;

        protected BoxCollider encounterZone;
        protected virtual void Awake()
        {
            encounterZone = GetComponent<BoxCollider>();

            if (encounterZone == null)
                Debug.LogError("Encounter couldn't find BoxCollider attached to gameobject");
        }
        protected virtual void Start()
        {
            // Find ProgressionManager in Scene and add this under its database
            progressionManager = FindManager();
            if (progressionManager == null)
                return;

            // add this to the manager's database
            AddEncounterToManager();

            // basic encounter setup
            SetupEncounter();
        }

        #region Setup Functions
        private ProgressionManager FindManager()
        {
            SceneAsset asset = SceneAsset.GetSceneAssetOfObject(this.gameObject);
            ProgressionManager manager = ProgressionManager.GetInstance(asset);
            if (manager == null)
            {
                Debug.LogError($"{this.gameObject.name} could not find ProgressionManager in scene {asset.name}");
            }
            return manager;
        }

        private void AddEncounterToManager()
        {
            progressionManager.AddEncounter(this);
        }

        protected abstract void SetupEncounter();
        #endregion

        #region Collider Functions
        protected virtual void OnTriggerEnter(Collider other)
        {
           zoneActive = true;
            Debug.Log("Zone Entered");
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            zoneActive = false;
            Debug.Log("Zone Left");
        }
        #endregion

        #region Debug Scripts
        protected abstract Color DebugColor { get; }

        private void OnDrawGizmos()
        {
            if(encounterZone == null)
                encounterZone = GetComponent<BoxCollider>();

            Gizmos.color = DebugColor;
            Gizmos.DrawWireCube(encounterZone.bounds.center, encounterZone.bounds.size);
        }
        #endregion
    }
}