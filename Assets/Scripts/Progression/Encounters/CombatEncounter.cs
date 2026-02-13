using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly List<GameObject> enemyGameObjects = new();
        private readonly List<BaseEnemyCore> enemies = new();

        private bool waveCompleted = false;

        public event Action<Wave> OnWaveComplete;
        public event Action<Vector3> UpdateLastEnemyPosition;

        // class constructor
        public Wave(GameObject _waveRoot, List<GameObject> _enemies)
        {
            waveRoot = _waveRoot;
            enemyGameObjects = _enemies;
            waveCompleted = false;

            InitializeEnemies();
        }

        private void InitializeEnemies()
        {
            enemies.Clear();
            foreach (var enemy in enemyGameObjects)
                if (enemy.TryGetComponent<BaseEnemyCore>(out var enemyCore))
                {
                    enemies.Add(enemyCore);
                    enemyCore.OnDeath -= OnEnemyDefeated; // Prevent double-subscription
                    enemyCore.OnDeath += OnEnemyDefeated;
                }
        }

        public override string ToString()
        {
            return $"{waveRoot.name} with {enemies.Count} enemies";
        }

        public void Cleanup()
        {
            UnsubscribeAllEnemies();
        }

        private void UnsubscribeAllEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    enemy.OnDeath -= OnEnemyDefeated;
            }
        }

        /// <summary>
        /// Function to spawn all enemies in this wave
        /// Currently only activates the gameobjects but eventially should trigger spawn behavior in base enemy script
        /// </summary>
        public void SpawnEnemies()
        {
            if (waveRoot != null && !waveRoot.activeSelf)
                waveRoot.SetActive(true);

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

            foreach (BaseEnemyCore enemy in enemies)
            {
                enemy.ResetEnemy();
            }

            waveCompleted = false;
        }

        /// <summary>
        /// Handles the logic for when an enemy is defeated
        /// Function should be subscribed to the enemy's OnDefeated event
        /// </summary>
        /// <param name="enemy"></param>
        private void OnEnemyDefeated(BaseEnemyCore enemy)
        {
            Debug.Log($"[CombatEncounter] Enemy defeated: {enemy.name}");

#if UNITY_EDITOR
            // Short diagnostic log to show who invoked the event (stack trace)
            Debug.Log($"[CombatEncounter] OnEnemyDefeated invoked from:\n{System.Environment.StackTrace}");
#endif

            if (!enemies.Contains(enemy))
                return;

            UpdateLastEnemyPosition?.Invoke(enemy.transform.position);

            enemy.OnDeath -= OnEnemyDefeated; // Unsubscribe from the enemy's death event to prevent memory leaks
            enemies.Remove(enemy);

            if (!RemainingEnemiesCheck() && !waveCompleted)
                OnWaveComplete?.Invoke(this); // trigger next wave or end encounter
        }

        private bool RemainingEnemiesCheck()
        {
            if (enemies == null || enemies.Count == 0)
                return false;

            foreach (var enemy in enemies)
                if (enemy != null && enemy.isAlive)
                    return true;

            return false;
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
        [SerializeField] private bool autoFindByTag = false;
        [SerializeField] private string enemyTag = "Enemy";


        [SerializeField] private bool dropObjectOnClear = false;
        [SerializeField] private GameObject objectToDrop;
        private bool dropAtLastEnemyPosition = true;

        private Vector3 lastEnemyPosition;

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

        protected override void SetupEncounter()
        {
            // iterates through each child object under this encounter
            foreach(Transform child in transform)
            {
                // if the child object doesn't have "wave" in the name, skip it.
                // This allows for organization of the encounter gameobject without breaking functionality
                if (!child.name.ToLower().Contains("wave")) continue;

                // Create a new wave using the child object as the parent for the wave's enemies
                Wave newWave = CreateWave(child);

                newWave.OnWaveComplete += WaveComplete;
                newWave.UpdateLastEnemyPosition += (position) => lastEnemyPosition = position;

                allWaves.Add(newWave);
            }

            ResetWaves();

            // SyncNextEncounterDelay();

            // sub function to create a new wave of enemies using all the gameobjects childed to an empty gameobject
            static Wave CreateWave(Transform parentObject)
            {
                List<GameObject> enemiesToAdd = new List<GameObject>();
                foreach (Transform waveChild in parentObject)
                    enemiesToAdd.Add(waveChild.gameObject);

                return new Wave(parentObject.gameObject, enemiesToAdd);
            }
        }

        private void BeginEncounter()
        {
            if (encounterStarted)
                return;

            encounterStarted = true;
            SpawnNextWave();
        }

        private void CompleteEncounter()
        {
            Debug.Log($"[CombatEncounter] Encounter completed: {name}");

            DropItem();
        }

        private void DropItem()
        {
            if (!dropObjectOnClear || objectToDrop == null)
                return;

            Vector3 dropPosition = objectToDrop.transform.position;
            if (dropAtLastEnemyPosition && lastEnemyPosition != null)
                dropPosition = lastEnemyPosition + Vector3.forward;

            Debug.Log($"[CombatEncounter] Dropping object {objectToDrop.name} at position {dropPosition}");

            objectToDrop.transform.position = dropPosition;
            objectToDrop.SetActive(true);
        }

        #region Wave Manipulation Functions
        private void WaveComplete(Wave completedWave)
        {
            Debug.Log($"[CombatEncounter] Wave completed: {completedWave}");

            if (wavesQueue.Peek() != completedWave) return;

            CleanupWave(completedWave);

            wavesQueue.Dequeue();

            // Check if there are more waves to spawn
            if (wavesQueue.Count != 0) SpawnNextWave(3f);
            else CompleteEncounter();
        }

        private void CleanupWave(Wave wave)
        {
            wave.Cleanup();
            wave.UpdateLastEnemyPosition -= (position) => lastEnemyPosition = position;
            wave.OnWaveComplete -= WaveComplete;
        }

        private void SpawnNextWave()
        {
            // Ejects from the function early if there are no more waves to spawn
            if (wavesQueue.Count == 0) return;

            Wave currentWave = wavesQueue.Peek();
            Debug.Log($"[CombatEncounter] Spawning next wave: {currentWave}");
            currentWave.SpawnEnemies();
        }

        private async void SpawnNextWave(float delay)
        {
            if (delay > 0f)
            {
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            SpawnNextWave();
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
        #endregion

        #region Trigger Events
        protected override void PlayerEnteredZone()
        {
            BeginEncounter();
        }
        #endregion
    }
}