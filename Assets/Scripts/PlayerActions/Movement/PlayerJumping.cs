/*
Written by Brandon Wahl

Manages the player's ability to jump

*/

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJumping : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private InputReader input;
    private GroundCheck groundCheck;

    [SerializeField] private float gravity = 9.81f;

    [Tooltip("How high the player will jump")][SerializeField][Range(1, 10)] float jumpHeight;

    private CharacterController characterController;
    void Start()
    {
        playerMovement = this.transform.GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        input = InputReader.Instance;
        groundCheck = GetComponent<GroundCheck>();
    }

    // Update is called once per frame
    public void Update()
    {
        Jumping();
    }

    private void Jumping()
    {

        //Checks if player is grounded
        if (groundCheck.AmIGrounded())
        {

            jumpHeight = 5;

            playerMovement.currentMovement.y = 0;

            //Checks if the action map action is trigger
            if (input.JumpTrigger)
            {
                //Increases y pos according to the square root of the jump height multiplied by gravity
                playerMovement.currentMovement.y += Mathf.Sqrt(jumpHeight * gravity);
                Debug.Log("Hi");

                characterController.Move(playerMovement.currentMovement * Time.deltaTime);
            }

        }
        //If the player is not grounded they will continously fall until they are grounded
        else
        {
            playerMovement.currentMovement.y -= gravity * Time.deltaTime;
        }


    }


}
