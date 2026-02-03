using System;
using System.Collections;
using UnityEngine;

public class CargoBayCrane : CranePuzzle
{
    protected enum DetectionResult
    {
        None,
        Target,
        Wrong
    }

    [Header("Crane References")]
    [SerializeField] public GameObject magnetExtender;
    [SerializeField] protected float magnetExtendHeight;

    [Header("Grab References")]
    [SerializeField] protected CraneGrabObject craneGrabObjectScript;

    [Header("Grab Settings")]
    [Tooltip("Object crane needs to grab")]
    [SerializeField] internal GameObject targetObject;
    [SerializeField] protected LayerMask grabLayerMask;
    [SerializeField] protected float magnetDetectLength;
    [SerializeField] protected GameObject targetDropZone;
    
    protected Coroutine retractCoroutine;
    internal bool isGrabbed;

    private void Update()
    {
        if(_confirmPuzzleAction != null && _confirmPuzzleAction.action != null && !isExtending && !AmIMoving())
        {
            CheckForConfirm();
        }
    }

    protected bool AmIMoving()
    {
        foreach (CranePart part in craneParts)
        {
            if (part.partObject == null) continue;

            Vector3 currentPos = part.partObject.transform.localPosition;
            if (cranePartStartLocalPositions.TryGetValue(part, out Vector3 startPos))
            {
                if (currentPos != startPos)
                {
                    return true;
                }
            }
        }
        return false;
    }

