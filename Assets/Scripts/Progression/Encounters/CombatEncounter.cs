using Behaviors;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Progression.Encounters
{
    public class CombatEncounter : BasicEncounter
    {
        private class Wave
        {
            // Holds all the enemy game objects in this wave, and whether they are alive
            private Dictionary<GameObject, bool> enemies = new Dictionary<GameObject, bool>();

            // class constructor
            public Wave(List<GameObject> _enemies)
            {
                foreach (var enemy in _enemies)
                {
                    // verifies that the gameobject is an enemy
                    /*
                    if (enemy.GetComponent<BaseEnemy< , >> != null)
                    {

                    }
                    */

                    enemies.Add(enemy, true);
                }
            }

            /// <summary>
            /// Function to spawn all enemies in this wave
            /// Currently only activates the gameobjects but eventially should trigger spawn behavior in base enemy script
            /// </summary>
            public void SpawnEnemies()
            {
                foreach (KeyValuePair<GameObject, bool> pair in enemies)
                {
                    pair.Key.SetActive(true);
                }
            }

            /// <summary>
            /// Function to reset all enemies in this wave
            /// Currently only deactivates the gameobjects but eventially should trigger reset behavior in base enemy script
            /// </summary>
            public void ResetEnemies()
            {
                foreach (KeyValuePair<GameObject, bool> pair in enemies)
                {
                    pair.Key.SetActive(false);
                }
            }

            /// <summary>
            /// Handles the logic for when an enemy is defeated
            /// Function should be subscribed to the enemy's OnDefeated event
            /// </summary>
            /// <param name="enemy"></param>
            private void OnEnemyDefeated(GameObject enemy)
            {
                if (enemies.ContainsKey(enemy))
                {
                    enemies[enemy] = false;
                    // check if all enemies are defeated
                    if (AreAllEnemiesDefeated())
                    {
                        // trigger next wave or end encounter
                    }
                }

                bool AreAllEnemiesDefeated()
                {
                    foreach (var status in enemies.Values)
                        if (status) // if any enemy is still alive
                            return false;
                    return true;
                }
            }
        }

        protected override Color DebugColor { get => Color.red; }

        // the full list of each wave in the encounter
        private List<Wave> waves = new();

        protected override void SetupEncounter()
        {
            // iterates through each child object under this encounter
            foreach(Transform child in transform)
            {
                Wave newWave = CreateWave(child);

                newWave.ResetEnemies();

                waves.Add(newWave);
            }

            // sub function to create a new wave of enemies using all the gameobjects childed to an empty gameobject
            Wave CreateWave(Transform parentObject)
            {
                List<GameObject> enemiesToAdd = new List<GameObject>();
                foreach (Transform waveChild in parentObject)
                    enemiesToAdd.Add(waveChild.gameObject);

                return new Wave(enemiesToAdd);
            }
        }

        #region Trigger Events
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            // enable all enemies
            waves[0].SpawnEnemies();
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);

            // reset enemies
            foreach (var wave in waves)
            {
                wave.ResetEnemies();
            }
        }
        #endregion


    }
}

