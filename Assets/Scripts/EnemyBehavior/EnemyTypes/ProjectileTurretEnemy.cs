using System.Collections;
using UnityEngine;

// [RequireComponent(typeof(MeshRenderer))]
public class ProjectileTurretEnemy : BaseTurretEnemy
{
    [Header("Projectile Turret")]
    [Tooltip("Override default fire cooldown for the standard projectile turret.")]
    [SerializeField] private float overrideFireCooldown = -1f;

    [Header("Burst Fire Settings")]
    [Tooltip("Seconds spent actively firing per cycle.")]
    [SerializeField] private float burstDuration = 4f;
    [Tooltip("Seconds spent reloading between bursts.")]
    [SerializeField] private float reloadDuration = 8f;
    [Tooltip("Delay between individual shots while bursting.")]
    [SerializeField] private float burstShotInterval = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        // Optionally override fire rate in inspector for this turret type
        if (overrideFireCooldown > 0f)
        {
            // Use reflection-safe pattern: fireCooldown is protected in base
            var field = typeof(BaseTurretEnemy).GetField("fireCooldown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(this, overrideFireCooldown);
        }
    }

    protected override IEnumerator GetAttackLoopRoutine()
    {
        return BurstFireLoop();
    }

    protected override void OnProjectileFired()
    {
        // Attack animation is played per burst instead of per projectile.
    }

    private IEnumerator BurstFireLoop()
    {
        WaitForSeconds shotDelay = WaitForSecondsCache.Get(Mathf.Max(0.01f, burstShotInterval));
        WaitForSeconds reloadDelay = WaitForSecondsCache.Get(Mathf.Max(0f, reloadDuration));

        while (enemyAI != null && enemyAI.State.Equals(EnemyState.Attack))
        {
            PlayAttackAnim();
            yield return FireBurst(shotDelay);

            if (!enemyAI.State.Equals(EnemyState.Attack))
                yield break;

            RequestIdlePose();
            yield return reloadDelay;
        }
    }

    private IEnumerator FireBurst(WaitForSeconds shotDelay)
    {
        float endTime = Time.time + Mathf.Max(0f, burstDuration);

        while (Time.time < endTime)
        {
            if (enemyAI == null || !enemyAI.State.Equals(EnemyState.Attack))
                yield break;

            if (player == null)
            {
                // Use PlayerPresenceManager if available
                if (PlayerPresenceManager.IsPlayerPresent)
                {
                    player = PlayerPresenceManager.PlayerTransform;
                }
                else
                {
                    var found = GameObject.FindGameObjectWithTag("Player");
                    player = found != null ? found.transform : null;
                }
                PlayerTarget = player;
            }

            if (player != null)
            {
                AimAtTarget(player);
                FireProjectile(player);
            }

            yield return shotDelay;
        }
    }
}