    protected IEnumerator AnimateMagnet(GameObject magnet, Vector3 targetPosition, float duration, bool magnetRetract = true)
    {
        LockOrUnlockMovement(true);
        Vector3 startPosition = magnet.transform.localPosition;
        Vector3 extendTarget = new Vector3(magnet.transform.localPosition.x, magnetExtendHeight, magnet.transform.localPosition.z);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            magnet.transform.localPosition = Vector3.Lerp(startPosition, extendTarget, elapsed / duration);
            
            // Check continuously during extension for objects below
            DetectionResult detectionResult = DetectDesiredObjectBelow();
            
            // If hit wrong object, bounce back immediately
            if (detectionResult == DetectionResult.Wrong && elapsed > 0.1f) // Small delay to avoid instant bounce
            {
                isExtending = false;
                
                if (retractCoroutine != null)
                {
                    StopCoroutine(retractCoroutine);
                }
                retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, duration * 0.5f));
                yield break;
            }
            else if (detectionResult == DetectionResult.Target) // Target found
            {
                isExtending = false;
                if (magnetRetract)
                {
                    if (retractCoroutine != null)
                    {
                        StopCoroutine(retractCoroutine);
                    }
                    retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, duration));
                }
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        magnet.transform.localPosition = extendTarget;

        // Final check at full extension
        DetectionResult finalCheck = DetectDesiredObjectBelow();
        
        if (magnetRetract)
        {
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
            }
            float retractDuration = finalCheck == DetectionResult.Wrong ? duration * 0.5f : duration;
            retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, retractDuration));
        }
        else
        {
            isExtending = false;
        }
    }

    protected IEnumerator MoveCraneToPosition(GameObject crane, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = crane.transform.localPosition;
        CranePart cranePart = craneParts.Find(p => p.partObject == crane);

        Vector3 finalTarget = new Vector3(
            cranePart.moveX ? targetPosition.x : startPosition.x,
            cranePart.moveY ? targetPosition.y : startPosition.y,
            cranePart.moveZ ? targetPosition.z : startPosition.z
        );
        
        if (cranePart.moveX)
            finalTarget.x = Mathf.Clamp(finalTarget.x, cranePart.minX, cranePart.maxX);
        if (cranePart.moveY)
            finalTarget.y = Mathf.Clamp(finalTarget.y, cranePart.minY, cranePart.maxY);
        if (cranePart.moveZ)
            finalTarget.z = Mathf.Clamp(finalTarget.z, cranePart.minZ, cranePart.maxZ);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            crane.transform.localPosition = Vector3.Lerp(startPosition, finalTarget, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        crane.transform.localPosition = finalTarget;
    }

    protected IEnumerator ReturnCraneToStartPosition(GameObject crane, Vector3 startPosition, float duration)
    {
        Vector3 currentPos = crane.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            crane.transform.localPosition = Vector3.Lerp(currentPos, startPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        crane.transform.localPosition = startPosition;
    }

    // Returns the World Position where the magnet should move to align with the target object
    private Vector3 CalculateMagnetTargetWorldPos(Vector3 targetWorldPos)
    {
        if (targetObject == null)
            return targetWorldPos;

        // Local pos of target object
        Vector3 objectLocalPos = targetObject.transform.localPosition;
        // Offset of object relative to magnet in magnet's local space
        Vector3 objectWorldOffset = magnetExtender.transform.TransformVector(objectLocalPos);
        return targetWorldPos - objectWorldOffset;
    }

    // Moves the crane parts to position the magnet above the target world position
    private IEnumerator MoveCraneToMagnetTarget(Vector3 magnetTargetWorldPos)
    {
        Debug.Log("[CargoBayCrane] MoveCraneToMagnetTarget: Case 2A (part 0 parent == part 1 transform)");

        // Gets the target position in part 1's parent space
        Vector3 targetInPart1ParentSpace = craneParts[1].partObject.transform.parent != null
            ? craneParts[1].partObject.transform.parent.InverseTransformPoint(magnetTargetWorldPos)
            : magnetTargetWorldPos;

        // Calculate target Z for part 1 based on magnet target position
        float magnetZOffsetFromPart1 = magnetExtender.transform.position.z - craneParts[1].partObject.transform.position.z;

        // Determine where part 1 needs to move to align magnet with target
        Vector3 part1TargetWorldPos = new Vector3(
            craneParts[1].partObject.transform.position.x,
            craneParts[1].partObject.transform.position.y,
            magnetTargetWorldPos.z - magnetZOffsetFromPart1
        );

        // Convert part1 target position to its parent's local space
        Vector3 part1TargetInParentSpace = craneParts[1].partObject.transform.parent.InverseTransformPoint(part1TargetWorldPos);
        float targetZForPart1 = part1TargetInParentSpace.z;

        yield return StartCoroutine(MoveCraneToPosition(craneParts[1].partObject, new Vector3(0, 0, targetZForPart1), 1));

        // Now move part 0 to align magnet horizontally
        Vector3 magnetOffsetInPart0Local = magnetExtender.transform.localPosition;
        Vector3 targetInPart1Space = craneParts[1].partObject.transform.InverseTransformPoint(magnetTargetWorldPos);
        Vector3 part0TargetInPart1Space = targetInPart1Space - magnetOffsetInPart0Local;

        yield return StartCoroutine(MoveCraneToPosition(craneParts[0].partObject, new Vector3(part0TargetInPart1Space.x, 0, 0), 1));

        yield return new WaitForSeconds(0.5f);
    }

    // Lowers the magnet until it collides with an object (excluding the target object and magnet itself) or reaches max drop distance
    private IEnumerator LowerMagnetUntilCollision(float dropSpeed, float maxDropDistance, Action<bool> onComplete)
    {
        Vector3 dropStartPos = magnetExtender.transform.localPosition;
        float droppedDistance = 0f;
        bool reachedDropTarget = false;

        Collider targetCollider = targetObject != null ? targetObject.GetComponent<Collider>() : null;
        // Lower magnet until collision or max distance reached
        while (droppedDistance < maxDropDistance && !reachedDropTarget)
        {
            float step = dropSpeed * Time.deltaTime;
            magnetExtender.transform.localPosition += Vector3.down * step;
            droppedDistance += step;

            if (targetCollider != null)
            {
                // Check for collisions with objects other than the target and magnet
                Bounds bounds = targetCollider.bounds;
                // Gets all collider overlaps at the target object's bounds
                Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, targetObject.transform.rotation, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hitCol = hits[i];
                    bool hitTargetObject = hitCol.gameObject == targetObject || hitCol.transform.IsChildOf(targetObject.transform);
                    bool hitMagnet = hitCol.transform.IsChildOf(magnetExtender.transform);
                    if (!hitTargetObject && !hitMagnet)
                    {
                        reachedDropTarget = true;
                        break;
                    }
                }
            }
            // Fallback spherecast check directly below magnet
            else
            {
                Vector3 castOrigin = magnetExtender.transform.position;
                float castRadius = 0.3f;
                float castDistance = step + 0.3f;
                // Spherecast downwards to check for collisions
                if (Physics.SphereCast(castOrigin, castRadius, Vector3.down, out RaycastHit hitInfo, castDistance, ~0, QueryTriggerInteraction.Collide))
                {
                    bool hitTargetObject = targetObject != null && (hitInfo.collider.gameObject == targetObject || hitInfo.collider.transform.IsChildOf(targetObject.transform));
                    bool hitMagnet = hitInfo.collider.transform.IsChildOf(magnetExtender.transform);
                    if (!hitTargetObject && !hitMagnet)
                    {
                        reachedDropTarget = true;
                    }
                }
            }

            yield return null;
        }
        // Snap magnet to final drop position
        magnetExtender.transform.localPosition = new Vector3(dropStartPos.x, magnetExtender.transform.localPosition.y, dropStartPos.z);
        onComplete?.Invoke(reachedDropTarget);
    }

    protected IEnumerator RetractMagnet(GameObject magnet, Vector3 originalPosition, float duration)
    {
        Vector3 startPosition = magnet.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            magnet.transform.localPosition = Vector3.Lerp(startPosition, originalPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if(!isCompleted)
            magnet.transform.localPosition = originalPosition;
        
        isExtending = false;
        
        // Only unlock movement after retraction is complete and we're not grabbing anything
        if(!isGrabbed)
        {
            LockOrUnlockMovement(false);
        }
        
        if(isGrabbed)
        {
            isAutomatedMovement = true;
            LockOrUnlockMovement(false);

            // Move crane to target drop zone
            Vector3 targetWorldPos = targetDropZone.transform.position;
            Vector3 magnetTargetWorldPos = CalculateMagnetTargetWorldPos(targetWorldPos);

            yield return StartCoroutine(MoveCraneToMagnetTarget(magnetTargetWorldPos));

            bool reachedDropTarget = false;
            yield return StartCoroutine(LowerMagnetUntilCollision(5f, 20f, result => reachedDropTarget = result));

            if (reachedDropTarget && craneGrabObjectScript != null && targetObject != null)
            {
                craneGrabObjectScript.ReleaseObject(targetObject);
            }

            isGrabbed = false;
            targetObject = null;

            yield return StartCoroutine(RetractMagnet(magnetExtender, originalPosition, 1f));

            isAutomatedMovement = false;
            isCompleted = true;
            EndPuzzle();
        }
    }

    // Checks for confirm input to start magnet extension
    protected override void CheckForConfirm()
    {
        if (_confirmPuzzleAction.action.triggered && targetObject != null && !isExtending && !AmIMoving())
        {
            isExtending = true;
            StartCoroutine(AnimateMagnet(magnetExtender, new Vector3(targetObject.transform.position.x, magnetExtender.transform.position.y, targetObject.transform.position.z), 2f, true));
        }
    }

    #region Grab and Detection Logic

    protected DetectionResult DetectDesiredObjectBelow()
    {
        Debug.Log(targetObject != null ? $"Detecting object: {targetObject.name} (Layer: {LayerMask.LayerToName(targetObject.layer)})" : "No target object set for detection");
        Debug.Log($"GrabLayerMask value: {grabLayerMask.value} (Layers: {GetLayerMaskNames(grabLayerMask)})");

        GetRayData(out var originA, out var originB, out var originC, out var originD, out var castDir);

        Debug.Log($"Magnet position: {magnetExtender.transform.position}, Cast direction: {castDir}, Detect length: {magnetDetectLength}");
        if (targetObject != null)
        {
            float distanceToTarget = Vector3.Distance(magnetExtender.transform.position, targetObject.transform.position);
            Debug.Log($"Distance to target: {distanceToTarget}, Target position: {targetObject.transform.position}");
            
            Collider targetCollider = targetObject.GetComponent<Collider>();
            Debug.Log($"Target has collider: {targetCollider != null}, Collider enabled: {(targetCollider != null ? targetCollider.enabled.ToString() : "N/A")}");
        }

        // Raycast with all layers to detect any object below, not just grabLayerMask
        int allLayersMask = ~0; // All layers
        bool hitFirst = Physics.Raycast(originA, castDir, out var hit, magnetDetectLength, allLayersMask);
        bool hitSecond = Physics.Raycast(originB, castDir, out var hit2, magnetDetectLength, allLayersMask);
        bool hitThird = Physics.Raycast(originC, castDir, out var hit3, magnetDetectLength, allLayersMask);
        bool hitFourth = Physics.Raycast(originD, castDir, out var hit4, magnetDetectLength, allLayersMask);
        
        Debug.Log($"Raycast hits: A={hitFirst}, B={hitSecond}, C={hitThird}, D={hitFourth}");
        if (hitFirst) Debug.Log($"Hit A: {hit.collider.gameObject.name} at distance {hit.distance}");
        if (hitSecond) Debug.Log($"Hit B: {hit2.collider.gameObject.name} at distance {hit2.distance}");
        if (hitThird) Debug.Log($"Hit C: {hit3.collider.gameObject.name} at distance {hit3.distance}");
        if (hitFourth) Debug.Log($"Hit D: {hit4.collider.gameObject.name} at distance {hit4.distance}");
        
        if(hitFirst || hitSecond || hitThird || hitFourth)
        {
            
            if((hitFirst && hit.collider.gameObject == targetObject) || (hitSecond && hit2.collider.gameObject == targetObject) 
                || (hitThird && hit3.collider.gameObject == targetObject) || (hitFourth && hit4.collider.gameObject == targetObject))
            {
                Debug.Log("Desired object detected below magnet");
                
                if (craneGrabObjectScript != null)
                {
                    craneGrabObjectScript.GrabObject(targetObject);
                    isGrabbed = true;
                    Debug.Log($"Successfully grabbed {targetObject.name}");
                }
                else
                {
                    Debug.LogError("CraneGrabObject script is not assigned! Cannot grab object.");
                }
                
                return DetectionResult.Target;
            }
            else
            {
                string hitName = hitFirst ? hit.collider.gameObject.name : hitSecond ? hit2.collider.gameObject.name 
                    : hitThird ? hit3.collider.gameObject.name : hit4.collider.gameObject.name;
                Debug.Log($"Object detected below magnet: {hitName}, but it is not the desired object - bouncing off!");
                return DetectionResult.Wrong;
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything");
        }

        return DetectionResult.None;
    }

    private void GetRayData(out Vector3 originA, out Vector3 originB, out Vector3 originC, out Vector3 originD, out Vector3 castDir)
    {
        Vector3 offset = magnetExtender.transform.TransformDirection(Vector3.forward * 2f);
        Vector3 offset2 = magnetExtender.transform.TransformDirection(Vector3.right * 2f);
        originA = magnetExtender.transform.position + offset;
        originB = magnetExtender.transform.position - offset;
        originC = magnetExtender.transform.position + offset2;
        originD = magnetExtender.transform.position - offset2;
        castDir = magnetExtender.transform.TransformDirection(Vector3.down);
    }

    public void BounceOffObject()
    {
        // Called by MagnetCollisionHandler when magnet hits a non-target object
        isExtending = false;
        
        if (retractCoroutine != null)
        {
            StopCoroutine(retractCoroutine);
        }
        
        if (magnetExtender != null)
        {
            Vector3 startPosition = magnetExtender.transform.localPosition;
            retractCoroutine = StartCoroutine(RetractMagnet(magnetExtender, startPosition, 0.5f));
        }
    }

    protected void AssignRayData()
    {
        if (magnetExtender == null) return;

        GetRayData(out var originA, out var originB, out var originC, out var originD, out var castDir);

        if (Physics.Raycast(originA, castDir, out var dbgHitA, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originA, castDir * dbgHitA.distance, dbgHitA.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originA, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originB, castDir, out var dbgHitB, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originB, castDir * dbgHitB.distance, dbgHitB.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originB, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originC, castDir, out var dbgHitC, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originC, castDir * dbgHitC.distance, dbgHitC.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originC, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originD, castDir, out var dbgHitD, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originD, castDir * dbgHitD.distance, dbgHitD.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originD, castDir * magnetDetectLength, Color.yellow);
        }
    }

    protected void OnDrawGizmos()
    {
        if (magnetExtender == null) return;

        GetRayData(out var originA, out var originB, out var originC, out var originD, out var castDir);

        // Draw gizmos for all four raycasts
        if (Physics.Raycast(originA, castDir, out var hitA, magnetDetectLength, grabLayerMask))
        {
            Gizmos.color = hitA.collider.gameObject == targetObject ? Color.cyan : Color.red;
            Gizmos.DrawLine(originA, hitA.point);
            Gizmos.DrawWireSphere(hitA.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originA, originA + castDir * magnetDetectLength);
        }

        if (Physics.Raycast(originB, castDir, out var hitB, magnetDetectLength, grabLayerMask))
        {
            Gizmos.color = hitB.collider.gameObject == targetObject ? Color.cyan : Color.red;
            Gizmos.DrawLine(originB, hitB.point);
            Gizmos.DrawWireSphere(hitB.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originB, originB + castDir * magnetDetectLength);
        }

        if (Physics.Raycast(originC, castDir, out var hitC, magnetDetectLength, grabLayerMask))
        {
            Gizmos.color = hitC.collider.gameObject == targetObject ? Color.cyan : Color.red;
            Gizmos.DrawLine(originC, hitC.point);
            Gizmos.DrawWireSphere(hitC.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originC, originC + castDir * magnetDetectLength);
        }

        if (Physics.Raycast(originD, castDir, out var hitD, magnetDetectLength, grabLayerMask))
        {
            Gizmos.color = hitD.collider.gameObject == targetObject ? Color.cyan : Color.red;
            Gizmos.DrawLine(originD, hitD.point);
            Gizmos.DrawWireSphere(hitD.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originD, originD + castDir * magnetDetectLength);
        }
    }

    private string GetLayerMaskNames(LayerMask mask)
    {
        System.Collections.Generic.List<string> layers = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                }
            }
        }
        return layers.Count > 0 ? string.Join(", ", layers) : "None";
    }

    #endregion
}