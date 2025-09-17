/*
Written by Brandon Wahl

Handles player movement and saves/loads player position

*/

using UnityEngine;
using UnityEditor;
using System.Collections;
public class PlayerMovement : MonoBehaviour, IDataPersistenceManager
{
    private CharacterController characterController;
    private InputReader input;

    [Tooltip("Speed of player")][SerializeField][Range(4, 8)] private float speed;

    internal Vector3 currentMovement = Vector3.zero;

    //GroundCheck Variables


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        input = InputReader.Instance;
    }

    // Update is called once per frame
    public void Update()
    {
        Move();

    }

    public void LoadData(GameData data)
    {
        this.transform.position = data.playerPos;

    }

    public void SaveData(GameData data)
    {
        data.playerPos = this.transform.position;
    }


    private void Move()
    {
        //Assigns the movement input gather to a local vector3
        Vector3 horizontalMovement = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);

        //Whenever player rotation changes, the player's direction changes accordingly
        Vector3 worldDirection = transform.TransformDirection(horizontalMovement);

        //Normalizes worldDirection
        worldDirection.Normalize();

        //Mulitplies the direction moved by player speed
        currentMovement.x = worldDirection.x * speed;
        currentMovement.z = worldDirection.z * speed;

        //Moves the player
        characterController.Move(currentMovement * Time.deltaTime);
    }


}
