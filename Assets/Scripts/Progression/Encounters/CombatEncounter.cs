using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Animations;
using UnityEngine;

namespace Progression.Encounters
{
    public class CombatEncounter : BasicEncounter
    {
        private class Wave
        {
            private Dictionary<GameObject, bool> enemies = new Dictionary<GameObject, bool>();

            // class constructor
            public Wave(List<GameObject> _enemies)
            {
                foreach (var enemy in _enemies)
                {
                    enemies.Add(enemy, true);
                }
            }

            public void SpawnEnemies()
            {
                foreach (KeyValuePair<GameObject, bool> pair in enemies)
                {
                    pair.Key.SetActive(true);
                }
            }

            public void ResetEnemies()
            {
                foreach (KeyValuePair<GameObject, bool> pair in enemies)
                {
                    pair.Key.SetActive(false);
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

