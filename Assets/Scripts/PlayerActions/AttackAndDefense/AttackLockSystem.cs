/*
 * Written by Will Thomsen
 * 
 * this script is designed to check if the player is facing an enemy when attacking
 * if the player is facing an enemy, the player will move towards the enmemy and line up the attack for the player
 */

using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using Utilities.Combat.Attacks;

public class AttackLockSystem : MonoBehaviour
{
    [SerializeField, Tooltip("Angle within which to lock on to an enemy")]
    private float lockOnAngle = 30f;

    [SerializeField, Tooltip("Maximum distance to search for enemies")]
    private float lockOnDistance = 5f;

    [SerializeField, CriticalReference, Tooltip("Reference to the player GameObject")]
    private GameObject player;

    private Transform playerTransform => player.transform;

    // Track running coroutine so we don't start multiple movements simultaneously
    private Coroutine lockOnCoroutine;

    // subscribe to the attack event
    private void OnEnable()
    {
        EnhancedPlayerAttackManager.OnAttack += LockOnAttack;
    }

    private void OnDisable()
    {
        EnhancedPlayerAttackManager.OnAttack -= LockOnAttack;
    }

    // Main function to lock on to an enemy when an attack is used
    private void LockOnAttack(PlayerAttack attackUsed)
    {
        // Only lock on for single target attacks
        if (attackUsed.attackType == AttackType.LightSingle || attackUsed.attackType == AttackType.HeavySingle)
        {
            Transform targettedEnemy = SearchForEnemy(); // Search for an enemy to lock on to

            if (targettedEnemy != null)
            {
                Vector3 direction = getDirectionFromEnemy(targettedEnemy.position);
                direction.y = 0f; // Keep direction horizontal

                // Determine desired distance from enemy for the attack
                float attackDistance = attackUsed.distanceFromPlayer;

                // Compute the final target position (keep player's current Y)
                Vector3 targetPosition = targettedEnemy.position - (direction * attackDistance);
                targetPosition.y = playerTransform.position.y;

                // Compute target rotation to face the enemy horizontally
                Quaternion targetRotation = playerTransform.rotation;
                if (direction != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(direction);
                }

                // Stop any existing movement coroutine and start a new one using attack's startLag
                if (lockOnCoroutine != null)
                {
                    StopCoroutine(lockOnCoroutine);
                    lockOnCoroutine = null;
                }

                float duration = attackUsed.startLag;
                lockOnCoroutine = StartCoroutine(MoveAndFaceCoroutine(targetPosition, targetRotation, duration));
            }
        }

        // ----------------------------------------

        // Searches for the closest enemy within lockOnDistance and lockOnAngle
        Transform SearchForEnemy()
        {
            // Find all colliders within range of lockOnDistance
            Collider[] hitColliders = Physics.OverlapSphere(playerTransform.position, lockOnDistance);
            Transform closestEnemy = null;

            // Determines which enemy the player is most directly facing
            // Will return null if no enemies are greater than lockOnAngle
            float closestAngle = lockOnAngle;
            foreach (var hitCollider in hitColliders)
            {
                // Check if the collider belongs to an enemy
                if (hitCollider.CompareTag("Enemy"))
                {
                    // Calculate angle to enemy
                    Vector3 directionToEnemy = getDirectionFromEnemy(hitCollider.transform.position);
                    float angleToEnemy = Vector3.Angle(playerTransform.forward, directionToEnemy);

                    // Check if this enemy is the closest within the lockOnAngle
                    if (angleToEnemy < closestAngle)
                    {
                        closestAngle = angleToEnemy;
                        closestEnemy = hitCollider.transform;
                    }
                }
            }

            return closestEnemy;
        }

        // Helper function to get horizontal direction from player to enemy
        Vector3 getDirectionFromEnemy(Vector3 enemyPosition)
        {
            // Calculate horizontal direction to enemy
            Vector3 direction = (enemyPosition - playerTransform.position).normalized;

            return direction;
        }
    }

    private IEnumerator MoveAndFaceCoroutine(Vector3 endPos, Quaternion targetRot, float duration)
    {
        // Instant snap if no time to move
        if (duration <= Mathf.Epsilon)
        {
            playerTransform.position = endPos;
            playerTransform.rotation = targetRot;
            lockOnCoroutine = null;
            yield break;
        }

        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth interpolation (can change to SmoothStep / easing if desired)
            playerTransform.position = Vector3.Lerp(startPos, endPos, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // Ensure final values are exact
        playerTransform.position = endPos;
        playerTransform.rotation = targetRot;

        lockOnCoroutine = null;
    }

    // Visualize lock-on range and angle in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, lockOnDistance);
        // Draw lock-on angle lines
        Vector3 forward = playerTransform.forward;
        Quaternion leftRotation = Quaternion.Euler(0, -lockOnAngle, 0);
        Quaternion rightRotation = Quaternion.Euler(0, lockOnAngle, 0);
        Vector3 leftDirection = leftRotation * forward;
        Vector3 rightDirection = rightRotation * forward;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftDirection * lockOnDistance);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightDirection * lockOnDistance);
    }
}
