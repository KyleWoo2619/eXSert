/*
    Temporary interaction to teleport the player to the FinalBoss scene.
    Copy of PuzzleInteraction structure with teleport behavior on execute.
*/

using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalBossTeleportInteraction : UnlockableInteraction
{
    [Header("Teleport Settings")]
    [SerializeField] private string targetSceneName = "FinalBoss";
    [SerializeField] private string spawnPointId = "default";
    [SerializeField] private bool pauseUntilLoaded = false;

    protected override void ExecuteInteraction()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[FinalBossTeleportInteraction] No target scene name set.");
            return;
        }

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadInitialGameScene(
                targetSceneName,
                additiveSceneName: null,
                pauseUntilLoaded: pauseUntilLoaded,
                spawnPointIdOverride: spawnPointId,
                updateCheckpointAfterLoad: false);
            return;
        }

        Debug.LogWarning("[FinalBossTeleportInteraction] SceneLoader not found. Loading scene directly.");
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
