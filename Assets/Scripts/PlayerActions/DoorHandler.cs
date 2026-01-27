/*
    Written by Brandon Wahl

    This script handles the different types of door interactions with realistic door movements.
    Doors can open upwards, outwards, or inwards depending on the door type set in the inspector.
    Lock management is handled by the DoorInteractions component (part of UnlockableInteraction system).
*/


using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DoorHandler : MonoBehaviour
{
    public enum DoorState { Open, Closed }

    public enum DoorType { OpenUp, OpenOut, OpenIn }

    public DoorState currentDoorState;
    public DoorType doorType;

    private Vector3 doorPosOrigin;
    private Quaternion doorRotOrigin;
    private Vector3 targetDoorPos;
    private Quaternion targetDoorRot;
    private Vector3 targetPos;

    [SerializeField] private float doorUpTarget;
    [SerializeField] private float openSpeed = 2f;

    private bool isOpening = false;
    private bool isOpened = false;

    [Tooltip("Optional hinge pivot. If null, a pivot GameObject will be created at the door origin.")]
    [SerializeField] private Transform hingePivot;

    // Hinge variables that are used for OpenIn and OpenOut door types
    private Quaternion hingeStartRot;
    private Quaternion hingeTargetRot;
    private Coroutine hingeAnimCoroutine = null;

    // Store original parent to reparent door after using hinge pivot
    private Transform originalParent;

    private void Awake()
    {
        doorPosOrigin = this.transform.position;
        doorRotOrigin = this.transform.rotation;
    }

    /// <summary>
    /// Toggles the door between Open and Closed states.
    /// Lock checking is handled by DoorInteractions component.
    /// </summary>
    public void Interact()
    {
        switch (currentDoorState)
        {
            case DoorState.Open:
                CloseDoor();
                break;
            case DoorState.Closed:
                OpenDoor();
                break;
        }
    }

    private void OpenDoor()
    {
        Debug.Log("Opening the door.");
        currentDoorState = DoorState.Open;

        switch (doorType)
        {
            case DoorType.OpenUp:
                OpenUp();
                break;
            case DoorType.OpenOut:
                OpenOut();
                break;
            case DoorType.OpenIn:
                OpenIn();
                break;
        }
    }

    private void CloseDoor()
    {
        Debug.Log("Closing the door.");
        currentDoorState = DoorState.Closed;

        switch (doorType)
        {
            case DoorType.OpenUp:
                CloseUp();
                break;
            case DoorType.OpenOut:
                EnsurePivot();
                StartHingeAnimation(hingePivot.rotation, doorRotOrigin, 1f / openSpeed);
                break;
            case DoorType.OpenIn:
                EnsurePivot();
                StartHingeAnimation(hingePivot.rotation, doorRotOrigin, 1f / openSpeed);
                break;
        }
    }

    private void OpenOut()
    {
        Debug.Log("Door opening outwards.");
        // Use hinge pivot to rotate outwards so the door stays locked in its socket
        EnsurePivot();
        hingeStartRot = hingePivot.rotation;
        hingeTargetRot = hingeStartRot * Quaternion.Euler(0f, -90f, 0f);
        StartHingeAnimation(hingeStartRot, hingeTargetRot, 1f / openSpeed);
    }

    private void OpenIn()
    {
        Debug.Log("Door opening inwards.");
        // Use hinge pivot to rotate inwards so the door stays locked in its socket
        EnsurePivot();
        hingeStartRot = hingePivot.rotation;
        hingeTargetRot = hingeStartRot * Quaternion.Euler(0f, 90f, 0f);
        StartHingeAnimation(hingeStartRot, hingeTargetRot, 1f / openSpeed); 
    }

    // These two functions handle the OpenUp door type
    private void OpenUp()
    {
        StopAllCoroutines();
        StartCoroutine(OpenUpCoroutine());
    }

    private void CloseUp()
    {
        StopAllCoroutines();
        StartCoroutine(CloseUpCoroutine());
    }

    // Ensure the hinge pivot exists and is parent of the door
    private void EnsurePivot()
    {
        // If the hinge pivot doesn't exist, create it at the door's origin
        if (hingePivot == null)
        {
            GameObject go = new GameObject(this.gameObject.name + "PivotPoint");
            go.transform.position = doorPosOrigin;
            go.transform.rotation = doorRotOrigin;
            // parent pivot to the door's original parent to keep hierarchy
            go.transform.SetParent(this.transform.parent, true);
            hingePivot = go.transform;
            // store original parent and reparent the door to hinge while preserving world transform
            originalParent = this.transform.parent;
            this.transform.SetParent(hingePivot, true);
        }
        else
        {
            //if there is a pivot point already, just make sure the door is parented to it
            if (this.transform.parent != hingePivot)
            {
                originalParent = this.transform.parent;
                this.transform.SetParent(hingePivot, true);
            }
        }
    }

    // Start hinge animation coroutine
    private void StartHingeAnimation(Quaternion from, Quaternion to, float duration)
    {
        if (hingeAnimCoroutine != null)
            StopCoroutine(hingeAnimCoroutine);
        hingeAnimCoroutine = StartCoroutine(AnimateHinge(from, to, duration));
    }

    // Coroutine to animate the hinge rotation smoothly over time. from == starting rotation, to == target rotation
    private IEnumerator AnimateHinge(Quaternion from, Quaternion to, float duration)
    {
        if (hingePivot == null)
            yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Animate the hinge rotation smoothly over time
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            hingePivot.rotation = Quaternion.Slerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        hingePivot.rotation = to;
        hingeAnimCoroutine = null;
    }

    // Coroutines for opening and closing the door upwards
    private IEnumerator OpenUpCoroutine()
    {
        targetDoorPos = doorPosOrigin + new Vector3(0f, doorUpTarget, 0f);

        if(this.transform.position == targetDoorPos)
        {
            isOpened = true;
            yield break;
        }

        while (Vector3.Distance(this.transform.position, targetDoorPos) > 0.01f)
        {
            float t = Mathf.Clamp01(openSpeed * Time.deltaTime);
            this.transform.position = Vector3.Lerp(this.transform.position, targetDoorPos, t);
            yield return null;
        }
        // mark opened when finished
        isOpened = true;
        yield return null;
    }

    private IEnumerator CloseUpCoroutine()
    {
        targetDoorPos = doorPosOrigin;

    
        if(this.transform.position == targetDoorPos)
        {
            isOpened = false;
            yield break;
        }

        while (Vector3.Distance(this.transform.position, targetDoorPos) > 0.01f)
        {
            float t = Mathf.Clamp01(openSpeed * Time.deltaTime);
            this.transform.position = Vector3.Lerp(this.transform.position, targetDoorPos, t);
            yield return null;
        }
        // mark closed when finished
        isOpened = false;
        yield return null;
    }
}