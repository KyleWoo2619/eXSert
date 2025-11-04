using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages enemy waves for tutorial progression.
/// Enables/disables parent GameObjects containing pre-placed enemies.
/// Works with ObjectiveManager to track progress and update notices.
/// </summary>
public class WaveBasedProgression : MonoBehaviour
{
    [System.Serializable]
    public class EnemyWave
    {
        [Tooltip("Parent GameObject containing this wave's enemies")]
        public GameObject waveParent;
        
        [Tooltip("Notice text to show after completing previous wave")]
        public string noticeText = "Use the X Button to Light Attack!";
        
        [Tooltip("Delay after previous wave clears before spawning this wave (seconds)")]
        public float spawnDelay = 3f;
    }

    [Header("Wave Configuration")]
    [SerializeField, Tooltip("Array of enemy waves to spawn sequentially")]
    private EnemyWave[] enemyWaves;

    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool wavesComplete = false;

    [Header("Objective Settings")]
    [SerializeField] private string objectiveText = "Eliminate ALL ENEMIES";

    [Header("Progression Settings")]
    [SerializeField] private GameObject elevatorWallClosed;
    [SerializeField] private GameObject elevatorWallOpen;
    [SerializeField] private string nextSceneName = "FP_Bridge";
    [SerializeField] private bool loadSceneAdditively = true;
    [SerializeField] private float delayBeforeSceneLoad = 2f;

    [Header("Enemy Tracking")]
    [SerializeField] private float checkInterval = 0.5f;
    private List<GameObject> currentWaveEnemies = new List<GameObject>();
    private bool isCheckingWave = false;

    private void Start()
    {
        // Verify ObjectiveManager exists
        if (ObjectiveManager.Instance == null)
        {
            Debug.LogError("[WaveBasedProgression] ObjectiveManager not found in scene! Add one first.");
            return;
        }

        // Disable all wave parents initially
        foreach (var wave in enemyWaves)
        {
            if (wave.waveParent != null)
                wave.waveParent.SetActive(false);
        }

        // Set initial objective
        ObjectiveManager.Instance.UpdateObjective(objectiveText, false);

        // Start first wave
        StartCoroutine(StartWave(0));
    }

    private IEnumerator StartWave(int waveIndex)
    {
        if (waveIndex >= enemyWaves.Length)
        {
            // All waves complete
            OnAllWavesComplete();
            yield break;
        }

        currentWaveIndex = waveIndex;
        EnemyWave wave = enemyWaves[waveIndex];

        Debug.Log($"🌊 [WaveBasedProgression] Starting Wave {waveIndex + 1}/{enemyWaves.Length}");

        // Update notice text for this wave
        if (!string.IsNullOrEmpty(wave.noticeText))
        {
            ObjectiveManager.Instance.UpdateNotice(wave.noticeText, waveIndex > 0); // Play sound for waves after first
        }

        // Wait for spawn delay (except first wave)
        if (waveIndex > 0)
        {
            Debug.Log($"⏳ [WaveBasedProgression] Waiting {wave.spawnDelay}s before spawning wave...");
            yield return new WaitForSeconds(wave.spawnDelay);
        }

        // Enable wave parent GameObject
        if (wave.waveParent != null)
        {
            wave.waveParent.SetActive(true);
            Debug.Log($"✅ [WaveBasedProgression] Wave {waveIndex + 1} spawned (enabled {wave.waveParent.name})");

            // Collect enemy references from this wave
            CollectWaveEnemies(wave.waveParent);
        }
        else
        {
            Debug.LogWarning($"[WaveBasedProgression] Wave {waveIndex + 1} has no waveParent assigned!");
        }

        // Start monitoring this wave
        if (!isCheckingWave)
        {
            StartCoroutine(MonitorCurrentWave());
        }
    }

    private void CollectWaveEnemies(GameObject parent)
    {
        currentWaveEnemies.Clear();

        // Find all GameObjects with IHealthSystem (enemies) under this parent
        IHealthSystem[] healthSystems = parent.GetComponentsInChildren<IHealthSystem>();
        
        foreach (var healthSystem in healthSystems)
        {
            if (healthSystem is MonoBehaviour mb)
            {
                currentWaveEnemies.Add(mb.gameObject);
                Debug.Log($"👾 [WaveBasedProgression] Registered enemy: {mb.gameObject.name}");
            }
        }

        Debug.Log($"👥 [WaveBasedProgression] Wave {currentWaveIndex + 1} has {currentWaveEnemies.Count} enemies");
    }

    private IEnumerator MonitorCurrentWave()
    {
        isCheckingWave = true;
        var wait = new WaitForSeconds(checkInterval);

        while (!wavesComplete)
        {
            // Remove destroyed or defeated enemies
            currentWaveEnemies.RemoveAll(e => e == null || IsEnemyDefeated(e));

            // Check if wave is cleared
            if (currentWaveEnemies.Count == 0 && currentWaveIndex < enemyWaves.Length)
            {
                Debug.Log($"✅ [WaveBasedProgression] Wave {currentWaveIndex + 1} cleared!");
                
                // Mark objective complete
                ObjectiveManager.Instance.CompleteObjective();

                // Move to next wave
                int nextWave = currentWaveIndex + 1;
                if (nextWave < enemyWaves.Length)
                {
                    yield return StartCoroutine(StartWave(nextWave));
                }
                else
                {
                    // All waves complete
                    OnAllWavesComplete();
                    yield break;
                }
            }

            yield return wait;
        }

        isCheckingWave = false;
    }

    private bool IsEnemyDefeated(GameObject enemy)
    {
        if (enemy == null) return true;

        var healthSystem = enemy.GetComponent<IHealthSystem>();
        if (healthSystem == null) return true;

        return healthSystem.currentHP <= 0f;
    }

    private void OnAllWavesComplete()
    {
        if (wavesComplete) return;
        
        wavesComplete = true;
        Debug.Log("🎉 [WaveBasedProgression] All waves complete!");

        // Clear and hide notice, update objective
        ObjectiveManager.Instance.UpdateNotice("", false);
        ObjectiveManager.Instance.HideNotice(); // Hide the notice GameObject
        ObjectiveManager.Instance.UpdateObjective("Proceed Forward", true);

        // Open elevator/door
        if (elevatorWallClosed != null) elevatorWallClosed.SetActive(false);
        if (elevatorWallOpen != null) elevatorWallOpen.SetActive(true);

        // Load next scene after delay
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneLoad);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Prevent loading the currently active scene
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (nextSceneName == activeSceneName)
            {
                Debug.LogWarning($"[WaveBasedProgression] Cannot load '{nextSceneName}' - it's the currently active scene!");
                yield break;
            }
            
            // Check if scene is already loaded
            if (IsSceneLoaded(nextSceneName))
            {
                Debug.LogWarning($"[WaveBasedProgression] Scene '{nextSceneName}' already loaded, skipping.");
                yield break;
            }

            Debug.Log($"[WaveBasedProgression] Loading scene '{nextSceneName}'...");
            
            if (loadSceneAdditively)
            {
                SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
            }
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }

    // Public methods for manual control
    public void ForceNextWave()
    {
        if (currentWaveIndex < enemyWaves.Length - 1)
        {
            currentWaveEnemies.Clear();
            StartCoroutine(StartWave(currentWaveIndex + 1));
        }
    }

    public int GetCurrentWave() => currentWaveIndex + 1;
    public int GetTotalWaves() => enemyWaves.Length;
}
