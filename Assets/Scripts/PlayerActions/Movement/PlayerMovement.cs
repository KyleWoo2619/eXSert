/*
Written by Brandon Wahl

Handles player movement and saves/loads player position

*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.ComponentModel;
public class PlayerMovement : MonoBehaviour, IDataPersistenceManager
{
    private CharacterController characterController;
    private InputReader input;

    [Tooltip("Speed of player")][SerializeField] internal float speed;

    internal Vector3 currentMovement = Vector3.zero;

    [Header("Player Jump Settings")]
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("How high the player will jump")][SerializeField][Range(5f, 15)] private float jumpHeight;
    [SerializeField] [Range(15, 50)] private float terminalVelocity = 20;
    [SerializeField][Range(.1f, 2)] private float fallSpeed;
    [SerializeField] private bool canDoubleJump;

    [Header("GroundCheck Variables")]
    [SerializeField] private Vector3 boxSize = new Vector3(.8f, .1f, .8f);
    [SerializeField] private float maxDistance;
    [Tooltip("Which layer the ground check detects for")] public LayerMask layerMask;


    [Header("Player Rotation Settings")]
    [Tooltip("Mouse Sensitivity")][SerializeField][Range(1, 3)] internal float mouseSens;
    [Tooltip("Range of looking up or down")][SerializeField][Range(10, 100)] private float upDownRange;
    private float verticalRotation;

    [Header("Dash Settings")]
    [SerializeField] [Range(1, 5)] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        input = InputReader.Instance;
    }

    // Update is called once per frame
    public void Update()
    {
        Move();
        Jumping();
        Rotation();
        Dash();
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

    private void Jumping()
    {
        float verticalVel;
        verticalVel = characterController.velocity.y;


        //Checks if player is grounded
        if (AmIGrounded())
        {
            currentMovement.y = 0;
            
            canDoubleJump = false;

            //Checks if the action map action is trigger
            if (input.JumpTrigger)
            {
                //Increases y pos according to the square root of the jump height multiplied by gravity
                currentMovement.y += Mathf.Sqrt((jumpHeight * (-gravity)));
                StartCoroutine(CanDoubleJump());
            }

            

        }
        else if (input.JumpTrigger && canDoubleJump)
        {
            currentMovement.y += jumpHeight / 2;
            StartCoroutine(CanDoubleJump());
        }
        //If the player is not grounded they will continously fall until they are grounded
        else
        {

            input.JumpTrigger = false;

            currentMovement.y += gravity * Time.deltaTime;

            currentMovement.y += ((gravity * fallSpeed) * Time.deltaTime);

            currentMovement.y = Mathf.Clamp(currentMovement.y, -terminalVelocity, float.MaxValue);
            
        }

    }

    private IEnumerator CanDoubleJump()
    {
        if (!canDoubleJump)
        {
            yield return new WaitForSeconds(.1f);
            canDoubleJump = true;
        }
        else
        {
            yield return new WaitForSeconds(.01f);
            canDoubleJump = false;
        }
    }

    public void Rotation()
    {
        input.mouseSens = mouseSens;
        float mouseYInput = invertYAxis ? -input.LookInput.y : input.LookInput.y;

        float mouseXRotation = input.LookInput.x * input.mouseSens;
        transform.Rotate(0, mouseXRotation, 0);

        verticalRotation -= mouseYInput * input.mouseSens;

        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        
    }

    public void Dash()
    {
        dashCooldown -= Time.deltaTime;

        if (dashCooldown < -1)
        {
            dashCooldown = -1;
        }

        if(input.DashTrigger && dashCooldown <= 0)
        {
            StartCoroutine(DashCoroutine(currentMovement));
        }
    }
    private IEnumerator DashCoroutine(Vector3 direction)
    {
        float startime = Time.time;

        while (Time.time < startime + dashTime)
        {
            characterController.Move(direction * dashSpeed * Time.deltaTime);
            dashCooldown = 3;

            yield return null;
        }
    }

    //Returns true or false if boxcast collides with the layermask
    public bool AmIGrounded()
    {
        if (Physics.BoxCast(transform.position, boxSize, -transform.up, transform.rotation, maxDistance, layerMask))
        {

            return true;
        }
        else
        {

            return false;
        }
    }

    //Draws the boxcast for debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position - transform.up * maxDistance, boxSize);
    }
}
