// BossPlayerEjector.cs
// Purpose: Prevents the player from getting trapped inside the boss by detecting overlap and pushing them out.
// Attach to the boss GameObject (same object as BossRoombaBrain).
// Note: Only ejects if player is at same Y-level as boss (not when standing on top).

using System.Collections;
using UnityEngine;

namespace EnemyBehavior.Boss
{
    public class BossPlayerEjector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Radius to check for player overlap with boss center (horizontal only)")]
        public float OverlapCheckRadius = 4f;

        [Tooltip("How far above the boss center the player must be to count as 'on top' (not trapped)")]
        public float OnTopHeightThreshold = 2f;

        [Tooltip("How far below the boss center to still check for trapping")]
        public float BelowThreshold = 1f;

        [Tooltip("How often to check for overlap (seconds)")]
        public float CheckInterval = 0.1f;

        [Header("Ejection Settings")]
        [Tooltip("Force applied to push player out")]
        public float EjectionForce = 15f;

        [Tooltip("Upward force component to help player clear the boss")]
        public float EjectionUpwardForce = 5f;

        [Tooltip("Minimum distance player must be from boss center after ejection")]
        public float MinSafeDistance = 6f;

        [Header("References")]
        [Tooltip("Transform representing the center of the boss (auto-found if null)")]
        public Transform BossCenter;

        private Transform player;
        private PlayerMovement playerMovement;
        private CharacterController playerController;
        private float lastCheckTime;

        private void Start()
        {
            if (BossCenter == null)
                BossCenter = transform;

            CachePlayerReference();
        }

        private void CachePlayerReference()
        {
            if (PlayerPresenceManager.IsPlayerPresent)
            {
                var presencePlayer = PlayerPresenceManager.PlayerTransform;
                if (presencePlayer != null)
                {
                    playerMovement = presencePlayer.GetComponent<PlayerMovement>()
                        ?? presencePlayer.GetComponentInParent<PlayerMovement>()
                        ?? presencePlayer.GetComponentInChildren<PlayerMovement>();

                    if (playerMovement != null)
                    {
                        player = playerMovement.transform;
                        playerController = playerMovement.GetComponent<CharacterController>()
                            ?? playerMovement.GetComponentInChildren<CharacterController>();
                        return;
                    }
                }
            }

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<PlayerMovement>()
                    ?? playerObj.GetComponentInParent<PlayerMovement>()
                    ?? playerObj.GetComponentInChildren<PlayerMovement>();

                if (playerMovement != null)
                {
                    player = playerMovement.transform;
                    playerController = playerMovement.GetComponent<CharacterController>()
                        ?? playerMovement.GetComponentInChildren<CharacterController>();
                }
                else
                {
                    player = playerObj.transform;
                }
            }
        }

        private void Update()
        {
            if (Time.time - lastCheckTime < CheckInterval) return;
            lastCheckTime = Time.time;

            if (player == null)
            {
                CachePlayerReference();
                return;
            }

            CheckAndEjectPlayer();
        }

        private void CheckAndEjectPlayer()
        {
            Vector3 bossPos = BossCenter.position;
            Vector3 playerPos = player.position;

            // Check Y-level first: if player is above the boss (standing on top), don't eject
            float yDifference = playerPos.y - bossPos.y;
            
            // Player is on top of the boss - this is allowed
            if (yDifference > OnTopHeightThreshold)
                return;
            
            // Player is too far below the boss - probably not actually inside
            if (yDifference < -BelowThreshold)
                return;

            // Player is at roughly the same Y level as boss - check horizontal distance
            Vector3 bossPos2D = new Vector3(bossPos.x, 0, bossPos.z);
            Vector3 playerPos2D = new Vector3(playerPos.x, 0, playerPos.z);
            float distance2D = Vector3.Distance(bossPos2D, playerPos2D);

            float ejectionThreshold = OverlapCheckRadius * 0.7f;
            if (distance2D < ejectionThreshold)
            {
                EjectPlayer(bossPos, playerPos);
            }
        }

        private void EjectPlayer(Vector3 bossPos, Vector3 playerPos)
        {
            Vector3 ejectionDir = (playerPos - bossPos);
            ejectionDir.y = 0;

            if (ejectionDir.sqrMagnitude < 0.01f)
            {
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                ejectionDir = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
            }

            ejectionDir = ejectionDir.normalized;

            Vector3 targetPos = bossPos + ejectionDir * MinSafeDistance;
            targetPos.y = playerPos.y;

#if UNITY_EDITOR
            Debug.LogWarning($"[BossPlayerEjector] Player trapped inside boss! Ejecting to {targetPos}");
#endif

            if (playerController != null)
            {
                playerController.enabled = false;
                player.position = targetPos;
                playerController.enabled = true;
            }
            else
            {
                player.position = targetPos;
            }

            if (playerMovement != null)
            {
                Vector3 ejectionVelocity = ejectionDir * EjectionForce + Vector3.up * EjectionUpwardForce;
                playerMovement.SetExternalVelocity(ejectionVelocity);
                StartCoroutine(ClearEjectionVelocityAfterDelay(0.3f));
            }
        }

        private IEnumerator ClearEjectionVelocityAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (playerMovement != null)
                playerMovement.ClearExternalVelocity();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = BossCenter != null ? BossCenter.position : transform.position;

            // Draw horizontal detection radius (as a flat disc)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            DrawWireDisc(center, OverlapCheckRadius, 32);

            // Draw ejection threshold disc
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            DrawWireDisc(center, OverlapCheckRadius * 0.7f, 32);

            // Draw safe distance
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            DrawWireDisc(center, MinSafeDistance, 32);

            // Draw Y-level bounds (the vertical range where ejection can happen)
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Vector3 topBound = center + Vector3.up * OnTopHeightThreshold;
            Vector3 bottomBound = center - Vector3.up * BelowThreshold;
            
            // Draw lines showing the Y bounds
            float r = OverlapCheckRadius * 0.7f;
            Gizmos.DrawLine(center + new Vector3(r, 0, 0), topBound + new Vector3(r, 0, 0));
            Gizmos.DrawLine(center + new Vector3(-r, 0, 0), topBound + new Vector3(-r, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, 0, r), topBound + new Vector3(0, 0, r));
            Gizmos.DrawLine(center + new Vector3(0, 0, -r), topBound + new Vector3(0, 0, -r));
            
            Gizmos.DrawLine(center + new Vector3(r, 0, 0), bottomBound + new Vector3(r, 0, 0));
            Gizmos.DrawLine(center + new Vector3(-r, 0, 0), bottomBound + new Vector3(-r, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, 0, r), bottomBound + new Vector3(0, 0, r));
            Gizmos.DrawLine(center + new Vector3(0, 0, -r), bottomBound + new Vector3(0, 0, -r));
            
            // Draw top threshold disc (above this = on top, allowed)
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            DrawWireDisc(topBound, r, 16);
        }

        private void DrawWireDisc(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}