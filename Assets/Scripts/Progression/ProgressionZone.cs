using UnityEngine;

namespace Progression
{
    [RequireComponent(typeof(BoxCollider))]
    public abstract class ProgressionZone : MonoBehaviour
    {
        private ProgressionManager progressionManager;

        protected BoxCollider progressionCollider;

        /// <summary>
        /// Indicates if the encounter can be started when the player is in the zone
        /// </summary>
        protected bool zoneEnabled = false;

        /// <summary>
        /// Indicates whether the player is currently within the encounter zone
        /// </summary>
        protected bool zoneActive = false;

        protected virtual void Awake()
        {
            progressionCollider = GetComponent<BoxCollider>();

            if (progressionCollider == null)
                Debug.LogError("ProgressionZone requires a BoxCollider component.");
            else
                progressionCollider.isTrigger = true;
        }

        protected virtual void Start()
        {
            progressionManager = FindManager();

            if (progressionManager == null) return;

            AddToManager();
        }

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

        private void AddToManager()
        {
            progressionManager.AddProgressable(this);
        }

        #region Collider Triggers
        protected void OnTriggerEnter(Collider other)
        {
            if (!zoneActive || !other.CompareTag("Player")) return;
            zoneActive = true;
            PlayerEnteredZone();
        }
        protected void OnTriggerExit(Collider other)
        {
            if (!zoneEnabled || !other.CompareTag("Player")) return;
            zoneActive = false;
            PlayerExitedZone();
        }

        protected abstract void PlayerEnteredZone();
        protected abstract void PlayerExitedZone();
        #endregion

        #region Debug Scripts
        protected abstract Color DebugColor { get; }

        private void OnDrawGizmos()
        {
            if (progressionCollider == null)
                progressionCollider = GetComponent<BoxCollider>();

            Gizmos.color = DebugColor;
            Gizmos.DrawWireCube(progressionCollider.bounds.center, progressionCollider.bounds.size);
        }
        #endregion
    }
}
