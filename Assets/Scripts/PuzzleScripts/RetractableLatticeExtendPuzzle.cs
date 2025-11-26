using System;
using System.Collections;
using UnityEngine;

public class RetractableLatticeExtendPuzzle : MonoBehaviour, IPuzzleInterface
{
    [Header("Platform Variables")]
    [SerializeField] private GameObject platform;
    [Tooltip("How long (in seconds) the extension should take")][SerializeField] private float platformLerpDuration = 0.5f;
    [Tooltip("Offset to apply to the platform when extending (local space). Set Z negative to move back, positive to move forward.")][SerializeField] private Vector3 platformExtendOffset = new Vector3(0f, 0f, -5f);
    [Tooltip("If true, compute platform duration from `platformSpeed` (units/sec) instead of using `lerpDuration`")][SerializeField] private bool usePlatformSpeed = false;
    [Tooltip("Platform movement speed in world units per second (used when `usePlatformSpeed` is true)")][SerializeField] private float platformSpeed = 5f;
    private Vector3 platformStartPos;
    private Vector3 platformTargetPos;
    private float platformDuration = 0.5f;
    protected Vector3 platformOrigin;

    [Space(20)]

    [Header("Lattice (Magnet) Variables")]
    [SerializeField] private GameObject magnetOne;
    [SerializeField] private GameObject magnetTwo;  
    [Tooltip("How long each lattice part should take to move")][SerializeField] private float latticeLerpDuration = 0.25f;
    [Tooltip("If true, compute lattice durations from `latticeSpeed` units per second instead of using `latticeLerpDuration`")][SerializeField] private bool useLatticeSpeed = false;
    [Tooltip("Lattice (magnet) movement speed in local units per second (used when `useLatticeSpeed` is true)")][SerializeField] private float latticeSpeed = 2f;
    [SerializeField] private Vector3 latticeExtendOffset = new Vector3(0f, 5f, 0f);
    private Vector3 magnetOneStartPos;
    private Vector3 magnetOneTargetPos;
    private Vector3 magnetTwoStartPos;
    private Vector3 magnetTwoTargetPos;
    private float magnetOneDuration = 0.25f;
    private float magnetTwoDuration = 0.25f;

    protected Vector3 magnetOneOrigin;
    protected Vector3 magnetTwoOrigin;

    private bool isExtending = false;

    private void Awake()
    {
        // Cache original positions once
        platformOrigin = platform != null ? platform.transform.position : Vector3.zero;
        magnetOneOrigin = magnetOne != null ? magnetOne.transform.position : Vector3.zero;
        magnetTwoOrigin = magnetTwo != null ? magnetTwo.transform.position : Vector3.zero;
    }

    public bool isCompleted { get; set; }

    public void StartPuzzle()
    {
        // Prepare start/target positions
        platformStartPos = platformOrigin;
        magnetOneStartPos = magnetOneOrigin;
        magnetTwoStartPos = magnetTwoOrigin;

        // Compute world-space target using platform's transform to respect orientation
        if (platform != null)
            platformTargetPos = platformStartPos + platform.transform.TransformDirection(platformExtendOffset);
        else
            platformTargetPos = platformStartPos + platformExtendOffset;

        if (magnetOne != null)
            magnetOneTargetPos = magnetOneStartPos + magnetOne.transform.TransformDirection(latticeExtendOffset);
        else
            magnetOneTargetPos = magnetOneStartPos + latticeExtendOffset;

        if (magnetTwo != null)
            magnetTwoTargetPos = magnetTwoStartPos + magnetTwo.transform.TransformDirection(latticeExtendOffset);
        else
            magnetTwoTargetPos = magnetTwoStartPos + latticeExtendOffset;

        // compute durations from speeds if requested
        float platformDist = Vector3.Distance(platformStartPos, platformTargetPos);
        platformDuration = (usePlatformSpeed && platformSpeed > 0f) ? platformDist / platformSpeed : platformLerpDuration;

        magnetOneDuration = 0f;
        if (magnetOne != null)
        {
            float m1dist = Vector3.Distance(magnetOneStartPos, magnetOneTargetPos);
            magnetOneDuration = (useLatticeSpeed && latticeSpeed > 0f) ? m1dist / latticeSpeed : latticeLerpDuration;
        }

        magnetTwoDuration = 0f;
        if (magnetTwo != null)
        {
            float m2dist = Vector3.Distance(magnetTwoStartPos, magnetTwoTargetPos);
            magnetTwoDuration = (useLatticeSpeed && latticeSpeed > 0f) ? m2dist / latticeSpeed : latticeLerpDuration;
        }

        // Start smooth extension coroutine (do not flip isCompleted here; coroutine will set it)
        StopAllCoroutines();
        isCompleted = false;
        StartCoroutine(ExtendLatticeParts());
    }

