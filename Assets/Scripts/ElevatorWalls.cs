/*
    Written by Brandon Wahl

    This script handles the movement for the elevator walls. Instead of having a long corridor and moving the platform down,
    there is two sets of elevator walls that move upwards. When one set reaches a certain height, it resets to the bottom,
     creating an infinite elevator wall effect.
*/

using UnityEngine;

public class ElevatorWalls : MonoBehaviour
{
    public GameObject elevatorWall;
    public GameObject wallBelow; 

    [Space(10)]

    [Header("Elevator Movement Settings")]
    [Tooltip("The y position at which the elevator walls will reset to the bottom")]
    [SerializeField] private float yBounds = -22.4f;
    
    [Tooltip("Speed at which the elevator walls move upwards")]
    [SerializeField] private float elevatorSpeed;

    [Tooltip("The y position the elevator walls will reset to")]
    [SerializeField] private float restartPoint = 28f;

    [Space(10)]

    [SerializeField] internal bool isMoving = false;
    
    void Update()
    {
        ElevatorMovement();
    }

    public void ElevatorMovement(){
        if(isMoving)
        {
            if(elevatorWall != null)
            {
                elevatorWall.transform.position -= new Vector3(0, elevatorSpeed * Time.deltaTime, 0);

            if(elevatorWall.transform.position.y <= yBounds)
            {
                // Reset positions
                elevatorWall.transform.position = new Vector3(elevatorWall.transform.position.x, restartPoint, 0f);
            }
            
            } 

            if(wallBelow != null)
            {
                wallBelow.transform.position -= new Vector3(0, elevatorSpeed * Time.deltaTime, 0);

                if(wallBelow.transform.position.y <= yBounds)
                {
                    wallBelow.transform.position = new Vector3(wallBelow.transform.position.x, restartPoint, 0f);
                }
            }
        } 
        else 
        {
            return;
        }
    }
}
