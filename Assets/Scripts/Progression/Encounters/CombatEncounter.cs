using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Progression.Encounters
{
    /// <summary>
    /// Subclass Wave to hold the data for the individual waves for encounters.
    /// Additionally holds wave specific functionality.
    /// </summary>
    internal class Wave
    {
        private readonly GameObject waveRoot;
        // Holds all the enemy game objects in this wave, and whether they are alive
        internal List<BaseEnemyCore> enemies = new List<BaseEnemyCore>();
        private readonly HashSet<BaseEnemyCore> defeatedEnemies = new HashSet<BaseEnemyCore>();
        private int remainingEnemies;

        public event Action<Wave> OnWaveComplete;

        // class constructor
        public Wave(GameObject _waveRoot, List<GameObject> _enemies)
        {
            waveRoot = _waveRoot;
            foreach (var enemy in _enemies)
            {
                BaseEnemyCore enemyCore = enemy.GetComponent<BaseEnemyCore>();
                if (enemyCore != null)
                {
                    enemies.Add(enemyCore);

                    enemyCore.OnDeath += OnEnemyDefeated;
                }
                else
                    Debug.LogWarning($"[CombatEncounter] Detected nonenemy gameobject {enemy.name} attached to encounter. Skipping object");
            }

            remainingEnemies = enemies.Count;
        }

        /// <summary>
        /// Function to spawn all enemies in this wave
        /// Currently only activates the gameobjects but eventially should trigger spawn behavior in base enemy script
        /// </summary>
        public void SpawnEnemies()
        {
            if (waveRoot != null && !waveRoot.activeSelf)
                waveRoot.SetActive(true);

            defeatedEnemies.Clear();
            remainingEnemies = enemies.Count;

            foreach (BaseEnemyCore enemy in enemies)
            {
                enemy.Spawn();
            }
        }

        /// <summary>
        /// Function to reset all enemies in this wave
        /// Currently only deactivates the gameobjects but eventially should trigger reset behavior in base enemy script
        /// </summary>
        public void ResetEnemies()
        {
            if (waveRoot != null && waveRoot.activeSelf)
                waveRoot.SetActive(false);

            defeatedEnemies.Clear();
            remainingEnemies = enemies.Count;

            foreach (BaseEnemyCore enemy in enemies)
            {
                enemy.ResetEnemy();
            }
        }

        /// <summary>
        /// Handles the logic for when an enemy is defeated
        /// Function should be subscribed to the enemy's OnDefeated event
        /// </summary>
        /// <param name="enemy"></param>
        private void OnEnemyDefeated(BaseEnemyCore enemy)
        {
            if (!enemies.Contains(enemy))
                return;

            if (!defeatedEnemies.Add(enemy))
                return;

            remainingEnemies = Mathf.Max(0, remainingEnemies - 1);
            if (remainingEnemies == 0)
                OnWaveComplete?.Invoke(this); // trigger next wave or end encounter
        }
    }

    public class CombatEncounter : BasicEncounter
    {
        [Header("Combat Encounter Settings")]

        [SerializeField] private bool tempIsCompleted;

        [Header("Timing")]
        [SerializeField, Tooltip("Seconds to wait before advancing to the next wave.")]
        private float nextWaveDelaySeconds = 0.15f;

        [Header("Progression")]
        [Tooltip("Enemies for this encounter. If empty and Auto Find is on, we'll find by tag.")]
        [SerializeField] private List<GameObject> encounterEnemies = new List<GameObject>();
        [SerializeField] private bool autoFindByTag = false;
        [SerializeField] private string enemyTag = "Enemy";


        [SerializeField] private bool dropObjectOnClear = false;
        [SerializeField] private GameObject objectToDrop;
        [SerializeField] private bool dropAtLastEnemyPosition = true;

        [SerializeField] private bool loadSceneOnClear = false;
        [SerializeField] private string nextSceneName = string.Empty;
        [SerializeField] private bool loadAdditive = true;
        private static readonly HashSet<string> loadedScenes = new HashSet<string>();
        private GameObject lastEnemyAlive;

        public override bool isCompleted
        {
            get
            {
                return tempIsCompleted;
            }
        }

        protected override Color DebugColor { get => Color.red; }

        /// <summary>
        /// The entire list of each wave. All waves persist even once they are compleated
        /// </summary>
        private readonly List<Wave> allWaves = new();

        /// <summary>
        /// The queue of the incoming waves. Waves are removed once they are compleated
        /// </summary>
        private readonly Queue<Wave> wavesQueue = new();

        private bool encounterStarted;
        private Coroutine waveAdvanceRoutine;

        protected override void Start()
        {
            base.Start();

            if (IsPlayerInsideZone())
                BeginEncounter();
        }

        protected override void SetupEncounter()
        {
            ResolveEncounterEnemies();

            // iterates through each child object under this encounter
            foreach(Transform child in transform)
            {
                Wave newWave = CreateWave(child);

                newWave.OnWaveComplete += WaveComplete;

                allWaves.Add(newWave);
            }

            ResetWaves();

            SyncNextEncounterDelay();

            // sub function to create a new wave of enemies using all the gameobjects childed to an empty gameobject
            Wave CreateWave(Transform parentObject)
            {
                List<GameObject> enemiesToAdd = new List<GameObject>();
                foreach (Transform waveChild in parentObject)
                    enemiesToAdd.Add(waveChild.gameObject);

                return new Wave(parentObject.gameObject, enemiesToAdd);
            }
        }

        private void WaveComplete(Wave compleatedWave)
        {
            if (wavesQueue.Count == 0)
                return;

            if (wavesQueue.Peek() == compleatedWave)
            {
                compleatedWave.OnWaveComplete -= WaveComplete;
                wavesQueue.Dequeue();

                if (wavesQueue.Count == 0)
                {
                    tempIsCompleted = true;
                    HandleCompletionProgression();
                    HandleEncounterCompleted();
                    return;
                }

                if (waveAdvanceRoutine != null)
                {
                    StopCoroutine(waveAdvanceRoutine);
                    waveAdvanceRoutine = null;
                }

                waveAdvanceRoutine = StartCoroutine(SpawnNextWaveAfterDelay());
            }
        }

        private System.Collections.IEnumerator SpawnNextWaveAfterDelay()
        {
            if (nextWaveDelaySeconds > 0f)
                yield return new WaitForSeconds(nextWaveDelaySeconds);

            SpawnNextWave();
            waveAdvanceRoutine = null;
        }

        private void SpawnNextWave()
        {
            if (wavesQueue.Count == 0)
                return;

            Wave currentWave = wavesQueue.Peek();
            DeactivateOtherWaves(currentWave);
            currentWave.SpawnEnemies();
        }

        private void ResetWaves()
        {
            wavesQueue.Clear();
            encounterStarted = false;

            if (waveAdvanceRoutine != null)
            {
                StopCoroutine(waveAdvanceRoutine);
                waveAdvanceRoutine = null;
            }

            foreach (Wave wave in allWaves)
            {
                wavesQueue.Enqueue(wave);
                wave.ResetEnemies();
            }
        }

        #region Trigger Events
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            BeginEncounter();
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);

            ResetWaves();

            // reset enemies
            foreach (var wave in allWaves)
                wave.ResetEnemies();
        }
        #endregion

        private void BeginEncounter()
        {
            if (encounterStarted)
                return;

            encounterStarted = true;
            SpawnNextWave();
        }

        private void DeactivateOtherWaves(Wave activeWave)
        {
            foreach (var wave in allWaves)
            {
                if (wave != activeWave)
                    wave.ResetEnemies();
            }
        }

        private void ResolveEncounterEnemies()
        {
            if ((encounterEnemies == null || encounterEnemies.Count == 0) && autoFindByTag)
            {
                encounterEnemies = GameObject.FindGameObjectsWithTag(enemyTag).ToList();
            }

            if (encounterEnemies != null)
                encounterEnemies.RemoveAll(e => e == null);

        }

        private void HandleCompletionProgression()
        {
            CacheLastEnemyAlive();

            if (dropObjectOnClear && objectToDrop != null)
            {
                Vector3 dropPosition = objectToDrop.transform.position;
                if (dropAtLastEnemyPosition && lastEnemyAlive != null)
                    dropPosition = lastEnemyAlive.transform.position + Vector3.forward;

                objectToDrop.transform.position = dropPosition;
                objectToDrop.SetActive(true);
            }

            if (loadSceneOnClear && !string.IsNullOrWhiteSpace(nextSceneName))
            {
                string activeSceneName = SceneManager.GetActiveScene().name;
                if (nextSceneName == activeSceneName)
                {
                    Debug.LogError($"[CombatEncounter] Cannot load '{nextSceneName}' because it is the active scene.");
                    return;
                }

                if (IsSceneLoaded(nextSceneName))
                {
                    Debug.LogWarning($"[CombatEncounter] Scene '{nextSceneName}' is already loaded. Skipping load.");
                    return;
                }

                if (loadedScenes.Contains(nextSceneName))
                {
                    Debug.LogWarning($"[CombatEncounter] Scene '{nextSceneName}' is already being loaded. Skipping duplicate load.");
                    return;
                }

                loadedScenes.Add(nextSceneName);

                if (loadAdditive)
                {
                    Debug.Log($"[CombatEncounter] Loading scene '{nextSceneName}' additively.");
                    SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
                }
                else
                {
                    Debug.Log($"[CombatEncounter] Loading scene '{nextSceneName}' as single.");
                    SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
                }
            }
        }

        private void CacheLastEnemyAlive()
        {
            lastEnemyAlive = null;
            if (TryCacheFromList(encounterEnemies))
                return;

            for (int i = allWaves.Count - 1; i >= 0; i--)
            {
                var wave = allWaves[i];
                for (int j = wave.enemies.Count - 1; j >= 0; j--)
                {
                    var enemy = wave.enemies[j];
                    if (enemy != null)
                    {
                        lastEnemyAlive = enemy.gameObject;
                        return;
                    }
                }
            }
        }

        private bool TryCacheFromList(List<GameObject> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return false;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                var candidate = candidates[i];
                if (candidate != null)
                {
                    lastEnemyAlive = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                    return true;
            }
            return false;
        }

        private void SyncNextEncounterDelay()
        {
            SetEnableNextEncounterDelaySeconds(nextWaveDelaySeconds);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncNextEncounterDelay();
        }

        [UnityEditor.MenuItem("GameObject/eXSert/Add Selected Enemies to Combat Encounter", false, 0)]
        private static void AddSelectedEnemiesToEncounter()
        {
            var encounter = FindFirstObjectByType<CombatEncounter>();
            if (encounter == null)
            {
                Debug.LogWarning("No CombatEncounter found in scene!");
                return;
            }

            var selectedObjects = UnityEditor.Selection.gameObjects;
            var enemiesAdded = 0;

            foreach (var obj in selectedObjects)
            {
                if (obj.GetComponentInChildren<IHealthSystem>() != null || obj.CompareTag("Enemy"))
                {
                    if (!encounter.encounterEnemies.Contains(obj))
                    {
                        encounter.encounterEnemies.Add(obj);
                        enemiesAdded++;
                    }
                }
            }

            if (enemiesAdded > 0)
            {
                UnityEditor.EditorUtility.SetDirty(encounter);
                Debug.Log($"Added {enemiesAdded} enemies to CombatEncounter list.");
            }
            else
            {
                Debug.Log("No valid enemies found in selection.");
            }
        }
#endif
    }
}

