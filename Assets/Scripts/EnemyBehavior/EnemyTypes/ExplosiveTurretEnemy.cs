// ExplosiveTurretEnemy.cs
// Purpose: Turret enemy that fires explosive projectiles and manages firing behavior.
// Works with: ExplosiveEnemyProjectile, BaseTurretEnemy, Pathfinding optional.

using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ExplosiveTurretEnemy : BaseTurretEnemy
{
    [Header("Explosive Turret")]
    [Tooltip("Override fire cooldown for explosive turret (slower than standard).")]
    [SerializeField] private float explosiveFireCooldown = 2.5f;

    [Tooltip("Explosive projectile prefab (should have ExplosiveEnemyProjectile).")]
    [SerializeField] private GameObject explosiveProjectilePrefab;

    [Tooltip("Projectile speed for explosive rounds (often slower).")]
    [SerializeField] private float explosiveProjectileSpeed = 18f;

    protected override void Awake()
    {
        base.Awake();

        // Swap to explosive projectile and slower fire rate
        if (explosiveProjectilePrefab != null)
        {
            var projField = typeof(BaseTurretEnemy).GetField("projectilePrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (projField != null) projField.SetValue(this, explosiveProjectilePrefab);
        }

        var cdField = typeof(BaseTurretEnemy).GetField("fireCooldown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (cdField != null) cdField.SetValue(this, explosiveFireCooldown);

        var spdField = typeof(BaseTurretEnemy).GetField("projectileSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (spdField != null) spdField.SetValue(this, explosiveProjectileSpeed);
    }
}