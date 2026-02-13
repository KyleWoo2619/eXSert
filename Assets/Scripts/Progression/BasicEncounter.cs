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
    public abstract class BasicEncounter : ProgressionZone
    {
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
        /// Indicates whether the encounter has been completed
        /// </summary>
        public abstract bool isCompleted { get; }

        /// <summary>
        /// Indicates whether the encounter has been cleaned up after completion.
        /// </summary>
        public bool isCleanedUp { get; private set; } = false;

        
        protected override void Start()
        {
            base.Start();

            SetupEncounter();

            SetEncounterEnabled(startEnabled);
        }

        #region Setup Functions
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
            progressionCollider.enabled = false;
        }

        public void ManualEncounterStart()
        {
            Debug.Log($"Manual start call for encounter {encounterName} in scene {SceneAsset.GetSceneAssetOfObject(this.gameObject).name}.");
            SetEncounterEnabled(true);
        }

        public void ManualCleanUpCall()
        {
            Debug.Log($"Manual cleanup call for encounter {encounterName} in scene {SceneAsset.GetSceneAssetOfObject(this.gameObject).name}.");
            CleanupEncounter();
        }
        #endregion

        #region Collider Functions
        protected override void PlayerEnteredZone()
        {
            Debug.Log($"Player entered encounter zone: {encounterName}.");
        }

        protected override void PlayerExitedZone()
        {
            if (isCompleted && !isCleanedUp)
                CleanupEncounter();
        }

        public void SetEncounterEnabled(bool enabled)
        {
            zoneEnabled = enabled;
            if (progressionCollider != null)
                progressionCollider.enabled = enabled;
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
    }
}