    public void EndPuzzle()
    {
        if (isCompleted)
        {
            // Prepare start/target positions for retraction
            platformStartPos = platform != null ? platform.transform.position : Vector3.zero;
            platformTargetPos = platformOrigin;

            magnetOneStartPos = magnetOne != null ? magnetOne.transform.position : Vector3.zero;
            magnetOneTargetPos = magnetOneOrigin;

            magnetTwoStartPos = magnetTwo != null ? magnetTwo.transform.position : Vector3.zero;
            magnetTwoTargetPos = magnetTwoOrigin;

            // compute durations from speeds if requested
            float platformDist = Vector3.Distance(platformStartPos, platformTargetPos);
            platformDuration = (usePlatformSpeed && platformSpeed > 0f) ? platformDist / platformSpeed : platformLerpDuration;

            magnetOneDuration = 0f;
            if (magnetOne != null)
            {
                float m1dist = Vector3.Distance(magnetOneStartPos, magnetOneTargetPos);
                magnetOneDuration = (useLatticeSpeed && latticeSpeed > 0f) ? m1dist / latticeSpeed : latticeLerpDuration;
            }

            magnetTwoDuration = 0f;
            if (magnetTwo != null)
            {
                float m2dist = Vector3.Distance(magnetTwoStartPos, magnetTwoTargetPos);
                magnetTwoDuration = (useLatticeSpeed && latticeSpeed > 0f) ? m2dist / latticeSpeed : latticeLerpDuration;
            }

            // Start smooth retraction coroutine (do not flip isCompleted here; coroutine will set it)
            StopAllCoroutines();
            StartCoroutine(ExtendLatticeParts());
        }
    }

    private IEnumerator ExtendPlatform()
    {
        // Animate platform
        if (platform == null)
            yield break;

        // compute platform duration based on speed if requested (computed earlier in StartPuzzle)
        float pDur = platformDuration;

        // if duration is <= 0, snap and finish
        if (pDur <= 0f)
        {
            platform.transform.position = platformTargetPos;
            yield break;
        }

        isExtending = true;
        float elapsedPlat = 0f;
        while (elapsedPlat < pDur)
        {
            float t = Mathf.Clamp01(elapsedPlat / pDur);
            platform.transform.position = Vector3.Lerp(platformStartPos, platformTargetPos, t);
            elapsedPlat += Time.deltaTime;
            yield return null;
        }

        // Ensure final position
        platform.transform.position = platformTargetPos;
        isExtending = false;
    }

