/*
Written by Brandon Wahl

Handles player movement and saves/loads player position
*
* edited by Will T
* 
* Added dash functionality and modified jump to include double jump
* Also added animator integration
*/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private static CharacterController characterController;
    public static bool isGrounded { get {return characterController.isGrounded; } }

    [Header("Player Animator")]
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;

    [Header("Player Movement Settings")]
    [Tooltip("Speed of player"), SerializeField] internal float speed;
    [SerializeField] private float maxNormalSpeed = 5f;
    [SerializeField] private float guardWalkingSpeed = 2.5f;
    [SerializeField, Range(0f, 20f)] private float friction = 3f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool shouldFaceMoveDirection = true;

    internal Vector3 currentMovement = Vector3.zero;

    [Header("Player Jump Settings")]
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("How high the player will jump")][SerializeField][Range(10, 50)] private float jumpForce;
    [SerializeField, Range(10, 50)] private float doubleJumpForce;
    [SerializeField, Range(0, 15)] private float airAttackHopForce = 5;

    [SerializeField, Range(1, 50)] private float terminalVelocity = 20;
    [SerializeField] private bool canDoubleJump;

    [Header("GroundCheck Variables")]
    [SerializeField] private Vector3 boxSize = new Vector3(.8f, .1f, .8f);
    [SerializeField] private float maxDistance;
    [Tooltip("Which layer the ground check detects for")] public LayerMask layerMask;

    [Header("Dash Settings")]
    [SerializeField] [Range(1, 5)] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCoolDown;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    private float maxRunningSpeed => CombatManager.isGuarding ? guardWalkingSpeed : maxNormalSpeed;

    private bool canDash = true;

    private Vector3 forward => new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
    private Vector3 right => new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z);

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Assign PlayerInput to InputReader
        InputReader.AssignPlayerInput(GetComponent<PlayerInput>());
    }
    private void OnEnable()
    {
        PlayerAttackManager.onAttack += AerialAttackHop;
    }

    private void OnDisable()
    {
        PlayerAttackManager.onAttack -= AerialAttackHop;
    }

    // Update is called once per frame
    public void FixedUpdate()
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

        if (animator == null)
        {
            Debug.LogWarning("Animator is NULL! Player animations will not play.");
        }
        
        Move();

        if (_jumpAction != null && _jumpAction.action != null && _jumpAction.action.triggered)
            OnJump();
        else
            animator.SetBool("jumpTrigger", false);

        if (_dashAction != null && _dashAction.action != null && _dashAction.action.triggered)
            OnDash();
        
        if (isGrounded)
        {
            animator.SetBool("isGrounded", true);
        }
        else
        {
            animator.SetBool("isGrounded", false);
        }

        ApplyMovement();
    }

    private void Move()
    {
        // Use InputReader instead of Input System callbacks
        Vector2 inputMove = InputReader.MoveInput;


        // player input detected
        if (inputMove != Vector2.zero && !InputReader.inputBusy)
        {
            if (animator != null)
                animator.SetBool("moving", true);

            Vector3 moveDirection = forward * inputMove.y + right * inputMove.x;
            moveDirection.Normalize();

            // Move horizontally, apply speed and guard modifier if guarding
            Vector3 horizontalMovement = moveDirection * speed;

            if (horizontalMovement.magnitude > 0.001f)
            {
                //characterController.Move(horizontalMovement);
                currentMovement += horizontalMovement;
            }

            // Rotate player to face movement direction if enabled
            if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }

        // no player input detected
        else
        {
            if (animator != null)
                animator.SetBool("moving", false);

            // Apply friction when no input is detected
            currentMovement.x = Mathf.Lerp(currentMovement.x, 0, friction * Time.deltaTime);
            currentMovement.z = Mathf.Lerp(currentMovement.z, 0, friction * Time.deltaTime);
        }
    }

    private void OnJump()
    {
        // checks to see if the player can jump or double jump
        if (characterController.isGrounded)
        {
            Debug.Log("Grounded Jumped");
            currentMovement.y = jumpForce;

            canDoubleJump = false;
            StartCoroutine(CanDoubleJump());

            // animator trigger jump
            if (animator != null)
            {
                animator.SetBool("jumpTrigger", true);
                animator.SetBool("isGrounded", false);
            }
        }
        else if (canDoubleJump)
        {
            Debug.Log("Double Jumped");
            currentMovement.y += doubleJumpForce;
        }
    }

    private void OnDash()
    {
       if (canDash && !InputReader.inputBusy)
        {
            canDash = false;
            InputReader.inputBusy = true;

            // Use the current horizontal movement for dash direction
            Vector3 dashDirection = (forward * InputReader.MoveInput.y) + (right * InputReader.MoveInput.x);
            dashDirection.Normalize();

            if (dashDirection.magnitude < 0.1f) // If not moving, dash forward
            {
                dashDirection = transform.forward;
            }
            StartCoroutine(DashCoroutine(dashDirection));
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

    private IEnumerator DashCoroutine(Vector3 direction)
    {
        float starttime = Time.time;

        animator.SetBool("dashTrigger", true);

        yield return null;

        animator.SetBool("dashTrigger", false);


        while (Time.time < starttime + dashTime)
        {
            characterController.Move(direction * dashSpeed * Time.deltaTime);

            yield return null;
        }

        InputReader.inputBusy = false;

        yield return new WaitForSeconds(dashCoolDown);
        canDash = true;
        
    }

    private void AerialAttackHop()
    {
        if (characterController.isGrounded) return;

        currentMovement.y = airAttackHopForce;
    }

    private void ApplyMovement()
    {
        // apply gravity
        currentMovement.y = Mathf.Clamp(currentMovement.y - gravity, -terminalVelocity, terminalVelocity);

        // apply max running speed
        Vector3 horizontalMovement = new Vector3(currentMovement.x, 0, currentMovement.z);
        if (horizontalMovement.magnitude > maxRunningSpeed)
        {
            horizontalMovement = horizontalMovement.normalized * maxRunningSpeed;
            currentMovement.x = horizontalMovement.x;
            currentMovement.z = horizontalMovement.z;
        }

        // move the character controller
        characterController.Move(currentMovement * Time.deltaTime);

        // reset vertical movement when grounded
        if (characterController.isGrounded && currentMovement.y < 0)
        {
            currentMovement.y = -1f; // small negative value to keep the player grounded
            animator.SetBool("isGrounded", true);
        }
    }
}
