/*
Written by Brandon Wahl

Handles player movement and saves/loads player position

*/

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using System.Collections;
using System.ComponentModel;
public class PlayerMovement : MonoBehaviour, IDataPersistenceManager
{
    private CharacterController characterController;
    private InputReader input;

    [Tooltip("Speed of player")][SerializeField] internal float speed;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool shouldFaceMoveDirection = true;

    internal Vector3 currentMovement = Vector3.zero;

    [Header("Player Jump Settings")]
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("How high the player will jump")][SerializeField][Range(5f, 15)] private float jumpHeight;
    [SerializeField][Range(1f, 10)] private float doubleJumpHeight;
    [SerializeField] [Range(15, 50)] private float terminalVelocity = 20;
    [SerializeField][Range(.1f, 2)] private float fallSpeed;
    [SerializeField] private bool canDoubleJump;

    [Header("GroundCheck Variables")]
    [SerializeField] private Vector3 boxSize = new Vector3(.8f, .1f, .8f);
    [SerializeField] private float maxDistance;
    [Tooltip("Which layer the ground check detects for")] public LayerMask layerMask;
    private float verticalRotation;

    [Header("Dash Settings")]
    [SerializeField] [Range(1, 5)] private float dashSpeed;
    [SerializeField] private float dashTime;
    private float dashCurrentTime;
    [SerializeField] private float dashCoolDown;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

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
        // Debug checks
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is NULL! Assign your Cinemachine camera to Camera Transform field.");
            return;
        }
        
        if (characterController == null)
        {
            Debug.LogError("Character Controller is NULL!");
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Use InputReader instead of Input System callbacks
        Vector2 inputMove = input != null ? input.MoveInput : Vector2.zero;
        Vector3 moveDirection = forward * inputMove.y + right * inputMove.x;
        
        // Debug movement
        if (inputMove.magnitude > 0.1f)
        {
            // Debug.Log($"MoveInput: {inputMove}, MoveDirection: {moveDirection}, Speed: {speed}");
        }
        
        // Move horizontally
        Vector3 horizontalMovement = moveDirection * speed * Time.deltaTime;
        
        if (horizontalMovement.magnitude > 0.001f)
        {
            // Debug.Log($"Moving with: {horizontalMovement}");
            characterController.Move(horizontalMovement);
        }

        // Rotate player to face movement direction if enabled
        if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
        }
    }

    private void Jumping()
    {
        //Checks if player is grounded
        if (AmIGrounded())
        {
            currentMovement.y = 0;
            canDoubleJump = false;

            //Checks if the action map action is trigger
            if (input != null && input.JumpTrigger)
            {
                //Increases y pos according to the square root of the jump height multiplied by gravity
                currentMovement.y += Mathf.Sqrt((jumpHeight * (-gravity)));
                StartCoroutine(CanDoubleJump());
            }
        }
        else if (input != null && input.JumpTrigger && canDoubleJump)
        {
            currentMovement.y += doubleJumpHeight;
            StartCoroutine(CanDoubleJump());
        }
        //If the player is not grounded they will continuously fall until they are grounded
        else
        {
            if (input != null)
                input.JumpTrigger = false;

            currentMovement.y += gravity * Time.deltaTime;
            currentMovement.y += ((gravity * fallSpeed) * Time.deltaTime);
            currentMovement.y = Mathf.Clamp(currentMovement.y, -terminalVelocity, float.MaxValue);
        }

        // Apply vertical movement separately
        characterController.Move(new Vector3(0, currentMovement.y, 0) * Time.deltaTime);
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

    public void Dash()
    {
        dashCurrentTime -= Time.deltaTime;

        if (dashCurrentTime < -1)
        {
            dashCurrentTime = -1;
        }

        if (input != null && input.DashTrigger && dashCurrentTime <= 0)
        {
            // Use the current horizontal movement for dash direction
            Vector3 dashDirection = new Vector3(currentMovement.x, 0, currentMovement.z).normalized;
            if (dashDirection.magnitude < 0.1f) // If not moving, dash forward
            {
                dashDirection = transform.forward;
            }
            StartCoroutine(DashCoroutine(dashDirection * speed));
            input.DashTrigger = false;
        }
    }
    private IEnumerator DashCoroutine(Vector3 direction)
    {
        float starttime = Time.time;

        while (Time.time < starttime + dashTime)
        {
            characterController.Move(direction * dashSpeed * Time.deltaTime);
            dashCurrentTime = dashCoolDown;

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
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position - transform.up * maxDistance, boxSize);
    }


    //Draws the boxcast for debugging

}
