// IProjectileShooter.cs
// Purpose: Interface for entities that can shoot projectiles (turrets, enemies).
// Works with: Turret controllers, Projectile pooling systems.

using UnityEngine;

public interface IProjectileShooter
{
    GameObject ProjectilePrefab { get; }
    float ProjectileSpeed { get; }
    Transform FirePoint { get; }
    void FireProjectile(Transform target);
}
