using UnityEngine;

namespace EnemyBehavior
{
    // Lightweight scene validator. Add to any GameObject in each scene.
    // Warns when required systems are missing/misconfigured to help during additive loading/testing.
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "SceneBootstrap: runtime validator that logs warnings if core systems are missing.\n" +
            "Use in every scene during development, disable or remove for release.\n" +
            "Enable boss hint to remind adding ScenePoolManager when testing the boss scene.";

        [Header("Expected Systems In Scene")]
        public bool RequirePathRequestManager = true;
        public bool RequireDensityGrid = true;
        public bool RequireCrowdController = true;
        public bool RequirePlayerTag = true;

        [Header("Optional (Boss Scene)")]
        public bool SuggestScenePoolManager = false;

        void Start()
        {
#if UNITY_EDITOR
            if (RequirePathRequestManager && FindObjectOfType<EnemyBehavior.Pathfinding.PathRequestManager>() == null)
            {
                Debug.LogWarning("[Bootstrap] PathRequestManager not found in scene.");
            }
            if (RequireDensityGrid && EnemyBehavior.Density.DensityGrid.Instance == null)
            {
                Debug.LogWarning("[Bootstrap] DensityGrid not found or not initialized in scene.");
            }
            if (RequireCrowdController && EnemyBehavior.Crowd.CrowdController.Instance == null)
            {
                Debug.LogWarning("[Bootstrap] CrowdController not found in scene.");
            }
            if (RequirePlayerTag && !PlayerPresenceManager.IsPlayerPresent && GameObject.FindGameObjectWithTag("Player") == null)
            {
                Debug.LogWarning("[Bootstrap] No GameObject tagged 'Player' found in scene.");
            }
            if (SuggestScenePoolManager && EnemyBehavior.Crowd.ScenePoolManager.Instance == null)
            {
                Debug.Log("[Bootstrap] ScenePoolManager is not present. That's fine unless this is the boss scene with add spawns.");
            }
#endif
        }
    }
}
