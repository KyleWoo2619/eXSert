using System.Collections;
using UnityEngine;

/*
Written by Kyle
This script manages a trigger zone that opens and closes a two-part door when the player enters or exits the zone.
The door consists of a top and bottom part that slide apart vertically when opened and come back together when closed.
*/

[RequireComponent(typeof(BoxCollider))]
public class DoorTriggerZone : MonoBehaviour
{
    [Header("Door Pieces")]
    [SerializeField] private Transform topDoor;      // top slab/panel
    [SerializeField] private Transform bottomDoor;   // bottom slab/panel

    [Header("Motion")]
    [SerializeField] private float topOpenDistance = 2.0f;     // meters upward
    [SerializeField] private float bottomOpenDistance = 2.0f;  // meters downward
    [SerializeField] private float moveDuration = 0.75f;       // seconds per open/close
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Player Filter")]
    [SerializeField] private string playerTag = "Player";       // set your player’s tag

    // cached start/end positions
    private Vector3 _topClosedPos, _bottomClosedPos;
    private Vector3 _topOpenPos, _bottomOpenPos;

    private Coroutine _motion;
    private int _overlapCount = 0;   // supports multiple colliders entering (player + weapon, etc.)

    private void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true; // make sure it’s a trigger
    }

    private void Awake()
    {
        _topClosedPos = topDoor.position;
        _bottomClosedPos = bottomDoor.position;

        _topOpenPos = _topClosedPos + Vector3.up * topOpenDistance;
        _bottomOpenPos = _bottomClosedPos + Vector3.down * bottomOpenDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _overlapCount++;
        if (_overlapCount == 1) StartMove(open: true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (_overlapCount == 0) StartMove(open: false);
    }

    private void StartMove(bool open)
    {
        if (_motion != null) StopCoroutine(_motion);
        _motion = StartCoroutine(MoveDoors(open));
    }

    private IEnumerator MoveDoors(bool open)
    {
        Vector3 topFrom = topDoor.position;
        Vector3 botFrom = bottomDoor.position;

        Vector3 topTo = open ? _topOpenPos : _topClosedPos;
        Vector3 botTo = open ? _bottomOpenPos : _bottomClosedPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, moveDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            topDoor.position    = Vector3.LerpUnclamped(topFrom, topTo, k);
            bottomDoor.position = Vector3.LerpUnclamped(botFrom, botTo, k);
            yield return null;
        }

        topDoor.position    = topTo;
        bottomDoor.position = botTo;
        _motion = null;
    }
}
