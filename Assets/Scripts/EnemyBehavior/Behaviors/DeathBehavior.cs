using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class DeathBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine deathSequenceCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;
        
        // Death sound configuration (can be overridden per enemy)
        private AudioClip deathSound;
        private float deathSoundVolume = 0.8f;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            
            Debug.Log($"💀 DeathBehavior.OnEnter called for {enemy.gameObject.name}");

            // Disable movement and other components
            if (enemy.agent != null)
                enemy.agent.enabled = false;

            // Optionally set a "dead" color or visual
            enemy.SetEnemyColor(Color.black);
            
            // Try to get death sound from enemy if it has a DeathSoundConfig component
            var soundConfig = enemy.GetComponent<EnemyDeathSoundConfig>();
            if (soundConfig != null)
            {
                deathSound = soundConfig.deathSound;
                deathSoundVolume = soundConfig.volume;
                Debug.Log($"💀 Found death sound config: {(deathSound != null ? deathSound.name : "NULL")}");
            }

            // Start the death sequence coroutine
            if (deathSequenceCoroutine != null)
                enemy.StopCoroutine(deathSequenceCoroutine);
            deathSequenceCoroutine = enemy.StartCoroutine(DeathSequence());
        }

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (deathSequenceCoroutine != null)
            {
                enemy.StopCoroutine(deathSequenceCoroutine);
                deathSequenceCoroutine = null;
            }
        }

        private IEnumerator DeathSequence()
        {
            Debug.Log($"💀 {enemy.gameObject.name} starting death sequence");
            
            // Play death SFX immediately
            PlayDeathSFX();
            
            // Wait a moment for death animation/effects
            yield return new WaitForSeconds(0.5f);

            // Destroy health bar if it exists
            if (enemy.healthBarInstance != null)
            {
                Object.Destroy(enemy.healthBarInstance.gameObject);
                enemy.healthBarInstance = null;
            }

            // Only remove from pocket if this is a crawler
            if (enemy is BaseCrawlerEnemy crawler && crawler.Pocket != null)
            {
                crawler.Pocket.activeEnemies.Remove(crawler);
            }

            // Wait a bit longer before destroying (let sound finish)
            yield return new WaitForSeconds(1.5f);

            Debug.Log($"💀 {enemy.gameObject.name} destroying GameObject");
            Object.Destroy(enemy.gameObject);
        }

        private void PlayDeathSFX()
        {
            Debug.Log($"🔊 PlayDeathSFX called for {enemy.gameObject.name}");
            
            // Try multiple methods to play sound
            
            // Method 1: Use assigned death sound with SoundManager
            if (deathSound != null && SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
            {
                Debug.Log($"🔊 Playing death sound '{deathSound.name}' through SoundManager");
                SoundManager.Instance.sfxSource.PlayOneShot(deathSound, deathSoundVolume);
                return;
            }
            
            // Method 2: Check for AudioSource on the enemy itself
            var audioSource = enemy.GetComponent<AudioSource>();
            if (audioSource != null && deathSound != null)
            {
                Debug.Log($"🔊 Playing death sound '{deathSound.name}' through enemy AudioSource");
                audioSource.PlayOneShot(deathSound, deathSoundVolume);
                return;
            }
            
            // Method 3: Check for PlaySoundOnEvent component
            var soundPlayer = enemy.GetComponent<PlaySoundOnEvent>();
            if (soundPlayer != null)
            {
                Debug.Log($"🔊 Playing death sound through PlaySoundOnEvent component");
                soundPlayer.PlaySound();
                return;
            }
            
            Debug.LogWarning($"🔊 {enemy.gameObject.name} has no death sound configured! Add EnemyDeathSoundConfig component or assign sound manually.");
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}