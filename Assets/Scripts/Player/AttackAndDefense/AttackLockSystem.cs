/*
 * Written by Will Thomsen
 *  
 * this script is designed to check if the player is facing an enemy when attacking
 * if the player is facing an enemy, the player will move towards the enmemy and line up the attack for the player
 *
 * Updated By Kyle Woo
 * Updated to support soft lock nudges (player movement) and a hard lock mode that
 * steers the active Cinemachine camera toward the selected enemy.
 */

using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Utilities.Combat.Attacks;

public class AttackLockSystem : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField]
    [Tooltip("Angle within which to lock on to an enemy")]
    private float lockOnAngle = 30f;

    [SerializeField]
    [Tooltip("Maximum distance to search for enemies")]
    private float lockOnDistance = 6f;

    [SerializeField]
    [CriticalReference]
    [Tooltip("Reference to the player GameObject (used as the search origin).")]
    private GameObject player;

    [SerializeField]
    [Tooltip("Optional reference to PlayerMovement so we can pause rotation while dashing.")]
    private PlayerMovement playerMovement;

    [SerializeField]
    [Tooltip("Optionally restrict candidates to specific layers.")]
    private LayerMask enemyLayers = ~0;

    [SerializeField]
    [Tooltip("Require enemies to be on the specified layer mask.")]
    private bool enforceLayerMask = false;

    [Header("Soft Lock Settings")]
    [SerializeField]
    [Tooltip("Radius within which soft lock nudges will move the player toward the target.")]
    private float softLockRadius = 2.5f;

    [SerializeField]
    [Tooltip("Maximum nudge distance when soft locking.")]
    private float softLockMoveDistance = 0.75f;

    [SerializeField]
    [Tooltip("Minimum buffer to leave between the player and the target after a soft lock nudge.")]
    private float softLockStopBuffer = 0.5f;

    [SerializeField]
    [Tooltip("Inside this radius the soft lock stops moving the player and only rotates them toward the target.")]
    private float softLockNoMoveRadius = 1.15f;

    [SerializeField, Range(0.05f, 0.4f)]
    [Tooltip("Duration of the soft lock movement blend.")]
    private float softLockMoveDuration = 0.12f;

    [SerializeField]
    [Tooltip("Only soft lock on single-target melee strikes.")]
    private bool onlySoftLockSingleTarget = true;

    [Header("Camera Lock Settings")]
    [SerializeField]
    [Tooltip("Steer the active camera instead of moving the player root.")]
    private bool steerCamera = true;

    [Header("Hard Lock Settings")]
    [SerializeField]
    [Tooltip("Rotate the player toward the locked enemy while hard lock is active.")]
    private bool rotatePlayerDuringHardLock = true;

    [SerializeField, Range(30f, 1440f)]
    [Tooltip("Degrees per second to rotate while tracking a hard-lock target.")]
    private float hardLockRotateSpeed = 540f;

    [SerializeField, Range(0.05f, 1.5f)]
    [Tooltip("Seconds it should take to align the camera towards the enemy.")]
    private float cameraSnapTime = 0.35f;

    [SerializeField]
    [Tooltip("Camera manager reference. Defaults to CameraManager.Instance if left empty.")]
    private CameraManager cameraManager;

    [SerializeField]
    [Tooltip("Fallback: also rotate the player instantly if camera steering is disabled.")]
    private bool rotatePlayerIfCameraDisabled = false;

    private Transform playerTransform => player != null ? player.transform : transform;
    private Transform currentTarget;
    private bool hardLockActive;
    private Coroutine moveCoroutine;
    public bool IsHardLockActive => hardLockActive && currentTarget != null;
    public Transform CurrentHardLockTarget => currentTarget;

    private void Awake()
    {
        cameraManager ??= CameraManager.Instance;
        ResolvePlayerMovement();
    }

    private void OnEnable()
    {
        PlayerAttackManager.OnAttack += HandleAttackEvent;
        InputReader.LockOnPressed += HandleLockOnToggle;
        InputReader.LeftTargetPressed += HandleLeftTargetRequested;
        InputReader.RightTargetPressed += HandleRightTargetRequested;
    }

    private void OnDisable()
    {
        PlayerAttackManager.OnAttack -= HandleAttackEvent;
        InputReader.LockOnPressed -= HandleLockOnToggle;
        InputReader.LeftTargetPressed -= HandleLeftTargetRequested;
        InputReader.RightTargetPressed -= HandleRightTargetRequested;
        StopMoveRoutine();
        ClearHardLock();
    }

    private void Update()
    {
        if (!hardLockActive || currentTarget == null)
            return;

        if (!IsTargetValid(currentTarget, lockOnDistance))
        {
            currentTarget = FindBestHardLockTarget();
            if (currentTarget == null)
            {
                ClearHardLock();
                return;
            }
        }

        if (steerCamera)
            AimCameraAtTarget(currentTarget, instant: false);
        else if (rotatePlayerIfCameraDisabled)
            FaceTargetImmediately(currentTarget);

        if (rotatePlayerDuringHardLock)
            RotatePlayerTowardTarget(currentTarget, instant: false);
    }

    private void HandleAttackEvent(PlayerAttack executedAttack)
    {
        if (executedAttack == null)
            return;

        if (hardLockActive)
        {
            if (currentTarget == null)
                currentTarget = FindBestHardLockTarget();

            if (currentTarget != null)
            {
                if (steerCamera)
                    AimCameraAtTarget(currentTarget, instant: true);
                else if (rotatePlayerIfCameraDisabled)
                    FaceTargetImmediately(currentTarget);

                if (rotatePlayerDuringHardLock)
                    RotatePlayerTowardTarget(currentTarget, instant: true);
            }
            else
            {
                ClearHardLock();
            }

            return;
        }

        if (onlySoftLockSingleTarget && !IsSingleTargetAttack(executedAttack))
            return;

        TryApplySoftLockNudge();
    }

    private void HandleLockOnToggle()
    {
        if (hardLockActive)
        {
            ClearHardLock();
            return;
        }

        ActivateHardLock(null, instantCameraAlign: true);
    }

    private void HandleLeftTargetRequested() => CycleHardLock(-1);

    private void HandleRightTargetRequested() => CycleHardLock(1);

    private void CycleHardLock(int direction)
    {
        if (!hardLockActive || direction == 0)
            return;

        Transform nextTarget = FindAdjacentTarget(direction);
        if (nextTarget == null || nextTarget == currentTarget)
            return;

        currentTarget = nextTarget;
        AlignPlayerAndCamera(nextTarget, instantCameraAlign: true);
    }

    public bool ActivateHardLock(Transform forcedTarget = null, bool instantCameraAlign = false)
    {
        Transform candidate = forcedTarget ?? FindBestHardLockTarget();
        if (candidate == null)
            return false;

        hardLockActive = true;
        currentTarget = candidate;
        AlignPlayerAndCamera(candidate, instantCameraAlign);
        return true;
    }

    public bool EnsureHardLock(bool instantCameraAlign = false)
    {
        if (IsHardLockActive)
        {
            AlignPlayerAndCamera(currentTarget, instantCameraAlign);
            return true;
        }

        return ActivateHardLock(null, instantCameraAlign);
    }

    public void ReleaseHardLock()
    {
        ClearHardLock();
    }

    public void AlignPlayerAndCamera(Transform target, bool instantCameraAlign)
    {
        if (target == null)
            return;

        if (steerCamera)
            AimCameraAtTarget(target, instantCameraAlign);
        else if (rotatePlayerIfCameraDisabled)
            FaceTargetImmediately(target);

        if (rotatePlayerDuringHardLock)
            RotatePlayerTowardTarget(target, instant: true);
        else
            FaceTargetImmediately(target);
    }

    private void TryApplySoftLockNudge()
    {
        Transform target = FindNearestEnemy(softLockRadius);
        if (target == null)
            return;

        Vector3 direction = GetFlatDirection(target.position);
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction);

        float planarDistance = Vector3.Distance(
            new Vector3(target.position.x, playerTransform.position.y, target.position.z),
            playerTransform.position
        );

        if (planarDistance <= softLockNoMoveRadius)
        {
            FaceTargetImmediately(target);
            return;
        }

        float moveDistance = Mathf.Clamp(
            planarDistance - softLockStopBuffer,
            0f,
            softLockMoveDistance
        );

        if (moveDistance <= 0.01f)
        {
            FaceTargetImmediately(target);
            return;
        }

        Vector3 desiredPosition = playerTransform.position + direction * moveDistance;
        if (TrySnapPlayerToSoftLock(desiredPosition, desiredRotation))
            return;

        StopMoveRoutine();
        moveCoroutine = StartCoroutine(
            MoveAndFaceCoroutine(desiredPosition, desiredRotation, softLockMoveDuration)
        );
    }

    private bool TrySnapPlayerToSoftLock(Vector3 worldPosition, Quaternion desiredRotation)
    {
        PlayerMovement movement = ResolvePlayerMovement();
        if (movement == null)
            return false;

        return movement.TrySnapToSoftLock(worldPosition, desiredRotation);
    }

    private void ClearHardLock()
    {
        hardLockActive = false;
        currentTarget = null;
    }

    private void StopMoveRoutine()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }

    private Transform FindBestHardLockTarget()
    {
        Transform screenAligned = FindScreenAlignedEnemy(lockOnDistance);
        if (screenAligned != null)
            return screenAligned;

        return FindNearestEnemy(lockOnDistance);
    }

    private Transform FindNearestEnemy(float radius, Transform ignore = null)
    {
        Collider[] hits = GetEnemyHits(radius);
        Transform closest = null;
        float smallestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!ColliderIsEnemy(hit))
                continue;

            Transform candidate = hit.transform;
            if (candidate == ignore)
                continue;

            float sqrDistance = (candidate.position - playerTransform.position).sqrMagnitude;
            if (sqrDistance < smallestDistance)
            {
                smallestDistance = sqrDistance;
                closest = candidate;
            }
        }

        return closest;
    }

    private Transform FindScreenAlignedEnemy(float radius)
    {
        if (!TryGetCameraBasis(out Vector3 camForward, out _))
            return null;

        Collider[] hits = GetEnemyHits(radius);
        Transform best = null;
        float smallestAngle = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!ColliderIsEnemy(hit))
                continue;

            Vector3 direction = GetFlatDirection(hit.transform.position);
            if (direction.sqrMagnitude < 0.001f)
                continue;

            float angle = Vector3.Angle(camForward, direction);
            if (angle <= lockOnAngle * 2f && angle < smallestAngle)
            {
                smallestAngle = angle;
                best = hit.transform;
            }
        }

        return best;
    }

    private Transform FindAdjacentTarget(int direction)
    {
        if (!TryGetCameraBasis(out Vector3 camForward, out Vector3 camRight))
            return null;

        Collider[] hits = GetEnemyHits(lockOnDistance);
        Transform best = null;
        float bestScore = float.MaxValue;
        float sideThreshold = 0.05f;

        foreach (Collider hit in hits)
        {
            if (!ColliderIsEnemy(hit))
                continue;

            Transform candidate = hit.transform;
            if (candidate == currentTarget)
                continue;

            Vector3 directionToCandidate = GetFlatDirection(candidate.position);
            if (directionToCandidate.sqrMagnitude < 0.001f)
                continue;

            float sideDot = Vector3.Dot(camRight, directionToCandidate);
            if (direction < 0 && sideDot >= -sideThreshold)
                continue;
            if (direction > 0 && sideDot <= sideThreshold)
                continue;

            float angle = Vector3.Angle(camForward, directionToCandidate);
            if (angle > lockOnAngle * 2f)
                continue;

            if (angle < bestScore)
            {
                bestScore = angle;
                best = candidate;
            }
        }

        return best;
    }

    private Collider[] GetEnemyHits(float radius)
    {
        int mask = enforceLayerMask ? enemyLayers.value : ~0;

        return Physics.OverlapSphere(
            playerTransform.position,
            radius,
            mask,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool ColliderIsEnemy(Collider hit)
    {
        if (hit == null)
            return false;

        if (!hit.CompareTag("Enemy"))
            return false;

        if (!enforceLayerMask)
            return true;

        int bit = 1 << hit.gameObject.layer;
        return (enemyLayers.value & bit) != 0;
    }

    private bool TryGetCameraBasis(out Vector3 forward, out Vector3 right)
    {
        forward = Vector3.zero;
        right = Vector3.zero;

        CinemachineCamera activeCamera =
            cameraManager != null ? cameraManager.GetActiveCamera() : null;
        
        if (activeCamera == null || activeCamera.transform == null)
            return false;

        forward = activeCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = activeCamera.transform.forward;
        forward.Normalize();

        right = activeCamera.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(Vector3.up, forward);
        right.Normalize();

        return true;
    }

    private static bool IsSingleTargetAttack(PlayerAttack attack)
    {
        if (attack == null)
            return false;

        return attack.attackType == AttackType.LightSingle
            || attack.attackType == AttackType.HeavySingle;
    }

    private bool IsTargetValid(Transform target, float maxDistance)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        float sqrDistance = (target.position - playerTransform.position).sqrMagnitude;
        return sqrDistance <= maxDistance * maxDistance;
    }

    private Vector3 GetFlatDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - playerTransform.position;
        direction.y = 0f;
        return direction.normalized;
    }

    private void AimCameraAtTarget(Transform target, bool instant)
    {
        CinemachineCamera activeCamera =
            cameraManager != null ? cameraManager.GetActiveCamera() : null;
        if (activeCamera == null)
            return;

        CinemachineOrbitalFollow orbital = activeCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbital == null)
            return;

        Vector3 toTarget = target.position - playerTransform.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude < 0.001f)
            return;

        float desiredYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        float lerpFactor = instant ? 1f : Time.deltaTime / Mathf.Max(0.001f, cameraSnapTime);
        float nextYaw = Mathf.LerpAngle(orbital.HorizontalAxis.Value, desiredYaw, lerpFactor);
        orbital.HorizontalAxis.Value = nextYaw;
    }

    private void FaceTargetImmediately(Transform target)
    {
        Vector3 direction = target.position - playerTransform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        playerTransform.rotation = Quaternion.LookRotation(direction);
    }

    private void RotatePlayerTowardTarget(Transform target, bool instant)
    {
        if (target == null || playerTransform == null)
            return;

        if (!instant && IsPlayerCurrentlyDashing())
            return;

        Vector3 direction = target.position - playerTransform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desired = Quaternion.LookRotation(direction);

        if (instant || hardLockRotateSpeed <= 0f)
        {
            playerTransform.rotation = desired;
            return;
        }

        float maxStep = hardLockRotateSpeed * Time.deltaTime;
        playerTransform.rotation = Quaternion.RotateTowards(
            playerTransform.rotation,
            desired,
            maxStep
        );
    }

    private bool IsPlayerCurrentlyDashing()
    {
        PlayerMovement movement = ResolvePlayerMovement();
        return movement != null && movement.IsDashing;
    }

    private PlayerMovement ResolvePlayerMovement()
    {
        if (playerMovement != null)
            return playerMovement;

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>()
                ?? player.GetComponentInChildren<PlayerMovement>()
                ?? player.GetComponentInParent<PlayerMovement>();
            if (playerMovement != null)
                return playerMovement;
        }

        playerMovement = GetComponent<PlayerMovement>()
            ?? GetComponentInChildren<PlayerMovement>()
            ?? GetComponentInParent<PlayerMovement>();

        return playerMovement;
    }

    private IEnumerator MoveAndFaceCoroutine(Vector3 endPos, Quaternion endRot, float duration)
    {
        if (duration <= Mathf.Epsilon)
        {
            playerTransform.SetPositionAndRotation(endPos, endRot);
            moveCoroutine = null;
            yield break;
        }

        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            playerTransform.position = Vector3.Lerp(startPos, endPos, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        playerTransform.SetPositionAndRotation(endPos, endRot);
        moveCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = playerTransform;
        if (origin == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, lockOnDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin.position, softLockRadius);

        Vector3 forward = origin.forward;
        Quaternion leftRotation = Quaternion.Euler(0f, -lockOnAngle, 0f);
        Quaternion rightRotation = Quaternion.Euler(0f, lockOnAngle, 0f);

        Gizmos.DrawLine(
            origin.position,
            origin.position + (leftRotation * forward) * lockOnDistance
        );

        Gizmos.DrawLine(
            origin.position,
            origin.position + (rightRotation * forward) * lockOnDistance
        );

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin.position, currentTarget.position);
        }
    }
}

