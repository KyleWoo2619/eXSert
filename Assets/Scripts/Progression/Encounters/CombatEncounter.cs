using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Progression.Encounters
{
    /// <summary>
    /// Subclass Wave to hold the data for the individual waves for encounters.
    /// Additionally holds wave specific functionality.
    /// </summary>
    internal class Wave
    {
        // Holds all the enemy game objects in this wave, and whether they are alive
        internal List<BaseEnemyCore> enemies = new List<BaseEnemyCore>();

        public event Action<Wave> OnWaveComplete;

        // class constructor
        public Wave(List<GameObject> _enemies)
        {
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
        }

        /// <summary>
        /// Function to spawn all enemies in this wave
        /// Currently only activates the gameobjects but eventially should trigger spawn behavior in base enemy script
        /// </summary>
        public void SpawnEnemies()
        {
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
            if (enemies.Contains(enemy) && AreAllEnemiesDefeated()) // check if all enemies are defeated
                OnWaveComplete?.Invoke(this); // trigger next wave or end encounter

            bool AreAllEnemiesDefeated()
            {
                foreach (var enemy in enemies)
                    if (enemy.isAlive) // if any enemy is still alive
                        return false;
                return true;
            }
        }

        public void CleanUp()
        {
            foreach (var enemy in enemies)
                enemy.OnDeath -= OnEnemyDefeated;

            // removes all the enemies from the wave and clears the list to free up memory
            enemies.Clear();
        }
    }

    public class CombatEncounter : BasicEncounter
    {
        public override bool isCompleted => wavesQueue.Count == 0;

        protected override Color DebugColor { get => Color.red; }

        /// <summary>
        /// The entire list of each wave. All waves persist even once they are compleated
        /// </summary>
        private List<Wave> allWaves = new();

        /// <summary>
        /// The queue of the incoming waves. Waves are removed once they are compleated
        /// </summary>
        private Queue<Wave> wavesQueue = new();

        protected override void SetupEncounter()
        {
            // iterates through each child object under this encounter
            foreach(Transform child in transform)
            {
                Wave newWave = CreateWave(child);

                newWave.OnWaveComplete += WaveComplete;

                // adds the new wave to the list of all waves
                allWaves.Add(newWave);
            }

            ResetWaves();

            // sub function to create a new wave of enemies using all the gameobjects childed to an empty gameobject
            static Wave CreateWave(Transform parentObject)
            {
                List<GameObject> enemiesToAdd = new List<GameObject>();
                foreach (Transform waveChild in parentObject)
                    enemiesToAdd.Add(waveChild.gameObject);

                return new Wave(enemiesToAdd);
            }
        }

        protected override void CleanupEncounter()
        {
            foreach (var wave in allWaves)
            {
                wave.OnWaveComplete -= WaveComplete;
                wave.CleanUp();
            }
            allWaves.Clear();
            wavesQueue.Clear();

            base.CleanupEncounter();
        }

        private void WaveComplete(Wave compleatedWave)
        {
            // ensures that the wave that triggered the event is the current wave.
            if (wavesQueue.Peek() != compleatedWave)
                return;

            compleatedWave.OnWaveComplete -= WaveComplete;
            wavesQueue.Dequeue();

            // check if there are any more waves to spawn, if not the encounter is compleated
            if (wavesQueue.Count == 0)
            {
                Debug.Log($"Combat Encounter {gameObject.name} Completed!");
                return;
            }

            SpawnNextWave();
        }

        private void SpawnNextWave()
        {
            wavesQueue.Peek().SpawnEnemies();
        }

        private void ResetWaves()
        {
            wavesQueue.Clear();

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

            SpawnNextWave();
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
    }
}

