/*
    Written by Brandon Wahl
    This Script manages the continuous movement of elevator walls to create an infinite elevator effect.
    It moves the walls downward at a specified speed and resets their position when they go below a certain point.

*/

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the continuous movement of elevator walls.
/// Creates an infinite elevator effect by moving walls downward and resetting them at the top.
/// </summary>
public class ElevatorWalls : MonoBehaviour
{
    [Header("Wall References")]
    [SerializeField] internal GameObject elevatorWall;
    [SerializeField] internal GameObject wallBelow;
    [SerializeField] internal GameObject wallWithDoor;
    
    internal bool isMoving = true;

    [Header("Movement Settings")]
    [Tooltip("The y position where walls reset to the top")]
    [SerializeField] [Range(-50f, 0f)] internal float yBounds = -22.4f;
    
    [Tooltip("Speed at which elevator walls move downward")]
    [SerializeField] [Range(0f, 50f)] internal float elevatorSpeed = 0f;

    [Tooltip("The y position where walls start after reset")]
    [SerializeField] [Range(0f, 100f)] internal float restartPoint = 28f;

    [SerializeField] private GameObject elevatorPlatform;
    internal float endYPos;

    private void Awake()
    {
        if(elevatorPlatform != null)
        {
            endYPos = elevatorPlatform.transform.position.y - 0.5f; // assuming platform height is 1 unit
        }
    }

    private void Start()
    {
        wallWithDoor.gameObject.SetActive(false);

        StartCoroutine(MoveWall(elevatorWall));
        StartCoroutine(MoveWall(wallBelow));
        StartCoroutine(MoveWall(wallWithDoor));
    } 

    /// <summary>
    /// Moves a single elevator wall downward and resets it when it goes below bounds.
    /// </summary>
    /// <param name="wall">The wall GameObject to move</param>
    private IEnumerator MoveWall(GameObject wall)
    {
        if(wall == null)
            yield break;

        while(isMoving)
        {
            Vector3 position = wall.transform.position;
            position.y -= elevatorSpeed * Time.deltaTime;
            wall.transform.position = position;

            // Reset wall to top when it goes below bounds - preserve original X and Z
            if(position.y <= yBounds)
            {
                position.y = restartPoint;
                wall.transform.position = position;
            }

            yield return null;
        }
    }
}