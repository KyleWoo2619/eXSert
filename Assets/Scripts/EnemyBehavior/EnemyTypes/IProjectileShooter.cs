using UnityEngine;
public interface IProjectileShooter
{
    GameObject ProjectilePrefab { get; }
    float ProjectileSpeed { get; }
    Transform FirePoint { get; }
    void FireProjectile(Transform target);
}
