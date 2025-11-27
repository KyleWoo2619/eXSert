// EnemyProjectile.cs
// Purpose: Base projectile logic for enemy-fired projectiles, handles movement and collision damage.
// Works with: Turret weapons, ExplosiveEnemyProjectile, player hit detection.

using UnityEngine;
using System.Collections;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f; // seconds
    [SerializeField] private float damage = 10f;  // default damage, can be set on spawn

    // Optional: owner for drone-side pooling
    private DroneEnemy owner;

    private Coroutine lifeRoutine;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Start lifetime timer
        lifeRoutine = StartCoroutine(DeactivateAfterLifetime());
    }

    private void OnDisable()
    {
        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        // Reset physics for pooling reuse
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator DeactivateAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        DeactivateToPool();
    }

    // Handle physics collisions
    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    // Handle trigger hits too (in case player's collider is trigger)
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider col)
    {
        // Only react to the player for now; other collisions are ignored (lifetime handles cleanup)
        if (col != null && col.CompareTag("Player"))
        {
            Debug.Log($"[EnemyProjectile] Player hit by projectile: {name}");
            DeactivateToPool();
        }
    }

    private void DeactivateToPool()
    {
        // Prefer turret pooling helper if present
        var pooled = GetComponent<TurretPooledProjectile>();
        if (pooled != null)
        {
            pooled.ReparentToPoolAndDeactivate();
            return;
        }

        // Fallback: drone-side pooling
        if (owner != null)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            owner.ReturnProjectileToPool(gameObject);
            return;
        }

        // Last resort: just deactivate
        gameObject.SetActive(false);
    }

    public void SetDamage(float dmg) => damage = dmg;

    // For drone pooling
    public void SetOwner(DroneEnemy drone) => owner = drone;
}