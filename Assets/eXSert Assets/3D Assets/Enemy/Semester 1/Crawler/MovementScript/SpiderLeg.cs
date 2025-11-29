using System.Collections;
using UnityEngine;

public class SpiderLeg : MonoBehaviour
{
    [Header("References")]
    public Transform body;
    public Transform home;          // LegX_Home
    public Transform target;        // LegX_Target
    public SpiderLegGroup myGroup;  // GroupA or GroupB
    public SpiderLegGroup otherGroup; // the opposite group

    [Header("Stepping Settings")]
    public float raycastHeight = 1.0f;
    public float stepDistance = 0.7f;
    public float stepHeight = 0.3f;
    public float stepSpeed = 5f;
    public LayerMask groundMask;

    [HideInInspector] public bool isStepping;

    Vector3 _lastPlantedPos;

    void Start()
    {
        _lastPlantedPos = target.position;
    }

    void Update()
    {
        Vector3 desired = GetDesiredFootPosition();
        float dist = Vector3.Distance(_lastPlantedPos, desired);

        // ----- GROUP LOGIC -----
        bool otherGroupMoving = otherGroup != null && otherGroup.IsAnyStepping;

        // Allow this leg to step if:
        // 1) it's not already stepping
        // 2) the opposite group is NOT stepping (so groups alternate)
        // 3) it's far enough from its desired position
        if (!isStepping && !otherGroupMoving && dist > stepDistance)
        {
            StartCoroutine(StepTo(desired));
        }
    }

    Vector3 GetDesiredFootPosition()
    {
        Vector3 homePos = home.position;
        Vector3 rayOrigin = homePos + Vector3.up * raycastHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask))
        {
            return hit.point;
        }

        return homePos;
    }

    IEnumerator StepTo(Vector3 newPos)
    {
        isStepping = true;

        Vector3 start = target.position;
        Vector3 mid = (start + newPos) * 0.5f + Vector3.up * stepHeight;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * stepSpeed;
            t = Mathf.Clamp01(t);

            Vector3 p1 = Vector3.Lerp(start, mid, t);
            Vector3 p2 = Vector3.Lerp(mid, newPos, t);
            target.position = Vector3.Lerp(p1, p2, t);

            yield return null;
        }

        _lastPlantedPos = newPos;
        isStepping = false;
    }
}