    private IEnumerator ExtendMagnets()
    {
        if (magnetOne == null && magnetTwo == null)
            yield break;

        // compute magnet durations based on speeds if requested (computed earlier in StartPuzzle)
        float m1 = magnetOneDuration;
        float m2 = magnetTwoDuration;

        // if both durations are <= 0, snap them and finish
        if (m1 <= 0f && m2 <= 0f)
        {
            if (magnetOne != null) magnetOne.transform.position = magnetOneTargetPos;
            if (magnetTwo != null) magnetTwo.transform.position = magnetTwoTargetPos;
            yield break;
        }

        // animate simultaneously using per-magnet durations (use max to drive the loop)
        float maxDur = Mathf.Max(m1 > 0f ? m1 : 0f, m2 > 0f ? m2 : 0f);
        if (maxDur <= 0f) maxDur = 0.0001f;
        isExtending = true;
        float elapsedMag = 0f;
        while (elapsedMag < maxDur)
        {
            float t1 = (m1 > 0f) ? Mathf.Clamp01(elapsedMag / m1) : 1f;
            float t2 = (m2 > 0f) ? Mathf.Clamp01(elapsedMag / m2) : 1f;
            if (magnetOne != null)
                magnetOne.transform.position = Vector3.Lerp(magnetOneStartPos, magnetOneTargetPos, t1);
            if (magnetTwo != null)
                magnetTwo.transform.position = Vector3.Lerp(magnetTwoStartPos, magnetTwoTargetPos, t2);
            elapsedMag += Time.deltaTime;
            yield return null;
        }

        // Ensure final position
        if (magnetOne != null) magnetOne.transform.position = magnetOneTargetPos;
        if (magnetTwo != null) magnetTwo.transform.position = magnetTwoTargetPos;
        isExtending = false;
    }

    private IEnumerator RetractMagnets()
    {
        if (magnetOne == null && magnetTwo == null)
            yield break;

        // compute magnet durations based on speeds if requested (computed earlier in StartPuzzle)
        float m1 = magnetOneDuration;
        float m2 = magnetTwoDuration;

        // if both durations are <= 0, snap them and finish
        if (m1 <= 0f && m2 <= 0f)
        {
            if (magnetOne != null) magnetOne.transform.position = magnetOneTargetPos;
            if (magnetTwo != null) magnetTwo.transform.position = magnetTwoTargetPos;
            yield break;
        }

        // animate simultaneously using per-magnet durations (use max to drive the loop)
        float maxDur = Mathf.Max(m1 > 0f ? m1 : 0f, m2 > 0f ? m2 : 0f);
        if (maxDur <= 0f) maxDur = 0.0001f;
        isExtending = true;
        float elapsedMag = 0f;
        while (elapsedMag < maxDur)
        {
            float t1 = (m1 > 0f) ? Mathf.Clamp01(elapsedMag / m1) : 1f;
            float t2 = (m2 > 0f) ? Mathf.Clamp01(elapsedMag / m2) : 1f;
            if (magnetOne != null)
                magnetOne.transform.position = Vector3.Lerp(magnetOneStartPos, magnetOneTargetPos, t1);
            if (magnetTwo != null)
                magnetTwo.transform.position = Vector3.Lerp(magnetTwoStartPos, magnetTwoTargetPos, t2);
            elapsedMag += Time.deltaTime;
            yield return null;
        }

        // Ensure final position
        if (magnetOne != null) magnetOne.transform.position = magnetOneTargetPos;
        if (magnetTwo != null) magnetTwo.transform.position = magnetTwoTargetPos;
        isExtending = false;
    }

    private IEnumerator RetractPlatform()
    {
        if (platform == null)
            yield break;

        // compute platform duration based on speed if requested (computed earlier in StartPuzzle)
        float pDur = platformDuration;

        // if duration is <= 0, snap and finish
        if (pDur <= 0f)
        {
            platform.transform.position = platformTargetPos;
            yield break;
        }

        isExtending = true;
        float elapsedPlat = 0f;
        while (elapsedPlat < pDur)
        {
            float t = Mathf.Clamp01(elapsedPlat / pDur);
            platform.transform.position = Vector3.Lerp(platformStartPos, platformTargetPos, t);
            elapsedPlat += Time.deltaTime;
            yield return null;
        }

        // Ensure final position
        platform.transform.position = platformTargetPos;
        isExtending = false;
    }

    IEnumerator ExtendLatticeParts()
    {
        if (!isCompleted)
        {
            // extend magnets then platform, waiting for each coroutine to finish
            yield return StartCoroutine(ExtendMagnets());
            yield return StartCoroutine(ExtendPlatform());

            // mark completed after successful extension
            isCompleted = true;
            yield break;
        }
        else
        {
            // retract magnets then platform
            yield return StartCoroutine(RetractMagnets());
            yield return StartCoroutine(RetractPlatform());

            // mark not completed after retraction
            isCompleted = false;
            yield break;
        }
    }
}
