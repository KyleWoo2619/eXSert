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

        public string encounterName => this.gameObject.name;

        #region Inspector Settup
        [SerializeField]
        
        private bool startEnabled = true;

        [SerializeField]
        private bool enableEncounterOnComplete = false;

        [SerializeField]
        public BasicEncounter encounterToEnable;

        [SerializeField, Tooltip("Seconds to wait before enabling the next encounter.")]
        protected float enableNextEncounterDelaySeconds = 3f;
        #endregion

        /// <summary>
        /// Indicates whether the player is currently within the encounter zone
        /// </summary>
        protected bool zoneActive = false;

        /// <summary>
        /// Indicates if the encounter can be started when the player is in the zone
        /// </summary>
        protected bool zoneEnabled = false;

        /// <summary>
        /// Indicates whether the encounter has been completed
        /// </summary>
        public abstract bool isCompleted { get; }

        /// <summary>
        /// Indicates whether the encounter has been cleaned up after completion.
        /// </summary>
        public bool isCleanedUp { get; private set; } = false;

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

            SetEncounterEnabled(startEnabled);
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

        /// <summary>
        /// The setup function for the encounter, called during Start after being added to the ProgressionManager
        /// </summary>
        protected abstract void SetupEncounter();

        /// <summary>
        /// The function to clean up the encounter after it is completed, called by the ProgressionManager when this encounter is marked as completed.
        /// </summary>
        protected virtual void CleanupEncounter()
        {
            isCleanedUp = true;

            // disables the encounter collider for simplicity
            encounterZone.enabled = false;
        }

        public void ManualEncounterStart()
        {
            Debug.Log($"Manual start call for encounter {encounterName} in scene {SceneAsset.GetSceneAssetOfObject(this.gameObject).name}.");
            SetupEncounter();
            SetEncounterEnabled(true);
        }

        public void ManualCleanUpCall()
        {
            Debug.Log($"Manual cleanup call for encounter {encounterName} in scene {SceneAsset.GetSceneAssetOfObject(this.gameObject).name}.");
            CleanupEncounter();
        }
        #endregion

        #region Collider Functions
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!zoneEnabled)
                return;
            if (other.gameObject.tag != "Player")
                return;
            zoneActive = true;
            Debug.Log("Zone Entered");
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!zoneEnabled)
                return;
            if (other.gameObject.tag != "Player")
                return;
            zoneActive = false;
            Debug.Log("Zone Left");

            if(isCompleted && !isCleanedUp)
            {
                CleanupEncounter();
            }
        }

        public void SetEncounterEnabled(bool enabled)
        {
            zoneEnabled = enabled;
            if (encounterZone != null)
                encounterZone.enabled = enabled;
        }

        protected void HandleEncounterCompleted()
        {
            if (enableEncounterOnComplete && encounterToEnable != null)
            {
                if (enableNextEncounterDelaySeconds > 0f)
                {
                    StartCoroutine(EnableEncounterAfterDelay());
                }
                else
                {
                    encounterToEnable.SetEncounterEnabled(true);
                }
            }
        }

        private System.Collections.IEnumerator EnableEncounterAfterDelay()
        {
            yield return new WaitForSeconds(enableNextEncounterDelaySeconds);
            encounterToEnable.SetEncounterEnabled(true);
        }

        protected void SetEnableNextEncounterDelaySeconds(float seconds)
        {
            enableNextEncounterDelaySeconds = Mathf.Max(0f, seconds);
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